using Microsoft.AspNetCore.Authorization; // Proporciona atributos para limitar el acceso a usuarios autenticados.
using Microsoft.AspNetCore.Mvc; // Proporciona Controller, acciones MVC y respuestas como View o Redirect.
using Microsoft.EntityFrameworkCore; // Permite identificar errores al guardar cambios con Entity Framework Core.
using MiProyectoMvc.Mappings; // Incluye conversiones entre entidades Categoria y modelos de vista.
using MiProyectoMvc.Repositories; // Incluye IUnitOfWork para acceder a repositorios y guardar cambios.
using MiProyectoMvc.ViewModels; // Incluye los modelos que transportan datos entre las vistas y el controlador.

namespace MiProyectoMvc.Controllers // Agrupa los controladores MVC de la aplicacion.
{
    // Este controlador administra las categorias y conserva la categoria General como asignacion por defecto.
    [Authorize] // Exige que el usuario inicie sesion antes de ejecutar cualquiera de estas acciones.
    public class CategoriasController : Controller // Hereda las funciones comunes de un controlador MVC.
    {
        private const int CategoriaGeneralId = 1; // Identificador reservado para la categoria General que no puede eliminarse.

        private readonly IUnitOfWork _unitOfWork; // Mantiene el acceso a los repositorios y al guardado compartido.

        // El contenedor de dependencias proporciona la unidad de trabajo configurada para la solicitud.
        public CategoriasController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork; // Guarda la dependencia para usar los repositorios de categorias.
        }

        // GET /Categorias: muestra el listado de categorias.
        public async Task<IActionResult> Index()
        {
            var categorias = await _unitOfWork.Categorias.GetAllResumenAsync(); // Consulta las categorias con el resumen que necesita la lista.
            return View(new CategoriaIndexViewModel { Categorias = categorias }); // Entrega los datos a la vista Index.
        }

        // GET /Categorias/Create: presenta un formulario vacio para crear una categoria.
        public IActionResult Create()
        {
            return View(new CategoriaFormViewModel()); // Crea el modelo inicial y renderiza el formulario.
        }

        [HttpPost] // Acepta exclusivamente solicitudes POST que envian el formulario.
        [ValidateAntiForgeryToken] // Comprueba el token antifalsificacion generado por el formulario.
        // POST /Categorias/Create: valida y guarda una categoria nueva.
        public async Task<IActionResult> Create(CategoriaFormViewModel model)
        {
            // Comprueba duplicados solo cuando el nombre contiene algun caracter que no sea espacio.
            if (!string.IsNullOrWhiteSpace(model.Nombre) &&
                await _unitOfWork.Categorias.NombreExistsAsync(model.Nombre))
            {
                ModelState.AddModelError(nameof(model.Nombre), "Ya existe una categoría con ese nombre."); // Asocia el error al campo Nombre para mostrarlo en el formulario.
            }

            // No intenta guardar si fallan las reglas de validacion del modelo o el control de duplicados.
            if (!ModelState.IsValid)
            {
                return View(model); // Vuelve a mostrar los valores escritos junto con sus mensajes de error.
            }

            // Captura errores de base de datos que pueden ocurrir incluso despues de validar el nombre.
            try
            {
                await _unitOfWork.Categorias.AddAsync(model.ToEntity()); // Convierte el formulario en entidad y la agrega al contexto.
                await _unitOfWork.SaveAsync(); // Confirma en una sola operacion los cambios pendientes de todos los repositorios.
            }
            catch (DbUpdateException) // Captura un fallo al persistir la categoria en la base de datos.
            {
                // Si otro proceso creo el mismo nombre al mismo tiempo, presenta un error de duplicado al usuario.
                if (await _unitOfWork.Categorias.NombreExistsAsync(model.Nombre))
                {
                    ModelState.AddModelError(nameof(model.Nombre), "Ya existe una categoría con ese nombre."); // Informa el conflicto junto al campo correspondiente.
                    return View(model); // Conserva el formulario para que el usuario corrija el nombre.
                }

                throw; // Propaga errores diferentes para que no se oculten fallos inesperados.
            }

            TempData["Ok"] = "Categoría creada correctamente."; // Guarda una confirmacion temporal para mostrarla despues de redirigir.
            return RedirectToAction(nameof(Index)); // Regresa al listado aplicando el patron POST-Redirect-GET.
        }

        // GET /Categorias/Edit/5: carga el formulario con la categoria existente.
        public async Task<IActionResult> Edit(int id)
        {
            var categoria = await _unitOfWork.Categorias.GetByIdAsync(id); // Busca la categoria indicada por la ruta.
            return categoria is null ? NotFound() : View(categoria.ToFormViewModel()); // Responde 404 si no existe; si existe, prepara y muestra el formulario.
        }

        [HttpPost] // Acepta exclusivamente el envio POST del formulario de edicion.
        [ValidateAntiForgeryToken] // Rechaza formularios que no incluyan el token antifalsificacion esperado.
        // POST /Categorias/Edit/5: valida los datos y actualiza la categoria.
        public async Task<IActionResult> Edit(int id, CategoriaFormViewModel model)
        {
            // Impide que el identificador de la URL y el del formulario apunten a categorias distintas.
            if (id != model.Id)
            {
                return BadRequest(); // Devuelve HTTP 400 cuando los identificadores no coinciden.
            }

            var categoria = await _unitOfWork.Categorias.GetByIdAsync(id); // Obtiene la entidad que se va a modificar.
            if (categoria is null)
            {
                return NotFound(); // Devuelve HTTP 404 si la categoria fue eliminada o no existe.
            }

            // Permite conservar el nombre de esta categoria, pero impide usar el de otra.
            if (!string.IsNullOrWhiteSpace(model.Nombre) &&
                await _unitOfWork.Categorias.NombreExistsAsync(model.Nombre, id))
            {
                ModelState.AddModelError(nameof(model.Nombre), "Ya existe una categoría con ese nombre."); // Marca el nombre duplicado como error de validacion.
            }

            // Conserva la informacion enviada si el modelo contiene errores.
            if (!ModelState.IsValid)
            {
                return View(model); // Vuelve al formulario mostrando los errores detectados.
            }

            model.ApplyTo(categoria); // Copia a la entidad solo los valores editables del formulario.

            // Persiste los cambios y maneja la posible carrera por un nombre duplicado.
            try
            {
                await _unitOfWork.Categorias.UpdateAsync(categoria); // Marca la categoria actualizada en el repositorio.
                await _unitOfWork.SaveAsync(); // Guarda la modificacion usando la unidad de trabajo compartida.
            }
            catch (DbUpdateException) // Captura errores producidos durante la escritura en la base de datos.
            {
                // Verifica si la causa del fallo fue que otra solicitud registro el mismo nombre.
                if (await _unitOfWork.Categorias.NombreExistsAsync(model.Nombre, id))
                {
                    ModelState.AddModelError(nameof(model.Nombre), "Ya existe una categoría con ese nombre."); // Vincula el mensaje al campo Nombre.
                    return View(model); // Permite corregir el formulario sin perder los datos ingresados.
                }

                throw; // Deja subir cualquier error de base de datos que no sea un duplicado conocido.
            }

            TempData["Ok"] = "Categoría actualizada correctamente."; // Prepara un mensaje para la proxima solicitud.
            return RedirectToAction(nameof(Index)); // Vuelve al listado para evitar reenviar el formulario al actualizar.
        }

        // GET /Categorias/Delete/5: muestra la confirmacion y los datos asociados a la categoria.
        public async Task<IActionResult> Delete(int id)
        {
            var categoria = await _unitOfWork.Categorias.GetByIdAsync(id); // Busca la categoria que el usuario quiere eliminar.
            if (categoria is null)
            {
                return NotFound(); // Responde 404 si el identificador no corresponde a una categoria existente.
            }

            var count = await _unitOfWork.Categorias.CountProductosAsync(id); // Cuenta los productos que dependen de esta categoria.

            // Construye el modelo con los datos que la pagina de confirmacion necesita presentar.
            return View(new CategoriaDeleteViewModel
            {
                Id = categoria.Id, // Identificador que se enviara al confirmar la eliminacion.
                Nombre = categoria.Nombre, // Nombre que permite reconocer la categoria seleccionada.
                Descripcion = categoria.Descripcion, // Descripcion que ayuda a confirmar el elemento correcto.
                ProductosCount = count // Cantidad de productos relacionados que la vista puede advertir.
            });
        }

        [HttpPost] // Ejecuta la eliminacion unicamente mediante una solicitud POST.
        [ActionName("Delete")] // Publica la accion con el nombre Delete aunque el metodo se llame DeleteConfirmed.
        [ValidateAntiForgeryToken] // Protege la solicitud frente a envios falsificados desde otro sitio.
        // POST /Categorias/Delete/5: verifica las restricciones y elimina la categoria.
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // La categoria General se reserva como valor predeterminado y no debe eliminarse.
            if (id == CategoriaGeneralId)
            {
                TempData["Error"] = "La categoría General no se puede eliminar."; // Informa por que se rechazo la operacion.
                return RedirectToAction(nameof(Index)); // Regresa al listado sin modificar la base de datos.
            }

            var count = await _unitOfWork.Categorias.CountProductosAsync(id); // Comprueba si hay productos que mantengan una relacion con la categoria.
            // Evita eliminar categorias que aun estan siendo utilizadas por productos.
            if (count > 0)
            {
                TempData["Error"] = $"No se puede eliminar la categoría: tiene {count} producto(s) asociado(s)."; // Explica el bloqueo e indica cuantos productos la usan.
                return RedirectToAction(nameof(Index)); // Regresa al listado conservando la categoria.
            }

            var marcadaParaEliminar = await _unitOfWork.Categorias.DeleteAsync(id); // Solicita al repositorio marcar la categoria para eliminar.
            // El repositorio devuelve false cuando el registro ya no existe.
            if (!marcadaParaEliminar)
            {
                TempData["Error"] = "No se pudo encontrar la categoría."; // Comunica que no habia una categoria que eliminar.
                return RedirectToAction(nameof(Index)); // Regresa al listado sin intentar guardar.
            }

            // Guarda la eliminacion y contempla que se haya asociado un producto en paralelo.
            try
            {
                await _unitOfWork.SaveAsync(); // Confirma en la base los cambios pendientes de la unidad de trabajo.
                TempData["Ok"] = "Categoría eliminada correctamente."; // Prepara una confirmacion para mostrar tras la redireccion.
            }
            catch (DbUpdateException) // Detecta un posible conflicto de integridad al borrar la fila.
            {
                // Vuelve a contar productos para saber si una relacion nueva provoco el conflicto.
                if (await _unitOfWork.Categorias.CountProductosAsync(id) > 0)
                {
                    TempData["Error"] = "No se puede eliminar la categoría porque tiene productos asociados."; // Explica que la relacion impide eliminar la categoria.
                }
                else
                {
                    throw; // Propaga errores no explicados por la existencia de productos asociados.
                }
            }

            return RedirectToAction(nameof(Index)); // Finaliza regresando al listado para evitar repetir el POST.
        }
    }
}
