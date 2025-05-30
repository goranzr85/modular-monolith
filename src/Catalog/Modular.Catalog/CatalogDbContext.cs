﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Modular.Common;

namespace Modular.Catalog;

public sealed class CatalogDbContext : DbContext
{
    internal static readonly string Schema = "Catalogs";

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

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

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Type)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(c => c.Content)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(c => c.OccurredOnUtc)
                .IsRequired();

            builder.Property(c => c.Error)
                .HasMaxLength(3000);

            builder.ToTable("OutboxMessages");
        });

    }
}

