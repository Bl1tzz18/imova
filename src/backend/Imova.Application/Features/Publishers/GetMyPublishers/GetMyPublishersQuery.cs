using Imova.Contracts.Publishers;
using MediatR;

namespace Imova.Application.Features.Publishers.GetMyPublishers;

// The identities the caller can publish a listing under — their Individual publisher, plus their
// Agency one if they've created it. Backs the publisher picker on the listing form.
public record GetMyPublishersQuery(Guid UserId) : IRequest<List<PublisherDto>>;
