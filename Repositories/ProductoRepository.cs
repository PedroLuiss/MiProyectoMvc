using Microsoft.EntityFrameworkCore;
using MiProyectoMvc.Data;
using MiProyectoMvc.Models;

namespace MiProyectoMvc.Repositories
{
    // Implementacion del contrato de productos usando Entity Framework Core.
    public class ProductoRepository : IProductoRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductoRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Producto>> GetAllAsync(int? categoriaId = null, string? buscar = null)
        {
            // AsNoTracking acelera lecturas: estas entidades no se van a modificar.
            var query = _context.Productos
                .Include(p => p.Categoria)
                .AsNoTracking()
                .AsQueryable();

            if (categoriaId is > 0)
            {
                query = query.Where(p => p.CategoriaId == categoriaId);
            }

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var termino = buscar.Trim();
                query = query.Where(p =>
                    p.Nombre.Contains(termino) ||
                    p.Sku.Contains(termino) ||
                    (p.Marca != null && p.Marca.Contains(termino)));
            }

            return await query.OrderBy(p => p.Nombre).ToListAsync();
        }

        public async Task<Producto?> GetByIdAsync(int id)
        {
            // Aqui si se necesita seguimiento porque Edit modifica la entidad.
            return await _context.Productos
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public Task AddAsync(Producto producto)
        {
            return _context.Productos.AddAsync(producto).AsTask();
        }

        public Task UpdateAsync(Producto producto)
        {
            // Edit trabaja con una entidad ya rastreada; solo se guarda si viene suelta.
            if (_context.Entry(producto).State == EntityState.Detached)
            {
                _context.Attach(producto);
                _context.Entry(producto).State = EntityState.Modified;
            }

            return Task.CompletedTask;
        }

        public async Task<Producto?> DeleteAsync(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto is null)
            {
                return null;
            }

            _context.Productos.Remove(producto);
            return producto;
        }

        public Task<bool> SkuExistsAsync(string sku, int? excludeId = null)
        {
            var normalizado = sku.Trim().ToUpperInvariant();
            return _context.Productos.AnyAsync(p =>
                p.Sku == normalizado && (excludeId == null || p.Id != excludeId));
        }
    }
}
