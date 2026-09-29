using Imova.Contracts.Admins;
using MediatR;

namespace Imova.Application.Features.Admins.GetAdmins;

public record GetAdminsQuery(Guid ActorUserId) : IRequest<IReadOnlyList<AdminUserDto>>;
