# 10 — Testing

## What it is

Three layers of tests, each with a clear job:

1. **Unit tests** — fast, in-memory tests of a single class (aggregate, handler, validator, adapter)
   with its collaborators mocked.
2. **Integration tests** — the module (or the whole host) booted with `WebApplicationFactory`, real
   in-process pipeline, real database (or a test database), and **external HTTP dependencies stubbed
   with WireMock**.
3. **Architecture tests** — `NetArchTest` rules that fail the build when a module or layer boundary
   is violated.

Toolset: **xUnit** (runner), **NSubstitute** (mocking), **AutoFixture** (test data), **FluentAssertions**
(assertions), **WireMock.Net** (HTTP stubbing). Shared helpers live in `Dominodo.TestUtilities`.

## Why

- Unit tests give fast feedback on business logic and edge cases.
- Integration tests prove the wired system behaves — routing, validation, persistence, error mapping,
  event publication — against realistic boundaries.
- WireMock lets us exercise adapter behavior (success, failure, retries) deterministically without
  touching real third parties.
- Architecture tests turn the boundary rules in this guide into an automated gate, so isolation does
  not erode silently.

## Unit tests

Test one thing; mock its ports. Aggregates need no mocks — assert on the returned `Result` and state.

```csharp
public sealed class PqrTests
{
    [Fact]
    public void Close_WhenAlreadyClosed_ReturnsConflict()
    {
        var pqr = Pqr.Open(Guid.NewGuid(), Guid.NewGuid(), "Leak", "Water leak").Value;
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        pqr.Close(clock);

        var result = pqr.Close(clock);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }
}
```

Handler with mocked ports:

```csharp
public sealed class OpenPqrCommandHandlerTests
{
    private readonly IPqrRepository _repo = Substitute.For<IPqrRepository>();
    private readonly ITenantsModuleApi _tenants = Substitute.For<ITenantsModuleApi>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();

    [Fact]
    public async Task Handle_WhenApartmentMissing_ReturnsNotFound()
    {
        _tenants.GetApartmentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns((ApartmentDto?)null);
        var sut = new OpenPqrCommandHandler(_repo, _tenant, _tenants);

        var result = await sut.Handle(new OpenPqrCommand(Guid.NewGuid(), "s", "b"), default);

        result.Error.Type.Should().Be(ErrorType.NotFound);
        _repo.DidNotReceive().Add(Arg.Any<Pqr>());
    }
}
```

Use AutoFixture to remove noise from arranging data:

```csharp
[Theory, AutoData]
public void Validator_RejectsEmptySubject(OpenPqrCommand command)
{
    var result = new OpenPqrCommandValidator().TestValidate(command with { Subject = "" });
    result.ShouldHaveValidationErrorFor(x => x.Subject);
}
```

## Integration tests

Boot the app with `WebApplicationFactory`, override DI to point external calls at WireMock and to use
a test database, and drive real HTTP requests.

```csharp
// Dominodo.<Module>.IntegrationTests/DominodoApiFactory.cs
public sealed class DominodoApiFactory : WebApplicationFactory<IApiMarker>, IAsyncLifetime
{
    public WireMockServer WireMock { get; } = WireMockServer.Start();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            // point adapters at the WireMock base address
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Adapters:Email:BaseUrl"] = WireMock.Url,
                ["Adapters:WhatsApp:BaseUrl"] = WireMock.Url,
                ["ConnectionStrings:Dominodo"] = TestDatabase.ConnectionString
            });
        });
    }

    public async Task InitializeAsync() => await TestDatabase.EnsureCreatedAsync();
    public new async Task DisposeAsync() { WireMock.Stop(); await TestDatabase.DropAsync(); }
}
```

A base class exposes the client, the WireMock server, and resets state between tests:

```csharp
public abstract class IntegrationTestBase(DominodoApiFactory factory) : IClassFixture<DominodoApiFactory>
{
    protected HttpClient Client { get; } = factory.CreateClient();
    protected WireMockServer WireMock => factory.WireMock;
}
```

### WireMock: stub external HTTP dependencies

Keep stub setup in reusable builder classes (one per external service) with fluent, intention-revealing
methods and support for error/recovery scenarios.

```csharp
// Dominodo.TestUtilities/WireMock/EmailProviderStub.cs
public sealed class EmailProviderStub(WireMockServer server)
{
    public EmailProviderStub RespondsOk()
    {
        server.Given(Request.Create().WithPath("/v1/messages").UsingPost())
              .RespondWith(Response.Create().WithStatusCode(200));
        return this;
    }

    public EmailProviderStub FailsThenRecovers(int failures)
    {
        server.Given(Request.Create().WithPath("/v1/messages").UsingPost())
              .InScenario("email").WillSetStateTo("recovered", failures)
              .RespondWith(Response.Create().WithStatusCode(500));

        server.Given(Request.Create().WithPath("/v1/messages").UsingPost())
              .InScenario("email").WhenStateIs("recovered")
              .RespondWith(Response.Create().WithStatusCode(200));
        return this;
    }
}
```

### An integration test

```csharp
public sealed class OpenPqrEndpointTests(DominodoApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Post_ValidPqr_Returns201()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/pqrs",
            new { apartmentId = SeededData.ApartmentId, subject = "Leak", body = "Water leak" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Post_EmptySubject_Returns400WithFieldError()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/pqrs",
            new { apartmentId = SeededData.ApartmentId, subject = "", body = "b" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemResponse>();
        problem!.Errors.Should().ContainSingle(e => e.Property == "Subject");
    }
}
```

## Architecture tests

Encode the boundary rules from this guide so a violation breaks the build.

```csharp
public sealed class ModuleBoundaryTests
{
    [Fact]
    public void Domain_ShouldNotDependOnApplicationOrPersistence()
    {
        var result = Types.InAssembly(typeof(Pqr).Assembly)
            .ShouldNot().HaveDependencyOnAny("Dominodo.Pqrs.Application", "Dominodo.Pqrs.Persistence")
            .GetResult();
        result.IsSuccessful.Should().BeTrue(because: string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Modules_MayOnlyReferenceOtherModulesContracts()
    {
        // e.g. Pqrs must not depend on Tenants' Domain/Application/Persistence — only Tenants.Contracts
        var result = Types.InAssembly(typeof(Pqrs.Application.DependencyInjection).Assembly)
            .ShouldNot().HaveDependencyOnAny(
                "Dominodo.Tenants.Domain", "Dominodo.Tenants.Application", "Dominodo.Tenants.Persistence")
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Handlers_ShouldBeInternal()
    {
        var result = Types.InAssembly(typeof(Pqrs.Application.DependencyInjection).Assembly)
            .That().ImplementInterface(typeof(IRequestHandler<,>))
            .Should().NotBePublic()
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }
}
```

Run architecture tests in CI as a required check — they are the automated enforcement of everything
in the README's dependency table.

## Do / Don't

- **Do** unit-test aggregates and validators with no mocks; mock only ports in handler tests.
- **Do** boot integration tests through `WebApplicationFactory` and stub external HTTP with WireMock.
- **Do** keep WireMock stubs in reusable builders, including error/recovery scenarios.
- **Do** encode every boundary rule as an architecture test and gate CI on it.
- **Don't** hit real third-party services in tests.
- **Don't** assert on private state; assert on returned `Result`s and observable behavior.
- **Don't** let integration tests share mutable state — reset between tests.
