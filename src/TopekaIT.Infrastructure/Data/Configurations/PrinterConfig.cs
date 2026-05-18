using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data.Configurations;

public class PrinterConfig : IEntityTypeConfiguration<Printer>
{
    public void Configure(EntityTypeBuilder<Printer> b)
    {
        b.ToTable("Printers");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasMaxLength(16);
        b.Property(x => x.Name).HasMaxLength(128).IsRequired();
        b.Property(x => x.Department).HasMaxLength(128);
        b.Property(x => x.Model).HasMaxLength(128);
        b.Property(x => x.IpAddress).HasMaxLength(64);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);

        b.Property(x => x.LastPingAt).HasColumnType("datetimeoffset");
        b.Property(x => x.LastLatencyMs);
        b.Property(x => x.ConsecutiveFailures).HasDefaultValue(0);

        b.Property(x => x.SerialNumber).HasMaxLength(64);
        b.Property(x => x.FirmwareVersion).HasMaxLength(64);
        b.Property(x => x.MacAddress).HasMaxLength(32);
        b.Property(x => x.Location).HasMaxLength(256);
        b.Property(x => x.Contact).HasMaxLength(256);
    }
}
