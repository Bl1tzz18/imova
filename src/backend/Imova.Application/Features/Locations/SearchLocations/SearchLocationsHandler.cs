using Imova.Application.Common.Interfaces;
using Imova.Contracts.Locations;
using Imova.Domain.Locations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Imova.Application.Features.Locations.SearchLocations;

public class SearchLocationsHandler(IApplicationDbContext dbContext, IMemoryCache cache)
    : IRequestHandler<SearchLocationsQuery, List<LocationSuggestionDto>>
{
    private const string CacheKey = "locations:search-index";
    private static readonly string[] KindOrder = ["Raion", "Sector", "Localitate"];

    private sealed record Entry(LocationSuggestionDto Suggestion, string Normalized);

    public async Task<List<LocationSuggestionDto>> Handle(SearchLocationsQuery request, CancellationToken cancellationToken)
    {
        var needle = LocationSearchText.Normalize(request.Text);
        if (needle.Length < 2)
        {
            return [];
        }

        var index = await IndexAsync(cancellationToken);
        return index
            .Where(e => e.Normalized.Contains(needle))
            // Names starting with the text first, then raioane before sectors before localitati.
            .OrderBy(e => e.Normalized.StartsWith(needle) ? 0 : 1)
            .ThenBy(e => Array.IndexOf(KindOrder, e.Suggestion.Kind))
            .ThenBy(e => e.Suggestion.Name, StringComparer.Ordinal)
            .Take(Math.Clamp(request.Limit, 1, 20))
            .Select(e => e.Suggestion)
            .ToList();
    }

    // Every raion, localitate and sector (~1 700 names) — reference data, cached for the process
    // lifetime like GetRaioaneHandler's.
    private async Task<List<Entry>> IndexAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKey, out List<Entry>? cached))
        {
            return cached!;
        }

        var raioane = await dbContext.Raioane.AsNoTracking().ToListAsync(cancellationToken);
        var raionNames = raioane.ToDictionary(r => r.Id, r => r.NameRo);
        var localitati = await dbContext.Localitati.AsNoTracking().ToListAsync(cancellationToken);
        var sectors = await dbContext.ChisinauSectors.AsNoTracking().ToListAsync(cancellationToken);
        var chisinau = raioane.FirstOrDefault(r => r.LocalityLabel == LocalityLabel.Sector);

        var entries = raioane
            .Select(r => new LocationSuggestionDto("Raion", r.Id, r.NameRo, r.Id, r.NameRo))
            .Concat(localitati.Select(l => new LocationSuggestionDto(
                "Localitate", l.Id, l.NameRo, l.RaionId, raionNames.GetValueOrDefault(l.RaionId) ?? string.Empty)))
            .Concat(chisinau is null
                ? []
                : sectors.Select(s => new LocationSuggestionDto("Sector", s.Id, s.Name, chisinau.Id, chisinau.NameRo)))
            .Select(s => new Entry(s, LocationSearchText.Normalize(s.Name)))
            .ToList();

        cache.Set(CacheKey, entries);
        return entries;
    }
}
