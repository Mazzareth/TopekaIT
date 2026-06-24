using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data.Configurations;

/// <summary>
/// EF map for audit sessions.
/// </summary>
public class AuditSessionConfig : IEntityTypeConfiguration<AuditSession>
{
    public void Configure(EntityTypeBuilder<AuditSession> b)
    {
        b.ToTable("AuditSessions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasMaxLength(16);
        b.Property(x => x.DivisionId).HasMaxLength(64).IsRequired();
        b.Property(x => x.ConductedBy).HasMaxLength(16).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(2000);

        b.HasMany(x => x.Entries)
            .WithOne(x => x.Session)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
