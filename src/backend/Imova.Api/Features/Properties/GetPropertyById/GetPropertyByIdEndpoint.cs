using Imova.Application.Features.Properties.GetPropertyById;
using MediatR;

namespace Imova.Api.Features.Properties.GetPropertyById;

public static class GetPropertyByIdEndpoint
{
    public static void MapGetPropertyById(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/properties/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            var property = await sender.Send(new GetPropertyByIdQuery(id), cancellationToken);
            return property is null ? Results.NotFound() : Results.Ok(property);
        });
    }
}
