using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data.Configurations;

public class ActivityConfig : IEntityTypeConfiguration<ActivityEvent>
{
    public void Configure(EntityTypeBuilder<ActivityEvent> b)
    {
        b.ToTable("ActivityEvents");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasMaxLength(24);
        b.Property(x => x.Kind).HasMaxLength(32);
        b.Property(x => x.Text).HasMaxLength(512);
        b.HasIndex(x => x.Timestamp);
    }
}
