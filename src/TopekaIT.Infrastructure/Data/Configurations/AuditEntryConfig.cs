using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data.Configurations;

public class AuditEntryConfig : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> b)
    {
        b.ToTable("AuditEntries");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasMaxLength(16);
        b.Property(x => x.SessionId).HasMaxLength(16).IsRequired();
        b.Property(x => x.AssetId).HasMaxLength(16).IsRequired();
        b.Property(x => x.LockerId).HasMaxLength(16);
        b.Property(x => x.DiscrepancyNote).HasMaxLength(512);

        b.HasOne(x => x.Session)
            .WithMany(x => x.Entries)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
