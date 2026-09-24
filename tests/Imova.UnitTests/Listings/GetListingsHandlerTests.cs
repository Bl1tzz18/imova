using Imova.Application.Features.Listings.GetListings;
using Imova.Domain.Favorites;
using Imova.Domain.Listings;
using Imova.Domain.Properties;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Listings;

public class GetListingsHandlerTests
{
    private readonly ImovaDbContext _dbContext = TestDbContextFactory.Create();
    private readonly Guid _publisherId;

    public GetListingsHandlerTests()
    {
        _publisherId = ListingTestData.AddIndividualPublisher(_dbContext).Id;
    }

    private Listing AddActive(
        TransactionType transactionType = TransactionType.Rent,
        Price? price = null,
        PropertyType propertyType = PropertyType.Apartment) =>
        ListingTestData.AddListing(_dbContext, _publisherId, transactionType, price, propertyType: propertyType)
            .MoveTo(ListingStatus.Active);

    private async Task<List<Guid>> IdsAsync(GetListingsQuery query)
    {
        await _dbContext.SaveChangesAsync(CancellationToken.None);
        var result = await new GetListingsHandler(_dbContext, new FakeBlobStorageService()).Handle(query, CancellationToken.None);
        return result.Select(l => l.Id).ToList();
    }

    [Fact]
    public async Task Handle_ReturnsOnlyActiveListings()
    {
        var active = AddActive();
        ListingTestData.AddListing(_dbContext, _publisherId).MoveTo(ListingStatus.PendingReview);
        ListingTestData.AddListing(_dbContext, _publisherId).MoveTo(ListingStatus.Archived);
        ListingTestData.AddListing(_dbContext, _publisherId).MoveTo(ListingStatus.Suspended);

        Assert.Equal([active.Id], await IdsAsync(new GetListingsQuery()));
    }

    [Fact]
    public async Task Handle_FiltersByTheUnderlyingPropertysType()
    {
        AddActive(propertyType: PropertyType.Apartment);
        var garage = AddActive(propertyType: PropertyType.Garage);

        Assert.Equal([garage.Id], await IdsAsync(new GetListingsQuery(PropertyType: PropertyType.Garage)));
    }

    [Fact]
    public async Task Handle_FiltersByTransactionType()
    {
        AddActive(TransactionType.Rent);
        var sale = AddActive(TransactionType.Sale);

        Assert.Equal([sale.Id], await IdsAsync(new GetListingsQuery(TransactionType: TransactionType.Sale)));
    }

    [Fact]
    public async Task Handle_FiltersPriceByPriceEurRegardlessOfCurrency()
    {
        // 10,000 MDL ~ 500 EUR, 1,000 EUR, 400 USD ~ 360 EUR (FakeExchangeRateProvider-style rates).
        var mdl = AddActive(price: Price.Create(10_000m, Currency.MDL, false, 0.05m));
        AddActive(price: ListingTestData.Eur(1_000m));
        AddActive(price: Price.Create(400m, Currency.USD, false, 0.9m));

        Assert.Equal([mdl.Id], await IdsAsync(new GetListingsQuery(MinPriceEur: 400m, MaxPriceEur: 600m)));
    }

    [Fact]
    public async Task Handle_MarksWhichListingsTheCurrentUserSaved()
    {
        var saved = AddActive();
        var notSaved = AddActive();
        var userId = Guid.NewGuid();
        _dbContext.Favorites.Add(Favorite.Create(userId, saved.Id));
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await new GetListingsHandler(_dbContext, new FakeBlobStorageService())
            .Handle(new GetListingsQuery(CurrentUserId: userId), CancellationToken.None);

        Assert.True(result.Single(l => l.Id == saved.Id).IsSaved);
        Assert.False(result.Single(l => l.Id == notSaved.Id).IsSaved);
    }

    [Fact]
    public async Task Handle_IncludesPropertyLocationAndPublisherButNotContactDetails()
    {
        AddActive();
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var dto = Assert.Single(await new GetListingsHandler(_dbContext, new FakeBlobStorageService())
            .Handle(new GetListingsQuery(), CancellationToken.None));

        Assert.Equal("Apartment", dto.Property.PropertyType);
        Assert.Equal("Chișinău", dto.Property.Location!.RaionName);
        Assert.Equal("Ion Popescu", dto.Publisher.DisplayName);
        Assert.Null(dto.Publisher.Phone);
        Assert.Null(dto.Publisher.Email);
    }

    [Fact]
    public async Task Handle_WithNoListings_ReturnsEmptyList()
    {
        Assert.Empty(await IdsAsync(new GetListingsQuery()));
    }
}
