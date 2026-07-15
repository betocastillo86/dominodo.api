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
| [0002](./0002-rol-en-membership-no-en-user.md) | Rol asignado en Membership, no en User | Accepted |
| [0003](./0003-catalogo-global-de-roles-y-permisos.md) | Catálogo global de roles y permisos | Accepted |
| [0004](./0004-modulo-admin-agrupa-notificaciones-y-config.md) | Módulo `Admin` agrupa notificaciones y configuración | Accepted |
| [0005](./0005-modulo-operations-unificado.md) | Módulo `Operations` unifica PQRS, paquetería y visitas | Accepted |
