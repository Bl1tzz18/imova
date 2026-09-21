using System.Reflection;
using System.Text.Json;
using Imova.Domain.Locations;
using Microsoft.EntityFrameworkCore;

namespace Imova.Infrastructure.Locations;

// Seeds Raioane/Localitati once, from Moldova's official CUATM administrative-division
// classifier, embedded as a trimmed JSON snapshot (id/code/nameRo/nameRu/parentId only) fetched
// from mconnect.gov.md's public term catalog (no auth required) — see
// Locations/SeedData/cuatm-locations.json. Called from Program.cs right after
// dbContext.Database.Migrate(), alongside the existing Identity-role seeding.
//
// Every CUATM record whose parentId is null becomes exactly one Raion (37 of them: 3 municipii,
// 32 raioane, UTA Găgăuzia, and the Transnistria left-bank unit). Every other record — at any
// depth below a root — becomes a Localitate flattened directly under that root's RaionId
// (ParentLocalityId still records the true immediate CUATM parent, for data fidelity only). This
// is what lets Chișinău's dropdown show its 5 real sectors *and* the suburban towns/communes
// administratively part of it side by side in one flat list, per the feature's requirement,
// instead of forcing a deeper drill-down UI.
public static class CuatmLocationSeeder
{
    private const string ChisinauNameRo = "Chișinău";

    public static async Task SeedAsync(ImovaDbContext dbContext, CancellationToken cancellationToken)
    {
        // Idempotency guard — this is "seed once if empty," matching the existing Identity-role
        // seeding pattern right above this call in Program.cs. Refreshing CUATM data later is a
        // deliberate out-of-scope follow-up (swap the embedded JSON + add a re-seed path), not
        // something this seeder detects on its own.
        if (await dbContext.Raioane.AnyAsync(cancellationToken))
        {
            return;
        }

        var records = LoadRecords();

        var roots = records.Where(r => r.ParentId is null).ToList();
        var byParentId = records
            .Where(r => r.ParentId is not null)
            .ToLookup(r => r.ParentId!.Value);

        var raionByRecordId = new Dictionary<Guid, Raion>();
        foreach (var root in roots)
        {
            var localityLabel = root.NameRo == ChisinauNameRo ? LocalityLabel.Sector : LocalityLabel.Localitate;
            var raion = Raion.Create(root.Id, root.Code, root.NameRo, root.NameRu, localityLabel);
            raionByRecordId[root.Id] = raion;
        }

        dbContext.Raioane.AddRange(raionByRecordId.Values);

        // Breadth-first over each root's subtree, flattening every descendant (any depth) into
        // that root's RaionId, while still recording the true immediate-parent chain via
        // ParentLocalityId (null when the immediate CUATM parent is the root itself).
        var localitateByRecordId = new Dictionary<Guid, Localitate>();
        foreach (var root in roots)
        {
            var raion = raionByRecordId[root.Id];
            var queue = new Queue<(CuatmRecord Record, Guid? ParentLocalityRecordId)>();
            foreach (var child in byParentId[root.Id])
            {
                queue.Enqueue((child, null));
            }

            while (queue.Count > 0)
            {
                var (record, parentLocalityRecordId) = queue.Dequeue();
                var parentLocalityId = parentLocalityRecordId is null
                    ? (Guid?)null
                    : localitateByRecordId[parentLocalityRecordId.Value].Id;

                var localitate = Localitate.Create(record.Id, raion.Id, parentLocalityId, record.Code, record.NameRo, record.NameRu);
                localitateByRecordId[record.Id] = localitate;

                foreach (var grandchild in byParentId[record.Id])
                {
                    queue.Enqueue((grandchild, record.Id));
                }
            }
        }

        dbContext.Localitati.AddRange(localitateByRecordId.Values);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<CuatmRecord> LoadRecords()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith("cuatm-locations.json", StringComparison.Ordinal));

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");

        // The embedded JSON's keys are camelCase ("code", "nameRo", "parentId") — case-insensitive
        // matching is required since JsonSerializer defaults to case-sensitive property binding.
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<List<CuatmRecord>>(stream, options)
            ?? throw new InvalidOperationException("CUATM seed data deserialized to null.");
    }

    private sealed record CuatmRecord(Guid Id, string Code, string NameRo, string? NameRu, Guid? ParentId);
}
