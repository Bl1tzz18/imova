using System.Text.Json;
using Imova.Application.Features.Listings.Attributes;
using Imova.Domain.Agencies;
using Imova.Domain.Listings;
using Imova.Domain.Properties;
using Imova.Domain.Properties.Attributes;

namespace Imova.Application.Features.Agencies.Import;

public enum ImportColumnKind
{
    Text = 1,
    Integer = 2,
    // A dot or a comma before the decimals ("58.5" or "58,5"); no thousands separators.
    Decimal = 3,
    // da / nu (yes / no, true / false, 1 / 0 are read too).
    YesNo = 4,
    // One of Choices, written as listed (case doesn't matter).
    Choice = 5,
    // yyyy-mm-dd.
    Date = 6,
    // Several values separated by "|".
    List = 7,
}

public enum ImportColumnRequirement
{
    Required = 1,
    // Required only for some value of another column (e.g. heating details for a boiler).
    Conditional = 2,
    Optional = 3,
}

// Which offers a column is for: rental terms only mean something on a rental.
public enum ImportColumnScope
{
    Both = 1,
    Rent = 2,
}

// One column of an agency's listings CSV. Name is the header (snake_case, English — stable across
// languages); AttributeKey is the TypeSpecificAttributes JSON key a column fills (null for the rest);
// Choices are the accepted values of a Choice column (amenities / proximities: their keys, filled in
// from the database where they're listed).
public sealed record ImportColumn(
    string Name,
    ImportColumnKind Kind,
    ImportColumnRequirement Requirement,
    ImportColumnScope Scope = ImportColumnScope.Both,
    IReadOnlyList<string>? Choices = null,
    int? MaxLength = null,
    string? AttributeKey = null);

// The columns of the CSV for one property type — one template per type, so a file only has the
// columns that mean something for what it lists. The listing's own fields come first, then the
// type's details (read off its attributes record, required-ness off PropertyAttributesValidator, so a
// field added to the listing form becomes a column with no change here), then where, photos,
// extras, rental terms, contact and author.
public static class ImportColumns
{
    public const string ExternalRef = "external_ref";
    public const string PropertyTypeColumn = "property_type";
    public const string DealType = "deal_type";
    public const string Status = "status";
    public const string Title = "title";
    public const string Description = "description";
    public const string Price = "price";
    public const string Currency = "currency";
    public const string PriceNegotiable = "price_negotiable";
    public const string AreaM2 = "area_m2";
    public const string YearBuilt = "year_built";
    public const string Condition = "condition";
    public const string City = "city";
    public const string Locality = "locality";
    public const string Sector = "sector";
    public const string Street = "street";
    public const string BuildingNumber = "building_number";
    public const string Latitude = "latitude";
    public const string Longitude = "longitude";
    public const string ImageUrls = "image_urls";
    public const string Amenities = "amenities";
    public const string Proximities = "proximities";
    public const string MinLeaseMonths = "min_lease_months";
    public const string Deposit = "deposit";
    public const string UtilitiesIncluded = "utilities_included";
    public const string AvailableFrom = "available_from";
    public const string PetsAllowed = "pets_allowed";
    public const string ContactName = "contact_name";
    public const string ContactPhone = "contact_phone";
    public const string ContactEmail = "contact_email";
    public const string AgentEmail = "agent_email";

    public const string Sale = "sale";
    public const string Rent = "rent";
    public const string Active = "active";
    public const string Inactive = "inactive";

    // How many photo addresses a row may give (the first one is the cover).
    public const int MaxImageUrls = 20;

    public const char ListSeparator = '|';

    private static readonly IReadOnlyDictionary<PropertyType, IReadOnlyList<ImportColumn>> ByType =
        Enum.GetValues<PropertyType>().ToDictionary(type => type, Build);

    public static IReadOnlyList<ImportColumn> For(PropertyType propertyType) => ByType[propertyType];

    // Every column of every type, by name — what a file's header may contain.
    public static readonly IReadOnlyDictionary<string, ImportColumn> All =
        ByType.Values.SelectMany(columns => columns)
            .GroupBy(c => c.Name)
            .ToDictionary(g => g.Key, g => g.First());

    // "LivingAreaM2" → "living_area_m2" (the header).
    public static string ColumnName(string propertyName) => JsonNamingPolicy.SnakeCaseLower.ConvertName(propertyName);

    private static IReadOnlyList<ImportColumn> Build(PropertyType type)
    {
        var columns = new List<ImportColumn>
        {
            new(ExternalRef, ImportColumnKind.Text, ImportColumnRequirement.Required, MaxLength: ListingImportRow.MaxExternalRefLength),
            // In every template (with that template's type), so one file may mix types: each row is
            // read against its own type's columns.
            new(PropertyTypeColumn, ImportColumnKind.Choice, ImportColumnRequirement.Required, Choices: Enum.GetNames<PropertyType>()),
            new(DealType, ImportColumnKind.Choice, ImportColumnRequirement.Required, Choices: [Sale, Rent]),
            new(Status, ImportColumnKind.Choice, ImportColumnRequirement.Optional, Choices: [Active, Inactive]),
            new(Title, ImportColumnKind.Text, ImportColumnRequirement.Required, MaxLength: 200),
            new(Description, ImportColumnKind.Text, ImportColumnRequirement.Required, MaxLength: 4000),
            new(Price, ImportColumnKind.Decimal, ImportColumnRequirement.Required),
            new(Currency, ImportColumnKind.Choice, ImportColumnRequirement.Required, Choices: Enum.GetNames<Currency>()),
            new(PriceNegotiable, ImportColumnKind.YesNo, ImportColumnRequirement.Optional),
            new(AreaM2, ImportColumnKind.Decimal, ImportColumnRequirement.Required),
        };

        // The listing form's rules: no year for land; the general condition only for a garage or a
        // room (the others have their own finish condition among the details).
        if (type != PropertyType.Land)
        {
            columns.Add(new(YearBuilt, ImportColumnKind.Integer, ImportColumnRequirement.Optional));
        }

        if (type is PropertyType.Garage or PropertyType.Room)
        {
            columns.Add(new(Condition, ImportColumnKind.Choice, ImportColumnRequirement.Optional, Choices: Enum.GetNames<PropertyCondition>()));
        }

        columns.AddRange(AttributeColumns(type));

        columns.AddRange(
        [
            // The raion / municipality (Chișinău counts as one), as in the listing form.
            new(City, ImportColumnKind.Text, ImportColumnRequirement.Required),
            new(Locality, ImportColumnKind.Text, ImportColumnRequirement.Optional),
            // Chișinău's sectors only; not together with a locality.
            new(Sector, ImportColumnKind.Text, ImportColumnRequirement.Optional),
            new(Street, ImportColumnKind.Text, ImportColumnRequirement.Required, MaxLength: 200),
            new(BuildingNumber, ImportColumnKind.Text, ImportColumnRequirement.Optional, MaxLength: 20),
            // Both or neither; used when they fall inside Moldova near the locality, else geocoded.
            new(Latitude, ImportColumnKind.Decimal, ImportColumnRequirement.Optional),
            new(Longitude, ImportColumnKind.Decimal, ImportColumnRequirement.Optional),
            // 3 to MaxImageUrls public http(s) addresses of photos; the first is the cover.
            new(ImageUrls, ImportColumnKind.List, ImportColumnRequirement.Required),
            new(Amenities, ImportColumnKind.List, ImportColumnRequirement.Optional),
            new(Proximities, ImportColumnKind.List, ImportColumnRequirement.Optional),
            new(MinLeaseMonths, ImportColumnKind.Integer, ImportColumnRequirement.Optional, ImportColumnScope.Rent),
            new(Deposit, ImportColumnKind.Decimal, ImportColumnRequirement.Optional, ImportColumnScope.Rent),
            new(UtilitiesIncluded, ImportColumnKind.YesNo, ImportColumnRequirement.Optional, ImportColumnScope.Rent),
            new(AvailableFrom, ImportColumnKind.Date, ImportColumnRequirement.Optional, ImportColumnScope.Rent),
        ]);

        // A required Yes/No for a rented home, not asked for anything else (RentalDetails.PetsApplyTo).
        if (RentalDetails.PetsApplyTo(type))
        {
            columns.Add(new(PetsAllowed, ImportColumnKind.YesNo, ImportColumnRequirement.Conditional, ImportColumnScope.Rent));
        }

        columns.AddRange(
        [
            // Empty: the agency's own name, phone and email.
            new(ContactName, ImportColumnKind.Text, ImportColumnRequirement.Optional, MaxLength: 100),
            new(ContactPhone, ImportColumnKind.Text, ImportColumnRequirement.Optional, MaxLength: 20),
            new(ContactEmail, ImportColumnKind.Text, ImportColumnRequirement.Optional, MaxLength: 254),
            // A member's account email: the listing's author. Empty (or not a member): whoever uploads.
            new(AgentEmail, ImportColumnKind.Text, ImportColumnRequirement.Optional, MaxLength: 254),
        ]);

        return columns;
    }

    private static IEnumerable<ImportColumn> AttributeColumns(PropertyType type) =>
        PropertyAttributes.EmptyFor(type).GetType().GetProperties()
            .Select(p =>
            {
                var valueType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                var kind = valueType.IsEnum ? ImportColumnKind.Choice
                    : valueType == typeof(bool) ? ImportColumnKind.YesNo
                    : valueType == typeof(int) ? ImportColumnKind.Integer
                    : valueType == typeof(decimal) ? ImportColumnKind.Decimal
                    : ImportColumnKind.Text;
                var requirement = PropertyAttributesValidator.RequirementOf(type, p.Name) switch
                {
                    AttributeRequirement.Required => ImportColumnRequirement.Required,
                    AttributeRequirement.Conditional => ImportColumnRequirement.Conditional,
                    _ => ImportColumnRequirement.Optional,
                };

                return new ImportColumn(
                    ColumnName(p.Name),
                    kind,
                    requirement,
                    Choices: valueType.IsEnum ? Enum.GetNames(valueType) : null,
                    AttributeKey: JsonNamingPolicy.CamelCase.ConvertName(p.Name));
            });
}
