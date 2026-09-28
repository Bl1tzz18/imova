using Imova.Api.Features.Auth.ChangePassword;
using Imova.Api.Features.Auth.Config;
using Imova.Api.Features.Auth.ConfirmEmail;
using Imova.Api.Features.Auth.ForgotPassword;
using Imova.Api.Features.Auth.GetProfile;
using Imova.Api.Features.Auth.GoogleLogin;
using Imova.Api.Features.Auth.Login;
using Imova.Api.Features.Auth.Logout;
using Imova.Api.Features.Auth.RefreshSession;
using Imova.Api.Features.Auth.Register;
using Imova.Api.Features.Auth.ResendConfirmation;
using Imova.Api.Features.Auth.ResetPassword;
using Imova.Api.Features.Auth.SignOutOtherSessions;
using Imova.Api.Features.Auth.UpdatePhoneNumber;
using Imova.Api.Features.Auth.UpdateProfile;

namespace Imova.Api.Features.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapRegister();
        app.MapLogin();
        app.MapGoogleLogin();
        app.MapAuthConfig();
        app.MapUpdatePhoneNumber();
        app.MapGetProfile();
        app.MapUpdateProfile();
        app.MapChangePassword();
        app.MapForgotPassword();
        app.MapResetPassword();
        app.MapConfirmEmail();
        app.MapResendConfirmation();
        app.MapSignOutOtherSessions();
        app.MapRefreshSession();
        app.MapLogout();
    }
}
