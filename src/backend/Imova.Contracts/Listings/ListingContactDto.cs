namespace Imova.Contracts.Listings;

// Who to contact about a listing — only on a listing's detail view. Name/Email are already
// resolved (a "Self" contact shows the publisher's own). Phone is null when the owner hid it and
// the viewer isn't the owner or an admin; visitors then only have platform messages.
// PersonType: Self | Other. MessagingApps: WhatsApp | Viber | Telegram.
// PreferredContactMethod: PhoneCall | PlatformMessages | Any. CallHoursFrom/To: "HH:mm", both or neither.
public record ListingContactDto(
    string PersonType,
    string? Name,
    string? Phone,
    string? Email,
    IReadOnlyList<string> MessagingApps,
    string PreferredContactMethod,
    bool HidePhoneNumber,
    string? CallHoursFrom,
    string? CallHoursTo);
