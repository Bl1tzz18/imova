using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Listings.GetListingById;
using MediatR;

namespace Imova.Api.Features.Listings.GetListingById;

public static class GetListingByIdEndpoint
{
    public static void MapGetListingById(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/listings/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var currentUserId = user.Identity?.IsAuthenticated == true ? user.GetUserId() : (Guid?)null;
            var isAdmin = user.Identity?.IsAuthenticated == true && user.IsInRole(Roles.Admin);
            var listing = await sender.Send(new GetListingByIdQuery(id, currentUserId, isAdmin), cancellationToken);
            return listing is null ? Results.NotFound() : Results.Ok(listing);
        });
    }
}
