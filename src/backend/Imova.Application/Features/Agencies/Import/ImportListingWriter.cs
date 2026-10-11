using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings;
using Imova.Application.Features.Publishers;
using Imova.Domain.Agencies;
using Imova.Domain.Listings;
using Imova.Domain.Properties;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Agencies.Import;

public sealed record ImportWriteResult(Listing? Listing, bool Created, IReadOnlyList<ListingImportProblem> Problems);

// Saves one valid row: creates the agency's listing with that external_ref, or updates the one it
// already has — the same steps as the listing form's create and edit (ListingWriteSupport, price
// history), but without the review queue (a verified agency's import goes live at once).
//
// Status: a new row goes Active (status inactive: deactivated). An existing listing: inactive takes an
// Active one down; active puts a deactivated / expired one back up (and publishes one still a draft);
// one in review, rejected, suspended, sold or rented keeps its status — an import never overrides
// moderation or a closed deal — but its details are updated.
// Author: a new listing's is the row's (agent_email or the default); an existing one changes author
// only when agent_email names a member.
// Adds to the context; the caller saves.
public sealed class ImportListingWriter(IApplicationDbContext dbContext, IExchangeRateProvider exchangeRates, ImportPhotoSync photoSync)
{
    public async Task<ImportWriteResult> WriteAsync(
        ImportRowPlan plan, Guid agencyId, Guid uploadedByUserId, double? latitude, double? longitude, CancellationToken cancellationToken)
    {
        var command = plan.Command;
        var address = await AddressAsync(plan.Location, latitude, longitude, cancellationToken);
        var existing = await dbContext.Listings.FirstOrDefaultAsync(l => l.AgencyId == agencyId && l.ExternalRef == plan.ExternalRef, cancellationToken);

        var listingId = existing?.Id ?? Guid.NewGuid();
        var photos = await photoSync.SyncAsync(listingId, plan.ImageUrls, uploadedByUserId, cancellationToken);
        if (!photos.Enough)
        {
            return new ImportWriteResult(null, false, photos.Problems);
        }

        if (existing is null)
        {
            var publisher = await PublisherProvisioning.EnsureIndividualAsync(dbContext, plan.AuthorUserId, cancellationToken);
            var location = ListingWriteSupport.CreateLocation(command, address);
            var property = Property.Create(
                command.PropertyType,
                command.TotalAreaM2,
                command.YearBuilt,
                command.Condition,
                location.Id,
                ListingWriteSupport.ParseAttributes(command),
                ListingWriteSupport.AmenityIds(command),
                ListingWriteSupport.ProximityIds(command));
            var listing = Listing.Create(
                property.Id,
                publisher.Id,
                command.TransactionType,
                command.Title,
                command.Description,
                ListingWriteSupport.BuildPrice(command, exchangeRates),
                rentalDetails: ListingWriteSupport.RentalDetails(command),
                contact: command.Contact,
                id: listingId,
                agencyId: agencyId);
            listing.AssignExternalRef(plan.ExternalRef);
            if (plan.Inactive)
            {
                listing.ImportDeactivated();
            }
            else
            {
                listing.PublishImported();
            }

            dbContext.PropertyLocations.Add(location);
            dbContext.Properties.Add(property);
            dbContext.Listings.Add(listing);
            return new ImportWriteResult(listing, true, photos.Problems);
        }

        var existingProperty = await dbContext.Properties
            .Include(p => p.Amenities)
            .Include(p => p.Proximities)
            .FirstAsync(p => p.Id == existing.PropertyId, cancellationToken);
        var existingLocation = await dbContext.PropertyLocations.FirstAsync(l => l.Id == existingProperty.LocationId, cancellationToken);
        ListingWriteSupport.UpdateLocation(existingLocation, command, address);
        existingProperty.UpdateDetails(
            command.PropertyType,
            command.TotalAreaM2,
            command.YearBuilt,
            command.Condition,
            ListingWriteSupport.ParseAttributes(command),
            ListingWriteSupport.AmenityIds(command),
            ListingWriteSupport.ProximityIds(command));

        var oldPrice = existing.Price;
        existing.UpdateDetails(
            command.TransactionType,
            command.Title,
            command.Description,
            ListingWriteSupport.BuildPrice(command, exchangeRates),
            command.TransactionType == TransactionType.Sale ? existing.SaleDetails : null,
            ListingWriteSupport.RentalDetails(command),
            command.Contact);
        if (ListingPriceChange.Between(existing.Id, oldPrice, existing.Price, existing.UpdatedAt) is { } priceChange)
        {
            dbContext.ListingPriceChanges.Add(priceChange);
        }

        if (plan.AuthorNamed)
        {
            var publisher = await PublisherProvisioning.EnsureIndividualAsync(dbContext, plan.AuthorUserId, cancellationToken);
            if (publisher.Id != existing.PublisherId)
            {
                existing.ChangeAuthor(publisher.Id);
            }
        }

        ApplyStatus(existing, plan.Inactive);
        return new ImportWriteResult(existing, false, photos.Problems);
    }

    public static void ApplyStatus(Listing listing, bool inactive)
    {
        switch (listing.Status)
        {
            case ListingStatus.Active when inactive:
                listing.Archive();
                break;
            case ListingStatus.Archived or ListingStatus.Expired when !inactive:
                listing.Publish();
                break;
            case ListingStatus.Draft when inactive:
                listing.ImportDeactivated();
                break;
            case ListingStatus.Draft:
                listing.PublishImported();
                break;
        }
    }

    // The reference rows behind the row's place, with the coordinates already decided (ImportGeolocator).
    private async Task<ListingWriteSupport.ResolvedAddress> AddressAsync(
        ImportLocation location, double? latitude, double? longitude, CancellationToken cancellationToken)
    {
        var raion = await dbContext.Raioane.AsNoTracking().FirstAsync(r => r.Id == location.RaionId, cancellationToken);
        var localitate = location.LocalitateId is { } localitateId
            ? await dbContext.Localitati.AsNoTracking().FirstAsync(l => l.Id == localitateId, cancellationToken)
            : null;
        var sector = location.ChisinauSectorId is { } sectorId
            ? await dbContext.ChisinauSectors.AsNoTracking().FirstAsync(s => s.Id == sectorId, cancellationToken)
            : null;
        return new ListingWriteSupport.ResolvedAddress(raion, localitate, sector, latitude, longitude);
    }
}
