using Imova.Contracts.Properties;
using MediatR;

namespace Imova.Application.Features.Properties.SubmitForReview;

// RequestingUserId/IsAdmin are always supplied by the endpoint from the caller's JWT claims —
// never trust these from the request body.
public record SubmitForReviewCommand(Guid Id, Guid RequestingUserId, bool IsAdmin) : IRequest<PropertyDto?>;
