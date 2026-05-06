using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data;

public class TopekaDbContext : DbContext
{
    public TopekaDbContext(DbContextOptions<TopekaDbContext> options) : base(options) { }

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

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(
            typeof(TopekaDbContext).Assembly,
            type => type != typeof(Configurations.MasterUserConfig)
                && type != typeof(Configurations.DivisionConfig));
        base.OnModelCreating(mb);
    }
}
