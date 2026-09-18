using Imova.Contracts.Auth;
using MediatR;

namespace Imova.Application.Features.Users.RemoveProfilePicture;

public record RemoveProfilePictureCommand(Guid UserId) : IRequest<UserProfileDto>;
