﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modular.Customers.Migrations
{
    [DbContext(typeof(CustomerDbContext))]
    [Migration("20241011123123_CreateCustomerTable")]
    partial class CreateCustomerTable
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("Users")
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Modular.Customers.Customer", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.ComplexProperty<Dictionary<string, object>>("Address", "Modular.Customers.Customer.Address#Address", b1 =>
                        {
                            b1.IsRequired();

                            b1.Property<string>("City")
                                .IsRequired()
                                .HasMaxLength(30)
                                .HasColumnType("character varying(30)")
                                .HasColumnName("City");

                            b1.Property<string>("State")
                                .IsRequired()
                                .HasMaxLength(50)
                                .HasColumnType("character varying(50)")
                                .HasColumnName("State");

                            b1.Property<string>("Street")
                                .IsRequired()
                                .HasMaxLength(50)
                                .HasColumnType("character varying(50)")
                                .HasColumnName("Street");

                            b1.Property<string>("Zip")
                                .IsRequired()
                                .HasMaxLength(15)
                                .HasColumnType("character varying(15)")
                                .HasColumnName("Zip");
                        });

                    b.ComplexProperty<Dictionary<string, object>>("Contact", "Modular.Customers.Customer.Contact#Contact", b1 =>
                        {
                            b1.IsRequired();

                            b1.Property<string>("Email")
                                .HasMaxLength(80)
                                .HasColumnType("character varying(80)")
                                .HasColumnName("Email");

                            b1.Property<string>("Phone")
                                .HasMaxLength(50)
                                .HasColumnType("character varying(50)")
                                .HasColumnName("Phone");
                        });

                    b.ComplexProperty<Dictionary<string, object>>("FullName", "Modular.Customers.Customer.FullName#FullName", b1 =>
                        {
                            b1.IsRequired();

                            b1.Property<string>("FirstName")
                                .IsRequired()
                                .HasMaxLength(50)
                                .HasColumnType("character varying(50)")
                                .HasColumnName("FirstName");

                            b1.Property<string>("LastName")
                                .IsRequired()
                                .HasMaxLength(50)
                                .HasColumnType("character varying(50)")
                                .HasColumnName("LastName");

                            b1.Property<string>("MiddleName")
                                .HasMaxLength(50)
                                .HasColumnType("character varying(50)")
                                .HasColumnName("MiddleName");
                        });

                    b.HasKey("Id");

                    b.ToTable("Customers", "Users");
                });
#pragma warning restore 612, 618
        }
    }
}
