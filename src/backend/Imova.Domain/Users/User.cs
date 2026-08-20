using Imova.Domain.Common;

namespace Imova.Domain.Users;

public sealed class User : AggregateRoot
{
    private User(
        Guid id,
        string email)
        : base(id)
    {
        Email = email;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Email { get; private set; }

    public string? Phone { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    public static User Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        return new User(Guid.NewGuid(), email);
    }

    public void UpdatePhone(string phone)
    {
        Phone = phone;
    }
}
