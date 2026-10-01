namespace Imova.Contracts.Listings;

// Who to contact about a listing — only on a listing's detail view. Name/Email are already
// resolved (a "Self" contact shows the publisher's own). Email and Phone are only for the owner or an
// admin (null for everyone else). The public gets the number's shape instead — PhonePrefix (its
// first digits, "+" kept) and PhoneHiddenDigits (how many follow) — and asks for the number itself
// with POST /api/v1/listings/{id}/contact/phone; both are null when there is no number or the owner
// hid it (visitors then only have platform messages).
// PersonType: Self | Other. MessagingApps: WhatsApp | Viber | Telegram.
// PreferredContactMethod: PhoneCall | PlatformMessages | Any. CallHoursFrom/To: "HH:mm", both or neither.
// PictureUrl: the person's photo (a Self contact: the publisher's own profile picture, else the
// agency's logo); null for an "Other" person. AgencyName: set whenever the listing is published by an
// agency — the contact (Self: the account behind it, i.e. the agent) works for that agency.
public record ListingContactDto(
    string PersonType,
    string? Name,
    string? Phone,
    string? Email,
    IReadOnlyList<string> MessagingApps,
    string PreferredContactMethod,
    bool HidePhoneNumber,
    string? CallHoursFrom,
    string? CallHoursTo,
    string? PictureUrl,
    string? AgencyName,
    string? PhonePrefix,
    int? PhoneHiddenDigits);
