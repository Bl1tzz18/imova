using System.Collections.Concurrent;
using System.Net.Http.Json;
using Imova.Application.Common.Logging;
using Imova.Contracts.Auth;
using Imova.Contracts.Listings;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Imova.IntegrationTests.Monitoring;

// The business events (AppEvents) through the real pipeline: a signed-in request's events carry the
// caller's account id (CallerLogScope), so "who approved this listing?" has an answer.
public class BusinessEventLogTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly EventCapture _events = new();

    private WebApplicationFactory<Program> Factory() =>
        ListingApi.Configure(factory).WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddSingleton<ILoggerProvider>(_events)));

    [Fact]
    public async Task ApprovingAListing_IsLoggedWithTheAdminAndTheListing()
    {
        var app = Factory();
        var (owner, _) = await ListingApi.RegisterAsync(app);
        var admin = await ListingApi.RegisterAdminAsync(app);
        var adminId = (await admin.GetFromJsonAsync<UserProfileDto>("/api/v1/auth/me"))!.Id;
        var listing = (await (await owner.PostAsJsonAsync("/api/v1/listings", await ListingApi.ValidBodyAsync(owner))).Content.ReadFromJsonAsync<ListingDto>())!;

        (await admin.PostAsync($"/api/v1/listings/{listing.Id}/approve", null)).EnsureSuccessStatusCode();

        var entry = Assert.Single(_events.Entries, e => e.Message.StartsWith("ApproveListing succeeded"));
        Assert.Equal(adminId, entry.Scope["CallerId"]);
        Assert.Equal(listing.Id, entry.Scope["Id"]);

        await owner.DeleteAsync($"/api/v1/listings/{listing.Id}");
    }

    [Fact]
    public async Task AWrongPassword_IsARefusalWithItsCode_AndNoEmail()
    {
        var app = Factory();
        var (_, user) = await ListingApi.RegisterAsync(app);

        await app.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new { email = user.Email, password = "Wrong-password1!" });

        var entry = Assert.Single(_events.Entries, e => e.Message.StartsWith("Login refused"));
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.DoesNotContain(user.Email, entry.Message);
        // A typo in a password is not an error: nothing at Error level (which the alerts watch).
        Assert.Empty(_events.AllEntries.Where(e => e.Level >= LogLevel.Error).Select(e => $"{e.Category}: {e.Message[..Math.Min(120, e.Message.Length)]}"));
    }

    private sealed class EventCapture : ILoggerProvider
    {
        public sealed record Entry(string Category, LogLevel Level, string Message, IReadOnlyDictionary<string, object?> Scope);

        private readonly ConcurrentQueue<Entry> _entries = new();
        private readonly LoggerExternalScopeProvider _scopes = new();

        // Business events only; AllEntries has every category.
        public IReadOnlyList<Entry> Entries => [.. _entries.Where(e => e.Category == AppEvents.Category)];

        public IReadOnlyList<Entry> AllEntries => [.. _entries];

        public ILogger CreateLogger(string categoryName) => new Logger(this, categoryName);

        public void Dispose() { }

        private sealed class Logger(EventCapture capture, string category) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => capture._scopes.Push(state);

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                var scope = new Dictionary<string, object?>();
                capture._scopes.ForEachScope((value, values) =>
                {
                    if (value is IEnumerable<KeyValuePair<string, object>> pairs)
                    {
                        foreach (var pair in pairs) values[pair.Key] = pair.Value;
                    }
                }, scope);
                capture._entries.Enqueue(new Entry(category, logLevel, formatter(state, exception), scope));
            }
        }
    }
}
