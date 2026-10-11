using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ImageMagick;
using Imova.Application.Features.Listings;
using Imova.Contracts.Listings;
using Imova.Contracts.Media;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Imova.IntegrationTests.Media;

// Photo upload endpoints (sign-in + ownership, see MediaAccess) and the API's CORS policy.
public class MediaEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    // A real image: confirm makes its display sizes, so a bare PNG signature isn't enough anymore.
    private static readonly byte[] Png = new MagickImage(MagickColors.SteelBlue, 40, 30).ToByteArray(MagickFormat.Png);

    private readonly WebApplicationFactory<Program> _factory;

    public MediaEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = ListingApi.Configure(factory);
    }

    private static Task<HttpResponseMessage> RequestUploadUrlAsync(HttpClient client, Guid listingId) =>
        client.PostAsJsonAsync($"/api/v1/listings/{listingId}/media/upload-url", new { fileExtension = ".png" });

    // upload-url → PUT to storage (Azurite) → confirm, as the web app does.
    private static async Task<HttpResponseMessage> UploadPhotoAsync(HttpClient client, Guid listingId, byte[]? content = null)
    {
        var urlResponse = await RequestUploadUrlAsync(client, listingId);
        urlResponse.EnsureSuccessStatusCode();
        var upload = (await urlResponse.Content.ReadFromJsonAsync<UploadUrlDto>())!;

        using var storage = new HttpClient();
        var put = new HttpRequestMessage(HttpMethod.Put, upload.UploadUrl) { Content = new ByteArrayContent(content ?? Png) };
        put.Headers.Add("x-ms-blob-type", "BlockBlob");
        put.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        (await storage.SendAsync(put)).EnsureSuccessStatusCode();

        return await client.PostAsJsonAsync($"/api/v1/listings/{listingId}/media/confirm", new { blobName = upload.BlobName });
    }

    // With its own id: topped up to the photos a listing needs (ListingPhotoRules) besides the ones
    // the test uploaded there.
    private static async Task<ListingDto> CreateListingAsync(HttpClient owner, Guid? id = null, int uploadedThere = 0)
    {
        var body = await ListingApi.ValidBodyAsync(owner);
        if (id is { } own)
        {
            body["id"] = own;
            await ListingApi.AddPhotosAsync(owner, own, Math.Max(0, ListingPhotoRules.MinPhotos - uploadedThere));
        }

        var response = await owner.PostAsJsonAsync("/api/v1/listings", body);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<ListingDto>())!;
    }

    [Fact]
    public async Task UploadEndpoints_RequireSignIn()
    {
        var anonymous = _factory.CreateClient();
        var listingId = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.Unauthorized, (await RequestUploadUrlAsync(anonymous, listingId)).StatusCode);
        var confirm = await anonymous.PostAsJsonAsync(
            $"/api/v1/listings/{listingId}/media/confirm", new { blobName = $"{listingId}/x.png" });
        Assert.Equal(HttpStatusCode.Unauthorized, confirm.StatusCode);
    }

    [Fact]
    public async Task AListing_NeedsThreePhotosToBeCreated_AndKeepsThemWhileInReviewOrLive()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var body = await ListingApi.ValidBodyAsync(owner);
        var shortId = Guid.NewGuid();
        await ListingApi.AddPhotosAsync(owner, shortId, count: 2);
        body["id"] = shortId;

        var refused = await owner.PostAsJsonAsync("/api/v1/listings", body);
        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Contains("listing.notEnoughPhotos", await refused.Content.ReadAsStringAsync());

        var listing = await CreateListingAsync(owner);
        Assert.Equal("PendingReview", listing.Status);
        var removal = await owner.DeleteAsync($"/api/v1/listings/{listing.Id}/media/{listing.Photos[0].Id}");
        Assert.Equal(HttpStatusCode.BadRequest, removal.StatusCode);
        Assert.Contains("listing.lastPhotos", await removal.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Owner_CanAddPhotosToTheirListing_SomeoneElseCannot()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var (stranger, _) = await ListingApi.RegisterAsync(_factory);
        var listing = await CreateListingAsync(owner);

        Assert.Equal(HttpStatusCode.OK, (await UploadPhotoAsync(owner, listing.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await RequestUploadUrlAsync(stranger, listing.Id)).StatusCode);

        var stored = (await owner.GetFromJsonAsync<ListingDto>($"/api/v1/listings/{listing.Id}"))!;
        Assert.Equal(ListingPhotoRules.MinPhotos + 1, stored.Photos.Count);
    }

    [Fact]
    public async Task NewListing_PhotosUploadedBeforeCreation_BelongToTheirUploader()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var (stranger, _) = await ListingApi.RegisterAsync(_factory);
        var pendingId = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.OK, (await UploadPhotoAsync(owner, pendingId)).StatusCode);

        // Someone else can neither add to that id nor create a listing with it.
        Assert.Equal(HttpStatusCode.Forbidden, (await RequestUploadUrlAsync(stranger, pendingId)).StatusCode);
        var body = await ListingApi.ValidBodyAsync(stranger);
        body["id"] = pendingId;
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.PostAsJsonAsync("/api/v1/listings", body)).StatusCode);

        var listing = await CreateListingAsync(owner, pendingId, uploadedThere: 1);
        Assert.Equal(ListingPhotoRules.MinPhotos, listing.Photos.Count);
    }

    [Fact]
    public async Task Upload_MakesSmallJpegSizes_ThatBrowsersMayCacheForAYear()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var photo = new MagickImage(MagickColors.OrangeRed, 2400, 1800).ToByteArray(MagickFormat.Jpeg);

        var response = await UploadPhotoAsync(owner, Guid.NewGuid(), photo);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = (await response.Content.ReadFromJsonAsync<PhotoDto>())!;

        using var storage = new HttpClient();
        foreach (var (url, width, height) in new[] { (dto.ThumbnailUrl, 400u, 300u), (dto.CardUrl, 800u, 600u), (dto.Url, 1600u, 1200u) })
        {
            var file = await storage.GetAsync(url);
            file.EnsureSuccessStatusCode();
            Assert.Equal("image/jpeg", file.Content.Headers.ContentType?.MediaType);
            Assert.Contains("immutable", file.Headers.CacheControl?.ToString());
            using var image = new MagickImage(await file.Content.ReadAsByteArrayAsync());
            Assert.Equal((width, height), (image.Width, image.Height));
        }
    }

    [Fact]
    public async Task Upload_OfAFileThatOnlyStartsLikeAnImage_IsRefused()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        byte[] fake = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 0x49, 0x48, 0x44, 0x52];

        var response = await UploadPhotoAsync(owner, Guid.NewGuid(), fake);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("upload.notAnImage", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Cors_AllowsOnlyTheConfiguredOrigins()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("Cors:AllowedOrigins:0", "https://imova.example"));
        var client = factory.CreateClient();

        Assert.Equal("https://imova.example", await AllowedOriginAsync(client, "https://imova.example"));
        Assert.Null(await AllowedOriginAsync(client, "http://localhost:3000"));
        Assert.Null(await AllowedOriginAsync(client, "https://evil.example"));

        // Without the setting, local development's web app origin.
        var defaults = _factory.CreateClient();
        Assert.Equal("http://localhost:3000", await AllowedOriginAsync(defaults, "http://localhost:3000"));
    }

    // The Access-Control-Allow-Origin a browser preflight from `origin` gets back, if any.
    private static async Task<string?> AllowedOriginAsync(HttpClient client, string origin)
    {
        var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/v1/locations/raioane");
        preflight.Headers.Add("Origin", origin);
        preflight.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(preflight);
        return response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values) ? values.Single() : null;
    }
}
