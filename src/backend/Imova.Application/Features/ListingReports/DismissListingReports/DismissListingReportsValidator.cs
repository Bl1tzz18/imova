using FluentValidation;
using Imova.Domain.Listings;

namespace Imova.Application.Features.ListingReports.DismissListingReports;

public class DismissListingReportsValidator : AbstractValidator<DismissListingReportsCommand>
{
    public DismissListingReportsValidator()
    {
        RuleFor(c => c.ListingId).NotEmpty();
        RuleFor(c => c.Note).MaximumLength(ListingReport.MaxNoteLength);
    }
}
