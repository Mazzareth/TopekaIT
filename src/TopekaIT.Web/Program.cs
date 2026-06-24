using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Ports;
using ApexCharts;
using TopekaIT.Core.Services;
using TopekaIT.Infrastructure;
using TopekaIT.Infrastructure.Data;
using TopekaIT.Infrastructure.Seed;
using TopekaIT.Web.Authorization;
using TopekaIT.Web.Components;
using TopekaIT.Web.Services;

namespace TopekaIT.Web;

/// <summary>
/// The web app's front door: hosting, auth, policies, services, background workers, health checks, and startup seeding all meet here.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        // Windows Service hosting starts from the service folder, so point content root back at the deployed app files.
        var webAppOptions = new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = Microsoft.Extensions.Hosting.WindowsServices.WindowsServiceHelpers.IsWindowsService() 
                ? AppContext.BaseDirectory : default
        };

        var builder = WebApplication.CreateBuilder(webAppOptions);
        builder.Host.UseWindowsService();

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddControllersWithViews();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHealthChecks();
        builder.Services.AddDataProtection()
            .SetApplicationName("TopekaITPortal");

        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "TopekaAuth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.LoginPath = "/login";
                options.LogoutPath = "/auth/logout";
                options.AccessDeniedPath = "/login";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            });

        // Every permission in Core becomes a named ASP.NET policy the Razor pages can use.
        builder.Services.AddAuthorization(AccessAuthorizationPolicies.AddPolicies);
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
        builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        var topekaConnectionString = builder.Configuration.GetConnectionString("Topeka")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=TopekaIT;Trusted_Connection=true;";
        var masterConnectionString = builder.Configuration.GetConnectionString("Master")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=TopekaIT_Master;Trusted_Connection=true;";

        builder.Services.AddMasterInfrastructure(masterConnectionString);
        builder.Services.AddDivisionInfrastructure();

        builder.Services.AddScoped<UserService>();
        builder.Services.AddScoped<AccessControlService>();
        builder.Services.AddScoped<DivisionService>();
        builder.Services.AddScoped<PrinterService>();
        builder.Services.AddScoped<PrinterModelService>();
        builder.Services.AddScoped<AssetService>();
        builder.Services.AddScoped<AssetModelService>();
        builder.Services.AddScoped<LockerService>();
        builder.Services.AddScoped<TicketService>();
        builder.Services.AddScoped<ActivityService>();
        builder.Services.AddScoped<RmaService>();
        builder.Services.AddScoped<EquipmentStationService>();
        builder.Services.AddScoped<MobileEquipmentService>();
        builder.Services.AddScoped<AuditService>();
        builder.Services.AddScoped<PingHistoryService>();
        builder.Services.AddScoped<PrinterEventService>();
        builder.Services.AddScoped<PrinterSetupService>();
        builder.Services.AddSingleton<PrintNetCommandCatalog>();
        builder.Services.AddScoped<LantronixDeviceService>();
        builder.Services.AddSingleton<IPrinterSetupTelnetClient, PrinterSetupTelnetClient>();
        builder.Services.AddSingleton<ILantronixFuelClient, LantronixFuelClient>();
        builder.Services.AddSingleton(_ =>
        {
            var settings = new PrinterSetupSettings();
            builder.Configuration.GetSection("PrinterAutoSetup").Bind(settings);
            return settings;
        });
        builder.Services.AddSingleton<PrinterSnmpService>();
        builder.Services.AddSingleton<PrinterRouteResolver>();

        builder.Services.AddScoped<AppState>();

        builder.Services.AddApexCharts();
        builder.Services.AddHostedService<PrinterMonitoringService>();
        builder.Services.AddHostedService<PrinterLogSinkService>();
        builder.Services.AddHostedService<PrinterSnmpTrapSinkService>();
        builder.Services.AddHostedService<PingRetentionService>();
        builder.Services.AddHostedService<LantronixAutoPollService>();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapControllers();
        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Lifetime.ApplicationStarted.Register(() =>
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await SeedStartupDataAsync(app.Services, topekaConnectionString);
                }
                catch (Exception ex)
                {
                    app.Logger.LogCritical(ex, "Startup data seeding failed. Stopping the portal host.");
                    await app.StopAsync();
                }
            });
        });

        await app.RunAsync();
    }

    static async Task SeedStartupDataAsync(IServiceProvider services, string topekaConnectionString)
    {
        using var scope = services.CreateScope();

        // Seed master first so the division list exists, then seed each tenant database on its own connection.
        var masterFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MasterDbContext>>();
        await using (var masterDb = await masterFactory.CreateDbContextAsync())
        {
            await DataSeeder.SeedMasterAsync(masterDb, topekaConnectionString);
        }

        var divisionRepo = scope.ServiceProvider.GetRequiredService<IDivisionRepository>();
        var dataProtectionProvider = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>();
        foreach (var division in await divisionRepo.GetAllAsync())
        {
            var options = new DbContextOptionsBuilder<TopekaDbContext>()
                .UseSqlServer(division.ConnectionString, sql => sql.CommandTimeout(120))
                .Options;
            await using var divisionDb = new TopekaDbContext(options, dataProtectionProvider);
            await DataSeeder.SeedDivisionAsync(divisionDb);
        }
    }

}
