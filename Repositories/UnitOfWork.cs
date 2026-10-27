using MiProyectoMvc.Data;

namespace MiProyectoMvc.Repositories
{
    // Coordina repositorios asociados a la misma instancia de ApplicationDbContext.
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public UnitOfWork(
            ApplicationDbContext context,
            IProductoRepository productos,
            ICategoriaRepository categorias)
        {
            _context = context;
            Productos = productos;
            Categorias = categorias;
        }

        public IProductoRepository Productos { get; }

        public ICategoriaRepository Categorias { get; }

        // EF Core ejecuta los cambios rastreados en una transacción de SaveChanges.
        public Task<int> SaveAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
