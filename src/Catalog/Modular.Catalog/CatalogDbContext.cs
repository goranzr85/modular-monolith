using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Modular.Catalog;

public class CatalogDbContext : DbContext
{
    internal static readonly string Schema = "Catalogs";

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Product>(builder =>
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Sku)
                .HasMaxLength(15)
                .IsRequired();

            builder.HasIndex(c => c.Sku).IsUnique();

            builder.Property(c => c.Name)
                .HasMaxLength(50)
                .IsRequired();

            builder.HasIndex(c => c.Name);

            var priceConverter = new ValueConverter<Price, decimal>(
                price => (decimal)price,
                value => Price.Create(value));

            builder.Property(c => c.Price)
                .HasConversion(priceConverter)
                .IsRequired();


            builder.ToTable("Products");
        });

    }
}

