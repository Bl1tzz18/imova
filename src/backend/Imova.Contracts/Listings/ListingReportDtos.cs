namespace Imova.Contracts.Listings;

// Reasons: Fraud, WrongInformation, NoLongerAvailable, Duplicate, Other.
// Outcomes: ListingSuspended, Dismissed.

// Amended = the caller already had an open report on this listing, and it was updated instead.
public record ReportListingResultDto(bool Amended);

// The admin moderation page's badge: listings waiting for a decision, and the reports on them.
public record ListingReportSummaryDto(int OpenListings, int OpenReports);

// One case in the admin queue: a listing with the reports that are open on it (Resolution null),
// or — in the history — the reports resolved together by one decision.
public record ReportedListingDto(
    ListingDto Listing,
    int ReportCount,
    // Most frequent first.
    IReadOnlyList<ReportReasonCountDto> Reasons,
    DateTimeOffset FirstReportedAt,
    DateTimeOffset LastReportedAt,
    // Newest first.
    IReadOnlyList<ListingReportItemDto> Reports,
    ListingReportResolutionDto? Resolution);

public record ReportReasonCountDto(string Reason, int Count);

public record ListingReportItemDto(
    Guid Id,
    string Reason,
    string? Details,
    DateTimeOffset CreatedAt,
    // Later than CreatedAt when the reporter changed their report.
    DateTimeOffset UpdatedAt,
    ListingReporterDto Reporter);

// Enough to judge the report: who it is, and how their earlier reports went (someone whose
// reports keep getting dismissed may be reporting a competitor).
public record ListingReporterDto(
    Guid UserId,
    string? DisplayName,
    string? Email,
    // The account was deleted since; the report is kept.
    bool IsDeleted,
    // Listing reports this person has filed in total (this one included), and how many of them
    // an admin dismissed.
    int ReportsFiled,
    int ReportsDismissed);

public record ListingReportResolutionDto(
    string Outcome,
    DateTimeOffset ResolvedAt,
    Guid ResolvedByUserId,
    string? ResolvedByName,
    // The suspension reason (the owner sees it too) or the dismissal note (admins only).
    string? Note);
