using Microsoft.EntityFrameworkCore;
using WonderpediaAPI.Models;

namespace WonderpediaAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }

        public DbSet<HistorialLogro> HistorialLogros { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>()
                .ToTable("Usuarios", tb => tb.HasTrigger("trg_RegistrarLogroUsuario"));

            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Nombre)
                .IsUnique();

            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Correo)
                .IsUnique();

            modelBuilder.Entity<Usuario>()
                .Property(u => u.FechaCreacion)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Usuario>()
                .Property(u => u.FinalizarIngles)
                .HasDefaultValue(false);

            modelBuilder.Entity<Usuario>()
                .Property(u => u.FinalizarMates)
                .HasDefaultValue(false);

            modelBuilder.Entity<Usuario>()
                .Property(u => u.FinalizarHistoria)
                .HasDefaultValue(false);

            modelBuilder.Entity<HistorialLogro>()
                .ToTable("HistorialLogros");

            modelBuilder.Entity<HistorialLogro>()
                .Property(h => h.FechaLogro)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<HistorialLogro>()
                .HasOne(h => h.Usuario)
                .WithMany(u => u.HistorialLogros)
                .HasForeignKey(h => h.UsuarioId);
        }
    }
}