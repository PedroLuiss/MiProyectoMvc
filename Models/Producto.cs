namespace MiProyectoMvc.Models
{
    // Entidad del dominio: representa un producto y EF Core la guarda en la tabla Productos.
    public class Producto
    {
        // EF Core reconoce Id por convencion como clave primaria autoincremental.
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        // Codigo unico del producto; se normaliza a mayusculas antes de guardar.
        public string Sku { get; set; } = string.Empty;

        public string? Marca { get; set; }

        // Decimal es adecuado para importes porque evita errores de precision binaria.
        public decimal Precio { get; set; }

        public int Stock { get; set; }

        public bool Activo { get; set; } = true;

        // Ruta relativa de la foto, por ejemplo /uploads/productos/archivo.jpg.
        public string? ImagenUrl { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Clave foranea obligatoria: todo producto pertenece a una categoria.
        public int CategoriaId { get; set; }

        public Categoria? Categoria { get; set; }
    }
}
