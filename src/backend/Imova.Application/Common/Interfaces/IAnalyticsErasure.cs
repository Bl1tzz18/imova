namespace Imova.Application.Common.Interfaces;

// Product analytics (PostHog) as account deletion sees it: the web app identifies a visitor who
// accepted analytics cookies by their account id, so deleting the account must delete that person
// and their events there too (Infrastructure's PostHogAnalyticsErasure; a no-op where PostHog isn't
// configured). Throws on failure — the caller decides that it's best-effort.
public interface IAnalyticsErasure
{
    Task EraseUserAsync(Guid userId, CancellationToken cancellationToken);
}
