using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data.Configurations;

/// <summary>
/// EF map for RMA records.
/// </summary>
public class RmaRecordConfig : IEntityTypeConfiguration<RmaRecord>
{
    public void Configure(EntityTypeBuilder<RmaRecord> b)
    {
        b.ToTable("RmaRecords");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasMaxLength(16);
        b.Property(x => x.AssetId).HasMaxLength(16).IsRequired();
        b.Property(x => x.Comments).HasMaxLength(2000);
        b.Property(x => x.Section).HasMaxLength(64);
        b.Property(x => x.AssetTag).HasMaxLength(64);
        
        b.HasOne(x => x.Asset)
            .WithMany(a => a.RmaRecords)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
