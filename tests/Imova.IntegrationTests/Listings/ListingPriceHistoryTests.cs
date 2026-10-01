using System.Net;
using System.Net.Http.Json;
using Imova.Contracts.Listings;
using Imova.Domain.Listings;
using Imova.Infrastructure;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Imova.IntegrationTests.Listings;

// Edits through the API record a price change (see ListingPriceChange) in the real database, and
// deleting the listing takes its history with it.
public class ListingPriceHistoryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ListingPriceHistoryTests(WebApplicationFactory<Program> factory)
    {
        _factory = ListingApi.Configure(factory);
    }

    private async Task<List<ListingPriceChange>> HistoryAsync(Guid listingId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ImovaDbContext>();
        return await db.ListingPriceChanges.AsNoTracking().Where(c => c.ListingId == listingId).OrderBy(c => c.ChangedAt).ToListAsync();
    }

    [Fact]
    public async Task EditingThePrice_RecordsIt_OtherEditsDoNot_AndDeletingTheListingRemovesTheHistory()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var body = await ListingApi.ValidBodyAsync(owner);
        var created = await owner.PostAsJsonAsync("/api/v1/listings", body);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var listing = (await created.Content.ReadFromJsonAsync<ListingDto>())!;
        Assert.Empty(await HistoryAsync(listing.Id));

        body["title"] = "Apartament 2 camere, renovat";
        body["isNegotiable"] = true;
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync($"/api/v1/listings/{listing.Id}", body)).StatusCode);
        Assert.Empty(await HistoryAsync(listing.Id));

        body["price"] = 11_000;
        body["currency"] = "MDL";
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync($"/api/v1/listings/{listing.Id}", body)).StatusCode);

        var change = Assert.Single(await HistoryAsync(listing.Id));
        Assert.Equal((550m, Currency.EUR, 550m), (change.OldAmount, change.OldCurrency, change.OldPriceEur));
        Assert.Equal((11_000m, Currency.MDL), (change.NewAmount, change.NewCurrency));
        Assert.True(change.NewPriceEur is > 0 and < 11_000m);

        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/v1/listings/{listing.Id}")).StatusCode);
        Assert.Empty(await HistoryAsync(listing.Id));
    }

    [Fact]
    public async Task APublishedListingWhosePriceDrops_ShowsPretRedusEverywhere_AndItsHistoryOnTheDetailView()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var admin = await ListingApi.RegisterAdminAsync(_factory);
        var anonymous = _factory.CreateClient();
        var body = await ListingApi.ValidBodyAsync(owner);
        body["price"] = 600;
        var listing = (await (await owner.PostAsJsonAsync("/api/v1/listings", body)).Content.ReadFromJsonAsync<ListingDto>())!;
        (await admin.PostAsync($"/api/v1/listings/{listing.Id}/approve", null)).EnsureSuccessStatusCode();

        body["price"] = 540;
        var saved = await owner.PutAsJsonAsync($"/api/v1/listings/{listing.Id}", body);
        var savedDto = (await saved.Content.ReadFromJsonAsync<ListingDto>())!;

        // The save's own answer already tells the owner what visitors will see.
        Assert.Equal((600m, 10), (savedDto.PriceReduction!.PreviousAmount, savedDto.PriceReduction.Percent));

        var detail = (await anonymous.GetFromJsonAsync<ListingDto>($"/api/v1/listings/{listing.Id}"))!;
        Assert.Equal(10, detail.PriceReduction!.Percent);
        var change = Assert.Single(detail.PriceHistory!.Changes);
        Assert.Equal((600m, 540m, -10), (change.OldAmount, change.NewAmount, change.ChangePercent));
        Assert.True(detail.PriceHistory.StartsAtPublication);

        var mine = (await owner.GetFromJsonAsync<List<ListingDto>>("/api/v1/users/me/listings"))!;
        var card = mine.Single(l => l.Id == listing.Id);
        Assert.Equal(10, card.PriceReduction!.Percent);
        Assert.Null(card.PriceHistory);
    }
}
