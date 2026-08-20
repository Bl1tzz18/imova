using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Imova.IntegrationTests;

// Requires the compose Postgres to be reachable (`docker compose up -d postgres`),
// since Program.cs runs EF migrations on startup. Not using Testcontainers yet —
// not justified until the test suite grows beyond a smoke test.
public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(
                "ConnectionStrings:Default",
                "Host=localhost;Port=5432;Database=imova;Username=imova;Password=imova");
        });
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
