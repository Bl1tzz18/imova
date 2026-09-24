using FluentValidation;

namespace Imova.Application.Features.Listings.GetListings;

public class GetListingsValidator : AbstractValidator<GetListingsQuery>
{
    public GetListingsValidator()
    {
        RuleFor(q => q.MinPriceEur).GreaterThanOrEqualTo(0);
        RuleFor(q => q.MaxPriceEur).GreaterThanOrEqualTo(0);
        RuleFor(q => q.MaxPriceEur)
            .GreaterThanOrEqualTo(q => q.MinPriceEur!.Value)
            .When(q => q.MinPriceEur.HasValue && q.MaxPriceEur.HasValue);
    }
}
