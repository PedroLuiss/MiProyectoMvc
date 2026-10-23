using MiProyectoMvc.Models;

namespace MiProyectoMvc.Repositories
{
    // Operaciones de persistencia de categorias.
    public interface ICategoriaRepository
    {
        Task<IReadOnlyList<Categoria>> GetAllAsync();

        // Proyeccion con el numero de productos: evita cargar los productos completos.
        Task<IReadOnlyList<CategoriaResumen>> GetAllResumenAsync();

        Task<Categoria?> GetByIdAsync(int id);

        Task<int> CreateAsync(Categoria categoria);

        Task UpdateAsync(Categoria categoria);

        Task<bool> DeleteAsync(int id);

        Task<int> CountProductosAsync(int categoriaId);

        Task<bool> NombreExistsAsync(string nombre, int? excludeId = null);
    }
}
