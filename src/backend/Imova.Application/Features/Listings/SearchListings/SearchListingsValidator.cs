using FluentValidation;
using Imova.Domain.Listings;

namespace Imova.Application.Features.Listings.SearchListings;

public class SearchListingsValidator : AbstractValidator<SearchListingsQuery>
{
    public SearchListingsValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, SearchFilterRules.MaxPageSize);
        RuleFor(q => q.Sort).IsInEnum();
        RuleFor(q => q.TransactionType).IsInEnum();
        RuleForEach(q => q.PropertyTypes).IsInEnum();

        Range(q => q.MinPriceEur, q => q.MaxPriceEur, nameof(SearchListingsQuery.MinPriceEur), nameof(SearchListingsQuery.MaxPriceEur));
        Range(q => q.MinAreaM2, q => q.MaxAreaM2, nameof(SearchListingsQuery.MinAreaM2), nameof(SearchListingsQuery.MaxAreaM2));
        Range(q => q.MinRooms, q => q.MaxRooms, nameof(SearchListingsQuery.MinRooms), nameof(SearchListingsQuery.MaxRooms));
        Range(q => q.MinFloor, q => q.MaxFloor, nameof(SearchListingsQuery.MinFloor), nameof(SearchListingsQuery.MaxFloor));
        Range(q => q.MinLandAreaM2, q => q.MaxLandAreaM2, nameof(SearchListingsQuery.MinLandAreaM2), nameof(SearchListingsQuery.MaxLandAreaM2));
        RuleFor(q => q.MinPriceEur).GreaterThanOrEqualTo(0);
        RuleFor(q => q.MinAreaM2).GreaterThanOrEqualTo(0);
        RuleFor(q => q.MinRooms).GreaterThanOrEqualTo(0);
        RuleFor(q => q.MinBathrooms).GreaterThanOrEqualTo(0);
        RuleFor(q => q.MaxLeasePeriodMonths).InclusiveBetween(1, 120);

        RuleFor(q => q)
            .Must(q => q.LocalitateId is null || q.ChisinauSectorId is null)
            .WithMessage("Pick a localitate or a Chișinău sector, not both.")
            .OverridePropertyName(nameof(SearchListingsQuery.ChisinauSectorId));

        // A type-specific filter needs exactly one property type, and one it belongs to.
        RuleFor(q => q).Custom((q, context) =>
        {
            foreach (var filter in SearchFilterRules.UsedTypeSpecificFilters(q))
            {
                var types = SearchFilterRules.TypeSpecificFilters[filter];
                if (q.PropertyTypes.Distinct().Count() != 1 || !types.Contains(q.PropertyTypes[0]))
                {
                    context.AddFailure(
                        filter,
                        $"The '{filter}' filter needs exactly one property type: {string.Join(" or ", types)}.");
                }
            }
        });

        RuleFor(q => q)
            .Must(q => !(q.TransactionType == TransactionType.Sale && SearchFilterRules.UsesRentalFilters(q)))
            .WithMessage("Rental filters (pets, utilities, lease period) only apply to rentals.")
            .OverridePropertyName(nameof(SearchListingsQuery.TransactionType));
    }

    private void Range<T>(Func<SearchListingsQuery, T?> min, Func<SearchListingsQuery, T?> max, string minName, string maxName)
        where T : struct, IComparable<T>
    {
        RuleFor(q => q)
            .Must(q => min(q) is not { } lo || max(q) is not { } hi || lo.CompareTo(hi) <= 0)
            .WithMessage($"{minName} can't be greater than {maxName}.")
            .OverridePropertyName(minName);
    }
}
