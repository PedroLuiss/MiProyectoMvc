using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiProyectoMvc.Models;

namespace MiProyectoMvc.Data
{
	// Heredamos de IdentityDbContext para que gestione automáticamente las tablas de Usuarios, Roles, etc.
	public class ApplicationDbContext : IdentityDbContext
	{


		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			: base(options)
		{
		}

		public DbSet<Producto> Productos { get; set; }
	}
}