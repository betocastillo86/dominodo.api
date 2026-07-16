using System.Reflection;
using FluentAssertions;
using MediatR;
using NetArchTest.Rules;

namespace Dominodo.ArchitectureTests;

public sealed class AdminModuleBoundaryTests
{
    private static readonly Assembly DomainAssembly = typeof(Admin.Domain.Notifications.NotificationDelivery).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Admin.Application.DependencyInjection).Assembly;
    private static readonly Assembly ContractsAssembly = typeof(Admin.Contracts.IAdminContractsMarker).Assembly;

    [Fact]
    public void Domain_ShouldNotDependOnApplicationOrPersistence()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Dominodo.Admin.Application", "Dominodo.Admin.Persistence")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(because: string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_ShouldNotDependOnPersistence()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("Dominodo.Admin.Persistence")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(because: string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Contracts_ShouldNotDependOnDomainApplicationOrPersistence()
    {
        var result = Types.InAssembly(ContractsAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Dominodo.Admin.Domain", "Dominodo.Admin.Application", "Dominodo.Admin.Persistence")
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
