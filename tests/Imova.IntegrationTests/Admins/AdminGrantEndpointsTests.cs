using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Imova.Application.Common.Interfaces;
using Imova.Contracts.Admins;
using Imova.Contracts.Auth;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Imova.IntegrationTests.Admins;

public class AdminGrantEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "SuperSecret1!";
    private readonly CapturingEmailSender _email = new();
    private readonly WebApplicationFactory<Program> _factory;

    public AdminGrantEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = ListingApi.Configure(factory).WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(_email);
        }));
    }

    private static Task<HttpResponseMessage> GrantAsync(HttpClient client, string email, string? password = Password) =>
        client.PostAsJsonAsync("/api/v1/admin/admins", new { email, password });

    private static async Task<string?> CodeAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("errorCodes")[0].GetProperty("code").GetString();
    }

    [Fact]
    public async Task AnAdmin_WithTheirPassword_MakesAConfirmedAccountAnAdmin()
    {
        using var admin = await ListingApi.RegisterAdminAsync(_factory);
        var (_, target) = await ListingApi.RegisterAsync(_factory);

        Assert.Equal(HttpStatusCode.NoContent, (await GrantAsync(admin, target.Email.ToUpperInvariant())).StatusCode);

        // A fresh sign-in carries the role and opens the admin routes.
        var targetClient = _factory.CreateClient();
        var login = await targetClient.PostAsJsonAsync("/api/v1/auth/login", new { email = target.Email, password = Password });
        var auth = (await login.Content.ReadFromJsonAsync<AuthResultDto>())!;
        targetClient.DefaultRequestHeaders.Authorization = new("Bearer", auth.Token);
        Assert.Equal(HttpStatusCode.OK, (await targetClient.GetAsync("/api/v1/admin/listings?status=Active")).StatusCode);

        var admins = (await admin.GetFromJsonAsync<List<AdminUserDto>>("/api/v1/admin/admins"))!;
        var row = Assert.Single(admins, a => a.Id == target.Id);
        Assert.NotNull(row.GrantedAt);
        Assert.NotNull(row.GrantedBy);
        Assert.NotEmpty(_email.SentTo(target.Email));
    }

    [Fact]
    public async Task NonAdmins_AndAnonymousCallers_AreRefused()
    {
        var (user, _) = await ListingApi.RegisterAsync(_factory);
        var (_, target) = await ListingApi.RegisterAsync(_factory);

        Assert.Equal(HttpStatusCode.Forbidden, (await GrantAsync(user, target.Email)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/v1/admin/admins")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await GrantAsync(_factory.CreateClient(), target.Email)).StatusCode);
        Assert.Empty(_email.SentTo(target.Email).Where(m => m.Subject.Contains("administrator")));
    }

    [Fact]
    public async Task AWrongPassword_OrAnUnconfirmedAccount_IsRefused()
    {
        using var admin = await ListingApi.RegisterAdminAsync(_factory);
        var (_, confirmed) = await ListingApi.RegisterAsync(_factory);
        var (_, unconfirmed) = await ListingApi.RegisterAsync(_factory, confirmEmail: false);

        var wrong = await GrantAsync(admin, confirmed.Email, "NotMine123!");
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        Assert.Equal("account.wrongPassword", await CodeAsync(wrong));

        var notConfirmed = await GrantAsync(admin, unconfirmed.Email);
        Assert.Equal(HttpStatusCode.BadRequest, notConfirmed.StatusCode);
        Assert.Equal("admin.emailNotConfirmed", await CodeAsync(notConfirmed));

        var admins = (await admin.GetFromJsonAsync<List<AdminUserDto>>("/api/v1/admin/admins"))!;
        Assert.DoesNotContain(admins, a => a.Id == confirmed.Id || a.Id == unconfirmed.Id);
    }
}
