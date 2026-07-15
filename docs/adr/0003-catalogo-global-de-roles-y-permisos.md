# ADR-0003 — Catálogo global de roles y permisos

- **Estado:** Accepted
- **Fecha:** 2026-07-15

## Contexto

Todos los conjuntos ofrecen las mismas funcionalidades. No se quiere atar la definición de permisos al
tenant: si se agrega un permiso nuevo, debe aplicar a todos por igual. Un anti-patrón común es hacer
**CRUD de permisos** en BD: se crean permisos que ningún `if` del código verifica jamás.

## Decisión

- **`Permission`** es un **catálogo estático ligado al código** (seed por migración). Cada permiso
  existe porque hay código que lo verifica. No hay CRUD de permisos.
- **`Role`** es global y sembrado: `SuperAdmin`, `Administrador`, `AsistenteAdministracion`,
  `Vigilante`, `Residente`. `SuperAdmin` tiene todos los permisos y acceso cross-tenant.
- **`RolePermission`** (mapeo) es global. Agregar un permiso a un rol aplica en todos los conjuntos.
- Los permisos **no** viajan en el JWT; se resuelven en servidor desde caché (token pequeño, revocable).

## Consecuencias

- No hay roles/permisos "a medida" por conjunto — decisión explícita a favor de simplicidad y
  consistencia. Si en el futuro se necesitaran roles custom por tenant, requerirá un ADR que supersede.
- `SuperAdmin` no necesita módulo propio: es un rol + autorización cross-tenant.
- El catálogo de permisos crece junto con las capacidades del código, no por configuración.
