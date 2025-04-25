using Microsoft.EntityFrameworkCore;
using Modular.Warehouse.UseCases.Orders;

namespace Modular.Warehouse;
public sealed class OrderDbContext : DbContext
{
    internal static readonly string Schema = "warehouse";

    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Order>(builder =>
        {
            builder.HasKey(c => c.Id);

            builder.HasMany(c => c.Items)
                .WithOne()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ToTable("Orders");
        });

        modelBuilder.Entity<OrderItem>(builder =>
        {
            builder.HasKey(c => new { c.ProductSku, c.OrderId } );

            builder.Property(c => c.Quantity)
                .IsRequired();

            builder.ToTable("OrderItems");
        });
    }
}
