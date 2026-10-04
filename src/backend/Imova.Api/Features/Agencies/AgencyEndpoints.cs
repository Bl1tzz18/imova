using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Agencies;
using Imova.Application.Features.Agencies.CreateAgency;
using Imova.Application.Features.Agencies.GetAgency;
using Imova.Application.Features.Agencies.Logos;
using Imova.Application.Features.Agencies.UpdateAgency;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Imova.Api.Features.Agencies;

public record AgencyProfileRequest(
    string Name,
    string Phone,
    string? Email,
    string? Bio,
    string? Website,
    string? Address,
    Guid? RaionId);

public static class AgencyEndpoints
{
    public static void MapAgencyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/agencies", async (
            AgencyProfileRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var agency = await sender.Send(
                new CreateAgencyCommand(
                    user.GetUserId(), request.Name, request.Phone, request.Email, request.Bio, request.Website, request.Address, request.RaionId),
                cancellationToken);
            return Results.Created($"/api/v1/agencies/{agency.Id}", agency);
        }).RequireAuthorization();

        app.MapGet("/api/v1/users/me/agencies", async (ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetMyAgenciesQuery(user.GetUserId()), cancellationToken)))
            .RequireAuthorization();

        app.MapGet("/api/v1/agencies/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var signedIn = user.Identity?.IsAuthenticated == true;
            var agency = await sender.Send(
                new GetAgencyQuery(id, signedIn ? user.GetUserId() : null, signedIn && user.IsInRole(Roles.Admin)),
                cancellationToken);
            return agency is null ? Results.NotFound() : Results.Ok(agency);
        });

        app.MapPut("/api/v1/agencies/{id:guid}", async (
            Guid id,
            AgencyProfileRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var agency = await sender.Send(
                new UpdateAgencyCommand(
                    id,
                    user.GetUserId(),
                    user.IsInRole(Roles.Admin),
                    request.Name,
                    request.Phone,
                    request.Email,
                    request.Bio,
                    request.Website,
                    request.Address,
                    request.RaionId),
                cancellationToken);
            return agency is null ? Results.NotFound() : Results.Ok(agency);
        }).RequireAuthorization();

        app.MapPost("/api/v1/agencies/{id:guid}/logo", async (
            Guid id,
            IFormFile file,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            await using var stream = file.OpenReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);

            var agency = await sender.Send(
                new UploadAgencyLogoCommand(id, user.GetUserId(), user.IsInRole(Roles.Admin), buffer.ToArray()), cancellationToken);
            return agency is null ? Results.NotFound() : Results.Ok(agency);
        }).RequireAuthorization().DisableAntiforgery();

        app.MapDelete("/api/v1/agencies/{id:guid}/logo", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var agency = await sender.Send(new RemoveAgencyLogoCommand(id, user.GetUserId(), user.IsInRole(Roles.Admin)), cancellationToken);
            return agency is null ? Results.NotFound() : Results.Ok(agency);
        }).RequireAuthorization();
    }
}
