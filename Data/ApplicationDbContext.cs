using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiProyectoMvc.Models;

namespace MiProyectoMvc.Data
{
    // IdentityDbContext incluye las entidades necesarias para usuarios, roles y claims.
    // Esta clase extiende ese modelo para incluir tambien los productos de la aplicacion.
    public class ApplicationDbContext : IdentityDbContext
    {
        // ASP.NET Core inyecta estas opciones, configuradas en Program.cs con SQL Server.
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet representa la tabla Productos y permite consultarla/modificarla con LINQ.
        public DbSet<Producto> Productos { get; set; }
    }
}