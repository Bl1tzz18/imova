using FluentValidation;
using Imova.Application.Common.Identity;
using Imova.Application.Features.Publishers.CreateAgencyPublisher;
using Imova.Domain.Publishers;
using Imova.Infrastructure;
using Imova.UnitTests.TestSupport;

namespace Imova.UnitTests.Publishers;

public class CreateAgencyPublisherTests
{
    private readonly ImovaDbContext _dbContext = TestDbContextFactory.Create();
    private readonly ApplicationUser _user = new() { Id = Guid.NewGuid(), Email = "ana@example.com", UserName = "ana@example.com" };

    public CreateAgencyPublisherTests()
    {
        _dbContext.Users.Add(_user);
        _dbContext.SaveChanges();
    }

    private CreateAgencyPublisherCommand Command(string? email = null, string? logoUrl = null) =>
        new(_user.Id, "Imobil Grup", "+373 22 000 000", email, logoUrl, "Agenție imobiliară.");

    [Fact]
    public async Task Handle_CreatesAnAgencyPublisherLinkedToTheSameUser()
    {
        var dto = await new CreateAgencyPublisherHandler(_dbContext).Handle(Command(), CancellationToken.None);

        var publisher = Assert.Single(_dbContext.Publishers);
        Assert.Equal(_user.Id, publisher.UserId);
        Assert.Equal(PublisherType.Agency, publisher.PublisherType);
        Assert.Equal("Agency", dto.PublisherType);
        Assert.Equal("Agenție imobiliară.", dto.Bio);
    }

    [Fact]
    public async Task Handle_WithoutEmail_DefaultsToTheAccountEmail()
    {
        var dto = await new CreateAgencyPublisherHandler(_dbContext).Handle(Command(), CancellationToken.None);

        Assert.Equal("ana@example.com", dto.Email);
    }

    [Fact]
    public async Task Handle_WithEmail_UsesIt()
    {
        var dto = await new CreateAgencyPublisherHandler(_dbContext).Handle(Command(email: "office@imobil.md"), CancellationToken.None);

        Assert.Equal("office@imobil.md", dto.Email);
    }

    [Fact]
    public async Task Handle_CoexistsWithTheUsersIndividualPublisher()
    {
        ListingTestData.AddIndividualPublisher(_dbContext, _user.Id);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        await new CreateAgencyPublisherHandler(_dbContext).Handle(Command(), CancellationToken.None);

        Assert.Equal(2, _dbContext.Publishers.Count(p => p.UserId == _user.Id));
    }

    [Fact]
    public async Task Handle_WhenTheUserAlreadyHasAnAgency_ThrowsValidationException()
    {
        ListingTestData.AddAgencyPublisher(_dbContext, _user.Id);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        await Assert.ThrowsAsync<ValidationException>(() =>
            new CreateAgencyPublisherHandler(_dbContext).Handle(Command(), CancellationToken.None));
    }

    [Fact]
    public void Validator_AcceptsAValidCommand()
    {
        Assert.True(new CreateAgencyPublisherValidator().Validate(Command(logoUrl: "https://imobil.md/logo.png")).IsValid);
    }

    [Theory]
    [InlineData("", "+373 22 000 000", null, null)]
    [InlineData("Agenție", "", null, null)]
    [InlineData("Agenție", "abc", null, null)]
    [InlineData("Agenție", "+373 22 000 000", "not-an-email", null)]
    [InlineData("Agenție", "+373 22 000 000", null, "ftp://imobil.md/logo.png")]
    [InlineData("Agenție", "+373 22 000 000", null, "logo.png")]
    public void Validator_RejectsInvalidInput(string displayName, string phone, string? email, string? logoUrl)
    {
        var result = new CreateAgencyPublisherValidator().Validate(
            new CreateAgencyPublisherCommand(Guid.NewGuid(), displayName, phone, email, logoUrl, null));

        Assert.False(result.IsValid);
    }
}
