using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data;

public class TopekaDbContext : DbContext
{
    private readonly IDataProtectionProvider _dataProtectionProvider;

    public TopekaDbContext(
        DbContextOptions<TopekaDbContext> options,
        IDataProtectionProvider dataProtectionProvider) : base(options)
    {
        _dataProtectionProvider = dataProtectionProvider
            ?? throw new ArgumentNullException(nameof(dataProtectionProvider));
    }

    public DbSet<Printer> Printers => Set<Printer>();
    public DbSet<PrinterModel> PrinterModels => Set<PrinterModel>();
    public DbSet<AssetModel> AssetModels => Set<AssetModel>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<ActivityEvent> Activity => Set<ActivityEvent>();
    public DbSet<RmaRecord> RmaRecords => Set<RmaRecord>();
    public DbSet<LoanRecord> LoanRecords => Set<LoanRecord>();
    public DbSet<PingSample> PingSamples => Set<PingSample>();
    public DbSet<PrinterEvent> PrinterEvents => Set<PrinterEvent>();
    public DbSet<PrinterAlertState> PrinterAlertStates => Set<PrinterAlertState>();

    public DbSet<Locker> Lockers => Set<Locker>();
    public DbSet<LockerOccupant> LockerOccupants => Set<LockerOccupant>();
    public DbSet<IssueTagDefinition> IssueTagDefinitions => Set<IssueTagDefinition>();
    public DbSet<AssetIssueTag> AssetIssueTags => Set<AssetIssueTag>();
    public DbSet<BatteryContainer> BatteryContainers => Set<BatteryContainer>();
    public DbSet<AuditSession> AuditSessions => Set<AuditSession>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<EquipmentTransaction> EquipmentTransactions => Set<EquipmentTransaction>();
    public DbSet<SavedView> SavedViews => Set<SavedView>();
    public DbSet<StatusFlagHistory> StatusFlagHistory => Set<StatusFlagHistory>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // Tenant databases must not create or configure master-only tables.
        mb.ApplyConfigurationsFromAssembly(
            typeof(TopekaDbContext).Assembly,
                type => type != typeof(Configurations.MasterUserConfig)
                && type != typeof(Configurations.DivisionConfig)
                && type != typeof(Configurations.UserPermissionOverrideConfig)
                && type != typeof(Configurations.LantronixDeviceConfig)
                && type != typeof(Configurations.LantronixPollSampleConfig));
        ComboProtection.ApplyLockerProtection(mb, _dataProtectionProvider);
        base.OnModelCreating(mb);
    }
}
