using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Imova.IntegrationTests.Auth;

// A missing Jwt setting used to go unnoticed: the API started, signed tokens without an issuer or
// audience and then refused every one of them (a 401 on every signed-in call — how CI first failed,
// since Issuer/Audience only lived in a gitignored local file). Now the host refuses to start.
public class JwtConfigurationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public JwtConfigurationTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Theory]
    [InlineData("Key")]
    [InlineData("Issuer")]
    [InlineData("Audience")]
    public void TheApi_RefusesToStart_WithoutAJwtSetting(string setting)
    {
        using var misconfigured = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", ListingApi.ConnectionString);
            builder.UseSetting($"Jwt:{setting}", "");
        });

        var error = Assert.Throws<InvalidOperationException>(() => misconfigured.CreateClient());
        Assert.Contains($"Jwt:{setting}", error.Message);
    }
}
