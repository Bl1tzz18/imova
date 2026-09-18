using FluentValidation;

namespace Imova.Application.Features.Auth.ChangePassword;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(c => c.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(100);
    }
}
