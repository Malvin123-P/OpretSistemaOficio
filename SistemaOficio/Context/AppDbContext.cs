using Microsoft.EntityFrameworkCore;
using OfiGest.Entities;

namespace OfiGest.Context
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Rol> Roles { get; set; }
        public DbSet<Departamento> Departamentos { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<TipoOficio> TiposOficio { get; set; }
        public DbSet<Oficio> Oficios { get; set; }
        public DbSet<Divisiones> Divisiones { get; set; }
        public DbSet<LogOficio> LogOficios { get; set; }
        public DbSet<ContadorLocalOficio> ContadorLocalOficio { get; set; }
        public DbSet<Notificaciones> Notificaciones { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Rol>().HasIndex(r => r.Nombre).IsUnique();
            modelBuilder.Entity<Departamento>().HasIndex(d => d.Iniciales).IsUnique();
            modelBuilder.Entity<Usuario>().HasIndex(u => u.Correo).IsUnique();
            modelBuilder.Entity<TipoOficio>().HasIndex(t => t.Iniciales).IsUnique();
            modelBuilder.Entity<Oficio>().HasIndex(o => o.Codigo).IsUnique();
            modelBuilder.Entity<Divisiones>().HasIndex(o => o.Nombre).IsUnique();
            modelBuilder.Entity<Notificaciones>().HasIndex(o => o.Id).IsUnique();


        }
    }
}
