namespace Imova.Contracts.Publishers;

// Phone/Email are null wherever contact details aren't meant to be shown (listing cards/search
// results) — they're only populated on a listing's detail view and on the publisher's own
// "my publishers" view.
public record PublisherDto(
    Guid Id,
    Guid UserId,
    string PublisherType,
    string DisplayName,
    string? Phone,
    string? Email,
    string? LogoUrl,
    string? Bio);
