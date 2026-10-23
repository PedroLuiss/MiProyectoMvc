using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiProyectoMvc.Models;

namespace MiProyectoMvc.Data
{
    // IdentityDbContext incluye las entidades necesarias para usuarios, roles y claims.
    // Esta clase extiende ese modelo para incluir categorias y productos.
    public class ApplicationDbContext : IdentityDbContext
    {
        // ASP.NET Core inyecta estas opciones, configuradas en Program.cs con SQL Server.
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet representa cada tabla y permite consultarla/modificarla con LINQ.
        public DbSet<Producto> Productos { get; set; }

        public DbSet<Categoria> Categorias { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Fluent API: las reglas de persistencia viven aqui y no en las entidades.
            builder.Entity<Categoria>(entity =>
            {
                entity.Property(c => c.Nombre).HasMaxLength(80).IsRequired();
                entity.Property(c => c.Descripcion).HasMaxLength(250);
                entity.HasIndex(c => c.Nombre).IsUnique();

                // Datos de referencia: se aplican junto con la migracion.
                entity.HasData(
                    new Categoria { Id = 1, Nombre = "General", Descripcion = "Productos sin clasificar." },
                    new Categoria { Id = 2, Nombre = "Tecnología", Descripcion = "Equipos y accesorios." },
                    new Categoria { Id = 3, Nombre = "Hogar", Descripcion = "Artículos para el hogar." },
                    new Categoria { Id = 4, Nombre = "Ropa", Descripcion = "Prendas y accesorios." },
                    new Categoria { Id = 5, Nombre = "Alimentos", Descripcion = "Comida y bebida." });
            });

            builder.Entity<Producto>(entity =>
            {
                entity.Property(p => p.Nombre).HasMaxLength(120).IsRequired();
                entity.Property(p => p.Descripcion).HasMaxLength(500);
                entity.Property(p => p.Sku).HasMaxLength(40).IsRequired();
                entity.Property(p => p.Marca).HasMaxLength(80);
                entity.Property(p => p.Precio).HasPrecision(18, 2);
                entity.Property(p => p.ImagenUrl).HasMaxLength(300);

                // El SKU no se repite; la base lo garantiza ademas de la validacion.
                entity.HasIndex(p => p.Sku).IsUnique();
                entity.HasIndex(p => p.CategoriaId);

                // Restrict impide borrar una categoria que todavia tiene productos.
                entity.HasOne(p => p.Categoria)
                    .WithMany(c => c.Productos)
                    .HasForeignKey(p => p.CategoriaId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
