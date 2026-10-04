using FluentValidation;

namespace Imova.Application.Features.Agencies.CreateAgency;

public class CreateAgencyValidator : AgencyProfileValidator<CreateAgencyCommand>
{
    public CreateAgencyValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
    }
}
