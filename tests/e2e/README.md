# Dominodo — Suite de pruebas E2E

Esta carpeta contiene una **solución .NET independiente** (`Dominodo.E2E.sln`) que prueba la API de
Dominodo **como una caja negra, a través de HTTP**. Vive en el mismo repositorio por comodidad de
trabajo, pero **no comparte ni una sola línea de código con la API**: no referencia ningún proyecto de
`src/`, no reutiliza sus DTOs y no se genera desde su OpenAPI.

Este documento es la **definición y la autoridad** del proyecto: qué es, cómo se estructura, qué reglas
son innegociables y **cómo evoluciona de la mano de la API sin quedar acoplada a ella**. Léelo antes de
crear un cliente, un modelo, un builder o un test.

---

## 1. El miedo que este diseño resuelve (y cómo)

> *"Si introduzco un bug en la API, no quiero que los tests E2E se 'autocorrijan' con el bug y parezca
> que todo funciona."*

Estar en otra solución **no** resuelve eso por sí solo. Lo que lo resuelve es **una disciplina**, y de
ella derivan las reglas de oro de más abajo:

1. **Los modelos y las rutas se escriben a mano, nunca se autogeneran del Swagger/OpenAPI de la API.**
   Si generaras el cliente desde el contrato de la API, cualquier cambio de contrato —incluido uno
   defectuoso— se propagaría solo al test y taparía el bug. Al escribirlos a mano, el test codifica el
   contrato **esperado**; si la API se desvía, el test **se rompe ruidosamente**. Esa ruptura es el
   producto, no un defecto.
2. **La suite prueba el comportamiento esperado (la especificación), no la implementación.** Un test se
   escribe desde "qué debería pasar", no desde "qué hace hoy el handler".
3. **La independencia está enforced físicamente:** solución separada, cero `ProjectReference` hacia
   `src/`, y una verificación en CI que falla si alguien la agrega (ver §5).

La duplicación de código (modelos replicados) **es deliberada y es el precio del aislamiento.** No es
deuda técnica; es la barrera que impide que el bug y su test se muevan juntos.

---

## 2. Reglas innegociables

1. **Caja negra por HTTP.** El único punto de contacto con la API es su superficie HTTP. Nada de
   `DbContext`, nada de invocar handlers, nada de `WebApplicationFactory`. Eso ya lo cubren los tests de
   integración dentro de `src/` (ver `docs/architecture/10-testing.md`); esta suite es **otra capa**.
2. **Cero acoplamiento de código con la API.** Ningún proyecto de esta solución referencia un proyecto
   de `src/`. Los modelos se replican a mano en `Dominodo.E2E.Clients`.
3. **Nada de codegen desde OpenAPI.** Interfaces Refit y modelos, escritos y mantenidos a mano.
4. **Los clientes solo se usan en el `Act`.** El `Arrange` se construye con **RequestBuilders**; el
   `Assert` valida la respuesta. El `Act` es la llamada al cliente Refit que estamos probando.
5. **Un eje de identidad y un eje de tenant, separados.** *Quién eres* = JWT (login real, cacheado).
   *Dónde actúas* = slug en el header `X-Tenant`. Son independientes (ver §7).
6. **Consistencia eventual = espera explícita.** Toda verificación que dependa de un integration event
   (efecto cross-módulo) se hace con *polling con reintentos*, nunca con un `assert` inmediato (ver §8).

---

## 3. Layout de la solución

Espeja el modelo de Pollaya, renombrado a Dominodo y alineado al vocabulario de módulos
(`Users`, `Tenants`, `Operations`, `Admin`).

```
tests/e2e/                                  # raíz de la solución E2E — NO se incluye en Dominodo.sln
  Dominodo.E2E.sln
  Directory.Build.props                     # TFM = el de la API (net9.0), nullable, ImplicitUsings
  Directory.Packages.props                  # (opcional) Central Package Management

  src/
    Dominodo.E2E.Core/                      # transversal, sin HTTP
      Autofixture/                          #   AutoDataAttribute, InlineAutoData
      Faker/                                #   extensiones Bogus (teléfonos E.164, NIT, slugs...)
      Context/                              #   AmbientTenantContext (AsyncLocal: slug actual)
      Policies/                             #   RetryPolicies (Polly) para consistencia eventual
      DominodoConstants.cs                  #   Headers, Roles, Defaults (slug por defecto)

    Dominodo.E2E.Clients.Core/              # fontanería HTTP (sin lógica de negocio)
      Api/         ApiSettings.cs           #   BaseUrl, DefaultTenantSlug, timeouts
      Handlers/                             #   DelegatingHandlers encadenados
        AuthorizationHandler.cs             #     inyecta Bearer (token del login real cacheado)
        TenantHeaderHandler.cs              #     inyecta X-Tenant desde AmbientTenantContext
        CorrelationIdHandler.cs             #     inyecta X-Correlation-Id + X-TestName
        LoggingHandler.cs
        DefaultRetryHandler.cs
      Context/     TestExecutionContext.cs  #   AsyncLocal: correlationId + testName
      Models/                               #   respuestas base replicadas: ProblemDetailsModel,
                                            #     PagedResultModel<T>, CreatedModel

    Dominodo.E2E.Clients/                   # clientes Refit + modelos + builders, POR MÓDULO
      Common/      BaseRequestBuilder.cs
      Auth/                                 #   IAuthClient + IAuthTokenProvider (login real, cacheado)
      Modules/
        Users/       IUsersClient.cs  Models/  UsersRequestBuilder.cs
        Tenants/     ITenantsClient.cs Models/  TenantsRequestBuilder.cs
        Operations/  IOperationsClient.cs Models/ OperationsRequestBuilder.cs
        Admin/       IAdminClient.cs  Models/  AdminRequestBuilder.cs
      ClientsServiceRegister.cs             #   AddUsersClient(), AddTenantsClient(), ... + handlers

  tests/
    Dominodo.E2E.Tests.Shared/              # base classes + fixture + seeding compartidos
      BaseE2ETests.cs                       #   Fixture, Faker, correlation/test-name por test
      E2ESetupFixtureBase.cs                #   OneTimeSetUp: DI + seeding del tenant por defecto
      Seeding/                              #   siembra tenant por defecto, roles, super-admin
    Dominodo.E2E.Tests.Users/               # 1 proyecto por módulo (SetUpFixture es por-assembly)
    Dominodo.E2E.Tests.Tenants/
    Dominodo.E2E.Tests.Operations/
    Dominodo.E2E.Tests.Admin/
```

**Dependencias entre proyectos** (todas apuntan "hacia el core", igual que la API apunta hacia adentro):

| Proyecto                     | Referencia a                                            |
| ---------------------------- | ------------------------------------------------------- |
| `E2E.Core`                   | solo NuGet                                              |
| `E2E.Clients.Core`           | `E2E.Core`                                              |
| `E2E.Clients`                | `E2E.Clients.Core`, `E2E.Core`                          |
| `E2E.Tests.Shared`           | `E2E.Clients`, `E2E.Core`                               |
| `E2E.Tests.<Module>`         | `E2E.Tests.Shared` (y por transitividad, lo anterior)   |
| **cualquiera → `src/`**      | **prohibido** (enforced en CI, §5)                      |

> **Decisión — un proyecto de tests por módulo.** Es tu requisito explícito y da mejor aislamiento y
> paralelismo. Costo a asumir: `[SetUpFixture]` de NUnit es **por assembly**, así que cada proyecto de
> módulo arranca su propio `ServiceProvider` y ejecuta el seeding. Por eso el `SetUpFixture` y el seeding
> viven en `E2E.Tests.Shared` como base reutilizable, y cada módulo tiene un `SetUpFixture` de una línea
> que hereda de `E2ESetupFixtureBase`.

---

## 4. Capa de clientes (Refit + handlers)

Un cliente por módulo. Interfaz Refit con las rutas **versionadas y escritas a mano**
(`/api/v1/...`, ver `docs/architecture/11-cross-cutting.md`). El token va como parámetro Refit
`[Authorize("Bearer")]` (null ⇒ request anónimo, para probar endpoints públicos y los 401).

```csharp
public interface IOperationsClient
{
    [Post("/api/v1/requests")]
    Task<ApiResponse<CreatedModel>> CreateRequest(
        [Body] NewRequestModel model,
        [Authorize("Bearer")] string? token = null);

    [Get("/api/v1/requests/{id}")]
    Task<ApiResponse<RequestModel>> GetRequestById(
        Guid id,
        [Authorize("Bearer")] string? token = null);

    [Get("/api/v1/requests")]
    Task<ApiResponse<PagedResultModel<RequestModel>>> GetRequests(
        [Query] RequestFilterModel filter,
        [Authorize("Bearer")] string? token = null);
}
```

**Handlers encadenados** (registrados en `ClientsServiceRegister`, uno por cliente):

- `TenantHeaderHandler` — inyecta `X-Tenant: <slug>` desde `AmbientTenantContext` (el slug de la clase
  de test actual, o el del tenant por defecto). Se puede sobreescribir por llamada.
- `AuthorizationHandler` — no reescribe nada; el token ya viene del `[Authorize("Bearer")]`. (Se deja
  como punto de extensión para políticas transversales; hoy es un passthrough.)
- `CorrelationIdHandler` — `X-Correlation-Id` + `X-TestName` desde `TestExecutionContext` (trazabilidad
   punta a punta contra los logs de la API, ver `docs/architecture/11-cross-cutting.md`).
- `LoggingHandler`, `DefaultRetryHandler` — logging estructurado y reintentos de transporte (5xx/timeout),
  **no** de aserción.

**Serialización:** `System.Text.Json` alineado a los defaults de ASP.NET Core de la API (enums como
string, `DateTimeOffset` ISO-8601, camelCase). *No* usamos Newtonsoft (Pollaya sí lo usaba por su API
legada; Dominodo es greenfield y debe casar con lo que emite el host).

---

## 5. La independencia, enforced (no solo prometida)

Tres barreras, de la más barata a la más fuerte:

1. **Solución separada.** `Dominodo.E2E.sln` nunca incluye proyectos de `src/`, y `Dominodo.sln`
   (el de la API) nunca incluye `tests/e2e/`.
2. **Guarda en CI.** Un check que falla el build si aparece un `ProjectReference` que salga de
   `tests/e2e/` hacia `src/`:

   ```bash
   # scripts/e2e-guard.sh — corre en el workflow de E2E
   if grep -rl --include="*.csproj" -E 'ProjectReference[^>]*\.\./\.\./src/' tests/e2e; then
     echo "❌ La suite E2E no puede referenciar proyectos de la API (src/)."; exit 1
   fi
   ```
3. **Prohibición de codegen.** No hay target de MSBuild ni script que genere clientes desde el OpenAPI.
   Si algún día se quiere, se discute como cambio de arquitectura — porque **rompe la propiedad central**
   de esta suite.

---

## 6. Modelos replicados a mano

Viven en `Modules/<Module>/Models/`, uno por request/response. Espejan el DTO público de la API
(`*.Contracts`) **por valor, no por referencia**: mismos nombres de campo, tipos equivalentes.

```csharp
// Dominodo.E2E.Clients/Modules/Operations/Models/NewRequestModel.cs
public sealed class NewRequestModel
{
    public Guid ApartmentId { get; init; }
    public string Type { get; init; } = default!;   // "Peticion" | "Queja" | ...
    public string Title { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string? Location { get; init; }
}

// RequestModel, PagedResultModel<T>, ProblemDetailsModel (RFC 9457), CreatedModel...
```

Convención: sufijo `Model` (no `Dto`, para no confundir con los DTO de la API). `New*` para creación,
`Update*` para edición, `*FilterModel` para query strings.

---

## 7. Autenticación y multitenancy en E2E

Refleja `docs/architecture/09-multitenancy.md`: **el slug del header `X-Tenant` decide el tenant; el
JWT solo valida** que el usuario pertenezca a ese tenant.

### Identidad — login real cacheado

Un `IAuthTokenProvider` que llama a los endpoints reales de auth y **cachea por `(usuario, tenantSlug)`**
para no re-loguear en cada test. Ejercita el flujo de auth de verdad; el costo es que un bug en login
tumba muchas suites (aceptable y deseable: auth es crítico).

```csharp
public interface IAuthTokenProvider
{
    // login + selección de tenant reales; cacheado
    Task<string> GetTokenAsync(string phone, string password, string tenantSlug);
    Task<string> GetTokenForRoleAsync(string role, string tenantSlug); // usuarios sembrados por rol
}
```

### Tenant — slug ambient + creación on-demand

`AmbientTenantContext` (AsyncLocal) guarda el slug "actual"; `TenantHeaderHandler` lo inyecta en cada
request. La estrategia de datos elegida (**tenant sembrado por defecto + tenants nuevos cuando el caso
lo pida**, estilo Pollaya):

- El `SetUpFixture` siembra **un tenant por defecto** (`DominodoConstants.Defaults.TenantSlug`,
  p.ej. `e2e-default`) con sus roles, un super-admin y usuarios base por rol. La mayoría de tests actúan
  sobre él.
- Cuando un caso necesita aislamiento fuerte o datos vírgenes, `TenantsRequestBuilder.CreateTenant()`
  crea uno nuevo (como super-admin), devuelve su slug, y el test lo fija en el `AmbientTenantContext`
  para sus llamadas.

> **Riesgo a vigilar (tenant compartido):** tests que escriben sobre el tenant por defecto pueden
> contaminarse entre sí (p.ej. conteos, listados paginados). Mitigación: aserciones que no dependan del
> estado global (filtrar por el recurso creado en el test, no por totales del tenant), y mover a tenant
> propio cualquier caso sensible al volumen de datos.

### La matriz de reconciliación como test de primer nivel

Justo el tipo de bug que un test acoplado taparía. Cubrir explícitamente la tabla del doc 09:

| Caso                                   | `X-Tenant` | Token           | Esperado             |
| -------------------------------------- | ---------- | --------------- | -------------------- |
| Usuario regular, su sitio              | slug de A  | JWT de A        | `200`                |
| Usuario regular, slug de otro tenant   | slug de B  | JWT de A        | `403 Tenant.Mismatch`|
| Slug desconocido                       | `nope`     | cualquiera      | `400 Tenant.Unknown` |
| Anónimo en endpoint público            | slug de A  | —               | `200`                |
| Usuario de tenant sin header           | —          | JWT de A        | `403 Tenant.Mismatch`|
| Super-admin cross-tenant               | ausente    | JWT super-admin | `200` (todos)        |

---

## 8. Capa de tests

**Stack:** NUnit + AutoFixture + Bogus + Shouldly + Polly (mismas librerías que Pollaya).

- `BaseE2ETests` (en `Shared`): expone `Fixture`, `Faker`, fija `TestExecutionContext` (correlationId +
  nombre del test) en `[SetUp]` y lo limpia en `[TearDown]`.
- Cada módulo tiene una `Base<Module>Tests` que resuelve del `ServiceProvider` sus builders y clientes en
  `[OneTimeSetUp]`, y fija el `AmbientTenantContext` al tenant por defecto (o crea el suyo).
- **Estructura del test:** `Arrange` con builders → `Act` = **una** llamada al cliente Refit bajo prueba
  → `Assert` sobre `StatusCode`, `Content` y/o el `ProblemDetailsModel`.

```csharp
[TestFixture]
public class CreateRequestTests : BaseOperationsTests
{
    [Test]
    public async Task _401_WhenNoToken()
    {
        // Act — sin token (anónimo)
        var response = await OperationsClient.CreateRequest(
            OperationsRequestBuilder.BuildNewRequestModel());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _201_CreatesRequest_ForResident()
    {
        // Arrange — un residente con apartamento en el tenant actual
        var (token, resident, apartment) =
            await OperationsRequestBuilder.SetupResidentWithApartment();
        var model = OperationsRequestBuilder.BuildNewRequestModel(apartmentId: apartment.Id);

        // Act
        var response = await OperationsClient.CreateRequest(model, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Content!.Id.ShouldNotBe(Guid.Empty);
    }
}
```

### RequestBuilders — el corazón del `Arrange`

Un builder por módulo, componible: un builder puede depender de otros para armar casos de uso completos
(igual que en Pollaya `TriviasRequestBuilder` depende de `OrdersRequestBuilder`/`LeaguesRequestBuilder`).
En Dominodo esto modela la dependencia natural del dominio: para crear un PQR necesitas tenant + usuario
+ membresía + apartamento.

```csharp
public sealed class OperationsRequestBuilder : BaseRequestBuilder
{
    private readonly IOperationsClient _operations;
    private readonly TenantsRequestBuilder _tenants;   // crea tenant/apartamento
    private readonly UsersRequestBuilder _users;       // crea usuario + membresía/rol
    // ...ctor inyecta todo...

    // Construye el modelo (datos falsos por defecto, sobreescribibles) — NO llama a la API
    public NewRequestModel BuildNewRequestModel(Guid? apartmentId = null, string? title = null) => new()
    {
        ApartmentId = apartmentId ?? Guid.NewGuid(),
        Type = "Peticion",
        Title = title ?? Faker.Lorem.Sentence(4),
        Description = Faker.Lorem.Paragraph(),
    };

    // Caso de uso completo de Arrange — SÍ llama a la API (setup, no es el Act bajo prueba)
    public async Task<(string Token, ResidentModel Resident, ApartmentModel Apartment)>
        SetupResidentWithApartment()
    {
        var apartment = await _tenants.CreateApartment();
        var (token, resident) = await _users.CreateResidentWithMembership(apartment.Id);
        return (token, resident, apartment);
    }
}
```

> **Regla:** los builders lanzan excepción si un paso de `Arrange` falla (respuesta no exitosa). Un
> `Arrange` roto debe **abortar** el test, no producir un `Assert` engañoso. El único punto donde
> evaluamos códigos de estado como parte del resultado es el `Act`.

### Consistencia eventual (integration events)

Los efectos cross-módulo son asíncronos (ver `docs/architecture/07-inter-module-communication.md`):
crear un `Request` publica `RequestOpenedIntegrationEvent`, y el módulo `Admin` consume y genera
notificaciones. Un `Assert` inmediato sería *flaky*. Se usa **polling con reintentos** (Polly), como el
`RetryPolicies.CreateAssertionRetryPolicy` de Pollaya:

```csharp
// Act
await OperationsClient.CreateRequest(model, token);

// Assert eventual — reintenta hasta que la notificación aparezca (o expira el timeout)
await RetryPolicies.Until<PagedResultModel<NotificationModel>>(
    action: () => AdminClient.GetMyNotifications(residentToken),
    predicate: page => page.Items.Any(n => n.Type == "RequestOpened"));
```

---

## 9. Entorno y ejecución

- **Contra qué corre:** la API + Postgres + bus levantados con **docker-compose local** (o Aspire). Los
  tests apuntan a `http://localhost:<port>`. `BaseUrl` y `DefaultTenantSlug` en `appsettings.json`
  (+ `appsettings.Local.json` opcional, gitignored) por proyecto de test.
- **Estado de BD:** al ser local y controlado, el `SetUpFixture` puede resetear/migrar la BD antes de
  sembrar. El aislamiento principal viene del **tenant** (por defecto + on-demand), no del reset.
- **CI:** un workflow dedicado (separado del de la API) que: levanta docker-compose → espera health
  (`/health/ready`, ver doc 11) → corre `dotnet test Dominodo.E2E.sln` → publica resultados.
- **Load tests:** **fuera de alcance** por ahora (omitidos deliberadamente).

---

## 10. Cómo evoluciona junto a la API (el punto crítico)

La suite E2E **va detrás** de la API, a propósito y con intención. El flujo:

1. La API agrega/cambia un endpoint (feature slice; ver `docs/architecture/03-cqrs-mediatr.md` y la skill
   `domi-add-feature-slice`).
2. En un **paso/PR separado y revisado por humano**, la suite E2E incorpora:
   - el/los **modelos** replicados a mano en `Modules/<Module>/Models/`,
   - el/los **métodos Refit** en `I<Module>Client`,
   - el/los métodos de **RequestBuilder** para el `Arrange`,
   - los **tests** (camino feliz + errores: 400/401/403/404/409/422 según doc 08, y la matriz de tenant
     del doc 09 cuando aplique).
3. **La desincronización es visible, no silenciosa.** Si la API cambió un contrato, el modelo a mano ya
   no casa y el test falla. Ese fallo es la señal de "algo cambió, revísalo a conciencia" — exactamente
   lo que buscabas.

**Reglas para preservar la propiedad:**

- El cambio de comportamiento de la API y el ajuste del test E2E **no van en el mismo commit sin
  revisión**. Si un test E2E hay que cambiarlo, el PR debe justificar *por qué* el contrato esperado
  cambió (no "para que pase").
- **Versionado:** cuando la API rompe contrato, sube versión (`/api/v2/...`). La suite mantiene los tests
  de `v1` mientras `v1` exista, y agrega los de `v2`. Nunca se "mueve" un test de v1 a v2 en silencio.
- **Checklist por feature slice nuevo** (pegar en el PR de E2E):
  - [ ] Modelos replicados a mano (sin copiar del proyecto de la API).
  - [ ] Método(s) Refit con ruta versionada.
  - [ ] RequestBuilder para el `Arrange`.
  - [ ] Camino feliz + errores relevantes (doc 08).
  - [ ] Matriz de tenant si el endpoint es tenant-scoped/anónimo/super-admin (doc 09).
  - [ ] Aserción eventual (Polly) si dispara integration events (doc 07).
  - [ ] Cliente usado **solo** en el `Act`.

---

## 11. Convenciones de nombres

- **Proyectos:** `Dominodo.E2E.<Área>` / `Dominodo.E2E.Tests.<Módulo>`.
- **Clientes:** `I<Módulo>Client` (`IOperationsClient`).
- **Builders:** `<Módulo>RequestBuilder`.
- **Modelos:** `New<Noun>Model`, `Update<Noun>Model`, `<Noun>Model`, `<Noun>FilterModel`.
- **Clases de test:** `<Verbo><Noun>Tests` (`CreateRequestTests`), una por caso de uso/endpoint.
- **Tests:** `_<StatusCode>_<Escenario>` (`_403_WhenTenantMismatch`) — estilo Pollaya, legible en el runner.

---

## 12. Librerías (alinear versiones al crear los `.csproj`)

| Propósito                 | Paquete                                                        |
| ------------------------- | ------------------------------------------------------------- |
| Test runner               | `NUnit`, `NUnit3TestAdapter`, `Microsoft.NET.Test.Sdk`        |
| Clientes HTTP tipados     | `Refit`, `Refit.HttpClientFactory`                            |
| Datos falsos              | `Bogus`, `AutoFixture`, `AutoFixture.NUnit3`                   |
| Aserciones                | `Shouldly`                                                    |
| Reintentos / eventual     | `Polly`                                                       |
| Config + DI + logging     | `Microsoft.Extensions.*`, `Serilog.Sinks.Console`             |

> Serialización con `System.Text.Json` (no Newtonsoft) para casar con el host de Dominodo.

---

## 13. Estado y próximos pasos

Este documento **es la semilla**. Todavía no existen los `.csproj`. Para arrancar la implementación:

1. Crear `Dominodo.E2E.sln` y los proyectos de §3 con el TFM de la API.
2. Implementar `E2E.Clients.Core` (handlers) y `E2E.Core` (auth token provider, contexts, retry).
3. Escribir a mano el primer módulo end-to-end (`Users` o `Tenants`) como **ejemplar canónico** —el que
   todo lo demás copia—, incluida su siembra y la matriz de tenant.
4. Añadir la guarda de CI (§5) y el workflow de docker-compose (§9).
5. A partir de ahí, cada feature slice de la API arrastra su slice de E2E (§10).
