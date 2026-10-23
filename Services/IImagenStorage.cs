namespace MiProyectoMvc.Services
{
    // Abstraccion del almacenamiento de imagenes. Permite cambiar el destino
    // (disco local, Azure Blob, S3) sin tocar los controladores.
    public interface IImagenStorage
    {
        // Guarda la imagen y devuelve su ruta publica relativa.
        Task<string> SaveAsync(IFormFile imagen, CancellationToken cancellationToken = default);

        // Elimina una imagen previamente guardada. Ignora rutas externas.
        Task DeleteAsync(string? imagenUrl);
    }
}
