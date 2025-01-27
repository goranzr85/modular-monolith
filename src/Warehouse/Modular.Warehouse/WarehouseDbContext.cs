using Microsoft.EntityFrameworkCore;

namespace Modular.Warehouse;

internal class WarehouseDbContext : DbContext
{
    internal static readonly string Schema = "Warehouse";

    public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Product>(builder =>
        {
            builder.HasKey(c => c.Sku);

            builder.Property(c => c.Sku)
                .HasMaxLength(15)
                .IsRequired();

            builder.HasIndex(c => c.Sku).IsUnique();

            builder.Property(c => c.Quantity).IsRequired();

            builder.ToTable("Products");
        });

    }
}

