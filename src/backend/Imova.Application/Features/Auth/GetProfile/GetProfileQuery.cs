using Imova.Contracts.Auth;
using MediatR;

namespace Imova.Application.Features.Auth.GetProfile;

public record GetProfileQuery(Guid UserId) : IRequest<UserProfileDto>;
