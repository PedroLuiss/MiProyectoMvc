namespace MiProyectoMvc.Models
{
    // Agrupa productos por tipo. La relacion es 1:N y se configura en ApplicationDbContext.
    public class Categoria
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}
