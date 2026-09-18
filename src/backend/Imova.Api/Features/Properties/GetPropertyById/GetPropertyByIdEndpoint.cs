using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Properties.GetPropertyById;
using MediatR;

namespace Imova.Api.Features.Properties.GetPropertyById;

public static class GetPropertyByIdEndpoint
{
    public static void MapGetPropertyById(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/properties/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var currentUserId = user.Identity?.IsAuthenticated == true ? user.GetUserId() : (Guid?)null;
            var property = await sender.Send(new GetPropertyByIdQuery(id, currentUserId), cancellationToken);
            return property is null ? Results.NotFound() : Results.Ok(property);
        });
    }
}
