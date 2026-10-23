using MiProyectoMvc.Models;

namespace MiProyectoMvc.ViewModels
{
    public class CategoriaIndexViewModel
    {
        public IReadOnlyList<CategoriaResumen> Categorias { get; init; } = [];
    }

    public class CategoriaDeleteViewModel
    {
        public int Id { get; init; }
        public string Nombre { get; init; } = string.Empty;
        public string? Descripcion { get; init; }
        public int ProductosCount { get; init; }
    }
}
