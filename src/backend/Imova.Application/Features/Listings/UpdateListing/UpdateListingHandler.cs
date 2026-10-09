using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
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

        // In review, live, rejected or suspended: only with its photos (an older listing short of them
        // gets them on this, its next change — ListingPhotoRules).
        await ListingPhotoRules.EnsureEnoughForEditAsync(dbContext, listing, cancellationToken);

        // Moving it between private and an agency (or between agencies):
        // - out of an agency: only that agency's Owners/Admins — the agency keeps its inventory, so an
        //   Agent can't take its listings with them before leaving;
        // - into an agency: only its author, and only an agency they're an active member of.
        if (request.AgencyId != listing.AgencyId)
        {
            if (listing.AgencyId is { } leaving
                && !await ListingAccess.ManagesAgencyAsync(dbContext, leaving, request.RequestingUserId, cancellationToken))
            {
                throw new ForbiddenAccessException(
                    "Only the agency's owners and administrators can take a listing out of it.", ErrorCodes.ListingAgencyLeaveManagerOnly);
            }

            if (request.AgencyId is { } joining)
            {
                if (!await ListingAccess.IsAuthorAsync(dbContext, listing, request.RequestingUserId, cancellationToken))
                {
                    throw new ForbiddenAccessException(
                        "Only the listing's author can publish it under an agency.", ErrorCodes.ListingAgencyChangeAuthorOnly);
                }

                await ListingAgencyRules.EnsureCanPublishAsAsync(dbContext, joining, request.RequestingUserId, cancellationToken);
            }

            listing.ChangeAgency(request.AgencyId);
        }

        var property = await dbContext.Properties
            .Include(p => p.Amenities)
            .Include(p => p.Proximities)
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
            ListingWriteSupport.AmenityIds(request),
            ListingWriteSupport.ProximityIds(request));

        var oldPrice = listing.Price;
        listing.UpdateDetails(
            request.TransactionType,
            request.Title,
            request.Description,
            ListingWriteSupport.BuildPrice(request, exchangeRates),
            // Keep whatever sale terms exist while it stays a sale; switching to rent drops them.
            request.TransactionType == TransactionType.Sale ? listing.SaleDetails : null,
            ListingWriteSupport.RentalDetails(request),
            request.Contact);

        // Price history (see ListingPriceChange): only when the asking price itself changed.
        if (ListingPriceChange.Between(listing.Id, oldPrice, listing.Price, listing.UpdatedAt) is { } priceChange)
        {
            dbContext.ListingPriceChanges.Add(priceChange);
        }

        // Saving edits to a Rejected listing is the owner's way of addressing whatever an admin
        // flagged — resubmit it in the same step instead of making them press a separate button.
        // (Only with a confirmed owner email — see ReviewEligibility; otherwise it stays Rejected.)
        if (listing.Status == ListingStatus.Rejected
            && await ReviewEligibility.IsOwnerEmailConfirmedAsync(dbContext, listing, cancellationToken))
        {
            listing.SubmitForReview();
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await ListingDtoLoader.LoadOneAsync(
            dbContext, blobStorageService, listing, request.RequestingUserId, cancellationToken, includeContactDetails: true,
            viewerIsAdmin: request.IsAdmin);
    }
}
