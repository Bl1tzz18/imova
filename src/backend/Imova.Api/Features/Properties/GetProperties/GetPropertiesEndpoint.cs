using Imova.Application.Features.Properties.GetProperties;
using Imova.Domain.Properties;
using MediatR;

namespace Imova.Api.Features.Properties.GetProperties;

public static class GetPropertiesEndpoint
{
    public static void MapGetProperties(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/properties", async (string? propertyType, ISender sender, CancellationToken cancellationToken) =>
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

            var properties = await sender.Send(new GetPropertiesQuery(parsedType), cancellationToken);
            return Results.Ok(properties);
        });
    }
}
