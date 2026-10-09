using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Agencies;
using Imova.Application.Features.Agencies.Admin;
using Imova.Application.Features.Agencies.CreateAgency;
using Imova.Application.Features.Agencies.Lifecycle;
using Imova.Application.Features.Agencies.Directory;
using Imova.Application.Features.Agencies.GetAgency;
using Imova.Application.Features.Agencies.Listings;
using Imova.Application.Features.Agencies.Logos;
using Imova.Application.Features.Agencies.UpdateAgency;
using Imova.Contracts.Agencies;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Imova.Api.Features.Agencies;

public record AgencyProfileRequest(
    string Name,
    string Phone,
    string? Email,
    string? Bio,
    string? Website,
    string? Address,
    Guid? RaionId);

public record DeleteAgencyRequest(string? ConfirmName);

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

        // The public directory: active agencies, verified first. pageSize at most 50.
        app.MapGet("/api/v1/agencies", async (
            string? q,
            Guid? raionId,
            bool? verified,
            int? page,
            int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(
                new GetAgencyDirectoryQuery(
                    q,
                    raionId,
                    verified == true,
                    page ?? 1,
                    pageSize ?? GetAgencyDirectoryQuery.DefaultPageSize),
                cancellationToken)));

        // The public page's agency. A former slug (the agency was renamed) answers 301 with the current
        // slug — in Location and in the body, for a caller that doesn't follow redirects.
        app.MapGet("/api/v1/agencies/by-slug/{slug}", async (
            string slug,
            HttpContext httpContext,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var signedIn = user.Identity?.IsAuthenticated == true;
            var result = await sender.Send(
                new GetAgencyBySlugQuery(slug, signedIn ? user.GetUserId() : null, signedIn && user.IsInRole(Roles.Admin)),
                cancellationToken);

            if (result?.CurrentSlug is { } currentSlug)
            {
                httpContext.Response.Headers.Location = $"/api/v1/agencies/by-slug/{Uri.EscapeDataString(currentSlug)}";
                return Results.Json(new AgencySlugRedirectDto(currentSlug), statusCode: StatusCodes.Status301MovedPermanently);
            }

            return result?.Agency is { } agency ? Results.Ok(agency) : Results.NotFound();
        });

        // The agency's full phone number (the page only has its shape) — rate-limited per IP together
        // with listings' numbers.
        app.MapPost("/api/v1/agencies/{id:guid}/contact/phone", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var signedIn = user.Identity?.IsAuthenticated == true;
            var phone = await sender.Send(
                new RevealAgencyPhoneQuery(id, signedIn ? user.GetUserId() : null, signedIn && user.IsInRole(Roles.Admin)),
                cancellationToken);
            return phone is null ? Results.NotFound() : Results.Ok(phone);
        }).RequireRateLimiting(ListingVisitorRateLimiting.PhoneRevealPolicy);

        // Its listings in every status, for the management page — members (an Agent: their own) and
        // admins. One page of one tab: ?group=active|unpublished|ended&q=&sort=&page=&pageSize= (≤ 60).
        app.MapGet("/api/v1/agencies/{id:guid}/listings", async (
            Guid id,
            string? group,
            string? q,
            string? sort,
            int? page,
            int? pageSize,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var listings = await sender.Send(
                new GetAgencyListingsQuery(
                    id,
                    user.GetUserId(),
                    user.IsInRole(Roles.Admin),
                    group,
                    q,
                    sort,
                    page ?? 1,
                    pageSize ?? GetAgencyListingsQuery.DefaultPageSize),
                ct);
            return listings is null ? Results.NotFound() : Results.Ok(listings);
        }).RequireAuthorization();

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

        // Hide the agency and all its listings from the public, or show them again — Owners/Admins.
        app.MapPost("/api/v1/agencies/{id:guid}/deactivate", (Guid id, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            SetActiveAsync(id, active: false, user, sender, ct)).RequireAuthorization();

        app.MapPost("/api/v1/agencies/{id:guid}/reactivate", (Guid id, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            SetActiveAsync(id, active: true, user, sender, ct)).RequireAuthorization();

        // Delete for good — an Owner or a site admin, with the agency's exact name; its listings become
        // their authors' private listings.
        app.MapDelete("/api/v1/agencies/{id:guid}", async (
            Guid id,
            [FromBody] DeleteAgencyRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
            await sender.Send(new DeleteAgencyCommand(id, user.GetUserId(), user.IsInRole(Roles.Admin), request.ConfirmName), ct)
                ? Results.NoContent()
                : Results.NotFound()).RequireAuthorization();

        // --- Site admins ---

        var admin = app.MapGroup("/api/v1/admin/agencies").RequireAdmin();

        admin.MapGet("", async (string? q, bool? verified, int? page, int? pageSize, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(
                new GetAdminAgenciesQuery(q, verified, page ?? 1, pageSize ?? GetAdminAgenciesQuery.DefaultPageSize), ct)));

        admin.MapPost("/{id:guid}/verify", (Guid id, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            SetVerifiedAsync(id, verified: true, user, sender, ct));

        admin.MapPost("/{id:guid}/unverify", (Guid id, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            SetVerifiedAsync(id, verified: false, user, sender, ct));

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

    private static async Task<IResult> SetActiveAsync(Guid id, bool active, ClaimsPrincipal user, ISender sender, CancellationToken ct)
    {
        var agency = await sender.Send(new SetAgencyActiveCommand(id, user.GetUserId(), user.IsInRole(Roles.Admin), active), ct);
        return agency is null ? Results.NotFound() : Results.Ok(agency);
    }

    private static async Task<IResult> SetVerifiedAsync(Guid id, bool verified, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        await sender.Send(new SetAgencyVerifiedCommand(id, user.GetUserId(), verified), ct) ? Results.NoContent() : Results.NotFound();
}
