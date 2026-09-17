using Imova.Contracts.Auth;
using MediatR;

namespace Imova.Application.Features.Auth.GoogleLogin;

public record GoogleLoginCommand(string IdToken) : IRequest<AuthResultDto>;
