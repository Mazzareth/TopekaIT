using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TopekaIT.Infrastructure.Data;

public class MasterDbContextFactory : IDesignTimeDbContextFactory<MasterDbContext>
{
    public MasterDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=TopekaIT_Master;Trusted_Connection=true;",
                sql => sql.CommandTimeout(120))
            .Options;

        return new MasterDbContext(options);
    }
}
