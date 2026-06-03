using Microsoft.EntityFrameworkCore;
using GestionEmpresarialApp.Models;

namespace GestionEmpresarialApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Rol> Roles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Rol>().ToTable("roles");
            modelBuilder.Entity<Usuario>().ToTable("usuarios");
        }
    }
}