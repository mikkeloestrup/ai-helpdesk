using Microsoft.EntityFrameworkCore;
using SupportSystem.Domain;

namespace SupportSystem.Data;

/// <summary>Seeder: 4 kategorier (matcher AiCategory) og 3 agenter. Idempotent.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(SupportDbContext db, CancellationToken ct = default)
    {
        if (!await db.Categories.AnyAsync(ct))
        {
            db.Categories.AddRange(
                new Category { Name = "Fakturering", SortOrder = 1 },
                new Category { Name = "TekniskFejl", SortOrder = 2 },
                new Category { Name = "Konto", SortOrder = 3 },
                new Category { Name = "Generelt", SortOrder = 4 });
        }

        if (!await db.Agents.AnyAsync(ct))
        {
            db.Agents.AddRange(
                new Agent { Name = "Søren Sørensen", Email = "soren@numax.dk", Team = "Support" },
                new Agent { Name = "Mette Madsen", Email = "mette@numax.dk", Team = "Support" },
                new Agent { Name = "Anders And", Email = "anders@numax.dk", Team = "Teknik" });
        }

        await db.SaveChangesAsync(ct);
    }
}
