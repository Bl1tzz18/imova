using Imova.Application.Features.Locations.GetStreetSuggestions;
using MediatR;

namespace Imova.Api.Features.Locations.GetStreetSuggestions;

public static class GetStreetSuggestionsEndpoint
{
    public static void MapGetStreetSuggestions(this IEndpointRouteBuilder app)
    {
        // Public (no RequireAuthorization) — same treatment as the other location lookup
        // endpoints. Not cached (unlike Raioane/Localitati/ChisinauSectors): the query varies per
        // keystroke, so there's nothing stable to key a cache on, and IStreetSuggestionService
        // already throttles its own upstream calls.
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
