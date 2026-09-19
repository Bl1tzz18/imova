using FluentValidation;

namespace Imova.Application.Features.Properties.MarkAsSold;

public class MarkAsSoldValidator : AbstractValidator<MarkAsSoldCommand>
{
    public MarkAsSoldValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
    }
}
