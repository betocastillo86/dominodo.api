# ADR-0002 — Rol asignado en Membership, no en User

- **Estado:** Accepted
- **Fecha:** 2026-07-15

## Contexto

Una persona puede tener distinta relación según el conjunto: residente en el Conjunto A y
administradora contratada en el Conjunto B. En pollaya `User.RoleId` es un escalar (un rol global por
persona), lo que no modela este caso. Además, la llave de identidad es el **teléfono** (ADR-0001), así
que el workaround de "cuenta de trabajo vs. personal" (dos emails) no aplica limpiamente (exigiría dos
teléfonos).

Aparte, se decidió que **propietario/arrendatario no son roles RBAC** sino el tipo de vínculo con un
apartamento (vive en `ApartmentResident.RelationType`).

## Decisión

- El `RoleId` se asigna en `Membership` (usuario↔conjunto↔rol), **no** en `User`.
- Un usuario tiene, por cada conjunto al que pertenece, exactamente un rol (`Membership` único por
  `(UserId, TenantId)`).
- Las **definiciones** de rol/permiso siguen siendo globales (ADR-0003); lo que varía por conjunto es
  únicamente *cuál* rol tiene la persona ahí.

## Consecuencias

- Misma complejidad de tablas que ponerlo en `User` (un FK, en otra tabla), pero modela la realidad.
- El token es *tenant-scoped*: al elegir conjunto, lleva el rol de ese `Membership`.
- Residente multi-conjunto y "propietario en A / arrendatario en B" se resuelven sin duplicar usuario.
