using FluentValidation;
using FluentValidation.Results;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings.Attributes;
using Imova.Domain.Listings;
using Imova.Domain.Locations;
using Imova.Domain.Properties.Attributes;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings;

// The "turn a validated IListingWriteCommand into domain values" steps shared by
// CreateListingHandler and UpdateListingHandler.
public static class ListingWriteSupport
{
    public sealed record ResolvedAddress(
        Raion Raion,
        Localitate? Localitate,
        ChisinauSector? ChisinauSector,
        double? Latitude,
        double? Longitude);

    // Resolves the reference-data names (guaranteed to exist by ListingWriteValidator) and
    // geocodes the address. Geocoding never throws (see IGeocodingService) — a null result just
    // means the listing saves without coordinates rather than failing the whole request.
    public static async Task<ResolvedAddress> ResolveAddressAsync(
        IApplicationDbContext dbContext,
        IGeocodingService geocodingService,
        IListingWriteCommand command,
        CancellationToken cancellationToken)
    {
        var raion = await dbContext.Raioane.AsNoTracking().FirstAsync(r => r.Id == command.RaionId, cancellationToken);
        var localitate = command.LocalitateId.HasValue
            ? await dbContext.Localitati.AsNoTracking().FirstAsync(l => l.Id == command.LocalitateId.Value, cancellationToken)
            : null;
        var chisinauSector = command.ChisinauSectorId.HasValue
            ? await dbContext.ChisinauSectors.AsNoTracking().FirstAsync(s => s.Id == command.ChisinauSectorId.Value, cancellationToken)
            : null;

        var address = PropertyAddress.Compose(
            command.StreetAddress, command.BuildingNumber, chisinauSector?.Name, localitate?.NameRo, raion.NameRo, command.Country);
        var geocoded = await geocodingService.GeocodeAsync(address, cancellationToken);

        return new ResolvedAddress(raion, localitate, chisinauSector, geocoded?.Latitude, geocoded?.Longitude);
    }

    public static PropertyLocation CreateLocation(IListingWriteCommand command, ResolvedAddress address) =>
        PropertyLocation.Create(
            command.Country,
            address.Raion.Id,
            address.Raion.NameRo,
            address.Localitate?.Id,
            address.Localitate?.NameRo,
            address.ChisinauSector?.Id,
            address.ChisinauSector?.Name,
            address.Latitude,
            address.Longitude,
            command.StreetAddress,
            command.BuildingNumber);

    public static void UpdateLocation(PropertyLocation location, IListingWriteCommand command, ResolvedAddress address) =>
        location.UpdateDetails(
            command.Country,
            address.Raion.Id,
            address.Raion.NameRo,
            address.Localitate?.Id,
            address.Localitate?.NameRo,
            address.ChisinauSector?.Id,
            address.ChisinauSector?.Name,
            address.Latitude,
            address.Longitude,
            command.StreetAddress,
            command.BuildingNumber);

    // Already validated by ListingWriteValidator; the throw is a guard for callers that bypass
    // the MediatR pipeline (e.g. handler unit tests).
    public static PropertyAttributes ParseAttributes(IListingWriteCommand command)
    {
        if (!PropertyAttributesJson.TryParse(command.PropertyType, command.TypeSpecificAttributes, out var attributes, out var error))
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(IListingWriteCommand.TypeSpecificAttributes), error)]);
        }

        return attributes!;
    }

    public static IReadOnlyList<Guid> AmenityIds(IListingWriteCommand command) => command.AmenityIds ?? [];

    public static Price BuildPrice(IListingWriteCommand command, IExchangeRateProvider exchangeRates) =>
        Price.Create(command.Price, command.Currency, command.IsNegotiable, exchangeRates.GetEurRate(command.Currency));

    public static RentalDetails? RentalDetails(IListingWriteCommand command) =>
        command.TransactionType == TransactionType.Rent ? command.RentalDetails ?? new RentalDetails() : null;
}
