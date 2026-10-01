using Imova.Application.Features.Media.Cleanup;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.Media;

public class AbandonedPhotoCleanupTests
{
    private readonly ImovaDbContext _db = TestDbContextFactory.Create();

    // Photo.Create stamps CreatedAt from the real clock; the job's clock is moved from there.
    private readonly ManualTimeProvider _clock = new(DateTimeOffset.UtcNow);
    private readonly FakeBlobStorageService _blobs = new();

    private Photo AddPhoto(Guid listingId, string name)
    {
        var photo = Photo.Create(listingId, name, "image/jpeg", 100);
        _db.Photos.Add(photo);
        _db.SaveChanges();
        return photo;
    }

    private Task<int> RunAsync() => new AbandonedPhotoCleanup(
            _db, _blobs, _clock, NullLogger<AbandonedPhotoCleanup>.Instance)
        .RunAsync(CancellationToken.None);

    [Fact]
    public async Task PhotosOfAListingThatWasNeverCreated_AreDeletedAfterAWeek()
    {
        AddPhoto(Guid.NewGuid(), "abandoned.jpg");

        _clock.Advance(TimeSpan.FromDays(6));
        Assert.Equal(0, await RunAsync());
        Assert.Single(_db.Photos);

        _clock.Advance(TimeSpan.FromDays(2));
        Assert.Equal(1, await RunAsync());
        Assert.Empty(_db.Photos);
        Assert.Equal(["abandoned.jpg", "abandoned_400.jpg", "abandoned_800.jpg", "abandoned_1600.jpg"], _blobs.DeletedBlobNames);
    }

    [Fact]
    public async Task PhotosOfExistingListings_IncludingDrafts_AreKept()
    {
        var publisher = ListingTestData.AddIndividualPublisher(_db);
        var draft = ListingTestData.AddListing(_db, publisher.Id);
        _db.SaveChanges();
        AddPhoto(draft.Id, "draft.jpg");

        _clock.Advance(TimeSpan.FromDays(60));

        Assert.Equal(0, await RunAsync());
        Assert.Single(_db.Photos);
        Assert.Empty(_blobs.DeletedBlobNames);
    }

    [Fact]
    public async Task APhotoWhoseFileCannotBeDeleted_KeepsItsRowForTheNextRun()
    {
        AddPhoto(Guid.NewGuid(), "stuck.jpg");
        AddPhoto(Guid.NewGuid(), "fine.jpg");
        _blobs.FailingDeletes.Add("stuck.jpg");
        _clock.Advance(TimeSpan.FromDays(8));

        Assert.Equal(1, await RunAsync());
        Assert.Equal("stuck.jpg", Assert.Single(_db.Photos).BlobName);

        _blobs.FailingDeletes.Clear();
        Assert.Equal(1, await RunAsync());
        Assert.Empty(_db.Photos);
    }
}
