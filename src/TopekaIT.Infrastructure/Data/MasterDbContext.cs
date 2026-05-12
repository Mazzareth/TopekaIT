using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Infrastructure.Data.Configurations;

namespace TopekaIT.Infrastructure.Data;

public class MasterDbContext : DbContext
{
    public MasterDbContext(DbContextOptions<MasterDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserPermissionOverride> UserPermissionOverrides => Set<UserPermissionOverride>();
    public DbSet<Division> Divisions => Set<Division>();
    public DbSet<LantronixDevice> LantronixDevices => Set<LantronixDevice>();
    public DbSet<LantronixPollSample> LantronixPollSamples => Set<LantronixPollSample>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(
            typeof(MasterDbContext).Assembly,
            type => type == typeof(MasterUserConfig)
                || type == typeof(UserPermissionOverrideConfig)
                || type == typeof(DivisionConfig)
                || type == typeof(LantronixDeviceConfig)
                || type == typeof(LantronixPollSampleConfig));
        base.OnModelCreating(mb);
    }
}
