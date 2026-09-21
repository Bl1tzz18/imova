using Imova.Application.Features.Locations.GetStreetSuggestions;
using MediatR;

namespace Imova.Api.Features.Locations.GetStreetSuggestions;

public static class GetStreetSuggestionsEndpoint
{
    public static void MapGetStreetSuggestions(this IEndpointRouteBuilder app)
    {
        // Public (no RequireAuthorization) — same treatment as the other location lookup
        // endpoints. No browser Cache-Control header (unlike Raioane/Localitati/ChisinauSectors):
        // the query varies per keystroke, so there's nothing a browser could usefully cache
        // client-side. GetStreetSuggestionsHandler does apply its own short-lived server-side
        // IMemoryCache keyed on the (query, raionId, localitateId) tuple, on top of
        // IStreetSuggestionService's own upstream throttling.
        app.MapGet("/api/v1/locations/street-suggestions", async (
            string? query,
            Guid? raionId,
            Guid? localitateId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var suggestions = await sender.Send(
                new GetStreetSuggestionsQuery(query ?? string.Empty, raionId, localitateId), cancellationToken);
            return Results.Ok(suggestions);
        });
    }
}
