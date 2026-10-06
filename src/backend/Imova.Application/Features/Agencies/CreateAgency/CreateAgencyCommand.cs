using Imova.Contracts.Agencies;
using MediatR;

namespace Imova.Application.Features.Agencies.CreateAgency;

// A new agency with the caller (UserId, from the JWT) as its Owner. Email is optional — the
// account's own email is used when it's left out.
public record CreateAgencyCommand(
    Guid UserId,
    string Name,
    string Phone,
    string? Email,
    string? Bio,
    string? Website,
    string? Address,
    Guid? RaionId) : IRequest<AgencyDto>, IAgencyProfileCommand;
