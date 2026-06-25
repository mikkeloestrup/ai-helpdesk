using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SupportSystem.Data;

/// <summary>
/// Design-time factory så `dotnet ef migrations` virker uden en kørende Aspire-host.
/// Connection string'en bruges kun til modelbygning ved `migrations add` — ikke til en live DB.
/// </summary>
public sealed class SupportDbContextFactory : IDesignTimeDbContextFactory<SupportDbContext>
{
    public SupportDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SupportDbContext>()
            .UseSqlServer("Server=localhost;Database=supportdb;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new SupportDbContext(options);
    }
}
