using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiProyectoMvc.Models;
using MiProyectoMvc.Repositories;
using System.Threading.Tasks;

namespace MiProyectoMvc.Controllers
{
    // Protege todas las acciones del controlador: solo usuarios autenticados pueden acceder.
    [Authorize]
    public class ProductosController : Controller
    {
        // El controlador coordina la solicitud, pero delega el acceso a datos al repositorio.
        private readonly IProductoRepository _productoRepository;

        // Inyeccion por constructor: ASP.NET entrega la implementacion registrada en Program.cs.
        public ProductosController(IProductoRepository productoRepository)
        {
            _productoRepository = productoRepository;
        }

        // GET /Productos o /Productos/Index: consulta productos y los entrega a la vista.
        public async Task<IActionResult> Index()
        {
            var productos = await _productoRepository.GetAllAsync();
            return View(productos);
        }

        // GET /Productos/Create: muestra el formulario vacio para crear un producto.
        public IActionResult Create()
        {
            return View();
        }

        // POST /Productos/Create: recibe los datos enviados desde el formulario.
        [HttpPost]
        // Comprueba el token antifalsificacion incluido por los formularios Razor.
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Producto producto)
        {
            // ModelState contiene los resultados de validacion del modelo recibido.
            if (ModelState.IsValid)
            {
                // Primero se agrega la entidad al contexto y luego se persiste en SQL.
                await _productoRepository.AddAsync(producto);
                await _productoRepository.SaveChangesAsync();

                // Redirigir evita reenviar el formulario al actualizar la pagina.
                return RedirectToAction(nameof(Index));
            }

            // Si hay errores, se vuelve a mostrar el formulario con los datos recibidos.
            return View(producto);
        }
    }
}