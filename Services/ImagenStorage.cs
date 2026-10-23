namespace MiProyectoMvc.Services
{
    // Guarda las fotos en wwwroot/uploads/productos con un nombre generado.
    public class ImagenStorage : IImagenStorage
    {
        private const long MaxBytes = 2 * 1024 * 1024;
        private const string Carpeta = "uploads/productos";
        private const string PrefijoRuta = "/uploads/productos/";

        private static readonly HashSet<string> ExtensionesPermitidas =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

        private static readonly HashSet<string> TiposPermitidos =
            new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ImagenStorage> _logger;

        public ImagenStorage(IWebHostEnvironment environment, ILogger<ImagenStorage> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string> SaveAsync(IFormFile imagen, CancellationToken cancellationToken = default)
        {
            if (imagen.Length == 0)
            {
                throw new InvalidOperationException("La imagen está vacía.");
            }

            if (imagen.Length > MaxBytes)
            {
                throw new InvalidOperationException("La imagen no puede superar los 2 MB.");
            }

            var extension = Path.GetExtension(imagen.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !ExtensionesPermitidas.Contains(extension))
            {
                throw new InvalidOperationException("Formato no permitido. Usa JPG, PNG o WebP.");
            }

            if (!TiposPermitidos.Contains(imagen.ContentType))
            {
                throw new InvalidOperationException("El archivo no es una imagen válida.");
            }

            await using var contenido = imagen.OpenReadStream();
            var encabezado = new byte[12];
            var bytesLeidos = await contenido.ReadAsync(encabezado);
            if (!CoincideConFormato(extension, encabezado.AsSpan(0, bytesLeidos)))
            {
                throw new InvalidOperationException("El contenido del archivo no coincide con el formato de imagen indicado.");
            }

            var carpetaFisica = Path.Combine(_environment.WebRootPath, Carpeta);
            Directory.CreateDirectory(carpetaFisica);

            // Nunca se usa el nombre original: evita rutas maliciosas y colisiones.
            var nombreArchivo = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var rutaFisica = Path.Combine(carpetaFisica, nombreArchivo);

            await using var stream = new FileStream(rutaFisica, FileMode.CreateNew);
            contenido.Position = 0;
            await contenido.CopyToAsync(stream, cancellationToken);

            return $"{PrefijoRuta}{nombreArchivo}";
        }

        private static bool CoincideConFormato(string extension, ReadOnlySpan<byte> encabezado)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" =>
                    encabezado.Length >= 3 &&
                    encabezado[0] == 0xFF &&
                    encabezado[1] == 0xD8 &&
                    encabezado[2] == 0xFF,
                ".png" =>
                    encabezado.Length >= 8 &&
                    encabezado[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
                ".webp" =>
                    encabezado.Length >= 12 &&
                    encabezado[..4].SequenceEqual("RIFF"u8) &&
                    encabezado[8..12].SequenceEqual("WEBP"u8),
                _ => false
            };
        }

        public Task DeleteAsync(string? imagenUrl)
        {
            if (string.IsNullOrWhiteSpace(imagenUrl) ||
                !imagenUrl.StartsWith(PrefijoRuta, StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            // Path.GetFileName descarta cualquier intento de salir de la carpeta.
            var nombreArchivo = Path.GetFileName(imagenUrl);
            if (string.IsNullOrWhiteSpace(nombreArchivo))
            {
                return Task.CompletedTask;
            }

            var rutaFisica = Path.Combine(_environment.WebRootPath, Carpeta, nombreArchivo);

            try
            {
                if (File.Exists(rutaFisica))
                {
                    File.Delete(rutaFisica);
                }
            }
            catch (IOException ex)
            {
                // Un archivo bloqueado no debe impedir borrar el registro de la base.
                _logger.LogWarning(ex, "No se pudo eliminar la imagen {ImagenUrl}", imagenUrl);
            }

            return Task.CompletedTask;
        }
    }
}
