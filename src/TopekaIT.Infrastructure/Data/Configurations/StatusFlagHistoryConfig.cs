using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data.Configurations;

/// <summary>
/// EF map for asset flag change history.
/// </summary>
public class StatusFlagHistoryConfig : IEntityTypeConfiguration<StatusFlagHistory>
{
    public void Configure(EntityTypeBuilder<StatusFlagHistory> b)
    {
        b.ToTable("StatusFlagHistory");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasMaxLength(16);
        b.Property(x => x.AssetId).HasMaxLength(16).IsRequired();
        b.Property(x => x.FlagChanged).HasConversion<int>();
        b.Property(x => x.ChangedBy).HasMaxLength(16);

        b.HasOne(x => x.Asset)
            .WithMany(x => x.FlagHistory)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.AssetId, x.ChangedAt });
    }
}
