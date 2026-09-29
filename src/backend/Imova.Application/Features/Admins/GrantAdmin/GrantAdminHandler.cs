using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common.Validation;
using Imova.Application.Features.Auth.Login;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Imova.Application.Features.Admins.GrantAdmin;

// In order: the caller is an admin now (database, not token) → their own password, under the same
// lockout as sign-in → the target is an existing, email-confirmed account that isn't an admin yet →
// role granted and an audit entry written together → the new admin and every admin are emailed.
public class GrantAdminHandler(
    UserManager<ApplicationUser> userManager,
    IApplicationDbContext dbContext,
    AdminEmails emails,
    TimeProvider timeProvider,
    ILogger<GrantAdminHandler> logger) : IRequestHandler<GrantAdminCommand>
{
    public async Task Handle(GrantAdminCommand request, CancellationToken cancellationToken)
    {
        var actor = await AdminAccess.EnsureCurrentAdminAsync(userManager, request.ActorUserId);
        await EnsurePasswordAsync(actor, request.Password);

        var target = await userManager.FindByEmailAsync(request.Email.Trim());
        if (target is null)
        {
            throw Failure(nameof(GrantAdminCommand.Email), "There is no account with this email.", ErrorCodes.AdminUserNotFound);
        }

        // Only an address that's proven to belong to someone — never an account anyone could have
        // registered with a look-alike or someone else's email.
        if (!target.EmailConfirmed)
        {
            throw Failure(nameof(GrantAdminCommand.Email), "That account hasn't confirmed its email yet.", ErrorCodes.AdminEmailNotConfirmed);
        }

        if (await userManager.IsInRoleAsync(target, Roles.Admin))
        {
            throw Failure(nameof(GrantAdminCommand.Email), "That account is already an admin.", ErrorCodes.AlreadyAdmin);
        }

        // Written first and saved by AddToRoleAsync's own SaveChanges (same DbContext), so the
        // role never exists without its audit entry.
        dbContext.AdminAuditEntries.Add(new AdminAuditEntry
        {
            Id = Guid.NewGuid(),
            Action = AdminAuditEntry.GrantAdmin,
            ActorUserId = actor.Id,
            TargetUserId = target.Id,
            CreatedAt = timeProvider.GetUtcNow(),
            IpAddress = request.IpAddress,
        });
        var result = await userManager.AddToRoleAsync(target, Roles.Admin);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Could not grant the Admin role: " + string.Join("; ", result.Errors.Select(e => e.Code)));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogWarning("Admin role granted to account {TargetUserId} by admin {ActorUserId} from {IpAddress}.",
            target.Id, actor.Id, request.IpAddress);

        await NotifyAsync(actor, target, cancellationToken);
    }

    private async Task EnsurePasswordAsync(ApplicationUser actor, string? password)
    {
        if (!await userManager.HasPasswordAsync(actor))
        {
            throw Failure(nameof(GrantAdminCommand.Password),
                "Set a password on your account first — it's needed to confirm this.", ErrorCodes.AdminPasswordNeeded);
        }

        if (string.IsNullOrEmpty(password))
        {
            throw Failure(nameof(GrantAdminCommand.Password), "Enter your password to confirm.", ErrorCodes.AccountPasswordRequired);
        }

        if (await userManager.IsLockedOutAsync(actor))
        {
            throw new TooManyRequestsException(LoginHandler.LockedOut, ErrorCodes.LockedOut);
        }

        if (!await userManager.CheckPasswordAsync(actor, password))
        {
            await userManager.AccessFailedAsync(actor);
            if (await userManager.IsLockedOutAsync(actor))
            {
                throw new TooManyRequestsException(LoginHandler.LockedOut, ErrorCodes.LockedOut);
            }

            throw Failure(nameof(GrantAdminCommand.Password), "The password is incorrect.", ErrorCodes.AccountWrongPassword);
        }

        if (await userManager.GetAccessFailedCountAsync(actor) > 0)
        {
            await userManager.ResetAccessFailedCountAsync(actor);
        }
    }

    // The role is granted by now; a failed email is logged, never undoes it.
    private async Task NotifyAsync(ApplicationUser actor, ApplicationUser target, CancellationToken cancellationToken)
    {
        var grantedBy = actor.Email ?? actor.Id.ToString();
        var newAdmin = target.Email!;
        try
        {
            await emails.SendWelcomeAsync(newAdmin, grantedBy, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not email the new admin {TargetUserId}.", target.Id);
        }

        foreach (var admin in await userManager.GetUsersInRoleAsync(Roles.Admin))
        {
            if (admin.Id == target.Id || string.IsNullOrWhiteSpace(admin.Email))
            {
                continue;
            }

            try
            {
                await emails.SendNoticeAsync(admin.Email, newAdmin, grantedBy, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Could not tell admin {AdminUserId} about the new admin.", admin.Id);
            }
        }
    }

    private static ValidationException Failure(string field, string message, string code) =>
        new([CodedFailure.Of(field, message, code)]);
}
