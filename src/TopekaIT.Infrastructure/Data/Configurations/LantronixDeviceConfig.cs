using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data.Configurations;

public class LantronixDeviceConfig : IEntityTypeConfiguration<LantronixDevice>
{
    public void Configure(EntityTypeBuilder<LantronixDevice> b)
    {
        b.ToTable("LantronixDevices");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasMaxLength(16);
        b.Property(x => x.Name).HasMaxLength(128).IsRequired();
        b.Property(x => x.DivisionId).HasMaxLength(64);
        b.Property(x => x.Hostname).HasMaxLength(128);
        b.Property(x => x.IpAddress).HasMaxLength(64).IsRequired();
        b.Property(x => x.Port).IsRequired();
        b.Property(x => x.PollCommand).HasMaxLength(32).IsRequired();
        b.Property(x => x.DeviceType).HasMaxLength(64).IsRequired();
        b.Property(x => x.SerialSettings).HasMaxLength(128);
        b.Property(x => x.CreatedAt).HasColumnType("datetimeoffset");
        b.Property(x => x.LastPollAt).HasColumnType("datetimeoffset");
        b.Property(x => x.LastFailureReason).HasMaxLength(256);
        b.Property(x => x.LastFuelVolume).HasPrecision(18, 2);
        b.Property(x => x.LastTcVolume).HasPrecision(18, 2);
        b.Property(x => x.LastUllage).HasPrecision(18, 2);
        b.Property(x => x.LastHeight).HasPrecision(18, 2);
        b.Property(x => x.LastWater).HasPrecision(18, 2);
        b.Property(x => x.LastTemperature).HasPrecision(18, 2);

        b.HasIndex(x => x.DivisionId);
        b.HasIndex(x => x.Name);

        b.HasOne(x => x.Division)
            .WithMany()
            .HasForeignKey(x => x.DivisionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
