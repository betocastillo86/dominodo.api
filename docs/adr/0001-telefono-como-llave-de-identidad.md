# ADR-0001 — Teléfono como llave de identidad

- **Estado:** Accepted
- **Fecha:** 2026-07-15

## Contexto

Una persona debe poder existir **una sola vez** y participar en varios conjuntos (residente en uno,
administrador en otro). El canal principal del MVP para residentes es **WhatsApp**, que identifica al
usuario por su número telefónico. En pollaya la llave era el email; aquí el teléfono es el punto de
contacto natural y el que resuelve al usuario en el chatbot.

## Decisión

- `User.Phone` (E.164) es **obligatorio y único**: la llave natural de identidad.
- `User.Email` es **opcional y único si se provee**.
- La identidad es **global** (un `User`); la pertenencia a cada conjunto se modela en `Membership`.
- Verificación de teléfono por **OTP** (SMS/WhatsApp); el password (bcrypt/argon2, salt embebido) es
  opcional para residentes que solo usan WhatsApp.

## Consecuencias

- El adaptador de WhatsApp resuelve al residente con `IUsersModuleApi.GetUserByPhone`.
- Un mismo teléfono no puede tener dos cuentas → refuerza "una persona = un registro" y evita el truco
  de cuentas duplicadas (que con email sería trivial y con teléfono es indeseable).
- Requiere flujo de verificación OTP y normalización estricta a E.164.
