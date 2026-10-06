using Imova.Contracts.Agencies;
using MediatR;

namespace Imova.Application.Features.Agencies.Logos;

// A new logo, uploaded through the API like a profile picture (one small file — no direct-to-storage
// upload). Null when the agency doesn't exist or the caller may not see it (404).
public record UploadAgencyLogoCommand(Guid AgencyId, Guid UserId, bool IsAdmin, byte[] Content) : IRequest<AgencyDto?>;
