using FluentValidation;
using Imova.Application.Common.Validation;

namespace Imova.Application.Features.Auth.ChangePassword;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(c => c.NewPassword).StrongPassword();
    }
}
