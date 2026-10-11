using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Imova.Application.Features.Agencies.Import;
using Imova.Domain.Properties;
using Imova.Domain.Properties.Attributes;
using Imova.Infrastructure.Amenities;
using Imova.Infrastructure.Proximities;

namespace Imova.UnitTests.Agencies;

public class ImportColumnsTests
{
    public static TheoryData<PropertyType> Types() => new(Enum.GetValues<PropertyType>());

    [Fact]
    public void ColumnNames_AreSnakeCase()
    {
        Assert.Equal("living_area_m2", ImportColumns.ColumnName("LivingAreaM2"));
        Assert.Equal("total_floors_in_building", ImportColumns.ColumnName("TotalFloorsInBuilding"));
    }

    [Theory]
    [MemberData(nameof(Types))]
    public void EveryTypeHasItsOwnDetails_AsColumns_WithUniqueNames(PropertyType type)
    {
        var columns = ImportColumns.For(type);

        Assert.Equal(columns.Count, columns.Select(c => c.Name).Distinct().Count());
        var fields = PropertyAttributes.EmptyFor(type).GetType().GetProperties().Select(p => ImportColumns.ColumnName(p.Name));
        Assert.All(fields, field => Assert.Contains(columns, c => c.Name == field && c.AttributeKey is not null));
        Assert.Equal(ImportColumns.ExternalRef, columns[0].Name);
        Assert.Contains(columns, c => c.Name == ImportColumns.ImageUrls && c.Requirement == ImportColumnRequirement.Required);
    }

    [Fact]
    public void RequiredDetails_FollowTheListingForm()
    {
        ImportColumnRequirement Of(PropertyType type, string column) => ImportColumns.For(type).Single(c => c.Name == column).Requirement;

        Assert.Equal(ImportColumnRequirement.Required, Of(PropertyType.Apartment, "rooms"));
        Assert.Equal(ImportColumnRequirement.Required, Of(PropertyType.Apartment, "total_floors"));
        Assert.Equal(ImportColumnRequirement.Optional, Of(PropertyType.Apartment, "gas_supply"));
        Assert.Equal(ImportColumnRequirement.Conditional, Of(PropertyType.Apartment, "heating_energy_source"));
        Assert.Equal(ImportColumnRequirement.Required, Of(PropertyType.House, "land_area_m2"));
        // Allowed only for agricultural land, but optional even there.
        Assert.Equal(ImportColumnRequirement.Optional, Of(PropertyType.Land, "soil_quality_score"));
        Assert.Equal(ImportColumnRequirement.Optional, Of(PropertyType.Commercial, "number_of_offices"));
        Assert.Equal(ImportColumnRequirement.Conditional, Of(PropertyType.House, "heating_distribution"));
        Assert.Equal(ImportColumnRequirement.Required, Of(PropertyType.Garage, "parking_type"));
        Assert.Equal(ImportColumnRequirement.Required, Of(PropertyType.Room, "bathroom_type"));
    }

    [Fact]
    public void OnlyTheColumnsThatMeanSomething_ForTheType()
    {
        bool Has(PropertyType type, string column) => ImportColumns.For(type).Any(c => c.Name == column);

        Assert.False(Has(PropertyType.Land, ImportColumns.YearBuilt));
        Assert.True(Has(PropertyType.Garage, ImportColumns.Condition));
        Assert.False(Has(PropertyType.Apartment, ImportColumns.Condition));
        Assert.True(Has(PropertyType.Room, ImportColumns.PetsAllowed));
        Assert.False(Has(PropertyType.Commercial, ImportColumns.PetsAllowed));

        var choice = ImportColumns.For(PropertyType.Apartment).Single(c => c.Name == "heating_system");
        Assert.Equal(ImportColumnKind.Choice, choice.Kind);
        Assert.Contains("OwnBoiler", choice.Choices!);
        Assert.Equal(ImportColumnScope.Rent, ImportColumns.For(PropertyType.Room).Single(c => c.Name == ImportColumns.Deposit).Scope);
    }

    [Theory]
    [MemberData(nameof(Types))]
    public void TheExampleRow_FillsEveryRequiredColumn_WithAcceptedValues(PropertyType type)
    {
        var columns = ImportColumns.For(type).ToDictionary(c => c.Name);
        var example = ImportExamples.For(type);
        var amenityKeys = AmenityConfiguration.Seed.Where(a => a.AppliesTo(type)).Select(a => a.Key).ToHashSet();
        var proximityKeys = ProximityConfiguration.Seed.Where(p => p.AppliesTo(type)).Select(p => p.Key).ToHashSet();

        Assert.All(example.Keys, key => Assert.True(columns.ContainsKey(key), $"'{key}' is not a {type} column."));
        Assert.All(
            columns.Values.Where(c => c.Requirement == ImportColumnRequirement.Required),
            c => Assert.False(string.IsNullOrWhiteSpace(example.GetValueOrDefault(c.Name)), $"{type}: no example for {c.Name}."));

        foreach (var (name, value) in example)
        {
            var column = columns[name];
            switch (column.Kind)
            {
                case ImportColumnKind.Choice:
                    Assert.Contains(value, column.Choices!, StringComparer.OrdinalIgnoreCase);
                    break;
                case ImportColumnKind.YesNo:
                    Assert.Contains(value, new[] { "da", "nu" });
                    break;
                case ImportColumnKind.Integer:
                    Assert.True(int.TryParse(value, CultureInfo.InvariantCulture, out _), $"{name}: {value}");
                    break;
                case ImportColumnKind.Decimal:
                    Assert.True(decimal.TryParse(value, CultureInfo.InvariantCulture, out _), $"{name}: {value}");
                    break;
                case ImportColumnKind.Date:
                    Assert.True(DateOnly.TryParseExact(value, "yyyy-MM-dd", out _), $"{name}: {value}");
                    break;
            }
        }

        var photos = example[ImportColumns.ImageUrls].Split(ImportColumns.ListSeparator);
        Assert.InRange(photos.Length, 3, ImportColumns.MaxImageUrls);
        Assert.All(example.GetValueOrDefault(ImportColumns.Amenities, "").Split('|', StringSplitOptions.RemoveEmptyEntries), key => Assert.Contains(key, amenityKeys));
        Assert.All(example.GetValueOrDefault(ImportColumns.Proximities, "").Split('|', StringSplitOptions.RemoveEmptyEntries), key => Assert.Contains(key, proximityKeys));
    }

    [Theory]
    [MemberData(nameof(Types))]
    public void TheTemplate_IsTheHeaderAndTheExample_SemicolonSeparated_WithABom(PropertyType type)
    {
        var bytes = ImportTemplate.Build(type);

        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);

        using var reader = new StringReader(Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3));
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ";" });
        Assert.True(csv.Read());
        csv.ReadHeader();
        Assert.Equal(ImportColumns.For(type).Select(c => c.Name), csv.HeaderRecord!);
        Assert.True(csv.Read());
        foreach (var (name, value) in ImportExamples.For(type))
        {
            Assert.Equal(value, csv.GetField(name));
        }

        Assert.False(csv.Read());
        Assert.Equal($"imova-import-{type.ToString().ToLowerInvariant()}.csv", ImportTemplate.FileName(type));
    }
}
