using NetArchTest.Rules;

namespace Imova.ArchitectureTests;

public class LayeringTests
{
    private const string DomainNamespace = "Imova.Domain";
    private const string ContractsNamespace = "Imova.Contracts";
    private const string ApplicationNamespace = "Imova.Application";
    private const string InfrastructureNamespace = "Imova.Infrastructure";
    private const string ApiNamespace = "Imova.Api";

    [Fact]
    public void Domain_Should_Not_DependOn_OtherLayers()
    {
        var result = Types.InAssembly(typeof(Domain.Properties.Property).Assembly)
            .Should()
            .NotHaveDependencyOnAny(ContractsNamespace, ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Contracts_Should_Not_DependOn_OtherLayers()
    {
        var result = Types.InAssembly(typeof(Contracts.Properties.PropertyDto).Assembly)
            .Should()
            .NotHaveDependencyOnAny(DomainNamespace, ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_Should_Not_DependOn_Api()
    {
        var result = Types.InAssembly(typeof(Application.Features.Listings.ListingMapping).Assembly)
            .Should()
            .NotHaveDependencyOn(ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_Should_Not_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Application.Features.Listings.ListingMapping).Assembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Infrastructure_Should_Not_DependOn_Api()
    {
        var result = Types.InAssembly(typeof(Infrastructure.ImovaDbContext).Assembly)
            .Should()
            .NotHaveDependencyOn(ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }
}
