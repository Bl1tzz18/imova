using FluentValidation;
using FluentValidation.Results;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Media;
using Imova.Application.Features.Publishers;
using Imova.Contracts.Listings;
using Imova.Domain.Listings;
using Imova.Domain.Properties;
using Imova.Domain.Publishers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Listings.CreateListing;

public class CreateListingHandler(
    IApplicationDbContext dbContext,
    IGeocodingService geocodingService,
    IExchangeRateProvider exchangeRates,
    IBlobStorageService blobStorageService) : IRequestHandler<CreateListingCommand, ListingDto>
{
    public async Task<ListingDto> Handle(CreateListingCommand request, CancellationToken cancellationToken)
    {
        // The caller is the author; publishing under an agency needs them to be its member.
        if (request.AgencyId is { } agencyId)
        {
            await ListingAgencyRules.EnsureCanPublishAsAsync(dbContext, agencyId, request.RequestingUserId, cancellationToken);
        }

        var publisher = await PublisherProvisioning.EnsureIndividualAsync(dbContext, request.RequestingUserId, cancellationToken);

        // The form uploads photos under this id before creating the listing — they must be the
        // caller's own (see MediaAccess), or anyone could plant photos on someone else's listing.
        if (request.Id is { } id)
        {
            await MediaAccess.EnsureNoOneElsesPhotosAsync(dbContext, id, request.RequestingUserId, cancellationToken);
        }

        var address = await ListingWriteSupport.ResolveAddressAsync(dbContext, geocodingService, request, cancellationToken);
        var location = ListingWriteSupport.CreateLocation(request, address);

        var property = Property.Create(
            request.PropertyType,
            request.TotalAreaM2,
            request.YearBuilt,
            request.Condition,
            location.Id,
            ListingWriteSupport.ParseAttributes(request),
            ListingWriteSupport.AmenityIds(request),
            ListingWriteSupport.ProximityIds(request));

        var listing = Listing.Create(
            property.Id,
            publisher.Id,
            request.TransactionType,
            request.Title,
            request.Description,
            ListingWriteSupport.BuildPrice(request, exchangeRates),
            rentalDetails: ListingWriteSupport.RentalDetails(request),
            contact: request.Contact,
            id: request.Id,
            agencyId: request.AgencyId);

        // A new listing goes straight into the admin review queue — the owner doesn't take a
        // separate "submit for review" step for a listing they just finished creating.
        // SubmitListingForReview stays available for resubmitting a fixed Rejected listing.
        // Unless the owner's email isn't confirmed yet: then it waits as a Draft, and confirming
        // the email submits it (see ReviewEligibility).
        if (await ReviewEligibility.IsEmailConfirmedAsync(dbContext, publisher.UserId, cancellationToken))
        {
            listing.SubmitForReview();
        }

        // One SaveChanges = one transaction: the location, property (+ amenities and proximities), and listing are
        // either all created or none are.
        dbContext.PropertyLocations.Add(location);
        dbContext.Properties.Add(property);
        dbContext.Listings.Add(listing);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await ListingDtoLoader.LoadOneAsync(
            dbContext, blobStorageService, listing, request.RequestingUserId, cancellationToken, includeContactDetails: true);
    }
}
