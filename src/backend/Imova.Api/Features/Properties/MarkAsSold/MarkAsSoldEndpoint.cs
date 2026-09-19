using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Properties.MarkAsSold;
using MediatR;

namespace Imova.Api.Features.Properties.MarkAsSold;

public static class MarkAsSoldEndpoint
{
    public static void MapMarkAsSold(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/properties/{id:guid}/mark-as-sold", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new MarkAsSoldCommand(id, user.GetUserId(), user.IsInRole(Roles.Admin));
            var property = await sender.Send(command, cancellationToken);
            return property is null ? Results.NotFound() : Results.Ok(property);
        }).RequireAuthorization();
    }
}
