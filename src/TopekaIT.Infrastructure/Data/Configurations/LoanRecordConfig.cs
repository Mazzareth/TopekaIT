using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data.Configurations;

/// <summary>
/// EF map for spare-loan records.
/// </summary>
public class LoanRecordConfig : IEntityTypeConfiguration<LoanRecord>
{
    public void Configure(EntityTypeBuilder<LoanRecord> b)
    {
        b.ToTable("LoanRecords");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasMaxLength(16);
        b.Property(x => x.AssetId).HasMaxLength(16).IsRequired();
        b.Property(x => x.BorrowerId).HasMaxLength(16).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(500);
        b.Property(x => x.Comments).HasMaxLength(2000);
        
        b.HasOne(x => x.Asset)
            .WithMany(a => a.LoanRecords)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        // BorrowerId is a soft reference; users live in the master DB.
    }
}
