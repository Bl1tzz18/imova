using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Publishers.CreateAgencyPublisher;
using Imova.Application.Features.Publishers.GetMyPublishers;
using MediatR;

namespace Imova.Api.Features.Publishers;

public record CreateAgencyPublisherRequest(string DisplayName, string Phone, string? Email, string? LogoUrl, string? Bio);

public static class PublisherEndpoints
{
    public static void MapPublisherEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/publishers/mine", async (ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetMyPublishersQuery(user.GetUserId()), cancellationToken)))
            .RequireAuthorization();

        app.MapPost("/api/v1/publishers/agency", async (
            CreateAgencyPublisherRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateAgencyPublisherCommand(
                user.GetUserId(), request.DisplayName, request.Phone, request.Email, request.LogoUrl, request.Bio);
            var publisher = await sender.Send(command, cancellationToken);
            return Results.Created($"/api/v1/publishers/{publisher.Id}", publisher);
        }).RequireAuthorization();
    }
}
