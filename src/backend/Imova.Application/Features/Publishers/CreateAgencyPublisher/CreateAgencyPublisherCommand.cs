using Imova.Contracts.Publishers;
using MediatR;

namespace Imova.Application.Features.Publishers.CreateAgencyPublisher;

// UserId is always supplied by the endpoint from the caller's JWT claims. Email is optional —
// defaults to the account's own email when omitted.
public record CreateAgencyPublisherCommand(
    Guid UserId,
    string DisplayName,
    string Phone,
    string? Email,
    string? LogoUrl,
    string? Bio) : IRequest<PublisherDto>;
