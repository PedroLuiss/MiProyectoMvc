using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MiProyectoMvc.Mappings;
using MiProyectoMvc.Repositories;
using MiProyectoMvc.Services;
using MiProyectoMvc.ViewModels;

namespace MiProyectoMvc.Controllers
{
    // Protege todas las acciones del controlador: solo usuarios autenticados pueden acceder.
    [Authorize]
    public class ProductosController : Controller
    {
        private readonly IProductoRepository _productoRepository;
        private readonly ICategoriaRepository _categoriaRepository;
        private readonly IImagenStorage _imagenStorage;

        public ProductosController(
            IProductoRepository productoRepository,
            ICategoriaRepository categoriaRepository,
            IImagenStorage imagenStorage)
        {
            _productoRepository = productoRepository;
            _categoriaRepository = categoriaRepository;
            _imagenStorage = imagenStorage;
        }

        // GET /Productos: catalogo con filtros opcionales por categoria y texto.
        public async Task<IActionResult> Index(int? categoriaId, string? buscar)
        {
            var productos = await _productoRepository.GetAllAsync(categoriaId, buscar);
            var categorias = await _categoriaRepository.GetAllAsync();

            var model = new ProductoIndexViewModel
            {
                Productos = productos.Select(p => p.ToResumenViewModel()).ToList(),
                Categorias = categorias,
                CategoriaId = categoriaId is > 0 ? categoriaId : null,
                Buscar = string.IsNullOrWhiteSpace(buscar) ? null : buscar.Trim()
            };

            return View(model);
        }

        // GET /Productos/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var producto = await _productoRepository.GetByIdAsync(id);
            return producto is null ? NotFound() : View(producto.ToDetailsViewModel());
        }

        // GET /Productos/Create
        public async Task<IActionResult> Create()
        {
            var model = new ProductoFormViewModel();
            await CargarCategoriasAsync(model);
            return View(model);
        }

        // POST /Productos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductoFormViewModel model)
        {
            await ValidarSkuAsync(model, null);
            await ValidarCategoriaAsync(model);

            if (!ModelState.IsValid)
            {
                await CargarCategoriasAsync(model);
                return View(model);
            }

            var producto = model.ToEntity();
            producto.FechaCreacion = DateTime.UtcNow;

            try
            {
                if (model.Imagen is not null)
                {
                    producto.ImagenUrl = await _imagenStorage.SaveAsync(model.Imagen);
                }
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(model.Imagen), ex.Message);
                await CargarCategoriasAsync(model);
                return View(model);
            }

            try
            {
                await _productoRepository.CreateAsync(producto);
            }
            catch (DbUpdateException)
            {
                // Si la base rechaza el alta, no dejamos la foto huerfana en disco.
                await _imagenStorage.DeleteAsync(producto.ImagenUrl);
                if (await _productoRepository.SkuExistsAsync(model.Sku))
                {
                    ModelState.AddModelError(nameof(model.Sku), "Ya existe un producto con ese SKU.");
                    await CargarCategoriasAsync(model);
                    return View(model);
                }

                throw;
            }

            TempData["Ok"] = "Producto creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Productos/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var producto = await _productoRepository.GetByIdAsync(id);
            if (producto is null)
            {
                return NotFound();
            }

            var model = producto.ToFormViewModel();
            await CargarCategoriasAsync(model);
            return View(model);
        }

        // POST /Productos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductoFormViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var producto = await _productoRepository.GetByIdAsync(id);
            if (producto is null)
            {
                return NotFound();
            }

            await ValidarSkuAsync(model, id);
            await ValidarCategoriaAsync(model);

            if (!ModelState.IsValid)
            {
                await CargarCategoriasAsync(model);
                return View(model);
            }

            var imagenAnterior = producto.ImagenUrl;
            string? imagenNueva = null;

            try
            {
                if (model.Imagen is not null)
                {
                    imagenNueva = await _imagenStorage.SaveAsync(model.Imagen);
                }
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(model.Imagen), ex.Message);
                await CargarCategoriasAsync(model);
                return View(model);
            }

            model.ApplyTo(producto);
            if (imagenNueva is not null)
            {
                producto.ImagenUrl = imagenNueva;
            }

            try
            {
                await _productoRepository.UpdateAsync(producto);
            }
            catch (DbUpdateException)
            {
                await _imagenStorage.DeleteAsync(imagenNueva);
                if (await _productoRepository.SkuExistsAsync(model.Sku, id))
                {
                    ModelState.AddModelError(nameof(model.Sku), "Ya existe un producto con ese SKU.");
                    await CargarCategoriasAsync(model);
                    return View(model);
                }

                throw;
            }

            // La foto anterior se borra solo despues de confirmar el cambio en la base.
            if (imagenNueva is not null)
            {
                await _imagenStorage.DeleteAsync(imagenAnterior);
            }

            TempData["Ok"] = "Producto actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Productos/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var producto = await _productoRepository.GetByIdAsync(id);
            return producto is null ? NotFound() : View(producto.ToDetailsViewModel());
        }

        // POST /Productos/Delete/5
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producto = await _productoRepository.DeleteAsync(id);
            if (producto is null)
            {
                return NotFound();
            }

            await _imagenStorage.DeleteAsync(producto.ImagenUrl);

            TempData["Ok"] = "Producto eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task ValidarSkuAsync(ProductoFormViewModel model, int? excludeId)
        {
            if (!string.IsNullOrWhiteSpace(model.Sku) &&
                await _productoRepository.SkuExistsAsync(model.Sku, excludeId))
            {
                ModelState.AddModelError(nameof(model.Sku), "Ya existe un producto con ese SKU.");
            }
        }

        private async Task CargarCategoriasAsync(ProductoFormViewModel model)
        {
            var categorias = await _categoriaRepository.GetAllAsync();
            model.Categorias = categorias
                .Select(c => new SelectListItem(c.Nombre, c.Id.ToString()))
                .ToList();
        }

        private async Task ValidarCategoriaAsync(ProductoFormViewModel model)
        {
            if (model.CategoriaId > 0 &&
                await _categoriaRepository.GetByIdAsync(model.CategoriaId) is null)
            {
                ModelState.AddModelError(nameof(model.CategoriaId), "La categoría seleccionada no existe.");
            }
        }
    }
}
