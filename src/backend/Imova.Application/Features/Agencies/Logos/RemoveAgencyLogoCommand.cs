using Imova.Contracts.Agencies;
using MediatR;

namespace Imova.Application.Features.Agencies.Logos;

// Null when the agency doesn't exist or the caller may not see it (404). No logo: nothing to do.
public record RemoveAgencyLogoCommand(Guid AgencyId, Guid UserId, bool IsAdmin) : IRequest<AgencyDto?>;
