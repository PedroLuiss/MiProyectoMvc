using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiProyectoMvc.Models;
using MiProyectoMvc.Repositories;
using System.Threading.Tasks;

namespace MiProyectoMvc.Controllers
{
    // [Authorize] -> Esto protege el controlador para que solo usuarios logueados puedan entrar
    [Authorize]
    public class ProductosController : Controller
    {
        private readonly IProductoRepository _productoRepository;

        // Inyectamos el Repositorio a través del Constructor (Inyección de Dependencias)
        public ProductosController(IProductoRepository productoRepository)
        {
            _productoRepository = productoRepository;
        }

        // GET: /Productos/
        public async Task<IActionResult> Index()
        {
            var productos = await _productoRepository.GetAllAsync();
            return View(productos);
        }

        // GET: /Productos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Productos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Producto producto)
        {
            if (ModelState.IsValid)
            {
                await _productoRepository.AddAsync(producto);
                await _productoRepository.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(producto);
        }
    }
}