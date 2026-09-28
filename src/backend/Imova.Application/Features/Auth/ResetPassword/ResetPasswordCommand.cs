using MediatR;

namespace Imova.Application.Features.Auth.ResetPassword;

// Token is exactly as it appears in the emailed link (Base64Url — see AccountTokens).
public record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest;
