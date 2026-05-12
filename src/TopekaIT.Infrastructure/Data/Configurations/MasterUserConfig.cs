using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;

namespace TopekaIT.Infrastructure.Data.Configurations;

public class MasterUserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasMaxLength(16);
        b.Property(x => x.Name).HasMaxLength(128).IsRequired();
        b.Property(x => x.Username).HasMaxLength(64).IsRequired();
        b.HasIndex(x => x.Username).IsUnique();
        b.Property(x => x.PasswordHash).HasMaxLength(128).IsRequired();
        b.Property(x => x.PasswordSalt).HasMaxLength(128).IsRequired();
        b.Property(x => x.Role)
            .HasConversion(
                v => v.ToString(),
                v => AccessTierExtensions.ParseTierOrWorker(v))
            .HasMaxLength(32);
        b.Property(x => x.Avatar).HasMaxLength(8);
        b.Property(x => x.Position).HasMaxLength(64);
        // Locker fields kept for backward-compat read; data migrated to Locker entity
        b.Property(x => x.LockerNumber).HasMaxLength(16);
        b.Property(x => x.LockerCombo).HasMaxLength(32);
        b.Property(x => x.LockSerialNumber).HasMaxLength(64);
        b.Property(x => x.DivisionId).HasMaxLength(64);
        b.Property(x => x.LastActiveAt).HasColumnType("datetimeoffset");
        b.Property(x => x.OnLOAReason).HasMaxLength(512);
    }
}
