using FluentValidation;

namespace Imova.Application.Features.Listings.GetPendingReviewListings;

public class GetPendingReviewListingsValidator : AbstractValidator<GetPendingReviewListingsQuery>
{
    public GetPendingReviewListingsValidator()
    {
        RuleFor(c => c.Page).GreaterThanOrEqualTo(1);
        RuleFor(c => c.PageSize).InclusiveBetween(1, 100);
    }
}
