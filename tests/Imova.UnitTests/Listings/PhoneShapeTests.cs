using Imova.Application.Features.Listings;

namespace Imova.UnitTests.Listings;

public class PhoneShapeTests
{
    [Theory]
    [InlineData("+373 22 555 010", "+373225", 5)]
    [InlineData("+37369123456", "+373691", 5)]
    [InlineData("069 123 456", "0691", 5)]
    public void ShowsTheFirstDigits_AndHowManyFollow(string phone, string prefix, int hidden)
    {
        Assert.Equal((prefix, hidden), PhoneShape.For(phone));
    }

    [Fact]
    public void AShortNumber_StillShowsThreeDigits()
    {
        Assert.Equal(("123", 4), PhoneShape.For("1234567"));
        Assert.Equal(("12", 0), PhoneShape.For("12"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("abc")]
    public void NoDigits_NoShape(string? phone)
    {
        Assert.Null(PhoneShape.For(phone));
    }
}
