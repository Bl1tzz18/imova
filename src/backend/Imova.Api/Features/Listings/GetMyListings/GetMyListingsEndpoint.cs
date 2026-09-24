using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Listings.GetMyListings;
using MediatR;

namespace Imova.Api.Features.Listings.GetMyListings;

public static class GetMyListingsEndpoint
{
    public static void MapGetMyListings(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/users/me/listings", async (ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetMyListingsQuery(user.GetUserId()), cancellationToken)))
            .RequireAuthorization();
    }
}
