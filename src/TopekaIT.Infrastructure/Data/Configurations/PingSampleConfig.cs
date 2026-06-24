using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data.Configurations;

/// <summary>
/// EF map for printer ping samples.
/// </summary>
public class PingSampleConfig : IEntityTypeConfiguration<PingSample>
{
    public void Configure(EntityTypeBuilder<PingSample> b)
    {
        b.ToTable("PingSamples");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.PrinterId).HasMaxLength(16).IsRequired();
        b.Property(x => x.Timestamp).HasColumnType("datetimeoffset");
        b.Property(x => x.Success).IsRequired();
        b.Property(x => x.LatencyMs);
        b.Property(x => x.FailureReason).HasMaxLength(128);

        b.HasIndex(x => new { x.PrinterId, x.Timestamp });

        b.HasOne(x => x.Printer)
            .WithMany()
            .HasForeignKey(x => x.PrinterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
