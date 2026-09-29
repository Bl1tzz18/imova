using FluentValidation;

namespace Imova.Application.Features.Admins.GrantAdmin;

public class GrantAdminValidator : AbstractValidator<GrantAdminCommand>
{
    public GrantAdminValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(c => c.Password).MaximumLength(200);
    }
}
