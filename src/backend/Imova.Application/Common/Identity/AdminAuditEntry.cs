namespace Imova.Application.Common.Identity;

// A permanent record of a change to who is an admin — who did it, to whom, when, from where. Never
// updated or deleted by the application; ids only (no FK, so it survives an account's deletion).
public sealed class AdminAuditEntry
{
    public const string GrantAdmin = "GrantAdmin";

    public Guid Id { get; set; }

    public required string Action { get; set; }

    public Guid ActorUserId { get; set; }

    public Guid TargetUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    // The caller's IP as the API saw it (after the trusted-proxy X-Forwarded-For handling).
    public string? IpAddress { get; set; }
}
