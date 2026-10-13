using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Auth;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Imova.IntegrationTests.Account;

// The account's language through the real endpoints: taken from the site at sign-up, changed when
// the signed-in user switches language, and the one the account's emails are written in.
public class PreferredLanguageEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly CapturingEmailSender _email = new();
    private readonly WebApplicationFactory<Program> _factory;

    public PreferredLanguageEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = ListingApi.Configure(factory).WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(_email);
        }));
    }

    [Fact]
    public async Task SignUpLanguage_ThenASwitch_DecideTheLanguageOfTheEmails()
    {
        var client = _factory.CreateClient();
        var email = $"language-{Guid.NewGuid():N}@example.com";

        var registered = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "SuperSecret1!",
            displayName = "Ana",
            phoneNumber = "+373 69 123 456",
            language = "en",
        });
        registered.EnsureSuccessStatusCode();
        var auth = (await registered.Content.ReadFromJsonAsync<AuthResultDto>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        // The confirmation link went out in the language the site was in.
        Assert.Equal("Confirm your email address — IMOVA", Assert.Single(_email.SentTo(email)).Subject);

        var switched = await client.PutAsJsonAsync("/api/v1/users/me/language", new { language = "ru" });
        Assert.Equal(HttpStatusCode.NoContent, switched.StatusCode);

        (await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email })).EnsureSuccessStatusCode();
        Assert.Equal("Сброс пароля — IMOVA", _email.SentTo(email)[^1].Subject);
    }

    [Fact]
    public async Task AnUnknownLanguage_Is400_AndSignedOut_Is401()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);

        var unknown = await client.PutAsJsonAsync("/api/v1/users/me/language", new { language = "de" });
        var anonymous = await _factory.CreateClient().PutAsJsonAsync("/api/v1/users/me/language", new { language = "en" });

        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }
}
