using Imova.Contracts.Listings;
using MediatR;

namespace Imova.Application.Features.Favorites.GetFavorites;

public record GetFavoritesQuery(Guid UserId) : IRequest<List<ListingDto>>;
