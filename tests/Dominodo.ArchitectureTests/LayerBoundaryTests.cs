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
}
