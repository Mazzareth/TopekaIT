using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data.Configurations;

/// <summary>
/// EF map for master division rows.
/// </summary>
public class DivisionConfig : IEntityTypeConfiguration<Division>
{
    public void Configure(EntityTypeBuilder<Division> b)
    {
        b.ToTable("Divisions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasMaxLength(64);
        b.Property(x => x.Name).HasMaxLength(128).IsRequired();
        b.Property(x => x.ConnectionString).HasMaxLength(1024).IsRequired();
        b.Property(x => x.PrinterPasswordCode).HasMaxLength(32);
        b.Property(x => x.PrinterPasswordZipCode).HasMaxLength(16);
        b.Property(x => x.EquipmentCheckInIntervalDays).HasDefaultValue(30);
        b.Property(x => x.CreatedAt).HasColumnType("datetimeoffset");
    }
}
