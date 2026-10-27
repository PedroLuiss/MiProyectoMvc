namespace MiProyectoMvc.Repositories
{
    // Reúne repositorios que comparten el mismo contexto y una única confirmación.
    public interface IUnitOfWork
    {
        IProductoRepository Productos { get; }

        ICategoriaRepository Categorias { get; }

        // Persiste juntas todas las operaciones pendientes en el contexto compartido.
        Task<int> SaveAsync();
    }
}
