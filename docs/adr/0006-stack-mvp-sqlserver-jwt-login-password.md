# ADR-0006 — Stack del MVP: SQL Server, JWT access+refresh, login password-only

- **Estado:** Accepted
- **Fecha:** 2026-07-16

## Contexto

El primer slice vertical (`Users` + `Admin`) necesitaba concretar tres decisiones de stack que los docs
de arquitectura dejaban abiertas (mostraban Npgsql como ejemplo): motor de datos, formato de token y
método de login. Había que elegir algo que corriera localmente en macOS y sirviera para el MVP sin
sobre-diseñar.

## Decisión

- **Motor de datos: SQL Server** (`Microsoft.EntityFrameworkCore.SqlServer`), una sola base, **un schema
  por módulo**, historial de migraciones por schema (`__ef_migrations`). En local corre en un contenedor
  Docker (no hay LocalDB en macOS).
- **Tokens: JWT access + refresh.** El access token es corto; el refresh token se guarda **hasheado**,
  con **rotación** (revoca el anterior, `ReplacedByTokenId`) y **revocación** (logout).
- **Login: sólo teléfono + contraseña.** El OTP se usa **únicamente** para verificar el teléfono en el
  registro (no para login). Hash de contraseña con **BCrypt.Net-Next** (work factor 11).

## Consecuencias

- Los docs `06`/`11` que mostraban Npgsql se actualizaron a SQL Server. Cambiar de motor luego es
  trabajo real (SQL específico, tipos), pero el patrón EF/schema-por-módulo se mantiene.
- El token no lleva permisos (se resuelven en servidor); ver ADR-0008 para los claims de rol.
- Login password-only mantiene el MVP simple; un futuro login por OTP o federado requerirá otro ADR.
- Las credenciales del secreto JWT y proveedores viven en configuración; en producción van a un secreto.
