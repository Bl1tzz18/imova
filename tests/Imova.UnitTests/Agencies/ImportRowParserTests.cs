using Imova.Application.Common;
using Imova.Application.Features.Agencies.Import;
using Imova.Domain.Agencies;
using Imova.Domain.Listings;
using Imova.Domain.Properties;
using Imova.Infrastructure;
using Imova.Infrastructure.Amenities;
using Imova.Infrastructure.Proximities;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Agencies;

// One CSV row → a listing payload checked by the listing form's own rules, or every problem with it;
// and whose listing it becomes (the author rule, including a site admin importing for an agency).
public class ImportRowParserTests : IAsyncDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly ImovaDbContext _db = TestDbContextFactory.Create();
    private readonly ImportTestPlaces _places = new();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _agent = Guid.NewGuid();
    private readonly Agency _agency;

    public ImportRowParserTests()
    {
        _places.AddTo(_db);
        _db.Amenities.AddRange(AmenityConfiguration.Seed);
        _db.Proximities.AddRange(ProximityConfiguration.Seed);
        AddUser(_owner);
        AddUser(_agent);
        _agency = ListingTestData.AddAgency(_db, _owner, "Casa Ta");
        _agency.AddMember(_agent, AgencyRole.Agent, Now);
        _db.SaveChanges();
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();

    private void AddUser(Guid id)
    {
        var user = ListingTestData.AddUser(_db, id);
        user.NormalizedEmail = user.Email!.ToUpperInvariant();
    }

    private static string EmailOf(Guid id) => $"{id:N}@example.com";

    private async Task<ImportRowParser> ParserAsync(Guid? uploader = null) =>
        new(await ImportContext.LoadAsync(_db, _agency.Id, uploader ?? _owner, CancellationToken.None), new ImportListingValidator(_db));

    // The type's example row, with some cells changed ("" empties one).
    private static ImportCsvRow Row(PropertyType type, params (string Column, string Value)[] changes)
    {
        var values = ImportColumns.For(type).ToDictionary(c => c.Name, c => ImportExamples.For(type).GetValueOrDefault(c.Name, string.Empty));
        foreach (var (column, value) in changes)
        {
            values[column] = value;
        }

        return new ImportCsvRow(7, values);
    }

    public static TheoryData<PropertyType> Types() => new(Enum.GetValues<PropertyType>());

    [Theory]
    [MemberData(nameof(Types))]
    public async Task EveryTemplatesExample_IsAValidListing(PropertyType type)
    {
        var result = await (await ParserAsync()).ParseAsync(Row(type), CancellationToken.None);

        Assert.Empty(result.Problems);
        var plan = result.Plan!;
        Assert.Equal(type, plan.Command.PropertyType);
        Assert.Equal(ImportExamples.For(type)[ImportColumns.ExternalRef], plan.ExternalRef);
        Assert.Equal(3, plan.ImageUrls.Count);
        Assert.False(plan.Inactive);
        Assert.Equal(_owner, plan.AuthorUserId);

        // Contact: the agency's own details.
        var contact = plan.Command.Contact!;
        Assert.Equal((ContactPersonType.Other, "Casa Ta", _agency.Phone, _agency.Email), (contact.PersonType, contact.Name, contact.Phone, contact.Email));
    }

    [Fact]
    public async Task TheExample_BecomesTheSamePayloadTheFormWouldSend()
    {
        var plan = (await (await ParserAsync()).ParseAsync(Row(PropertyType.Apartment), CancellationToken.None)).Plan!;
        var command = plan.Command;

        Assert.Equal((78000m, Currency.EUR, 58m, 2019, TransactionType.Sale), (command.Price, command.Currency, command.TotalAreaM2, command.YearBuilt, command.TransactionType));
        Assert.Equal((_places.Chisinau.Id, _places.Botanica.Id, (Guid?)null), (command.RaionId, command.ChisinauSectorId, command.LocalitateId));
        Assert.Equal(("bd. Dacia", "27"), (command.StreetAddress, command.BuildingNumber));
        Assert.Equal(2, command.TypeSpecificAttributes!.Value.GetProperty("rooms").GetInt32());
        Assert.Equal("OwnBoiler", command.TypeSpecificAttributes.Value.GetProperty("heatingSystem").GetString());
        Assert.True(command.TypeSpecificAttributes.Value.GetProperty("gasSupply").GetBoolean());
        Assert.Equal(3, command.AmenityIds!.Count);
        Assert.Null(command.RentalDetails);
    }

    [Fact]
    public async Task EverythingWrongInARow_IsReportedAtOnce_OnItsColumn()
    {
        var row = Row(
            PropertyType.Apartment,
            ("price", "optzeci"),
            ("heating_system", "centrala"),
            ("rooms", ""),
            ("deposit", "100"),
            ("plot_type", "Agricultural"),
            ("image_urls", "ftp://exemplu.md/1.jpg|https://exemplu.md/2.jpg"),
            ("amenities", "balcony|jacuzzi"),
            ("latitude", "46.99"));

        var result = await (await ParserAsync()).ParseAsync(row, CancellationToken.None);

        Assert.Null(result.Plan);
        Assert.Equal("AP-1024", result.ExternalRef);
        var problems = result.Problems.Select(p => (p.Column, p.Code)).ToList();
        Assert.Contains(("price", ErrorCodes.ImportNotANumber), problems);
        Assert.Contains(("heating_system", ErrorCodes.ImportUnknownValue), problems);
        Assert.Contains(("rooms", ErrorCodes.ImportRequired), problems);
        Assert.Contains(("deposit", ErrorCodes.ImportRentOnly), problems);
        Assert.Contains(("plot_type", ErrorCodes.ImportColumnNotForType), problems);
        Assert.Contains(("image_urls", ErrorCodes.ImportBadPhotoUrl), problems);
        Assert.Contains(("image_urls", ErrorCodes.ImportTooFewPhotos), problems);
        Assert.Contains(("amenities", ErrorCodes.ImportUnknownAmenity), problems);
        Assert.Contains(("longitude", ErrorCodes.ImportCoordinatesIncomplete), problems);
        Assert.All(result.Problems, p => Assert.Equal(ListingImportProblemSeverity.Error, p.Severity));
    }

    [Fact]
    public async Task TheListingFormsOwnRules_AreReportedOnTheCsvsColumns()
    {
        var parser = await ParserAsync();

        var floor = await parser.ParseAsync(Row(PropertyType.Apartment, ("external_ref", "A1"), ("floor", "12")), CancellationToken.None);
        Assert.Contains(floor.Problems, p => p.Column == "floor");

        var pets = await parser.ParseAsync(Row(PropertyType.Apartment, ("external_ref", "A2"), ("deal_type", "rent"), ("pets_allowed", "")), CancellationToken.None);
        Assert.Contains(pets.Problems, p => p.Column == "pets_allowed");

        var boiler = await parser.ParseAsync(Row(PropertyType.House, ("heating_energy_source", "")), CancellationToken.None);
        Assert.Contains(boiler.Problems, p => p.Column == "heating_energy_source");
    }

    [Fact]
    public async Task TheSameExternalRef_TwiceInAFile_FailsTheSecondRow()
    {
        var parser = await ParserAsync();

        Assert.NotNull((await parser.ParseAsync(Row(PropertyType.Garage), CancellationToken.None)).Plan);
        var second = await parser.ParseAsync(Row(PropertyType.Garage, ("external_ref", "gr-3")), CancellationToken.None);

        Assert.Equal(ErrorCodes.ImportDuplicateRef, Assert.Single(second.Problems).Code);
    }

    [Fact]
    public async Task StatusCoordinatesAndRentalTerms_AreRead()
    {
        var row = Row(PropertyType.Room, ("status", "Inactive"), ("latitude", "47,05"), ("longitude", "28,85"), ("available_from", "01.12.2026"));

        var plan = (await (await ParserAsync()).ParseAsync(row, CancellationToken.None)).Plan!;

        Assert.True(plan.Inactive);
        Assert.Equal((47.05, 28.85), (plan.Latitude!.Value, plan.Longitude!.Value));
        var rental = plan.Command.RentalDetails!;
        Assert.Equal((6, true, false), (rental.MinLeasePeriodMonths, rental.UtilitiesIncluded, rental.PetsAllowed!.Value));
        Assert.Equal(new DateTime(2026, 12, 1), rental.AvailableFrom!.Value.Date);
    }

    [Fact]
    public async Task TheAuthor_IsTheUploader_OrAMemberNamedInAgentEmail()
    {
        var parser = await ParserAsync();

        var byAgent = await parser.ParseAsync(Row(PropertyType.Garage, ("agent_email", EmailOf(_agent).ToUpperInvariant())), CancellationToken.None);
        Assert.Equal(_agent, byAgent.Plan!.AuthorUserId);
        Assert.Empty(byAgent.Problems);

        var stranger = Guid.NewGuid();
        AddUser(stranger);
        await _db.SaveChangesAsync();
        var notMember = await parser.ParseAsync(Row(PropertyType.Room, ("agent_email", EmailOf(stranger))), CancellationToken.None);
        Assert.Equal(_owner, notMember.Plan!.AuthorUserId);
        var warning = Assert.Single(notMember.Problems);
        Assert.Equal((ListingImportProblemSeverity.Warning, "agent_email", ErrorCodes.ImportAgentNotMember), (warning.Severity, warning.Column, warning.Code));
        Assert.Equal(notMember.Problems, notMember.Plan.Warnings);
    }

    [Fact]
    public async Task ASiteAdminImportingForAnAgency_MakesItsLongestStandingOwnerTheAuthor()
    {
        // Later than the founder's real join time (AddAgency uses the clock), whatever day the test runs.
        var laterOwner = Guid.NewGuid();
        AddUser(laterOwner);
        var founderJoined = _agency.Members.Single(m => m.UserId == _owner).JoinedAt;
        _agency.AddMember(laterOwner, AgencyRole.Owner, founderJoined.AddDays(1));
        await _db.SaveChangesAsync();

        var siteAdmin = Guid.NewGuid();
        var parser = await ParserAsync(uploader: siteAdmin);

        Assert.Equal(_owner, (await parser.ParseAsync(Row(PropertyType.Garage), CancellationToken.None)).Plan!.AuthorUserId);
        Assert.Equal(_agent, (await parser.ParseAsync(Row(PropertyType.Room, ("agent_email", EmailOf(_agent))), CancellationToken.None)).Plan!.AuthorUserId);
    }

    [Fact]
    public async Task ARowWithoutAKnownPropertyType_StopsThere()
    {
        var result = await (await ParserAsync()).ParseAsync(Row(PropertyType.Garage, ("property_type", "Castle")), CancellationToken.None);

        var problem = Assert.Single(result.Problems);
        Assert.Equal(("property_type", ErrorCodes.ImportUnknownValue), (problem.Column, problem.Code));
    }
}
