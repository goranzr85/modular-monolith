using Microsoft.EntityFrameworkCore;
using Modular.Common.User.Configuration;
using Modular.Notifications.Customers;

namespace Modular.Notifications;
public sealed class NotificationDbContext : DbContext
{
    internal static readonly string Schema = "Notifications";

    public DbSet<Customer> Customers { get; set; }
    public DbSet<InboxMessage> InboxMessages { get; set; }

    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

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

            builder.ComplexProperty(c => c.Contact, buildAction =>
            {
                buildAction.Property(o => o!.Email)
                    .HasColumnName("Email")
                    .HasMaxLength(ContactConfiguration.EmailMaxLength)
                    .IsRequired(false);

                buildAction.Property(o => o!.Phone)
                    .HasColumnName("Phone")
                    .HasMaxLength(ContactConfiguration.PhoneMaxLength)
                    .IsRequired(false);

                buildAction.Property(o => o!.PrimaryContactType)
                    .HasColumnName("PrimaryContactType")
                    .HasConversion<string>()
                    .IsRequired();

                buildAction.IsRequired();
            });


            builder.ToTable("Customers");
        });

        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.ToTable("InboxMessages");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.MessageType)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(e => e.Payload)
                  .IsRequired();

            entity.Property(e => e.ReceivedAt)
                  .IsRequired();

            entity.Property(e => e.ProcessedAt)
                  .IsRequired();
        });
    }
}
