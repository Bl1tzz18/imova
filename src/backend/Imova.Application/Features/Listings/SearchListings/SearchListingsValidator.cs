using FluentValidation;
using Imova.Domain.Listings;

namespace Imova.Application.Features.Listings.SearchListings;

public class SearchListingsValidator : AbstractValidator<SearchListingsQuery>
{
    public SearchListingsValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize)
            .Must((q, size) => size >= 1 && size <= (q.OnlyWithCoordinates ? SearchFilterRules.MaxMapResults : SearchFilterRules.MaxPageSize))
            .WithMessage(q => $"Page size must be between 1 and {(q.OnlyWithCoordinates ? SearchFilterRules.MaxMapResults : SearchFilterRules.MaxPageSize)}.");
        RuleFor(q => q.Sort).IsInEnum();
        RuleFor(q => q.TransactionType).IsInEnum();
        RuleForEach(q => q.PropertyTypes).IsInEnum();

        Range(q => q.MinPriceEur, q => q.MaxPriceEur, nameof(SearchListingsQuery.MinPriceEur), nameof(SearchListingsQuery.MaxPriceEur));
        Range(q => q.MinAreaM2, q => q.MaxAreaM2, nameof(SearchListingsQuery.MinAreaM2), nameof(SearchListingsQuery.MaxAreaM2));
        Range(q => q.MinYearBuilt, q => q.MaxYearBuilt, nameof(SearchListingsQuery.MinYearBuilt), nameof(SearchListingsQuery.MaxYearBuilt));
        RuleForEach(q => q.Conditions).IsInEnum();
        RuleFor(q => q.MinPriceEur).GreaterThanOrEqualTo(0);
        RuleFor(q => q.MinAreaM2).GreaterThanOrEqualTo(0);
        RuleFor(q => q.MaxLeasePeriodMonths).InclusiveBetween(1, 120);

        RuleFor(q => q)
            .Must(q => q.LocalitateId is null || q.ChisinauSectorId is null)
            .WithMessage("Pick a localitate or a Chișinău sector, not both.")
            .OverridePropertyName(nameof(SearchListingsQuery.ChisinauSectorId));

        // An attribute filter needs exactly one property type, and one whose attributes have the field.
        RuleFor(q => q).Custom((q, context) =>
        {
            var types = q.PropertyTypes.Distinct().ToList();
            foreach (var filter in q.AttributeFilters)
            {
                if (types.Count != 1 || !AttributeSearchSchema.FieldsFor(types[0]).ContainsKey(filter.Field))
                {
                    context.AddFailure(
                        filter.Field,
                        $"The '{filter.Field}' filter needs exactly one property type: {string.Join(" or ", AttributeSearchSchema.TypesWith(filter.Field))}.");
                }
                else if (filter is { Min: { } min, Max: { } max } && min > max)
                {
                    context.AddFailure(filter.Field, $"The minimum {filter.Field} can't be greater than the maximum.");
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
