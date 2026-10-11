using System.Text.Json;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings;
using Imova.Domain.Listings;
using Imova.Domain.Properties;

namespace Imova.Application.Features.Agencies.Import;

// One CSV row as the same payload the listing form sends (IListingWriteCommand), so it's checked by
// the same rules (ImportListingValidator = ListingWriteValidator) and saved the same way.
public sealed record ImportListingCommand : IListingWriteCommand
{
    public PropertyType PropertyType { get; init; }

    public decimal TotalAreaM2 { get; init; }

    public int? YearBuilt { get; init; }

    public PropertyCondition? Condition { get; init; }

    public JsonElement? TypeSpecificAttributes { get; init; }

    public IReadOnlyList<Guid>? AmenityIds { get; init; }

    public IReadOnlyList<Guid>? ProximityIds { get; init; }

    public string Country { get; init; } = "Moldova";

    public Guid RaionId { get; init; }

    public Guid? LocalitateId { get; init; }

    public Guid? ChisinauSectorId { get; init; }

    public string? StreetAddress { get; init; }

    public string? BuildingNumber { get; init; }

    public TransactionType TransactionType { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public Currency Currency { get; init; }

    public bool IsNegotiable { get; init; }

    public RentalDetails? RentalDetails { get; init; }

    public ListingContact? Contact { get; init; }
}

public sealed class ImportListingValidator(IApplicationDbContext dbContext) : ListingWriteValidator<ImportListingCommand>(dbContext);
