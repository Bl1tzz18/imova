using Imova.Contracts.Auth;
using MediatR;

namespace Imova.Application.Features.Auth.ChangePassword;

// Changing the password signs out every session (see SessionStamp); the result carries a new token
// so the one that made the change (SessionId, from the caller's "sid" claim) stays signed in.
public record ChangePasswordCommand(Guid UserId, string? CurrentPassword, string NewPassword, Guid? SessionId = null)
    : IRequest<ChangePasswordResultDto>;
