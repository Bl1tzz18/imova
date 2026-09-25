using FluentValidation;
using FluentValidation.Results;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
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
        var publisher = await ResolvePublisherAsync(request, cancellationToken);

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
            id: request.Id);

        // A new listing goes straight into the admin review queue — the owner doesn't take a
        // separate "submit for review" step for a listing they just finished creating.
        // SubmitListingForReview stays available for resubmitting a fixed Rejected listing.
        listing.SubmitForReview();

        // One SaveChanges = one transaction: the location, property (+ amenities and proximities), and listing are
        // either all created or none are.
        dbContext.PropertyLocations.Add(location);
        dbContext.Properties.Add(property);
        dbContext.Listings.Add(listing);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await ListingDtoLoader.LoadOneAsync(
            dbContext, blobStorageService, listing, request.RequestingUserId, cancellationToken, includeContactDetails: true);
    }

    private async Task<Publisher> ResolvePublisherAsync(CreateListingCommand request, CancellationToken cancellationToken)
    {
        if (request.PublisherId is not { } publisherId)
        {
            return await PublisherProvisioning.EnsureIndividualAsync(dbContext, request.RequestingUserId, cancellationToken);
        }

        var publisher = await dbContext.Publishers.FirstOrDefaultAsync(p => p.Id == publisherId, cancellationToken)
            ?? throw new ValidationException(
                [new ValidationFailure(nameof(CreateListingCommand.PublisherId), "PublisherId does not reference a known publisher.")]);

        // Publishing under someone else's publisher (e.g. an agency you don't belong to) is never allowed.
        if (publisher.UserId != request.RequestingUserId)
        {
            throw new ForbiddenAccessException();
        }

        return publisher;
    }
}
