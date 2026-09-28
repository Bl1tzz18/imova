using MediatR;

namespace Imova.Application.Features.Auth.ResendConfirmation;

// The signed-in user asks for another confirmation link. UserId always comes from the caller's JWT.
public record ResendConfirmationCommand(Guid UserId) : IRequest;
