using Imova.Domain.Publishers;

namespace Imova.UnitTests.Publishers;

public class PublisherTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void CreateIndividual_SetsFields()
    {
        var publisher = Publisher.CreateIndividual(UserId, "Ion Popescu", null, "ion@example.com");

        Assert.Equal(UserId, publisher.UserId);
        Assert.Equal("Ion Popescu", publisher.DisplayName);
        Assert.Null(publisher.Phone);
        Assert.Equal("ion@example.com", publisher.Email);
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
}
