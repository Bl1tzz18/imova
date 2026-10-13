using System.Net;
using System.Net.Http.Json;
using System.Text;
using Imova.Contracts.Agencies;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Imova.IntegrationTests.Agencies;

// The import's template and columns guide end to end: a site admin downloads a CSV for a property type;
// the agency's own Owner and anyone else are refused (IMOVA imports agencies' listings by hand); a
// property type that doesn't exist is a 400.
public class AgencyImportTemplateEndpointsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = ListingApi.Configure(factory);

    [Fact]
    public async Task ASiteAdmin_DownloadsTheTemplate_AndTheColumns_TheAgencyItselfIsRefused()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var (stranger, _) = await ListingApi.RegisterAsync(_factory);
        var admin = await ListingApi.RegisterAdminAsync(_factory);
        var agency = (await (await owner.PostAsJsonAsync("/api/v1/agencies", new { name = $"Agenția Import {Guid.NewGuid():N}", phone = "+373 22 555 010" }))
            .Content.ReadFromJsonAsync<AgencyDto>())!;
        var template = $"/api/v1/agencies/{agency.Id}/import/template?propertyType=Apartment";

        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync(template)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync($"/api/v1/agencies/{agency.Id}/import/columns?propertyType=Room")).StatusCode);

        var response = await admin.GetAsync(template);
        response.EnsureSuccessStatusCode();
        Assert.Equal("text/csv", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("imova-import-apartment.csv", response.Content.Headers.ContentDisposition!.FileNameStar ?? response.Content.Headers.ContentDisposition.FileName);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        Assert.StartsWith("external_ref;property_type;deal_type;", text);
        Assert.Contains(";Apartment;sale;", text);
        Assert.Contains("Chișinău", text);

        var columns = (await admin.GetFromJsonAsync<List<ImportColumnDto>>($"/api/v1/agencies/{agency.Id}/import/columns?propertyType=Room"))!;
        Assert.Contains(columns, c => c.Name == "bathroom_type" && c.Requirement == "Required");
        Assert.Contains("kitchen_access", columns.Single(c => c.Name == "amenities").Choices!);

        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync(template)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync(template)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/v1/agencies/{Guid.NewGuid()}/import/template?propertyType=Apartment")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync($"/api/v1/agencies/{agency.Id}/import/template?propertyType=Castle")).StatusCode);
    }
}
