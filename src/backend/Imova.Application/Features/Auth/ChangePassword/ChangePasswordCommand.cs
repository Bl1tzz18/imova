using Imova.Contracts.Auth;
using MediatR;

namespace Imova.Application.Features.Auth.ChangePassword;

public record ChangePasswordCommand(Guid UserId, string? CurrentPassword, string NewPassword) : IRequest<UserProfileDto>;
