using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Listings;
using Imova.Contracts.Messaging;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Imova.IntegrationTests.Account;

// The personal data export and account deletion, through the real endpoints and database, with
// emails captured instead of sent.
public class AccountDataEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "SuperSecret1!";
    private readonly WebApplicationFactory<Program> _baseFactory;
    private readonly CapturingEmailSender _email = new();
    private readonly WebApplicationFactory<Program> _factory;

    public AccountDataEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _baseFactory = factory;
        _factory = WithCapturedEmail(ListingApi.Configure(factory));
    }

    private WebApplicationFactory<Program> WithCapturedEmail(WebApplicationFactory<Program> factory) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(_email);
        }));

    private static async Task<ListingDto> CreateListingAsync(HttpClient owner)
    {
        var response = await owner.PostAsJsonAsync("/api/v1/listings", await ListingApi.ValidBodyAsync(owner));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ListingDto>())!;
    }

    private static Task<HttpResponseMessage> DeleteAccountAsync(HttpClient client, string? password) =>
        client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/users/me") { Content = JsonContent.Create(new { password }) });

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });

    private static async Task<string?> ProblemCodeAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (body.RootElement.TryGetProperty("code", out var code))
        {
            return code.GetString();
        }

        return body.RootElement.GetProperty("errorCodes")[0].GetProperty("code").GetString();
    }

    [Fact]
    public async Task DataExport_IsAZipWithTheAccountsDataAndListings()
    {
        var (client, user) = await ListingApi.RegisterAsync(_factory);
        var listing = await CreateListingAsync(client);

        var response = await client.GetAsync("/api/v1/users/me/data-export");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/zip", response.Content.Headers.ContentType?.MediaType);
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"');
        Assert.StartsWith("imova-date-personale-", fileName);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString());

        using var zip = new ZipArchive(await response.Content.ReadAsStreamAsync(), ZipArchiveMode.Read);
        using var data = JsonDocument.Parse(await new StreamReader(zip.GetEntry("imova-data.json")!.Open()).ReadToEndAsync());
        var root = data.RootElement;
        Assert.Equal(user.Email, root.GetProperty("account").GetProperty("email").GetString());
        Assert.Equal(listing.Id, root.GetProperty("listings")[0].GetProperty("listing").GetProperty("id").GetGuid());
        Assert.Equal(1, root.GetProperty("sessions").GetArrayLength());
        Assert.NotNull(zip.GetEntry("CITESTE-MA.txt"));
    }

    [Fact]
    public async Task DataSummary_CountsTheAccountsListings()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);
        await CreateListingAsync(client);

        using var summary = JsonDocument.Parse(await client.GetStringAsync("/api/v1/users/me/data-summary"));

        Assert.Equal(1, summary.RootElement.GetProperty("listings").GetInt32());
        Assert.True(summary.RootElement.GetProperty("hasPassword").GetBoolean());
    }

    [Fact]
    public async Task TheAccountEndpoints_NeedASignedInUser()
    {
        var anonymous = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/users/me/data-export")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/users/me/data-summary")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await DeleteAccountAsync(anonymous, Password)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsync("/api/v1/users/me/deletion-link", null)).StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_WithTheWrongPassword_Is400_AndKeepsTheAccount()
    {
        var (client, user) = await ListingApi.RegisterAsync(_factory);

        var response = await DeleteAccountAsync(client, "NotMyPassword1!");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("account.wrongPassword", await ProblemCodeAsync(response));
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(_factory.CreateClient(), user.Email)).StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_WithThePassword_ErasesItAndEndsEverySession()
    {
        var (client, user) = await ListingApi.RegisterAsync(_factory);
        var listing = await CreateListingAsync(client);
        var anonymous = _factory.CreateClient();

        var response = await DeleteAccountAsync(client, Password);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(anonymous, user.Email)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/v1/listings/{listing.Id}")).StatusCode);

        var goodbye = _email.SentTo(user.Email).Last();
        Assert.Equal("Contul tău IMOVA a fost șters", goodbye.Subject);

        // The address is free again: the person can come back with a new account.
        var again = await anonymous.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = user.Email, password = Password, displayName = "Din nou", phoneNumber = "+373 69 123 456",
        });
        Assert.True(again.IsSuccessStatusCode);
    }

    [Fact]
    public async Task DeletionLink_EmailedAndConfirmed_ErasesTheAccount_AndCannotBeReused()
    {
        var (client, user) = await ListingApi.RegisterAsync(_factory);
        var anonymous = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/v1/users/me/deletion-link", null)).StatusCode);
        var link = _email.LinkQuery(user.Email);
        Assert.Equal(user.Id.ToString(), link["userId"]);

        var confirm = await anonymous.PostAsJsonAsync("/api/v1/auth/delete-account", new { userId = user.Id, token = link["token"] });
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(anonymous, user.Email)).StatusCode);

        var reuse = await anonymous.PostAsJsonAsync("/api/v1/auth/delete-account", new { userId = user.Id, token = link["token"] });
        Assert.Equal(HttpStatusCode.BadRequest, reuse.StatusCode);
        Assert.Equal("link.invalid", await ProblemCodeAsync(reuse));
    }

    [Fact]
    public async Task DeletionLink_WithAForgedToken_Is400_AndKeepsTheAccount()
    {
        var (_, user) = await ListingApi.RegisterAsync(_factory);
        var anonymous = _factory.CreateClient();

        var confirm = await anonymous.PostAsJsonAsync("/api/v1/auth/delete-account", new { userId = user.Id, token = "Zm9yZ2Vk" });

        Assert.Equal(HttpStatusCode.BadRequest, confirm.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(anonymous, user.Email)).StatusCode);
    }

    [Fact]
    public async Task AfterTheSellerDeletesTheirAccount_TheVisitorKeepsTheThreadButCannotReply()
    {
        var (seller, _) = await ListingApi.RegisterAsync(_factory);
        var (visitor, _) = await ListingApi.RegisterAsync(_factory);
        var listing = await CreateListingAsync(seller);
        using (var admin = await ListingApi.RegisterAdminAsync(_factory))
        {
            (await admin.PostAsync($"/api/v1/listings/{listing.Id}/approve", null)).EnsureSuccessStatusCode();
        }

        var started = await visitor.PostAsJsonAsync("/api/v1/messaging/conversations", new { listingId = listing.Id, body = "Bună ziua!" });
        started.EnsureSuccessStatusCode();
        var conversationId = (await started.Content.ReadFromJsonAsync<StartConversationResultDto>())!.ConversationId;

        Assert.Equal(HttpStatusCode.NoContent, (await DeleteAccountAsync(seller, Password)).StatusCode);

        var thread = (await visitor.GetFromJsonAsync<ConversationThreadDto>($"/api/v1/messaging/conversations/{conversationId}"))!;
        Assert.True(thread.Conversation.OtherParticipant.IsDeleted);
        Assert.Single(thread.Messages);

        var reply = await visitor.PostAsJsonAsync($"/api/v1/messaging/conversations/{conversationId}/messages", new { body = "Alo?" });
        Assert.Equal(HttpStatusCode.Forbidden, reply.StatusCode);
        Assert.Equal("messaging.recipientDeleted", await ProblemCodeAsync(reply));
    }

    [Fact]
    public async Task DataExport_IsRateLimitedPerUser()
    {
        var factory = WithCapturedEmail(ListingApi.Configure(_baseFactory)).WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:Account:Enabled", "true");
            builder.UseSetting("RateLimiting:Account:PermitLimit", "2");
        });
        var (client, _) = await ListingApi.RegisterAsync(factory);
        var (other, _) = await ListingApi.RegisterAsync(factory);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/users/me/data-export")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/users/me/data-export")).StatusCode);
        var limited = await client.GetAsync("/api/v1/users/me/data-export");

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await other.GetAsync("/api/v1/users/me/data-export")).StatusCode);
    }
}
