using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiProyectoMvc.Mappings;
using MiProyectoMvc.Repositories;
using MiProyectoMvc.ViewModels;

namespace MiProyectoMvc.Controllers
{
    // Gestion de categorias. La categoria General (Id 1) no se puede eliminar
    // porque es la asignacion por defecto de los productos existentes.
    [Authorize]
    public class CategoriasController : Controller
    {
        private const int CategoriaGeneralId = 1;

        private readonly IUnitOfWork _unitOfWork;

        public CategoriasController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            var categorias = await _unitOfWork.Categorias.GetAllResumenAsync();
            return View(new CategoriaIndexViewModel { Categorias = categorias });
        }

        public IActionResult Create()
        {
            return View(new CategoriaFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoriaFormViewModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.Nombre) &&
                await _unitOfWork.Categorias.NombreExistsAsync(model.Nombre))
            {
                ModelState.AddModelError(nameof(model.Nombre), "Ya existe una categoría con ese nombre.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _unitOfWork.Categorias.AddAsync(model.ToEntity());
                await _unitOfWork.SaveAsync();
            }
            catch (DbUpdateException)
            {
                if (await _unitOfWork.Categorias.NombreExistsAsync(model.Nombre))
                {
                    ModelState.AddModelError(nameof(model.Nombre), "Ya existe una categoría con ese nombre.");
                    return View(model);
                }

                throw;
            }

            TempData["Ok"] = "Categoría creada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var categoria = await _unitOfWork.Categorias.GetByIdAsync(id);
            return categoria is null ? NotFound() : View(categoria.ToFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoriaFormViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var categoria = await _unitOfWork.Categorias.GetByIdAsync(id);
            if (categoria is null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(model.Nombre) &&
                await _unitOfWork.Categorias.NombreExistsAsync(model.Nombre, id))
            {
                ModelState.AddModelError(nameof(model.Nombre), "Ya existe una categoría con ese nombre.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.ApplyTo(categoria);

            try
            {
                await _unitOfWork.Categorias.UpdateAsync(categoria);
                await _unitOfWork.SaveAsync();
            }
            catch (DbUpdateException)
            {
                if (await _unitOfWork.Categorias.NombreExistsAsync(model.Nombre, id))
                {
                    ModelState.AddModelError(nameof(model.Nombre), "Ya existe una categoría con ese nombre.");
                    return View(model);
                }

                throw;
            }

            TempData["Ok"] = "Categoría actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var categoria = await _unitOfWork.Categorias.GetByIdAsync(id);
            if (categoria is null)
            {
                return NotFound();
            }

            var count = await _unitOfWork.Categorias.CountProductosAsync(id);

            return View(new CategoriaDeleteViewModel
            {
                Id = categoria.Id,
                Nombre = categoria.Nombre,
                Descripcion = categoria.Descripcion,
                ProductosCount = count
            });
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (id == CategoriaGeneralId)
            {
                TempData["Error"] = "La categoría General no se puede eliminar.";
                return RedirectToAction(nameof(Index));
            }

            var count = await _unitOfWork.Categorias.CountProductosAsync(id);
            if (count > 0)
            {
                TempData["Error"] = $"No se puede eliminar la categoría: tiene {count} producto(s) asociado(s).";
                return RedirectToAction(nameof(Index));
            }

            var marcadaParaEliminar = await _unitOfWork.Categorias.DeleteAsync(id);
            if (!marcadaParaEliminar)
            {
                TempData["Error"] = "No se pudo encontrar la categoría.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _unitOfWork.SaveAsync();
                TempData["Ok"] = "Categoría eliminada correctamente.";
            }
            catch (DbUpdateException)
            {
                if (await _unitOfWork.Categorias.CountProductosAsync(id) > 0)
                {
                    TempData["Error"] = "No se puede eliminar la categoría porque tiene productos asociados.";
                }
                else
                {
                    throw;
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
