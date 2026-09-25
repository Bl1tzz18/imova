using Imova.Application.Features.Listings.GetListingById;
using Imova.Domain.Favorites;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Listings;

public class GetListingByIdHandlerTests
{
    private readonly ImovaDbContext _dbContext = TestDbContextFactory.Create();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _publisherId;

    public GetListingByIdHandlerTests()
    {
        _publisherId = ListingTestData.AddIndividualPublisher(_dbContext, _ownerId).Id;
    }

    private async Task<Imova.Contracts.Listings.ListingDto?> GetAsync(Guid id, Guid? currentUserId = null, bool isAdmin = false)
    {
        await _dbContext.SaveChangesAsync(CancellationToken.None);
        return await new GetListingByIdHandler(_dbContext, new FakeBlobStorageService())
            .Handle(new GetListingByIdQuery(id, currentUserId, isAdmin), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ForActiveListing_ReturnsItWithPublisherContactAndExactLocation()
    {
        // No Contact of its own (created before the Contact step) — falls back to the publisher's.
        var listing = ListingTestData.AddListing(_dbContext, _publisherId).MoveTo(ListingStatus.Active);

        var dto = await GetAsync(listing.Id);

        Assert.NotNull(dto);
        Assert.Equal("+373 69 123 456", dto!.Contact!.Phone);
        Assert.Equal("ion@example.com", dto.Contact.Email);
        Assert.Null(dto.Publisher.Phone);
        Assert.Equal("Strada Ismail", dto.Property.Location!.Street);
        Assert.Equal(47.0105, dto.Property.Location.Latitude);
    }

    [Fact]
    public async Task Handle_WithAHiddenPhone_LeavesItOutForAnonymousAndOtherUsers()
    {
        var listing = ListingTestData.AddListing(_dbContext, _publisherId, contact: TestContacts.HiddenPhone)
            .MoveTo(ListingStatus.Active);

        var anonymous = await GetAsync(listing.Id);
        var otherUser = await GetAsync(listing.Id, Guid.NewGuid());

        Assert.Null(anonymous!.Contact!.Phone);
        Assert.Null(anonymous.Publisher.Phone);
        Assert.True(anonymous.Contact.HidePhoneNumber);
        Assert.Null(otherUser!.Contact!.Phone);
    }

    [Fact]
    public async Task Handle_WithAHiddenPhone_StillShowsItToTheOwnerAndAdmins()
    {
        var listing = ListingTestData.AddListing(_dbContext, _publisherId, contact: TestContacts.HiddenPhone)
            .MoveTo(ListingStatus.Active);

        Assert.Equal("+373 69 555 666", (await GetAsync(listing.Id, _ownerId))!.Contact!.Phone);
        Assert.Equal("+373 69 555 666", (await GetAsync(listing.Id, Guid.NewGuid(), isAdmin: true))!.Contact!.Phone);
    }

    [Fact]
    public async Task Handle_ForUnknownId_ReturnsNull()
    {
        Assert.Null(await GetAsync(Guid.NewGuid()));
    }

    [Theory]
    [InlineData(ListingStatus.Draft)]
    [InlineData(ListingStatus.PendingReview)]
    [InlineData(ListingStatus.Archived)]
    public async Task Handle_ForNonActiveListing_IsHiddenFromAnonymousAndOtherUsers(ListingStatus status)
    {
        var listing = ListingTestData.AddListing(_dbContext, _publisherId).MoveTo(status);

        Assert.Null(await GetAsync(listing.Id));
        Assert.Null(await GetAsync(listing.Id, currentUserId: Guid.NewGuid()));
    }

    [Fact]
    public async Task Handle_ForNonActiveListing_IsVisibleToItsOwnerAndToAdmins()
    {
        var listing = ListingTestData.AddListing(_dbContext, _publisherId).MoveTo(ListingStatus.Archived);

        Assert.NotNull(await GetAsync(listing.Id, currentUserId: _ownerId));
        Assert.NotNull(await GetAsync(listing.Id, currentUserId: Guid.NewGuid(), isAdmin: true));
    }

    [Fact]
    public async Task Handle_ForNonActiveAgencyListing_IsVisibleToTheAgencysOwner()
    {
        var agencyOwner = Guid.NewGuid();
        var agency = ListingTestData.AddAgencyPublisher(_dbContext, agencyOwner);
        var listing = ListingTestData.AddListing(_dbContext, agency.Id).MoveTo(ListingStatus.PendingReview);

        Assert.NotNull(await GetAsync(listing.Id, currentUserId: agencyOwner));
    }

    [Fact]
    public async Task Handle_ReportsIsSavedForTheCurrentUserOnly()
    {
        var listing = ListingTestData.AddListing(_dbContext, _publisherId).MoveTo(ListingStatus.Active);
        var saver = Guid.NewGuid();
        _dbContext.Favorites.Add(Favorite.Create(saver, listing.Id));

        Assert.True((await GetAsync(listing.Id, currentUserId: saver))!.IsSaved);
        Assert.False((await GetAsync(listing.Id, currentUserId: Guid.NewGuid()))!.IsSaved);
        Assert.False((await GetAsync(listing.Id))!.IsSaved);
    }

    [Fact]
    public async Task Handle_ReturnsPhotosInSortOrderWithUrls()
    {
        var listing = ListingTestData.AddListing(_dbContext, _publisherId).MoveTo(ListingStatus.Active);
        _dbContext.Photos.Add(Photo.Create(listing.Id, "b.jpg", "image/jpeg", 10, sortOrder: 1));
        _dbContext.Photos.Add(Photo.Create(listing.Id, "a.jpg", "image/jpeg", 10, sortOrder: 0, isPrimary: true));

        var dto = await GetAsync(listing.Id);

        Assert.Equal(["https://blob.test/a.jpg", "https://blob.test/b.jpg"], dto!.Photos.Select(p => p.Url));
        Assert.True(dto.Photos[0].IsPrimary);
    }
}
