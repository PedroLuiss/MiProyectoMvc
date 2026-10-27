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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IImagenStorage _imagenStorage;

        public ProductosController(
            IUnitOfWork unitOfWork,
            IImagenStorage imagenStorage)
        {
            _unitOfWork = unitOfWork;
            _imagenStorage = imagenStorage;
        }

        // GET /Productos: catalogo con filtros opcionales por categoria y texto.
        public async Task<IActionResult> Index(int? categoriaId, string? buscar)
        {
            return View(await CrearModeloIndiceAsync(categoriaId, buscar));
        }

        // GET /Productos/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var producto = await _unitOfWork.Productos.GetByIdAsync(id);
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
                await _unitOfWork.Productos.AddAsync(producto);
                await _unitOfWork.SaveAsync();
            }
            catch (DbUpdateException)
            {
                // Si la base rechaza el alta, no dejamos la foto huerfana en disco.
                await _imagenStorage.DeleteAsync(producto.ImagenUrl);
                if (await _unitOfWork.Productos.SkuExistsAsync(model.Sku))
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
            var producto = await _unitOfWork.Productos.GetByIdAsync(id);
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

            var producto = await _unitOfWork.Productos.GetByIdAsync(id);
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
                await _unitOfWork.Productos.UpdateAsync(producto);
                await _unitOfWork.SaveAsync();
            }
            catch (DbUpdateException)
            {
                await _imagenStorage.DeleteAsync(imagenNueva);
                if (await _unitOfWork.Productos.SkuExistsAsync(model.Sku, id))
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
            var producto = await _unitOfWork.Productos.GetByIdAsync(id);
            return producto is null ? NotFound() : View(producto.ToDetailsViewModel());
        }

        // POST /Productos/Delete/5
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, int? categoriaId, string? buscar)
        {
            var producto = await _unitOfWork.Productos.DeleteAsync(id);
            if (producto is null)
            {
                return NotFound();
            }

            await _unitOfWork.SaveAsync();
            await _imagenStorage.DeleteAsync(producto.ImagenUrl);

            if (EsSolicitudAjax())
            {
                var modelo = await CrearModeloIndiceAsync(categoriaId, buscar);
                return PartialView("_ProductoResults", modelo);
            }

            TempData["Ok"] = "Producto eliminado correctamente.";
            return RedirectToAction(nameof(Index), new { categoriaId, buscar });
        }

        // POST /Productos/UpdateStock/5: actualiza el stock y devuelve su fragmento Razor.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(int id, ProductoStockViewModel model)
        {
            var producto = await _unitOfWork.Productos.GetByIdAsync(id);
            if (producto is null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                model.Id = id;
                model.Nombre = producto.Nombre;
                model.Error = ModelState[nameof(model.Stock)]?.Errors
                    .FirstOrDefault()?.ErrorMessage ?? "Introduce un valor de stock válido.";

                if (!EsSolicitudAjax())
                {
                    TempData["Error"] = model.Error;
                    return RedirectToAction(nameof(Index));
                }

                Response.StatusCode = StatusCodes.Status400BadRequest;
                return PartialView("_ProductoStock", model);
            }

            producto.Stock = model.Stock;
            await _unitOfWork.Productos.UpdateAsync(producto);
            await _unitOfWork.SaveAsync();

            model.Id = id;
            model.Nombre = producto.Nombre;
            model.Error = null;

            if (EsSolicitudAjax())
            {
                return PartialView("_ProductoStock", model);
            }

            TempData["Ok"] = "Stock actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task ValidarSkuAsync(ProductoFormViewModel model, int? excludeId)
        {
            if (!string.IsNullOrWhiteSpace(model.Sku) &&
                await _unitOfWork.Productos.SkuExistsAsync(model.Sku, excludeId))
            {
                ModelState.AddModelError(nameof(model.Sku), "Ya existe un producto con ese SKU.");
            }
        }

        private async Task CargarCategoriasAsync(ProductoFormViewModel model)
        {
            var categorias = await _unitOfWork.Categorias.GetAllAsync();
            model.Categorias = categorias
                .Select(c => new SelectListItem(c.Nombre, c.Id.ToString()))
                .ToList();
        }

        private async Task<ProductoIndexViewModel> CrearModeloIndiceAsync(int? categoriaId, string? buscar)
        {
            var productos = await _unitOfWork.Productos.GetAllAsync(categoriaId, buscar);
            var categorias = await _unitOfWork.Categorias.GetAllAsync();

            return new ProductoIndexViewModel
            {
                Productos = productos.Select(p => p.ToResumenViewModel()).ToList(),
                Categorias = categorias,
                CategoriaId = categoriaId is > 0 ? categoriaId : null,
                Buscar = string.IsNullOrWhiteSpace(buscar) ? null : buscar.Trim()
            };
        }

        private bool EsSolicitudAjax()
        {
            return string.Equals(
                Request.Headers["X-Requested-With"],
                "XMLHttpRequest",
                StringComparison.OrdinalIgnoreCase);
        }

        private async Task ValidarCategoriaAsync(ProductoFormViewModel model)
        {
            if (model.CategoriaId > 0 &&
                await _unitOfWork.Categorias.GetByIdAsync(model.CategoriaId) is null)
            {
                ModelState.AddModelError(nameof(model.CategoriaId), "La categoría seleccionada no existe.");
            }
        }
    }
}
