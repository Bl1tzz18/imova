using FluentValidation;
using Imova.Application.Features.Listings.SearchListings;
using Imova.Domain.Properties;

namespace Imova.UnitTests.Search;

public class AttributeFilterParserTests
{
    private static IReadOnlyList<AttributeFilter> Parse(params (string Key, string? Value)[] query) =>
        AttributeFilterParser.Parse(query.GroupBy(p => p.Key).Select(g => new KeyValuePair<string, string?[]>(g.Key, g.Select(p => p.Value).ToArray())));

    [Fact]
    public void NumberFields_AreMinMaxRanges_MergedPerField()
    {
        var filters = Parse(("minRooms", "2"), ("maxRooms", "4"), ("maxCeilingHeightM", "2.75"), ("minFloor", "-1"));

        Assert.Equal(new AttributeFilter("rooms", AttributeFilterKind.Number) { Min = 2, Max = 4 }, filters.Single(f => f.Field == "rooms"), new FilterComparer());
        Assert.Equal(2.75m, filters.Single(f => f.Field == "ceilingHeightM").Max);
        Assert.Equal(-1m, filters.Single(f => f.Field == "floor").Min);
    }

    [Fact]
    public void ChoiceFields_TakeEveryValue_InCanonicalCasing()
    {
        var filter = Assert.Single(Parse(("heatingSystem", "ownboiler"), ("heatingSystem", "HeatPump"), ("heatingSystem", "OwnBoiler")));

        Assert.Equal(AttributeFilterKind.Choice, filter.Kind);
        Assert.Equal(["OwnBoiler", "HeatPump"], filter.Values);
    }

    [Fact]
    public void YesNoFields_AreBooleans()
    {
        var filter = Assert.Single(Parse(("gasSupply", "false")));

        Assert.Equal(AttributeFilterKind.YesNo, filter.Kind);
        Assert.False(filter.Value);
    }

    [Fact]
    public void OtherParameters_AndEmptyValues_AreIgnored()
    {
        Assert.Empty(Parse(
            ("minPriceEur", "100"), ("maxAreaM2", "80"), ("minYearBuilt", "2000"), ("propertyType", "House"), ("sort", "PriceAsc"),
            ("rooms", "3"), ("layout", ""), ("minRooms", null), ("electricalPower", "380V"), ("maxLeasePeriodMonths", "6")));
    }

    [Theory]
    [InlineData("layout", "Castle")]
    [InlineData("layout", "1")]
    [InlineData("gasSupply", "yes")]
    [InlineData("minRooms", "two")]
    public void UnparsableValues_AreValidationErrors(string key, string value)
    {
        Assert.Throws<ValidationException>(() => Parse((key, value)));
    }

    [Fact]
    public void Schema_CoversEveryNonTextAttribute_OfEachType()
    {
        Assert.Equal(15, AttributeSearchSchema.FieldsFor(PropertyType.Apartment).Count);
        Assert.Equal(21, AttributeSearchSchema.FieldsFor(PropertyType.House).Count);
        Assert.Equal(9, AttributeSearchSchema.FieldsFor(PropertyType.Land).Count);
        Assert.Equal(10, AttributeSearchSchema.FieldsFor(PropertyType.Commercial).Count);
        Assert.False(AttributeSearchSchema.FieldsFor(PropertyType.Commercial).ContainsKey("electricalPower"));
        Assert.Equal(["parkingType"], AttributeSearchSchema.FieldsFor(PropertyType.Garage).Keys);
        Assert.Equal(2, AttributeSearchSchema.FieldsFor(PropertyType.Room).Count);
    }

    // A field name several types share must mean the same kind of filter for all of them.
    [Fact]
    public void SharedFieldNames_HaveOneKindAcrossTypes()
    {
        foreach (var (name, field) in AttributeSearchSchema.AllFields)
        {
            foreach (var type in AttributeSearchSchema.TypesWith(name))
            {
                var own = AttributeSearchSchema.FieldsFor(type)[name];
                Assert.True(own.Kind == field.Kind && own.EnumType == field.EnumType, $"{name} differs on {type}");
            }
        }
    }

    private sealed class FilterComparer : IEqualityComparer<AttributeFilter>
    {
        public bool Equals(AttributeFilter? x, AttributeFilter? y) =>
            x!.Field == y!.Field && x.Kind == y.Kind && x.Min == y.Min && x.Max == y.Max && x.Value == y.Value && x.Values.SequenceEqual(y.Values);

        public int GetHashCode(AttributeFilter obj) => obj.Field.GetHashCode();
    }
}
