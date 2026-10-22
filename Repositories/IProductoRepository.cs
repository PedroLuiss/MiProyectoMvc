using MiProyectoMvc.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MiProyectoMvc.Repositories
{
    public interface IProductoRepository
    {
        Task<IEnumerable<Producto>> GetAllAsync();
        Task<Producto?> GetByIdAsync(int id);
        Task AddAsync(Producto producto);
        Task SaveChangesAsync();
    }
}