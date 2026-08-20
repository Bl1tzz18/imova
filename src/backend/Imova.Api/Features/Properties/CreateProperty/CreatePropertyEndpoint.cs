using Imova.Application.Features.Properties.CreateProperty;
using MediatR;

namespace Imova.Api.Features.Properties.CreateProperty;

public static class CreatePropertyEndpoint
{
    public static void MapCreateProperty(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/properties", async (CreatePropertyCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            var property = await sender.Send(command, cancellationToken);
            return Results.Created($"/api/v1/properties/{property.Id}", property);
        });
    }
}
