using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TopekaIT.Infrastructure.Data;

/// <summary>
/// Design-time factory for tenant migrations. EF tooling needs a sample division database to build migrations.
/// </summary>
public class TopekaDbContextFactory : IDesignTimeDbContextFactory<TopekaDbContext>
{
    public TopekaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TopekaDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=TopekaIT;Trusted_Connection=true;",
                sql => sql.CommandTimeout(120))
            .Options;

        return new TopekaDbContext(options, new EphemeralDataProtectionProvider());
    }
}
