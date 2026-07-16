1. Resumen general

Aplicación que permita facilitar a los administradores de los conjuntos residenciales su día a día, la interacción con los propietarios/arrendatarios y diferentes proveedores. Dominodo debe ser una solución simple y fácil de usar para los residentes, así como debe mejorar  la productividad de los administradores de los conjuntos.

2. Dominio:
    1. Conjunto residencial/edificio: Tenant o unidad que se puede ver como un cliente que tiene acceso todas o algunas features que Dominodo ofrece. Internamente cada conjunto tiene una serie de elementos asociados, como usuarios y apartamentos.
    2. Apartamento/Casa: Unidad fisica de un edificio que se encuentra inscrita en un conjunto. Normalmente tiene un identificador y un propietario responsable por esta.
    3. Administrador: Es la persona/empresa encargada de un conjunto, tiene la responsabilidad de administrar la información y la organización de un conjunto.
    4. Colaborador: Es un trabajador que hace parte del conjunto. Puede ser un vigilante, aseador, o asistente del administrador.
    5. Residentes
        1. Propietario: es el que aparece como responsable/dueño de un apartamento. También puede al mismo tiempo vivir en la unidad.
        2. Arrendatario: es una persona que habita temporalmente un apartamento, tiene la capacidad de realizar diferentes acciones en el conjunto y en el apartamento, pero no es el dueño del mismo. Asi que tendrá algunas restricciones.
3. Aplicaciones
    1. Web superadministracion (devs, owners): Desde acá se podrá administrar todo lo existente en el sistema. Desde crear nuevos conjuntos
    2. Web de administradores/colaboradores: Sitio desde el que los administradores y colaboradores pueden interactuar con acciones relacionadas con el conjunto. Cómo invitar propietarios, Crear notificaciones, registrar paquetes, etc.
    3. Web propietarios/arrendatarios: Desde acá los que viven en los apartamentos podrán gestionar sus solicitudes desde un navegador web.
    4. Integración de Whatsapp (Chatbot): Desde acá los residentes podrán gestionar sus solicitudes desde whatsapp, esto estará integrado directamente con el sistema, no solo será un chat de pregunta/respuesta.
4. Tipos de roles y accesos
    1. Superadministradores
    2. Administradores
    3. Colaboradores
        1. Vigilantes
        2. Asistente de administración
    4. Propietarios
    5. Arrendatarios
5. Funcionalidades iniciales MVP
    1. Administración de usuarios: CRUD de usuarios con roles. 
    2. Administración de roles y permisos: CRUD de roles y permisos.
    3. Autenticación de usuarios
    4. Administración de conjuntos residenciales (Tenant): CRUD de conjuntos residenciales con las unidades/apartamentos existentes.
    5. Administración de notificaciones/boletín informativo: Permite a los administradores de los conjuntos notificar a los residentes de noticias en el conjunto.
    6. Administración de notificaciones: Permite configurar el tupo de notificaciones a nivel de sistema. Esto es a nivel de CRUD tanto el sistema que las puede disparar. Notificaciones por correo, sistema y por mobile.
    7. Administración de PQRs: Permite el CRUD de los PRQS como los diferentes cambios de estado existentes en un sistema como este.
    8. Paquetería: Modulo que permite registrar paquetes ingresando en el conjunto para los diferentes apartamentos.
    9. Visitas: Moduilo que permite registrar visitas e ingresos a parqueadersos de visitantes en los apartamentos