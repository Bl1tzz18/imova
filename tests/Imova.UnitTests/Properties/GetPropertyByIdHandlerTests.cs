using Imova.Application.Common.Identity;
using Imova.Application.Features.Properties.GetPropertyById;
using Imova.Domain.Favorites;
using Imova.Domain.Locations;
using Imova.Domain.Properties;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Properties;

public class GetPropertyByIdHandlerTests
{
    // Published by default — a Draft/Archived listing is only visible to its own owner (see the
    // dedicated tests below), so every pre-existing test here that fetches anonymously needs a
    // live listing.
    private static Property AddProperty(ImovaDbContext dbContext, Guid? ownerId = null)
    {
        var property = Property.Create(
            ownerId ?? Guid.NewGuid(), "Titlu", "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR",
            54m, 2m, null, 3, 9);
        property.SubmitForReview();
        property.Approve();
        dbContext.Properties.Add(property);
        return property;
    }

    [Fact]
    public async Task Handle_ForExistingProperty_ReturnsDto()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddProperty(dbContext);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPropertyByIdHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetPropertyByIdQuery(property.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(property.Id, result!.Id);
    }

    [Fact]
    public async Task Handle_ForUnknownId_ReturnsNull()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetPropertyByIdHandler(dbContext, new FakeBlobStorageService());

        var result = await handler.Handle(new GetPropertyByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_IncludesOwnerContactInfo()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var owner = new ApplicationUser { Id = Guid.NewGuid(), Email = "owner@example.com", PhoneNumber = "+373 69 123 456" };
        dbContext.Users.Add(owner);
        var property = AddProperty(dbContext, owner.Id);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPropertyByIdHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetPropertyByIdQuery(property.Id), CancellationToken.None);

        Assert.NotNull(result!.Owner);
        Assert.Equal("owner@example.com", result.Owner!.Email);
        Assert.Equal("+373 69 123 456", result.Owner.Phone);
    }

    [Fact]
    public async Task Handle_WithoutCurrentUserId_IsSavedIsFalse()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddProperty(dbContext);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPropertyByIdHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetPropertyByIdQuery(property.Id), CancellationToken.None);

        Assert.False(result!.IsSaved);
    }

    [Fact]
    public async Task Handle_WithCurrentUserIdWhoSavedIt_IsSavedIsTrue()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddProperty(dbContext);
        var currentUserId = Guid.NewGuid();
        dbContext.Favorites.Add(Favorite.Create(currentUserId, property.Id));
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPropertyByIdHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetPropertyByIdQuery(property.Id, currentUserId), CancellationToken.None);

        Assert.True(result!.IsSaved);
    }

    [Fact]
    public async Task Handle_WithCurrentUserIdWhoDidNotSaveIt_IsSavedIsFalse()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddProperty(dbContext);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPropertyByIdHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetPropertyByIdQuery(property.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result!.IsSaved);
    }

    [Fact]
    public async Task Handle_ForArchivedListing_AnonymousRequest_ReturnsNull()
    {
        // The bug this guards: a deactivated listing must not be directly viewable either, same
        // as it no longer appears on public browsing pages.
        var ownerId = Guid.NewGuid();
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddProperty(dbContext, ownerId);
        property.Archive();
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPropertyByIdHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetPropertyByIdQuery(property.Id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ForArchivedListing_OtherLoggedInUser_ReturnsNull()
    {
        var ownerId = Guid.NewGuid();
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddProperty(dbContext, ownerId);
        property.Archive();
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPropertyByIdHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetPropertyByIdQuery(property.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ForArchivedListing_OwnerRequest_StillReturnsIt()
    {
        // The owner needs to keep seeing their own Draft/Archived listing — this is what the
        // "my listings" edit page relies on.
        var ownerId = Guid.NewGuid();
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddProperty(dbContext, ownerId);
        property.Archive();
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPropertyByIdHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetPropertyByIdQuery(property.Id, ownerId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(property.Id, result!.Id);
    }

    [Fact]
    public async Task Handle_ForDraftListing_AnonymousRequest_ReturnsNull()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var draft = Property.Create(
            Guid.NewGuid(), "Titlu", "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR",
            54m, 2m, null, 3, 9);
        dbContext.Properties.Add(draft);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPropertyByIdHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetPropertyByIdQuery(draft.Id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ForPendingReviewListing_AdminRequest_ReturnsIt()
    {
        // The bug this guards: an admin opening a listing from the moderation queue (which links
        // to /property/{id}, not just /my-listings/{id}/edit) got "not found" for anyone else's
        // PendingReview listing, since only the owner was ever allowed to see a non-Published one.
        await using var dbContext = TestDbContextFactory.Create();
        var pending = Property.Create(
            Guid.NewGuid(), "Titlu", "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR",
            54m, 2m, null, 3, 9);
        pending.SubmitForReview();
        dbContext.Properties.Add(pending);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPropertyByIdHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(
            new GetPropertyByIdQuery(pending.Id, CurrentUserId: Guid.NewGuid(), IsAdmin: true), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(pending.Id, result!.Id);
    }

    [Fact]
    public async Task Handle_ForArchivedListing_AdminRequest_ReturnsIt()
    {
        var ownerId = Guid.NewGuid();
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddProperty(dbContext, ownerId);
        property.Archive();
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPropertyByIdHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(
            new GetPropertyByIdQuery(property.Id, CurrentUserId: Guid.NewGuid(), IsAdmin: true), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(property.Id, result!.Id);
    }

    [Fact]
    public async Task Handle_ForPendingReviewListing_NonAdminOtherUser_StillReturnsNull()
    {
        // IsAdmin doesn't leak into the ordinary non-owner path unless it's actually set.
        await using var dbContext = TestDbContextFactory.Create();
        var pending = Property.Create(
            Guid.NewGuid(), "Titlu", "Descriere", PropertyType.Apartment, ListingType.Rent, 550m, "EUR",
            54m, 2m, null, 3, 9);
        pending.SubmitForReview();
        dbContext.Properties.Add(pending);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPropertyByIdHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(
            new GetPropertyByIdQuery(pending.Id, CurrentUserId: Guid.NewGuid(), IsAdmin: false), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ForPublishedListing_AnyViewer_SeesExactStreetAndCoordinates()
    {
        // No masking: an anonymous visitor sees the same exact address/coordinates as the owner
        // or an admin would — there's no privacy toggle, every viewer gets the real data.
        await using var dbContext = TestDbContextFactory.Create();
        var property = AddProperty(dbContext);
        var location = PropertyLocation.Create(
            property.Id, "Moldova", Guid.NewGuid(), "Chisinau", Guid.NewGuid(), "Botanica", null, null, 47.01055, 28.86383, "Str. Ismail 44");
        dbContext.PropertyLocations.Add(location);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPropertyByIdHandler(dbContext, new FakeBlobStorageService());
        var result = await handler.Handle(new GetPropertyByIdQuery(property.Id), CancellationToken.None);

        Assert.Equal("Str. Ismail 44", result!.Location!.Street);
        Assert.Equal(47.01055, result.Location.Latitude);
        Assert.Equal(28.86383, result.Location.Longitude);
    }
}
