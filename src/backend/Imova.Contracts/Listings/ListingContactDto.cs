namespace Imova.Contracts.Listings;

// Who to contact about a listing — only on a listing's detail view. Name/Email are already
// resolved (a "Self" contact shows the publisher's own). Email is only for the owner or an admin
// (null for everyone else). Phone is null when the owner hid it and the viewer isn't the owner or an
// admin; visitors then only have platform messages.
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
    string? AgencyName);
