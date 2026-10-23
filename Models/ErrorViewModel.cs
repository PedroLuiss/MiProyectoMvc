namespace MiProyectoMvc.Models;

// Datos que la vista de error necesita mostrar sin exponer detalles internos de la excepcion.
public class ErrorViewModel
{
    // Identificador de la solicitud, util para buscar el error en los registros del servidor.
    public string? RequestId { get; set; }

    // Propiedad calculada: indica a la vista si hay un identificador que mostrar.
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
