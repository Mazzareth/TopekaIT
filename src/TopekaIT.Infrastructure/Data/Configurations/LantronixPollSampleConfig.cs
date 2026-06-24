using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data.Configurations;

/// <summary>
/// EF map for Lantronix poll samples.
/// </summary>
public class LantronixPollSampleConfig : IEntityTypeConfiguration<LantronixPollSample>
{
    public void Configure(EntityTypeBuilder<LantronixPollSample> b)
    {
        b.ToTable("LantronixPollSamples");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.DeviceId).HasMaxLength(16).IsRequired();
        b.Property(x => x.Timestamp).HasColumnType("datetimeoffset");
        b.Property(x => x.Success).IsRequired();
        b.Property(x => x.FailureReason).HasMaxLength(256);
        b.Property(x => x.ReportName).HasMaxLength(64);
        b.Property(x => x.Product).HasMaxLength(64);
        b.Property(x => x.Volume).HasPrecision(18, 2);
        b.Property(x => x.TcVolume).HasPrecision(18, 2);
        b.Property(x => x.Ullage).HasPrecision(18, 2);
        b.Property(x => x.Height).HasPrecision(18, 2);
        b.Property(x => x.Water).HasPrecision(18, 2);
        b.Property(x => x.Temperature).HasPrecision(18, 2);
        b.Property(x => x.RawResponse).HasMaxLength(4000);

        b.HasIndex(x => new { x.DeviceId, x.Timestamp });

        b.HasOne(x => x.Device)
            .WithMany()
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
