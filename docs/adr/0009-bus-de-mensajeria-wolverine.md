# ADR-0009 — Bus de mensajería: Wolverine (reemplaza a MassTransit)

- **Estado:** Accepted
- **Fecha:** 2026-07-16

## Contexto

El slice arrancó con **MassTransit** para los integration events (transporte in-memory + outbox EF por
módulo). Dos problemas aparecieron en ejecución: (1) **MassTransit pasó a licencia comercial en v9** y
9.1.2 lanza `ConfigurationException: License must be specified...` al construir el bus — el host no
arranca; (2) el outbox EF de MassTransit no operaba en el transporte in-process (había que dejar
`UseBusOutbox()` apagado). Necesitábamos un bus que corriera gratis, arrancara el host y tuviera outbox
transaccional funcionando in-process para nuestro modelo de un `DbContext`/un schema por módulo.

## Decisión

- **Bus = Wolverine** (WolverineFx 6.x, MIT). Reemplaza a MassTransit por completo.
- **Outbox durable in-process**: cada módulo enrola su `DbContext` con
  `AddDbContextWithWolverineIntegration<T>` + un **message store ancillary** por módulo
  (`PersistMessagesWithSqlServer(cs, MessageStoreRole.Ancillary).Enroll<T>()`); el almacenamiento de
  envelopes vive en el schema `wolverine`. Transporte = **durable local queues** (cambiar a RabbitMQ /
  Azure Service Bus es sólo configuración).
- **MediatR se mantiene** para dispatch intra-módulo y domain events; Wolverine sólo transporta
  integration events cross-module.
- Modular monolith v5+: `MultipleHandlerBehavior.Separated` (cada módulo su propia tx + reintentos) y
  `MessageIdentity.IdAndDestination` (mismo evento entregado a varios módulos).

## Consecuencias

- El host arranca sin licencia; el outbox transaccional funciona hoy in-process.
- **Detalles no obvios que quedan como convención** (ver `docs/architecture/07`): se requiere el paquete
  `WolverineFx.RuntimeCompilation` (el core ya no trae el compilador Roslyn); los handlers deben ser
  **públicos** (el código generado no accede a tipos internos) e inyectar dependencias como **parámetros
  del método** `Handle`; se registran explícitamente por módulo vía `IncludeType<T>`; y se fija
  `ServiceLocationPolicy.AlwaysAllowed` porque el `ISender` de MediatR se registra por factory.
- Se removió MassTransit de todos los proyectos y sus tablas de inbox/outbox de los DbContexts
  (migraciones regeneradas). Supersede el uso de MassTransit implícito en versiones previas del doc 07.
