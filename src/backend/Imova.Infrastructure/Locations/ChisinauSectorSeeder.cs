using Imova.Domain.Locations;
using Microsoft.EntityFrameworkCore;

namespace Imova.Infrastructure.Locations;

// Informal real-estate-industry neighborhood names for Chișinău — not CUATM data, see
// ChisinauSector.cs. Hardcoded since there's no authoritative external source for these, unlike
// CuatmLocationSeeder. Called from Program.cs right after CuatmLocationSeeder.SeedAsync.
public static class ChisinauSectorSeeder
{
    private static readonly string[] Names =
    [
        "Aeroport",
        "Botanica",
        "Buiucani",
        "Centru",
        "Ciocana",
        "Poștă Veche",
        "Râșcani",
        "Sculeni",
        "Telecentru",
    ];

    public static async Task SeedAsync(ImovaDbContext dbContext, CancellationToken cancellationToken)
    {
        // Idempotency guard, same "seed once if empty" pattern as CuatmLocationSeeder.
        if (await dbContext.ChisinauSectors.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.ChisinauSectors.AddRange(Names.Select(ChisinauSector.Create));
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
