using Imova.Contracts.Auth;
using MediatR;

namespace Imova.Application.Features.Auth.Register;

public record RegisterCommand(string Email, string Password, string? DisplayName, string PhoneNumber) : IRequest<AuthResultDto>;
