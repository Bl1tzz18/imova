using Imova.Domain.Listings;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Listings;

public class ListingContactTests
{
    [Fact]
    public void Normalized_Self_DropsAnyNameAndEmail()
    {
        var contact = (TestContacts.Self with { Name = "Ignored", Email = "ignored@example.com" }).Normalized();

        Assert.Null(contact.Name);
        Assert.Null(contact.Email);
        Assert.Equal("+373 69 111 222", contact.Phone);
    }

    [Fact]
    public void Normalized_Other_KeepsTrimmedNameAndEmail_AndBlankEmailBecomesNull()
    {
        var contact = new ListingContact(ContactPersonType.Other, " +373 79 333 444 ", Name: "  Maria ", Email: "  ").Normalized();

        Assert.Equal("Maria", contact.Name);
        Assert.Null(contact.Email);
        Assert.Equal("+373 79 333 444", contact.Phone);
    }

    [Fact]
    public void Normalized_HiddenPhone_ClearsMessagingAppsAndCallHours()
    {
        var contact = (TestContacts.HiddenPhone with
        {
            MessagingApps = [ContactMessagingApp.WhatsApp],
            CallHoursFrom = "09:00",
            CallHoursTo = "18:00",
        }).Normalized();

        Assert.Empty(contact.MessagingApps!);
        Assert.Null(contact.CallHoursFrom);
        Assert.Null(contact.CallHoursTo);
    }

    [Fact]
    public void Normalized_KeepsCallHoursOnlyWhileCallsAreAllowed()
    {
        var calls = TestContacts.Self with
        {
            PreferredContactMethod = PreferredContactMethod.PhoneCall, CallHoursFrom = "09:00", CallHoursTo = "18:00",
        };
        var messagesOnly = calls with { PreferredContactMethod = PreferredContactMethod.PlatformMessages };

        Assert.Equal(("09:00", "18:00"), (calls.Normalized().CallHoursFrom, calls.Normalized().CallHoursTo));
        Assert.Null(messagesOnly.Normalized().CallHoursFrom);
        Assert.Null(messagesOnly.Normalized().CallHoursTo);
    }

    [Fact]
    public void CallHourSlots_AreEveryHalfHourFrom0600To2300()
    {
        Assert.Equal(35, ListingContact.CallHourSlots.Count);
        Assert.Equal("06:00", ListingContact.CallHourSlots[0]);
        Assert.Equal("06:30", ListingContact.CallHourSlots[1]);
        Assert.Equal("23:00", ListingContact.CallHourSlots[^1]);
    }

    [Theory]
    [InlineData(null, null, true)]
    [InlineData("09:00", "18:00", true)]
    [InlineData("06:00", "06:30", true)]
    [InlineData("09:00", null, false)]
    [InlineData(null, "18:00", false)]
    [InlineData("18:00", "09:00", false)]
    [InlineData("09:00", "09:00", false)]
    [InlineData("09:15", "18:00", false)]
    [InlineData("05:30", "18:00", false)]
    [InlineData("9:00", "18:00", false)]
    public void IsValidCallHourRange(string? from, string? to, bool expected)
    {
        Assert.Equal(expected, ListingContact.IsValidCallHourRange(from, to));
    }

    [Fact]
    public void Normalized_DeduplicatesMessagingApps()
    {
        var contact = (TestContacts.Self with
        {
            MessagingApps = [ContactMessagingApp.Telegram, ContactMessagingApp.WhatsApp, ContactMessagingApp.Telegram],
        }).Normalized();

        Assert.Equal([ContactMessagingApp.WhatsApp, ContactMessagingApp.Telegram], contact.MessagingApps);
    }

    [Theory]
    [InlineData(PreferredContactMethod.PhoneCall)]
    [InlineData(PreferredContactMethod.Any)]
    public void Listing_WithAHiddenPhoneButCallsPreferred_IsRejected(PreferredContactMethod method)
    {
        Assert.Throws<ArgumentException>(() =>
            ListingTestData.NewListing(contact: TestContacts.HiddenPhone with { PreferredContactMethod = method }));
    }

    [Fact]
    public void Listing_WithAnotherPersonWithoutAName_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => ListingTestData.NewListing(contact: TestContacts.Other with { Name = " " }));
    }

    [Fact]
    public void Listing_StoresTheNormalizedContact_AndUpdateDetailsReplacesIt()
    {
        var listing = ListingTestData.NewListing(contact: TestContacts.Self with { Name = "Ignored" });
        Assert.Null(listing.Contact!.Name);

        listing.UpdateDetails(
            listing.TransactionType, listing.Title, listing.Description, listing.Price, null, listing.RentalDetails,
            TestContacts.Other);

        Assert.Equal(ContactPersonType.Other, listing.Contact!.PersonType);
        Assert.Equal("Maria Popescu", listing.Contact.Name);
    }
}
