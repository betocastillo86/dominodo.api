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
}
