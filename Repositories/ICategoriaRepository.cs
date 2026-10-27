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

        // Prepara una categoria nueva; IUnitOfWork.SaveAsync confirma el cambio.
        Task AddAsync(Categoria categoria);

        // Prepara los cambios de una categoria existente.
        Task UpdateAsync(Categoria categoria);

        // Marca la categoria para eliminar; false si no existe.
        Task<bool> DeleteAsync(int id);

        Task<int> CountProductosAsync(int categoriaId);

        Task<bool> NombreExistsAsync(string nombre, int? excludeId = null);
    }
}
