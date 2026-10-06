using Imova.Application.Common;
using Imova.Application.Features.Agencies.CreateAgency;
using Imova.Application.Features.Agencies.UpdateAgency;

namespace Imova.UnitTests.Agencies;

public class AgencyProfileValidatorTests
{
    private static CreateAgencyCommand Create(
        string name = "Casa Ta",
        string phone = "+373 22 555 010",
        string? email = null,
        string? website = null,
        string? bio = null) =>
        new(Guid.NewGuid(), name, phone, email, bio, website, null, null);

    private static UpdateAgencyCommand Update(string? email) =>
        new(Guid.NewGuid(), Guid.NewGuid(), false, "Casa Ta", "+373 22 555 010", email, null, null, null, null);

    [Fact]
    public void Create_WithTheRequiredFieldsOnly_IsValid()
    {
        Assert.True(new CreateAgencyValidator().Validate(Create()).IsValid);
    }

    [Theory]
    [InlineData("https://agentia.md")]
    [InlineData("http://www.agentia.md/despre")]
    public void Website_AnHttpAddress_IsValid(string website)
    {
        Assert.True(new CreateAgencyValidator().Validate(Create(website: website)).IsValid);
    }

    [Theory]
    [InlineData("agentia.md")]
    [InlineData("ftp://agentia.md")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://localhost")]
    public void Website_AnythingElse_IsRefusedWithItsCode(string website)
    {
        var error = Assert.Single(new CreateAgencyValidator().Validate(Create(website: website)).Errors);

        Assert.Equal(ErrorCodes.AgencyWebsiteInvalid, error.ErrorCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("12")]
    public void Phone_IsRequiredAndChecked(string phone)
    {
        Assert.Contains(new CreateAgencyValidator().Validate(Create(phone: phone)).Errors, e => e.PropertyName == "Phone");
    }

    [Fact]
    public void Name_IsRequired_AndLimited()
    {
        Assert.False(new CreateAgencyValidator().Validate(Create(name: "")).IsValid);
        Assert.False(new CreateAgencyValidator().Validate(Create(name: new string('a', 121))).IsValid);
    }

    [Fact]
    public void Bio_IsLimitedTo2000()
    {
        Assert.False(new CreateAgencyValidator().Validate(Create(bio: new string('a', 2001))).IsValid);
    }

    [Fact]
    public void Email_IsOptionalOnCreate_ButMustBeAnEmail()
    {
        Assert.True(new CreateAgencyValidator().Validate(Create(email: null)).IsValid);
        Assert.False(new CreateAgencyValidator().Validate(Create(email: "not-an-email")).IsValid);
    }

    [Fact]
    public void Email_IsRequiredOnUpdate()
    {
        Assert.Contains(new UpdateAgencyValidator().Validate(Update(email: null)).Errors, e => e.PropertyName == "Email");
        Assert.True(new UpdateAgencyValidator().Validate(Update(email: "office@casata.md")).IsValid);
    }
}
