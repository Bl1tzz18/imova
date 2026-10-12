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

    private async Task<Guid> AddMemberAsync(AgencyRole role)
    {
        var userId = Guid.NewGuid();
        ListingTestData.AddUser(_db, userId);
        _agency.AddMember(userId, role, Now);
        await _db.SaveChangesAsync();
        return userId;
    }

    private Task<ImportTemplateFile?> TemplateAsync(Guid userId, bool isAdmin = false, Guid? agencyId = null) =>
        new GetImportTemplateHandler(_db).Handle(
            new GetImportTemplateQuery(agencyId ?? _agency.Id, userId, isAdmin, PropertyType.Apartment), CancellationToken.None);

    [Fact]
    public async Task ASiteAdmin_GetsTheTemplate_EvenBeforeVerification()
    {
        Assert.False(_agency.IsVerified);
        var file = (await TemplateAsync(Guid.NewGuid(), isAdmin: true))!;
        Assert.Equal("imova-import-apartment.csv", file.FileName);
        Assert.StartsWith("text/csv", file.ContentType);

        _agency.Deactivate(Now);
        await _db.SaveChangesAsync();
        Assert.NotNull(await TemplateAsync(Guid.NewGuid(), isAdmin: true));
        Assert.Null(await TemplateAsync(Guid.NewGuid(), isAdmin: true, agencyId: Guid.NewGuid()));
    }

    [Fact]
    public async Task TheAgencysOwnOwnersAdminsAndAgents_AndStrangers_AreRefused()
    {
        // IMOVA imports agencies' listings by hand: the templates are a site admin's tool.
        var admin = await AddMemberAsync(AgencyRole.Admin);
        var agent = await AddMemberAsync(AgencyRole.Agent);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => TemplateAsync(_owner));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => TemplateAsync(admin));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => TemplateAsync(agent));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => TemplateAsync(Guid.NewGuid()));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => new GetImportColumnsHandler(_db).Handle(
            new GetImportColumnsQuery(_agency.Id, _owner, false, PropertyType.Garage), CancellationToken.None));
    }

    [Fact]
    public async Task TheColumnsGuide_ListsTheAmenitiesAndProximities_ThatApplyToTheType()
    {
        var columns = (await new GetImportColumnsHandler(_db).Handle(
            new GetImportColumnsQuery(_agency.Id, Guid.NewGuid(), true, PropertyType.Garage), CancellationToken.None))!;

        var amenities = columns.Single(c => c.Name == ImportColumns.Amenities);
        Assert.Contains("electricity", amenities.Choices!);
        Assert.DoesNotContain("balcony", amenities.Choices!);
        Assert.Equal("List", amenities.Kind);

        var parking = columns.Single(c => c.Name == "parking_type");
        Assert.Equal(("Choice", "Required", "Garage"), (parking.Kind, parking.Requirement, parking.Example));
        Assert.DoesNotContain(columns, c => c.Name == ImportColumns.PetsAllowed);
    }
}
