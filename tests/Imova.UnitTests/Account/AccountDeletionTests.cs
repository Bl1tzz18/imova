using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Account;
using Imova.Domain.Favorites;
using Imova.Domain.Listings;
using Imova.Domain.Messaging;
using Imova.Domain.SavedSearches;
using Imova.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.UnitTests.Account;

// What deleting an account removes, what it deliberately keeps, and what the people it talked to
// see afterwards. The seller (Ion) of MessagingFixture is the account being deleted unless a test
// says otherwise.
public class AccountDeletionTests
{
    private readonly MessagingFixture _f = new();

    private AccountDeletion Deletion() =>
        new(
            _f.Db,
            _f.Blobs,
            new AccountDeletionEmails(TestUserManagerFactory.Create(new FakeUserStore()), _f.Email, _f.App),
            NullLogger<AccountDeletion>.Instance);

    private Task<bool> DeleteAsync(Guid userId) => Deletion().DeleteAsync(userId, CancellationToken.None);

    private Photo AddPhoto(Guid listingId, Guid? uploadedBy = null)
    {
        var photo = Photo.Create(listingId, $"{listingId}/{Guid.NewGuid()}.jpg", "image/jpeg", 1000, uploadedByUserId: uploadedBy);
        _f.Db.Photos.Add(photo);
        _f.Blobs.StoredBlobNames.Add(photo.BlobName);
        return photo;
    }

    private void AddRefreshToken(Guid userId) =>
        _f.Db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionId = Guid.NewGuid(),
            TokenHash = Guid.NewGuid().ToString("N"),
            StampFingerprint = "stamp",
            CreatedAt = _f.Clock.Now,
            ExpiresAt = _f.Clock.Now.AddDays(1),
            SessionExpiresAt = _f.Clock.Now.AddDays(90),
        });

    [Fact]
    public async Task DeleteAsync_RemovesTheAccountAndEverythingItOwns()
    {
        var seller = _f.Seller;
        var agency = ListingTestData.AddAgencyPublisher(_f.Db, seller.Id);
        var agencyListing = ListingTestData.AddListing(_f.Db, agency.Id);
        var photo = AddPhoto(_f.Listing.Id, seller.Id);
        var property = _f.Listing.PropertyId;
        _f.Db.Favorites.Add(Favorite.Create(_f.Visitor.Id, _f.Listing.Id));
        _f.Db.Favorites.Add(Favorite.Create(seller.Id, Guid.NewGuid()));
        _f.Db.SavedSearches.Add(SavedSearch.Create(seller.Id, "Chirii", "transactionType=Rent", AlertFrequency.Daily, _f.Clock.Now));
        AddRefreshToken(seller.Id);
        _f.Db.UserBlocks.Add(new UserBlock(seller.Id, _f.Visitor.Id, _f.Clock.Now));
        _f.Db.UserBlocks.Add(new UserBlock(_f.Visitor.Id, seller.Id, _f.Clock.Now));
        await _f.Db.SaveChangesAsync();

        Assert.True(await DeleteAsync(seller.Id));

        Assert.False(await _f.Db.Users.AnyAsync(u => u.Id == seller.Id));
        Assert.False(await _f.Db.Publishers.AnyAsync(p => p.UserId == seller.Id));
        Assert.False(await _f.Db.Listings.AnyAsync(l => l.Id == _f.Listing.Id || l.Id == agencyListing.Id));
        Assert.False(await _f.Db.Properties.AnyAsync(p => p.Id == property));
        Assert.False(await _f.Db.Photos.AnyAsync());
        Assert.False(await _f.Db.Favorites.AnyAsync());
        Assert.False(await _f.Db.SavedSearches.AnyAsync());
        Assert.False(await _f.Db.RefreshTokens.AnyAsync());
        Assert.False(await _f.Db.UserBlocks.AnyAsync());
        Assert.Contains(photo.BlobName, _f.Blobs.DeletedBlobNames);
    }

    [Fact]
    public async Task DeleteAsync_LeavesOtherAccountsAndTheirDataAlone()
    {
        var otherPublisher = ListingTestData.AddIndividualPublisher(_f.Db, _f.Visitor.Id);
        var othersListing = ListingTestData.AddListing(_f.Db, otherPublisher.Id);
        var othersPhoto = AddPhoto(othersListing.Id, _f.Visitor.Id);
        _f.Db.Favorites.Add(Favorite.Create(_f.Visitor.Id, othersListing.Id));
        AddRefreshToken(_f.Visitor.Id);
        await _f.Db.SaveChangesAsync();

        await DeleteAsync(_f.Seller.Id);

        Assert.True(await _f.Db.Users.AnyAsync(u => u.Id == _f.Visitor.Id));
        Assert.True(await _f.Db.Listings.AnyAsync(l => l.Id == othersListing.Id));
        Assert.True(await _f.Db.Photos.AnyAsync(p => p.Id == othersPhoto.Id));
        Assert.Equal(1, await _f.Db.Favorites.CountAsync());
        Assert.Equal(1, await _f.Db.RefreshTokens.CountAsync());
        Assert.DoesNotContain(othersPhoto.BlobName, _f.Blobs.DeletedBlobNames);
    }

    [Fact]
    public async Task DeleteAsync_RemovesPhotosUploadedForAListingThatWasNeverCreated()
    {
        var stray = AddPhoto(Guid.NewGuid(), _f.Seller.Id);
        var someoneElses = AddPhoto(Guid.NewGuid(), _f.Visitor.Id);
        await _f.Db.SaveChangesAsync();

        await DeleteAsync(_f.Seller.Id);

        Assert.False(await _f.Db.Photos.AnyAsync(p => p.Id == stray.Id));
        Assert.True(await _f.Db.Photos.AnyAsync(p => p.Id == someoneElses.Id));
        Assert.Contains(stray.BlobName, _f.Blobs.DeletedBlobNames);
    }

    [Fact]
    public async Task DeleteAsync_DeletesEveryProfilePictureOfTheAccount()
    {
        _f.Seller.ProfilePictureUrl = $"https://blob.test/profile-pictures/{_f.Seller.Id}/current.jpg";
        _f.Blobs.StoredBlobNames.Add($"profile-pictures/{_f.Seller.Id}/current.jpg");
        _f.Blobs.StoredBlobNames.Add($"profile-pictures/{_f.Seller.Id}/older.jpg");
        _f.Blobs.StoredBlobNames.Add($"profile-pictures/{_f.Visitor.Id}/theirs.jpg");
        await _f.Db.SaveChangesAsync();

        await DeleteAsync(_f.Seller.Id);

        Assert.Contains($"profile-pictures/{_f.Seller.Id}/current.jpg", _f.Blobs.DeletedBlobNames);
        Assert.Contains($"profile-pictures/{_f.Seller.Id}/older.jpg", _f.Blobs.DeletedBlobNames);
        Assert.DoesNotContain($"profile-pictures/{_f.Visitor.Id}/theirs.jpg", _f.Blobs.DeletedBlobNames);
    }

    [Fact]
    public async Task DeleteAsync_KeepsTheConversationForThePersonWhoStillHasAnAccount()
    {
        var started = (await _f.StartAsync())!;
        await _f.SendAsync(_f.Seller.Id, started.ConversationId, "Da, este disponibil.");

        await DeleteAsync(_f.Seller.Id);

        var thread = (await _f.ThreadAsync(_f.Visitor.Id, started.ConversationId))!;
        Assert.Equal(2, thread.Messages.Count);
        Assert.True(thread.Conversation.OtherParticipant.IsDeleted);
        Assert.Equal(string.Empty, thread.Conversation.OtherParticipant.DisplayName);
        Assert.Null(thread.Listing);

        var inbox = await _f.InboxAsync(_f.Visitor.Id);
        Assert.True(Assert.Single(inbox).OtherParticipant.IsDeleted);
    }

    [Fact]
    public async Task AfterDeletion_TheOtherPersonCanNoLongerWriteToTheDeletedAccount()
    {
        var started = (await _f.StartAsync())!;

        await DeleteAsync(_f.Seller.Id);

        var error = await Assert.ThrowsAsync<ForbiddenAccessException>(() => _f.SendAsync(_f.Visitor.Id, started.ConversationId));
        Assert.Equal(ErrorCodes.RecipientDeleted, error.Code);
    }

    [Fact]
    public async Task DeleteAsync_WhenBothPeopleAreGone_RemovesTheConversationAndItsImages()
    {
        var image = _f.UploadedImage(_f.Visitor.Id);
        var started = (await _f.StartAsync(attachments: [image]))!;
        _f.Db.ConversationReports.Add(ConversationReport.Create(
            (await _f.Db.Conversations.FindAsync(started.ConversationId))!, _f.Visitor.Id, ReportReason.Spam, null, _f.Clock.Now));
        await _f.Db.SaveChangesAsync();

        await DeleteAsync(_f.Seller.Id);
        Assert.True(await _f.Db.Conversations.AnyAsync());
        Assert.DoesNotContain(image, _f.Blobs.DeletedMessageAttachmentNames);

        await DeleteAsync(_f.Visitor.Id);

        Assert.False(await _f.Db.Conversations.AnyAsync());
        Assert.False(await _f.Db.Messages.AnyAsync());
        Assert.False(await _f.Db.ConversationReports.AnyAsync());
        Assert.Contains(image, _f.Blobs.DeletedMessageAttachmentNames);
    }

    [Fact]
    public async Task DeleteAsync_DeletesImagesThatWereUploadedButNeverSent_AndKeepsSentOnes()
    {
        var sent = _f.UploadedImage(_f.Visitor.Id);
        var neverSent = _f.UploadedImage(_f.Visitor.Id);
        await _f.StartAsync(attachments: [sent]);

        await DeleteAsync(_f.Visitor.Id);

        Assert.Contains(neverSent, _f.Blobs.DeletedMessageAttachmentNames);
        Assert.DoesNotContain(sent, _f.Blobs.DeletedMessageAttachmentNames);
    }

    [Fact]
    public async Task DeleteAsync_EmailsAConfirmedAddressThatTheAccountIsGone()
    {
        _f.Seller.EmailConfirmed = true;
        await _f.Db.SaveChangesAsync();

        await DeleteAsync(_f.Seller.Id);

        var email = Assert.Single(_f.Email.Sent);
        Assert.Equal(_f.Seller.Email, email.To);
        Assert.Equal("Contul tău IMOVA a fost șters", email.Subject);
    }

    [Fact]
    public async Task DeleteAsync_DoesNotEmailAnUnconfirmedAddress()
    {
        await DeleteAsync(_f.Seller.Id);

        Assert.Empty(_f.Email.Sent);
    }

    [Fact]
    public async Task DeleteAsync_StillSucceeds_WhenFilesOrTheEmailCannotBeHandled()
    {
        var photo = AddPhoto(_f.Listing.Id, _f.Seller.Id);
        _f.Seller.EmailConfirmed = true;
        await _f.Db.SaveChangesAsync();
        _f.Blobs.FailingDeletes.Add(photo.BlobName);
        _f.Email.Fail = true;

        Assert.True(await DeleteAsync(_f.Seller.Id));
        Assert.False(await _f.Db.Users.AnyAsync(u => u.Id == _f.Seller.Id));
    }

    [Fact]
    public async Task DeleteAsync_ForAnAccountThatDoesNotExist_ReturnsFalse()
    {
        Assert.False(await DeleteAsync(Guid.NewGuid()));
    }
}
