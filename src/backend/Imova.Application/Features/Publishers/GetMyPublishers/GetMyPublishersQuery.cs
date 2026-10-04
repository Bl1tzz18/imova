using Imova.Contracts.Publishers;
using MediatR;

namespace Imova.Application.Features.Publishers.GetMyPublishers;

// The caller's own publisher (one per user — see Publisher), created on the spot if it's missing.
public record GetMyPublishersQuery(Guid UserId) : IRequest<List<PublisherDto>>;
