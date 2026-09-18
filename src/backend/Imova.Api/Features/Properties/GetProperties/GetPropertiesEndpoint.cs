using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Properties.GetProperties;
using Imova.Domain.Properties;
using MediatR;

namespace Imova.Api.Features.Properties.GetProperties;

public static class GetPropertiesEndpoint
{
    public static void MapGetProperties(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/properties", async (
            string? propertyType,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            PropertyType? parsedType = null;
            if (!string.IsNullOrEmpty(propertyType))
            {
                if (!Enum.TryParse<PropertyType>(propertyType, ignoreCase: true, out var type))
                {
                    return Results.BadRequest($"Unknown property type '{propertyType}'.");
                }

                parsedType = type;
            }

            // Public endpoint (no RequireAuthorization) — the current user id is only used, when
            // present, to mark which listings they've already saved (see PropertyDto.IsSaved).
            var currentUserId = user.Identity?.IsAuthenticated == true ? user.GetUserId() : (Guid?)null;

            var properties = await sender.Send(new GetPropertiesQuery(parsedType, currentUserId), cancellationToken);
            return Results.Ok(properties);
        });
    }
}
