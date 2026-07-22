# ADR-0011 — `Announcement` (comunicados) vive en `Operations`, no en `Admin`

- **Estado:** Proposed
- **Fecha:** 2026-07-21

## Contexto

ADR-0004 agrupó `Announcement` (el boletín/comunicados) dentro del módulo `Admin`, junto a las
notificaciones y la configuración. Al revisar el modelo se ve que eso mezcla dos responsabilidades
distintas:

- **Contenido de dominio:** un comunicado tiene ciclo de vida propio (`Draft → Published → Archived`),
  audiencia (`AllTenant`/`ByTower`/`ByApartments`), vigencia (`ExpiresAtUtc`), prioridad de despliegue y
  adjuntos. Es contenido que la administración **carga y gestiona** y que los residentes **consumen**.
- **Infraestructura de notificaciones:** `NotificationTemplate`, mensajes materializados
  (`InAppMessage`/`EmailMessage`/`PushMessage`), `DeviceRegistration` y la elección de canal/SMTP.
  Es *cañería de entrega*.

*Cargar noticias* es una capacidad operativa del día a día del conjunto (misma familia que
solicitudes, paquetería y visitas). *Entregar el aviso* de que hay una noticia nueva es un tema de
notificaciones. ADR-0004 las había fusionado por comodidad de MVP, dejando a `Admin` como un cajón de
sastre.

## Decisión

- El agregado **`Announcement`** (con `Attachments`) se mueve al módulo **`Operations`** (schema
  `operations`), como cuarto agregado independiente junto a `Request`, `Delivery` y `Visit`. Sigue
  siendo `ITenantOwned`.
- La **entrega** del aviso permanece en `Admin`: al pasar a `Published`, `Operations` publica
  `AnnouncementPublishedIntegrationEvent`; un consumer en `Admin` (`AnnouncementPublishedConsumer`)
  materializa las notificaciones según la audiencia. Reads sync por fachada, writes cross-module async
  por integration event — el mismo patrón que `RequestOpened → Admin`.
- La plantilla `NotificationTemplate.Type = Announcement` **se queda en `Admin`**: es la plantilla del
  *aviso*, no el comunicado.
- Se descartó crear un módulo `Content`/`Communications` propio: sobre-atomiza para el MVP, en contra
  del espíritu de ADR-0004/0005.

## Consecuencias

- `Operations` posee el **comunicado**; `Admin` posee el **envío**. La frontera queda limpia: contenido
  operativo separado de la cañería de notificaciones.
- Sin FK cruzada ni schema compartido: `AudienceFilter` guarda Guids crudos de torres/apartamentos de
  `Tenants`, resolubles por `ITenantsModuleApi` si hiciera falta.
- **Revisa ADR-0004:** su ítem "`Announcement` (boletín)" en la lista de contenidos de `Admin` queda
  sin efecto; el resto de ADR-0004 (Admin agrupa notificaciones + configuración) sigue vigente.
- **Extiende ADR-0005:** `Operations` pasa de tres a cuatro agregados independientes.
- Actualizado en `docs/domain/00-domain-model.md` (v3): §3.4 (`Announcement` en `Operations`), §3.6
  (nuevo integration event), §4 (removido de `Admin`, renumerado), §5.3 (nueva fila cross-module).
