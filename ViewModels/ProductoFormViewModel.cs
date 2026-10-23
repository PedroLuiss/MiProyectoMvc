using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MiProyectoMvc.ViewModels
{
    // Datos del formulario de alta y edicion. Separa la entrada del modelo de EF Core
    // y evita que el cliente pueda modificar campos como Id o FechaCreacion.
    public class ProductoFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(120, ErrorMessage = "El nombre no puede superar los 120 caracteres.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "El SKU es obligatorio.")]
        [StringLength(40, MinimumLength = 3, ErrorMessage = "El SKU debe tener entre 3 y 40 caracteres.")]
        [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9-]*$", ErrorMessage = "El SKU solo admite letras, números y guiones.")]
        [Display(Name = "SKU")]
        public string Sku { get; set; } = string.Empty;

        [StringLength(80, ErrorMessage = "La marca no puede superar los 80 caracteres.")]
        [Display(Name = "Marca")]
        public string? Marca { get; set; }

        [Range(typeof(decimal), "0.01", "999999999.99",
            ParseLimitsInInvariantCulture = true,
            ConvertValueInInvariantCulture = true,
            ErrorMessage = "El precio debe ser mayor que cero.")]
        [Display(Name = "Precio")]
        public decimal Precio { get; set; }

        [Range(0, 100000, ErrorMessage = "El stock debe estar entre 0 y 100000.")]
        [Display(Name = "Stock")]
        public int Stock { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Selecciona una categoría.")]
        [Display(Name = "Categoría")]
        public int CategoriaId { get; set; }

        [Display(Name = "Producto activo")]
        public bool Activo { get; set; } = true;

        // Ruta de la imagen actual; solo se usa para la vista previa.
        public string? ImagenUrl { get; set; }

        [Display(Name = "Foto del producto")]
        public IFormFile? Imagen { get; set; }

        public IReadOnlyList<SelectListItem> Categorias { get; set; } = [];
    }
}
