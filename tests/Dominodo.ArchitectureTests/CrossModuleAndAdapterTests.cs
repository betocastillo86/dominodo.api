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
    private static readonly Assembly TenantsApplication = typeof(Tenants.Application.DependencyInjection).Assembly;
    private static readonly Assembly TenantsDomain = typeof(Tenants.Domain.Tenants.Tenant).Assembly;
    private static readonly Assembly TenantsPersistence = typeof(Tenants.Persistence.DependencyInjection).Assembly;
    private static readonly Assembly OperationsApplication = typeof(Operations.Application.DependencyInjection).Assembly;
    private static readonly Assembly OperationsDomain = typeof(Operations.Domain.Requests.Request).Assembly;
    private static readonly Assembly OperationsPersistence = typeof(Operations.Persistence.DependencyInjection).Assembly;

    private static readonly string[] UsersNonContracts =
        ["Dominodo.Users.Domain", "Dominodo.Users.Application", "Dominodo.Users.Persistence"];
    private static readonly string[] AdminNonContracts =
        ["Dominodo.Admin.Domain", "Dominodo.Admin.Application", "Dominodo.Admin.Persistence"];
    private static readonly string[] TenantsNonContracts =
        ["Dominodo.Tenants.Domain", "Dominodo.Tenants.Application", "Dominodo.Tenants.Persistence"];
    private static readonly string[] OperationsNonContracts =
        ["Dominodo.Operations.Domain", "Dominodo.Operations.Application", "Dominodo.Operations.Persistence"];
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
                 AdminApplication, AdminDomain, AdminPersistence,
                 TenantsApplication, TenantsDomain, TenantsPersistence,
                 OperationsApplication, OperationsDomain, OperationsPersistence])
            .ShouldNot()
            .HaveDependencyOnAny(Adapters)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(because: string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Tenants_MayOnlyReachUsers_ThroughItsContracts()
    {
        var result = Types.InAssemblies([TenantsApplication, TenantsDomain, TenantsPersistence])
            .ShouldNot()
            .HaveDependencyOnAny(UsersNonContracts)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(because: string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Tenants_ShouldNotDependOnAdminAtAll()
    {
        // Tenants integrates with Admin only asynchronously (integration events); no direct reference.
        var result = Types.InAssemblies([TenantsApplication, TenantsDomain, TenantsPersistence])
            .ShouldNot()
            .HaveDependencyOnAny([.. AdminNonContracts, "Dominodo.Admin.Contracts"])
            .GetResult();

        result.IsSuccessful.Should().BeTrue(because: string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void OtherModules_MayOnlyReachTenants_ThroughItsContracts()
    {
        var result = Types.InAssemblies(
                [UsersApplication, UsersDomain, UsersPersistence,
                 AdminApplication, AdminDomain, AdminPersistence,
                 OperationsApplication, OperationsDomain, OperationsPersistence])
            .ShouldNot()
            .HaveDependencyOnAny(TenantsNonContracts)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(because: string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Operations_MayOnlyReachTenants_ThroughItsContracts()
    {
        var result = Types.InAssemblies([OperationsApplication, OperationsDomain, OperationsPersistence])
            .ShouldNot()
            .HaveDependencyOnAny(TenantsNonContracts)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(because: string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Operations_MayOnlyReachUsers_ThroughItsContracts()
    {
        var result = Types.InAssemblies([OperationsApplication, OperationsDomain, OperationsPersistence])
            .ShouldNot()
            .HaveDependencyOnAny(UsersNonContracts)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(because: string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void OtherModules_MayOnlyReachOperations_ThroughItsContracts()
    {
        var result = Types.InAssemblies(
                [UsersApplication, UsersDomain, UsersPersistence,
                 AdminApplication, AdminDomain, AdminPersistence,
                 TenantsApplication, TenantsDomain, TenantsPersistence])
            .ShouldNot()
            .HaveDependencyOnAny(OperationsNonContracts)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(because: string.Join(", ", result.FailingTypeNames ?? []));
    }
}
