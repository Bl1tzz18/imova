namespace Imova.Contracts.Publishers;

// A listing's author. Phone/Email are null wherever contact details aren't meant to be shown
// (listing cards/search results) — they're only populated on the author's own "my publishers" view.
public record PublisherDto(
    Guid Id,
    Guid UserId,
    string DisplayName,
    string? Phone,
    string? Email);
