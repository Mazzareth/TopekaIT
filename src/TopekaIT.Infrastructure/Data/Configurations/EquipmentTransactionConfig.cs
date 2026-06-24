using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data.Configurations;

/// <summary>
/// EF map for equipment-station transaction receipts.
/// </summary>
public class EquipmentTransactionConfig : IEntityTypeConfiguration<EquipmentTransaction>
{
    public void Configure(EntityTypeBuilder<EquipmentTransaction> b)
    {
        b.ToTable("EquipmentTransactions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasMaxLength(16);
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.DivisionId).HasMaxLength(64).IsRequired();
        b.Property(x => x.AssetId).HasMaxLength(16).IsRequired();
        b.Property(x => x.LinkedAssetId).HasMaxLength(16);
        b.Property(x => x.EmployeeId).HasMaxLength(16);
        b.Property(x => x.CurrentHolderId).HasMaxLength(16);
        b.Property(x => x.ActorId).HasMaxLength(16);
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.TicketId).HasMaxLength(32);
        b.Property(x => x.TicketLink).HasMaxLength(256);
        b.Property(x => x.RmaRecordId).HasMaxLength(16);
        b.Property(x => x.RmaLink).HasMaxLength(256);
        b.Property(x => x.ScanSource).HasMaxLength(128);
        b.Property(x => x.MobileSessionId).HasMaxLength(16);
        b.Property(x => x.ReaderDeviceSerial).HasMaxLength(128);
        b.Property(x => x.ScannedLockerId).HasMaxLength(16);
        b.Property(x => x.LockerNumberSnapshot).HasMaxLength(32);
        b.Property(x => x.EmployeeNameSnapshot).HasMaxLength(128);
        b.Property(x => x.BeforeStatus).HasMaxLength(32);
        b.Property(x => x.AfterStatus).HasMaxLength(32);
        b.Property(x => x.BeforeHolderId).HasMaxLength(16);
        b.Property(x => x.AfterHolderId).HasMaxLength(16);
        b.Property(x => x.BeforeLockerId).HasMaxLength(16);
        b.Property(x => x.AfterLockerId).HasMaxLength(16);
        b.Property(x => x.BeforeFlags).HasConversion<int>();
        b.Property(x => x.AfterFlags).HasConversion<int>();
        b.Property(x => x.BeforeState).HasMaxLength(512);
        b.Property(x => x.AfterState).HasMaxLength(512);

        b.HasOne(x => x.Asset)
            .WithMany()
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.Timestamp);
        b.HasIndex(x => x.AssetId);
        b.HasIndex(x => x.EmployeeId);
        b.HasIndex(x => x.DivisionId);
    }
}
