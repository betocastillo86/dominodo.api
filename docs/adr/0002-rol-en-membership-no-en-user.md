# ADR-0002 — Rol asignado por ámbito (Membership tenant / PlatformRoleAssignment platform)

- **Estado:** Accepted
- **Fecha:** 2026-07-16

## Contexto

Una persona puede tener distinta relación según el conjunto: residente en el Conjunto A y
administradora contratada en el Conjunto B. En pollaya `User.RoleId` es un escalar (un rol global por
persona), lo que no modela este caso. Además, la llave de identidad es el **teléfono** (ADR-0001), así
que el workaround de "cuenta de trabajo vs. personal" (dos emails) no aplica limpiamente (exigiría dos
teléfonos).

Aparte, se decidió que **propietario/arrendatario no son roles RBAC** sino el tipo de vínculo con un
apartamento (vive en `ApartmentResident.RelationType`).

Pero asignar el rol *solo* dentro de un conjunto deja fuera las capacidades que existen **sin tenant** y
que **preceden** a cualquier conjunto: crear/configurar conjuntos (`tenants.create`, `tenants.manage`),
gestionar el catálogo global de roles, operar cross-tenant. Cuando se crea el primer conjunto aún no
hay ningún tenant al que colgar el permiso. Un `SuperAdmin` "usuario mágico" hardcodeado sería el
anti-patrón a evitar: SuperAdmin debe ser un **rol** del sistema (ADR-0003), no un caso especial del
código.

La pieza que faltaba es el **ámbito (scope) de asignación de rol**: además del ámbito *tenant* hace
falta un ámbito *plataforma*.

## Decisión

- **`Role` lleva un campo `Scope`** (`Platform` | `Tenant`). El scope describe **cómo se asigna el
  rol**, no qué permisos agrupa (un rol `Platform` puede contener permisos de tenant).
- El rol se asigna **según su ámbito**, sobre el mismo catálogo global (ADR-0003):
  - **Ámbito `Tenant` → `Membership`** (usuario↔conjunto↔rol, `ITenantOwned`). Un usuario tiene, por
    cada conjunto al que pertenece, exactamente un rol (`Membership` único por `(UserId, TenantId)`).
  - **Ámbito `Platform` → `PlatformRoleAssignment`** (usuario↔rol, system-level, sin tenant). Aquí
    vive la autorización de plataforma.
- El rol **nunca** se asigna en `User`.
- **`SuperAdmin` = rol `Platform` con todos los permisos, asignado por DATO** (una fila
  `PlatformRoleAssignment` sembrada), no por constante en el código. El login deriva los claims de rol
  de las asignaciones reales del usuario.

## Consecuencias

- Residente multi-conjunto y "propietario en A / arrendatario en B" se resuelven sin duplicar usuario.
- El token es *tenant-scoped*: al elegir conjunto lleva el rol de ese `Membership`; además el usuario
  puede portar roles de plataforma. Los permisos efectivos = permisos de plataforma ∪ permisos del
  `Membership` del tenant activo (resueltos en servidor, ADR-0003).
- SuperAdmin es un rol de primera clase, no un usuario especial. Añadir otro rol de plataforma más
  acotado (p. ej. uno que solo cree conjuntos) es sembrar datos, sin tocar código. Su acceso
  cross-tenant sale de tener todos los permisos, no de un bypass hardcodeado.
- Hay **dos** tablas de asignación (platform y tenant) en vez de una. Es deliberado: cada una tiene
  semántica y ciclo de vida propios (el `Membership` tiene flujo de invitación; la de plataforma no),
  y evita un `TenantId` nullable que rompería `ITenantOwned`.
