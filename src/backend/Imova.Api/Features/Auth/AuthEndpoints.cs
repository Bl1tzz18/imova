using Imova.Api.Features.Auth.Config;
using Imova.Api.Features.Auth.GoogleLogin;
using Imova.Api.Features.Auth.Login;
using Imova.Api.Features.Auth.Register;

namespace Imova.Api.Features.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapRegister();
        app.MapLogin();
        app.MapGoogleLogin();
        app.MapAuthConfig();
    }
}
