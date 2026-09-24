using Imova.Application.Common.Interfaces;
using Imova.Contracts.Listings;
using Imova.Domain.Listings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings.UpdateListing;

public class UpdateListingHandler(
    IApplicationDbContext dbContext,
    IGeocodingService geocodingService,
    IExchangeRateProvider exchangeRates,
    IBlobStorageService blobStorageService) : IRequestHandler<UpdateListingCommand, ListingDto?>
{
    public async Task<ListingDto?> Handle(UpdateListingCommand request, CancellationToken cancellationToken)
    {
        var listing = await dbContext.Listings.FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);
        if (listing is null)
        {
            return null;
        }

        await ListingAccess.EnsureCanManageAsync(dbContext, listing, request.RequestingUserId, request.IsAdmin, cancellationToken);

        var property = await dbContext.Properties
            .Include(p => p.Amenities)
            .FirstAsync(p => p.Id == listing.PropertyId, cancellationToken);
        var location = await dbContext.PropertyLocations.FirstAsync(l => l.Id == property.LocationId, cancellationToken);

        // Re-geocode on every edit — the form doesn't tell us whether the address fields actually
        // changed, and geocoding never throws, so re-resolving is simpler than change detection.
        var address = await ListingWriteSupport.ResolveAddressAsync(dbContext, geocodingService, request, cancellationToken);
        ListingWriteSupport.UpdateLocation(location, request, address);

        property.UpdateDetails(
            request.PropertyType,
            request.TotalAreaM2,
            request.YearBuilt,
            request.Condition,
            ListingWriteSupport.ParseAttributes(request),
            ListingWriteSupport.AmenityIds(request));

        listing.UpdateDetails(
            request.TransactionType,
            request.Title,
            request.Description,
            ListingWriteSupport.BuildPrice(request, exchangeRates),
            // Keep whatever sale terms exist while it stays a sale; switching to rent drops them.
            request.TransactionType == TransactionType.Sale ? listing.SaleDetails : null,
            ListingWriteSupport.RentalDetails(request));

        // Saving edits to a Rejected listing is the owner's way of addressing whatever an admin
        // flagged — resubmit it in the same step instead of making them press a separate button.
        if (listing.Status == ListingStatus.Rejected)
        {
            listing.SubmitForReview();
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await ListingDtoLoader.LoadOneAsync(
            dbContext, blobStorageService, listing, request.RequestingUserId, cancellationToken, includeContactDetails: true);
    }
}
