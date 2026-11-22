using Imova.Application.Features.Sitemap;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Sitemap;

public class SitemapHandlerTests
{
    private readonly ImovaDbContext _dbContext = TestDbContextFactory.Create();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _publisherId;

    public SitemapHandlerTests()
    {
        _publisherId = ListingTestData.AddIndividualPublisher(_dbContext, _userId).Id;
    }

    private Task<Imova.Contracts.Sitemap.SitemapListingPageDto> ListingsAsync(int page = 1) =>
        new GetSitemapListingsHandler(_dbContext).Handle(new GetSitemapListingsQuery(page), CancellationToken.None);

    [Fact]
    public async Task Listings_AreThePublicOnes_InNumberOrder()
    {
        var older = ListingTestData.AddListing(_dbContext, _publisherId, photos: 3).MoveTo(ListingStatus.Active);
        ListingTestData.AddListing(_dbContext, _publisherId).MoveTo(ListingStatus.Draft);
        ListingTestData.AddListing(_dbContext, _publisherId, TransactionType.Sale, photos: 3).MoveTo(ListingStatus.Sold);
        var newer = ListingTestData.AddListing(_dbContext, _publisherId, photos: 3).MoveTo(ListingStatus.Active);
        await _dbContext.SaveChangesAsync();

        var page = await ListingsAsync();

        Assert.Equal(2, page.Total);
        Assert.Equal(GetSitemapListingsQuery.PageSize, page.PageSize);
        Assert.Equal(
            new[] { older.Number, newer.Number },
            page.Items.Select(i => i.Number));
        Assert.All(page.Items, i => Assert.False(string.IsNullOrEmpty(i.Title)));
    }

    [Fact]
    public async Task Listings_LeaveOutADeactivatedAgencysListings()
    {
        var agency = ListingTestData.AddAgency(_dbContext, _userId);
        ListingTestData.AddListing(_dbContext, _publisherId, agencyId: agency.Id, photos: 3).MoveTo(ListingStatus.Active);
        agency.Deactivate(DateTimeOffset.UtcNow);
        await _dbContext.SaveChangesAsync();

        Assert.Equal(0, (await ListingsAsync()).Total);
    }

    [Fact]
    public async Task Listings_PastTheLastPage_AreEmpty()
    {
        ListingTestData.AddListing(_dbContext, _publisherId, photos: 3).MoveTo(ListingStatus.Active);
        await _dbContext.SaveChangesAsync();

        var page = await ListingsAsync(page: 2);

        Assert.Equal(1, page.Total);
        Assert.Empty(page.Items);
    }

    [Fact]
    public void Listings_StartAtPageOne()
    {
        Assert.False(new GetSitemapListingsValidator().Validate(new GetSitemapListingsQuery(0)).IsValid);
        Assert.True(new GetSitemapListingsValidator().Validate(new GetSitemapListingsQuery(1)).IsValid);
    }

    [Fact]
    public async Task Agencies_AreTheActiveOnes()
    {
        var active = ListingTestData.AddAgency(_dbContext, _userId, "Casa Ta");
        var gone = ListingTestData.AddAgency(_dbContext, Guid.NewGuid(), "Imobil Vechi");
        gone.Deactivate(DateTimeOffset.UtcNow);
        await _dbContext.SaveChangesAsync();

        var agencies = await new GetSitemapAgenciesHandler(_dbContext).Handle(new GetSitemapAgenciesQuery(), CancellationToken.None);

        Assert.Equal([active.Slug], agencies.Select(a => a.Slug));
    }
}
