# ADR-0007 — OTP entregado a través del módulo `Admin` (WhatsApp con fallback email)

- **Estado:** Accepted
- **Fecha:** 2026-07-16

## Contexto

La verificación de teléfono en el registro necesita entregar un código OTP. `Users` podría llamar
directamente a un adaptador de WhatsApp, pero eso acoplaría la identidad al canal de mensajería y
duplicaría la lógica de entrega/plantillas que ya es responsabilidad de `Admin` (Notifications, ver
ADR-0004). También queremos que la entrega sea desacoplada y reintentable, no en la ruta HTTP del
registro.

## Decisión

- **`Users` genera, almacena (hasheado) y verifica** el código (`VerificationCode`: expiración,
  intentos máximos). **No** entrega.
- La entrega va **a través de `Admin`**: `Users` publica `UserOtpRequestedIntegrationEvent`
  (con `Code`, `Phone`, `Email`, `HasWhatsApp`) por el bus; un handler en `Admin` lo traduce a un
  comando idempotente que renderiza el mensaje y lo envía.
- **Canal = WhatsApp**, con **fallback a email** cuando el usuario no tiene WhatsApp. Los adaptadores
  (`Adapters.WhatsApp`, `Adapters.Email`) implementan puertos de `Shared.Abstractions` y se cablean
  sólo en el host.
- **Idempotencia:** el handler dedupe por el id del evento (`NotificationDelivery.SourceEventId`, índice
  único) — la entrega es at-least-once.

## Consecuencias

- `Users` no conoce canales ni proveedores: sólo el hecho "se pidió un OTP". La identidad queda limpia.
- La entrega sobrevive a fallos transitorios (reintentos del bus) sin bloquear el registro.
- Credenciales reales de proveedor quedan diferidas (los adaptadores están detrás de puertos y se
  stubbean con WireMock); cablear un proveedor real es sólo configuración.
