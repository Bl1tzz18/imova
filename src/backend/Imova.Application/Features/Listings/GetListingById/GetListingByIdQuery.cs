using Imova.Contracts.Listings;
using MediatR;

namespace Imova.Application.Features.Listings.GetListingById;

public record GetListingByIdQuery(Guid Id, Guid? CurrentUserId = null, bool IsAdmin = false) : IRequest<ListingDto?>;
