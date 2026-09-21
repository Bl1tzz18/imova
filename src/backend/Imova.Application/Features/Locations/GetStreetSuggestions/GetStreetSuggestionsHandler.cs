using Imova.Application.Common.Interfaces;
using Imova.Contracts.Locations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Imova.Application.Features.Locations.GetStreetSuggestions;

public class GetStreetSuggestionsHandler(IApplicationDbContext dbContext, IStreetSuggestionService streetSuggestionService, IMemoryCache cache)
    : IRequestHandler<GetStreetSuggestionsQuery, List<StreetSuggestionDto>>
{
    // Short-lived, unlike the indefinite cache on Raioane/Localitati/ChisinauSectors — those are
    // seeded-once reference data, this is live third-party search results that should refresh
    // reasonably soon, but a 60s window is still long enough to absorb typical backspace-then-
    // retype churn and repeated identical queries across users without ever going stale for long.
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    public async Task<List<StreetSuggestionDto>> Handle(GetStreetSuggestionsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = BuildCacheKey(request);
        if (cache.TryGetValue(cacheKey, out List<StreetSuggestionDto>? cached))
        {
            return cached!;
        }

        var locality = await ResolveLocalityBiasAsync(request, cancellationToken);
        var suggestions = await streetSuggestionService.SuggestStreetsAsync(request.Query, locality, cancellationToken);
        var result = suggestions.Select(s => new StreetSuggestionDto(s.Name, s.Latitude, s.Longitude)).ToList();

        // Cached even when empty — a query that legitimately matches nothing shouldn't keep
        // re-hitting Photon every time the user pauses on it within the window either.
        cache.Set(cacheKey, result, CacheDuration);
        return result;
    }

    // Trimmed/lowercased so "Ismail", " ismail ", and "ISMAIL" all hit the same cache entry —
    // maximizes hits for the backspace-then-retype and multi-user-same-street cases this cache
    // exists for, without changing the casing actually sent to Photon (SuggestStreetsAsync still
    // gets request.Query verbatim).
    private static string BuildCacheKey(GetStreetSuggestionsQuery request) =>
        $"street-suggestions:{request.Query.Trim().ToLowerInvariant()}:{request.RaionId}:{request.LocalitateId}";

    // A bare Raion selection (no Localitate picked yet) must still narrow the Photon search —
    // otherwise it silently falls through to an unbiased national search, and Photon returns
    // whatever's nationally most prominent for that street name (e.g. "Ștefan cel Mare" resolving
    // to Chișinău's boulevard even when the caller is in Soroca, ~150km away). This heuristic is
    // purely a search-bias input: it's never reflected in the visible Localitate dropdown, never
    // fed to IGeocodingService/Nominatim, and never saved to PropertyLocation — LocalitateId stays
    // exactly what the caller passed (typically null here).
    private async Task<string?> ResolveLocalityBiasAsync(GetStreetSuggestionsQuery request, CancellationToken cancellationToken)
    {
        // An unknown/stale LocalitateId just means no locality-text bias, same as not supplying
        // one at all — this endpoint is a UX aid, not something that should 404 on a bad id.
        if (request.LocalitateId.HasValue)
        {
            return await dbContext.Localitati
                .AsNoTracking()
                .Where(l => l.Id == request.LocalitateId.Value)
                .Select(l => l.NameRo)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (!request.RaionId.HasValue)
        {
            return null;
        }

        var raionName = await dbContext.Raioane
            .AsNoTracking()
            .Where(r => r.Id == request.RaionId.Value)
            .Select(r => r.NameRo)
            .FirstOrDefaultAsync(cancellationToken);
        if (raionName is null)
        {
            return null;
        }

        // Common in Moldova for a Raion's main town to share its exact name (Soroca, Orhei,
        // Ungheni, Cahul, ...) — when one exists, prefer biasing toward that specific town over
        // the Raion name alone. Municipii like Bălți/Bender have no child Localitate at all (the
        // municipiu itself IS the town), in which case this simply finds nothing and falls back
        // to the Raion name below — still narrower than no bias at all.
        var sameNamedLocalitateName = await dbContext.Localitati
            .AsNoTracking()
            .Where(l => l.RaionId == request.RaionId.Value && l.NameRo == raionName)
            .Select(l => l.NameRo)
            .FirstOrDefaultAsync(cancellationToken);

        return sameNamedLocalitateName ?? raionName;
    }
}
