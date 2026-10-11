using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Locations.SearchLocations;
using Imova.Domain.Agencies;
using Imova.Domain.Locations;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Agencies.Import;

// Where a row's listing is: the raion (city column), and a locality in it or one of Chișinău's
// sectors — with the names as stored, for the address and the geocoder.
public sealed record ImportLocation(
    Guid RaionId,
    string RaionName,
    Guid? LocalitateId,
    string? LocalitateName,
    Guid? ChisinauSectorId,
    string? ChisinauSectorName);

// The reference tables, loaded once per import and matched by name the way the location search
// does (LocationSearchText: case and diacritics don't matter), also ignoring the administrative
// prefixes people write ("mun. Chișinău", "or. Ialoveni", "s. Codru", "r-nul Orhei").
public sealed class ImportLocationIndex
{
    private static readonly string[] Prefixes =
    [
        "municipiul ", "mun. ", "mun ", "orasul ", "oras ", "or. ", "or ", "raionul ", "r-nul ", "r. ",
        "comuna ", "com. ", "satul ", "sat ", "s. ", "sectorul ", "sect. ", "sec. ",
    ];

    private readonly Dictionary<string, Raion> _raioaneByName;
    private readonly ILookup<(Guid RaionId, string Name), Localitate> _localitati;
    private readonly Dictionary<string, ChisinauSector> _sectorsByName;

    private ImportLocationIndex(IEnumerable<Raion> raioane, IEnumerable<Localitate> localitati, IEnumerable<ChisinauSector> sectors)
    {
        _raioaneByName = raioane.GroupBy(r => Key(r.NameRo)).ToDictionary(g => g.Key, g => g.First());
        _localitati = localitati.ToLookup(l => (l.RaionId, Key(l.NameRo)));
        _sectorsByName = sectors.GroupBy(s => Key(s.Name)).ToDictionary(g => g.Key, g => g.First());
    }

    public static async Task<ImportLocationIndex> LoadAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        new(
            await dbContext.Raioane.AsNoTracking().ToListAsync(cancellationToken),
            await dbContext.Localitati.AsNoTracking().ToListAsync(cancellationToken),
            await dbContext.ChisinauSectors.AsNoTracking().ToListAsync(cancellationToken));

    public static ImportLocationIndex From(IEnumerable<Raion> raioane, IEnumerable<Localitate> localitati, IEnumerable<ChisinauSector> sectors) =>
        new(raioane, localitati, sectors);

    public (ImportLocation? Location, IReadOnlyList<ListingImportProblem> Problems) Resolve(string city, string locality, string sector)
    {
        var problems = new List<ListingImportProblem>();
        if (city.Length == 0)
        {
            return (null, [ImportProblems.Error(ImportColumns.City, ErrorCodes.ImportRequired, "The city is required.")]);
        }

        if (!_raioaneByName.TryGetValue(Key(city), out var raion))
        {
            return (null, [ImportProblems.Error(ImportColumns.City, ErrorCodes.ImportUnknownCity, $"Unknown city '{city}'.", ("value", city))]);
        }

        if (locality.Length > 0 && sector.Length > 0)
        {
            problems.Add(ImportProblems.Error(ImportColumns.Sector, ErrorCodes.ImportLocalityAndSector, "Give a locality or a sector, not both."));
        }

        Localitate? localitate = null;
        if (locality.Length > 0)
        {
            // Two places of the same name in one raion: the town/commune itself (no parent) wins over
            // a village of it; otherwise it can't be told apart.
            var matches = _localitati[(raion.Id, Key(locality))].ToList();
            var topLevel = matches.Where(l => l.ParentLocalityId is null).ToList();
            localitate = matches.Count == 1 ? matches[0] : topLevel.Count == 1 ? topLevel[0] : null;
            if (matches.Count == 0)
            {
                problems.Add(ImportProblems.Error(
                    ImportColumns.Locality, ErrorCodes.ImportUnknownLocality, $"No locality '{locality}' in {raion.NameRo}.", ("value", locality), ("city", raion.NameRo)));
            }
            else if (localitate is null)
            {
                problems.Add(ImportProblems.Error(
                    ImportColumns.Locality, ErrorCodes.ImportAmbiguousLocality, $"Several localities '{locality}' in {raion.NameRo}.", ("value", locality), ("city", raion.NameRo)));
            }
        }

        ChisinauSector? chisinauSector = null;
        if (sector.Length > 0)
        {
            if (raion.LocalityLabel != LocalityLabel.Sector)
            {
                problems.Add(ImportProblems.Error(ImportColumns.Sector, ErrorCodes.ImportSectorOutsideChisinau, "Sectors are Chișinău's only."));
            }
            else if (!_sectorsByName.TryGetValue(Key(sector), out chisinauSector))
            {
                problems.Add(ImportProblems.Error(ImportColumns.Sector, ErrorCodes.ImportUnknownSector, $"Unknown sector '{sector}'.", ("value", sector)));
            }
        }

        return problems.Count > 0
            ? (null, problems)
            : (new ImportLocation(raion.Id, raion.NameRo, localitate?.Id, localitate?.NameRo, chisinauSector?.Id, chisinauSector?.Name), problems);
    }

    public static string Key(string name)
    {
        var text = LocationSearchText.Normalize(name);
        foreach (var prefix in Prefixes)
        {
            var normalizedPrefix = LocationSearchText.Normalize(prefix);
            if (text.StartsWith(normalizedPrefix, StringComparison.Ordinal))
            {
                text = text[normalizedPrefix.Length..];
                break;
            }
        }

        return string.Join(' ', text.Replace('-', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}

// Building an import problem with its params in one line.
public static class ImportProblems
{
    public static ListingImportProblem Error(string? column, string code, string message, params (string Key, string Value)[] parameters) =>
        new(ListingImportProblemSeverity.Error, column, code, message, Params(parameters));

    public static ListingImportProblem Warning(string? column, string code, string message, params (string Key, string Value)[] parameters) =>
        new(ListingImportProblemSeverity.Warning, column, code, message, Params(parameters));

    private static IReadOnlyDictionary<string, string>? Params((string Key, string Value)[] parameters) =>
        parameters.Length == 0 ? null : parameters.ToDictionary(p => p.Key, p => p.Value);
}
