using Imova.Contracts.Auth;
using MediatR;

namespace Imova.Application.Features.Users.UploadProfilePicture;

public record UploadProfilePictureCommand(Guid UserId, byte[] Content) : IRequest<UserProfileDto>;
