using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data.Configurations;

/// <summary>
/// EF map for manual user permission overrides.
/// </summary>
public class UserPermissionOverrideConfig : IEntityTypeConfiguration<UserPermissionOverride>
{
    public void Configure(EntityTypeBuilder<UserPermissionOverride> b)
    {
        b.ToTable("UserPermissionOverrides");
        b.HasKey(x => new { x.UserId, x.PermissionKey });
        b.Property(x => x.UserId).HasMaxLength(16);
        b.Property(x => x.PermissionKey).HasMaxLength(128);
        b.Property(x => x.State).HasConversion<string>().HasMaxLength(16);
        b.Property(x => x.UpdatedById).HasMaxLength(16);
        b.Property(x => x.UpdatedAt).HasColumnType("datetimeoffset");

        b.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.UpdatedBy)
            .WithMany()
            .HasForeignKey(x => x.UpdatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
