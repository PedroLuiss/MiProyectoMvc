using System.ComponentModel.DataAnnotations;

namespace MiProyectoMvc.DTOs;

/// <summary>
/// Datos enviados en JSON para solicitar sugerencias de descripcion y precio de un producto.
/// </summary>
public class ProductoDtoRequest
{
    /// <summary>
    /// Nombre del producto que se usara para generar las sugerencias.
    /// </summary>
    [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
    [StringLength(120, ErrorMessage = "El nombre no puede superar los 120 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
}
