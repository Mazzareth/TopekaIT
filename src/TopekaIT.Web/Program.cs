using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Ports;
using ApexCharts;
using TopekaIT.Core.Services;
using TopekaIT.Infrastructure;
using TopekaIT.Infrastructure.Data;
using TopekaIT.Infrastructure.Seed;
using TopekaIT.Web.Components;
using TopekaIT.Web.Services;

namespace TopekaIT.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
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

        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "TopekaAuth";
                options.LoginPath = "/login";
                options.LogoutPath = "/auth/logout";
                options.AccessDeniedPath = "/login";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            });

        builder.Services.AddAuthorization();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

        var topekaConnectionString = builder.Configuration.GetConnectionString("Topeka")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=TopekaIT;Trusted_Connection=true;";
        var masterConnectionString = builder.Configuration.GetConnectionString("Master")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=TopekaIT_Master;Trusted_Connection=true;";
        builder.Services.AddMasterInfrastructure(masterConnectionString);
        builder.Services.AddDivisionInfrastructure();

        builder.Services.AddScoped<UserService>();
        builder.Services.AddScoped<DivisionService>();
        builder.Services.AddScoped<PrinterService>();
        builder.Services.AddScoped<PrinterModelService>();
        builder.Services.AddScoped<AssetService>();
        builder.Services.AddScoped<AssetModelService>();
        builder.Services.AddScoped<TicketService>();
        builder.Services.AddScoped<ActivityService>();
        builder.Services.AddScoped<PingHistoryService>();
        builder.Services.AddScoped<PrinterEventService>();
        builder.Services.AddScoped<PrinterSetupService>();
        builder.Services.AddSingleton<IPrinterSetupTelnetClient, PrinterSetupTelnetClient>();
        builder.Services.AddSingleton(_ =>
        {
            var settings = new PrinterSetupSettings();
            builder.Configuration.GetSection("PrinterAutoSetup").Bind(settings);
            return settings;
        });
        builder.Services.AddSingleton<PrinterSnmpService>();

        builder.Services.AddScoped<AppState>();

        builder.Services.AddApexCharts();
        builder.Services.AddHostedService<PrinterMonitoringService>();
        builder.Services.AddHostedService<PrinterLogSinkService>();
        builder.Services.AddHostedService<PrinterSnmpTrapSinkService>();
        builder.Services.AddHostedService<PingRetentionService>();

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
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        using (var scope = app.Services.CreateScope())
        {
            var masterFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MasterDbContext>>();
            await using (var masterDb = await masterFactory.CreateDbContextAsync())
            {
                await DataSeeder.SeedMasterAsync(masterDb, topekaConnectionString);
            }

            var divisionRepo = scope.ServiceProvider.GetRequiredService<IDivisionRepository>();
            foreach (var division in await divisionRepo.GetAllAsync())
            {
                var options = new DbContextOptionsBuilder<TopekaDbContext>()
                    .UseSqlServer(division.ConnectionString, sql => sql.CommandTimeout(120))
                    .Options;
                await using var divisionDb = new TopekaDbContext(options);
                await DataSeeder.SeedDivisionAsync(divisionDb);
            }
        }

        app.Run();
    }
}
