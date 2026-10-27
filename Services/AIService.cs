using System.Globalization;
using Microsoft.Extensions.AI;
using OpenAI;

namespace MiProyectoMvc.Services
{
    public class AIService
    {
        private readonly IConfiguration _configuration;

        public AIService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<(string Descripcion, decimal PrecioSugerido)> EnriquecerProductoAsync(string nombreProducto)
        {
            var apiKey = _configuration["AISettings:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "No está configurada la clave del proveedor de IA. Configura AISettings:ApiKey mediante User Secrets o una variable de entorno.");
            }

            var modelName = _configuration["AISettings:ModelName"] ?? "gpt-4o-mini";
            var chatClient = new OpenAIClient(apiKey)
                .GetChatClient(modelName)
                .AsIChatClient();

            var prompt = $"""
                Actúa como experto en productos de comercio e inventario.
                Sugiere una descripción comercial breve y un precio razonable para este producto: {nombreProducto}
                Responde exactamente con estas dos líneas, sin Markdown ni texto adicional:
                DESCRIPCION: texto de la descripción
                PRECIO: 45.99
                El precio debe ser un número mayor que cero, usando punto decimal y sin símbolo de moneda.
                """;

            var response = await chatClient.GetResponseAsync(prompt);
            var text = response.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("El proveedor de IA devolvió una respuesta vacía.");
            }

            string? descripcion = null;
            decimal? precio = null;

            foreach (var line in text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.StartsWith("DESCRIPCION:", StringComparison.OrdinalIgnoreCase))
                {
                    descripcion = line["DESCRIPCION:".Length..].Trim();
                }
                else if (line.StartsWith("PRECIO:", StringComparison.OrdinalIgnoreCase))
                {
                    var precioTexto = line["PRECIO:".Length..].Trim();
                    if (decimal.TryParse(precioTexto, NumberStyles.Number, CultureInfo.InvariantCulture, out var precioParseado))
                    {
                        precio = precioParseado;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(descripcion) || precio is null or <= 0)
            {
                throw new InvalidOperationException(
                    "El proveedor de IA devolvió una respuesta con un formato inesperado. Inténtalo de nuevo.");
            }

            return (descripcion, precio.Value);
        }
    }
}
