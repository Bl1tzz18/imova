using FluentValidation;

namespace Imova.Application.Features.Listings.SubmitListingForReview;

public class SubmitListingForReviewValidator : AbstractValidator<SubmitListingForReviewCommand>
{
    public SubmitListingForReviewValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
    }
}
