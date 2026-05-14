using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data.Configurations;

public class AssetConfig : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> b)
    {
        b.ToTable("Assets");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasMaxLength(16);
        b.Property(x => x.Category).HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.Type).HasMaxLength(64);
        b.Property(x => x.Tag).HasMaxLength(64);
        b.Property(x => x.Serial).HasMaxLength(64);
        b.Property(x => x.Imei).HasMaxLength(32);
        b.Property(x => x.RfidTagId).HasMaxLength(64);
        b.Property(x => x.Model).HasMaxLength(128);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.Flags).HasConversion<int>();
        b.Property(x => x.HolderId).HasMaxLength(16);
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.ScannerType).HasMaxLength(16);
        b.Property(x => x.ScannerKind).HasConversion<string>().HasMaxLength(16);
        b.Property(x => x.PairedAssetId).HasMaxLength(16);
        b.Property(x => x.LockerId).HasMaxLength(16);
        b.Property(x => x.LastSeenLocation).HasMaxLength(128);

        // Self-referencing for scanner pairing — NoAction required; SQL Server rejects SetNull on self-ref FKs
        b.HasOne(x => x.PairedAsset)
            .WithMany()
            .HasForeignKey(x => x.PairedAssetId)
            .OnDelete(DeleteBehavior.NoAction);

        b.HasIndex(x => x.Serial);
        b.HasIndex(x => x.Tag);
        b.HasIndex(x => x.RfidTagId)
            .IsUnique()
            .HasFilter("[RfidTagId] IS NOT NULL");
        b.HasIndex(x => x.HolderId);
    }
}
