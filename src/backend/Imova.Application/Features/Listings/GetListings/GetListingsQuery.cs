using Imova.Contracts.Listings;
using Imova.Domain.Listings;
using Imova.Domain.Properties;
using MediatR;

namespace Imova.Application.Features.Listings.GetListings;

// Price bounds are in EUR and compared against Price.PriceEur, so a listing priced in MDL/USD
// filters the same way as one priced in EUR.
public record GetListingsQuery(
    PropertyType? PropertyType = null,
    TransactionType? TransactionType = null,
    decimal? MinPriceEur = null,
    decimal? MaxPriceEur = null,
    Guid? CurrentUserId = null) : IRequest<List<ListingDto>>;
