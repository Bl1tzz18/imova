using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Agencies.Invitations;
using Imova.Application.Features.Agencies.Members;
using Imova.Domain.Agencies;
using MediatR;

namespace Imova.Api.Features.Agencies;

public record ChangeMemberRoleRequest(AgencyRole Role);

public record InviteMemberRequest(string Email, AgencyRole Role);

// Members, the agency's invitations, and the invited person's side of an invitation.
public static class AgencyMemberEndpoints
{
    public static void MapAgencyMemberEndpoints(this IEndpointRouteBuilder app)
    {
        // --- Members ---

        app.MapGet("/api/v1/agencies/{id:guid}/members", async (Guid id, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            var members = await sender.Send(new GetAgencyMembersQuery(id, user.GetUserId(), user.IsInRole(Roles.Admin)), ct);
            return members is null ? Results.NotFound() : Results.Ok(members);
        }).RequireAuthorization();

        app.MapPatch("/api/v1/agencies/{id:guid}/members/{userId:guid}", async (
            Guid id, Guid userId, ChangeMemberRoleRequest request, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            await sender.Send(new ChangeMemberRoleCommand(id, user.GetUserId(), user.IsInRole(Roles.Admin), userId, request.Role), ct)
                ? Results.NoContent()
                : Results.NotFound()).RequireAuthorization();

        // Also how a member leaves (their own userId). ?reassignTo= picks who takes over their listings.
        app.MapDelete("/api/v1/agencies/{id:guid}/members/{userId:guid}", async (
            Guid id, Guid userId, Guid? reassignTo, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            await sender.Send(new RemoveMemberCommand(id, user.GetUserId(), user.IsInRole(Roles.Admin), userId, reassignTo), ct)
                ? Results.NoContent()
                : Results.NotFound()).RequireAuthorization();

        // --- The agency's invitations ---

        app.MapGet("/api/v1/agencies/{id:guid}/invitations", async (Guid id, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            var invitations = await sender.Send(new GetAgencyInvitationsQuery(id, user.GetUserId(), user.IsInRole(Roles.Admin)), ct);
            return invitations is null ? Results.NotFound() : Results.Ok(invitations);
        }).RequireAuthorization();

        app.MapPost("/api/v1/agencies/{id:guid}/invitations", async (
            Guid id, InviteMemberRequest request, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            var invitation = await sender.Send(
                new InviteMemberCommand(id, user.GetUserId(), user.IsInRole(Roles.Admin), request.Email, request.Role), ct);
            return invitation is null ? Results.NotFound() : Results.Ok(invitation);
        }).RequireAuthorization();

        app.MapPost("/api/v1/agencies/{id:guid}/invitations/{invitationId:guid}/resend", async (
            Guid id, Guid invitationId, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            var invitation = await sender.Send(
                new ResendAgencyInvitationCommand(id, user.GetUserId(), user.IsInRole(Roles.Admin), invitationId), ct);
            return invitation is null ? Results.NotFound() : Results.Ok(invitation);
        }).RequireAuthorization();

        app.MapDelete("/api/v1/agencies/{id:guid}/invitations/{invitationId:guid}", async (
            Guid id, Guid invitationId, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            await sender.Send(new RevokeAgencyInvitationCommand(id, user.GetUserId(), user.IsInRole(Roles.Admin), invitationId), ct)
                ? Results.NoContent()
                : Results.NotFound()).RequireAuthorization();

        // --- The invited person ---

        app.MapGet("/api/v1/invitations/{token}", async (string token, ISender sender, CancellationToken ct) =>
        {
            var invitation = await sender.Send(new GetInvitationByTokenQuery(token), ct);
            return invitation is null ? Results.NotFound() : Results.Ok(invitation);
        });

        app.MapPost("/api/v1/invitations/{token}/accept", async (string token, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            var invitation = await sender.Send(new AcceptInvitationCommand(token, null, user.GetUserId()), ct);
            return invitation is null ? Results.NotFound() : Results.Ok(invitation);
        }).RequireAuthorization();

        app.MapPost("/api/v1/invitations/{token}/decline", async (string token, ISender sender, CancellationToken ct) =>
            await sender.Send(new DeclineInvitationCommand(token, null, null), ct) ? Results.NoContent() : Results.NotFound());

        app.MapGet("/api/v1/users/me/invitations", async (ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetMyInvitationsQuery(user.GetUserId()), ct))).RequireAuthorization();

        app.MapPost("/api/v1/users/me/invitations/{invitationId:guid}/accept", async (
            Guid invitationId, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            var invitation = await sender.Send(new AcceptInvitationCommand(null, invitationId, user.GetUserId()), ct);
            return invitation is null ? Results.NotFound() : Results.Ok(invitation);
        }).RequireAuthorization();

        app.MapPost("/api/v1/users/me/invitations/{invitationId:guid}/decline", async (
            Guid invitationId, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            await sender.Send(new DeclineInvitationCommand(null, invitationId, user.GetUserId()), ct)
                ? Results.NoContent()
                : Results.NotFound()).RequireAuthorization();
    }
}
