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

        private readonly ICategoriaRepository _categoriaRepository;

        public CategoriasController(ICategoriaRepository categoriaRepository)
        {
            _categoriaRepository = categoriaRepository;
        }

        public async Task<IActionResult> Index()
        {
            var categorias = await _categoriaRepository.GetAllResumenAsync();
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
                await _categoriaRepository.NombreExistsAsync(model.Nombre))
            {
                ModelState.AddModelError(nameof(model.Nombre), "Ya existe una categoría con ese nombre.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _categoriaRepository.CreateAsync(model.ToEntity());
            }
            catch (DbUpdateException)
            {
                if (await _categoriaRepository.NombreExistsAsync(model.Nombre))
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
            var categoria = await _categoriaRepository.GetByIdAsync(id);
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

            var categoria = await _categoriaRepository.GetByIdAsync(id);
            if (categoria is null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(model.Nombre) &&
                await _categoriaRepository.NombreExistsAsync(model.Nombre, id))
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
                await _categoriaRepository.UpdateAsync(categoria);
            }
            catch (DbUpdateException)
            {
                if (await _categoriaRepository.NombreExistsAsync(model.Nombre, id))
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
            var categoria = await _categoriaRepository.GetByIdAsync(id);
            if (categoria is null)
            {
                return NotFound();
            }

            var count = await _categoriaRepository.CountProductosAsync(id);

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

            var count = await _categoriaRepository.CountProductosAsync(id);
            if (count > 0)
            {
                TempData["Error"] = $"No se puede eliminar la categoría: tiene {count} producto(s) asociado(s).";
                return RedirectToAction(nameof(Index));
            }

            var eliminada = await _categoriaRepository.DeleteAsync(id);
            if (eliminada)
            {
                TempData["Ok"] = "Categoría eliminada correctamente.";
            }
            else
            {
                TempData["Error"] = "No se pudo eliminar la categoría.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
