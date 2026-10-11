using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentValidation;
using FluentValidation.Results;
using Imova.Application.Common;
using Imova.Application.Features.Listings;
using Imova.Domain.Agencies;
using Imova.Domain.Listings;
using Imova.Domain.Properties;

namespace Imova.Application.Features.Agencies.Import;

// What a valid row becomes: the listing payload (validated like the form's), and what the import job
// still has to do with it — create or update by ExternalRef, as Active or deactivated, under which
// author, with which photos (downloaded in step 4) and where on the map (ImportGeolocator).
public sealed record ImportRowPlan(
    int RowNumber,
    string ExternalRef,
    ImportListingCommand Command,
    bool Inactive,
    Guid AuthorUserId,
    // agent_email named a member: on a re-import the listing moves to them; otherwise its author stays.
    bool AuthorNamed,
    ImportLocation Location,
    double? Latitude,
    double? Longitude,
    IReadOnlyList<string> ImageUrls,
    IReadOnlyList<ListingImportProblem> Warnings);

public sealed record ImportRowResult(int RowNumber, string? ExternalRef, ImportRowPlan? Plan, IReadOnlyList<ListingImportProblem> Problems);

// Turns one CSV row into an ImportRowPlan, or into the list of everything wrong with it (so a row can
// be fixed in one go). First every cell is read on its own (ImportValues, against the row's type's
// columns — ImportColumns), then, if they all read, the payload goes through the listing form's own
// rules (ListingWriteValidator) and their failures are reported against the CSV's columns.
// One parser per import: it also refuses a second row with the same external_ref.
public sealed class ImportRowParser(ImportContext context, IValidator<ImportListingCommand> validator)
{
    private static readonly IReadOnlyDictionary<string, string> PlaceholderParams = new Dictionary<string, string>
    {
        ["MinLength"] = "min",
        ["MaxLength"] = "max",
        ["From"] = "from",
        ["To"] = "to",
        ["ComparisonValue"] = "value",
    };

    private readonly HashSet<string> _externalRefs = new(StringComparer.OrdinalIgnoreCase);

    public async Task<ImportRowResult> ParseAsync(ImportCsvRow row, CancellationToken cancellationToken)
    {
        var cells = new Cells(row);
        var externalRef = row.Get(ImportColumns.ExternalRef);

        if (externalRef.Length == 0)
        {
            cells.Errors.Add(ImportProblems.Error(ImportColumns.ExternalRef, ErrorCodes.ImportRequired, "external_ref is required."));
        }
        else if (externalRef.Length > ListingImportRow.MaxExternalRefLength)
        {
            cells.Errors.Add(TooLong(ImportColumns.ExternalRef, ListingImportRow.MaxExternalRefLength));
        }
        else if (!_externalRefs.Add(externalRef))
        {
            cells.Errors.Add(ImportProblems.Error(
                ImportColumns.ExternalRef, ErrorCodes.ImportDuplicateRef, $"external_ref '{externalRef}' appears earlier in the file.", ("value", externalRef)));
        }

        var type = cells.Enum<PropertyType>(ImportColumns.PropertyTypeColumn, required: true);
        if (type is not { } propertyType)
        {
            return Failed(row, externalRef, cells.Errors);
        }

        var columns = ImportColumns.For(propertyType);
        var own = columns.Select(c => c.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var (name, value) in row.Values)
        {
            if (value.Length > 0 && !own.Contains(name))
            {
                cells.Errors.Add(ImportProblems.Error(
                    name, ErrorCodes.ImportColumnNotForType, $"{name} isn't a column for {propertyType}.", ("type", propertyType.ToString())));
            }
        }

        foreach (var column in columns.Where(c => c.Requirement == ImportColumnRequirement.Required && !row.Has(c.Name)))
        {
            if (column.Name is not (ImportColumns.ExternalRef or ImportColumns.PropertyTypeColumn))
            {
                cells.Errors.Add(ImportProblems.Error(column.Name, ErrorCodes.ImportRequired, $"{column.Name} is required."));
            }
        }

        foreach (var column in columns.Where(c => c.MaxLength is { } max && c.Name != ImportColumns.ExternalRef && row.Get(c.Name).Length > max))
        {
            cells.Errors.Add(TooLong(column.Name, column.MaxLength!.Value));
        }

        var dealType = cells.Choice(ImportColumns.DealType, [ImportColumns.Sale, ImportColumns.Rent]);
        var isRent = dealType == ImportColumns.Rent;
        if (dealType == ImportColumns.Sale)
        {
            foreach (var column in columns.Where(c => c.Scope == ImportColumnScope.Rent && row.Has(c.Name)))
            {
                cells.Errors.Add(ImportProblems.Error(column.Name, ErrorCodes.ImportRentOnly, $"{column.Name} is for rentals only."));
            }
        }

        var inactive = cells.Choice(ImportColumns.Status, [ImportColumns.Active, ImportColumns.Inactive]) == ImportColumns.Inactive;
        var price = cells.Decimal(ImportColumns.Price);
        var currency = cells.Enum<Currency>(ImportColumns.Currency);
        var negotiable = cells.YesNo(ImportColumns.PriceNegotiable);
        var area = cells.Decimal(ImportColumns.AreaM2);
        var yearBuilt = cells.Int(ImportColumns.YearBuilt);
        var condition = cells.Enum<PropertyCondition>(ImportColumns.Condition);
        var attributes = Attributes(columns, cells);

        var (location, locationProblems) = context.Locations.Resolve(
            row.Get(ImportColumns.City), row.Get(ImportColumns.Locality), row.Get(ImportColumns.Sector));
        // An empty city is already reported as required.
        cells.Errors.AddRange(locationProblems.Where(p => p.Code != ErrorCodes.ImportRequired));

        var latitude = cells.Decimal(ImportColumns.Latitude);
        var longitude = cells.Decimal(ImportColumns.Longitude);
        if (row.Has(ImportColumns.Latitude) != row.Has(ImportColumns.Longitude))
        {
            cells.Errors.Add(ImportProblems.Error(ImportColumns.Longitude, ErrorCodes.ImportCoordinatesIncomplete, "Give both latitude and longitude, or neither."));
        }

        var imageUrls = ImageUrls(row, cells);
        var amenityIds = Keys(row, ImportColumns.Amenities, context.AmenityIdsByKey, ErrorCodes.ImportUnknownAmenity, cells);
        var proximityIds = Keys(row, ImportColumns.Proximities, context.ProximityIdsByKey, ErrorCodes.ImportUnknownProximity, cells);

        var minLease = cells.Int(ImportColumns.MinLeaseMonths);
        var deposit = cells.Decimal(ImportColumns.Deposit);
        var utilities = cells.YesNo(ImportColumns.UtilitiesIncluded);
        var availableFrom = cells.Date(ImportColumns.AvailableFrom);
        var pets = cells.YesNo(ImportColumns.PetsAllowed);

        var warnings = new List<ListingImportProblem>();
        var author = context.DefaultAuthorId;
        var authorNamed = false;
        var agentEmail = row.Get(ImportColumns.AgentEmail);
        if (agentEmail.Length > 0)
        {
            if (context.MemberByEmail(agentEmail) is { } member)
            {
                author = member;
                authorNamed = true;
            }
            else
            {
                warnings.Add(ImportProblems.Warning(
                    ImportColumns.AgentEmail,
                    ErrorCodes.ImportAgentNotMember,
                    $"{agentEmail} isn't a member of the agency; imported under the default author.",
                    ("email", agentEmail)));
            }
        }

        if (cells.Errors.Count > 0 || location is null)
        {
            return Failed(row, externalRef, cells.Errors);
        }

        var agency = context.Agency;
        var command = new ImportListingCommand
        {
            PropertyType = propertyType,
            TotalAreaM2 = area ?? 0,
            YearBuilt = yearBuilt,
            Condition = condition,
            TypeSpecificAttributes = attributes,
            AmenityIds = amenityIds,
            ProximityIds = proximityIds,
            RaionId = location.RaionId,
            LocalitateId = location.LocalitateId,
            ChisinauSectorId = location.ChisinauSectorId,
            StreetAddress = row.Get(ImportColumns.Street),
            BuildingNumber = NullIfEmpty(row.Get(ImportColumns.BuildingNumber)),
            TransactionType = isRent ? TransactionType.Rent : TransactionType.Sale,
            Title = row.Get(ImportColumns.Title),
            Description = row.Get(ImportColumns.Description),
            Price = price ?? 0,
            Currency = currency ?? Currency.EUR,
            IsNegotiable = negotiable ?? false,
            RentalDetails = isRent
                ? new RentalDetails(minLease, deposit, utilities ?? false, availableFrom?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), pets)
                : null,
            // Someone else's details than the author's: the agency's, unless the row names others.
            Contact = new ListingContact(
                ContactPersonType.Other,
                NullIfEmpty(row.Get(ImportColumns.ContactPhone)) ?? agency.Phone,
                NullIfEmpty(row.Get(ImportColumns.ContactName)) ?? agency.Name,
                NullIfEmpty(row.Get(ImportColumns.ContactEmail)) ?? agency.Email),
        };

        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Failed(row, externalRef, validation.Errors.Select(ToProblem).ToList());
        }

        return new ImportRowResult(
            row.RowNumber,
            externalRef,
            new ImportRowPlan(
                row.RowNumber,
                externalRef,
                command,
                inactive,
                author,
                authorNamed,
                location,
                latitude is { } lat ? (double)lat : null,
                longitude is { } lon ? (double)lon : null,
                imageUrls,
                warnings),
            warnings);
    }

    // A listing-form field (as the validator names it) → the CSV column it came from; null for the row.
    public static string? ColumnFor(string propertyName) => propertyName switch
    {
        nameof(ImportListingCommand.PropertyType) => ImportColumns.PropertyTypeColumn,
        nameof(ImportListingCommand.TotalAreaM2) => ImportColumns.AreaM2,
        nameof(ImportListingCommand.YearBuilt) => ImportColumns.YearBuilt,
        nameof(ImportListingCommand.Condition) => ImportColumns.Condition,
        nameof(ImportListingCommand.AmenityIds) => ImportColumns.Amenities,
        nameof(ImportListingCommand.ProximityIds) => ImportColumns.Proximities,
        nameof(ImportListingCommand.RaionId) => ImportColumns.City,
        nameof(ImportListingCommand.LocalitateId) => ImportColumns.Locality,
        nameof(ImportListingCommand.ChisinauSectorId) => ImportColumns.Sector,
        nameof(ImportListingCommand.StreetAddress) => ImportColumns.Street,
        nameof(ImportListingCommand.BuildingNumber) => ImportColumns.BuildingNumber,
        nameof(ImportListingCommand.TransactionType) => ImportColumns.DealType,
        nameof(ImportListingCommand.Title) => ImportColumns.Title,
        nameof(ImportListingCommand.Description) => ImportColumns.Description,
        nameof(ImportListingCommand.Price) => ImportColumns.Price,
        nameof(ImportListingCommand.Currency) => ImportColumns.Currency,
        "RentalDetails.PetsAllowed" => ImportColumns.PetsAllowed,
        "RentalDetails.MinLeasePeriodMonths" => ImportColumns.MinLeaseMonths,
        "RentalDetails.SecurityDepositAmount" => ImportColumns.Deposit,
        "Contact.Phone" => ImportColumns.ContactPhone,
        "Contact.Name" => ImportColumns.ContactName,
        "Contact.Email" => ImportColumns.ContactEmail,
        _ when propertyName.StartsWith("TypeSpecificAttributes.", StringComparison.Ordinal) =>
            ImportColumns.ColumnName(propertyName["TypeSpecificAttributes.".Length..]),
        _ => null,
    };

    // The type's detail columns as the attributes JSON the form would send (camelCase keys, enum names).
    private static JsonElement Attributes(IReadOnlyList<ImportColumn> columns, Cells cells)
    {
        var json = new JsonObject();
        foreach (var column in columns.Where(c => c.AttributeKey is not null && cells.Row.Has(c.Name)))
        {
            JsonNode? value = column.Kind switch
            {
                ImportColumnKind.Choice => cells.Choice(column.Name, column.Choices!) is { } choice ? JsonValue.Create(choice) : null,
                ImportColumnKind.Integer => cells.Int(column.Name) is { } number ? JsonValue.Create(number) : null,
                ImportColumnKind.Decimal => cells.Decimal(column.Name) is { } amount ? JsonValue.Create(amount) : null,
                ImportColumnKind.YesNo => cells.YesNo(column.Name) is { } answer ? JsonValue.Create(answer) : null,
                _ => JsonValue.Create(cells.Row.Get(column.Name)),
            };

            if (value is not null)
            {
                json[column.AttributeKey!] = value;
            }
        }

        return JsonSerializer.SerializeToElement(json);
    }

    private static IReadOnlyList<string> ImageUrls(ImportCsvRow row, Cells cells)
    {
        var urls = ImportValues.List(row.Get(ImportColumns.ImageUrls)).Distinct(StringComparer.Ordinal).ToList();
        foreach (var url in urls)
        {
            if (url.Length > Photo.MaxSourceUrlLength
                || !Uri.TryCreate(url, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                cells.Errors.Add(ImportProblems.Error(ImportColumns.ImageUrls, ErrorCodes.ImportBadPhotoUrl, $"'{url}' isn't an http(s) address.", ("value", Shorten(url))));
            }
        }

        if (row.Has(ImportColumns.ImageUrls) && urls.Count < ListingPhotoRules.MinPhotos)
        {
            cells.Errors.Add(ImportProblems.Error(
                ImportColumns.ImageUrls,
                ErrorCodes.ImportTooFewPhotos,
                $"At least {ListingPhotoRules.MinPhotos} photos are needed.",
                ("min", ListingPhotoRules.MinPhotos.ToString(CultureInfo.InvariantCulture)),
                ("count", urls.Count.ToString(CultureInfo.InvariantCulture))));
        }
        else if (urls.Count > ImportColumns.MaxImageUrls)
        {
            cells.Errors.Add(ImportProblems.Error(
                ImportColumns.ImageUrls,
                ErrorCodes.ImportTooManyPhotos,
                $"At most {ImportColumns.MaxImageUrls} photos.",
                ("max", ImportColumns.MaxImageUrls.ToString(CultureInfo.InvariantCulture))));
        }

        return urls;
    }

    private static IReadOnlyList<Guid> Keys(ImportCsvRow row, string column, IReadOnlyDictionary<string, Guid> idsByKey, string unknownCode, Cells cells)
    {
        var ids = new List<Guid>();
        foreach (var key in ImportValues.List(row.Get(column)))
        {
            if (idsByKey.TryGetValue(key, out var id))
            {
                ids.Add(id);
            }
            else
            {
                cells.Errors.Add(ImportProblems.Error(column, unknownCode, $"Unknown {column} key '{key}'.", ("value", key)));
            }
        }

        return ids.Distinct().ToList();
    }

    private static ListingImportProblem ToProblem(ValidationFailure failure)
    {
        var parameters = failure.CustomState is IReadOnlyDictionary<string, object> custom
            ? custom.ToDictionary(kv => kv.Key, kv => Convert.ToString(kv.Value, CultureInfo.InvariantCulture) ?? string.Empty)
            : (failure.FormattedMessagePlaceholderValues ?? [])
                .Where(kv => PlaceholderParams.ContainsKey(kv.Key) && kv.Value is not null)
                .ToDictionary(kv => PlaceholderParams[kv.Key], kv => Convert.ToString(kv.Value, CultureInfo.InvariantCulture) ?? string.Empty);

        return new ListingImportProblem(
            ListingImportProblemSeverity.Error,
            ColumnFor(failure.PropertyName),
            string.IsNullOrEmpty(failure.ErrorCode) ? ErrorCodes.ImportInvalidValue : failure.ErrorCode,
            failure.ErrorMessage,
            parameters.Count > 0 ? parameters : null);
    }

    private static ImportRowResult Failed(ImportCsvRow row, string externalRef, IReadOnlyList<ListingImportProblem> problems) =>
        new(row.RowNumber, NullIfEmpty(externalRef), null, problems);

    private static ListingImportProblem TooLong(string column, int max) =>
        ImportProblems.Error(column, ErrorCodes.ImportTooLong, $"{column} is longer than {max} characters.", ("max", max.ToString(CultureInfo.InvariantCulture)));

    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;

    private static string Shorten(string value) => value.Length > 120 ? value[..117] + "..." : value;

    // Reads one row's cells, collecting what doesn't read. An empty cell is null (requiredness is
    // checked once, from the columns' Requirement).
    private sealed class Cells(ImportCsvRow row)
    {
        public ImportCsvRow Row { get; } = row;

        public List<ListingImportProblem> Errors { get; } = [];

        public decimal? Decimal(string column) =>
            Read(column, value => ImportValues.TryDecimal(value, out var result) ? result : (decimal?)null, ErrorCodes.ImportNotANumber, "a number");

        public int? Int(string column) =>
            Read(column, value => ImportValues.TryInt(value, out var result) ? result : (int?)null, ErrorCodes.ImportNotAWholeNumber, "a whole number");

        public bool? YesNo(string column) =>
            Read(column, value => ImportValues.TryYesNo(value, out var result) ? result : (bool?)null, ErrorCodes.ImportNotYesNo, "da or nu");

        public DateOnly? Date(string column) =>
            Read(column, value => ImportValues.TryDate(value, out var result) ? result : (DateOnly?)null, ErrorCodes.ImportNotADate, "a date (yyyy-mm-dd)");

        public TEnum? Enum<TEnum>(string column, bool required = false)
            where TEnum : struct, System.Enum
        {
            var name = Choice(column, System.Enum.GetNames<TEnum>());
            if (name is null && required && !Row.Has(column))
            {
                Errors.Add(ImportProblems.Error(column, ErrorCodes.ImportRequired, $"{column} is required."));
            }

            return name is null ? null : System.Enum.Parse<TEnum>(name);
        }

        public string? Choice(string column, IReadOnlyList<string> choices)
        {
            var value = Row.Get(column);
            if (value.Length == 0)
            {
                return null;
            }

            var match = ImportValues.MatchChoice(value, choices);
            if (match is null)
            {
                Errors.Add(ImportProblems.Error(
                    column,
                    ErrorCodes.ImportUnknownValue,
                    $"'{value}' isn't one of: {string.Join(", ", choices)}.",
                    ("value", value),
                    ("choices", string.Join(", ", choices))));
            }

            return match;
        }

        private T? Read<T>(string column, Func<string, T?> parse, string code, string expected)
            where T : struct
        {
            var value = Row.Get(column);
            if (value.Length == 0)
            {
                return null;
            }

            var result = parse(value);
            if (result is null)
            {
                Errors.Add(ImportProblems.Error(column, code, $"'{value}' isn't {expected}.", ("value", value)));
            }

            return result;
        }
    }
}
