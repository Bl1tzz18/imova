using FluentValidation;

namespace Imova.Application.Features.Auth.RefreshSession;

public class RefreshSessionValidator : AbstractValidator<RefreshSessionCommand>
{
    public RefreshSessionValidator()
    {
        RuleFor(c => c.RefreshToken).NotEmpty().MaximumLength(200);
    }
}
