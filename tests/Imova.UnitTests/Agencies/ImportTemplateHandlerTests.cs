using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Agencies.Import;
using Imova.Domain.Agencies;
using Imova.Domain.Properties;
using Imova.Infrastructure;
using Imova.Infrastructure.Amenities;
using Imova.Infrastructure.Proximities;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Agencies;

// Who may download the template and the columns guide: Owners, Admins and site admins, verified or
// not; an Agent 403; a stranger 403 (404 once the agency is deactivated); an unknown agency 404.
public class ImportTemplateHandlerTests : IAsyncDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly ImovaDbContext _db = TestDbContextFactory.Create();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Agency _agency;

    public ImportTemplateHandlerTests()
    {
        ListingTestData.AddUser(_db, _owner);
        _agency = ListingTestData.AddAgency(_db, _owner, "Casa Ta");
        _db.Amenities.AddRange(AmenityConfiguration.Seed);
        _db.Proximities.AddRange(ProximityConfiguration.Seed);
        _db.SaveChanges();
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();

    private Task<ImportTemplateFile?> TemplateAsync(bool isAdmin, Guid? agencyId = null) =>
        new GetImportTemplateHandler(_db).Handle(
            new GetImportTemplateQuery(agencyId ?? _agency.Id, isAdmin, PropertyType.Apartment), CancellationToken.None);

    [Fact]
    public async Task ASiteAdmin_GetsTheTemplate_EvenBeforeVerification()
    {
        Assert.False(_agency.IsVerified);
        var file = (await TemplateAsync(isAdmin: true))!;
        Assert.Equal("imova-import-apartment.csv", file.FileName);
        Assert.StartsWith("text/csv", file.ContentType);

        _agency.Deactivate(Now);
        await _db.SaveChangesAsync();
        Assert.NotNull(await TemplateAsync(isAdmin: true));
        Assert.Null(await TemplateAsync(isAdmin: true, agencyId: Guid.NewGuid()));
    }

    [Fact]
    public async Task AnyoneButASiteAdmin_IsRefused()
    {
        // IMOVA imports agencies' listings by hand: the templates are a site admin's tool. (That an
        // agency's own Owner, Admin and Agent get 403 is checked end to end in the integration tests.)
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => TemplateAsync(isAdmin: false));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => new GetImportColumnsHandler(_db).Handle(
            new GetImportColumnsQuery(_agency.Id, false, PropertyType.Garage), CancellationToken.None));
    }

    [Fact]
    public async Task TheColumnsGuide_ListsTheAmenitiesAndProximities_ThatApplyToTheType()
    {
        var columns = (await new GetImportColumnsHandler(_db).Handle(
            new GetImportColumnsQuery(_agency.Id, true, PropertyType.Garage), CancellationToken.None))!;

        var amenities = columns.Single(c => c.Name == ImportColumns.Amenities);
        Assert.Contains("electricity", amenities.Choices!);
        Assert.DoesNotContain("balcony", amenities.Choices!);
        Assert.Equal("List", amenities.Kind);

        var parking = columns.Single(c => c.Name == "parking_type");
        Assert.Equal(("Choice", "Required", "Garage"), (parking.Kind, parking.Requirement, parking.Example));
        Assert.DoesNotContain(columns, c => c.Name == ImportColumns.PetsAllowed);
    }
}
