using FluentValidation;

namespace Imova.Application.Features.Properties.SubmitForReview;

public class SubmitForReviewValidator : AbstractValidator<SubmitForReviewCommand>
{
    public SubmitForReviewValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
    }
}
