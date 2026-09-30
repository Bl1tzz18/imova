using FluentValidation;

namespace Imova.Application.Features.ListingReports.GetReportedListings;

public class GetReportedListingsValidator : AbstractValidator<GetReportedListingsQuery>
{
    public GetReportedListingsValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 50);
    }
}
