using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MiProyectoMvc.Models;

namespace MiProyectoMvc.Controllers;

// Acciones para las paginas generales que no pertenecen al catalogo de productos.
public class HomeController : Controller
{
    // La convencion de MVC busca Views/Home/Index.cshtml.
    public IActionResult Index()
    {
        return View();
    }

    // Muestra la pagina informativa de privacidad.
    public IActionResult Privacy()
    {
        return View();
    }

    // Evita guardar en cache la pagina de error, para que cada solicitud muestre su propio ID.
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        // Se prefiere el ID de la actividad diagnostica; si no existe, se usa el ID HTTP.
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
