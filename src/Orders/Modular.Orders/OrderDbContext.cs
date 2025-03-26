using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Modular.Common;
using Modular.Orders.Models;

namespace Modular.Orders;
public sealed class OrderDbContext : DbContext
{
    internal static readonly string Schema = "Orders";

    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        var priceConverter = new ValueConverter<Price, decimal>(
            price => (decimal)price,
            value => Price.Create(value));

        modelBuilder.Entity<Order>(builder =>
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.OrderDate)
                .IsRequired();

            builder.Property(c => c.Status)
                .IsRequired();

            builder.HasMany(c => c.Items)
                .WithOne()
                .HasForeignKey(i => i.OrderId);

            builder.Property(c => c.TotalAmount)
               .HasConversion(priceConverter)
               .IsRequired();

            builder.Property(c => c.CustomerId)
                .IsRequired();

            builder.Property(c => c.Status)
                .HasConversion(p => p.Name, p => OrderStatus.FromName(p)!)
                .IsRequired();

            builder.ToTable("Orders");
        });

        modelBuilder.Entity<Product>(builder =>
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.SKU)
                .IsRequired();

            builder.Property(c => c.Name)
                .HasMaxLength(50)
                .IsRequired();

            builder.HasIndex(c => c.SKU);
            builder.HasIndex(c => c.Name);

            builder.Property(c => c.Price)
                .HasConversion(priceConverter)
                .IsRequired();

            builder.ToTable("Products");
        });

        modelBuilder.Entity<OrderItem>(builder =>
        {
            builder.HasKey(c => new { c.OrderId, c.ProductId });

            builder.Property(c => c.Quantity)
                .IsRequired();

            builder.Property(c => c.Price)
                .HasConversion(priceConverter)
                .IsRequired();

            builder.ToTable("OrderItems");
        });
    }
}
