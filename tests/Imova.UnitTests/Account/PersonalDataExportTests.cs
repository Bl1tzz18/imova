using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Account.ExportPersonalData;
using Imova.Application.Features.Account.GetAccountDataSummary;
using Imova.Contracts.Account;
using Imova.Domain.Favorites;
using Imova.Domain.Listings;
using Imova.Domain.Messaging;
using Imova.Domain.SavedSearches;
using Imova.UnitTests.TestSupport;
using Microsoft.AspNetCore.Identity;

namespace Imova.UnitTests.Account;

// The download of everything stored about an account: what the data holds, and the ZIP around it.
// Exports are made for the seller (Ion) of MessagingFixture.
public class PersonalDataExportTests
{
    private readonly MessagingFixture _f = new();
    private readonly FakeUserStore _store = new();
    private readonly UserManager<ApplicationUser> _userManager;

    public PersonalDataExportTests()
    {
        _userManager = TestUserManagerFactory.Create(_store);
        // The handlers find the account through Identity, the rest through the database.
        _store.CreateAsync(_f.Seller, CancellationToken.None).GetAwaiter().GetResult();
        _store.CreateAsync(_f.Visitor, CancellationToken.None).GetAwaiter().GetResult();
    }

    private Task<PersonalDataExport> ExportAsync(Guid userId) =>
        new ExportPersonalDataHandler(_f.Db, _userManager, _f.Blobs, _f.App, _f.Clock)
            .Handle(new ExportPersonalDataQuery(userId), CancellationToken.None);

    private Photo AddPhoto(Guid listingId, int sortOrder = 0)
    {
        var photo = Photo.Create(listingId, $"{listingId}/{Guid.NewGuid()}.jpg", "image/jpeg", 1000, sortOrder, uploadedByUserId: _f.Seller.Id);
        _f.Db.Photos.Add(photo);
        _f.Blobs.StoredBlobNames.Add(photo.BlobName);
        return photo;
    }

    private void AddRefreshToken(Guid sessionId, DateTimeOffset createdAt, DateTimeOffset? revokedAt = null) =>
        _f.Db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = _f.Seller.Id,
            SessionId = sessionId,
            TokenHash = "secret-hash-" + Guid.NewGuid().ToString("N"),
            StampFingerprint = "stamp",
            Persistent = true,
            CreatedAt = createdAt,
            ExpiresAt = createdAt.AddDays(30),
            SessionExpiresAt = _f.Clock.Now.AddDays(90),
            RevokedAt = revokedAt,
        });

    [Fact]
    public async Task Export_HoldsTheAccountAndItsListingsWithTheirPhotos()
    {
        _f.Seller.PhoneNumber = "+373 69 123 456";
        _f.Seller.ProfilePictureUrl = $"https://blob.test/profile-pictures/{_f.Seller.Id}/me.png";
        var second = AddPhoto(_f.Listing.Id, sortOrder: 1);
        var first = AddPhoto(_f.Listing.Id, sortOrder: 0);
        await _f.Db.SaveChangesAsync();

        var export = await ExportAsync(_f.Seller.Id);

        var data = export.Data;
        Assert.Equal(ExportPersonalDataHandler.Format, data.Format);
        Assert.Equal(_f.Clock.Now, data.GeneratedAt);
        Assert.Equal(_f.Seller.Email, data.Account.Email);
        Assert.Equal("Ion Popescu", data.Account.DisplayName);
        Assert.Equal("+373 69 123 456", data.Account.PhoneNumber);
        Assert.Equal("profile/picture.png", data.Account.ProfilePictureFile);
        Assert.False(data.Account.HasPassword);

        Assert.Equal("Individual", Assert.Single(data.Publishers).Type);
        var listing = Assert.Single(data.Listings);
        Assert.Equal(_f.Listing.Id, listing.Listing.Id);
        Assert.Equal(
            [$"listings/{_f.Listing.Id}/photo-01.jpg", $"listings/{_f.Listing.Id}/photo-02.jpg"],
            listing.PhotoFiles);
        Assert.Contains(export.Files, f => f.Path == $"listings/{_f.Listing.Id}/photo-01.jpg" && f.BlobName == first.BlobName);
        Assert.Contains(export.Files, f => f.Path == $"listings/{_f.Listing.Id}/photo-02.jpg" && f.BlobName == second.BlobName);
        Assert.Contains(export.Files, f => f.Path == "profile/picture.png" && f.Source == ExportFileSource.Public);
        Assert.Empty(listing.PriceHistory);
    }

    [Fact]
    public async Task Export_HoldsEachListingsPriceHistory_OldestFirst()
    {
        var mdl = Price.Create(1_000_000m, Currency.MDL, false, eurRate: 0.05m);
        _f.Db.ListingPriceChanges.Add(ListingPriceChange.Between(_f.Listing.Id, ListingTestData.Eur(52_000m), mdl, _f.Clock.Now)!);
        _f.Db.ListingPriceChanges.Add(ListingPriceChange.Between(_f.Listing.Id, ListingTestData.Eur(55_000m), ListingTestData.Eur(52_000m), _f.Clock.Now.AddDays(-3))!);
        await _f.Db.SaveChangesAsync();

        var history = Assert.Single((await ExportAsync(_f.Seller.Id)).Data.Listings).PriceHistory;

        Assert.Equal(
            [
                new ExportedPriceChangeDto(55_000m, "EUR", 55_000m, 52_000m, "EUR", 52_000m, _f.Clock.Now.AddDays(-3)),
                new ExportedPriceChangeDto(52_000m, "EUR", 52_000m, 1_000_000m, "MDL", 50_000m, _f.Clock.Now),
            ],
            history);
    }

    [Fact]
    public async Task Export_HoldsFavoritesSavedSearchesSessionsAndStrayPhotos()
    {
        var gone = Guid.NewGuid();
        _f.Db.Favorites.Add(Favorite.Create(_f.Seller.Id, _f.Listing.Id));
        _f.Db.Favorites.Add(Favorite.Create(_f.Seller.Id, gone));
        _f.Db.SavedSearches.Add(SavedSearch.Create(_f.Seller.Id, "Chirii Botanica", "transactionType=Rent", AlertFrequency.Daily, _f.Clock.Now));
        var session = Guid.NewGuid();
        AddRefreshToken(session, _f.Clock.Now.AddDays(-2));
        AddRefreshToken(session, _f.Clock.Now.AddDays(-1), revokedAt: _f.Clock.Now);
        var stray = Photo.Create(Guid.NewGuid(), "draft/x.webp", "image/webp", 10, uploadedByUserId: _f.Seller.Id);
        _f.Db.Photos.Add(stray);
        await _f.Db.SaveChangesAsync();

        var data = (await ExportAsync(_f.Seller.Id)).Data;

        Assert.Equal(2, data.Favorites.Count);
        Assert.Contains(data.Favorites, f => f.ListingId == _f.Listing.Id && f.ListingTitle == _f.Listing.Title);
        Assert.Contains(data.Favorites, f => f.ListingId == gone && f.ListingTitle is null);

        var search = Assert.Single(data.SavedSearches);
        Assert.Equal("http://localhost:3000/search?transactionType=Rent", search.SearchUrl);
        Assert.Equal("Daily", search.AlertFrequency);

        var exportedSession = Assert.Single(data.Sessions);
        Assert.Equal(_f.Clock.Now.AddDays(-2), exportedSession.StartedAt);
        Assert.Equal(_f.Clock.Now.AddDays(-1), exportedSession.LastActiveAt);
        Assert.Equal(_f.Clock.Now, exportedSession.EndedAt);
        Assert.True(exportedSession.RememberMe);

        var photo = Assert.Single(data.PhotosNotInAListing);
        Assert.Equal($"photos-not-in-a-listing/{stray.Id}.webp", photo.File);
    }

    [Fact]
    public async Task Export_HoldsBothSidesOfEachConversation_WithTheirImages()
    {
        var image = _f.UploadedImage(_f.Visitor.Id);
        var started = (await _f.StartAsync(attachments: [image]))!;
        _f.Clock.Now = _f.Clock.Now.AddMinutes(5);
        await _f.SendAsync(_f.Seller.Id, started.ConversationId, "Da, este disponibil.");
        _f.Db.UserBlocks.Add(new UserBlock(_f.Seller.Id, _f.Visitor.Id, _f.Clock.Now));
        var conversation = (await _f.Db.Conversations.FindAsync(started.ConversationId))!;
        _f.Db.ConversationReports.Add(ConversationReport.Create(conversation, _f.Seller.Id, ReportReason.Other, "Cere avans.", _f.Clock.Now));
        await _f.Db.SaveChangesAsync();

        var export = await ExportAsync(_f.Seller.Id);

        var exported = Assert.Single(export.Data.Conversations);
        Assert.Equal("Publisher", exported.YourRole);
        Assert.Equal("Maria Rusu", exported.OtherParticipant);
        Assert.Equal(_f.Listing.Title, exported.ListingTitle);
        Assert.Collection(
            exported.Messages,
            received =>
            {
                Assert.False(received.SentByYou);
                Assert.Equal("Bună ziua!", received.Body);
                var file = Assert.Single(received.AttachmentFiles);
                Assert.StartsWith($"messages/{started.ConversationId}/", file);
                Assert.Contains(export.Files, f => f.Path == file && f.Source == ExportFileSource.MessageAttachment && f.BlobName == image);
            },
            sent =>
            {
                Assert.True(sent.SentByYou);
                Assert.Equal("Da, este disponibil.", sent.Body);
            });

        Assert.Equal("Maria Rusu", Assert.Single(export.Data.BlockedUsers).BlockedUser);
        var report = Assert.Single(export.Data.ReportsYouFiled);
        Assert.Equal("Other", report.Reason);
        Assert.Equal("Cere avans.", report.Details);
    }

    [Fact]
    public async Task Export_IncludesTheListingsYouReported_WithTheOutcome()
    {
        var report = ListingReport.Create(_f.Listing, _f.Visitor.Id, ListingReportReason.Fraud, "Cere avans.", _f.Clock.Now);
        report.Resolve(ListingReportOutcome.Dismissed, Guid.NewGuid(), "Only for the admins.", _f.Clock.Now);
        _f.Db.ListingReports.Add(report);
        await _f.Db.SaveChangesAsync();

        var exported = Assert.Single((await ExportAsync(_f.Visitor.Id)).Data.ListingReportsYouFiled);

        Assert.Equal(_f.Listing.Id, exported.ListingId);
        Assert.Equal("Fraud", exported.Reason);
        Assert.Equal("Cere avans.", exported.Details);
        Assert.Equal("Dismissed", exported.Outcome);
        Assert.Empty((await ExportAsync(_f.Seller.Id)).Data.ListingReportsYouFiled);
    }

    [Fact]
    public async Task Export_OfAVisitor_ShowsTheirRoleAndOnlyTheirData()
    {
        await _f.StartAsync();

        var data = (await ExportAsync(_f.Visitor.Id)).Data;

        Assert.Equal("Visitor", Assert.Single(data.Conversations).YourRole);
        Assert.Empty(data.Listings);
        Assert.Empty(data.Publishers);
    }

    [Fact]
    public async Task Archive_HoldsTheDataTheReadmeAndEveryFile_AndListsMissingOnes()
    {
        var photo = AddPhoto(_f.Listing.Id);
        var vanished = AddPhoto(_f.Listing.Id, sortOrder: 1);
        _f.Blobs.StoredBlobNames.Remove(vanished.BlobName);
        _f.Seller.DisplayName = "Ion Țurcanu";
        await _f.Db.SaveChangesAsync();
        var export = await ExportAsync(_f.Seller.Id);

        using var zipBytes = new MemoryStream();
        await PersonalDataArchive.WriteAsync(export, _f.Blobs, zipBytes, CancellationToken.None);
        zipBytes.Position = 0;
        using var zip = new ZipArchive(zipBytes, ZipArchiveMode.Read);

        var names = zip.Entries.Select(e => e.FullName).ToList();
        Assert.Contains(PersonalDataArchive.DataFileName, names);
        Assert.Contains(PersonalDataArchive.ReadmeFileName, names);
        var photoPath = $"listings/{_f.Listing.Id}/photo-01.jpg";
        var vanishedPath = $"listings/{_f.Listing.Id}/photo-02.jpg";
        Assert.Contains(photoPath, names);
        Assert.DoesNotContain(vanishedPath, names);
        using (var reader = new StreamReader(zip.GetEntry(photoPath)!.Open()))
        {
            Assert.Equal(photo.BlobName, await reader.ReadToEndAsync());
        }

        string json;
        using (var reader = new StreamReader(zip.GetEntry(PersonalDataArchive.DataFileName)!.Open(), Encoding.UTF8))
        {
            json = await reader.ReadToEndAsync();
        }

        // Readable as-is: camelCase, indented, diacritics not escaped.
        Assert.Contains("\"displayName\": \"Ion Țurcanu\"", json);
        var data = JsonSerializer.Deserialize<PersonalDataExportDto>(json, PersonalDataArchive.JsonOptions)!;
        Assert.Equal(vanishedPath, Assert.Single(data.MissingFiles));
        Assert.Equal(_f.Seller.Id, data.Account.Id);
        Assert.DoesNotContain("secret-hash", json);
    }

    [Fact]
    public void FileName_CarriesTheDate()
    {
        Assert.Equal(
            "imova-date-personale-2026-09-29.zip",
            PersonalDataArchive.FileName(new DateTimeOffset(2026, 9, 29, 23, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public async Task Summary_CountsWhatTheAccountHolds()
    {
        ListingTestData.AddAgencyPublisher(_f.Db, _f.Seller.Id);
        var draft = ListingTestData.AddListing(_f.Db, _f.Listing.PublisherId);
        _f.Db.Favorites.Add(Favorite.Create(_f.Seller.Id, draft.Id));
        await _f.Db.SaveChangesAsync();
        await _f.StartAsync();

        var summary = await new GetAccountDataSummaryHandler(_f.Db, _userManager)
            .Handle(new GetAccountDataSummaryQuery(_f.Seller.Id), CancellationToken.None);

        Assert.Equal(new AccountDataSummaryDto(2, 1, 1, 0, 1, HasAgency: true, HasPassword: false), summary);
    }
}
