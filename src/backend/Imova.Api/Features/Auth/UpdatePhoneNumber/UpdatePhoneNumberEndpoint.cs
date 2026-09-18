using System.Security.Claims;
using Imova.Api.Common;
using Imova.Application.Features.Auth.UpdatePhoneNumber;
using MediatR;

namespace Imova.Api.Features.Auth.UpdatePhoneNumber;

public static class UpdatePhoneNumberEndpoint
{
    public static void MapUpdatePhoneNumber(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/v1/auth/phone", async (
            UpdatePhoneNumberRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdatePhoneNumberCommand(user.GetUserId(), request.PhoneNumber);
            return Results.Ok(await sender.Send(command, cancellationToken));
        }).RequireAuthorization();
    }
}

public record UpdatePhoneNumberRequest(string PhoneNumber);
