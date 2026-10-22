using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiProyectoMvc.Data;
using MiProyectoMvc.Repositories;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar conexión a SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Configurar ASP.NET Core Identity (Autenticación de usuarios)
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();

// 3. Registrar el Patrón Repositorio en el contenedor de Inyección de Dependencias
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();

// Agregar soporte para MVC (Vistas Razor y Controladores)
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configurar el pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ¡Importante!: Autenticación antes de Autorización
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); // Necesario para las páginas de Identity por defecto

app.Run();