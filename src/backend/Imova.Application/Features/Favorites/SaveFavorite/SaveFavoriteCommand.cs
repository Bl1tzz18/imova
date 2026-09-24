using MediatR;

namespace Imova.Application.Features.Favorites.SaveFavorite;

// Returns false when ListingId doesn't exist (the endpoint maps that to 404) — true otherwise,
// whether this call actually created the row or it was already saved (idempotent).
public record SaveFavoriteCommand(Guid UserId, Guid ListingId) : IRequest<bool>;
