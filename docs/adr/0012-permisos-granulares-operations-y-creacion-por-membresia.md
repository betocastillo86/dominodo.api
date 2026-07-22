# ADR-0012 — Permisos granulares de Operations y creación de solicitud por membresía

- **Estado:** Proposed
- **Fecha:** 2026-07-22

## Contexto

Al aterrizar el módulo `Operations` (PQRS/paquetería/visitas/comunicados) el seed traía permisos
**gruesos y sin usar** para el dominio: `requests.create/manage`, `deliveries.register/manage`,
`visits.register`, `announcements.manage` (ids 3–8). No encajaban con el estilo fino ya adoptado en
`tenants.*` / `apartments.*` (ADR-0010) ni cubrían dos necesidades:

1. **Granularidad por acción** — ver, editar, gestionar el ciclo de vida y borrar deben poder otorgarse
   por separado (p. ej. un `Vigilante` registra paquetería/visitas pero no gestiona PQRS).
2. **Abrir una PQRS es una acción de residente/miembro, no una capacidad RBAC.** Exigir un permiso para
   `POST /requests` obligaría a conceder a cada residente un permiso de plataforma/tenant; el residente
   simplemente debe poder reportar en un conjunto donde tiene membresía activa.

Las lecturas por propiedad (solicitud propia/participante, delivery/visit del residente del apartamento)
ya están resueltas por `IResourceAccessAuthorizer` (ADR-0010); aquí no se reinventan.

## Decisión

- **Catálogo granular (13 códigos).** Se retiran los 6 códigos gruesos (ids 3–8) y se agregan como ids
  **23–35** (2-dígito-safe para el template de fixture E2E `…10{Id:D2}`, dejando estables los ids no
  relacionados): `requests.{view,edit,manage,delete}`, `deliveries.{view,edit,create}`,
  `visits.{view,edit,create}`, `announcements.{view,edit,create}`. Migración
  `GranularOperationsPermissions` (delete+insert de `Permission`/`RolePermission`); mirror E2E en
  `DominodoConstants`. Namespaces **plurales** para alinear con `tenants.*`/`apartments.*`.
- **Estado/transiciones bajo `*.edit`, salvo Requests.** Delivery y Visit hacen sus cambios de estado
  con `deliveries.edit` / `visits.edit`; solo `Request` conserva un `requests.manage` distinto para el
  ciclo de vida (New→…→Closed, assign, reject, reopen…).
- **Creación de solicitud por membresía, no por permiso.** `POST /requests` = `[Authorize]` + tenant
  resuelto (`X-Tenant`) + membresía **Active** en él (verificada en el handler vía
  `IUsersModuleApi.GetMemberships`); `CreatedByUserId` = usuario actual. `GET /announcements/mine` es
  igualmente auth-only (audiencia del llamante). Todo lo demás usa `[HasPermission(...)]`.
- **Asignación de roles (seed).** `Administrador` los 13; `AsistenteAdministracion` todos salvo
  `requests.delete`; `Vigilante` `deliveries.{view,edit,create}` + `visits.{view,edit,create}`;
  `Residente` ninguno (depende de propiedad + creación por membresía + `/mine`); `SuperAdmin` todos por
  superset de plataforma.

## Consecuencias

- El resolver de permisos efectivos devuelve los códigos granulares para los roles sembrados (p. ej.
  `deliveries.create` para `Vigilante`); los códigos gruesos desaparecen del catálogo y del mirror.
- Un residente sin permisos `operations.*` puede abrir una PQRS (membresía), leer su propia solicitud
  (participante) y su delivery/visita (residente del apartamento), y ver comunicados vía `/mine`.
- Encaja en las reglas de dependencia sin nuevas referencias: `Operations.Application` referencia
  `Shared.Abstractions`/`Shared.Kernel` y los `*.Contracts` de `Tenants`/`Users`. Complementa a la
  doc 12, ADR-0008 (RBAC) y ADR-0010 (propiedad).
- **Deferrals deliberados:** la deduplicación semántica de PQRS por LLM (§3.1.1) y el cableado de los
  consumers de `Admin` (que ahora ya pueden suscribirse a `Operations.Contracts`) quedan como follow-up.
