using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;

namespace Dominodo.ArchitectureTests;

public sealed class ApiLayerTests
{
    private static readonly Assembly UsersApplication = typeof(Users.Application.DependencyInjection).Assembly;
    private static readonly Assembly AdminApplication = typeof(Admin.Application.DependencyInjection).Assembly;
    private static readonly Assembly UsersApi = typeof(Users.Api.IUsersApiMarker).Assembly;
    private static readonly Assembly AdminApi = typeof(Admin.Api.IAdminApiMarker).Assembly;

    [Fact]
    public void Controllers_ShouldOnlyLiveInApiProjects()
    {
        var result = Types.InAssemblies([UsersApplication, AdminApplication])
            .That()
            .Inherit(typeof(ControllerBase))
            .GetTypes();

        result.Should().BeEmpty(
            because: "controllers must live in the module's *.Api project, not *.Application");
    }

    [Fact]
    public void Api_ShouldNotDependOnPersistence()
    {
        var result = Types.InAssemblies([UsersApi, AdminApi])
            .ShouldNot()
            .HaveDependencyOnAny(
                "Dominodo.Users.Persistence",
                "Dominodo.Admin.Persistence")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: string.Join(", ", result.FailingTypeNames ?? []));
    }
}
