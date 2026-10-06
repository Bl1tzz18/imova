using Imova.Contracts.Agencies;
using MediatR;

namespace Imova.Application.Features.Agencies.GetAgency;

// Public. Null (404) when it doesn't exist, or it's deactivated and the caller is neither a member
// nor an admin.
public record GetAgencyQuery(Guid AgencyId, Guid? UserId, bool IsAdmin) : IRequest<AgencyDto?>;
