using MediatR;

namespace Imova.Api.Features.Properties.GetProperties;

public static class GetPropertiesEndpoint
{
    public static void MapGetProperties(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/properties", async (ISender sender, CancellationToken cancellationToken) =>
        {
            var properties = await sender.Send(new GetPropertiesQuery(), cancellationToken);
            return Results.Ok(properties);
        });
    }
}
