using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Services;
using TopekaIT.Infrastructure.Data;

namespace TopekaIT.Infrastructure.Seed;

public static class DataSeeder
{
    private const string SuperAdminId = "bwilliams";
    private const string SuperAdminUsername = "bwilliams";
    private const string AdminPassword = "temp_password";
    private const string TopekaDivisionId = "6I-A";
    private const string FuelControllerDeviceId = "lan-6i-fuel";

    public static async Task SeedMasterAsync(MasterDbContext db, string topekaConnectionString, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        if (!await db.Users.AnyAsync(u => u.Username == SuperAdminUsername, ct))
        {
            var password = PasswordHasher.HashWithMetadata(AdminPassword);
            db.Users.Add(new User
            {
                Id = SuperAdminId,
                Name = "Brad Williams",
                Username = SuperAdminUsername,
                Role = AccessTier.SuperAdmin,
                Avatar = "BW",
                PasswordHash = password.hash,
                PasswordSalt = password.salt,
                PasswordIterations = password.iterations,
                MustChangePassword = true,
                DivisionId = null,
            });
            await db.SaveChangesAsync(ct);
        }

        var legacyTopeka = await db.Divisions.FirstOrDefaultAsync(d => d.Id == "topeka", ct);
        if (legacyTopeka != null)
        {
            db.Divisions.Remove(legacyTopeka);
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Divisions.AnyAsync(d => d.Id == TopekaDivisionId, ct))
        {
            db.Divisions.Add(new Division
            {
                Id = TopekaDivisionId,
                Name = "Topeka",
                ConnectionString = topekaConnectionString,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync(ct);
        }

        var fuelController = await db.LantronixDevices.FirstOrDefaultAsync(d => d.Id == FuelControllerDeviceId, ct);
        if (fuelController == null)
        {
            db.LantronixDevices.Add(new LantronixDevice
            {
                Id = FuelControllerDeviceId,
                Name = "6I Fuel Controller",
                DivisionId = TopekaDivisionId,
                Hostname = "tankcontroller6i.main.usfood.com",
                IpAddress = "10.36.152.222",
                Port = 10001,
                PollCommand = LantronixDeviceDefaults.InventoryCommand,
                DeviceType = "Lantronix XPort",
                SerialSettings = "RS232, 9600, 8, None, 1, Hardware",
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync(ct);
        }
        else
        {
            var changed = false;
            if (string.IsNullOrWhiteSpace(fuelController.Name))
            {
                fuelController.Name = "6I Fuel Controller";
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(fuelController.DivisionId))
            {
                fuelController.DivisionId = TopekaDivisionId;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(fuelController.Hostname))
            {
                fuelController.Hostname = "tankcontroller6i.main.usfood.com";
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(fuelController.IpAddress))
            {
                fuelController.IpAddress = "10.36.152.222";
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(fuelController.PollCommand))
            {
                fuelController.PollCommand = LantronixDeviceDefaults.InventoryCommand;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(fuelController.DeviceType))
            {
                fuelController.DeviceType = "Lantronix XPort";
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(fuelController.SerialSettings))
            {
                fuelController.SerialSettings = "RS232, 9600, 8, None, 1, Hardware";
                changed = true;
            }

            if (fuelController.Port <= 0)
            {
                fuelController.Port = 10001;
                changed = true;
            }

            if (changed)
            {
                await db.SaveChangesAsync(ct);
            }
        }
    }

    public static async Task SeedDivisionAsync(TopekaDbContext db, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        if (!await db.PrinterModels.AnyAsync(m => m.Name == PrinterModels.T8000, ct))
        {
            db.PrinterModels.Add(new PrinterModel
            {
                Name = PrinterModels.T8000,
                SupportsLogging = true,
            });
            await db.SaveChangesAsync(ct);
        }

        if (!await db.AssetModels.AnyAsync(m => m.Name == "WT6000", ct))
        {
            db.AssetModels.Add(new AssetModel
            {
                Id = Guid.NewGuid().ToString("N").Substring(0, 16),
                Name = "WT6000"
            });
            await db.SaveChangesAsync(ct);
        }

        await SeedIssueTagDefinitionsAsync(db, ct);
    }

    private static async Task SeedIssueTagDefinitionsAsync(TopekaDbContext db, CancellationToken ct)
    {
        if (await db.IssueTagDefinitions.AnyAsync(ct)) return;

        var definitions = new[]
        {
            new IssueTagDefinition { Code = "RightPortBroken",       Label = "Right port broken",        Severity = IssueSeverity.Critical, ApplicableCategories = "SaeDevice",          SortOrder = 10 },
            new IssueTagDefinition { Code = "LeftPortBroken",        Label = "Left port broken",         Severity = IssueSeverity.Critical, ApplicableCategories = "SaeDevice",          SortOrder = 11 },
            new IssueTagDefinition { Code = "ScreenCracked",         Label = "Screen cracked",           Severity = IssueSeverity.Warning,  ApplicableCategories = null,                  SortOrder = 20 },
            new IssueTagDefinition { Code = "ScreenBlackout",        Label = "Screen blackout",          Severity = IssueSeverity.Critical, ApplicableCategories = null,                  SortOrder = 21, Description = "Screen goes fully black during use" },
            new IssueTagDefinition { Code = "RebootLoop",            Label = "Reboot loop",              Severity = IssueSeverity.Critical, ApplicableCategories = null,                  SortOrder = 30, Description = "Device continuously restarts" },
            new IssueTagDefinition { Code = "WontCharge",            Label = "Won't charge",             Severity = IssueSeverity.Critical, ApplicableCategories = "SaeDevice,PodTc77",   SortOrder = 40 },
            new IssueTagDefinition { Code = "ChargesDraining",       Label = "Battery drains quickly",   Severity = IssueSeverity.Warning,  ApplicableCategories = "SaeDevice,PodTc77",   SortOrder = 41 },
            new IssueTagDefinition { Code = "WiFiDropping",          Label = "Wi-Fi dropping",           Severity = IssueSeverity.Warning,  ApplicableCategories = null,                  SortOrder = 50, Description = "Intermittent loss of wireless connection" },
            new IssueTagDefinition { Code = "PhysicalDamage",        Label = "Physical damage",          Severity = IssueSeverity.Warning,  ApplicableCategories = null,                  SortOrder = 60, Description = "Visible cracks, dents, or broken housing" },
            new IssueTagDefinition { Code = "ButtonStuck",           Label = "Button stuck / unresponsive", Severity = IssueSeverity.Warning, ApplicableCategories = null,              SortOrder = 70 },
            new IssueTagDefinition { Code = "SlowPerformance",       Label = "Slow / freezing",          Severity = IssueSeverity.Warning,  ApplicableCategories = null,                  SortOrder = 80 },
            new IssueTagDefinition { Code = "AppCrashing",           Label = "App crashing",             Severity = IssueSeverity.Warning,  ApplicableCategories = null,                  SortOrder = 90 },

            new IssueTagDefinition { Code = "TriggerStuck",          Label = "Trigger stuck",            Severity = IssueSeverity.Critical, ApplicableCategories = "Scanner",             SortOrder = 110 },
            new IssueTagDefinition { Code = "ScannerNotReading",     Label = "Scanner not reading",      Severity = IssueSeverity.Critical, ApplicableCategories = "Scanner",             SortOrder = 111 },
            new IssueTagDefinition { Code = "ScannerMisreading",     Label = "Frequent scan errors",     Severity = IssueSeverity.Warning,  ApplicableCategories = "Scanner",             SortOrder = 112 },
            new IssueTagDefinition { Code = "PairingIssue",          Label = "Pairing issue",            Severity = IssueSeverity.Warning,  ApplicableCategories = "Scanner",             SortOrder = 113, Description = "Won't stay connected to SAE device" },

            new IssueTagDefinition { Code = "BatteryWontHoldCharge", Label = "Won't hold charge",        Severity = IssueSeverity.Critical, ApplicableCategories = "Battery",             SortOrder = 120 },
            new IssueTagDefinition { Code = "BatterySwollen",        Label = "Swollen battery",          Severity = IssueSeverity.Critical, ApplicableCategories = "Battery",             SortOrder = 121, Description = "Physically swollen — remove from service immediately" },

            new IssueTagDefinition { Code = "LostAccessory",         Label = "Missing accessory",        Severity = IssueSeverity.Info,     ApplicableCategories = null,                  SortOrder = 200, Description = "Missing strap, case, or other accessory" },
            new IssueTagDefinition { Code = "NeedsClean",            Label = "Needs cleaning",           Severity = IssueSeverity.Info,     ApplicableCategories = null,                  SortOrder = 210 },
            new IssueTagDefinition { Code = "Other",                 Label = "Other issue",              Severity = IssueSeverity.Info,     ApplicableCategories = null,                  SortOrder = 999 },
        };

        db.IssueTagDefinitions.AddRange(definitions);
        await db.SaveChangesAsync(ct);
    }

}
