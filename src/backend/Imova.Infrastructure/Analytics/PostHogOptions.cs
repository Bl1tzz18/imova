namespace Imova.Infrastructure.Analytics;

// "PostHog" section: what the API needs to delete a person from product analytics when an account
// is deleted. Both values unset (local, CI) = nothing to delete (NoAnalyticsErasure). The personal
// API key needs only the person:write scope on this one project; it lives in Key Vault.
public sealed class PostHogOptions
{
    public const string SectionName = "PostHog";

    // The EU cloud's app host (the API lives there, not on the ingestion host).
    public string Host { get; init; } = "https://eu.posthog.com";

    public string ProjectId { get; init; } = "";

    public string PersonalApiKey { get; init; } = "";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ProjectId) && !string.IsNullOrWhiteSpace(PersonalApiKey);
}
