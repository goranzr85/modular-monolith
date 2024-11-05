using Microsoft.EntityFrameworkCore;
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
                    .HasMaxLength(50)
                    .IsRequired();

                buildAction.Property(o => o!.LastName)
                    .HasColumnName("LastName")
                    .HasMaxLength(50)
                    .IsRequired();

                buildAction.Property(o => o!.MiddleName)
                    .HasColumnName("MiddleName")
                    .HasMaxLength(50)
                    .IsRequired(false);

                buildAction.IsRequired();
            });

            builder.ComplexProperty(c => c.Address, buildAction =>
            {
                buildAction.Property(o => o!.City)
                    .HasColumnName("City")
                    .HasMaxLength(30)
                    .IsRequired();

                buildAction.Property(o => o!.Street)
                    .HasColumnName("Street")
                    .HasMaxLength(50)
                    .IsRequired();

                buildAction.Property(o => o!.State)
                    .HasColumnName("State")
                    .HasMaxLength(50)
                    .IsRequired();

                buildAction.Property(o => o!.Zip)
                    .HasColumnName("Zip")
                    .HasMaxLength(15)
                    .IsRequired();

                buildAction.IsRequired();
            });

            builder.ComplexProperty(c => c.Contact, buildAction =>
            {
                buildAction.Property(o => o!.Email)
                    .HasColumnName("Email")
                    .HasMaxLength(80)
                    .IsRequired(false); // Email is nullable

                buildAction.Property(o => o!.Phone)
                    .HasColumnName("Phone")
                    .HasMaxLength(50)
                    .IsRequired(false); // Phone is nullable

                buildAction.IsRequired();
            });


            builder.ToTable("Customers"); // Define the table name and schema
        });

    }
}

