using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data.Configurations;

/// <summary>
/// EF map for issue tags attached to assets.
/// </summary>
public class AssetIssueTagConfig : IEntityTypeConfiguration<AssetIssueTag>
{
    public void Configure(EntityTypeBuilder<AssetIssueTag> b)
    {
        b.ToTable("AssetIssueTags");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasMaxLength(16);
        b.Property(x => x.AssetId).HasMaxLength(16).IsRequired();
        b.Property(x => x.DefinitionCode).HasMaxLength(64).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.Property(x => x.TaggedBy).HasMaxLength(16).IsRequired();
        b.Property(x => x.ResolvedBy).HasMaxLength(16);

        b.HasOne(x => x.Asset)
            .WithMany(x => x.IssueTags)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Definition)
            .WithMany(x => x.IssueTags)
            .HasForeignKey(x => x.DefinitionCode)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.AssetId, x.ResolvedAt });
    }
}
