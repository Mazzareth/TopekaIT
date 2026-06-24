using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data.Configurations;

/// <summary>
/// EF map for printer model names.
/// </summary>
public class PrinterModelConfig : IEntityTypeConfiguration<PrinterModel>
{
    public void Configure(EntityTypeBuilder<PrinterModel> b)
    {
        b.ToTable("PrinterModels");
        b.HasKey(x => x.Name);
        b.Property(x => x.Name).HasMaxLength(128).IsRequired();
        b.Property(x => x.SupportsLogging).HasDefaultValue(false);
    }
}
