using FluentValidation;

namespace Imova.Application.Features.Properties.GetPendingReviewProperties;

public class GetPendingReviewPropertiesValidator : AbstractValidator<GetPendingReviewPropertiesQuery>
{
    public GetPendingReviewPropertiesValidator()
    {
        RuleFor(c => c.Page).GreaterThanOrEqualTo(1);
        RuleFor(c => c.PageSize).InclusiveBetween(1, 100);
    }
}
