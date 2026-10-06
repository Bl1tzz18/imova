using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Publishers.GetMyPublishers;
using MediatR;

namespace Imova.Api.Features.Publishers;

public static class PublisherEndpoints
{
    public static void MapPublisherEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/publishers/mine", async (ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetMyPublishersQuery(user.GetUserId()), cancellationToken)))
            .RequireAuthorization();
    }
}
