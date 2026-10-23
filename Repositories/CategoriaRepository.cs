using Microsoft.EntityFrameworkCore;
using MiProyectoMvc.Data;
using MiProyectoMvc.Models;

namespace MiProyectoMvc.Repositories
{
    public class CategoriaRepository : ICategoriaRepository
    {
        private readonly ApplicationDbContext _context;

        public CategoriaRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Categoria>> GetAllAsync()
        {
            return await _context.Categorias
                .AsNoTracking()
                .OrderBy(c => c.Nombre)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<CategoriaResumen>> GetAllResumenAsync()
        {
            // La proyeccion se traduce a un COUNT en SQL, no a una consulta por categoria.
            return await _context.Categorias
                .AsNoTracking()
                .OrderBy(c => c.Nombre)
                .Select(c => new CategoriaResumen(
                    c.Id,
                    c.Nombre,
                    c.Descripcion,
                    c.Productos.Count))
                .ToListAsync();
        }

        public async Task<Categoria?> GetByIdAsync(int id)
        {
            return await _context.Categorias.FindAsync(id);
        }

        public async Task<int> CreateAsync(Categoria categoria)
        {
            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();
            return categoria.Id;
        }

        public async Task UpdateAsync(Categoria categoria)
        {
            if (_context.Entry(categoria).State == EntityState.Detached)
            {
                _context.Attach(categoria);
                _context.Entry(categoria).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria is null)
            {
                return false;
            }

            _context.Categorias.Remove(categoria);

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                _context.Entry(categoria).State = EntityState.Detached;

                // La FK puede rechazar el borrado si un producto se asocio a la
                // categoria justo despues de la comprobacion del controlador.
                if (await _context.Productos.AnyAsync(p => p.CategoriaId == id))
                {
                    return false;
                }

                throw;
            }
        }

        public Task<int> CountProductosAsync(int categoriaId)
        {
            return _context.Productos.CountAsync(p => p.CategoriaId == categoriaId);
        }

        public Task<bool> NombreExistsAsync(string nombre, int? excludeId = null)
        {
            var normalizado = nombre.Trim();
            return _context.Categorias.AnyAsync(c =>
                c.Nombre == normalizado && (excludeId == null || c.Id != excludeId));
        }
    }
}
