using MiProyectoMvc.Models;

namespace MiProyectoMvc.ViewModels
{
    public class ProductoIndexViewModel
    {
        public IReadOnlyList<ProductoResumenViewModel> Productos { get; init; } = [];
        public IReadOnlyList<Categoria> Categorias { get; init; } = [];
        public int? CategoriaId { get; init; }
        public string? Buscar { get; init; }

        public bool TieneFiltros => CategoriaId.HasValue || !string.IsNullOrWhiteSpace(Buscar);
    }

    public class ProductoResumenViewModel
    {
        public int Id { get; init; }
        public string Nombre { get; init; } = string.Empty;
        public string? Descripcion { get; init; }
        public string Sku { get; init; } = string.Empty;
        public string? Marca { get; init; }
        public decimal Precio { get; init; }
        public int Stock { get; init; }
        public bool Activo { get; init; }
        public string? ImagenUrl { get; init; }
        public string CategoriaNombre { get; init; } = string.Empty;
    }
}
