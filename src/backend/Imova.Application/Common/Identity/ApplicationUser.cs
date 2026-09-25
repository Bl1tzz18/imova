using Microsoft.AspNetCore.Identity;

namespace Imova.Application.Common.Identity;

// The Identity-managed user row (AspNetUsers) — this is now the single source of truth for
// "who is this user", replacing the old standalone Imova.Domain.Users.User table. Lives in
// Application (not Domain) because it's an ASP.NET Core Identity type, not a domain concept —
// Domain stays free of framework dependencies; Infrastructure and Api both already depend on
// Application, so both can see it without breaking the Application → Infrastructure layering rule.
public sealed class ApplicationUser : IdentityUser<Guid>
{
    // Collected at registration (and, for Google sign-in, taken from the Google profile) —
    // Identity's base user has no name field of its own.
    public string? DisplayName { get; set; }

    // Always one of OUR OWN blob URLs (see IBlobStorageService.GetPublicUrl) — never an external
    // URL. Google's profile picture is downloaded and re-uploaded to our own storage on first
    // sign-in (see GoogleLoginHandler) rather than linked to directly, since Google's URL can be
    // unreliable/temporary and we don't want a third party able to change what renders here.
    public string? ProfilePictureUrl { get; set; }

    // Set by an admin (see SetMessagingBanHandler in Features/Messaging/Admin): the user can no longer send messages.
    public bool IsBannedFromMessaging { get; set; }
}
