using MiProyectoMvc.Models;
using MiProyectoMvc.ViewModels;

namespace MiProyectoMvc.Mappings
{
    // Mapeo manual entre ViewModels y entidades: explicito, sin dependencias extra.
    public static class ProductoMappings
    {
        public static Producto ToEntity(this ProductoFormViewModel model) => new()
        {
            Id = model.Id,
            Nombre = model.Nombre.Trim(),
            Descripcion = Limpiar(model.Descripcion),
            Sku = model.Sku.Trim().ToUpperInvariant(),
            Marca = Limpiar(model.Marca),
            Precio = model.Precio,
            Stock = model.Stock,
            Activo = model.Activo,
            CategoriaId = model.CategoriaId
        };

        public static void ApplyTo(this ProductoFormViewModel model, Producto producto)
        {
            producto.Nombre = model.Nombre.Trim();
            producto.Descripcion = Limpiar(model.Descripcion);
            producto.Sku = model.Sku.Trim().ToUpperInvariant();
            producto.Marca = Limpiar(model.Marca);
            producto.Precio = model.Precio;
            producto.Stock = model.Stock;
            producto.Activo = model.Activo;
            producto.CategoriaId = model.CategoriaId;
        }

        public static ProductoFormViewModel ToFormViewModel(this Producto producto) => new()
        {
            Id = producto.Id,
            Nombre = producto.Nombre,
            Descripcion = producto.Descripcion,
            Sku = producto.Sku,
            Marca = producto.Marca,
            Precio = producto.Precio,
            Stock = producto.Stock,
            Activo = producto.Activo,
            CategoriaId = producto.CategoriaId,
            ImagenUrl = producto.ImagenUrl
        };

        public static ProductoResumenViewModel ToResumenViewModel(this Producto producto) => new()
        {
            Id = producto.Id,
            Nombre = producto.Nombre,
            Descripcion = producto.Descripcion,
            Sku = producto.Sku,
            Marca = producto.Marca,
            Precio = producto.Precio,
            Stock = producto.Stock,
            Activo = producto.Activo,
            ImagenUrl = producto.ImagenUrl,
            CategoriaNombre = producto.Categoria?.Nombre ?? "Sin categoría"
        };

        public static ProductoDetailsViewModel ToDetailsViewModel(this Producto producto) => new()
        {
            Id = producto.Id,
            Nombre = producto.Nombre,
            Descripcion = producto.Descripcion,
            Sku = producto.Sku,
            Marca = producto.Marca,
            Precio = producto.Precio,
            Stock = producto.Stock,
            Activo = producto.Activo,
            ImagenUrl = producto.ImagenUrl,
            FechaCreacion = producto.FechaCreacion,
            CategoriaId = producto.CategoriaId,
            CategoriaNombre = producto.Categoria?.Nombre ?? "Sin categoría"
        };

        private static string? Limpiar(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }
}
