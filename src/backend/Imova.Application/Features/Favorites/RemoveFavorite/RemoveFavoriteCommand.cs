using MediatR;

namespace Imova.Application.Features.Favorites.RemoveFavorite;

public record RemoveFavoriteCommand(Guid UserId, Guid PropertyId) : IRequest;
