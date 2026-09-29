using FluentValidation;

namespace Imova.Application.Features.Listings.GetPendingReviewListings;

public class GetPendingReviewListingsValidator : AbstractValidator<GetPendingReviewListingsQuery>
{
    public GetPendingReviewListingsValidator()
    {
        RuleFor(c => c.Page).GreaterThanOrEqualTo(1);
        RuleFor(c => c.PageSize).InclusiveBetween(1, 100);
        RuleFor(c => c.Status).Must(AdminListingStatuses.Allowed.Contains)
            .WithMessage("Status must be PendingReview, Active or Suspended.");
        RuleFor(c => c.Search).MaximumLength(200);
    }
}
