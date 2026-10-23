using MiProyectoMvc.Models;
using MiProyectoMvc.ViewModels;

namespace MiProyectoMvc.Mappings
{
    public static class CategoriaMappings
    {
        public static Categoria ToEntity(this CategoriaFormViewModel model) => new()
        {
            Id = model.Id,
            Nombre = model.Nombre.Trim(),
            Descripcion = Limpiar(model.Descripcion)
        };

        public static void ApplyTo(this CategoriaFormViewModel model, Categoria categoria)
        {
            categoria.Nombre = model.Nombre.Trim();
            categoria.Descripcion = Limpiar(model.Descripcion);
        }

        public static CategoriaFormViewModel ToFormViewModel(this Categoria categoria) => new()
        {
            Id = categoria.Id,
            Nombre = categoria.Nombre,
            Descripcion = categoria.Descripcion
        };

        private static string? Limpiar(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }
}
