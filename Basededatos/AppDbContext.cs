using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext : DbContext   // 👈 DbContext (no DBContext)
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<vehiculos> Vehiculos { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<vehiculos>(e =>
        {
            e.ToTable("vehiculos");                       // nombre real de la tabla (minúsculas)

            e.HasKey(x => x.idvehiculo);
            e.Property(x => x.idvehiculo)
                .HasColumnName("idvehiculo")              // columna real
                .ValueGeneratedOnAdd();                   // IDENTITY

            e.Property(x => x.marca)
                .HasColumnName("marca")
                .HasMaxLength(15)
                .IsRequired();

            e.Property(x => x.kilometraje)
                .HasColumnName("kilometraje")
                .HasMaxLength(20)
                .IsRequired();

            e.Property(x => x.precio)
                .HasColumnName("precio")
                .HasColumnType("decimal(18,2)");          // si tu SQL es decimal(18,2)
            // Si tu SQL es float, usa: // e.Property(x => x.Precio).HasColumnName("precio");
        });
    }
}
