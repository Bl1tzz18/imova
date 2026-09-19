using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Properties.ArchiveProperty;
using MediatR;

namespace Imova.Api.Features.Properties.ArchiveProperty;

public static class ArchivePropertyEndpoint
{
    public static void MapArchiveProperty(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/properties/{id:guid}/archive", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new ArchivePropertyCommand(id, user.GetUserId(), user.IsInRole(Roles.Admin));
            var property = await sender.Send(command, cancellationToken);
            return property is null ? Results.NotFound() : Results.Ok(property);
        }).RequireAuthorization();
    }
}
