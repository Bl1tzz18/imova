using FluentValidation;
using Imova.Application.Common.Validation;

namespace Imova.Application.Features.Auth.Register;

public class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(c => c.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
        RuleFor(c => c.DisplayName).MaximumLength(200);
        RuleFor(c => c.PhoneNumber).ValidPhoneNumber();
    }
}
