# Architecture Decision Records (ADR)

Registro **append-only** de las decisiones significativas de Dominodo. Un ADR captura el *porqué* de
una decisión en el momento en que se toma. **No se editan** una vez aceptados: si una decisión cambia,
se escribe un ADR nuevo que **supersede** al anterior (y se marca el viejo como `Superseded by ADR-NNNN`).

Esto mantiene el modelo de dominio (`docs/domain/00-domain-model.md`) limpio y *actual*, mientras la
historia y el razonamiento quedan aquí sin inflar el contexto del agente.

## Convenciones

- Archivo: `NNNN-titulo-en-kebab-case.md` (numeración incremental, 4 dígitos).
- Estados: `Proposed` · `Accepted` · `Superseded by ADR-NNNN` · `Deprecated`.
- Estructura: **Contexto** (qué problema/fuerzas) · **Decisión** (qué elegimos) · **Consecuencias**
  (qué ganamos y qué cedemos). Corto y concreto.

## Índice

| ADR | Título | Estado |
| --- | --- | --- |
| [0001](./0001-telefono-como-llave-de-identidad.md) | Teléfono como llave de identidad | Accepted |
| [0002](./0002-rol-en-membership-no-en-user.md) | Rol asignado por ámbito (Membership tenant / PlatformRoleAssignment platform) | Accepted |
| [0003](./0003-catalogo-global-de-roles-y-permisos.md) | Catálogo global de roles y permisos | Accepted |
| [0004](./0004-modulo-admin-agrupa-notificaciones-y-config.md) | Módulo `Admin` agrupa notificaciones y configuración | Accepted |
| [0005](./0005-modulo-operations-unificado.md) | Módulo `Operations` unifica PQRS, paquetería y visitas | Accepted |
| [0006](./0006-stack-mvp-sqlserver-jwt-login-password.md) | Stack del MVP: SQL Server, JWT access+refresh, login password-only | Accepted |
| [0007](./0007-otp-a-traves-del-modulo-admin.md) | OTP entregado a través del módulo `Admin` (WhatsApp con fallback email) | Accepted |
| [0008](./0008-rbac-scope-y-autoridad-de-plataforma-por-dato.md) | RBAC: `Role.Scope` y autoridad de plataforma por dato (`PlatformRoleAssignment`) | Accepted |
| [0009](./0009-bus-de-mensajeria-wolverine.md) | Bus de mensajería: Wolverine (reemplaza a MassTransit) | Accepted |
| [0010](./0010-autorizacion-basada-en-recurso-propiedad.md) | Autorización basada en recurso (propiedad) | Proposed |
| [0011](./0011-announcement-en-operations.md) | `Announcement` (comunicados) vive en `Operations`, no en `Admin` | Proposed |
| [0012](./0012-permisos-granulares-operations-y-creacion-por-membresia.md) | Permisos granulares de Operations y creación de solicitud por membresía | Proposed |
