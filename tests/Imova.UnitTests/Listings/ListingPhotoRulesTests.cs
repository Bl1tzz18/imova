using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Features.Listings;
using Imova.Application.Features.Listings.ApproveListing;
using Imova.Application.Features.Listings.PublishListing;
using Imova.Application.Features.Listings.RenewListing;
using Imova.Application.Features.Listings.SubmitListingForReview;
using Imova.Application.Features.Media.DeleteMedia;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Imova.UnitTests.Listings;

// Every listing shown on IMOVA has at least three photos (ListingPhotoRules): no way into review or
// onto the site without them, and a live listing can't drop under them.
public class ListingPhotoRulesTests : IAsyncDisposable
{
    private readonly ImovaDbContext _db = TestDbContextFactory.Create();
    private readonly FakeBlobStorageService _blobs = new();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _publisherId;

    public ListingPhotoRulesTests()
    {
        ListingTestData.AddUser(_db, _ownerId);
        _publisherId = ListingTestData.AddIndividualPublisher(_db, _ownerId).Id;
        _db.SaveChanges();
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();

    private async Task<Listing> SeedAsync(ListingStatus status, int photos)
    {
        var listing = ListingTestData.AddListing(_db, _publisherId, photos: photos).MoveTo(status);
        await _db.SaveChangesAsync();
        return listing;
    }

    private static async Task ExpectNotEnoughPhotosAsync(Func<Task> act, int count)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(act);
        var failure = Assert.Single(ex.Errors);
        Assert.Equal(ErrorCodes.ListingNotEnoughPhotos, failure.ErrorCode);
        var parameters = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(failure.CustomState);
        Assert.Equal(ListingPhotoRules.MinPhotos, parameters["min"]);
        Assert.Equal(count, parameters["count"]);
    }

    [Fact]
    public async Task SubmittingADraftWithTwoPhotos_IsRefused_ItStaysADraft()
    {
        var listing = await SeedAsync(ListingStatus.Draft, photos: 2);

        await ExpectNotEnoughPhotosAsync(
            () => new SubmitListingForReviewHandler(_db, _blobs).Handle(new SubmitListingForReviewCommand(listing.Id, _ownerId, false), CancellationToken.None),
            count: 2);

        Assert.Equal(ListingStatus.Draft, (await _db.Listings.SingleAsync(l => l.Id == listing.Id)).Status);
    }

    [Fact]
    public async Task RePublishing_Renewing_AndApproving_NeedThePhotosToo()
    {
        var archived = await SeedAsync(ListingStatus.Archived, photos: 1);
        var active = await SeedAsync(ListingStatus.Active, photos: 0);
        var inReview = await SeedAsync(ListingStatus.PendingReview, photos: 2);

        await ExpectNotEnoughPhotosAsync(
            () => new PublishListingHandler(_db, _blobs).Handle(new PublishListingCommand(archived.Id, _ownerId, false), CancellationToken.None), 1);
        await ExpectNotEnoughPhotosAsync(
            () => new RenewListingHandler(_db, _blobs).Handle(new RenewListingCommand(active.Id, _ownerId, false), CancellationToken.None), 0);
        await ExpectNotEnoughPhotosAsync(
            () => new ApproveListingHandler(_db, _blobs).Handle(new ApproveListingCommand(inReview.Id, true), CancellationToken.None), 2);
    }

    [Fact]
    public async Task WithThreePhotos_SubmittingWorks()
    {
        var listing = await SeedAsync(ListingStatus.Draft, photos: 3);

        var dto = await new SubmitListingForReviewHandler(_db, _blobs)
            .Handle(new SubmitListingForReviewCommand(listing.Id, _ownerId, false), CancellationToken.None);

        Assert.Equal("PendingReview", dto!.Status);
    }

    [Theory]
    [InlineData(ListingStatus.Active)]
    [InlineData(ListingStatus.PendingReview)]
    [InlineData(ListingStatus.Rejected)]
    [InlineData(ListingStatus.Suspended)]
    public async Task EditingAListingInReviewOrLiveOrToBeResubmitted_NeedsThePhotos(ListingStatus status)
    {
        var listing = await SeedAsync(status, photos: 2);

        await ExpectNotEnoughPhotosAsync(() => ListingPhotoRules.EnsureEnoughForEditAsync(_db, listing, CancellationToken.None), 2);
    }

    [Theory]
    [InlineData(ListingStatus.Draft)]
    [InlineData(ListingStatus.Archived)]
    [InlineData(ListingStatus.Expired)]
    public async Task EditingADraftOrAnEndedListing_DoesntYet(ListingStatus status)
    {
        var listing = await SeedAsync(status, photos: 0);

        await ListingPhotoRules.EnsureEnoughForEditAsync(_db, listing, CancellationToken.None);
    }

    [Fact]
    public async Task ALiveListing_CantDropUnderThreePhotos_ButCanAboveIt()
    {
        var listing = await SeedAsync(ListingStatus.Active, photos: 4);
        var handler = new DeleteMediaHandler(_db, _blobs);
        var photos = await _db.Photos.Where(p => p.ListingId == listing.Id).Select(p => p.Id).ToListAsync();

        Assert.True(await handler.Handle(new DeleteMediaCommand(listing.Id, photos[0], _ownerId, false), CancellationToken.None));

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new DeleteMediaCommand(listing.Id, photos[1], _ownerId, false), CancellationToken.None));
        Assert.Equal(ErrorCodes.ListingLastPhotos, Assert.Single(ex.Errors).ErrorCode);
        Assert.Equal(3, await _db.Photos.CountAsync(p => p.ListingId == listing.Id));
    }

    [Theory]
    [InlineData(ListingStatus.Draft)]
    [InlineData(ListingStatus.Rejected)]
    [InlineData(ListingStatus.Archived)]
    public async Task OffTheSite_PhotosCanBeRemoved_TheRuleWaitsForTheNextStep(ListingStatus status)
    {
        var listing = await SeedAsync(status, photos: 3);
        var photo = await _db.Photos.FirstAsync(p => p.ListingId == listing.Id);

        Assert.True(await new DeleteMediaHandler(_db, _blobs).Handle(new DeleteMediaCommand(listing.Id, photo.Id, _ownerId, false), CancellationToken.None));
    }
}
