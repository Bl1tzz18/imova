using Imova.Domain.Common;

namespace Imova.Domain.Agencies;

// A real-estate agency with a public profile (/agencies/{slug}) and members (AgencyMember) who
// publish listings under it. A listing published under an agency still has a person as its author
// (Listing.PublisherId → that member's own Publisher); Listing.AgencyId says which agency it's
// published under, and the author must always be a current member.
//
// Phone and Email are required, like they were for the agency publishers this replaces: they are
// the agency's contact details on its page and the default contact for its listings.
public sealed class Agency : AggregateRoot
{
    public const int MaxNameLength = 120;
    public const int MaxBioLength = 2000;
    public const int MaxPhoneLength = 20;
    public const int MaxEmailLength = 254;
    public const int MaxWebsiteLength = 254;
    public const int MaxAddressLength = 250;

    private readonly List<AgencyMember> _members = [];

    // For EF Core materialization only.
    private Agency()
        : base(Guid.Empty)
    {
        Name = null!;
        Slug = null!;
        Phone = null!;
        Email = null!;
    }

    private Agency(Guid id, AgencyProfile profile, string slug, Guid ownerUserId, DateTimeOffset now)
        : base(id)
    {
        Name = null!;
        Slug = slug;
        Phone = null!;
        Email = null!;
        Apply(profile);
        Status = AgencyStatus.Active;
        CreatedAt = now;
        UpdatedAt = now;
        CreatedByUserId = ownerUserId;
    }

    public string Name { get; private set; }

    // Unique across current and former slugs (AgencyFormerSlug) — see AgencySlug.
    public string Slug { get; private set; }

    // The logo as uploaded through the API (never an outside URL); null until one is uploaded.
    public string? LogoBlobName { get; private set; }

    // Plain text, shown with its line breaks.
    public string? Bio { get; private set; }

    public string Phone { get; private set; }

    public string Email { get; private set; }

    public string? Website { get; private set; }

    public string? Address { get; private set; }

    // The agency's city — the same raion list as listing locations, so the directory can filter by it.
    public Guid? RaionId { get; private set; }

    // Set by site admins only.
    public bool IsVerified { get; private set; }

    public DateTimeOffset? VerifiedAt { get; private set; }

    public AgencyStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    // Who created it. Null once that account is deleted (the agency can outlive it).
    public Guid? CreatedByUserId { get; private set; }

    public IReadOnlyCollection<AgencyMember> Members => _members.AsReadOnly();

    // A new agency, with its creator as its first Owner.
    public static Agency Create(AgencyProfile profile, string slug, Guid ownerUserId, DateTimeOffset now, Guid? id = null)
    {
        if (ownerUserId == Guid.Empty)
        {
            throw new ArgumentException("An agency needs an owner.", nameof(ownerUserId));
        }

        if (string.IsNullOrWhiteSpace(slug) || slug.Length > AgencySlug.MaxLength)
        {
            throw new ArgumentException("A slug is required.", nameof(slug));
        }

        var agency = new Agency(id ?? Guid.NewGuid(), profile, slug, ownerUserId, now);
        agency._members.Add(AgencyMember.Create(agency.Id, ownerUserId, AgencyRole.Owner, now));
        return agency;
    }

    public AgencyRole? RoleOf(Guid userId) => _members.FirstOrDefault(m => m.UserId == userId)?.Role;

    public bool IsMember(Guid userId) => RoleOf(userId) is not null;

    private void Apply(AgencyProfile profile)
    {
        Name = Required(profile.Name, MaxNameLength, nameof(profile.Name));
        Phone = Required(profile.Phone, MaxPhoneLength, nameof(profile.Phone));
        Email = Required(profile.Email, MaxEmailLength, nameof(profile.Email));
        Bio = Optional(profile.Bio, MaxBioLength, nameof(profile.Bio));
        Website = Optional(profile.Website, MaxWebsiteLength, nameof(profile.Website));
        Address = Optional(profile.Address, MaxAddressLength, nameof(profile.Address));
        RaionId = profile.RaionId == Guid.Empty ? null : profile.RaionId;
    }

    private static string Required(string? value, int maxLength, string name) =>
        Optional(value, maxLength, name) ?? throw new ArgumentException($"{name} is required.", name);

    // Trimmed; blank becomes null.
    private static string? Optional(string? value, int maxLength, string name)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"{name} is longer than {maxLength} characters.", name);
        }

        return trimmed;
    }
}

// The details an agency's owners edit themselves (everything but slug, logo, verification and status).
public sealed record AgencyProfile(
    string Name,
    string Phone,
    string Email,
    string? Bio = null,
    string? Website = null,
    string? Address = null,
    Guid? RaionId = null);
