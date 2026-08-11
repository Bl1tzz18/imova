using FluentValidation;

namespace Imova.Api.Features.Properties.CreateProperty;

public class CreatePropertyValidator : AbstractValidator<CreatePropertyCommand>
{
    public CreatePropertyValidator()
    {
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Price).GreaterThan(0);
        RuleFor(c => c.Currency).NotEmpty().Length(3);
        RuleFor(c => c.City).NotEmpty().MaximumLength(100);
        RuleFor(c => c.District).NotEmpty().MaximumLength(100);
    }
}
