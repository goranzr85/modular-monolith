﻿using Microsoft.EntityFrameworkCore;
using Modular.Customers.Configuration;
using Modular.Customers.Models;

namespace Modular.Customers;

public class CustomerDbContext : DbContext
{
    internal static readonly string Schema = "Users";

    public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema); // This line will work now

        modelBuilder.Entity<Customer>(builder =>
        {
            builder.HasKey(c => c.Id);

            builder.ComplexProperty(c => c.FullName, buildAction =>
            {
                buildAction.Property(o => o!.FirstName)
                    .HasColumnName("FirstName")
                    .HasMaxLength(FullNameConfiguration.FirstNameLength)
                    .IsRequired();

                buildAction.Property(o => o!.LastName)
                    .HasColumnName("LastName")
                    .HasMaxLength(FullNameConfiguration.LastNameLength)
                    .IsRequired();

                buildAction.Property(o => o!.MiddleName)
                    .HasColumnName("MiddleName")
                    .HasMaxLength(FullNameConfiguration.MiddleNameLength)
                    .IsRequired(false);

                buildAction.IsRequired();
            });

            builder.ComplexProperty(c => c.Address, buildAction =>
            {
                buildAction.Property(o => o!.City)
                    .HasColumnName("City")
                    .HasMaxLength(AddressConfiguration.CityMaxLength)
                    .IsRequired();

                buildAction.Property(o => o!.Street)
                    .HasColumnName("Street")
                    .HasMaxLength(AddressConfiguration.StreetMaxLength)
                    .IsRequired();

                buildAction.Property(o => o!.State)
                    .HasColumnName("State")
                    .HasMaxLength(AddressConfiguration.StateMaxLength)
                    .IsRequired();

                buildAction.Property(o => o!.Zip)
                    .HasColumnName("Zip")
                    .HasMaxLength(AddressConfiguration.ZipMaxLength)
                    .IsRequired();

                buildAction.IsRequired();
            });
            
            builder.ComplexProperty(c => c.ShippingAddress, buildAction =>
            {
                buildAction.Property(o => o!.City)
                    .HasColumnName("ShippingCity")
                    .HasMaxLength(AddressConfiguration.CityMaxLength)
                    .IsRequired();

                buildAction.Property(o => o!.Street)
                    .HasColumnName("ShippingStreet")
                    .HasMaxLength(AddressConfiguration.StreetMaxLength)
                    .IsRequired();

                buildAction.Property(o => o!.State)
                    .HasColumnName("ShippingState")
                    .HasMaxLength(AddressConfiguration.StateMaxLength)
                    .IsRequired();

                buildAction.Property(o => o!.Zip)
                    .HasColumnName("ShippingZip")
                    .HasMaxLength(AddressConfiguration.ZipMaxLength)
                    .IsRequired();

                buildAction.IsRequired();
            });

            builder.ComplexProperty(c => c.Contact, buildAction =>
            {
                buildAction.Property(o => o!.Email)
                    .HasColumnName("Email")
                    .HasMaxLength(ContactConfiguration.EmailMaxLength)
                    .IsRequired(false); // Email is nullable

                buildAction.Property(o => o!.Phone)
                    .HasColumnName("Phone")
                    .HasMaxLength(ContactConfiguration.PhoneMaxLength)
                    .IsRequired(false); // Phone is nullable

                buildAction.IsRequired();
            });


            builder.ToTable("Customers"); // Define the table name and schema
        });

    }
}

