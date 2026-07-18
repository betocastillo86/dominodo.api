using System.Reflection;
using FluentAssertions;
using MediatR;
using NetArchTest.Rules;

namespace Dominodo.ArchitectureTests;

public sealed class TenantsModuleBoundaryTests
{
    private static readonly Assembly DomainAssembly = typeof(Tenants.Domain.Tenants.Tenant).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Tenants.Application.DependencyInjection).Assembly;
    private static readonly Assembly ContractsAssembly = typeof(Tenants.Contracts.ITenantsModuleApi).Assembly;

    [Fact]
    public void Domain_ShouldNotDependOnApplicationOrPersistence()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Dominodo.Tenants.Application", "Dominodo.Tenants.Persistence")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(because: string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_ShouldOnlyDependOnSharedKernel()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Dominodo.Shared.Infrastructure",
                "Microsoft.EntityFrameworkCore",
                "MediatR.Wrappers")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(because: string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_ShouldNotDependOnPersistence()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("Dominodo.Tenants.Persistence")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(because: string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Contracts_ShouldNotDependOnDomainApplicationOrPersistence()
    {
        var result = Types.InAssembly(ContractsAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Dominodo.Tenants.Domain",
                "Dominodo.Tenants.Application",
                "Dominodo.Tenants.Persistence")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(because: string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Handlers_ShouldBeInternal()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That().ImplementInterface(typeof(IRequestHandler<,>))
            .Should().NotBePublic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(because: string.Join(", ", result.FailingTypeNames ?? []));
    }
}
