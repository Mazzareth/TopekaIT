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

    public static async Task SeedMasterAsync(MasterDbContext db, string topekaConnectionString, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        if (!await db.Users.AnyAsync(u => u.Username == SuperAdminUsername, ct))
        {
            var (hash, salt) = PasswordHasher.Hash(AdminPassword);
            db.Users.Add(new User
            {
                Id = SuperAdminId,
                Name = "Brad Williams",
                Username = SuperAdminUsername,
                Role = UserRole.SuperAdmin,
                Avatar = "BW",
                PasswordHash = hash,
                PasswordSalt = salt,
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
    }
}
