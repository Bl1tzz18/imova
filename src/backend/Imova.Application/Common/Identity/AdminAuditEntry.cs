namespace Imova.Application.Common.Identity;

// A permanent record of a site admin's sensitive action — who did it, to whom or to which agency, when,
// from where: granting the Admin role, verifying or unverifying an agency. Never updated or deleted by
// the application; ids only (no FK, so it survives an account's or an agency's deletion).
public sealed class AdminAuditEntry
{
    public const string GrantAdmin = "GrantAdmin";
    public const string VerifyAgency = "VerifyAgency";
    public const string UnverifyAgency = "UnverifyAgency";

    public Guid Id { get; set; }

    public required string Action { get; set; }

    public Guid ActorUserId { get; set; }

    // The account acted on (GrantAdmin); null for an action on an agency.
    public Guid? TargetUserId { get; set; }

    // The agency acted on (VerifyAgency / UnverifyAgency); null otherwise.
    public Guid? TargetAgencyId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    // The caller's IP as the API saw it (after the trusted-proxy X-Forwarded-For handling).
    public string? IpAddress { get; set; }
}
