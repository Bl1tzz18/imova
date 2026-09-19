using FluentValidation;

namespace Imova.Application.Features.Properties.MarkAsRented;

public class MarkAsRentedValidator : AbstractValidator<MarkAsRentedCommand>
{
    public MarkAsRentedValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
    }
}
