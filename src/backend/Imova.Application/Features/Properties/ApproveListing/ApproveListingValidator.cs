using FluentValidation;

namespace Imova.Application.Features.Properties.ApproveListing;

public class ApproveListingValidator : AbstractValidator<ApproveListingCommand>
{
    public ApproveListingValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
    }
}
