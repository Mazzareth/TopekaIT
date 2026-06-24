using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data.Configurations;

/// <summary>
/// EF map for raw printer events.
/// </summary>
public class PrinterEventConfig : IEntityTypeConfiguration<PrinterEvent>
{
    public void Configure(EntityTypeBuilder<PrinterEvent> b)
    {
        b.ToTable("PrinterEvents");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.PrinterId).HasMaxLength(16).IsRequired();
        b.Property(x => x.Timestamp).HasColumnType("datetimeoffset");
        b.Property(x => x.EventType).HasMaxLength(32).IsRequired();
        b.Property(x => x.RawMessage).HasMaxLength(2000).IsRequired();
        b.Property(x => x.Severity).HasMaxLength(16);
        b.Property(x => x.AlertKey).HasMaxLength(128);
        b.Property(x => x.AlertTitle).HasMaxLength(160);
        b.Property(x => x.AlertCategory).HasMaxLength(96);
        b.Property(x => x.AlertDetail).HasMaxLength(256);
        b.Property(x => x.FriendlyMessage).HasMaxLength(512);

        b.HasIndex(x => new { x.PrinterId, x.Timestamp });
        b.HasIndex(x => new { x.AlertKey, x.Timestamp });

        b.HasOne(x => x.Printer)
            .WithMany()
            .HasForeignKey(x => x.PrinterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
