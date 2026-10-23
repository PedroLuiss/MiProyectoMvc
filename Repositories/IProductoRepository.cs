using MiProyectoMvc.Models;

namespace MiProyectoMvc.Repositories
{
    // Contrato entre el controlador y la persistencia. El controlador conoce esta
    // interfaz, no los detalles de Entity Framework ni de SQL Server.
    public interface IProductoRepository
    {
        // Devuelve productos con su categoria, opcionalmente filtrados.
        Task<IReadOnlyList<Producto>> GetAllAsync(int? categoriaId = null, string? buscar = null);

        // Devuelve el producto con su categoria o null si no existe.
        Task<Producto?> GetByIdAsync(int id);

        // Agrega y persiste el producto; devuelve el Id generado por la base.
        Task<int> CreateAsync(Producto producto);

        // Persiste los cambios de un producto existente.
        Task UpdateAsync(Producto producto);

        // Elimina el producto y lo devuelve (util para borrar su imagen); null si no existe.
        Task<Producto?> DeleteAsync(int id);

        // Comprueba la unicidad del SKU, excluyendo el producto que se esta editando.
        Task<bool> SkuExistsAsync(string sku, int? excludeId = null);
    }
}
