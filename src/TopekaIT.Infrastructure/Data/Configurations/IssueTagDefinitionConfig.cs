using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data.Configurations;

/// <summary>
/// EF map for the issue-tag dictionary.
/// </summary>
public class IssueTagDefinitionConfig : IEntityTypeConfiguration<IssueTagDefinition>
{
    public void Configure(EntityTypeBuilder<IssueTagDefinition> b)
    {
        b.ToTable("IssueTagDefinitions");
        b.HasKey(x => x.Code);
        b.Property(x => x.Code).HasMaxLength(64);
        b.Property(x => x.Label).HasMaxLength(128).IsRequired();
        b.Property(x => x.Description).HasMaxLength(512);
        b.Property(x => x.Severity).HasConversion<string>().HasMaxLength(16);
        b.Property(x => x.ApplicableCategories).HasMaxLength(128);

        b.HasMany(x => x.IssueTags)
            .WithOne(x => x.Definition)
            .HasForeignKey(x => x.DefinitionCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
