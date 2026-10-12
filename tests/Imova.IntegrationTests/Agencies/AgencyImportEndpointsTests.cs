using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using ImageMagick;
using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Agencies.Import;
using Imova.Application.Features.Media.Sizes;
using Imova.Contracts.Agencies;
using Imova.Domain.Listings;
using Imova.Domain.Properties;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imova.IntegrationTests.Agencies;

// The import end to end on the real stack: a site admin uploads a CSV for a verified agency they
// don't belong to (onboarding), the Worker job — run here in-process, with a downloader that hands
// back a real JPEG — creates the listing under the agency's Owner, and the reads show the result.
public class AgencyImportEndpointsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = ListingApi.Configure(factory);

    private sealed class JpegFetcher : IImportImageFetcher
    {
        private static readonly byte[] Jpeg = MakeJpeg();

        public Task<ImportImageDownload> DownloadAsync(string url, CancellationToken cancellationToken) =>
            Task.FromResult(ImportImageDownload.Of(Jpeg));

        private static byte[] MakeJpeg()
        {
            using var image = new MagickImage(MagickColors.SteelBlue, 640, 480);
            return image.ToByteArray(MagickFormat.Jpeg);
        }
    }

    private static MultipartFormDataContent File(string text)
    {
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(text));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        return new MultipartFormDataContent { { file, "file", "anunturi.csv" } };
    }

    private static string Csv(string externalRef)
    {
        var columns = ImportColumns.For(PropertyType.Apartment);
        var values = new Dictionary<string, string>(ImportExamples.For(PropertyType.Apartment)) { [ImportColumns.ExternalRef] = externalRef };
        var text = new StringWriter(CultureInfo.InvariantCulture);
        using (var csv = new CsvWriter(text, new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ";" }))
        {
            foreach (var column in columns)
            {
                csv.WriteField(column.Name);
            }

            csv.NextRecord();
            foreach (var column in columns)
            {
                csv.WriteField(values.GetValueOrDefault(column.Name, string.Empty));
            }

            csv.NextRecord();
        }

        return text.ToString();
    }

    private async Task<int> RunWorkerJobAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<IApplicationDbContext>();
        var blobs = services.GetRequiredService<IBlobStorageService>();
        var job = new ListingImports(
            dbContext,
            blobs,
            new StubGeocodingService(),
            new ImportListingWriter(
                dbContext,
                services.GetRequiredService<IExchangeRateProvider>(),
                new ImportPhotoSync(dbContext, blobs, new JpegFetcher(), services.GetRequiredService<PhotoSizeGenerator>(), NullLogger<ImportPhotoSync>.Instance)),
            TimeProvider.System,
            NullLogger<ListingImports>.Instance);
        return await job.RunAsync(CancellationToken.None);
    }

    private static async Task<string?> CodeOf(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;

    [Fact]
    public async Task ASiteAdmin_ImportsACsvForAVerifiedAgency_TheWorkerCreatesTheListingUnderItsOwner()
    {
        var (owner, ownerUser) = await ListingApi.RegisterAsync(_factory);
        var (stranger, _) = await ListingApi.RegisterAsync(_factory);
        var admin = await ListingApi.RegisterAdminAsync(_factory);
        var agency = (await (await owner.PostAsJsonAsync("/api/v1/agencies", new { name = $"Agenția Import {Guid.NewGuid():N}", phone = "+373 22 555 010" }))
            .Content.ReadFromJsonAsync<AgencyDto>())!;
        var importUrl = $"/api/v1/agencies/{agency.Id}/import";
        var externalRef = $"AP-{Guid.NewGuid():N}"[..20];

        // Not verified yet: no import, with the reason.
        var notVerified = await admin.PostAsync(importUrl, File(Csv(externalRef)));
        Assert.Equal(HttpStatusCode.Forbidden, notVerified.StatusCode);
        Assert.Equal(ErrorCodes.ImportAgencyNotVerified, await CodeOf(notVerified));
        (await admin.PostAsync($"/api/v1/admin/agencies/{agency.Id}/verify", null)).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.PostAsync(importUrl, File(Csv(externalRef)))).StatusCode);
        var wrongHeader = await admin.PostAsync(importUrl, File("external_ref;property_type;rooom\nA;Apartment;2\n"));
        Assert.Equal(HttpStatusCode.BadRequest, wrongHeader.StatusCode);

        var accepted = await admin.PostAsync(importUrl, File(Csv(externalRef)));
        Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
        var upload = (await accepted.Content.ReadFromJsonAsync<ListingImportJobDto>())!;
        Assert.Equal(("Pending", "anunturi.csv"), (upload.Status, upload.FileName));

        var second = await admin.PostAsync(importUrl, File(Csv(externalRef)));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(ErrorCodes.ImportAlreadyRunning, await CodeOf(second));

        // The agency's own Owner can't import or read imports: IMOVA imports agencies' listings by hand.
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PostAsync(importUrl, File(Csv(externalRef)))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync(importUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync($"{importUrl}/{upload.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync($"{importUrl}/{upload.Id}/rows")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync($"{importUrl}/{upload.Id}/report.csv")).StatusCode);

        // The Worker's tick (any other waiting import is processed first, oldest first).
        while (await RunWorkerJobAsync() > 0)
        {
            var current = (await admin.GetFromJsonAsync<ListingImportJobDto>($"{importUrl}/{upload.Id}"))!;
            if (current.Status is "Completed" or "Failed")
            {
                break;
            }
        }

        var done = (await admin.GetFromJsonAsync<ListingImportJobDto>($"{importUrl}/{upload.Id}"))!;
        Assert.Equal(("Completed", 1, 1, 1, 0), (done.Status, done.TotalRows, done.ProcessedRows, done.CreatedCount, done.FailedCount));
        Assert.Single((await admin.GetFromJsonAsync<List<ListingImportJobDto>>(importUrl))!);

        await using var db = ListingApi.NewDbContext();
        var listing = await db.Listings.AsNoTracking().SingleAsync(l => l.AgencyId == agency.Id && l.ExternalRef == externalRef);
        Assert.Equal(ListingStatus.Active, listing.Status);
        Assert.Equal(ownerUser.Id, (await db.Publishers.SingleAsync(p => p.Id == listing.PublisherId)).UserId);
        var photos = await db.Photos.Where(p => p.ListingId == listing.Id).OrderBy(p => p.SortOrder).ToListAsync();
        Assert.Equal(3, photos.Count);
        Assert.All(photos, p => Assert.True(p.SizesVersion > 0));
        Assert.Equal(HttpStatusCode.OK, (await _factory.CreateClient().GetAsync($"/api/v1/listings/{listing.Id}")).StatusCode);

        var rows = (await admin.GetFromJsonAsync<ListingImportRowsPageDto>($"{importUrl}/{upload.Id}/rows"))!;
        Assert.Equal(("Created", listing.Id), (Assert.Single(rows.Items).Outcome, rows.Items[0].ListingId));
        var report = await admin.GetAsync($"{importUrl}/{upload.Id}/report.csv");
        Assert.Equal("text/csv", report.Content.Headers.ContentType!.MediaType);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync(importUrl)).StatusCode);
    }
}
