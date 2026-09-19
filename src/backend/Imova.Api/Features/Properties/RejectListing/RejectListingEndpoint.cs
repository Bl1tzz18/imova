using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Properties.RejectListing;
using MediatR;

namespace Imova.Api.Features.Properties.RejectListing;

public static class RejectListingEndpoint
{
    public static void MapRejectListing(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/properties/{id:guid}/reject", async (
            Guid id,
            RejectListingRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new RejectListingCommand(id, user.IsInRole(Roles.Admin), request.Reason);
            var property = await sender.Send(command, cancellationToken);
            return property is null ? Results.NotFound() : Results.Ok(property);
        }).RequireAuthorization();
    }
}
