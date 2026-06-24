using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data.Configurations;

/// <summary>
/// EF map for the current active printer alert state.
/// </summary>
public class PrinterAlertStateConfig : IEntityTypeConfiguration<PrinterAlertState>
{
    public void Configure(EntityTypeBuilder<PrinterAlertState> b)
    {
        b.ToTable("PrinterAlertStates");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.PrinterId).HasMaxLength(16).IsRequired();
        b.Property(x => x.AlertKey).HasMaxLength(128).IsRequired();
        b.Property(x => x.AlertTitle).HasMaxLength(160).IsRequired();
        b.Property(x => x.AlertCategory).HasMaxLength(96).IsRequired();
        b.Property(x => x.AlertDetail).HasMaxLength(256);
        b.Property(x => x.FriendlyMessage).HasMaxLength(512);
        b.Property(x => x.Severity).HasMaxLength(16).IsRequired();
        b.Property(x => x.FirstSeenAt).HasColumnType("datetimeoffset");
        b.Property(x => x.LastSeenAt).HasColumnType("datetimeoffset");

        b.HasIndex(x => new { x.PrinterId, x.AlertKey }).IsUnique();
        b.HasIndex(x => new { x.BlipSuppressed, x.LastSeenAt });

        b.HasOne(x => x.Printer)
            .WithMany()
            .HasForeignKey(x => x.PrinterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
