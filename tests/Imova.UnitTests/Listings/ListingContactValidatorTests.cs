using Imova.Application.Features.Listings;
using Imova.Domain.Listings;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Listings;

public class ListingContactValidatorTests
{
    private readonly ListingContactValidator _validator = new();

    private List<string> ErrorsFor(ListingContact contact) =>
        _validator.Validate(contact).Errors.Select(e => e.PropertyName).Distinct().Order().ToList();

    [Fact]
    public void Self_OnlyNeedsAPhone()
    {
        Assert.Empty(ErrorsFor(new ListingContact(ContactPersonType.Self, "069123456")));
    }

    [Fact]
    public void Other_WithNamePhoneAndNoEmail_IsValid()
    {
        Assert.Empty(ErrorsFor(TestContacts.Other with { Email = null }));
    }

    [Fact]
    public void Other_RequiresAName()
    {
        Assert.Equal(["Name"], ErrorsFor(TestContacts.Other with { Name = null }));
    }

    [Theory]
    [InlineData(ContactPersonType.Self)]
    [InlineData(ContactPersonType.Other)]
    public void Phone_IsRequiredEitherWay(ContactPersonType personType)
    {
        var contact = TestContacts.Other with { PersonType = personType, Phone = "" };

        Assert.Contains("Phone", ErrorsFor(contact));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("+373 69 123 456 789 000 111")]
    public void Phone_MustBeAValidNumber(string phone)
    {
        Assert.Equal(["Phone"], ErrorsFor(TestContacts.Self with { Phone = phone }));
    }

    [Theory]
    [InlineData("+373 69 123 456")]
    [InlineData("069 123 456")]
    [InlineData("+40 721 123 456")]
    public void Phone_AcceptsMoldovanAndInternationalNumbers(string phone)
    {
        Assert.Empty(ErrorsFor(TestContacts.Self with { Phone = phone }));
    }

    [Fact]
    public void Phone_Null_IsReportedInsteadOfCrashing()
    {
        Assert.Contains("Phone", ErrorsFor(TestContacts.Self with { Phone = null! }));
    }

    [Fact]
    public void Other_WithoutAValidPhone_AlsoNeedsAnEmail()
    {
        Assert.Equal(["Email", "Phone"], ErrorsFor(TestContacts.Other with { Phone = "", Email = null }));
        Assert.Equal(["Phone"], ErrorsFor(TestContacts.Other with { Phone = "", Email = "maria@example.com" }));
    }

    [Fact]
    public void Email_WhenGiven_MustBeAnEmail()
    {
        Assert.Equal(["Email"], ErrorsFor(TestContacts.Other with { Email = "not-an-email" }));
    }

    [Theory]
    [InlineData(PreferredContactMethod.PhoneCall)]
    [InlineData(PreferredContactMethod.Any)]
    public void HiddenPhone_ForcesPlatformMessages(PreferredContactMethod method)
    {
        var errors = _validator.Validate(TestContacts.HiddenPhone with { PreferredContactMethod = method }).Errors;

        var error = Assert.Single(errors);
        Assert.Equal("PreferredContactMethod", error.PropertyName);
        Assert.Equal("A hidden phone number can only be contacted through platform messages.", error.ErrorMessage);
    }

    [Fact]
    public void HiddenPhone_WithPlatformMessages_IsValid_EvenWithAppsThatGetDropped()
    {
        Assert.Empty(ErrorsFor(TestContacts.HiddenPhone with { MessagingApps = [ContactMessagingApp.WhatsApp] }));
    }

    [Fact]
    public void UnknownEnums_AreRejected()
    {
        Assert.Equal(
            ["MessagingApps[0]", "PersonType", "PreferredContactMethod"],
            ErrorsFor(TestContacts.Self with
            {
                PersonType = (ContactPersonType)9,
                MessagingApps = [(ContactMessagingApp)9],
                PreferredContactMethod = (PreferredContactMethod)9,
            }));
    }

    [Theory]
    [InlineData("09:00", "18:00")]
    [InlineData(null, null)]
    [InlineData(" ", "")]
    public void CallHours_ValidRangeOrNone_AreAccepted(string? from, string? to)
    {
        Assert.Empty(ErrorsFor(TestContacts.Self with { CallHoursFrom = from, CallHoursTo = to }));
    }

    [Theory]
    [InlineData("09:00", null)]
    [InlineData("18:00", "09:00")]
    [InlineData("anytime", "18:00")]
    [InlineData("09:10", "18:00")]
    public void CallHours_OutsideTheOfferedSlots_AreRejected(string? from, string? to)
    {
        Assert.Equal(["CallHoursFrom"], ErrorsFor(TestContacts.Self with { CallHoursFrom = from, CallHoursTo = to }));
    }

    [Fact]
    public void CallHours_AreNotCheckedWhenCallsArentAllowed()
    {
        Assert.Empty(ErrorsFor(TestContacts.HiddenPhone with { CallHoursFrom = "garbage", CallHoursTo = null }));
    }
}
