using Imova.Domain.Common;

namespace Imova.Domain.SavedSearches;

public enum AlertFrequency
{
    Off = 0,

    // One digest email a day with the new matches.
    Daily = 1,

    // New matches emailed within minutes (each run of the alert job).
    Instant = 2,
}

// A search a user saved to come back to — stored as the /search page's own query string (see
// SearchQueryString in Application), so it always means exactly what the page showed. Optionally
// emails new matches (AlertFrequency); LastAlertedUpTo is the publication time up to which matches
// have already been emailed, LastViewedAt when the user last opened it ("N new since your visit").
public sealed class SavedSearch : Entity
{
    public const int MaxNameLength = 100;
    public const int MaxQueryLength = 2000;

    // Parameter names match properties so EF can use this constructor too.
    private SavedSearch(Guid id, Guid userId, string name, string queryString, AlertFrequency alertFrequency, DateTimeOffset createdAt)
        : base(id)
    {
        UserId = userId;
        Name = name;
        QueryString = queryString;
        AlertFrequency = alertFrequency;
        CreatedAt = createdAt;
        LastViewedAt = createdAt;
        LastAlertedUpTo = createdAt;
    }

    public Guid UserId { get; private set; }

    public string Name { get; private set; }

    public string QueryString { get; private set; }

    public AlertFrequency AlertFrequency { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset LastViewedAt { get; private set; }

    public DateTimeOffset LastAlertedUpTo { get; private set; }

    // When the last alert *email* went out — what "once a day" is measured from.
    public DateTimeOffset? LastAlertSentAt { get; private set; }

    public static SavedSearch Create(Guid userId, string name, string queryString, AlertFrequency alertFrequency, DateTimeOffset now)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        if (queryString.Length > MaxQueryLength)
        {
            throw new ArgumentException($"QueryString can be at most {MaxQueryLength} characters.", nameof(queryString));
        }

        return new SavedSearch(Guid.NewGuid(), userId, ValidName(name), queryString, ValidFrequency(alertFrequency), now);
    }

    public void Rename(string name) => Name = ValidName(name);

    public void ChangeAlertFrequency(AlertFrequency alertFrequency) => AlertFrequency = ValidFrequency(alertFrequency);

    public void MarkViewed(DateTimeOffset now) => LastViewedAt = now;

    // Daily: at most one email per ~24h. Instant: every run. Off: never.
    public bool IsAlertDue(DateTimeOffset now) => AlertFrequency switch
    {
        AlertFrequency.Instant => true,
        AlertFrequency.Daily => LastAlertSentAt is not { } sent || now - sent >= TimeSpan.FromHours(23),
        _ => false,
    };

    // Matches published up to `upTo` have been dealt with (emailed, or there were none).
    public void RecordAlertRun(DateTimeOffset upTo, bool emailSent, DateTimeOffset now)
    {
        if (upTo > LastAlertedUpTo)
        {
            LastAlertedUpTo = upTo;
        }

        if (emailSent)
        {
            LastAlertSentAt = now;
        }
    }

    private static string ValidName(string name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > MaxNameLength)
        {
            throw new ArgumentException($"Name must be 1-{MaxNameLength} characters.", nameof(name));
        }

        return trimmed;
    }

    private static AlertFrequency ValidFrequency(AlertFrequency frequency) =>
        Enum.IsDefined(frequency) ? frequency : throw new ArgumentOutOfRangeException(nameof(frequency));
}
