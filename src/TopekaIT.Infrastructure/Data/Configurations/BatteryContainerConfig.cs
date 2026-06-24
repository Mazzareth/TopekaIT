using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data.Configurations;

/// <summary>
/// EF map for battery containers.
/// </summary>
public class BatteryContainerConfig : IEntityTypeConfiguration<BatteryContainer>
{
    public void Configure(EntityTypeBuilder<BatteryContainer> b)
    {
        b.ToTable("BatteryContainers");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasMaxLength(16);
        b.Property(x => x.Name).HasMaxLength(128).IsRequired();
        b.Property(x => x.Location).HasMaxLength(256);
        b.Property(x => x.Notes).HasMaxLength(1000);
    }
}
