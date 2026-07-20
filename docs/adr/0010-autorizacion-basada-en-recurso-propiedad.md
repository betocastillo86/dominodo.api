# ADR-0010 — Autorización basada en recurso (propiedad)

- **Estado:** Proposed
- **Fecha:** 2026-07-20

## Contexto

La autorización de apartamentos era gruesa: `ApartmentsController` protegía toda lectura con
`tenants.view` y toda escritura con `tenants.edit`. Surgieron dos necesidades:

1. **Granularidad RBAC para apartamentos.** "Gestionar apartamentos" debía poder otorgarse sin
   conceder "editar todo el conjunto" (perfil, features, residentes). El permiso `tenants.*` no lo
   permite.
2. **Un residente/propietario debe leer *su propio* apartamento sin tener `apartments.view`.** El
   modelo `[HasPermission]` (doc 12) es **ciego al recurso** por diseño: responde `(user, tenant) →
   bool`, nunca `(user, tenant, apartmentId) → bool`. Otorgar `apartments.view` filtraría *todos* los
   apartamentos del conjunto; no otorgarlo bloquea al residente de leer el suyo. Es una preocupación
   de **autorización basada en recurso (propiedad)** que no encaja en el atributo de permiso, y
   recurrirá en otros módulos (p. ej. PQRS: un usuario lee su propia solicitud). La solución debe ser
   un **patrón reutilizable**, no rutas `/mine` por endpoint, y debe extenderse de lectura a escritura
   sin rediseño.

## Decisión

- **Split de permisos de apartamentos.** Se dividió el `tenants.*` para apartamentos en
  `apartments.create` / `apartments.view` / `apartments.edit` (catálogo `Permissions`, seed de Users,
  migración `AddApartmentPermissions`, mirror E2E). SuperAdmin los hereda por la regla del seed.
- **`IResourceAccessAuthorizer` (guard reutilizable).** Port en `Shared.Abstractions`, impl
  `ResourceAccessAuthorizer` en `Shared.Infrastructure`. Resuelve **"el llamante tiene el permiso
  (RBAC) O el llamante es dueño de *este* recurso"**. La comprobación de propiedad es siempre un
  **chequeo de datos del mismo módulo** que el llamante entrega como delegate (nunca un port
  cross-module); se ejecuta **solo si el permiso está ausente** (short-circuit para staff). Falla
  cerrado si no hay llamante autenticado. Devuelve un booleano de acceso plano; el handler moldea el
  error de transporte.
- **`ICurrentUser` (seam de identidad).** Interfaz en `Shared.Kernel`, impl `HttpCurrentUser` en
  `Shared.Infrastructure` — espejo de `ITenantContext`. Expone el `UserId` del llamante de forma
  ambiental (lee `NameIdentifier ?? sub`), para que handlers y el guard no propaguen el claim `sub` a
  través de cada comando.
- **`GET /apartments/{id}` dual-mode.** Pasa de `[HasPermission(tenants.view)]` a `[Authorize]`; el
  handler carga el apartamento con sus residentes y aplica el guard: `apartments.view` (staff) O ser
  residente activo del apartamento. La denegación devuelve el **mismo `Apartment.NotFound`** que una
  fila inexistente — **leak-safe**, sin revelar existencia.

## Consecuencias

- Staff con `apartments.view` lee cualquier apartamento del conjunto (lista + detalle); crear requiere
  `apartments.create`; actualizar/estado requieren `apartments.edit`. Un residente sin `apartments.view`
  lee **solo** el suyo por la ruta canónica `GET /apartments/{id}`; cualquier otro id devuelve `404`
  idéntico (sin fuga de existencia).
- El patrón es **reutilizable**: cualquier módulo inyecta el guard y aporta su propio delegate de
  propiedad. Es transport-agnostic — el handler decide `403` vs `404` leak-safe.
- **Extensible a escritura** sin rediseño: basta cambiar el permiso (p. ej. `apartments.edit`) en un
  futuro handler de edición del residente; el eje de propiedad es idéntico.
- **Deferrals deliberados:** los endpoints de **residentes** y **features** de un apartamento siguen en
  `tenants.view`/`tenants.edit` por ahora (podrían migrar a un grupo `residents.*` más adelante); el
  residente **no** puede *editar* su apartamento todavía (solo se aplicó el eje de propiedad a la
  lectura).
- Encaja en las reglas de dependencia sin nuevas referencias de proyecto: `Tenants.Application` ya
  referencia `Shared.Abstractions` y `Shared.Kernel`. Complementa a la doc 12 y a ADR-0008 (RBAC).
