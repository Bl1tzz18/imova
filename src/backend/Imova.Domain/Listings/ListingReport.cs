using Imova.Domain.Common;

namespace Imova.Domain.Listings;

// A signed-in visitor flagging a public listing for the admins (scam, wrong details, already
// sold, …). Open until an admin acts on the listing: suspending it or dismissing its reports
// resolves every open report on that listing at once, so the reports resolved together form one
// "case" in the admin history (same ResolvedAt, outcome and note).
//
// A reporter has at most one open report per listing: reporting again while it's open amends it
// (a unique partial index backs this up). After it's resolved they can report the listing again.
// The reporter is never shown to the listing's owner.
public sealed class ListingReport : Entity
{
    public const int MaxDetailsLength = 1000;
    public const int MaxNoteLength = 1000;

    // For EF Core materialization only.
    private ListingReport()
        : base(Guid.Empty)
    {
    }

    private ListingReport(Guid id, Guid listingId, Guid reporterUserId, ListingReportReason reason, string? details, DateTimeOffset now)
        : base(id)
    {
        ListingId = listingId;
        ReporterUserId = reporterUserId;
        Reason = reason;
        Details = details;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid ListingId { get; private set; }

    // No FK to the user: a report outlives its reporter's account (kept for the moderation record,
    // like conversation reports), shown as a deleted account.
    public Guid ReporterUserId { get; private set; }

    public ListingReportReason Reason { get; private set; }

    public string? Details { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    // Last time the reporter amended it (= CreatedAt until then).
    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    public Guid? ResolvedByUserId { get; private set; }

    public ListingReportOutcome? Outcome { get; private set; }

    // The admin's words: the suspension reason (which the owner sees too), or why the reports
    // were dismissed (admins only).
    public string? ResolutionNote { get; private set; }

    public bool IsResolved => ResolvedAt is not null;

    // Only a live, public listing can be reported — anything else isn't visible to the reporter.
    // Owners can't report their own listing; the caller checks that (it needs the publisher).
    public static ListingReport Create(
        Listing listing, Guid reporterUserId, ListingReportReason reason, string? details, DateTimeOffset now)
    {
        if (listing.Status != ListingStatus.Active)
        {
            throw new InvalidOperationException("Only an active listing can be reported.");
        }

        return new ListingReport(Guid.NewGuid(), listing.Id, reporterUserId, reason, Normalize(reason, details), now);
    }

    public void Amend(ListingReportReason reason, string? details, DateTimeOffset now)
    {
        if (IsResolved)
        {
            throw new InvalidOperationException("A resolved report can't be changed; report the listing again instead.");
        }

        Reason = reason;
        Details = Normalize(reason, details);
        UpdatedAt = now;
    }

    // Resolving twice keeps the first admin, time and outcome.
    public void Resolve(ListingReportOutcome outcome, Guid adminUserId, string? note, DateTimeOffset now)
    {
        if (IsResolved)
        {
            return;
        }

        if (!Enum.IsDefined(outcome))
        {
            throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown outcome.");
        }

        Outcome = outcome;
        ResolvedAt = now;
        ResolvedByUserId = adminUserId;
        ResolutionNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    private static string? Normalize(ListingReportReason reason, string? details)
    {
        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown report reason.");
        }

        details = string.IsNullOrWhiteSpace(details) ? null : details.Trim();
        if (reason == ListingReportReason.Other && details is null)
        {
            throw new ArgumentException("Describe the problem when the reason is Other.", nameof(details));
        }

        return details;
    }
}

public enum ListingReportReason
{
    // Scam / fraud: asks for money up front, fake listing, stolen photos.
    Fraud = 1,

    // Wrong price, area, location or photos of another property.
    WrongInformation = 2,

    // Already sold or rented.
    NoLongerAvailable = 3,

    // The same property posted more than once.
    Duplicate = 4,

    Other = 5,
}

public enum ListingReportOutcome
{
    ListingSuspended = 1,

    // Looked into, nothing to act on.
    Dismissed = 2,
}
