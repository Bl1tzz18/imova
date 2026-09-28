namespace Imova.Contracts.SavedSearches;

// QueryString is the /search page's query string (open it as /search?{QueryString}).
// AlertFrequency: "Off" | "Daily" | "Instant". NewListingsCount: matches published since the user
// last opened this search.
public record SavedSearchDto(
    Guid Id,
    string Name,
    string QueryString,
    string AlertFrequency,
    int NewListingsCount,
    DateTimeOffset CreatedAt);

public record SavedSearchUnsubscribedDto(string Name);
