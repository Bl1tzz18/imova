namespace Imova.Domain.Messaging;

// Blocker has blocked Blocked: Blocked can no longer send messages in any conversation with the
// Blocker (see MessagingRules). One row per pair and direction.
public sealed class UserBlock
{
    // For EF Core materialization only.
    private UserBlock()
    {
    }

    public UserBlock(Guid blockerUserId, Guid blockedUserId, DateTimeOffset now)
    {
        if (blockerUserId == blockedUserId)
        {
            throw new ArgumentException("You can't block yourself.", nameof(blockedUserId));
        }

        BlockerUserId = blockerUserId;
        BlockedUserId = blockedUserId;
        CreatedAt = now;
    }

    public Guid BlockerUserId { get; private set; }

    public Guid BlockedUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}
