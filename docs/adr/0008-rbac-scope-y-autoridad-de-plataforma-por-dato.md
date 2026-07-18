# ADR-0008 — RBAC: `Role.Scope` y autoridad de plataforma por dato (`PlatformRoleAssignment`)

- **Estado:** Accepted
- **Fecha:** 2026-07-16

## Contexto

El slice inicial sembró un modelo provisional: los roles no tenían ámbito y el claim `role=SuperAdmin`
del token se otorgaba por una **comprobación de id hardcodeada** (`user.Id == SuperAdminUserId`). Eso es
un bypass que no escala (no se puede dar autoridad de plataforma a otro usuario sin tocar código) y
contradice la decisión §3/§6 del modelo de dominio (el rol se asigna por ámbito, `SuperAdmin` es dato).

## Decisión

- Cada **`Role` tiene un `Scope`**: `Platform` o `Tenant` (almacenado como `int`). Los roles sembrados:
  `SuperAdmin = Platform`; `Administrador`/`AsistenteAdministracion`/`Vigilante`/`Residente` = `Tenant`.
- La **autoridad de plataforma vive en `PlatformRoleAssignment`** (usuario↔rol global). El `SuperAdmin`
  bootstrap es **una fila sembrada** de esta tabla (Guid fijo), no una constante ni un `if` en el código.
- Los **claims `role` del access token se derivan** de las asignaciones de ámbito `Platform` del usuario
  (join `PlatformRoleAssignment` → `Role` filtrado por `Scope = Platform`). El acceso cross-tenant del
  SuperAdmin **sale de tener los permisos**, no de una bandera.
- Se agregaron los permisos de plataforma del grupo `Plataforma`: `tenants.create`, `tenants.view` y
  `tenants.edit` (originalmente `tenants.create` + `tenants.manage`; este último se dividió en
  view/edit al implementar el módulo `Tenants`).
- La política de autorización `SuperAdmin` (endpoints de escritura de roles) verifica ese claim derivado.

## Consecuencias

- Dar autoridad de plataforma a otro usuario es insertar una fila, no desplegar código. Se eliminó
  `PlatformConstants` del código.
- El token puede llevar **múltiples** claims `role` (la firma de `IJwtTokenGenerator` pasó de un `role?`
  a `IEnumerable<string>`).
- La parte tenant de la autorización (`Membership`, `GetEffectivePermissions`) se difiere; hoy la fachada
  sólo expone `GetPlatformPermissions`. Reafirma y concreta ADR-0002 y ADR-0003.
