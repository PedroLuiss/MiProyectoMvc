namespace MiProyectoMvc.Models
{
    // Modelo de lectura para listados: evita cargar los productos solo para contarlos.
    public record CategoriaResumen(int Id, string Nombre, string? Descripcion, int ProductosCount);
}
