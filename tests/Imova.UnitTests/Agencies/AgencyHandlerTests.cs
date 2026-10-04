using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Agencies;
using Imova.Application.Features.Agencies.CreateAgency;
using Imova.Application.Features.Agencies.GetAgency;
using Imova.Application.Features.Agencies.Logos;
using Imova.Application.Features.Agencies.UpdateAgency;
using Imova.Domain.Agencies;
using Imova.Domain.Locations;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.Agencies;

public class AgencyHandlerTests : IAsyncDisposable
{
    private static readonly DateTimeOffset Start = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    private readonly ImovaDbContext _db = TestDbContextFactory.Create();
    private readonly FakeBlobStorageService _blobs = new();
    private readonly FakePhotoResizer _resizer = new();
    private readonly ManualTimeProvider _clock = new(Start);
    private readonly Guid _ownerId = Guid.NewGuid();

    public AgencyHandlerTests()
    {
        ListingTestData.AddUser(_db, _ownerId);
        _db.SaveChanges();
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();

    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];
    private static readonly byte[] Gif = "GIF89a......"u8.ToArray();

    private readonly AgencyOptions _options = new();

    private Task<Imova.Contracts.Agencies.AgencyDto> CreateAsync(string name = "Casa Ta Imobiliare", string? email = null, Guid? raionId = null) =>
        CreateWith(_db, _ownerId, name, email, raionId);

    private Task<Imova.Contracts.Agencies.AgencyDto> CreateWith(
        ImovaDbContext db, Guid userId, string name = "Casa Ta Imobiliare", string? email = null, Guid? raionId = null) =>
        new CreateAgencyHandler(db, _blobs, _clock, _options, new FakeDatabaseErrors()).Handle(
            new CreateAgencyCommand(userId, name, "+373 22 555 010", email, null, null, null, raionId), CancellationToken.None);

    private Task<Imova.Contracts.Agencies.AgencyDto?> UpdateAsync(Guid agencyId, Guid userId, string name = "Casa Ta Imobiliare", bool isAdmin = false) =>
        new UpdateAgencyHandler(_db, _blobs, _clock).Handle(
            new UpdateAgencyCommand(agencyId, userId, isAdmin, name, "+373 22 555 010", "office@casata.md", "Bio", null, null, null),
            CancellationToken.None);

    private Task<Imova.Contracts.Agencies.AgencyDto?> UploadLogoAsync(Guid agencyId, Guid userId, byte[] content) =>
        new UploadAgencyLogoHandler(_db, _blobs, _resizer, _clock, NullLogger<UploadAgencyLogoHandler>.Instance).Handle(
            new UploadAgencyLogoCommand(agencyId, userId, false, content), CancellationToken.None);

    private async Task<Guid> AddMemberAsync(Guid agencyId, AgencyRole role)
    {
        var userId = Guid.NewGuid();
        var agency = await _db.Agencies.Include(a => a.Members).SingleAsync(a => a.Id == agencyId);
        agency.AddMember(userId, role, Start);
        await _db.SaveChangesAsync();
        return userId;
    }

    // --- Create ---

    [Fact]
    public async Task Create_MakesTheCallerItsOwner_WithASlugFromTheName()
    {
        var dto = await CreateAsync();

        var agency = await _db.Agencies.Include(a => a.Members).SingleAsync();
        Assert.Equal("casa-ta-imobiliare", dto.Slug);
        Assert.Equal("Owner", dto.MyRole);
        Assert.Equal(1, dto.MemberCount);
        Assert.Equal("+373 22 555 010", dto.Phone);
        Assert.Equal(_ownerId, Assert.Single(agency.Members).UserId);
        Assert.Equal(Start, agency.CreatedAt);
    }

    [Fact]
    public async Task Create_WithoutAnEmail_UsesTheAccountsOwn()
    {
        var dto = await CreateAsync();

        Assert.Equal($"{_ownerId:N}@example.com", dto.Email);
    }

    [Fact]
    public async Task Create_WhenTheSlugIsTaken_NumbersIt()
    {
        await CreateAsync();
        var second = await CreateAsync();
        var third = await CreateAsync("Casa ta imobiliare!");

        Assert.Equal("casa-ta-imobiliare-2", second.Slug);
        Assert.Equal("casa-ta-imobiliare-3", third.Slug);
    }

    [Fact]
    public async Task Create_NeverTakesAnotherAgencysFormerSlug()
    {
        var first = await CreateAsync("Imobil Grup");
        await UpdateAsync(first.Id, _ownerId, "Imobil Grup Nou");

        var second = await CreateAsync("Imobil Grup");

        Assert.Equal("imobil-grup-2", second.Slug);
    }

    [Fact]
    public async Task Create_WithAnUnknownCity_IsAValidationError()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => CreateAsync(raionId: Guid.NewGuid()));

        Assert.Equal(ErrorCodes.AgencyRaionUnknown, Assert.Single(ex.Errors).ErrorCode);
        Assert.Empty(_db.Agencies);
    }

    [Fact]
    public async Task Create_WithAnUnconfirmedEmail_IsForbiddenWithItsCode()
    {
        var unconfirmed = Guid.NewGuid();
        ListingTestData.AddUser(_db, unconfirmed, emailConfirmed: false);
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ForbiddenAccessException>(() => CreateWith(_db, unconfirmed));

        Assert.Equal(ErrorCodes.AgencyEmailNotConfirmed, ex.Code);
        Assert.Empty(_db.Agencies);
    }

    [Fact]
    public async Task Create_BeyondTheLimit_IsRefusedWithItsCode()
    {
        for (var i = 0; i < 3; i++)
        {
            await CreateAsync($"Agenția {i}");
        }

        var ex = await Assert.ThrowsAsync<ValidationException>(() => CreateAsync("Agenția 4"));

        var error = Assert.Single(ex.Errors);
        Assert.Equal(ErrorCodes.AgencyLimitReached, error.ErrorCode);
        Assert.Equal(3, _db.Agencies.Count());
    }

    [Fact]
    public async Task Create_TheLimitIsConfigurable()
    {
        _options.MaxOwnedPerUser = 1;
        await CreateAsync("Prima");

        await Assert.ThrowsAsync<ValidationException>(() => CreateAsync("A doua"));

        _options.MaxOwnedPerUser = 2;
        Assert.NotNull(await CreateAsync("A doua"));
    }

    [Fact]
    public async Task Create_OnlyOwnedAgenciesCountTowardsTheLimit()
    {
        _options.MaxOwnedPerUser = 1;
        var someoneElse = Guid.NewGuid();
        ListingTestData.AddUser(_db, someoneElse);
        await _db.SaveChangesAsync();
        var theirs = await CreateWith(_db, someoneElse, "A lor");
        var agency = await _db.Agencies.Include(a => a.Members).SingleAsync(a => a.Id == theirs.Id);
        agency.AddMember(_ownerId, AgencyRole.Admin, Start);
        await _db.SaveChangesAsync();

        Assert.NotNull(await CreateAsync("A mea"));
    }

    [Fact]
    public async Task Create_WhenAnotherAgencyTakesTheSlugAtTheSameMoment_RetriesOnceWithTheNextNumber()
    {
        var databaseName = Guid.NewGuid().ToString();
        var race = new SlugRaceInterceptor(databaseName);
        await using var db = TestDbContextFactory.Create(databaseName, race);
        ListingTestData.AddUser(db, _ownerId);
        await db.SaveChangesAsync();

        var dto = await CreateWith(db, _ownerId);

        Assert.Equal(1, race.Failures);
        Assert.Equal("casa-ta-imobiliare-2", dto.Slug);
        await using var check = TestDbContextFactory.Create(databaseName);
        Assert.Equal(["casa-ta-imobiliare", "casa-ta-imobiliare-2"], check.Agencies.Select(a => a.Slug).OrderBy(s => s));
        Assert.Single(check.AgencyMembers, m => m.UserId == _ownerId);
    }

    [Fact]
    public async Task Create_WithAKnownCity_NamesIt()
    {
        var raion = Raion.Create(Guid.NewGuid(), "0100", "Chișinău", null, LocalityLabel.Sector);
        _db.Raioane.Add(raion);
        await _db.SaveChangesAsync();

        var dto = await CreateAsync(raionId: raion.Id);

        Assert.Equal(raion.Id, dto.RaionId);
        Assert.Equal("Chișinău", dto.RaionName);
    }

    // --- Get ---

    [Fact]
    public async Task Get_ForThePublic_HidesThePhoneButShowsItsShape()
    {
        var created = await CreateAsync();

        var dto = await new GetAgencyHandler(_db, _blobs).Handle(new GetAgencyQuery(created.Id, null, false), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Null(dto.Phone);
        Assert.Null(dto.MyRole);
        Assert.NotNull(dto.PhonePrefix);
        Assert.True(dto.PhoneHiddenDigits > 0);
    }

    [Fact]
    public async Task Get_UnknownAgency_IsNull()
    {
        Assert.Null(await new GetAgencyHandler(_db, _blobs).Handle(new GetAgencyQuery(Guid.NewGuid(), null, false), CancellationToken.None));
    }

    // --- Update ---

    [Theory]
    [InlineData(AgencyRole.Owner)]
    [InlineData(AgencyRole.Admin)]
    public async Task Update_ByAnOwnerOrAdmin_Succeeds(AgencyRole role)
    {
        var created = await CreateAsync();
        var editor = role == AgencyRole.Owner ? _ownerId : await AddMemberAsync(created.Id, role);
        _clock.Advance(TimeSpan.FromHours(1));

        var dto = await UpdateAsync(created.Id, editor);

        Assert.NotNull(dto);
        Assert.Equal("Bio", dto.Bio);
        Assert.Equal("office@casata.md", dto.Email);
        Assert.Equal(Start.AddHours(1), (await _db.Agencies.SingleAsync()).UpdatedAt);
    }

    [Fact]
    public async Task Update_ByASiteAdmin_Succeeds()
    {
        var created = await CreateAsync();

        Assert.NotNull(await UpdateAsync(created.Id, Guid.NewGuid(), isAdmin: true));
    }

    [Fact]
    public async Task Update_ByAnAgentOrAnOutsider_IsForbidden()
    {
        var created = await CreateAsync();
        var agent = await AddMemberAsync(created.Id, AgencyRole.Agent);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => UpdateAsync(created.Id, agent));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => UpdateAsync(created.Id, Guid.NewGuid()));
        Assert.Null((await _db.Agencies.SingleAsync()).Bio);
    }

    [Fact]
    public async Task Update_UnknownAgency_IsNull()
    {
        Assert.Null(await UpdateAsync(Guid.NewGuid(), _ownerId));
    }

    [Fact]
    public async Task Update_KeepingTheName_KeepsTheSlug()
    {
        var created = await CreateAsync();

        var dto = await UpdateAsync(created.Id, _ownerId);

        Assert.Equal("casa-ta-imobiliare", dto!.Slug);
        Assert.Empty(_db.AgencyFormerSlugs);
    }

    [Fact]
    public async Task Update_Renamed_MovesToANewSlug_AndRemembersTheOldOne()
    {
        var created = await CreateAsync();

        var dto = await UpdateAsync(created.Id, _ownerId, "Casa Noastră");

        Assert.Equal("casa-noastra", dto!.Slug);
        var former = Assert.Single(_db.AgencyFormerSlugs);
        Assert.Equal("casa-ta-imobiliare", former.Slug);
        Assert.Equal(created.Id, former.AgencyId);
    }

    [Fact]
    public async Task Update_RenamedBack_ReclaimsItsOldSlug()
    {
        var created = await CreateAsync();
        await UpdateAsync(created.Id, _ownerId, "Casa Noastră");

        var dto = await UpdateAsync(created.Id, _ownerId, "Casa Ta Imobiliare");

        Assert.Equal("casa-ta-imobiliare", dto!.Slug);
        Assert.Equal(["casa-noastra"], _db.AgencyFormerSlugs.Select(s => s.Slug));
    }

    [Fact]
    public async Task Update_RenamedOnlyInCase_KeepsTheSlug()
    {
        var created = await CreateAsync();

        var dto = await UpdateAsync(created.Id, _ownerId, "CASA TA IMOBILIARE");

        Assert.Equal("casa-ta-imobiliare", dto!.Slug);
        Assert.Empty(_db.AgencyFormerSlugs);
    }

    // --- Logo ---

    [Fact]
    public async Task UploadLogo_StoresBothSquareSizes_AndShowsThem()
    {
        var created = await CreateAsync();

        var dto = await UploadLogoAsync(created.Id, _ownerId, Png);

        var blobName = (await _db.Agencies.SingleAsync()).LogoBlobName!;
        Assert.StartsWith($"agencies/{created.Id}/logo-", blobName);
        Assert.Equal("square-512", System.Text.Encoding.UTF8.GetString(_blobs.UploadedContent[blobName]));
        Assert.Equal("square-128", System.Text.Encoding.UTF8.GetString(_blobs.UploadedContent[AgencyLogo.ThumbnailBlobName(blobName)]));
        Assert.Equal($"https://blob.test/{blobName}", dto!.LogoUrl);
        Assert.Equal($"https://blob.test/{AgencyLogo.ThumbnailBlobName(blobName)}", dto.LogoThumbnailUrl);
    }

    [Fact]
    public async Task UploadLogo_AgainReplacesIt_AndDeletesTheOldFiles()
    {
        var created = await CreateAsync();
        await UploadLogoAsync(created.Id, _ownerId, Png);
        var first = (await _db.Agencies.SingleAsync()).LogoBlobName!;
        _clock.Advance(TimeSpan.FromSeconds(5));

        await UploadLogoAsync(created.Id, _ownerId, Png);

        var second = (await _db.Agencies.SingleAsync()).LogoBlobName!;
        Assert.NotEqual(first, second);
        Assert.Equal(AgencyLogo.AllBlobNames(first), _blobs.DeletedBlobNames);
    }

    [Fact]
    public async Task UploadLogo_OfAnotherImageType_IsRefused()
    {
        var created = await CreateAsync();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => UploadLogoAsync(created.Id, _ownerId, Gif));

        Assert.Equal(ErrorCodes.AgencyLogoType, Assert.Single(ex.Errors).ErrorCode);
        Assert.Equal(0, _resizer.Calls);
    }

    [Fact]
    public async Task UploadLogo_ThatIsNotAnImageAtAll_IsRefusedByItsBytes()
    {
        var created = await CreateAsync();

        // A text file someone renamed to logo.png: the name never reaches the server's check.
        var ex = await Assert.ThrowsAsync<ValidationException>(() => UploadLogoAsync(created.Id, _ownerId, "not an image"u8.ToArray()));

        Assert.Equal(ErrorCodes.AgencyLogoType, Assert.Single(ex.Errors).ErrorCode);
    }

    [Fact]
    public async Task UploadLogo_WithAnImageHeaderButUnreadable_IsNotAnImage()
    {
        var created = await CreateAsync();
        _resizer.Unreadable = true;

        var ex = await Assert.ThrowsAsync<ValidationException>(() => UploadLogoAsync(created.Id, _ownerId, Png));

        Assert.Equal(ErrorCodes.UploadNotAnImage, Assert.Single(ex.Errors).ErrorCode);
        Assert.Null((await _db.Agencies.SingleAsync()).LogoBlobName);
    }

    [Theory]
    [InlineData(199, 800)]
    [InlineData(800, 199)]
    public async Task UploadLogo_SmallerThan200OnEitherSide_IsRefused(int width, int height)
    {
        var created = await CreateAsync();
        _resizer.Size = (width, height);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => UploadLogoAsync(created.Id, _ownerId, Png));

        Assert.Equal(ErrorCodes.AgencyLogoTooSmall, Assert.Single(ex.Errors).ErrorCode);
        Assert.Empty(_blobs.UploadedContent);
    }

    [Fact]
    public async Task UploadLogo_OfExactly200By200_IsAccepted()
    {
        var created = await CreateAsync();
        _resizer.Size = (200, 200);

        Assert.NotNull((await UploadLogoAsync(created.Id, _ownerId, Png))!.LogoUrl);
    }

    [Fact]
    public async Task UploadLogo_ByAnAgent_IsForbidden()
    {
        var created = await CreateAsync();
        var agent = await AddMemberAsync(created.Id, AgencyRole.Agent);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => UploadLogoAsync(created.Id, agent, Png));
        Assert.Empty(_blobs.UploadedContent);
    }

    [Fact]
    public async Task RemoveLogo_ClearsIt_AndDeletesBothFiles()
    {
        var created = await CreateAsync();
        await UploadLogoAsync(created.Id, _ownerId, Png);
        var blobName = (await _db.Agencies.SingleAsync()).LogoBlobName!;

        var dto = await new RemoveAgencyLogoHandler(_db, _blobs, _clock, NullLogger<RemoveAgencyLogoHandler>.Instance)
            .Handle(new RemoveAgencyLogoCommand(created.Id, _ownerId, false), CancellationToken.None);

        Assert.Null(dto!.LogoUrl);
        Assert.Null((await _db.Agencies.SingleAsync()).LogoBlobName);
        Assert.Equal(AgencyLogo.AllBlobNames(blobName), _blobs.DeletedBlobNames);
    }

    [Fact]
    public void UploadLogo_OverFiveMegabytes_IsRefusedByTheValidator()
    {
        var result = new UploadAgencyLogoValidator().Validate(
            new UploadAgencyLogoCommand(Guid.NewGuid(), Guid.NewGuid(), false, new byte[AgencyLogo.MaxFileSizeBytes + 1]));

        Assert.Equal(ErrorCodes.UploadTooLarge, Assert.Single(result.Errors).ErrorCode);
    }
}
