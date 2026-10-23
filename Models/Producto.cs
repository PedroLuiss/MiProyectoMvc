namespace MiProyectoMvc.Models
{
    // Entidad del dominio: representa un producto y EF Core la guarda en la tabla Productos.
    public class Producto
    {
        // EF Core reconoce Id por convencion como clave primaria autoincremental.
        public int Id { get; set; }

        // Nombre descriptivo del producto. El valor inicial evita null en objetos nuevos.
        public string Nombre { get; set; } = string.Empty;

        // Decimal es adecuado para importes porque evita errores de precision binaria.
        public decimal Precio { get; set; }
    }
}