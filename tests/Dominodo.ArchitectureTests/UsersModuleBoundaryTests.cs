using System.Reflection;
using FluentAssertions;
using MediatR;
using NetArchTest.Rules;

namespace Dominodo.ArchitectureTests;

public sealed class UsersModuleBoundaryTests
{
    private static readonly Assembly DomainAssembly = typeof(Users.Domain.Users.User).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Users.Application.DependencyInjection).Assembly;
    private static readonly Assembly ContractsAssembly = typeof(Users.Contracts.IUsersModuleApi).Assembly;

    [Fact]
    public void Domain_ShouldNotDependOnApplicationOrPersistence()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Dominodo.Users.Application",
                "Dominodo.Users.Persistence")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: string.Join(", ", result.FailingTypeNames ?? []));
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

        result.IsSuccessful.Should().BeTrue(
            because: string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_ShouldNotDependOnPersistence()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("Dominodo.Users.Persistence")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Contracts_ShouldNotDependOnDomainApplicationOrPersistence()
    {
        var result = Types.InAssembly(ContractsAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Dominodo.Users.Domain",
                "Dominodo.Users.Application",
                "Dominodo.Users.Persistence")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Handlers_ShouldBeInternal()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IRequestHandler<,>))
            .Should()
            .NotBePublic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: string.Join(", ", result.FailingTypeNames ?? []));
    }
}
