#!/usr/bin/env bash
# Ejecuta las migraciones EF de todos los módulos de un tirón, sin tener que recordar los flags.
# Cada módulo tiene su propio DbContext + schema, así que cada uno migra por separado (ver docs/local-development.md).
#
#   ./scripts/db.sh              # aplica migraciones pendientes a todos los contextos (dotnet ef database update)
#   ./scripts/db.sh update       # igual que arriba (explícito)
#   ./scripts/db.sh add <Nombre> # crea una migración <Nombre> en TODOS los contextos (raro; normalmente usas un módulo)
#   ./scripts/db.sh drop         # DROPEA la base de datos (¡borra todo!) — pide confirmación
#   ./scripts/db.sh reset        # drop + update: reinicio limpio con seed
set -euo pipefail

# Raíz del repo (este script vive en scripts/), para poder invocarlo desde cualquier carpeta.
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

STARTUP="src/Bootstrap/Dominodo.Api"

# Un módulo por línea: "<Context>|<ruta al proyecto Persistence>". Añade aquí cada módulo nuevo.
MODULES=(
  "UsersDbContext|src/Modules/Users/Dominodo.Users.Persistence"
  "AdminDbContext|src/Modules/Admin/Dominodo.Admin.Persistence"
)

update() {
  for m in "${MODULES[@]}"; do
    local ctx="${m%%|*}" proj="${m##*|}"
    echo "==> database update: $ctx"
    dotnet ef database update --project "$proj" --startup-project "$STARTUP" --context "$ctx"
  done
}

drop() {
  # Una sola BD física alberga todos los schemas, así que dropear con cualquier contexto la borra entera.
  local first="${MODULES[0]}" ctx="${first%%|*}" proj="${first##*|}"
  echo "==> database drop (BORRA TODA la base de datos)"
  dotnet ef database drop --force --project "$proj" --startup-project "$STARTUP" --context "$ctx"
}

add() {
  local name="${1:?Uso: ./scripts/db.sh add <NombreMigracion>}"
  for m in "${MODULES[@]}"; do
    local ctx="${m%%|*}" proj="${m##*|}"
    echo "==> migrations add $name: $ctx"
    dotnet ef migrations add "$name" --project "$proj" --startup-project "$STARTUP" --context "$ctx" --output-dir Migrations
  done
}

cmd="${1:-update}"
case "$cmd" in
  update) update ;;
  drop)   drop ;;
  reset)  drop; update ;;
  add)    add "${2:-}" ;;
  *) echo "Comando desconocido: $cmd (usa: update | add <Nombre> | drop | reset)" >&2; exit 1 ;;
esac

echo "Listo."
