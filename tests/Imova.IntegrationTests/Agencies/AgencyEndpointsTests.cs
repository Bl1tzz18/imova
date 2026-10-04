using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ImageMagick;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Agencies.CreateAgency;
using Imova.Contracts.Agencies;
using Imova.Domain.Agencies;
using Imova.Infrastructure;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Imova.IntegrationTests.Agencies;

// Creating and editing an agency, and its logo — against the real database and Azurite.
public class AgencyEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AgencyEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = ListingApi.Configure(factory);
    }

    private static object Profile(string name, string? bio = null) => new
    {
        name,
        phone = "+373 22 555 010",
        email = "office@agentie.md",
        bio,
        website = "https://agentie.md",
    };

    private static async Task<AgencyDto> CreateAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/agencies", Profile(name));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AgencyDto>())!;
    }

    private static Task<HttpResponseMessage> UploadLogoAsync(HttpClient client, Guid agencyId, byte[] content, string fileName = "logo.png")
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", fileName);
        return client.PostAsync($"/api/v1/agencies/{agencyId}/logo", form);
    }

    private static async Task<HttpStatusCode> FetchStatusAsync(string url)
    {
        using var http = new HttpClient();
        return (await http.GetAsync(url)).StatusCode;
    }

    [Fact]
    public async Task Create_MakesTheCallerOwner_AndNumbersASecondAgencyWithTheSameName()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);
        var name = $"Agenția Test {Guid.NewGuid():N}";

        var first = await CreateAsync(client, name);
        var second = await CreateAsync(client, name);

        Assert.Equal("Owner", first.MyRole);
        Assert.Equal(1, first.MemberCount);
        Assert.StartsWith("agentia-test-", first.Slug);
        Assert.Equal($"{first.Slug}-2", second.Slug);
    }

    [Fact]
    public async Task Create_Anonymously_Is401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/v1/agencies", Profile("Anonim"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithABadWebsite_Is400WithItsCode()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);

        var response = await client.PostAsJsonAsync("/api/v1/agencies", new { name = "X", phone = "+373 22 555 010", website = "agentie.md" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("agency.websiteInvalid", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_IsPublic_ButTheNumberIsOnlyForMembers()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var created = await CreateAsync(owner, $"Agenția Publică {Guid.NewGuid():N}");

        var asPublic = (await _factory.CreateClient().GetFromJsonAsync<AgencyDto>($"/api/v1/agencies/{created.Id}"))!;
        var asOwner = (await owner.GetFromJsonAsync<AgencyDto>($"/api/v1/agencies/{created.Id}"))!;

        Assert.Null(asPublic.Phone);
        Assert.NotNull(asPublic.PhonePrefix);
        Assert.Equal("+373 22 555 010", asOwner.Phone);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/v1/agencies/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Edit_ByTheOwner_Succeeds_ByAnotherUser_Is403_Anonymously_Is401()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var (stranger, _) = await ListingApi.RegisterAsync(_factory);
        var created = await CreateAsync(owner, $"Agenția Editată {Guid.NewGuid():N}");

        var byOwner = await owner.PutAsJsonAsync($"/api/v1/agencies/{created.Id}", Profile(created.Name, bio: "Din 2005."));
        var byStranger = await stranger.PutAsJsonAsync($"/api/v1/agencies/{created.Id}", Profile(created.Name, bio: "Hack"));
        var anonymous = await _factory.CreateClient().PutAsJsonAsync($"/api/v1/agencies/{created.Id}", Profile(created.Name));

        Assert.Equal(HttpStatusCode.OK, byOwner.StatusCode);
        Assert.Equal("Din 2005.", (await byOwner.Content.ReadFromJsonAsync<AgencyDto>())!.Bio);
        Assert.Equal(HttpStatusCode.Forbidden, byStranger.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task Rename_MovesTheSlug()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var suffix = Guid.NewGuid().ToString("N");
        var created = await CreateAsync(owner, $"Vechiul Nume {suffix}");

        var renamed = await owner.PutAsJsonAsync($"/api/v1/agencies/{created.Id}", Profile($"Noul Nume {suffix}"));

        Assert.Equal($"noul-nume-{suffix}", (await renamed.Content.ReadFromJsonAsync<AgencyDto>())!.Slug);
    }

    [Fact]
    public async Task Logo_IsStoredAsSquareJpegs_AndReplacingItDeletesTheOldFiles()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var created = await CreateAsync(owner, $"Agenția Logo {Guid.NewGuid():N}");
        var png = new MagickImage(MagickColors.SteelBlue, 600, 300).ToByteArray(MagickFormat.Png);

        var first = await UploadLogoAsync(owner, created.Id, png);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstDto = (await first.Content.ReadFromJsonAsync<AgencyDto>())!;

        using (var http = new HttpClient())
        {
            using var large = new MagickImage(await http.GetByteArrayAsync(firstDto.LogoUrl));
            using var small = new MagickImage(await http.GetByteArrayAsync(firstDto.LogoThumbnailUrl));
            Assert.Equal(MagickFormat.Jpeg, large.Format);
            Assert.Equal((512u, 512u), (large.Width, large.Height));
            Assert.Equal((128u, 128u), (small.Width, small.Height));
        }

        var second = (await (await UploadLogoAsync(owner, created.Id, png)).Content.ReadFromJsonAsync<AgencyDto>())!;

        Assert.NotEqual(firstDto.LogoUrl, second.LogoUrl);
        Assert.Equal(HttpStatusCode.NotFound, await FetchStatusAsync(firstDto.LogoUrl!));
        Assert.Equal(HttpStatusCode.NotFound, await FetchStatusAsync(firstDto.LogoThumbnailUrl!));
        Assert.Equal(HttpStatusCode.OK, await FetchStatusAsync(second.LogoUrl!));

        var removed = await owner.DeleteAsync($"/api/v1/agencies/{created.Id}/logo");
        Assert.Null((await removed.Content.ReadFromJsonAsync<AgencyDto>())!.LogoUrl);
        Assert.Equal(HttpStatusCode.NotFound, await FetchStatusAsync(second.LogoUrl!));
    }

    [Fact]
    public async Task Logo_WithAFakeExtension_Or_TheWrongType_Or_TooLarge_Is400()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var created = await CreateAsync(owner, $"Agenția Refuz {Guid.NewGuid():N}");
        var gif = new MagickImage(MagickColors.SteelBlue, 20, 20).ToByteArray(MagickFormat.Gif);

        var fake = await UploadLogoAsync(owner, created.Id, "<html>not an image</html>"u8.ToArray(), "logo.png");
        var wrongType = await UploadLogoAsync(owner, created.Id, gif, "logo.gif");
        var tooLarge = await UploadLogoAsync(owner, created.Id, new byte[5 * 1024 * 1024 + 1]);

        Assert.Equal(HttpStatusCode.BadRequest, fake.StatusCode);
        Assert.Contains("agency.logoType", await fake.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, wrongType.StatusCode);
        Assert.Contains("agency.logoType", await wrongType.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, tooLarge.StatusCode);
        Assert.Contains("upload.tooLarge", await tooLarge.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Logo_WithItsLongerSideUnderTwoHundredPixels_Is400_ButAWideTextLogoIsFine()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var created = await CreateAsync(owner, $"Agenția Mică {Guid.NewGuid():N}");
        var small = new MagickImage(MagickColors.SteelBlue, 190, 120).ToByteArray(MagickFormat.Png);
        var wide = new MagickImage(MagickColors.SteelBlue, 600, 150).ToByteArray(MagickFormat.Png);

        var refused = await UploadLogoAsync(owner, created.Id, small);
        var accepted = await UploadLogoAsync(owner, created.Id, wide);

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Contains("agency.logoTooSmall", await refused.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
    }

    [Fact]
    public async Task Create_WithAnUnconfirmedEmail_Is403WithItsCode()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory, confirmEmail: false);

        var response = await client.PostAsJsonAsync("/api/v1/agencies", Profile($"Neconfirmat {Guid.NewGuid():N}"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("agency.emailNotConfirmed", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Create_AFourthOwnedAgency_Is400WithItsCode()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);
        for (var i = 0; i < 3; i++)
        {
            await CreateAsync(client, $"Agenția Limită {i} {Guid.NewGuid():N}");
        }

        var fourth = await client.PostAsJsonAsync("/api/v1/agencies", Profile($"Agenția Limită 4 {Guid.NewGuid():N}"));

        Assert.Equal(HttpStatusCode.BadRequest, fourth.StatusCode);
        Assert.Contains("agency.limitReached", await fourth.Content.ReadAsStringAsync());
    }

    // What the create handler's retry relies on: Postgres's refusal of a duplicate slug is recognised
    // as exactly that index.
    [Fact]
    public async Task ADuplicateSlug_IsRecognisedAsTheSlugIndex()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ImovaDbContext>();
        var errors = scope.ServiceProvider.GetRequiredService<IDatabaseErrors>();
        var (_, user) = await ListingApi.RegisterAsync(_factory);
        var slug = $"dublura-{Guid.NewGuid():N}";
        var profile = new AgencyProfile("Dublura", "+373 22 555 010", "d@example.com");

        db.Agencies.Add(Agency.Create(profile, slug, user.Id, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();
        db.Agencies.Add(Agency.Create(profile, slug, user.Id, DateTimeOffset.UtcNow));
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        Assert.True(errors.IsUniqueViolation(ex, CreateAgencyHandler.SlugIndex));
        Assert.False(errors.IsUniqueViolation(ex, "IX_Something_Else"));
    }

    [Fact]
    public async Task Logo_ByAnotherUser_Is403()
    {
        var (owner, _) = await ListingApi.RegisterAsync(_factory);
        var (stranger, _) = await ListingApi.RegisterAsync(_factory);
        var created = await CreateAsync(owner, $"Agenția Străină {Guid.NewGuid():N}");
        var png = new MagickImage(MagickColors.SteelBlue, 50, 50).ToByteArray(MagickFormat.Png);

        Assert.Equal(HttpStatusCode.Forbidden, (await UploadLogoAsync(stranger, created.Id, png)).StatusCode);
    }
}
