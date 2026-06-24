using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data.Configurations;

/// <summary>
/// EF map for locker occupant history.
/// </summary>
public class LockerOccupantConfig : IEntityTypeConfiguration<LockerOccupant>
{
    public void Configure(EntityTypeBuilder<LockerOccupant> b)
    {
        b.ToTable("LockerOccupants");
        b.HasKey(x => new { x.LockerId, x.UserId, x.AssignedAt });
        b.Property(x => x.LockerId).HasMaxLength(16);
        b.Property(x => x.UserId).HasMaxLength(16);
        b.Property(x => x.AssignedBy).HasMaxLength(16);
        b.Property(x => x.UnassignedBy).HasMaxLength(16);

        b.HasOne(x => x.Locker)
            .WithMany(x => x.Occupants)
            .HasForeignKey(x => x.LockerId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.UserId, x.UnassignedAt });
    }
}
