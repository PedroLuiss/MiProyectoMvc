using MiProyectoMvc.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MiProyectoMvc.Repositories
{
    // Contrato entre el controlador y la persistencia. El controlador conoce esta
    // interfaz, no los detalles de Entity Framework ni de SQL Server.
    public interface IProductoRepository
    {
        // Devuelve todos los productos de forma asincrona para no bloquear el servidor.
        Task<IEnumerable<Producto>> GetAllAsync();

        // Devuelve el producto solicitado o null si no existe.
        Task<Producto?> GetByIdAsync(int id);

        // Prepara un producto nuevo para insertarlo; el guardado ocurre por separado.
        Task AddAsync(Producto producto);

        // Confirma en la base de datos los cambios pendientes del contexto.
        Task SaveChangesAsync();
    }
}