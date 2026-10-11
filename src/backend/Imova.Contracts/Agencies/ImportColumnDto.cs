namespace Imova.Contracts.Agencies;

// GET /api/v1/agencies/{id}/import/columns?propertyType= — the CSV's columns for one property type,
// for the import page's guide. Kind: Text | Integer | Decimal | YesNo | Choice | Date | List.
// Requirement: Required | Conditional (required for some value of another column) | Optional.
// Scope: Both | Rent (rental terms). Choices: the accepted values of a Choice column, and of the
// amenities / proximities lists (their keys). Example: the value in the template's example row.
public record ImportColumnDto(
    string Name,
    string Kind,
    string Requirement,
    string Scope,
    IReadOnlyList<string>? Choices,
    int? MaxLength,
    string? Example);
