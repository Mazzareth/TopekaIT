using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data.Configurations;

/// <summary>
/// EF map for tickets.
/// </summary>
public class TicketConfig : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> b)
    {
        b.ToTable("Tickets");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasMaxLength(16);
        b.Property(x => x.Title).HasMaxLength(256).IsRequired();
        b.Property(x => x.Description).HasMaxLength(4000);
        b.Property(x => x.AssetId).HasMaxLength(16);
        b.Property(x => x.AssetType).HasConversion<string?>().HasMaxLength(16);
        b.Property(x => x.ReportedById).HasMaxLength(16).IsRequired();
        b.Property(x => x.AssigneeId).HasMaxLength(16);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        b.Property(x => x.Priority).HasConversion<string>().HasMaxLength(16);
        b.Property(x => x.Resolution).HasMaxLength(2000);
    }
}
