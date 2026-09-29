using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Account.ConfirmAccountDeletion;
using Imova.Application.Features.Account.DeleteAccount;
using Imova.Application.Features.Account.ExportPersonalData;
using Imova.Application.Features.Account.GetAccountDataSummary;
using Imova.Application.Features.Account.RequestAccountDeletionLink;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Imova.Api.Features.Account;

// The data-subject rights on the user's own account: see what's stored, download all of it, and
// delete the account (see AccountDeletion for what that removes).
public static class AccountEndpoints
{
    public record DeleteAccountRequest(string? Password);

    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/users/me/data-summary", async (ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetAccountDataSummaryQuery(user.GetUserId()), cancellationToken)))
            .RequireAuthorization();

        // The ZIP is assembled in a temporary file (deleted as soon as the response is sent) rather
        // than in memory: an account with many listings can hold hundreds of megabytes of photos.
        app.MapGet("/api/v1/users/me/data-export", async (
            ClaimsPrincipal user,
            ISender sender,
            IBlobStorageService blobStorageService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var export = await sender.Send(new ExportPersonalDataQuery(user.GetUserId()), cancellationToken);

            var file = new FileStream(
                Path.Combine(Path.GetTempPath(), $"imova-export-{Guid.NewGuid():N}.zip"),
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.None,
                bufferSize: 81920,
                FileOptions.DeleteOnClose);
            try
            {
                await PersonalDataArchive.WriteAsync(export, blobStorageService, file, cancellationToken);
                file.Position = 0;
            }
            catch
            {
                await file.DisposeAsync();
                throw;
            }

            httpContext.Response.Headers.CacheControl = "no-store";
            return Results.File(file, "application/zip", PersonalDataArchive.FileName(export.Data.GeneratedAt));
        })
            .RequireAuthorization()
            .RequireRateLimiting(AuthRateLimiting.AccountPolicy);

        app.MapDelete("/api/v1/users/me", async (
            [FromBody] DeleteAccountRequest request, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DeleteAccountCommand(user.GetUserId(), request.Password), cancellationToken);
            return Results.NoContent();
        })
            .RequireAuthorization()
            .RequireRateLimiting(AuthRateLimiting.AccountPolicy);

        app.MapPost("/api/v1/users/me/deletion-link", async (ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RequestAccountDeletionLinkCommand(user.GetUserId()), cancellationToken);
            return Results.NoContent();
        })
            .RequireAuthorization()
            .RequireRateLimiting(AuthRateLimiting.AccountPolicy);

        // From the emailed link — anonymous (the token is the proof), so limited per IP like the
        // other link endpoints.
        app.MapPost("/api/v1/auth/delete-account", async (
            ConfirmAccountDeletionCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(command, cancellationToken);
            return Results.NoContent();
        })
            .RequireRateLimiting(AuthRateLimiting.Policy);
    }
}
