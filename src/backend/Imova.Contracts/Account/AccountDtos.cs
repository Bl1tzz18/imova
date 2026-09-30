using Imova.Contracts.Listings;

namespace Imova.Contracts.Account;

// What the account holds — shown before deleting it, so the user knows what goes.
public record AccountDataSummaryDto(
    int Listings,
    int ActiveListings,
    int Favorites,
    int SavedSearches,
    int Conversations,
    bool HasAgency,
    bool HasPassword);

// --- The personal data export (imova-data.json inside the downloaded ZIP) ---
//
// Everything IMOVA stores about the account, in a structured, machine-readable form (GDPR art. 15
// and 20; Moldova's Law 133/2011). Files (photos, message images, the profile picture) are in the
// same ZIP; the *File fields hold their path inside it. MissingFiles: files that were referenced
// but no longer exist in storage.
public record PersonalDataExportDto(
    string Format,
    DateTimeOffset GeneratedAt,
    ExportedAccountDto Account,
    IReadOnlyList<ExportedSessionDto> Sessions,
    IReadOnlyList<ExportedPublisherDto> Publishers,
    IReadOnlyList<ExportedListingDto> Listings,
    IReadOnlyList<ExportedPhotoDto> PhotosNotInAListing,
    IReadOnlyList<ExportedFavoriteDto> Favorites,
    IReadOnlyList<ExportedSavedSearchDto> SavedSearches,
    IReadOnlyList<ExportedConversationDto> Conversations,
    IReadOnlyList<ExportedBlockDto> BlockedUsers,
    IReadOnlyList<ExportedReportDto> ReportsYouFiled,
    IReadOnlyList<ExportedListingReportDto> ListingReportsYouFiled,
    IReadOnlyList<string> MissingFiles);

public record ExportedAccountDto(
    Guid Id,
    string Email,
    bool EmailConfirmed,
    string? DisplayName,
    string? PhoneNumber,
    string? ProfilePictureFile,
    bool HasPassword,
    // External sign-in providers linked to the account ("Google").
    IReadOnlyList<string> SignInProviders,
    IReadOnlyList<string> Roles,
    bool IsBannedFromMessaging);

// One login session (a device/browser). EndedAt: signed out or revoked.
public record ExportedSessionDto(
    DateTimeOffset StartedAt,
    DateTimeOffset LastActiveAt,
    bool RememberMe,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? EndedAt);

public record ExportedPublisherDto(
    Guid Id,
    string Type,
    string DisplayName,
    string? Phone,
    string Email,
    string? LogoUrl,
    string? Bio,
    DateTimeOffset CreatedAt);

public record ExportedListingDto(ListingDto Listing, IReadOnlyList<string> PhotoFiles);

// Uploaded while filling in a listing that was never created.
public record ExportedPhotoDto(Guid ListingId, string File, DateTimeOffset UploadedAt);

// ListingTitle is null when the listing no longer exists.
public record ExportedFavoriteDto(Guid ListingId, string? ListingTitle, DateTimeOffset SavedAt);

public record ExportedSavedSearchDto(
    string Name,
    string SearchUrl,
    string AlertFrequency,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastViewedAt,
    DateTimeOffset? LastAlertSentAt);

// YourRole: Visitor (you wrote about someone's listing) | Publisher (about your listing).
// OtherParticipant is null when that account has been deleted.
public record ExportedConversationDto(
    Guid Id,
    Guid ListingId,
    string? ListingTitle,
    string YourRole,
    string? OtherParticipant,
    DateTimeOffset StartedAt,
    bool ArchivedByYou,
    IReadOnlyList<ExportedMessageDto> Messages);

public record ExportedMessageDto(
    bool SentByYou,
    string Body,
    DateTimeOffset SentAt,
    DateTimeOffset? DeliveredAt,
    DateTimeOffset? ReadAt,
    IReadOnlyList<string> AttachmentFiles);

public record ExportedBlockDto(string? BlockedUser, DateTimeOffset BlockedAt);

// The outcome (ListingSuspended / Dismissed) is included, the admin's note isn't — that's
// the moderators' own record.
public record ExportedListingReportDto(
    Guid ListingId,
    string Reason,
    string? Details,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt,
    string? Outcome);

public record ExportedReportDto(
    Guid ConversationId,
    string Reason,
    string? Details,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt);
