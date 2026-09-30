using FluentValidation;
using Imova.Application.Common;
using Imova.Domain.Listings;

namespace Imova.Application.Features.Listings.ReportListing;

public class ReportListingValidator : AbstractValidator<ReportListingCommand>
{
    public ReportListingValidator()
    {
        RuleFor(c => c.ListingId).NotEmpty();
        RuleFor(c => c.Reason).IsInEnum();
        RuleFor(c => c.Details).MaximumLength(ListingReport.MaxDetailsLength);
        RuleFor(c => c.Details)
            .NotEmpty().WithMessage("Describe the problem when the reason is Other.").WithErrorCode(ErrorCodes.ReportDetailsRequired)
            .When(c => c.Reason == ListingReportReason.Other);
    }
}
