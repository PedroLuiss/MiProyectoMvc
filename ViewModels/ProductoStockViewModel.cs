using System.ComponentModel.DataAnnotations;

namespace MiProyectoMvc.ViewModels
{
    public class ProductoStockViewModel
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        [Range(0, 100000, ErrorMessage = "El stock debe estar entre 0 y 100000.")]
        public int Stock { get; set; }

        public string? Error { get; set; }
    }
}
