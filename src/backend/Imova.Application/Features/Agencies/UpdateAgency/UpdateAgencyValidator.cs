using FluentValidation;

namespace Imova.Application.Features.Agencies.UpdateAgency;

public class UpdateAgencyValidator : AgencyProfileValidator<UpdateAgencyCommand>
{
    public UpdateAgencyValidator()
    {
        RuleFor(c => c.AgencyId).NotEmpty();
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Email).NotEmpty();
    }
}
