using FluentAssertions;
using NetArchTest.Rules;

namespace Dominodo.ArchitectureTests;

public sealed class LayerBoundaryTests
{
    private const string SharedKernel = "Dominodo.Shared.Kernel";
    private const string SharedAbstractions = "Dominodo.Shared.Abstractions";
    private const string SharedInfrastructure = "Dominodo.Shared.Infrastructure";

    [Fact]
    public void SharedKernel_ShouldNotDependOnInfrastructureOrApplication()
    {
        var result = Types.InAssembly(typeof(Shared.Kernel.Entity).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(SharedInfrastructure, SharedAbstractions)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void SharedAbstractions_ShouldNotDependOnInfrastructure()
    {
        var result = Types.InAssembly(typeof(Shared.Abstractions.IEmailSender).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(SharedInfrastructure)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_ShouldNotDependOnSharedInfrastructure()
    {
        var result = Types.InAssemblies(
            [
                typeof(Users.Application.DependencyInjection).Assembly,
                typeof(Admin.Application.DependencyInjection).Assembly,
                typeof(Tenants.Application.DependencyInjection).Assembly,
            ])
            .ShouldNot()
            .HaveDependencyOn(SharedInfrastructure)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void SharedApplication_ShouldNotDependOnInfrastructureOrAspNet()
    {
        var result = Types.InAssembly(typeof(Shared.Application.DependencyInjection).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(SharedInfrastructure, "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void ICurrentUser_ShouldResideInSharedKernel()
    {
        typeof(Shared.Kernel.ICurrentUser).Assembly.GetName().Name
            .Should().Be(SharedKernel);
    }

    [Fact]
    public void IResourceAccessAuthorizer_ShouldResideInSharedAbstractions()
    {
        typeof(Shared.Abstractions.IResourceAccessAuthorizer).Assembly.GetName().Name
            .Should().Be(SharedAbstractions);
    }

    [Fact]
    public void AuthorizationImplementations_ShouldResideOnlyInSharedInfrastructure()
    {
        var infrastructure = typeof(Shared.Infrastructure.DependencyInjection).Assembly;
        var kernel = typeof(Shared.Kernel.Entity).Assembly;
        var abstractions = typeof(Shared.Abstractions.IEmailSender).Assembly;

        Types.InAssembly(infrastructure).That()
            .ImplementInterface(typeof(Shared.Kernel.ICurrentUser))
            .GetTypes().Should().NotBeEmpty(because: "HttpCurrentUser lives in Shared.Infrastructure");
        Types.InAssembly(infrastructure).That()
            .ImplementInterface(typeof(Shared.Abstractions.IResourceAccessAuthorizer))
            .GetTypes().Should().NotBeEmpty(because: "ResourceAccessAuthorizer lives in Shared.Infrastructure");

        Types.InAssemblies([kernel, abstractions]).That()
            .ImplementInterface(typeof(Shared.Kernel.ICurrentUser))
            .GetTypes().Should().BeEmpty(because: "Kernel/Abstractions are interface-only — no implementations");
        Types.InAssemblies([kernel, abstractions]).That()
            .ImplementInterface(typeof(Shared.Abstractions.IResourceAccessAuthorizer))
            .GetTypes().Should().BeEmpty(because: "Kernel/Abstractions are interface-only — no implementations");
    }
}
