using AutoFixture;
using Dominodo.E2E.Clients.Core.Context;
using Dominodo.E2E.Core.Autofixture;
using Dominodo.E2E.Core.Context;
using Dominodo.E2E.Core.Security;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dominodo.E2E.Tests.Shared;

[TestFixture]
public abstract class BaseE2ETests
{
    protected static IServiceProvider ServiceProvider => E2ESetupFixtureBase.ServiceProvider;

    protected IFixture Fixture { get; } = new Fixture().Customize(new DominodoCustomization());

    protected Bogus.Faker Faker { get; } = new("en");

    protected JwtTokenFactory JwtTokenFactory => ServiceProvider.GetRequiredService<JwtTokenFactory>();

    [SetUp]
    public void BaseSetUp()
    {
        var correlationId = Guid.NewGuid().ToString();
        var testName = TestContext.CurrentContext.Test.FullName;
        TestExecutionContext.Set(correlationId, testName);
    }

    [TearDown]
    public void BaseTearDown()
    {
        TestExecutionContext.Clear();
        AmbientTenantContext.Clear();
    }
}
