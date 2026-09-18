using Imova.Contracts.Auth;
using MediatR;

namespace Imova.Application.Features.Auth.UpdateProfile;

public record UpdateProfileCommand(Guid UserId, string? DisplayName, string PhoneNumber) : IRequest<UserProfileDto>;
