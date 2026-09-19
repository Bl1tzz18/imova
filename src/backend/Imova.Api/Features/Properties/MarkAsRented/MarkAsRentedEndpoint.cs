using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Properties.MarkAsRented;
using MediatR;

namespace Imova.Api.Features.Properties.MarkAsRented;

public static class MarkAsRentedEndpoint
{
    public static void MapMarkAsRented(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/properties/{id:guid}/mark-as-rented", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new MarkAsRentedCommand(id, user.GetUserId(), user.IsInRole(Roles.Admin));
            var property = await sender.Send(command, cancellationToken);
            return property is null ? Results.NotFound() : Results.Ok(property);
        }).RequireAuthorization();
    }
}
