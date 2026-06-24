using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data.Configurations;

public class MobileEquipmentSessionConfig : IEntityTypeConfiguration<MobileEquipmentSession>
{
    public void Configure(EntityTypeBuilder<MobileEquipmentSession> b)
    {
        b.ToTable("MobileEquipmentSessions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasMaxLength(16);
        b.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.UserId).HasMaxLength(16).IsRequired();
        b.Property(x => x.DivisionId).HasMaxLength(64).IsRequired();
        b.Property(x => x.ReaderDeviceSerial).HasMaxLength(128).IsRequired();
        b.Property(x => x.Platform).HasMaxLength(64);
        b.Property(x => x.AppVersion).HasMaxLength(32);

        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.ReaderDeviceSerial);
        b.HasIndex(x => x.ExpiresAt);
    }
}
