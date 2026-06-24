using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data.Configurations;

/// <summary>
/// EF map for the asset model pick-list.
/// </summary>
public class AssetModelConfig : IEntityTypeConfiguration<AssetModel>
{
    public void Configure(EntityTypeBuilder<AssetModel> builder)
    {
        builder.ToTable("AssetModels");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Name).IsRequired();
    }
}
