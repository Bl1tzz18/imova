using Imova.Contracts.Agencies;
using MediatR;

namespace Imova.Application.Features.Agencies.UpdateAgency;

// Replaces the agency's editable details (never its verification, status or logo). Renaming it
// moves it to a new slug; the old one keeps working as a redirect (AgencySlugHistory). Null when
// the agency doesn't exist or the caller may not see it (404); an Agent or outsider who can see it
// gets 403.
public record UpdateAgencyCommand(
    Guid AgencyId,
    Guid UserId,
    bool IsAdmin,
    string Name,
    string Phone,
    string? Email,
    string? Bio,
    string? Website,
    string? Address,
    Guid? RaionId) : IRequest<AgencyDto?>, IAgencyProfileCommand;
