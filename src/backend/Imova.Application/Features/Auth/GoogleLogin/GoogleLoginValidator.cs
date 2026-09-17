using FluentValidation;

namespace Imova.Application.Features.Auth.GoogleLogin;

public class GoogleLoginValidator : AbstractValidator<GoogleLoginCommand>
{
    public GoogleLoginValidator()
    {
        RuleFor(c => c.IdToken).NotEmpty();
    }
}
