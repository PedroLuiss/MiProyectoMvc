using System.Diagnostics; // Permite consultar la actividad diagnostica y su identificador de seguimiento.
using Microsoft.AspNetCore.Mvc; // Proporciona las clases y resultados usados por las acciones MVC.
using MiProyectoMvc.Models; // Incluye el modelo que transporta los datos de la pagina de error.

namespace MiProyectoMvc.Controllers; // Agrupa los controladores web de la aplicacion.

// Atiende paginas generales que no pertenecen al catalogo de productos.
public class HomeController : Controller // Hereda las utilidades para devolver vistas y respuestas HTTP.
{
    // GET /: la convencion de MVC busca Views/Home/Index.cshtml para esta accion.
    public IActionResult Index()
    {
        return View(); // Renderiza la pagina principal sin un modelo adicional.
    }

    // GET /Home/Privacy: muestra la pagina informativa de privacidad.
    public IActionResult Privacy()
    {
        return View(); // Renderiza la vista Privacy correspondiente al controlador.
    }

    // Desactiva la cache para que el identificador de seguimiento corresponda a la solicitud actual.
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    // GET /Home/Error: presenta una pagina de error con un identificador de seguimiento.
    public IActionResult Error()
    {
        // Usa el ID de la actividad diagnostica o, si no existe, el identificador de la solicitud HTTP.
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
