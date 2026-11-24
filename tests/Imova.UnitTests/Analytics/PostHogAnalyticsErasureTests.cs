using System.Net;
using Imova.Infrastructure.Analytics;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Analytics;

public class PostHogAnalyticsErasureTests
{
    private static readonly Guid UserId = Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff");
    private readonly List<string> _requests = [];

    private PostHogAnalyticsErasure Create(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(
            new HttpClient(new FakeHttpMessageHandler(request =>
            {
                _requests.Add($"{request.Method} {request.RequestUri!.PathAndQuery}");
                return respond(request);
            })) { BaseAddress = new Uri("https://eu.posthog.test/") },
            new PostHogOptions { ProjectId = "123", PersonalApiKey = "phx_key" });

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body) };

    [Fact]
    public async Task FindsThePersonByAccountId_ThenDeletesThemWithTheirEventsAndRecordings()
    {
        var erasure = Create(request => request.Method == HttpMethod.Get
            ? Json("""{"results":[{"id":"0190-person","distinct_ids":["x"]}]}""")
            : new HttpResponseMessage(HttpStatusCode.Accepted));

        await erasure.EraseUserAsync(UserId, CancellationToken.None);

        Assert.Equal(
            [
                $"GET /api/projects/123/persons/?distinct_id={UserId}",
                "DELETE /api/projects/123/persons/0190-person/?delete_events=true&delete_recordings=true",
            ],
            _requests);
    }

    [Fact]
    public async Task NoPerson_DeletesNothing()
    {
        var erasure = Create(_ => Json("""{"results":[]}"""));

        await erasure.EraseUserAsync(UserId, CancellationToken.None);

        Assert.Single(_requests);
    }

    [Fact]
    public async Task APersonAlreadyGone_IsNotAnError()
    {
        var erasure = Create(request => request.Method == HttpMethod.Get
            ? Json("""{"results":[{"id":"p1"}]}""")
            : new HttpResponseMessage(HttpStatusCode.NotFound));

        await erasure.EraseUserAsync(UserId, CancellationToken.None);
    }

    [Fact]
    public async Task AFailedDelete_Throws()
    {
        var erasure = Create(request => request.Method == HttpMethod.Get
            ? Json("""{"results":[{"id":"p1"}]}""")
            : new HttpResponseMessage(HttpStatusCode.InternalServerError));

        await Assert.ThrowsAsync<HttpRequestException>(() => erasure.EraseUserAsync(UserId, CancellationToken.None));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("123", "")]
    [InlineData("", "phx_key")]
    [InlineData("  ", "phx_key")]
    public void IsConfigured_NeedsBothTheProjectAndTheKey(string projectId, string key) =>
        Assert.False(new PostHogOptions { ProjectId = projectId, PersonalApiKey = key }.IsConfigured);
}
