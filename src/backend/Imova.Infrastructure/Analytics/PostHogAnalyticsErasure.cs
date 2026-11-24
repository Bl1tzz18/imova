using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Imova.Application.Common.Interfaces;

namespace Imova.Infrastructure.Analytics;

// Deletes the PostHog person whose distinct id is the account id, with their events and recordings.
// Only visitors who accepted analytics cookies were ever identified, so most accounts have no
// person there — that's not an error. PostHog finishes the deletion in the background.
public sealed class PostHogAnalyticsErasure(HttpClient client, PostHogOptions options) : IAnalyticsErasure
{
    public async Task EraseUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var project = Uri.EscapeDataString(options.ProjectId.Trim());
        var found = await client.GetFromJsonAsync<PersonList>(
            $"api/projects/{project}/persons/?distinct_id={userId}", cancellationToken);

        foreach (var person in found?.Results ?? [])
        {
            using var response = await client.DeleteAsync(
                $"api/projects/{project}/persons/{Uri.EscapeDataString(person.Id)}/?delete_events=true&delete_recordings=true",
                cancellationToken);
            // Already gone (a retry, or deleted in PostHog by hand) is fine.
            if (response.StatusCode != HttpStatusCode.NotFound)
            {
                response.EnsureSuccessStatusCode();
            }
        }
    }

    private sealed record PersonList([property: JsonPropertyName("results")] IReadOnlyList<Person>? Results);

    private sealed record Person([property: JsonPropertyName("id")] string Id);
}

// Where PostHog isn't configured: nothing was ever sent, so there's nothing to delete.
public sealed class NoAnalyticsErasure : IAnalyticsErasure
{
    public Task EraseUserAsync(Guid userId, CancellationToken cancellationToken) => Task.CompletedTask;
}
