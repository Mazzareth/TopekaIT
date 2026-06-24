using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TopekaIT.Core.Ports;
using TopekaIT.Infrastructure.Data;
using TopekaIT.Infrastructure.Repositories;
using TopekaIT.Infrastructure.Tenant;

namespace TopekaIT.Infrastructure;

/// <summary>
/// Wires Core ports to EF repositories. Master services are global; division services run against whichever tenant is active.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddMasterInfrastructure(this IServiceCollection services, string masterConnectionString)
    {
        services.AddDbContextFactory<MasterDbContext>(options =>
            options.UseSqlServer(masterConnectionString,
                sql => sql.CommandTimeout(120)));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserPermissionOverrideRepository, UserPermissionOverrideRepository>();
        services.AddScoped<IDivisionRepository, DivisionRepository>();
        services.AddScoped<ILantronixDeviceRepository, LantronixDeviceRepository>();

        return services;
    }

    public static IServiceCollection AddDivisionInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IDivisionDbContextFactory, TenantDivisionDbContextFactory>();

        services.AddScoped<IPrinterRepository, PrinterRepository>();
        services.AddScoped<IPrinterModelRepository, PrinterModelRepository>();
        services.AddScoped<IAssetModelRepository, AssetModelRepository>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<IPingHistoryRepository, PingHistoryRepository>();
        services.AddScoped<IPrinterEventRepository, PrinterEventRepository>();
        services.AddScoped<ILockerRepository, LockerRepository>();
        services.AddScoped<IRmaRecordRepository, RmaRecordRepository>();
        services.AddScoped<IEquipmentTransactionRepository, EquipmentTransactionRepository>();
        services.AddScoped<IMobileEquipmentSessionRepository, MobileEquipmentSessionRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();

        return services;
    }
}
