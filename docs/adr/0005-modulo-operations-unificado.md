# ADR-0005 — Módulo `Operations` unifica PQRS, paquetería y visitas

- **Estado:** Accepted
- **Fecha:** 2026-07-15

## Contexto

El planteamiento inicial ("PQRS") mezclaba solicitudes iniciadas por el residente (Request/Response)
con registros hechos por el vigilante (paquetería y visitas). Se evaluó separarlos en módulos distintos
por tener actores y ciclos diferentes, pero para el MVP se prefiere no sobre-atomizar.

## Decisión

- Un solo módulo **`Operations`** (schema `operations`) con tres agregados independientes:
  `Request` (PQRS, con ciclo de vida y participantes), `Delivery` (paquetería), `Visit` (visitas).
- El nombre reemplaza a "PQRS" (que no convencía).

## Consecuencias

- Un solo schema/`DbContext` para el día a día operativo del conjunto.
- Los agregados siguen siendo independientes entre sí (no comparten transacción con otros módulos).
- Publica integration events (`RequestOpened`, `DeliveryRegistered`, `VisitRegistered`, …) que consume
  `Admin` para notificar.
- Si a futuro paquetería/visitas justifican su propio ciclo de despliegue, se pueden separar con un ADR
  que supersede a este.
