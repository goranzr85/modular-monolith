using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Modular.Common;
public class OutputMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
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
    }
}
