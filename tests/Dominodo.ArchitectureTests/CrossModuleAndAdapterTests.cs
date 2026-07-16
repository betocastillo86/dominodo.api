using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace Dominodo.ArchitectureTests;

public sealed class CrossModuleAndAdapterTests
{
    private static readonly Assembly UsersApplication = typeof(Users.Application.DependencyInjection).Assembly;
    private static readonly Assembly UsersDomain = typeof(Users.Domain.Users.User).Assembly;
    private static readonly Assembly UsersPersistence = typeof(Users.Persistence.DependencyInjection).Assembly;
    private static readonly Assembly AdminApplication = typeof(Admin.Application.DependencyInjection).Assembly;
    private static readonly Assembly AdminDomain = typeof(Admin.Domain.Notifications.NotificationDelivery).Assembly;
    private static readonly Assembly AdminPersistence = typeof(Admin.Persistence.DependencyInjection).Assembly;

    private static readonly string[] UsersNonContracts =
        ["Dominodo.Users.Domain", "Dominodo.Users.Application", "Dominodo.Users.Persistence"];
    private static readonly string[] AdminNonContracts =
        ["Dominodo.Admin.Domain", "Dominodo.Admin.Application", "Dominodo.Admin.Persistence"];
    private static readonly string[] Adapters =
        ["Dominodo.Adapters.Email", "Dominodo.Adapters.WhatsApp"];

    [Fact]
    public void Admin_MayOnlyReachUsers_ThroughItsContracts()
    {
        var result = Types.InAssemblies([AdminApplication, AdminDomain, AdminPersistence])
            .ShouldNot()
            .HaveDependencyOnAny(UsersNonContracts)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(because: string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Users_MayOnlyReachAdmin_ThroughItsContracts()
    {
        var result = Types.InAssemblies([UsersApplication, UsersDomain, UsersPersistence])
            .ShouldNot()
            .HaveDependencyOnAny(AdminNonContracts)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(because: string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Modules_ShouldNotDependOnConcreteAdapters()
    {
        var result = Types.InAssemblies(
                [UsersApplication, UsersDomain, UsersPersistence,
                 AdminApplication, AdminDomain, AdminPersistence])
            .ShouldNot()
            .HaveDependencyOnAny(Adapters)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(because: string.Join(", ", result.FailingTypeNames ?? []));
    }
}
