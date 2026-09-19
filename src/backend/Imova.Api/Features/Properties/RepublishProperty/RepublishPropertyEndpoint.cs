using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Properties.RepublishProperty;
using MediatR;

namespace Imova.Api.Features.Properties.RepublishProperty;

public static class RepublishPropertyEndpoint
{
    public static void MapRepublishProperty(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/properties/{id:guid}/republish", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new RepublishPropertyCommand(id, user.GetUserId(), user.IsInRole(Roles.Admin));
            var property = await sender.Send(command, cancellationToken);
            return property is null ? Results.NotFound() : Results.Ok(property);
        }).RequireAuthorization();
    }
}
