using Imova.Domain.Common;

namespace Imova.Domain.Publishers;

// A person's public identity as the author of listings — separate from the login account
// (ApplicationUser), whose own details stay private. Every user gets exactly one, automatically
// (register / first Google sign-in; unique per user in PublisherConfiguration). Publishing under an
// agency doesn't change the author: the listing keeps its author's Publisher and also points at the
// agency (Listing.AgencyId).
public sealed class Publisher : AggregateRoot
{
    private Publisher(Guid id, Guid userId, string displayName, string? phone, string email)
        : base(id)
    {
        UserId = userId;
        DisplayName = displayName;
        Phone = phone;
        Email = email;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }

    public string DisplayName { get; private set; }

    // Null until the user adds a phone number: a Google sign-in account has none at first.
    public string? Phone { get; private set; }

    public string Email { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Publisher CreateIndividual(Guid userId, string displayName, string? phone, string email)
    {
        EnsureValid(userId, displayName, email);
        return new Publisher(Guid.NewGuid(), userId, displayName, phone, email);
    }

    // Keeps the publisher's public contact details in step with the account it belongs to (see
    // UpdateProfileHandler / UpdatePhoneNumberHandler).
    public void UpdateContactDetails(string displayName, string? phone, string email)
    {
        EnsureValid(UserId, displayName, email);
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
