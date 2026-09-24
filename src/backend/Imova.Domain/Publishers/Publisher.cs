using Imova.Domain.Common;

namespace Imova.Domain.Publishers;

// The identity a Listing is published under — separate from the login account (ApplicationUser)
// so one user can publish both as themselves and as an agency, and so an agency can have a public
// profile of its own. Every user gets an Individual publisher automatically (register / first
// Google sign-in); an Agency publisher is created explicitly. At most one of each type per user
// (unique index in PublisherConfiguration).
public sealed class Publisher : AggregateRoot
{
    private Publisher(
        Guid id,
        Guid userId,
        PublisherType publisherType,
        string displayName,
        string? phone,
        string email,
        string? logoUrl,
        string? bio)
        : base(id)
    {
        UserId = userId;
        PublisherType = publisherType;
        DisplayName = displayName;
        Phone = phone;
        Email = email;
        LogoUrl = logoUrl;
        Bio = bio;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }

    public PublisherType PublisherType { get; private set; }

    public string DisplayName { get; private set; }

    // Nullable for Individual publishers: a Google sign-in account has no phone number until the
    // user completes their profile. Always set for an Agency.
    public string? Phone { get; private set; }

    public string Email { get; private set; }

    // Agency only.
    public string? LogoUrl { get; private set; }

    // Agency only.
    public string? Bio { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Publisher CreateIndividual(Guid userId, string displayName, string? phone, string email)
    {
        EnsureValid(userId, displayName, email);
        return new Publisher(Guid.NewGuid(), userId, PublisherType.Individual, displayName, phone, email, null, null);
    }

    public static Publisher CreateAgency(
        Guid userId, string displayName, string phone, string email, string? logoUrl, string? bio)
    {
        EnsureValid(userId, displayName, email);

        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new ArgumentException("An agency requires a phone number.", nameof(phone));
        }

        return new Publisher(Guid.NewGuid(), userId, PublisherType.Agency, displayName, phone, email, logoUrl, bio);
    }

    // Keeps an Individual publisher's public contact details in step with the account they belong
    // to (see UpdateProfileHandler / UpdatePhoneNumberHandler).
    public void UpdateContactDetails(string displayName, string? phone, string email)
    {
        EnsureValid(UserId, displayName, email);

        if (PublisherType == PublisherType.Agency && string.IsNullOrWhiteSpace(phone))
        {
            throw new ArgumentException("An agency requires a phone number.", nameof(phone));
        }

        DisplayName = displayName;
        Phone = phone;
        Email = email;
    }

    private static void EnsureValid(Guid userId, string displayName, string email)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("DisplayName is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }
    }
}
