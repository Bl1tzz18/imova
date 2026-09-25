using Imova.Domain.Listings;

namespace Imova.UnitTests.TestSupport;

// Valid ListingContacts for tests that need one but aren't about contact details.
public static class TestContacts
{
    public static readonly ListingContact Self = new(ContactPersonType.Self, "+373 69 111 222");

    public static readonly ListingContact Other = new(
        ContactPersonType.Other, "+373 79 333 444", Name: "Maria Popescu", Email: "maria@example.com");

    public static readonly ListingContact HiddenPhone = new(
        ContactPersonType.Self, "+373 69 555 666",
        PreferredContactMethod: PreferredContactMethod.PlatformMessages, HidePhoneNumber: true);
}
