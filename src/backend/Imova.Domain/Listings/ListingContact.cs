namespace Imova.Domain.Listings;

// Who visitors should contact about a listing, and how. Lives on Listing (not Publisher): the same
// person may want a different number, apps or hours per offer.
//
// Self = the listing's publisher: only the phone is stored (it may differ from the account's), the
// name and email are the publisher's own, read at display time so they follow profile updates.
// Other = someone else entirely, with their own name/phone/email.
public sealed record ListingContact(
    ContactPersonType PersonType,
    string Phone,
    string? Name = null,
    string? Email = null,
    IReadOnlyList<ContactMessagingApp>? MessagingApps = null,
    PreferredContactMethod PreferredContactMethod = PreferredContactMethod.Any,
    // Keeps the phone off the public listing — visitors can only use in-platform messages, which
    // is why this requires PreferredContactMethod.PlatformMessages (see EnsureValid).
    bool HidePhoneNumber = false,
    // When calls are welcome, e.g. 09:00–18:00: "HH:mm" on the half hour (see IsCallHourSlot), set
    // together or not at all, From before To. Only kept while calls are allowed (see AllowsCalls).
    string? CallHoursFrom = null,
    string? CallHoursTo = null)
{
    // The slots the listing form offers: every half hour from 06:00 to 23:00.
    public static readonly IReadOnlyList<string> CallHourSlots = Enumerable.Range(12, 35)
        .Select(halfHours => $"{halfHours / 2:00}:{(halfHours % 2) * 30:00}")
        .ToArray();

    public static bool IsCallHourSlot(string? value) => value is not null && CallHourSlots.Contains(value);

    // Slots are zero-padded "HH:mm", so ordinal order is time order.
    public static bool IsValidCallHourRange(string? from, string? to) =>
        (from is null && to is null)
        || (IsCallHourSlot(from) && IsCallHourSlot(to) && string.CompareOrdinal(from, to) < 0);

    // A method, not a property, so it isn't serialized into the stored JSON.
    public bool AllowsCalls() => !HidePhoneNumber && PreferredContactMethod != PreferredContactMethod.PlatformMessages;

    // Drops what doesn't apply instead of rejecting it: Self has no name/email of its own, apps and
    // call hours mean nothing for a hidden number (call hours nothing without calls either).
    public ListingContact Normalized() =>
        this with
        {
            Phone = Phone.Trim(),
            Name = PersonType == ContactPersonType.Other ? Blank(Name) : null,
            Email = PersonType == ContactPersonType.Other ? Blank(Email) : null,
            MessagingApps = HidePhoneNumber ? [] : (MessagingApps ?? []).Distinct().Order().ToArray(),
            CallHoursFrom = AllowsCalls() ? Blank(CallHoursFrom) : null,
            CallHoursTo = AllowsCalls() ? Blank(CallHoursTo) : null,
        };

    public static void EnsureValid(ListingContact contact)
    {
        if (!Enum.IsDefined(contact.PersonType))
        {
            throw new ArgumentOutOfRangeException(nameof(contact), contact.PersonType, "Unknown contact person type.");
        }

        if (string.IsNullOrWhiteSpace(contact.Phone))
        {
            throw new ArgumentException("A contact phone number is required.", nameof(contact));
        }

        if (contact.PersonType == ContactPersonType.Other && string.IsNullOrWhiteSpace(contact.Name))
        {
            throw new ArgumentException("A contact name is required for another person.", nameof(contact));
        }

        if (contact.HidePhoneNumber && contact.PreferredContactMethod != PreferredContactMethod.PlatformMessages)
        {
            throw new ArgumentException(
                "A hidden phone number can only be contacted through platform messages.", nameof(contact));
        }
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public enum ContactPersonType
{
    Self = 1,
    Other = 2,
}

// Which messaging apps work on the contact phone number.
public enum ContactMessagingApp
{
    WhatsApp = 1,
    Viber = 2,
    Telegram = 3,
}

public enum PreferredContactMethod
{
    PhoneCall = 1,
    PlatformMessages = 2,
    Any = 3,
}
