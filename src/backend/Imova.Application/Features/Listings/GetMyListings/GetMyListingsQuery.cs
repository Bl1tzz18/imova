using Imova.Contracts.Listings;
using MediatR;

namespace Imova.Application.Features.Listings.GetMyListings;

// Every listing published under any of the user's publishers (Individual or Agency), in every
// status — the owner needs to see and manage all of them, not just the publicly visible ones.
public record GetMyListingsQuery(Guid UserId) : IRequest<List<ListingDto>>;
