using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Imova.Contracts.Listings;
using Imova.Contracts.Messaging;
using Imova.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;

namespace Imova.IntegrationTests.Messaging;

public class MessagingEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(10);
    private readonly WebApplicationFactory<Program> _factory;

    public MessagingEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = ListingApi.Configure(factory);
    }

    // A seller with an approved (Active) listing and a separate visitor.
    private async Task<(HttpClient Seller, HttpClient Visitor, Guid SellerId, Guid VisitorId, ListingDto Listing)> SetUpAsync(
        WebApplicationFactory<Program>? factory = null, object? contact = null)
    {
        factory ??= _factory;
        var (seller, sellerUser) = await ListingApi.RegisterAsync(factory);
        var (visitor, visitorUser) = await ListingApi.RegisterAsync(factory);
        var listing = await CreateActiveListingAsync(factory, seller, contact);
        return (seller, visitor, sellerUser.Id, visitorUser.Id, listing);
    }

    private static async Task<ListingDto> CreateActiveListingAsync(WebApplicationFactory<Program> factory, HttpClient owner, object? contact = null)
    {
        var admin = await ListingApi.RegisterAdminAsync(factory);
        var body = await ListingApi.ValidBodyAsync(owner);
        if (contact is not null)
        {
            body["contact"] = contact;
        }

        var listing = (await (await owner.PostAsJsonAsync("/api/v1/listings", body)).Content.ReadFromJsonAsync<ListingDto>())!;
        (await admin.PostAsync($"/api/v1/listings/{listing.Id}/approve", null)).EnsureSuccessStatusCode();
        return listing;
    }

    private static async Task<StartConversationResultDto> StartAsync(HttpClient visitor, Guid listingId, string body = "Bună ziua!")
    {
        var response = await visitor.PostAsJsonAsync("/api/v1/messaging/conversations", new { listingId, body });
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<StartConversationResultDto>())!;
    }

    private async Task<HubConnection> ConnectAsync(HttpClient client)
    {
        var token = (await (await client.PostAsync("/api/v1/messaging/realtime-token", null))
            .Content.ReadFromJsonAsync<RealtimeTokenDto>())!.Token;
        var server = _factory.Server;
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(server.BaseAddress, "/hubs/messaging"), options =>
            {
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();
        await connection.StartAsync();
        return connection;
    }

    private static TaskCompletionSource<T> Expect<T>(HubConnection connection, string eventName, Func<T, bool>? match = null)
    {
        var received = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<T>(eventName, payload =>
        {
            if (match is null || match(payload))
            {
                received.TrySetResult(payload);
            }
        });
        return received;
    }

    [Fact]
    public async Task StartConversation_GoesToThePublisher_EvenWhenTheListingNamesAnotherContact()
    {
        var (seller, visitor, sellerId, _, listing) = await SetUpAsync(contact: new Dictionary<string, object?>
        {
            ["personType"] = "Other", ["name"] = "Maria Popescu", ["phone"] = "+373 79 333 444", ["preferredContactMethod"] = "Any",
        });

        var response = await visitor.PostAsJsonAsync("/api/v1/messaging/conversations", new { listingId = listing.Id, body = "Bună ziua!" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var inbox = (await seller.GetFromJsonAsync<List<ConversationSummaryDto>>("/api/v1/messaging/conversations"))!;
        var row = Assert.Single(inbox);
        Assert.Equal("Bună ziua!", row.LastMessage!.Body);
        Assert.Equal(1, row.UnreadCount);
        var visitorRow = Assert.Single((await visitor.GetFromJsonAsync<List<ConversationSummaryDto>>("/api/v1/messaging/conversations"))!);
        Assert.Equal(sellerId, visitorRow.OtherParticipant.UserId);
    }

    [Fact]
    public async Task StartingAgain_ReusesTheConversation_AndItCanBeFoundByListing()
    {
        var (_, visitor, _, _, listing) = await SetUpAsync();

        var first = await StartAsync(visitor, listing.Id);
        var againResponse = await visitor.PostAsJsonAsync("/api/v1/messaging/conversations", new { listingId = listing.Id, body = "Încă o întrebare" });

        Assert.Equal(HttpStatusCode.OK, againResponse.StatusCode);
        var again = (await againResponse.Content.ReadFromJsonAsync<StartConversationResultDto>())!;
        Assert.True(again.Reused);
        Assert.Equal(first.ConversationId, again.ConversationId);
        var found = await visitor.GetFromJsonAsync<Dictionary<string, Guid>>($"/api/v1/messaging/conversations/by-listing/{listing.Id}");
        Assert.Equal(first.ConversationId, found!["conversationId"]);
    }

    [Fact]
    public async Task Messaging_RequiresLogin()
    {
        var anonymous = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/messaging/conversations")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anonymous.PostAsJsonAsync("/api/v1/messaging/conversations", new { listingId = Guid.NewGuid(), body = "x" })).StatusCode);
    }

    [Fact]
    public async Task RateLimit_ReturnsATooManyRequestsError()
    {
        var limited = _factory.WithWebHostBuilder(b => b.UseSetting("Messaging:MaxNewConversationsPerHour", "1"));
        var (seller, visitor, _, _, first) = await SetUpAsync(limited);
        var second = await CreateActiveListingAsync(limited, seller);

        await StartAsync(visitor, first.Id);
        var response = await visitor.PostAsJsonAsync("/api/v1/messaging/conversations", new { listingId = second.Id, body = "x" });

        Assert.Equal((HttpStatusCode)429, response.StatusCode);
        Assert.Contains("at most 1 new conversations per hour", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task BlockedVisitor_Gets403_AndAReportReachesAdmins()
    {
        var (seller, visitor, _, visitorId, listing) = await SetUpAsync();
        var started = await StartAsync(visitor, listing.Id);
        var id = started.ConversationId;

        Assert.Equal(HttpStatusCode.NoContent, (await seller.PostAsync($"/api/v1/messaging/conversations/{id}/block", null)).StatusCode);
        // Straight at the API (no UI in the way): neither side can send while the block stands.
        var blockedSend = await visitor.PostAsJsonAsync($"/api/v1/messaging/conversations/{id}/messages", new { body = "Alo?" });
        Assert.Equal(HttpStatusCode.Forbidden, blockedSend.StatusCode);
        var blockerSend = await seller.PostAsJsonAsync($"/api/v1/messaging/conversations/{id}/messages", new { body = "Totuși…" });
        Assert.Equal(HttpStatusCode.Forbidden, blockerSend.StatusCode);
        Assert.Contains("unblock", await blockerSend.Content.ReadAsStringAsync());
        var restart = await visitor.PostAsJsonAsync("/api/v1/messaging/conversations", new { listingId = listing.Id, body = "Din nou" });
        Assert.Equal(HttpStatusCode.Forbidden, restart.StatusCode);
        var thread = await visitor.GetFromJsonAsync<ConversationThreadDto>($"/api/v1/messaging/conversations/{id}");
        Assert.Single(thread!.Messages);

        // Unblocking restores both directions.
        Assert.Equal(HttpStatusCode.NoContent, (await seller.PostAsync($"/api/v1/messaging/conversations/{id}/unblock", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await seller.PostAsJsonAsync($"/api/v1/messaging/conversations/{id}/messages", new { body = "Revin" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await seller.PostAsync($"/api/v1/messaging/conversations/{id}/block", null)).StatusCode);

        var report = await seller.PostAsJsonAsync($"/api/v1/messaging/conversations/{id}/report", new { reason = "Spam", details = "Mesaje repetate" });
        Assert.Equal(HttpStatusCode.NoContent, report.StatusCode);

        var admin = await ListingApi.RegisterAdminAsync(_factory);
        var reports = (await admin.GetFromJsonAsync<List<MessagingReportDto>>("/api/v1/admin/messaging/reports"))!;
        var mine = Assert.Single(reports, r => r.ConversationId == id);
        Assert.Equal(visitorId, mine.ReportedUser.Id);
        Assert.Equal(HttpStatusCode.Forbidden, (await seller.GetAsync("/api/v1/admin/messaging/reports")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/api/v1/admin/messaging/reports/{mine.Id}/resolve", null)).StatusCode);
        Assert.DoesNotContain(
            (await admin.GetFromJsonAsync<List<MessagingReportDto>>("/api/v1/admin/messaging/reports"))!, r => r.Id == mine.Id);
    }

    [Fact]
    public async Task MessageImage_IsOnlyServedToTheParticipants()
    {
        var (seller, visitor, _, _, listing) = await SetUpAsync();
        var (stranger, _) = await ListingApi.RegisterAsync(_factory);

        // Upload to the private container through the SAS URL, as the browser does.
        var target = (await (await visitor.PostAsJsonAsync("/api/v1/messaging/attachments/upload-url", new { fileExtension = ".png" }))
            .Content.ReadFromJsonAsync<AttachmentUploadUrlDto>())!;
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, 0x49, 0x48, 0x44, 0x52];
        using var storage = new HttpClient();
        using var put = new HttpRequestMessage(HttpMethod.Put, target.UploadUrl) { Content = new ByteArrayContent(png) };
        put.Headers.Add("x-ms-blob-type", "BlockBlob");
        (await storage.SendAsync(put)).EnsureSuccessStatusCode();

        var started = (await (await visitor.PostAsJsonAsync("/api/v1/messaging/conversations", new
            {
                listingId = listing.Id, body = "Uitați o poză", attachmentBlobNames = new[] { target.BlobName },
            })).Content.ReadFromJsonAsync<StartConversationResultDto>())!;
        var attachment = Assert.Single(started.Message.Attachments);
        Assert.Equal($"/api/v1/messaging/attachments/{attachment.Id}", attachment.Url);

        var forSender = await visitor.GetAsync(attachment.Url);
        var forRecipient = await seller.GetAsync(attachment.Url);
        Assert.Equal(HttpStatusCode.OK, forSender.StatusCode);
        Assert.Equal("image/png", forSender.Content.Headers.ContentType!.MediaType);
        Assert.Equal(png, await forSender.Content.ReadAsByteArrayAsync());
        Assert.Contains("private", forSender.Headers.CacheControl!.ToString());
        Assert.Equal(HttpStatusCode.OK, forRecipient.StatusCode);

        // A third user gets the same 404 as for an attachment that doesn't exist; anonymous gets 401.
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync(attachment.Url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/v1/messaging/attachments/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync(attachment.Url)).StatusCode);

        // And the storage itself doesn't serve it publicly (the container is private).
        var directUrl = target.UploadUrl.Split('?')[0];
        Assert.False((await storage.GetAsync(directUrl)).IsSuccessStatusCode);

        var admin = await ListingApi.RegisterAdminAsync(_factory);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(attachment.Url)).StatusCode);
    }

    [Fact]
    public async Task UnreadCount_AndReadReceipts()
    {
        var (seller, visitor, _, _, listing) = await SetUpAsync();
        var started = await StartAsync(visitor, listing.Id);

        Assert.Equal(1, (await seller.GetFromJsonAsync<UnreadCountDto>("/api/v1/messaging/unread-count"))!.Count);
        Assert.Equal(HttpStatusCode.NoContent, (await seller.PostAsync($"/api/v1/messaging/conversations/{started.ConversationId}/read", null)).StatusCode);
        Assert.Equal(0, (await seller.GetFromJsonAsync<UnreadCountDto>("/api/v1/messaging/unread-count"))!.Count);

        var thread = await visitor.GetFromJsonAsync<ConversationThreadDto>($"/api/v1/messaging/conversations/{started.ConversationId}");
        Assert.Equal("Read", Assert.Single(thread!.Messages).Status);
    }

    [Fact]
    public async Task Hub_PushesMessagesTypingPresenceAndStatusChanges()
    {
        var (seller, visitor, sellerId, visitorId, listing) = await SetUpAsync();
        var started = await StartAsync(visitor, listing.Id);
        Assert.Equal("Sent", started.Message.Status);

        await using var visitorHub = await ConnectAsync(visitor);
        var delivered = Expect<MessageStatusChangedDto>(visitorHub, "MessageStatusChanged", c => c.Status == "Delivered");
        var sellerOnline = Expect<PresenceDto>(visitorHub, "PresenceChanged", p => p.UserId == sellerId && p.Online);

        // The seller comes online: the pending message becomes Delivered, the visitor sees them online.
        await using var sellerHub = await ConnectAsync(seller);
        Assert.Equal([started.Message.Id], (await delivered.Task.WaitAsync(EventTimeout)).MessageIds);
        await sellerOnline.Task.WaitAsync(EventTimeout);
        Assert.Equal([sellerId], await visitorHub.InvokeAsync<List<Guid>>("GetOnlineUsers", new List<Guid> { sellerId, Guid.NewGuid() }));

        // Typing reaches only the other participant.
        var typing = Expect<TypingDto>(sellerHub, "Typing");
        await visitorHub.InvokeAsync("Typing", started.ConversationId);
        Assert.Equal((started.ConversationId, visitorId), ((await typing.Task.WaitAsync(EventTimeout)).ConversationId, typing.Task.Result.UserId));

        // A new message arrives live, with the seller's unread badge count.
        var received = Expect<MessageDto>(sellerHub, "MessageReceived", m => m.Body == "Mai este disponibil?");
        var badge = Expect<UnreadCountDto>(sellerHub, "UnreadCountChanged");
        await visitor.PostAsJsonAsync($"/api/v1/messaging/conversations/{started.ConversationId}/messages", new { body = "Mai este disponibil?" });
        Assert.Equal(visitorId, (await received.Task.WaitAsync(EventTimeout)).SenderUserId);
        Assert.Equal(2, (await badge.Task.WaitAsync(EventTimeout)).Count);

        // The seller reads it: the visitor gets the Read receipt.
        var read = Expect<MessageStatusChangedDto>(visitorHub, "MessageStatusChanged", c => c.Status == "Read");
        await seller.PostAsync($"/api/v1/messaging/conversations/{started.ConversationId}/read", null);
        Assert.Equal(2, (await read.Task.WaitAsync(EventTimeout)).MessageIds.Count);

        // Going offline is announced too.
        var sellerOffline = Expect<PresenceDto>(visitorHub, "PresenceChanged", p => p.UserId == sellerId && !p.Online);
        await sellerHub.StopAsync();
        await sellerOffline.Task.WaitAsync(EventTimeout);
    }

    [Fact]
    public async Task Hub_AcceptsOnlyRealtimeTokens_AndTheApiRejectsThem()
    {
        var (client, _) = await ListingApi.RegisterAsync(_factory);
        var sessionToken = client.DefaultRequestHeaders.Authorization!.Parameter!;
        var realtimeToken = (await (await client.PostAsync("/api/v1/messaging/realtime-token", null))
            .Content.ReadFromJsonAsync<RealtimeTokenDto>())!.Token;
        var server = _factory.Server;
        var withSessionToken = new HubConnectionBuilder()
            .WithUrl(new Uri(server.BaseAddress, "/hubs/messaging"), options =>
            {
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                options.AccessTokenProvider = () => Task.FromResult<string?>(sessionToken);
            })
            .Build();

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => withSessionToken.StartAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, error.StatusCode);

        var api = _factory.CreateClient();
        api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", realtimeToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.GetAsync("/api/v1/messaging/conversations")).StatusCode);
    }
}
