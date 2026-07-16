# Modelo de dominio — Dominodo (MVP)

> **Estado:** vigente · **Versión del modelo:** v2 · **Última actualización:** 2026-07-16
> Este archivo refleja siempre la **verdad actual** del modelo (documento vivo, un solo archivo).
> El *porqué* de las decisiones vive en `docs/adr/`; la historia línea por línea, en git.
> Se sube la versión (v2, v3…) solo ante cambios **estructurales**, no por correcciones menores.

> Primer levantamiento de bounded contexts, agregados, entidades, campos y relaciones.
> Prosa en español; identificadores/enums en inglés para alinear con las convenciones del repo
> (`docs/architecture/`). Este documento es la fuente de verdad del **modelo**; los patrones de
> *implementación* viven en `docs/architecture/`.

## Convenciones usadas aquí

- **Agg** = aggregate root. **Ent** = entity dentro de un agregado. **VO** = value object.
- `Guid` como identificador por defecto. Códigos legibles (`Code`) son secundarios, no la PK.
- `*` junto a un campo = **único**. `?` = nullable/opcional.
- `ITenantOwned` = agregado con `TenantId` que se **scopea** por tenant en las lecturas
  (`ForCurrentTenant`, ver `09-multitenancy.md`). **System-level** = registro global.
  Ojo: una tabla puede tener una columna `TenantId` **sin** ser `ITenantOwned` (ver §4.2).
- **Nunca** hay FK entre módulos. Una referencia a algo de otro módulo se guarda como `Guid` crudo
  y se resuelve por fachada (`IModuleApi`) o se reacciona por integration event.

## Decisiones ya cerradas (contexto)

1. **Teléfono** = llave natural y única (canal WhatsApp). Email opcional, único si se provee.
2. **Roles y permisos: catálogo global y único.** Un permiso nuevo aplica a todos los conjuntos.
   Propietario/Arrendatario **no son roles**: son el tipo de vínculo con un apartamento.
3. **El rol se asigna según su ámbito (scope).** El rol de **ámbito `Tenant`** se asigna en el
   `Membership` (usuario↔conjunto↔rol); el rol de **ámbito `Platform`**, en el
   `PlatformRoleAssignment` (usuario↔rol global). Nunca en el `User`.
4. **PQRS + paquetería + visitas** viven en **un solo módulo** (`Operations`).
5. **Notifications + Configuration viven en un solo módulo `Admin`** (mismo schema, mismo futuro
   servicio): son temas administrativos que no queremos atomizar.
6. **Tenant en el token** (claim `tenant_id`). Login global → elegir conjunto → token *tenant-scoped*
   con el rol de ese conjunto. **`SuperAdmin`** es un rol de **ámbito `Platform`** con todos los
   permisos; se asigna a un usuario **por dato** (una fila `PlatformRoleAssignment`), no por
   constante en el código. Su acceso cross-tenant **sale de tener todos los permisos**, no de un
   bypass hardcodeado. No es un módulo ni un usuario aparte.
7. **Ejemplar canónico (primer módulo escrito a mano): `Users`** — con usuarios ya se pueden crear
   tenants después.

## Mapa de módulos

| Módulo | Responsabilidad | Schema |
| --- | --- | --- |
| `Users` | Identidad global, autenticación, catálogo global de roles/permisos, membresía a conjuntos | `users` |
| `Tenants` | Conjuntos (Tenant), apartamentos, vínculo residente↔apartamento, features habilitadas | `tenants` |
| `Operations` | Solicitudes (PQRS), paquetería, visitas | `operations` |
| `Admin` | Notificaciones (in-app/email/push), plantillas, boletín, dispositivos, y configuración (SystemSetting) | `admin` |

> Cada módulo = un schema + un `DbContext`. Cuando la app pase a microservicios, cada módulo es un
> candidato a servicio independiente. Por eso `Admin` agrupa notificaciones + configuración: para que
> viajen juntos y no queden atomizados.

## Clasificación de multitenancy (crítico)

| System-level (global, sin scoping) | Tenant-owned (`ITenantOwned`, se scopea) |
| --- | --- |
| `User`, `Role`, `Permission`, `RolePermission`, `PlatformRoleAssignment` | `Membership` |
| `RefreshToken`, `VerificationCode`, `DeviceRegistration` | `Apartment`, `ApartmentResident`, `TenantFeature` |
| `Tenant` (es el registro de tenants) | `Request`, `Delivery`, `Visit` y sus hijos |
| `SystemSetting` global, `NotificationTemplate` global | `Announcement` |
| Mensajes materializados: `UserNotification`, `EmailMessage`, `PushMessage` (§4.2) | `SystemSetting` override, `NotificationTemplate` override |

---

# 1. Módulo `Users`

Identidad **global**: una persona = un registro, reutilizable en todos los conjuntos. La autorización
por conjunto vive en `Membership`.

## 1.1 `User` (Agg) — system-level

| Campo | Tipo | Notas |
| --- | --- | --- |
| `Id` | `Guid` | PK |
| `Phone` | `string`* | E.164 (`+57...`). **Obligatorio y único.** Llave para WhatsApp |
| `Email` | `string?`* | Único si presente |
| `FirstName` | `string` | |
| `LastName` | `string` | |
| `DocumentType` | `enum?` | `CC`, `CE`, `NIT`, `Passport` — columna (los admins buscan por cédula) |
| `DocumentNumber` | `string?` | Columna, indexado (búsqueda) |
| `PasswordHash` | `string?` | **bcrypt/argon2**. El **salt va embebido en el hash** (formato estándar), no hay columna `Salt` aparte. Nullable: residentes pueden entrar solo por OTP/WhatsApp |
| `Status` | `enum` | `PendingVerification`, `Active`, `Disabled` |
| `PhoneVerifiedAtUtc` | `DateTimeOffset?` | |
| `EmailVerifiedAtUtc` | `DateTimeOffset?` | |
| `PreferredLanguage` | `string` | `es` por defecto |
| `AvatarUrl` | `string?` | |
| `Profile` | `json` | **JSON** — datos de perfil no filtrables (redes, preferencias UI, etc.) |
| `CreatedAtUtc` / `UpdatedAtUtc` | `DateTimeOffset` | |

**Índices:** único en `Phone`; único filtrado en `Email` (where not null); índice en `DocumentNumber`.

## 1.2 `Role` (Agg) — global, **seed**

| Campo | Tipo | Notas |
| --- | --- | --- |
| `Id` | `int` | Seed, `ValueGeneratedNever` |
| `Name` | `string`* | |
| `Description` | `string?` | |
| `IsSystem` | `bool` | Roles de sistema no se borran |
| `Scope` | `enum` | `Platform` \| `Tenant` — **cómo se asigna el rol** (ver nota) |

**Seed inicial:**

| Rol | Scope |
| --- | --- |
| `SuperAdmin` | `Platform` |
| `Administrador` | `Tenant` |
| `AsistenteAdministracion` | `Tenant` |
| `Vigilante` | `Tenant` |
| `Residente` | `Tenant` |

`SuperAdmin` = todos los permisos + acceso cross-tenant.

> **El `Scope` describe cómo se asigna el rol, no qué permisos agrupa.** Un rol `Platform` se asigna
> globalmente al usuario (`PlatformRoleAssignment`, §1.5); un rol `Tenant` se asigna dentro de un
> conjunto (`Membership`, §1.6). Un rol `Platform` **sí puede contener permisos de tenant**: de hecho
> `SuperAdmin`, siendo `Platform`, agrupa todos los permisos (incluidos los de tenant como
> `requests.create`) — por eso opera cross-tenant.

## 1.3 `Permission` (Agg) — global, **seed**

| Campo | Tipo | Notas |
| --- | --- | --- |
| `Id` | `int` | Seed |
| `Code` | `string`* | Namespaced: `requests.create`, `deliveries.register`, `users.manage`, `tenants.create`… |
| `Description` | `string` | |
| `Group` | `string` | Para agrupar en UI (`Solicitudes`, `Paquetería`, `Plataforma`…) |

**Permisos de plataforma** (grupo `Plataforma`): capacidades que existen **sin tenant** y preceden a
cualquier conjunto — `tenants.create`, `tenants.manage`. Se conceden por roles de ámbito `Platform`.

> **Permisos = catálogo ligado al código** (hay un `if` que los verifica). **No** hay CRUD de permisos.
> El **permiso no lleva scope**: es una capacidad neutra. Quién la ejerce y con qué alcance lo
> determina el rol que la agrupa (§1.2) y el ámbito en que ese rol fue asignado.

## 1.4 `RolePermission` (join) — global, **seed**

`RoleId` (int) + `PermissionId` (int). Único `(RoleId, PermissionId)`.

## 1.5 `PlatformRoleAssignment` (Agg) — system-level

La asignación **global** de un rol de ámbito `Platform` a un usuario. Aquí vive la **autorización de
plataforma**: capacidades que existen **sin tenant** y preceden a cualquier conjunto (crear/configurar
conjuntos, gestionar el catálogo de roles, operar cross-tenant). Es la contraparte system-level del
`Membership` (§1.6). El `SuperAdmin` bootstrap **es una fila de esta tabla**, no un caso especial del
código.

| Campo | Tipo | Notas |
| --- | --- | --- |
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK **interna** a `User` (mismo módulo) |
| `RoleId` | `int` | FK interna a `Role`. Debe ser un rol con `Scope = Platform` |

**Índice único** `(UserId, RoleId)`.

## 1.6 `Membership` (Agg) — **`ITenantOwned`**

El acceso de un usuario a un conjunto **con un rol** de ámbito `Tenant`. Aquí vive la autorización
por-conjunto (la contraparte tenant-owned del `PlatformRoleAssignment`, §1.5).

| Campo | Tipo | Notas |
| --- | --- | --- |
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK **interna** a `User` (mismo módulo) |
| `TenantId` | `Guid` | Valor crudo (Tenant vive en otro módulo). `ITenantOwned` |
| `RoleId` | `int` | FK interna a `Role` |
| `Status` | `enum` | `Invited`, `Active`, `Suspended` |
| `InvitedAtUtc` | `DateTimeOffset?` | |
| `JoinedAtUtc` | `DateTimeOffset?` | |

**Índice único** `(UserId, TenantId)` — un rol por persona por conjunto. Índice en `TenantId`.

## 1.7 Autenticación (soporte)

- **`RefreshToken`** (system-level): `Id`, `UserId`, `TokenHash`, `ExpiresAtUtc`, `RevokedAtUtc?`,
  `ReplacedByTokenId?`, `CreatedByIp?`. Rotación + revocación (lo que a pollaya le falta).
- **`VerificationCode`** (system-level): `Id`, `UserId?`, `Phone`, `Purpose`
  (`PhoneVerify`/`Login`/`PasswordReset`), `CodeHash`, `ExpiresAtUtc`, `ConsumedAtUtc?`, `Attempts`.
  Para OTP por SMS/WhatsApp.

## 1.8 Fachada `IUsersModuleApi` (Contracts)

> Esto **es** el `IModuleApi` de `07-inter-module-communication.md`: interfaz pública en `Contracts`,
> implementada internamente delegando a MediatR del propio módulo. **No** es un endpoint ni reemplaza
> MediatR. Los métodos son las lecturas cross-module que otros módulos necesitarán:

```
GetUserById(id)               -> UserDto?
GetUserByPhone(phoneE164)     -> UserDto?        // ← lo usa el adaptador de WhatsApp
GetMemberships(userId)        -> MembershipDto[]
GetPlatformPermissions(userId)            -> PermissionDto[]   // permisos de ámbito Platform del user
GetEffectivePermissions(userId, tenantId) -> PermissionDto[]   // platform ∪ tenant (ver nota)
```

> `GetEffectivePermissions(userId, tenantId)` devuelve la **unión** de los permisos de plataforma del
> usuario (sus `PlatformRoleAssignment`) y los permisos del `Membership` de ese tenant. Como
> `SuperAdmin` (`Platform`) tiene todos los permisos, para él devuelve todo en cualquier tenant —
> ese es el acceso cross-tenant, derivado de datos.

## 1.9 Integration events (Contracts)

`UserRegisteredIntegrationEvent`, `MembershipCreatedIntegrationEvent`,
`MembershipSuspendedIntegrationEvent`.

---

# 2. Módulo `Tenants`

El conjunto residencial y todo lo físico/estructural.

## 2.1 `Tenant` (Agg) — **system-level** (es el registro de tenants)

| Campo | Tipo | Notas |
| --- | --- | --- |
| `Id` | `Guid` | PK. Este es el `tenant_id` del claim |
| `Name` | `string` | |
| `LegalId` | `string?` | NIT |
| `Type` | `enum` | `Conjunto`, `Edificio`, `Mixto` |
| `Status` | `enum` | `Onboarding`, `Active`, `Suspended` |
| `Address` / `City` / `Country` | `string` | |
| `Branding` | `json` | **JSON** — logo, colores, textos. No filtrable |
| `Settings` | `json` | **JSON** — parámetros del conjunto no filtrables |
| `CreatedAtUtc` | `DateTimeOffset` | |

## 2.2 `TenantFeature` (Ent bajo `Tenant`) — **`ITenantOwned`**

Qué features tiene habilitadas el conjunto. Filas explícitas, **no JSON**, porque *gatean
comportamiento* y se consultan ("¿qué conjuntos tienen paquetería?").

| Campo | Tipo | Notas |
| --- | --- | --- |
| `Id` | `Guid` | |
| `TenantId` | `Guid` | |
| `FeatureKey` | `enum` | `Requests`, `Deliveries`, `Visits`, `Announcements`, `WhatsApp`… |
| `Enabled` | `bool` | |

## 2.3 `Apartment` (Agg) — **`ITenantOwned`**

| Campo | Tipo | Notas |
| --- | --- | --- |
| `Id` | `Guid` | PK |
| `TenantId` | `Guid` | `ITenantOwned` |
| `Tower` | `string?` | Torre/bloque |
| `Number` | `string` | Identificador ("101") |
| `Type` | `enum` | `Apartment`, `House`, `Commercial`, `Parking`, `Storage` |
| `Status` | `enum` | `Occupied`, `Vacant` |
| `Attributes` | `json` | **JSON** — área, nº parqueaderos, etc. (no filtrable) |

**Índice único** `(TenantId, Tower, Number)`.

## 2.4 `ApartmentResident` (Ent bajo `Apartment`) — **`ITenantOwned`**

El vínculo persona↔apartamento. **Aquí vive propietario/arrendatario** (no es un rol RBAC).
Multipropietario soportado: varias filas con `RelationType = Owner` para el mismo apartamento.

| Campo | Tipo | Notas |
| --- | --- | --- |
| `Id` | `Guid` | |
| `ApartmentId` | `Guid` | |
| `TenantId` | `Guid` | |
| `UserId` | `Guid` | Valor crudo (de `Users`) |
| `RelationType` | `enum` | `Owner`, `Renter` — *(renombrado: `Renter` en vez de `Tenant` para no chocar con el conjunto)* |
| `LivesHere` | `bool` | Un propietario puede no habitar |
| `StartDate` / `EndDate` | `DateOnly?` | |
| `IsActive` | `bool` | |

## 2.5 Fachada `ITenantsModuleApi` (Contracts)

```
GetTenant(id)                          -> TenantDto?
GetApartment(id)                       -> ApartmentDto?
ApartmentExists(id, tenantId)          -> bool
GetApartmentResidents(apartmentId)     -> ResidentDto[]
IsFeatureEnabled(tenantId, featureKey) -> bool
```

## 2.6 Integration events (Contracts)

`TenantCreatedIntegrationEvent`, `ApartmentCreatedIntegrationEvent`,
`ResidentAssignedToApartmentIntegrationEvent`, `ResidentRemovedFromApartmentIntegrationEvent`.

---

# 3. Módulo `Operations`

PQRS + paquetería + visitas. Tres agregados independientes en un mismo módulo/schema.
Todos `ITenantOwned`. Guardan `UserId`/`ApartmentId` como valores crudos y validan existencia vía
`ITenantsModuleApi`/`IUsersModuleApi`.

## 3.1 `Request` (Agg) — Solicitud / PQRS

| Campo | Tipo | Notas |
| --- | --- | --- |
| `Id` | `Guid` | PK |
| `TenantId` | `Guid` | `ITenantOwned` |
| `Code` | `string` | Legible, secuencial por tenant: `SOL-2026-0001` |
| `Type` | `enum` | `Peticion`, `Queja`, `Reclamo`, `Sugerencia`, `Maintenance` |
| `Category` | `string?` | Subtipo libre por conjunto |
| `Title` | `string` | |
| `Description` | `string` | |
| `Location` | `string?` | Texto libre ("Torre 3, puerta principal"). Ayuda a issues de zona común y al matching por LLM |
| `Status` | `enum` | Ver ciclo de vida abajo |
| `Priority` | `enum` | `Low`, `Medium`, `High` |
| `CreatedByUserId` | `Guid` | Reportante original |
| `ApartmentId` | `Guid?` | Apartamento asociado (null si es zona común) |
| `AssignedToUserId` | `Guid?` | Colaborador responsable |
| `Metadata` | `json` | **JSON** — campos adicionales no filtrables |
| `CreatedAtUtc` / `UpdatedAtUtc` | `DateTimeOffset` | |
| `ResolvedAtUtc` / `ClosedAtUtc` | `DateTimeOffset?` | |

**Ciclo de vida** (punto intermedio, ni naïve ni sobre-diseñado):

```
New ──▶ InReview ──▶ InProgress ──▶ Resolved ──▶ Closed
  │         │             │             │
  └─────────┴─────────────┴──▶ Rejected / Cancelled
                                  Resolved ──▶ Reopened ──▶ InProgress
```

Transiciones validadas en el agregado; cada cambio deja fila en `RequestStatusHistory`.

### 3.1.1 Issue compartido + deduplicación por LLM

Escenario: la puerta X está dañada; el usuario A crea la solicitud, y luego B intenta crear otra por
lo mismo. El sistema **no** debe duplicar: un adaptador de entrada con LLM (puerto en `Application`,
ver `05-ports-and-adapters.md`) busca solicitudes **abiertas** del mismo tenant/apartamento/zona que
hagan match semántico y, si encuentra una:

- **No crea** una nueva `Request`; en su lugar **agrega a B como `RequestParticipant`** de la original.
- Opcionalmente registra lo que B describió como un `RequestUpdate` de tipo `Evidence`.
- B ahora recibe todas las actualizaciones **como si fuera propia** (es participante, no mero espectador).

El LLM es un adaptador; el **modelo de dominio** solo necesita: participantes, actualizaciones y
evidencia. Si no hay match → se crea la `Request` con el reportante como primer participante.

### 3.1.2 Hijos de `Request`

- **`RequestParticipant`** (Ent) — quiénes están "en" la solicitud y reciben actualizaciones.
  Reemplaza al antiguo `RequestFollower`.
  `Id`, `RequestId`, `UserId`, `ParticipantType` (`Reporter` | `Follower`),
  `Source` (`Self` | `AutoMatched` | `Admin`), `JoinedAtUtc`. Único `(RequestId, UserId)`.

- **`RequestUpdate`** (Ent) — la **línea de tiempo**. Reemplaza a `RequestResponse` y cubre tu punto:
  no toda entrada es una "respuesta final"; hay **avances** de administración y **más evidencia** de
  residentes. Cualquier participante (o staff) puede aportar.
  `Id`, `RequestId`, `AuthorUserId`, `Type`
  (`Progress` = avance del staff · `Comment` = comentario · `Evidence` = aporta pruebas ·
  `Resolution` = respuesta final), `Body?`, `IsInternal` (nota interna del staff vs. visible),
  `CreatedAtUtc`.

- **`RequestAttachment`** (Ent) — evidencia (fotos/archivos). Puede colgar de la solicitud o de un
  update concreto, así **un participante puede agregar evidencia**.
  `Id`, `RequestId`, `RequestUpdateId?`, `FileUrl`, `FileName`, `ContentType`, `UploadedByUserId`,
  `CreatedAtUtc`.

- **`RequestStatusHistory`** (Ent) — timeline de estados (consultable, da insight):
  `Id`, `RequestId`, `FromStatus`, `ToStatus`, `ChangedByUserId`, `ChangedAtUtc`, `Note?`.

## 3.2 `Delivery` (Agg) — Paquetería

Registrada por vigilante, asociada a un apartamento.

| Campo | Tipo | Notas |
| --- | --- | --- |
| `Id` | `Guid` | PK |
| `TenantId` | `Guid` | |
| `Code` | `string` | `PAQ-2026-0001` |
| `ApartmentId` | `Guid` | Destino |
| `Type` | `enum` | `Package`, `Letter`, `Envelope`, `Food`, `Other` |
| `Status` | `enum` | `Received` → `Notified` → `Delivered` / `Returned` |
| `RegisteredByUserId` | `Guid` | Vigilante |
| `PhotoUrl` | `string?` | Foto del producto |
| `Comment` | `string?` | |
| `Carrier` | `string?` | Transportadora |
| `ReceivedAtUtc` | `DateTimeOffset` | |
| `DeliveredAtUtc` | `DateTimeOffset?` | |
| `ReceivedByName` | `string?` | **Texto libre** — quién lo recogió (p. ej. "el hijo"). Este es el campo normal en la práctica |
| `DeliveredToUserId` | `Guid?` | Opcional y poco usual: solo si quien retira es un usuario del sistema |
| `Metadata` | `json` | **JSON** |

> **Facilidad real:** seleccionar el usuario exacto que retira casi nunca es viable (puede ser el
> hijo, la empleada, alguien sin cuenta). Por eso `ReceivedByName` (texto libre) es el camino por
> defecto y `DeliveredToUserId` es opcional.

## 3.3 `Visit` (Agg) — Visitas / ingresos

Registrada por vigilante, asociada a un apartamento.

| Campo | Tipo | Notas |
| --- | --- | --- |
| `Id` | `Guid` | PK |
| `TenantId` | `Guid` | |
| `ApartmentId` | `Guid` | Destino |
| `Type` | `enum` | `Visitor`, `Delivery` (domiciliario), `Service`, `Vehicle` |
| `Status` | `enum` | `InProgress` → `Finished` |
| `VisitorName` | `string` | |
| `VisitorDocument` | `string?` | |
| `PhotoUrl` | `string?` | Foto del visitante |
| `VehiclePlate` | `string?` | Cuando `Type = Vehicle` |
| `AmountPaid` | `decimal?` | **Opcional** — monto cobrado (p. ej. parqueadero de visitante) |
| `RegisteredByUserId` | `Guid` | Vigilante |
| `AuthorizedByUserId` | `Guid?` | Residente que autorizó (pre-autorización) |
| `EntryAtUtc` | `DateTimeOffset` | |
| `ExitAtUtc` | `DateTimeOffset?` | Para parqueadero/tiempos |
| `Metadata` | `json` | **JSON** |

## 3.4 Fachada `IOperationsModuleApi` (Contracts)

Lecturas cross-module (p. ej. dashboards super-admin): `GetRequestSummary`,
`GetOpenRequestsCount(tenantId)`… (mismo criterio que §1.8).

## 3.5 Integration events (Contracts)

`RequestOpenedIntegrationEvent`, `RequestUpdatedIntegrationEvent`,
`RequestStatusChangedIntegrationEvent`, `RequestClosedIntegrationEvent`,
`DeliveryRegisteredIntegrationEvent`, `DeliveryDeliveredIntegrationEvent`,
`VisitRegisteredIntegrationEvent`.
→ Los consume `Admin` (notificaciones) para avisar a residentes y participantes.

---

# 4. Módulo `Admin`

Agrupa **notificaciones** y **configuración** (temas administrativos, mismo schema/servicio).

## 4.1 `NotificationTemplate` (Agg)

`TenantId` **nullable**: `null` = plantilla global por defecto; con valor = override del conjunto
(solo el override es `ITenantOwned`).

| Campo | Tipo | Notas |
| --- | --- | --- |
| `Id` | `Guid` | |
| `TenantId` | `Guid?` | null = global; set = override |
| `Type` | `enum` | `RequestOpened`, `RequestUpdated`, `RequestClosed`, `DeliveryReceived`, `VisitRegistered`, `Announcement`… por ahora solo el correo de bienvenida `Welcome` es necesario. Despues se agregarán más | 
| `Channels` | `flags` | `Email`, `Push`, `InApp` |
| `EmailSubject` / `EmailBodyHtml` | `string?` | |
| `InAppText` / `PushText` | `string?` | |
| `IsActive` | `bool` | |
| `Localization` | `json` | **JSON** — traducciones por idioma |

## 4.2 Mensajes materializados — **NO `ITenantOwned`**

Igual que en pollaya: **solo `NotificationTemplate` es tenant-owned**. Estos tres son el **resultado**
de ejecutar el notification service — artefactos de procesamiento (outbox) que consume el servicio de
envío. Llevan `TenantId` como **columna simple** (para elegir SMTP/remitente y reportería), pero **no**
se scopean con `ForCurrentTenant`; se consultan por destinatario/estado.

- **`UserNotification`** (in-app): `Id`, `TenantId`, `RecipientUserId`, `Type`, `Title`, `Body`,
  `TargetUrl?`, `IsRead`, `ReadAtUtc?`, `TriggeredByUserId?`, `CreatedAtUtc`. Se lee por `RecipientUserId`.
- **`EmailMessage`** (outbox): `Id`, `TenantId`, `To`, `ToName?`, `Subject`, `BodyHtml`,
  `Priority` (`byte`), `Status` (`Pending`/`Sent`/`Failed`), `Attempts`, `ScheduledAtUtc?`, `SentAtUtc?`.
- **`PushMessage`** (outbox): `Id`, `TenantId`, `RecipientUserId`, `Title`, `Body`, `TargetUrl?`,
  `Platform` (`Android`/`iOS`), `Status`, `Attempts`, `DedupHash`, `SentAtUtc?`.

## 4.3 `Announcement` (Agg) — Boletín informativo — **`ITenantOwned`**

Broadcast admin→residentes (distinto de las notificaciones transaccionales).

| Campo | Tipo | Notas |
| --- | --- | --- |
| `Id` | `Guid` | |
| `TenantId` | `Guid` | |
| `Title` / `Body` | `string` | |
| `Priority` | `byte` | **Escala numérica** de orden de despliegue; **0 = prioridad máxima** |
| `PublishedAtUtc` | `DateTimeOffset?` | |
| `ExpiresAtUtc` | `DateTimeOffset?` | **Tiempo de vida**: tras esta fecha deja de mostrarse |
| `AudienceType` | `enum` | `AllTenant`, `ByTower`, `ByApartments` |
| `AudienceFilter` | `json` | **JSON** — torres/apartamentos objetivo |
| `Status` | `enum` | `Draft`, `Published`, `Archived` |
| `PublishedByUserId` | `Guid?` | |
| `Attachments` | (Ent) | igual patrón que `RequestAttachment` |

> "Vigente" = `Status = Published` **y** (`ExpiresAtUtc` nulo o futuro). Orden de despliegue por
> `Priority` ascendente (0 primero).

## 4.4 `DeviceRegistration` (Agg) — **system-level** (atado al user, no al tenant)

`Id`, `UserId`, `Platform`, `Token`, `IsActive`, `UpdatedAtUtc`.

## 4.5 `SystemSetting` (Agg) — configuración operativa

Nombre `SystemSetting` (no `Setting`) para evitar conflictos de nombre. Key/value con override por
tenant, caché en memoria e invalidación por evento (patrón pollaya, mejorado). **Las features por
conjunto NO viven aquí** — viven en `Tenants.TenantFeature`. Aquí: SMTP, API keys de WhatsApp, textos
por defecto, límites, etc.

| Campo | Tipo | Notas |
| --- | --- | --- |
| `Id` | `Guid` | |
| `Key` | `string` | Namespaced: `smtp.host`, `whatsapp.apiKey`, `requests.maxAttachments` |
| `TenantId` | `Guid?` | null = valor global; set = override por conjunto |
| `Value` | `json` | **JSON tipado** (no todo string) |
| `ValueType` | `enum` | `String`, `Int`, `Bool`, `Json` (para parseo/validación) |
| `UpdatedAtUtc` | `DateTimeOffset` | |

**Único** `(Key, TenantId)`. Lectura: override de tenant si existe, si no el global.
Acceso en runtime: `ISystemSettings` (en `Shared.Abstractions`) inyectable, lee de caché en memoria;
la escritura publica `SystemSettingChangedIntegrationEvent` y un consumer refresca la caché. Cambios
sin recompilar ni redeploy.

## 4.6 Consumers (Application, internos)

Un consumer por integration event relevante; traduce a un comando idempotente que crea los mensajes
según la plantilla y el canal. Ej.: `RequestOpenedConsumer` → notifica a los `RequestParticipant`
(reportantes + followers) y al responsable.

---

# 5. Cross-cutting

## 5.1 JSON vs. columna — la regla

- **Columna** si el campo se **filtra, ordena, agrupa o da insight** (estado, tipo, fechas, `TenantId`,
  documento, apartamento, prioridad). También todo lo que participe en FK interna o índice único.
- **JSON** (`Metadata`/`Attributes`/`Branding`/`Profile`) solo para datos que se **leen junto al
  agregado y nunca se consultan por sí solos**. Evita migraciones por cada campo cosmético.
- Historial y auditoría (`RequestStatusHistory`) van en **tabla**, no JSON: son consultables.

## 5.2 Códigos legibles (`Code`) — cómo se generan

Los agregados tienen dos identificadores distintos:
- `Id` (`Guid`) → la **PK técnica**, para relaciones y URLs internas.
- `Code` (`string`) → un **identificador legible para humanos** que se muestra al usuario y sirve para
  buscar/citar: `SOL-2026-0001` (solicitud), `PAQ-2026-0001` (paquete).

El `Code` es un **contador secuencial por conjunto** (no global, para que cada conjunto empiece en 1 y
no filtre volúmenes entre tenants). Ejemplo: la solicitud nº 42 del Conjunto A en 2026 → `SOL-2026-0042`.
El **cómo** se genera el contador atómicamente (secuencia dedicada, tabla de contadores por tenant, etc.)
es un detalle de la capa de **persistencia**, no del dominio; aquí solo fijamos el **formato y el alcance
(por tenant + año + tipo)**.

## 5.3 Comunicación entre módulos — resumen

| Necesidad | Mecanismo |
| --- | --- |
| WhatsApp resuelve residente por teléfono | `IUsersModuleApi.GetUserByPhone` (sync) |
| `Operations` valida que el apartamento existe | `ITenantsModuleApi.ApartmentExists` (sync) |
| `Operations` valida feature habilitada | `ITenantsModuleApi.IsFeatureEnabled` (sync) |
| Abrir/actualizar/cerrar solicitud → notificar | integration event → consumer en `Admin` (async) |
| Registrar paquete/visita → notificar residente | integration event → consumer en `Admin` (async) |
| Crear conjunto → sembrar config/plantillas default | integration event → consumer en `Admin` (async) |

## 5.4 WhatsApp (nota de arquitectura)

El chatbot es un **adaptador de entrada** (como un controller): traduce mensajes de WhatsApp en
comandos/queries de los módulos. Resuelve la identidad por **teléfono** (`GetUserByPhone`) y el
conjunto activo por las membresías del usuario. No es un módulo de dominio. La deduplicación de
solicitudes por LLM (§3.1.1) es otro adaptador de entrada del módulo `Operations`.

---

# 6. Decisiones resueltas (antes "preguntas abiertas")

1. **`Vigilante` y `AsistenteAdministracion` son roles separados.** ✅
2. **Nombre del módulo: `Operations`.** ✅
3. **Multipropietario permitido** (varias filas `ApartmentResident` con `Owner`). ✅
4. **`SuperAdmin` = rol de ámbito `Platform` con todos los permisos y acceso cross-tenant**, asignado
   por dato (`PlatformRoleAssignment`), no un módulo ni un usuario aparte. ✅
5. **Ejemplar canónico = `Users`** (primero usuarios; con ellos se crean tenants). ✅
