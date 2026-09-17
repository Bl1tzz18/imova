using Imova.Contracts.Auth;
using MediatR;

namespace Imova.Application.Features.Auth.Login;

public record LoginCommand(string Email, string Password) : IRequest<AuthResultDto>;
