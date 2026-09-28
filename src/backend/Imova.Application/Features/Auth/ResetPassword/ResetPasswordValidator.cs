using FluentValidation;
using Imova.Application.Common.Validation;

namespace Imova.Application.Features.Auth.ResetPassword;

public class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(c => c.Email).NotEmpty().MaximumLength(256);
        RuleFor(c => c.Token).NotEmpty().MaximumLength(2000);
        RuleFor(c => c.NewPassword).StrongPassword();
    }
}
