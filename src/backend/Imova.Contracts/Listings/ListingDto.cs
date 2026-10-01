using Imova.Contracts.Properties;
using Imova.Contracts.Publishers;

namespace Imova.Contracts.Listings;

public record ListingDto(
    Guid Id,
    string Status,
    string TransactionType,
    string Title,
    string Description,
    PriceDto Price,
    SaleDetailsDto? SaleDetails,
    RentalDetailsDto? RentalDetails,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ExpiresAt,
    string? RejectionReason,
    string? SuspensionReason,
    PropertyDto Property,
    PublisherDto Publisher,
    IReadOnlyList<PhotoDto> Photos,
    bool IsSaved,
    // The short public number ("ID 100231").
    long Number,
    // Different people who opened it / asked for its phone number (each counted once per 24 hours,
    // never the owner) — the owner's and admins' only, else null.
    int? ViewCount,
    int? PhoneRevealCount,
    // Only on a listing's detail view (null on cards/search results) — see ListingContactDto.
    ListingContactDto? Contact = null,
    // "Preț redus": set while the price is down from what it was before its latest drop (see
    // ListingPriceHistory for the rule) — on every view, so cards can show it.
    PriceReductionDto? PriceReduction = null,
    // The price changes since the listing was first published, oldest first (at most the latest
    // ListingPriceHistory.MaxEntries) — only on the detail view, null elsewhere.
    PriceHistoryDto? PriceHistory = null);

// Percent: how much lower, rounded (from EUR values when the currency changed).
public record PriceReductionDto(decimal PreviousAmount, string PreviousCurrency, int Percent, DateTimeOffset ReducedAt);

// StartsAtPublication: Changes[0]'s old price is the price it was published at (false when older
// changes were left out).
public record PriceHistoryDto(bool StartsAtPublication, IReadOnlyList<PriceChangeDto> Changes);

// ChangePercent is signed: negative = cheaper.
public record PriceChangeDto(
    decimal OldAmount,
    string OldCurrency,
    decimal NewAmount,
    string NewCurrency,
    int ChangePercent,
    DateTimeOffset ChangedAt);

public record PriceDto(decimal Amount, string Currency, decimal PriceEur, bool IsNegotiable);

public record SaleDetailsDto(string? OwnershipDocumentType);

public record RentalDetailsDto(
    int? MinLeasePeriodMonths,
    decimal? SecurityDepositAmount,
    bool UtilitiesIncluded,
    DateTime? AvailableFrom,
    // Null when not asked (rentals other than an apartment, house or room).
    bool? PetsAllowed);
