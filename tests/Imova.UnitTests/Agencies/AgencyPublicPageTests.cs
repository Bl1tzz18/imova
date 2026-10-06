using Imova.Application.Features.Agencies.Directory;
using Imova.Application.Features.Agencies.GetAgency;
using Imova.Contracts.Agencies;
using Imova.Domain.Agencies;
using Imova.Domain.Listings;
using Imova.Domain.Locations;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Agencies;

// The public side of agencies (step 5): the page by slug, the directory and the phone reveal.
public class AgencyPublicPageTests : IAsyncDisposable
{
    private readonly ImovaDbContext _db = TestDbContextFactory.Create();
    private readonly FakeBlobStorageService _blobs = new();
    private readonly Guid _ownerId = Guid.NewGuid();

    public ValueTask DisposeAsync() => _db.DisposeAsync();

    private Agency AddAgency(string name, bool verified = false, bool deactivated = false, Guid? raionId = null)
    {
        var agency = ListingTestData.AddAgency(_db, _ownerId, name);
        var entry = _db.Entry(agency);
        // Verification and deactivation have no domain methods yet (step 7); set them directly.
        entry.Property(a => a.IsVerified).CurrentValue = verified;
        entry.Property(a => a.Status).CurrentValue = deactivated ? AgencyStatus.Deactivated : AgencyStatus.Active;
        entry.Property(a => a.RaionId).CurrentValue = raionId;
        return agency;
    }

    private void AddListings(Agency agency, int active, int drafts = 0)
    {
        var publisher = ListingTestData.AddIndividualPublisher(_db, _ownerId);
        for (var i = 0; i < active; i++)
        {
            ListingTestData.AddListing(_db, publisher.Id, agencyId: agency.Id).MoveTo(ListingStatus.Active);
        }

        for (var i = 0; i < drafts; i++)
        {
            ListingTestData.AddListing(_db, publisher.Id, agencyId: agency.Id);
        }
    }

    private Task<AgencyBySlugResult?> BySlugAsync(string slug, Guid? userId = null, bool isAdmin = false) =>
        new GetAgencyBySlugHandler(_db, _blobs).Handle(new GetAgencyBySlugQuery(slug, userId, isAdmin), CancellationToken.None);

    private Task<Imova.Contracts.Common.PagedResult<AgencyCardDto>> DirectoryAsync(
        string? q = null, Guid? raionId = null, bool verifiedOnly = false, int page = 1, int pageSize = 24) =>
        new GetAgencyDirectoryHandler(_db, _blobs).Handle(
            new GetAgencyDirectoryQuery(q, raionId, verifiedOnly, page, pageSize), CancellationToken.None);

    private Task<AgencyPhoneDto?> RevealAsync(Guid agencyId, Guid? userId = null, bool isAdmin = false) =>
        new RevealAgencyPhoneHandler(_db).Handle(new RevealAgencyPhoneQuery(agencyId, userId, isAdmin), CancellationToken.None);

    // --- By slug ---

    [Fact]
    public async Task BySlug_CurrentSlug_ReturnsTheAgency_WithoutItsPhoneForThePublic()
    {
        var agency = AddAgency("Casa Ta Imobiliare");
        AddListings(agency, active: 2, drafts: 1);
        await _db.SaveChangesAsync();

        var result = await BySlugAsync("casa-ta-imobiliare");

        Assert.NotNull(result?.Agency);
        Assert.Null(result.CurrentSlug);
        Assert.Equal(agency.Id, result.Agency.Id);
        Assert.Equal(2, result.Agency.ActiveListingCount);
        Assert.Null(result.Agency.Phone);
        Assert.NotNull(result.Agency.PhonePrefix);
    }

    [Fact]
    public async Task BySlug_IgnoresCapitalsAndSpaces()
    {
        AddAgency("Casa Ta Imobiliare");
        await _db.SaveChangesAsync();

        Assert.NotNull((await BySlugAsync("  Casa-Ta-Imobiliare "))?.Agency);
    }

    [Fact]
    public async Task BySlug_FormerSlug_GivesTheCurrentOne()
    {
        var agency = AddAgency("Casa Noua");
        _db.AgencyFormerSlugs.Add(AgencyFormerSlug.Create("casa-veche", agency.Id, DateTimeOffset.UtcNow));
        await _db.SaveChangesAsync();

        var result = await BySlugAsync("casa-veche");

        Assert.NotNull(result);
        Assert.Null(result.Agency);
        Assert.Equal("casa-noua", result.CurrentSlug);
    }

    [Fact]
    public async Task BySlug_Unknown_IsNull()
    {
        AddAgency("Casa Ta Imobiliare");
        await _db.SaveChangesAsync();

        Assert.Null(await BySlugAsync("nu-exista"));
    }

    [Fact]
    public async Task BySlug_Deactivated_IsHiddenFromThePublic_ButNotFromMembersOrAdmins()
    {
        var agency = AddAgency("Casa Inchisa", deactivated: true);
        _db.AgencyFormerSlugs.Add(AgencyFormerSlug.Create("casa-inchisa-veche", agency.Id, DateTimeOffset.UtcNow));
        await _db.SaveChangesAsync();

        Assert.Null(await BySlugAsync("casa-inchisa"));
        Assert.Null(await BySlugAsync("casa-inchisa", Guid.NewGuid()));
        Assert.Null(await BySlugAsync("casa-inchisa-veche"));
        Assert.NotNull((await BySlugAsync("casa-inchisa", _ownerId))?.Agency);
        Assert.NotNull((await BySlugAsync("casa-inchisa", Guid.NewGuid(), isAdmin: true))?.Agency);
        Assert.Equal("casa-inchisa", (await BySlugAsync("casa-inchisa-veche", _ownerId))?.CurrentSlug);
    }

    // --- Directory ---

    [Fact]
    public async Task Directory_VerifiedFirst_ThenMostActiveListings_ThenByName()
    {
        var small = AddAgency("Beta Imobil");
        var big = AddAgency("Zeta Imobil");
        var verified = AddAgency("Omega Imobil", verified: true);
        var alpha = AddAgency("Alfa Imobil");
        AddListings(small, active: 1, drafts: 5);
        AddListings(big, active: 3);
        await _db.SaveChangesAsync();

        var result = await DirectoryAsync();

        Assert.Equal([verified.Id, big.Id, small.Id, alpha.Id], result.Items.Select(a => a.Id));
        Assert.Equal([0, 3, 1, 0], result.Items.Select(a => a.ActiveListingCount));
        Assert.Equal(4, result.TotalCount);
    }

    [Fact]
    public async Task Directory_LeavesOutDeactivatedAgencies()
    {
        AddAgency("Activa");
        AddAgency("Inchisa", deactivated: true);
        await _db.SaveChangesAsync();

        var result = await DirectoryAsync();

        Assert.Equal("Activa", Assert.Single(result.Items).Name);
    }

    [Theory]
    [InlineData("agentia")]
    [InlineData("AGENȚIA")]
    [InlineData("usor")]
    [InlineData("Ușor")]
    public async Task Directory_Q_MatchesTheName_WithOrWithoutDiacritics(string q)
    {
        AddAgency("Agenția Ușor");
        AddAgency("Casa Ta");
        await _db.SaveChangesAsync();

        var result = await DirectoryAsync(q);

        Assert.Equal("Agenția Ușor", Assert.Single(result.Items).Name);
    }

    [Fact]
    public async Task Directory_Q_InCyrillic_MatchesTheTransliteratedSlug()
    {
        AddAgency("Дом Плюс");
        AddAgency("Casa Ta");
        await _db.SaveChangesAsync();

        var result = await DirectoryAsync("дом");

        Assert.Equal("Дом Плюс", Assert.Single(result.Items).Name);
    }

    [Fact]
    public async Task Directory_FiltersByCityAndVerified_AndNamesTheCity()
    {
        var raion = Raion.Create(Guid.NewGuid(), "0100", "Chișinău", null, LocalityLabel.Sector);
        _db.Raioane.Add(raion);
        AddAgency("In Chisinau", raionId: raion.Id);
        AddAgency("Verificata in Chisinau", verified: true, raionId: raion.Id);
        AddAgency("Fara oras", verified: true);
        await _db.SaveChangesAsync();

        var inCity = await DirectoryAsync(raionId: raion.Id);
        var verifiedInCity = await DirectoryAsync(raionId: raion.Id, verifiedOnly: true);

        Assert.Equal(2, inCity.TotalCount);
        Assert.All(inCity.Items, a => Assert.Equal("Chișinău", a.RaionName));
        Assert.Equal("Verificata in Chisinau", Assert.Single(verifiedInCity.Items).Name);
    }

    [Fact]
    public async Task Directory_Pages_KeepTheTotal()
    {
        for (var i = 0; i < 5; i++)
        {
            AddAgency($"Agentia {i}");
        }

        await _db.SaveChangesAsync();

        var second = await DirectoryAsync(page: 2, pageSize: 2);

        Assert.Equal(["Agentia 2", "Agentia 3"], second.Items.Select(a => a.Name));
        Assert.Equal(5, second.TotalCount);
        Assert.Equal(2, second.Page);
    }

    [Theory]
    [InlineData(0, 24, true)]
    [InlineData(1, 0, true)]
    [InlineData(1, 51, true)]
    [InlineData(1, 50, false)]
    public void DirectoryValidator_BoundsThePaging(int page, int pageSize, bool invalid)
    {
        var result = new GetAgencyDirectoryValidator().Validate(new GetAgencyDirectoryQuery(null, null, false, page, pageSize));

        Assert.Equal(invalid, !result.IsValid);
    }

    // --- Phone reveal ---

    [Fact]
    public async Task RevealPhone_GivesTheNumber_ToAnyone_WhileActive()
    {
        var agency = AddAgency("Casa Ta");
        await _db.SaveChangesAsync();

        Assert.Equal("+373 22 000 000", (await RevealAsync(agency.Id))?.Phone);
    }

    [Fact]
    public async Task RevealPhone_DeactivatedOrUnknown_IsNull_ExceptForMembers()
    {
        var agency = AddAgency("Casa Inchisa", deactivated: true);
        await _db.SaveChangesAsync();

        Assert.Null(await RevealAsync(agency.Id));
        Assert.Null(await RevealAsync(Guid.NewGuid()));
        Assert.NotNull(await RevealAsync(agency.Id, _ownerId));
    }
}
