using FluentValidation;
using Imova.Application.Common.Exceptions;
using Imova.Application.Features.Listings.ApproveListing;
using Imova.Application.Features.Listings.ArchiveListing;
using Imova.Application.Features.Listings.MarkListingAsRented;
using Imova.Application.Features.Listings.MarkListingAsSold;
using Imova.Application.Features.Listings.PublishListing;
using Imova.Application.Features.Listings.ReinstateListing;
using Imova.Application.Features.Listings.RejectListing;
using Imova.Application.Features.Listings.SubmitListingForReview;
using Imova.Application.Features.Listings.SuspendListing;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Imova.UnitTests.Listings;

// The lifecycle handlers all share one shape (load -> authorize -> domain transition -> save), so
// they're covered together: each transition's happy path, plus the authorization and
// illegal-transition behavior they have in common.
public class ListingTransitionHandlerTests
{
    private readonly ImovaDbContext _dbContext = TestDbContextFactory.Create();
    private readonly FakeBlobStorageService _blobs = new();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _publisherId;

    public ListingTransitionHandlerTests()
    {
        _publisherId = ListingTestData.AddIndividualPublisher(_dbContext, _ownerId).Id;
    }

    private async Task<Listing> SeedAsync(ListingStatus status, TransactionType transactionType = TransactionType.Rent)
    {
        var listing = ListingTestData.AddListing(_dbContext, _publisherId, transactionType).MoveTo(status);
        await _dbContext.SaveChangesAsync(CancellationToken.None);
        return listing;
    }

    private async Task<ListingStatus> StoredStatusAsync(Guid id) =>
        (await _dbContext.Listings.AsNoTracking().SingleAsync(l => l.Id == id)).Status;

    // --- Owner actions ---

    [Fact]
    public async Task SubmitForReview_ByOwner_MovesDraftToPendingReview()
    {
        var listing = await SeedAsync(ListingStatus.Draft);

        var dto = await new SubmitListingForReviewHandler(_dbContext, _blobs)
            .Handle(new SubmitListingForReviewCommand(listing.Id, _ownerId, false), CancellationToken.None);

        Assert.Equal("PendingReview", dto!.Status);
        Assert.Equal(ListingStatus.PendingReview, await StoredStatusAsync(listing.Id));
    }

    [Fact]
    public async Task Archive_ByOwner_ArchivesActiveListing()
    {
        var listing = await SeedAsync(ListingStatus.Active);

        var dto = await new ArchiveListingHandler(_dbContext, _blobs)
            .Handle(new ArchiveListingCommand(listing.Id, _ownerId, false), CancellationToken.None);

        Assert.Equal("Archived", dto!.Status);
    }

    [Fact]
    public async Task Publish_ByOwner_ReactivatesArchivedListing()
    {
        var listing = await SeedAsync(ListingStatus.Archived);

        var dto = await new PublishListingHandler(_dbContext, _blobs)
            .Handle(new PublishListingCommand(listing.Id, _ownerId, false), CancellationToken.None);

        Assert.Equal("Active", dto!.Status);
    }

    [Fact]
    public async Task MarkAsRented_ByOwner_OnActiveRental()
    {
        var listing = await SeedAsync(ListingStatus.Active);

        var dto = await new MarkListingAsRentedHandler(_dbContext, _blobs)
            .Handle(new MarkListingAsRentedCommand(listing.Id, _ownerId, false), CancellationToken.None);

        Assert.Equal("Rented", dto!.Status);
    }

    [Fact]
    public async Task MarkAsSold_ByOwner_OnActiveSale()
    {
        var listing = await SeedAsync(ListingStatus.Active, TransactionType.Sale);

        var dto = await new MarkListingAsSoldHandler(_dbContext, _blobs)
            .Handle(new MarkListingAsSoldCommand(listing.Id, _ownerId, false), CancellationToken.None);

        Assert.Equal("Sold", dto!.Status);
    }

    [Fact]
    public async Task MarkAsSold_OnRentalListing_IsAValidationErrorNotACrash()
    {
        var listing = await SeedAsync(ListingStatus.Active, TransactionType.Rent);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => new MarkListingAsSoldHandler(_dbContext, _blobs)
            .Handle(new MarkListingAsSoldCommand(listing.Id, _ownerId, false), CancellationToken.None));

        Assert.Contains(ex.Errors, e => e.ErrorMessage == "Only a for-sale listing can be marked as sold.");
        Assert.Equal(ListingStatus.Active, await StoredStatusAsync(listing.Id));
    }

    [Fact]
    public async Task Archive_OfNeverLiveListing_IsAValidationError()
    {
        var listing = await SeedAsync(ListingStatus.PendingReview);

        await Assert.ThrowsAsync<ValidationException>(() => new ArchiveListingHandler(_dbContext, _blobs)
            .Handle(new ArchiveListingCommand(listing.Id, _ownerId, false), CancellationToken.None));
    }

    [Fact]
    public async Task OwnerAction_ByNonOwner_IsForbidden()
    {
        var listing = await SeedAsync(ListingStatus.Active);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => new ArchiveListingHandler(_dbContext, _blobs)
            .Handle(new ArchiveListingCommand(listing.Id, Guid.NewGuid(), false), CancellationToken.None));

        Assert.Equal(ListingStatus.Active, await StoredStatusAsync(listing.Id));
    }

    [Fact]
    public async Task OwnerAction_ByAdminNonOwner_IsAllowed()
    {
        var listing = await SeedAsync(ListingStatus.Active);

        var dto = await new ArchiveListingHandler(_dbContext, _blobs)
            .Handle(new ArchiveListingCommand(listing.Id, Guid.NewGuid(), true), CancellationToken.None);

        Assert.Equal("Archived", dto!.Status);
    }

    [Fact]
    public async Task OwnerAction_ForUnknownListing_ReturnsNull()
    {
        Assert.Null(await new PublishListingHandler(_dbContext, _blobs)
            .Handle(new PublishListingCommand(Guid.NewGuid(), _ownerId, false), CancellationToken.None));
    }

    // --- Admin actions ---

    [Fact]
    public async Task Approve_ByAdmin_ActivatesPendingListing()
    {
        var listing = await SeedAsync(ListingStatus.PendingReview);

        var dto = await new ApproveListingHandler(_dbContext, _blobs)
            .Handle(new ApproveListingCommand(listing.Id, true), CancellationToken.None);

        Assert.Equal("Active", dto!.Status);
        Assert.NotNull(dto.PublishedAt);
    }

    [Fact]
    public async Task Reject_ByAdmin_StoresReason()
    {
        var listing = await SeedAsync(ListingStatus.PendingReview);

        var dto = await new RejectListingHandler(_dbContext, _blobs)
            .Handle(new RejectListingCommand(listing.Id, true, "Poze neclare."), CancellationToken.None);

        Assert.Equal("Rejected", dto!.Status);
        Assert.Equal("Poze neclare.", dto.RejectionReason);
    }

    [Fact]
    public async Task SuspendThenReinstate_ByAdmin()
    {
        var listing = await SeedAsync(ListingStatus.Active);

        var suspended = await new SuspendListingHandler(_dbContext, _blobs)
            .Handle(new SuspendListingCommand(listing.Id, true, "Duplicat."), CancellationToken.None);
        var reinstated = await new ReinstateListingHandler(_dbContext, _blobs)
            .Handle(new ReinstateListingCommand(listing.Id, true), CancellationToken.None);

        Assert.Equal("Suspended", suspended!.Status);
        Assert.Equal("Duplicat.", suspended.SuspensionReason);
        Assert.Equal("Active", reinstated!.Status);
    }

    [Fact]
    public async Task AdminAction_ByTheOwnerWhoIsNotAnAdmin_IsForbidden()
    {
        var listing = await SeedAsync(ListingStatus.PendingReview);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => new ApproveListingHandler(_dbContext, _blobs)
            .Handle(new ApproveListingCommand(listing.Id, false), CancellationToken.None));
    }

    [Fact]
    public async Task Approve_WhenNotPendingReview_IsAValidationError()
    {
        var listing = await SeedAsync(ListingStatus.Draft);

        await Assert.ThrowsAsync<ValidationException>(() => new ApproveListingHandler(_dbContext, _blobs)
            .Handle(new ApproveListingCommand(listing.Id, true), CancellationToken.None));
    }

    [Fact]
    public async Task RejectValidator_RequiresAReason()
    {
        var result = await new RejectListingValidator().ValidateAsync(new RejectListingCommand(Guid.NewGuid(), true, ""));

        Assert.Contains(result.Errors, e => e.PropertyName == "Reason");
    }
}
