using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using MiProyectoMvc.Data;
using MiProyectoMvc.Repositories;
using MiProyectoMvc.Services;
using System.Globalization;

// WebApplication.CreateBuilder carga la configuracion, registra servicios del framework
// y prepara el contenedor de inyeccion de dependencias.
var builder = WebApplication.CreateBuilder(args);

// Lee la cadena "DefaultConnection" desde appsettings.json o desde la configuracion
// del entorno. Si no existe, se detiene la aplicacion con un error claro.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Registra el contexto de EF Core para que pueda inyectarse en otras clases.
// UseSqlServer indica que la persistencia se realiza en SQL Server.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configura Identity para gestionar cuentas, inicio de sesion y contrasenas.
// AddEntityFrameworkStores guarda los datos de identidad en ApplicationDbContext.
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Cuando una clase solicite IProductoRepository, ASP.NET creara ProductoRepository.
// Scoped crea una instancia por solicitud HTTP, compartida durante esa solicitud.
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();

// El almacenamiento de fotos tambien se resuelve por interfaz.
builder.Services.AddScoped<IImagenStorage, ImagenStorage>();

// Limita el tamano del formulario multipart; la validacion de 2 MB se hace en ImagenStorage.
builder.Services.Configure<FormOptions>(options =>
    options.MultipartBodyLengthLimit = 3 * 1024 * 1024);

// Habilita controladores y vistas Razor. Identity tambien necesita endpoints Razor Pages.
builder.Services.AddControllersWithViews();

// Mantiene consistente el formato decimal entre formularios, validacion y precios.
var culturaAplicacion = new CultureInfo("es-MX");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture(culturaAplicacion);
    options.SupportedCultures = [culturaAplicacion];
    options.SupportedUICultures = [culturaAplicacion];
});

var app = builder.Build();

// En produccion se muestra una pagina de error general y se activa HSTS.
// En desarrollo se conserva informacion util para depurar errores.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Middleware: cada componente procesa la solicitud antes de pasarla al siguiente.
app.UseHttpsRedirection();
app.UseStaticFiles();

// Establece la cultura antes de que MVC enlace y valide los datos del formulario.
app.UseRequestLocalization();

// Routing encuentra que controlador/accion corresponde a la URL solicitada.
app.UseRouting();

// Authentication identifica al usuario; Authorization comprueba sus permisos.
// El orden importa: la autorizacion necesita conocer primero la identidad.
app.UseAuthentication();
app.UseAuthorization();

// Ruta convencional: por ejemplo, /Productos/Index llama a esa accion.
// Si se omiten partes, se usan Home, Index y un id opcional.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Publica las paginas Razor incluidas por Identity, como Login y Register.
app.MapRazorPages();

// Inicia el servidor web y comienza a aceptar solicitudes.
app.Run();