using FluentValidation;
using Imova.Application.Common.Validation;

namespace Imova.Application.Features.Auth.Register;

public class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(c => c.Password).StrongPassword();
        RuleFor(c => c.DisplayName).MaximumLength(200);
        RuleFor(c => c.PhoneNumber).ValidPhoneNumber();
    }
}
