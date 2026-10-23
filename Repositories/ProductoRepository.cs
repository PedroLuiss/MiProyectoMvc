using MiProyectoMvc.Data;
using MiProyectoMvc.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MiProyectoMvc.Repositories
{
    // Implementacion del contrato de productos usando Entity Framework Core.
    public class ProductoRepository : IProductoRepository
    {
        // El contexto mantiene la conexion y el seguimiento de entidades de EF Core.
        private readonly ApplicationDbContext _context;

        // El contenedor de dependencias proporciona el contexto configurado en Program.cs.
        public ProductoRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // ToListAsync ejecuta la consulta y materializa los resultados en una lista.
        public async Task<IEnumerable<Producto>> GetAllAsync()
        {
            return await _context.Productos.ToListAsync();
        }

        // FindAsync busca por la clave primaria (Id) y devuelve null si no hay coincidencia.
        public async Task<Producto?> GetByIdAsync(int id)
        {
            return await _context.Productos.FindAsync(id);
        }

        // AddAsync marca la entidad como nueva en el contexto; todavia no la envia a SQL.
        public async Task AddAsync(Producto producto)
        {
            await _context.Productos.AddAsync(producto);
        }

        // SaveChangesAsync envia a la base de datos todas las operaciones pendientes.
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}