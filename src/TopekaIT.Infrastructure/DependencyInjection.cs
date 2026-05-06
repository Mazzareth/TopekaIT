using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TopekaIT.Core.Ports;
using TopekaIT.Infrastructure.Data;
using TopekaIT.Infrastructure.Repositories;
using TopekaIT.Infrastructure.Tenant;

namespace TopekaIT.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMasterInfrastructure(this IServiceCollection services, string masterConnectionString)
    {
        services.AddDbContextFactory<MasterDbContext>(options =>
            options.UseSqlServer(masterConnectionString,
                sql => sql.CommandTimeout(120)));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDivisionRepository, DivisionRepository>();

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

        return services;
    }
}
