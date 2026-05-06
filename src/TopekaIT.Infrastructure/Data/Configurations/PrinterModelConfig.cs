using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data.Configurations;

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
