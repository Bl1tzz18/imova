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
    // Different people who opened it (each counted once per 24 hours, never the owner).
    int ViewCount,
    // Different people who asked for its phone number — the owner's and admins' only, else null.
    int? PhoneRevealCount,
    // Only on a listing's detail view (null on cards/search results) — see ListingContactDto.
    ListingContactDto? Contact = null);

public record PriceDto(decimal Amount, string Currency, decimal PriceEur, bool IsNegotiable);

public record SaleDetailsDto(string? OwnershipDocumentType);

public record RentalDetailsDto(
    int? MinLeasePeriodMonths,
    decimal? SecurityDepositAmount,
    bool UtilitiesIncluded,
    DateTime? AvailableFrom,
    // Null when not asked (rentals other than an apartment, house or room).
    bool? PetsAllowed);
