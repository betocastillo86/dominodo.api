using System.Reflection;
using FluentAssertions;
using MediatR;
using NetArchTest.Rules;

namespace Dominodo.ArchitectureTests;

public sealed class OperationsModuleBoundaryTests
{
    private static readonly Assembly DomainAssembly = typeof(Operations.Domain.Requests.Request).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Operations.Application.DependencyInjection).Assembly;
    private static readonly Assembly ContractsAssembly = typeof(Operations.Contracts.IOperationsModuleApi).Assembly;

    [Fact]
    public void Domain_ShouldNotDependOnApplicationOrPersistence()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Dominodo.Operations.Application", "Dominodo.Operations.Persistence")
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
            .HaveDependencyOn("Dominodo.Operations.Persistence")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(because: string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Contracts_ShouldNotDependOnDomainApplicationOrPersistence()
    {
        var result = Types.InAssembly(ContractsAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Dominodo.Operations.Domain",
                "Dominodo.Operations.Application",
                "Dominodo.Operations.Persistence")
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
