using FluentValidation;

namespace Imova.Application.Features.Auth.ConfirmEmail;

public class ConfirmEmailValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Token).NotEmpty().MaximumLength(2000);
    }
}
