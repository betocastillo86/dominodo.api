# Cómo organizar el repo para desarrollar con IA (Claude Code) al máximo

> **Qué es y qué NO es este documento.** Es una guía de **organización del repositorio y del entorno de trabajo** para que un agente de código (Claude Code) sea el motor de desarrollo más efectivo posible. **No** prescribe arquitectura de implementación, patrones, ni cómo hacer APIs — eso lo defines tú después. Todo aquí es **agnóstico de la arquitectura que elijas**: es la capa de "andamiaje para la IA" que envuelve *cualquier* arquitectura.

> **Fuentes** (2025–2026): documentación oficial de Claude Code (`code.claude.com/docs`), Anthropic Engineering (best practices, context engineering), y prácticas de spec-driven / agent-driven development. URLs al final.

---

## El único principio del que todo se deriva

Un agente de código está **cuellobotellado por el contexto** y **descuellobotellado por la verificación**.

- **Contexto:** "la ventana de contexto de Claude se llena rápido y el desempeño se degrada a medida que se llena". El objetivo es *la menor cantidad de tokens de alta señal* que maximice el resultado deseado. Organiza el repo para que el agente encuentre lo justo, rápido, y no tenga que leer de más para entender de menos.
- **Verificación:** "Claude se detiene cuando el trabajo *parece* hecho. Sin un chequeo, *tú* te vuelves el bucle de verificación. Dale algo que produzca pass/fail y el bucle se cierra solo".

Cada decisión de organización de abajo sirve a uno de esos dos ejes: **subir la señal** o **cerrar el bucle**. Nada más.

---

## 1. Estructura y descubribilidad del repo (agnóstica de arquitectura)

**Principio.** La organización de archivos *es* una herramienta de contexto: jerarquías, nombres y ubicación son señales que el agente usa para navegar just-in-time por glob/grep. Sea cual sea tu arquitectura, organízala para que el agente cargue *exactamente* lo que necesita.

**Qué establecer (independiente del patrón que elijas después):**
- **Cohesión por cercanía.** Que lo que cambia junto viva junto. Cuando una unidad de trabajo cabe en una carpeta, el agente carga esa carpeta y nada más → contexto mínimo, señal máxima. (Si eliges vertical slices, feature folders, o lo que sea — el objetivo organizacional es el mismo.)
- **Archivos pequeños, de responsabilidad única.** Un agente razona mejor sobre archivos cortos; también genera diffs más pequeños y revisables.
- **Nombres predecibles y consistentes.** Convenciones estrictas y uniformes (`*Handler`, `*Endpoint`, `*Tests`, etc.) hacen que los globs del agente sean confiables y que copie el patrón correcto. El agente *imita*: la consistencia es lo que hace que imite bien.
- **Un ejemplar canónico escrito a mano.** El primer módulo/feature hecho por ti a mano es el patrón que el agente (y tus skills) copiarán mil veces. Es la inversión de organización de mayor retorno: fija el estándar una vez.
- **Colocación de tests junto al código** que prueban → el agente encuentra y actualiza el test al lado sin buscar.
- **Monorepo/monolito para un dev solo con IA.** Claude carga automáticamente los `CLAUDE.md` ancestros y opera dentro de una sola frontera de contexto; los cambios transversales son triviales. Cada repo separado es una frontera dura que el agente no cruza sin `--add-dir`. Ve a multi-repo solo cuando algo *despliegue* de forma independiente.

---

## 2. El árbol `.claude/` — el sistema nervioso del repo para IA

Este es el corazón de tu pregunta. El directorio `.claude/` (versionado en git) es lo que convierte un repo cualquiera en un repo que un agente conduce bien. Modelo mental:

| Mecanismo | Qué es | Cuándo carga |
|---|---|---|
| `CLAUDE.md` | Instrucción persistente siempre-activa | Cada sesión (¡cuesta tokens siempre!) |
| `.claude/rules/*.md` | Reglas con alcance por `paths:` | On-demand según archivos tocados |
| `CLAUDE.md` de subdirectorio | Contexto local de un módulo | On-demand al leer ahí |
| `.claude/skills/` | Conocimiento/flujos reutilizables | On-demand cuando aplica |
| `.claude/agents/` (subagents) | Contexto aislado + toolset propio | Cuando delegas |
| `.claude/settings.json` (hooks) | Enforcement determinista | En eventos (edit, stop, etc.) |
| `.mcp.json` | Conexión a herramientas externas | Sesión |

### 2.1 `CLAUDE.md` — instrucción, no documentación
- Se carga **cada sesión** → un archivo inflado *degrada* al agente: "los CLAUDE.md inflados hacen que Claude ignore tus instrucciones reales". Contraintuitivo: más reglas → *menos* obedecidas.
- **Objetivo <200 líneas.** Prueba de cada línea: *"¿quitar esto haría que Claude cometa un error?"* Si no, se va.
- **Incluye:** comandos que el agente no puede adivinar (build, test, migraciones, cómo levantar local), convenciones que difieren del default, etiqueta de repo/branch/PR, punteros a dónde vive cada cosa.
- **Excluye:** lo inferible leyendo código, convenciones estándar del lenguaje, docs largas (enlázalas), y — clave — las reglas *must-always-happen* (esas van a un **hook**, no aquí; CLAUDE.md es contexto, no enforcement).

### 2.2 `.claude/rules/` con `paths:` (el refinamiento moderno)
- Divide temas en archivos con frontmatter `paths:` → una regla carga **solo** cuando el agente toca archivos que matchean. Así mantienes el contexto siempre-activo mínimo y la especificidad alta donde importa.

### 2.3 `CLAUDE.md` por subdirectorio (la palanca del monorepo)
- Root + ancestros cargan completos al inicio; **los de subdirectorio cargan on-demand** al leer ahí. Un módulo con idioms propios lleva su `CLAUDE.md` local que solo cuesta tokens cuando el agente trabaja en él.

### 2.4 Skills — flujos repetidos empaquetados
- `.claude/skills/<nombre>/SKILL.md` carga solo cuando es relevante → guarda el detalle que inflaría el CLAUDE.md. Ejemplos de organización: un skill que *scaffoldea un nuevo módulo desde tu ejemplar canónico*, otro para tareas mecánicas recurrentes. Codifica tu "cómo se hacen las cosas aquí" en algo ejecutable.

### 2.5 Subagents — contexto aislado
- `.claude/agents/`: ventana de contexto propia, toolset restringido, modelo propio. Úsalos para (a) exploración ruidosa sin contaminar tu sesión, y (b) **revisión con contexto fresco** (un revisor nuevo no está sesgado hacia el código que acaba de escribir).

### 2.6 Hooks — lo que debe pasar SIEMPRE
- En `.claude/settings.json`, para "acciones que deben ocurrir cada vez, sin excepción" — determinista, a diferencia del CLAUDE.md que es advisory. Eventos útiles: `PostToolUse` en Edit/Write → formateador; `Stop` → corre la suite y **bloquea el fin de turno si falla** (esto es lo que hace que corridas largas terminen correctas); `PreToolUse` → bloquear escrituras a rutas protegidas.

### 2.7 `settings.json` (permisos) y `.mcp.json`
- **Permisos versionados** para reducir fricción: allowlist de comandos read-only frecuentes (git status, build, test) para que el agente no pida permiso a cada paso.
- **`.mcp.json` versionado** solo cuando de verdad lo necesitas: MCP es para *datos vivos/dinámicos* (consultas a DB de esquema, issue tracker). Para casi todo, un **CLI es más eficiente en contexto** que un MCP (`gh`, `git`, tu CLI de build). Regla: CLI si existe un buen CLI; MCP solo para lo que no lo tiene y estás pegando a mano.

---

## 3. La verificación es parte del repo (EL habilitador)

**Principio.** El bucle de verificación que el agente corre **solo** es la inversión de organización de mayor apalancamiento. Es lo que te deja *alejarte del teclado*.

**Qué establecer (agnóstico de tu arquitectura):**
- **Suite de tests rápida** + build + linter, con el **comando exacto en `CLAUDE.md`** para que el agente lo corra sin adivinar.
- **CI como required status check** en cada PR: tu backstop no-negociable para código generado por IA. Diff pequeño + gate verde = puedes revisar por excepción.
- **Tests de arquitectura como gate del build.** Aquí está el puente entre "yo defino la arquitectura después" y "la IA no me la erosiona": el día que definas tus fronteras (capas, módulos, dependencias permitidas), **codifícalas como tests que rompen el build** (p. ej. ArchUnitNET / NetArchTest en .NET). Así tus reglas arquitectónicas dejan de ser convención (que el agente puede violar en silencio) y se vuelven un muro determinista. No necesitas saber *cuál* será la arquitectura hoy; solo dejar el mecanismo listo para llenarlo.
- **Hook `Stop`** que corra la suite rápida → el agente itera hasta verde sin ti.

---

## 4. Documentación y contexto como artefacto de primera clase

**Principio.** En desarrollo con IA, la documentación no es un extra: es *entrada de contexto* que humanos **y** agente leen. Organiza el repo para que la intención sea recuperable.

**Qué establecer:**
- **`docs/adr/` (Architecture Decision Records) desde el inicio.** Cuando definas tus decisiones de arquitectura, escríbelas como ADRs versionados: el agente los lee y respeta. Es el canal por el que tu criterio de 20 años entra al motor de desarrollo de forma persistente.
- **Plantilla `SPEC.md` + flujo de "entrevista".** Para features no triviales: deja que el agente te entreviste, escriba un spec, y **luego implemente en una sesión fresca** contra ese spec. La intención escrita evita el fallo caro: construir con seguridad *lo equivocado*.
- **README orientado al agente:** cómo levantar el proyecto, correr tests, y el mapa de "dónde vive cada cosa" — lo primero que un agente necesita para orientarse.

---

## 5. Higiene de git para trabajar con IA

- **Commits pequeños y frecuentes**, uno por unidad verificable. Diffs chicos = revisión tratable y rollback fácil por git (los checkpoints del agente no reemplazan git).
- **Branch + PR por feature**, con el gate de CI como control de calidad automático del código de IA.
- **Worktrees / sesiones paralelas** para features independientes cuando tu bucle ya sea fluido (no antes).
- **Ramas protegidas + CI required** = el agente puede conducir sin poder romper `main`.

---

## 6. El bucle de trabajo diario (cómo se siente en la práctica)

`Explore → Plan → Implement → Verify → Review → Commit`, y repetir:

1. **Enmarcar** en plan mode; para algo grande, entrevista → `SPEC.md`, luego sesión fresca.
2. **Plan escrito** que revisas/editas antes de una sola línea de código.
3. **Implementar** una unidad pequeña contra criterios expresados como tests.
4. **Verificar:** el agente corre tests/build/arch-tests a verde (los hooks lo fuerzan).
5. **Revisar:** subagent adversarial con contexto fresco que ve solo el diff y los criterios.
6. **Commit** chico + PR (CI required) → `/clear` → siguiente unidad.

---

## 7. Anti-patrones a evitar (modos de fallo de la IA, y su cura organizacional)

- **`CLAUDE.md` como documentación** → inflado → reglas ignoradas. *Cura: podar a <200 líneas, mover enforcement a hooks.*
- **Sesión cajón-de-sastre** → `/clear` entre tareas no relacionadas.
- **Corregir en bucle** → tras 2 correcciones fallidas, `/clear` y reescribe el prompt. "Una sesión limpia con mejor prompt casi siempre gana a una larga con correcciones acumuladas".
- **Context rot** → `/clear` frecuente, `/compact`, delega investigación a subagents.
- **Confiar sin verificar** → si no puedes verificarlo con un pass/fail, no lo mezcles. La verificación es el cimiento, no un extra.
- **Convención sin enforcement** → lo que importa, hazlo un test o un hook; el resto el agente lo tratará como opcional.

---

## Checklist de organización "día uno" (solo andamiaje, no arquitectura)

1. **Monorepo** con estructura cohesiva por cercanía; **un ejemplar canónico** escrito a mano como patrón; tests colocados junto al código.
2. **`CLAUDE.md` <200 líneas** (vía `/init`, luego podar): comandos, convenciones, etiqueta de PR, mapa de "dónde vive qué".
3. **`.claude/rules/`** con `paths:` para reglas específicas por área; `CLAUDE.md` de subdirectorio para módulos con idioms propios.
4. **`.claude/skills/`**: un skill que scaffoldea desde tu ejemplar canónico.
5. **`.claude/agents/`**: un subagent revisor de contexto fresco.
6. **Hooks** en `settings.json`: formato-al-editar, tests-al-terminar, bloquear rutas protegidas.
7. **Permisos versionados** (allowlist de comandos read-only frecuentes); `.mcp.json` solo si de verdad lo necesitas (CLI antes que MCP).
8. **Suite de tests rápida + CI como required gate**; deja lista la **clase de arch-tests vacía** para llenarla cuando definas tu arquitectura.
9. **`docs/adr/` + plantilla `SPEC.md`**; README orientado al agente.
10. **Ramas protegidas + commits pequeños + PR por feature**.

---

## Los tres no-negociables si solo recuerdas tres cosas

1. **Sube la señal:** repo cohesivo, archivos pequeños, nombres consistentes, un ejemplar canónico. El agente encuentra lo justo, rápido.
2. **Cierra el bucle:** tests + CI required + hook de verificación. El agente se auto-corrige a verde sin que tú seas el verificador.
3. **`.claude/` versionado es el sistema nervioso:** `CLAUDE.md` lean como instrucción (no docs), reglas con `paths:`, skills para flujos, subagents para revisión fresca, hooks para lo que debe pasar siempre.

---

## Fuentes primarias
- Claude Code — Best practices: https://code.claude.com/docs/en/best-practices
- Claude Code — Memory / CLAUDE.md: https://code.claude.com/docs/en/memory
- Claude Code — Subagents: https://code.claude.com/docs/en/sub-agents
- Claude Code — Hooks: https://code.claude.com/docs/en/hooks
- Claude Code — MCP: https://code.claude.com/docs/en/mcp
- Anthropic Engineering — Effective context engineering for AI agents: https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents
- GitHub — Spec-driven development toolkit (Spec Kit): https://github.com/github/spec-kit
- Architecture testing en .NET (Milan Jovanović) · ArchUnitNET · NetArchTest: https://milanjovanovic.tech/blog/shift-left-with-architecture-testing-in-dotnet
