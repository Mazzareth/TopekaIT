using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Infrastructure.Data;

namespace TopekaIT.Infrastructure.Data.Configurations;

public class LockerConfig : IEntityTypeConfiguration<Locker>
{
    public void Configure(EntityTypeBuilder<Locker> b)
    {
        b.ToTable("Lockers");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasMaxLength(16);
        b.Property(x => x.Number).HasMaxLength(32).IsRequired();
        b.Property(x => x.Section).HasMaxLength(64);
        b.Property(x => x.LockCombo).HasMaxLength(ComboProtection.ProtectedComboMaxLength);
        b.Property(x => x.LockSerial).HasMaxLength(64);
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.Property(x => x.LastAuditedBy).HasMaxLength(16);

        b.HasMany(x => x.Occupants)
            .WithOne(x => x.Locker)
            .HasForeignKey(x => x.LockerId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Assets)
            .WithOne(x => x.Locker)
            .HasForeignKey(x => x.LockerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
