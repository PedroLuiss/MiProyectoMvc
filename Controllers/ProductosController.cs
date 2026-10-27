using System.ClientModel; // Proporciona la excepcion HTTP usada por el SDK del proveedor de IA.
using Microsoft.AspNetCore.Authorization; // Proporciona el atributo que restringe el acceso a usuarios autenticados.
using Microsoft.AspNetCore.Mvc; // Proporciona los tipos base y resultados de las acciones MVC.
using Microsoft.AspNetCore.Mvc.Rendering; // Proporciona SelectListItem para construir opciones de listas desplegables.
using Microsoft.EntityFrameworkCore; // Proporciona el tipo de error que puede ocurrir al guardar en la base de datos.
using MiProyectoMvc.DTOs; // Incluye el tipo que recibe los datos JSON enviados al endpoint de sugerencias.
using MiProyectoMvc.Mappings; // Incluye conversiones entre entidades de producto y modelos de vista.
using MiProyectoMvc.Repositories; // Incluye IUnitOfWork y sus repositorios de productos y categorias.
using MiProyectoMvc.Services; // Incluye el servicio que guarda y elimina archivos de imagen.
using MiProyectoMvc.ViewModels; // Incluye los modelos de datos usados por las vistas de productos.

namespace MiProyectoMvc.Controllers // Agrupa los controladores MVC de la aplicacion.
{
    // Protege todas las acciones: solo usuarios autenticados pueden administrar el catalogo.
    [Authorize] // Aplica la autorizacion a todas las acciones de este controlador.
    public class ProductosController : Controller // Hereda las utilidades comunes para atender solicitudes MVC.
    {
        private readonly IUnitOfWork _unitOfWork; // Coordina el acceso a repositorios y el guardado en la base de datos.
        private readonly IImagenStorage _imagenStorage; // Abstrae el almacenamiento y la eliminacion de imagenes.
        private readonly AIService _aiService; // Genera sugerencias de descripcion y precio para productos.
        private readonly ILogger<ProductosController> _logger; // Registra los fallos al solicitar sugerencias de IA.

        // Recibe las dependencias desde el contenedor de servicios de ASP.NET Core.
        public ProductosController(
            IUnitOfWork unitOfWork,
            IImagenStorage imagenStorage,
            AIService aiService,
            ILogger<ProductosController> logger)
        {
            _unitOfWork = unitOfWork; // Conserva la unidad de trabajo para usar productos, categorias y guardado.
            _imagenStorage = imagenStorage; // Conserva el servicio que administra las fotos de los productos.
            _aiService = aiService; // Conserva el servicio de IA para enriquecer los productos.
            _logger = logger; // Conserva el registrador para documentar errores sin exponerlos al navegador.
        }

        // GET /Productos: catalogo con filtros opcionales por categoria y texto.
        public async Task<IActionResult> Index(int? categoriaId, string? buscar)
        {
            return View(await CrearModeloIndiceAsync(categoriaId, buscar)); // Prepara productos y filtros, y los entrega a la vista Index.
        }

        // GET /Productos/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var producto = await _unitOfWork.Productos.GetByIdAsync(id); // Busca el producto indicado por el identificador de la ruta.
            return producto is null ? NotFound() : View(producto.ToDetailsViewModel()); // Devuelve 404 si no existe; si existe, adapta sus datos y muestra el detalle.
        }

        // GET /Productos/Create
        public async Task<IActionResult> Create()
        {
            var model = new ProductoFormViewModel(); // Crea el modelo vacio que recibira los datos del formulario.
            await CargarCategoriasAsync(model); // Llena las opciones de categoria que necesita la lista desplegable.
            return View(model); // Renderiza el formulario de creacion con sus categorias disponibles.
        }

        // POST /Productos/Create
        [HttpPost] // Acepta el envio del formulario mediante el metodo HTTP POST.
        [ValidateAntiForgeryToken] // Comprueba que el formulario incluya el token de seguridad esperado.
        public async Task<IActionResult> Create(ProductoFormViewModel model)
        {
            await ValidarSkuAsync(model, null); // Rechaza un SKU que ya pertenezca a otro producto.
            await ValidarCategoriaAsync(model); // Comprueba que la categoria seleccionada exista realmente.

            // Detiene el alta si las validaciones del formulario o las comprobaciones adicionales fallan.
            if (!ModelState.IsValid)
            {
                await CargarCategoriasAsync(model); // Vuelve a cargar las opciones para que el formulario pueda mostrarse de nuevo.
                return View(model); // Devuelve los datos escritos y los errores para corregirlos.
            }

            var producto = model.ToEntity(); // Convierte los campos permitidos del formulario en una entidad de dominio.
            producto.FechaCreacion = DateTime.UtcNow; // Registra la fecha de creacion usando una hora independiente de la zona local.

            // Guarda primero la imagen y conserva su ruta para asociarla luego al producto.
            try
            {
                // Una imagen es opcional, por eso solo se almacena cuando el usuario adjunto un archivo.
                if (model.Imagen is not null)
                {
                    producto.ImagenUrl = await _imagenStorage.SaveAsync(model.Imagen); // Persiste el archivo y asigna su ruta publica al producto.
                }
            }
            catch (InvalidOperationException ex) // Captura errores esperados de validacion o almacenamiento de la imagen.
            {
                ModelState.AddModelError(nameof(model.Imagen), ex.Message); // Muestra el motivo del fallo junto al campo de imagen.
                await CargarCategoriasAsync(model); // Recarga las categorias que requiere la vista del formulario.
                return View(model); // Conserva los valores para que el usuario pueda corregir el archivo.
            }

            // Registra el producto y confirma la transaccion compartida de la unidad de trabajo.
            try
            {
                await _unitOfWork.Productos.AddAsync(producto); // Agrega la entidad al contexto mediante el repositorio de productos.
                await _unitOfWork.SaveAsync(); // Persiste juntos los cambios pendientes de todos los repositorios.
            }
            catch (DbUpdateException) // Captura un fallo de persistencia, por ejemplo una restriccion de unicidad.
            {
                // Si la base rechaza el alta, no dejamos la foto huerfana en disco.
                await _imagenStorage.DeleteAsync(producto.ImagenUrl); // Elimina el archivo que ya se habia guardado pero no quedo asociado.
                // Comprueba si el rechazo se debio a que el SKU ya existe.
                if (await _unitOfWork.Productos.SkuExistsAsync(model.Sku))
                {
                    ModelState.AddModelError(nameof(model.Sku), "Ya existe un producto con ese SKU."); // Comunica el duplicado junto al campo SKU.
                    await CargarCategoriasAsync(model); // Restaura la lista de categorias necesaria para reabrir el formulario.
                    return View(model); // Permite que el usuario corrija el dato sin perder el resto del formulario.
                }

                throw; // No oculta excepciones de base de datos que no correspondan al SKU duplicado.
            }

            TempData["Ok"] = "Producto creado correctamente."; // Prepara un mensaje temporal para la siguiente pagina.
            return RedirectToAction(nameof(Index)); // Regresa al catalogo con el patron POST-Redirect-GET.
        }

        // GET /Productos/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var producto = await _unitOfWork.Productos.GetByIdAsync(id); // Obtiene el producto que se va a editar.
            if (producto is null)
            {
                return NotFound(); // Devuelve HTTP 404 si el producto no existe.
            }

            var model = producto.ToFormViewModel(); // Copia los datos actuales al modelo de edicion.
            await CargarCategoriasAsync(model); // Carga las categorias disponibles para el selector.
            return View(model); // Presenta el formulario con los datos actuales del producto.
        }

        // POST /Productos/Edit/5
        [HttpPost] // Acepta el envio del formulario de edicion mediante POST.
        [ValidateAntiForgeryToken] // Comprueba el token antifalsificacion incluido en el formulario.
        public async Task<IActionResult> Edit(int id, ProductoFormViewModel model)
        {
            // Confirma que el identificador enviado en la ruta sea el mismo del modelo.
            if (id != model.Id)
            {
                return BadRequest(); // Devuelve HTTP 400 para evitar modificar un registro distinto al solicitado.
            }

            var producto = await _unitOfWork.Productos.GetByIdAsync(id); // Carga la entidad existente antes de aplicar cambios.
            if (producto is null)
            {
                return NotFound(); // Devuelve HTTP 404 si el producto ya no existe.
            }

            await ValidarSkuAsync(model, id); // Valida el SKU excluyendo al propio producto editado.
            await ValidarCategoriaAsync(model); // Comprueba que la categoria seleccionada siga existiendo.

            // Si el formulario no pasa las validaciones, no modifica ni guarda la entidad.
            if (!ModelState.IsValid)
            {
                await CargarCategoriasAsync(model); // Recupera las opciones de categoria para volver a dibujar el formulario.
                return View(model); // Conserva los valores y presenta los errores de validacion.
            }

            var imagenAnterior = producto.ImagenUrl; // Conserva la ruta actual para borrarla solo despues de guardar con exito.
            string? imagenNueva = null; // Inicializa la nueva ruta; permanece null cuando no se adjunta una imagen de reemplazo.

            // Intenta persistir la imagen nueva antes de actualizar la entidad.
            try
            {
                // Mantiene la imagen actual si el formulario no contiene un archivo nuevo.
                if (model.Imagen is not null)
                {
                    imagenNueva = await _imagenStorage.SaveAsync(model.Imagen); // Guarda el archivo nuevo y obtiene la ruta que se asociara al producto.
                }
            }
            catch (InvalidOperationException ex) // Captura errores previstos al validar o guardar el archivo.
            {
                ModelState.AddModelError(nameof(model.Imagen), ex.Message); // Vincula el mensaje del servicio al campo de imagen.
                await CargarCategoriasAsync(model); // Recarga las categorias necesarias para la vista de edicion.
                return View(model); // Devuelve el formulario sin aplicar cambios al producto.
            }

            model.ApplyTo(producto); // Copia a la entidad los datos editables del formulario.
            // Solo reemplaza la ruta cuando se guardo una imagen nueva.
            if (imagenNueva is not null)
            {
                producto.ImagenUrl = imagenNueva; // Actualiza la imagen del producto con la ruta del archivo recien guardado.
            }

            // Actualiza la entidad en el repositorio y confirma la modificacion en la base.
            try
            {
                await _unitOfWork.Productos.UpdateAsync(producto); // Marca los datos del producto como modificados.
                await _unitOfWork.SaveAsync(); // Guarda la actualizacion con la unidad de trabajo.
            }
            catch (DbUpdateException) // Maneja fallos al persistir, incluidos posibles conflictos de SKU.
            {
                await _imagenStorage.DeleteAsync(imagenNueva); // Elimina la nueva imagen si no pudo quedar asociada al producto.
                // Comprueba si el conflicto se produjo porque otro producto tiene el SKU enviado.
                if (await _unitOfWork.Productos.SkuExistsAsync(model.Sku, id))
                {
                    ModelState.AddModelError(nameof(model.Sku), "Ya existe un producto con ese SKU."); // Informa el conflicto junto al campo SKU.
                    await CargarCategoriasAsync(model); // Recarga las categorias para volver a presentar el formulario.
                    return View(model); // Permite corregir el SKU sin perder los demas valores.
                }

                throw; // Propaga errores de persistencia distintos al duplicado conocido.
            }

            // La foto anterior se borra solo despues de confirmar el cambio en la base.
            // Evita eliminar la imagen actual cuando el usuario no subio una nueva.
            if (imagenNueva is not null)
            {
                await _imagenStorage.DeleteAsync(imagenAnterior); // Borra el archivo anterior una vez que la ruta nueva ya esta guardada.
            }

            TempData["Ok"] = "Producto actualizado correctamente."; // Prepara el aviso de exito para la pagina siguiente.
            return RedirectToAction(nameof(Index)); // Regresa al catalogo evitando repetir el POST al actualizar.
        }

        // GET /Productos/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var producto = await _unitOfWork.Productos.GetByIdAsync(id); // Carga los datos necesarios para confirmar que se eligio el producto correcto.
            return producto is null ? NotFound() : View(producto.ToDetailsViewModel()); // Devuelve 404 si falta; si existe, muestra sus datos para confirmar la eliminacion.
        }

        // POST /Productos/Delete/5
        [HttpPost] // Acepta la eliminacion solo mediante POST.
        [ActionName("Delete")] // Expone la ruta con nombre Delete aunque el metodo se llame DeleteConfirmed.
        [ValidateAntiForgeryToken] // Protege la solicitud con el token generado por el formulario.
        public async Task<IActionResult> DeleteConfirmed(int id, int? categoriaId, string? buscar)
        {
            var producto = await _unitOfWork.Productos.DeleteAsync(id); // Solicita al repositorio marcar el producto para eliminar y obtener sus datos.
            // El repositorio devuelve null si el identificador no corresponde a ningun producto.
            if (producto is null)
            {
                return NotFound(); // Devuelve HTTP 404 cuando no hay un producto que borrar.
            }

            await _unitOfWork.SaveAsync(); // Confirma la eliminacion del producto en la base de datos.
            await _imagenStorage.DeleteAsync(producto.ImagenUrl); // Elimina la foto asociada despues de guardar la eliminacion.

            // Detecta solicitudes AJAX para actualizar solo el fragmento del catalogo.
            if (EsSolicitudAjax())
            {
                var modelo = await CrearModeloIndiceAsync(categoriaId, buscar); // Reconstruye la lista respetando los filtros actuales.
                return PartialView("_ProductoResults", modelo); // Devuelve el fragmento HTML que el navegador reemplazara sin recargar la pagina.
            }

            TempData["Ok"] = "Producto eliminado correctamente."; // Guarda la confirmacion que se mostrara tras la redireccion tradicional.
            return RedirectToAction(nameof(Index), new { categoriaId, buscar }); // Regresa al catalogo conservando los filtros seleccionados.
        }

        // POST /Productos/UpdateStock/5: actualiza el stock y devuelve su fragmento Razor.
        [HttpPost] // Acepta el cambio de stock mediante una solicitud POST.
        [ValidateAntiForgeryToken] // Valida el token antifalsificacion enviado junto al formulario.
        public async Task<IActionResult> UpdateStock(int id, ProductoStockViewModel model)
        {
            var producto = await _unitOfWork.Productos.GetByIdAsync(id); // Busca el producto cuyo stock se quiere modificar.
            if (producto is null)
            {
                return NotFound(); // Devuelve HTTP 404 si no se encontro el producto.
            }

            // Si el valor recibido es invalido, prepara el fragmento con el error correspondiente.
            if (!ModelState.IsValid)
            {
                model.Id = id; // Repone el identificador para que el formulario parcial apunte al producto correcto.
                model.Nombre = producto.Nombre; // Agrega el nombre que la vista parcial muestra junto al campo.
                // Obtiene el error de validacion del stock o prepara un mensaje generico si no existe uno.
                model.Error = ModelState[nameof(model.Stock)]?.Errors
                    .FirstOrDefault()?.ErrorMessage ?? "Introduce un valor de stock válido.";

                // Para una solicitud normal se informa mediante TempData y se vuelve al listado.
                if (!EsSolicitudAjax())
                {
                    TempData["Error"] = model.Error; // Guarda el error para mostrarlo despues de la redireccion.
                    return RedirectToAction(nameof(Index)); // Regresa al catalogo usando el flujo tradicional.
                }

                Response.StatusCode = StatusCodes.Status400BadRequest; // Indica al cliente AJAX que los datos enviados no son validos.
                return PartialView("_ProductoStock", model); // Devuelve el campo de stock con el mensaje para reemplazar el fragmento actual.
            }

            producto.Stock = model.Stock; // Asigna a la entidad el nuevo valor validado.
            await _unitOfWork.Productos.UpdateAsync(producto); // Marca la entidad como actualizada en el repositorio.
            await _unitOfWork.SaveAsync(); // Guarda el stock en la base de datos.

            model.Id = id; // Mantiene el identificador en el modelo que usara la vista parcial.
            model.Nombre = producto.Nombre; // Mantiene el nombre del producto en el modelo de respuesta.
            model.Error = null; // Limpia cualquier error previo porque la actualizacion fue correcta.

            // Devuelve un fragmento HTML cuando la actualizacion vino de AJAX.
            if (EsSolicitudAjax())
            {
                return PartialView("_ProductoStock", model); // Renderiza el control actualizado sin recargar el catalogo.
            }

            TempData["Ok"] = "Stock actualizado correctamente."; // Prepara la confirmacion para la navegacion tradicional.
            return RedirectToAction(nameof(Index)); // Regresa al catalogo despues de guardar el cambio.
        }

        // Comprueba que el SKU tenga contenido y que no este asignado a otro producto.
        private async Task ValidarSkuAsync(ProductoFormViewModel model, int? excludeId)
        {
            if (!string.IsNullOrWhiteSpace(model.Sku) &&
                await _unitOfWork.Productos.SkuExistsAsync(model.Sku, excludeId))
            {
                ModelState.AddModelError(nameof(model.Sku), "Ya existe un producto con ese SKU."); // Agrega un error de validacion asociado al campo SKU.
            }
        }

        // Consulta las categorias y las transforma en opciones para el selector del formulario.
        private async Task CargarCategoriasAsync(ProductoFormViewModel model)
        {
            var categorias = await _unitOfWork.Categorias.GetAllAsync(); // Obtiene las categorias disponibles desde su repositorio.
            model.Categorias = categorias // Asigna al formulario las opciones que se presentaran en la lista desplegable.
                .Select(c => new SelectListItem(c.Nombre, c.Id.ToString())) // Convierte cada categoria en texto visible y valor de formulario.
                .ToList(); // Materializa las opciones en una lista que Razor puede recorrer.
        }

        // Construye todos los datos del listado y conserva los filtros recibidos por la solicitud.
        private async Task<ProductoIndexViewModel> CrearModeloIndiceAsync(int? categoriaId, string? buscar)
        {
            var productos = await _unitOfWork.Productos.GetAllAsync(categoriaId, buscar); // Consulta los productos aplicando filtros de categoria y texto.
            var categorias = await _unitOfWork.Categorias.GetAllAsync(); // Carga las categorias que la vista necesita para sus controles de filtro.

            // Devuelve el modelo completo que comparte la vista de catalogo y su fragmento AJAX.
            return new ProductoIndexViewModel
            {
                Productos = productos.Select(p => p.ToResumenViewModel()).ToList(), // Convierte las entidades en resumentes aptos para presentar en la lista.
                Categorias = categorias, // Entrega las categorias disponibles para filtrar el catalogo.
                CategoriaId = categoriaId is > 0 ? categoriaId : null, // Conserva solo un identificador de categoria positivo.
                Buscar = string.IsNullOrWhiteSpace(buscar) ? null : buscar.Trim() // Normaliza el texto de busqueda quitando espacios externos o lo deja vacio.
            };
        }

        // Reconoce la cabecera que el navegador usa para indicar una peticion AJAX.
        private bool EsSolicitudAjax()
        {
            return string.Equals(
                Request.Headers["X-Requested-With"], // Lee la cabecera que identifica el tipo de solicitud.
                "XMLHttpRequest", // Compara con el valor usado por las llamadas AJAX del cliente.
                StringComparison.OrdinalIgnoreCase); // Ignora diferencias entre mayusculas y minusculas.
        }

        // Verifica que la categoria recibida del formulario exista antes de guardar el producto.
        private async Task ValidarCategoriaAsync(ProductoFormViewModel model)
        {
            if (model.CategoriaId > 0 &&
                await _unitOfWork.Categorias.GetByIdAsync(model.CategoriaId) is null)
            {
                ModelState.AddModelError(nameof(model.CategoriaId), "La categoría seleccionada no existe."); // Informa que debe elegirse una categoria existente.
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutocompletarConIA([FromBody] ProductoDtoRequest? request)
        {
            if (request is null || !ModelState.IsValid || string.IsNullOrWhiteSpace(request.Nombre))
            {
                return BadRequest(new { error = "Escribe un nombre de producto válido." });
            }

            try
            {
                var (descripcion, precio) = await _aiService.EnriquecerProductoAsync(request.Nombre.Trim());
                return Json(new { descripcion, precio });
            }
            catch (ClientResultException ex)
            {
                _logger.LogError(ex, "El proveedor de IA respondió con el estado HTTP {ProviderStatusCode}.", ex.Status);

                var mensaje = ex.Status switch
                {
                    401 => "OpenAI rechazó la clave. Comprueba que sea válida y que pertenezca al proyecto correcto.",
                    403 => "La clave no tiene permisos para usar este modelo o servicio.",
                    404 => "No se encontró el modelo configurado. Comprueba AISettings:ModelName.",
                    429 => "OpenAI rechazó la solicitud por límite de uso o cuota. Revisa la cuota y facturación del proyecto.",
                    >= 500 => "OpenAI está teniendo un problema temporal. Inténtalo de nuevo más tarde.",
                    _ => "OpenAI rechazó la solicitud. Comprueba la configuración de la integración."
                };

                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    error = mensaje,
                    providerStatusCode = ex.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudieron generar sugerencias de IA para el producto.");
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    error = $"No se pudieron generar sugerencias ({ex.GetType().Name}). Revisa el registro del servidor para ver el detalle."
                });
            }
        }
    }
}
