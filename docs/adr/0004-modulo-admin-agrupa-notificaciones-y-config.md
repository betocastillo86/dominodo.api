# ADR-0004 — Módulo `Admin` agrupa notificaciones y configuración

- **Estado:** Accepted
- **Fecha:** 2026-07-15

## Contexto

En el monolito modular, cada módulo tiene su propio schema y su propio `DbContext`, y es un candidato a
servicio independiente en el futuro. Notificaciones y configuración son temas **administrativos** y de
soporte; separarlos en dos módulos atómicos agrega andamiaje sin beneficio claro para el MVP.

## Decisión

- Un solo módulo **`Admin`** (schema `admin`, un `DbContext`) que contiene:
  - Notificaciones: `NotificationTemplate`, mensajes materializados (`InAppMessage`,
    `EmailMessage`, `PushMessage`), `Announcement` (boletín), `DeviceRegistration`.
  - Configuración: `SystemSetting` (key/value, global + override por tenant, con caché invalidada por
    evento).

## Consecuencias

- Notificaciones y configuración viajan juntas si algún día se extraen a un servicio.
- Menos módulos que mantener en el MVP.
- Coherente con la clasificación de multitenancy: solo `NotificationTemplate` (override) y
  `SystemSetting` (override) y `Announcement` son tenant-owned; los mensajes materializados son
  artefactos de procesamiento con `TenantId` como columna simple, no scopeados.
- Si notificaciones creciera mucho, podría separarse luego con un ADR que supersede a este.
