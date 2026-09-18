using Imova.Contracts.Auth;
using MediatR;

namespace Imova.Application.Features.Auth.UpdatePhoneNumber;

public record UpdatePhoneNumberCommand(Guid UserId, string PhoneNumber) : IRequest<AuthUserDto>;
