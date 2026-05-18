using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Infrastructure.Data.Configurations;

namespace TopekaIT.Infrastructure.Data;

public class MasterDbContext : DbContext
{
    private readonly IDataProtectionProvider _dataProtectionProvider;

    public MasterDbContext(
        DbContextOptions<MasterDbContext> options,
        IDataProtectionProvider dataProtectionProvider) : base(options)
    {
        _dataProtectionProvider = dataProtectionProvider
            ?? throw new ArgumentNullException(nameof(dataProtectionProvider));
    }

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
        ComboProtection.ApplyLegacyUserLockerProtection(mb, _dataProtectionProvider);
        base.OnModelCreating(mb);
    }
}
