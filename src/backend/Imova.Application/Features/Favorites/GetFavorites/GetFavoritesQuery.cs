using Imova.Contracts.Properties;
using MediatR;

namespace Imova.Application.Features.Favorites.GetFavorites;

public record GetFavoritesQuery(Guid UserId) : IRequest<List<PropertyDto>>;
