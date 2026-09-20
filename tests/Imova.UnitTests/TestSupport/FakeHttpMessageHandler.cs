namespace Imova.UnitTests.TestSupport;

// Lets a service built on HttpClient be tested without any real network call — construct with
// `new HttpClient(new FakeHttpMessageHandler(...))`. The responder can throw to simulate a network
// failure; that's wrapped into the returned task's fault state (Task.FromException) rather than
// thrown synchronously, matching how a real transport failure would surface through HttpClient.
internal sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        try
        {
            return Task.FromResult(responder(request));
        }
        catch (Exception ex)
        {
            return Task.FromException<HttpResponseMessage>(ex);
        }
    }
}
