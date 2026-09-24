using Imova.Domain.Publishers;

namespace Imova.UnitTests.Publishers;

public class PublisherTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void CreateIndividual_SetsFieldsAndHasNoAgencyOnlyFields()
    {
        var publisher = Publisher.CreateIndividual(UserId, "Ion Popescu", null, "ion@example.com");

        Assert.Equal(UserId, publisher.UserId);
        Assert.Equal(PublisherType.Individual, publisher.PublisherType);
        Assert.Equal("Ion Popescu", publisher.DisplayName);
        Assert.Null(publisher.Phone);
        Assert.Equal("ion@example.com", publisher.Email);
        Assert.Null(publisher.LogoUrl);
        Assert.Null(publisher.Bio);
    }

    [Fact]
    public void CreateAgency_SetsAgencyFields()
    {
        var publisher = Publisher.CreateAgency(
            UserId, "Imobil Grup", "+373 22 000 000", "office@imobil.md", "https://imobil.md/logo.png", "Agenție din 2005.");

        Assert.Equal(PublisherType.Agency, publisher.PublisherType);
        Assert.Equal("https://imobil.md/logo.png", publisher.LogoUrl);
        Assert.Equal("Agenție din 2005.", publisher.Bio);
    }

    [Fact]
    public void CreateAgency_WithoutPhone_Throws()
    {
        Assert.Throws<ArgumentException>(() => Publisher.CreateAgency(UserId, "Agenție", " ", "a@b.md", null, null));
    }

    [Theory]
    [InlineData("", "ion@example.com")]
    [InlineData("Ion", "")]
    public void CreateIndividual_WithMissingNameOrEmail_Throws(string displayName, string email)
    {
        Assert.Throws<ArgumentException>(() => Publisher.CreateIndividual(UserId, displayName, null, email));
    }

    [Fact]
    public void CreateIndividual_WithEmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() => Publisher.CreateIndividual(Guid.Empty, "Ion", null, "ion@example.com"));
    }

    [Fact]
    public void UpdateContactDetails_UpdatesNamePhoneAndEmail()
    {
        var publisher = Publisher.CreateIndividual(UserId, "Ion", null, "ion@example.com");

        publisher.UpdateContactDetails("Ion P.", "+373 69 123 456", "ion.p@example.com");

        Assert.Equal("Ion P.", publisher.DisplayName);
        Assert.Equal("+373 69 123 456", publisher.Phone);
        Assert.Equal("ion.p@example.com", publisher.Email);
    }

    [Fact]
    public void UpdateContactDetails_ClearingAnAgencysPhone_Throws()
    {
        var publisher = Publisher.CreateAgency(UserId, "Agenție", "+373 22 000 000", "a@b.md", null, null);

        Assert.Throws<ArgumentException>(() => publisher.UpdateContactDetails("Agenție", null, "a@b.md"));
    }
}
