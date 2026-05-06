using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Infrastructure.Data.Configurations;

namespace TopekaIT.Infrastructure.Data;

public class MasterDbContext : DbContext
{
    public MasterDbContext(DbContextOptions<MasterDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Division> Divisions => Set<Division>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(
            typeof(MasterDbContext).Assembly,
            type => type == typeof(MasterUserConfig) || type == typeof(DivisionConfig));
        base.OnModelCreating(mb);
    }
}
