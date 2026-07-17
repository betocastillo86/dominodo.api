# Local development

## Database (SQL Server in Docker)

There is no LocalDB on macOS, so the engine runs in a container. It is codified in
[`docker-compose.yml`](../docker-compose.yml) (repo root) — **the container is no longer created by hand**.

- Engine: `mcr.microsoft.com/mssql/server:2022-latest` (Developer edition).
- Port **1435** on the host → 1433 in the container. The connection strings (`ConnectionStrings:Dominodo`
  in `appsettings*.json`) point to `localhost,1435`.
- User `sa`, password `Dominodo!Pass123`.
- Data persists in the named volume `dominodo-mssql-data` → it survives Mac reboots and
  `docker compose down` (but **not** `down -v`).

### Bootstrapping from scratch

```bash
# 1. Start the server and wait until it accepts connections
docker compose up -d --wait

# 2. Apply each module's migrations (creates the users/admin schemas + seed).
#    Wolverine auto-provisions its "wolverine" schema on host startup (UseResourceSetupOnStartup).
./scripts/db.sh
```

`./scripts/db.sh` runs `dotnet ef database update` for every module context in one go (see below).
In **Development** the host also does this for you on startup (see "Automatic startup bootstrap"); the
script + manual commands remain for CI, resets, and running a single context.

Under the hood it is just these two commands (useful if you need to run one context on its own):

```bash
dotnet ef database update \
  --project src/Modules/Users/Dominodo.Users.Persistence \
  --startup-project src/Bootstrap/Dominodo.Api --context UsersDbContext

dotnet ef database update \
  --project src/Modules/Admin/Dominodo.Admin.Persistence \
  --startup-project src/Bootstrap/Dominodo.Api --context AdminDbContext
```

### Automatic startup bootstrap (Development only)

When the host runs under `ASPNETCORE_ENVIRONMENT=Development` (e.g. pressing F5 in Rider/VS, or
`dotnet run`), `DevBootstrap` (`src/Bootstrap/Dominodo.Api/DevBootstrap.cs`) runs before the app serves:

1. Probes the DB port from the connection string. If it is **not** reachable, it runs
   `docker compose up -d --wait` from the repo root — so you don't need to start the container by hand.
   If the DB is already up, Docker is skipped.
2. Applies each module's pending migrations (`MigrateAsync`, idempotent).

Everything is best-effort: if a migration fails (e.g. the model drifted ahead of its last migration, or
Docker isn't installed), it logs an actionable warning and **still boots** against the existing schema —
it never blocks startup. It never runs outside Development.

Opt out (use your own DB / start it yourself) by setting in `appsettings.Development.json` or an env var:

```jsonc
{ "DevBootstrap": { "Enabled": false } }
```

### Migration commands (`scripts/db.sh`)

Each module has its own DbContext + schema, so each migrates separately. The script wraps all module
contexts (defined in a `MODULES` array — add a line per new module):

```bash
./scripts/db.sh          # apply pending migrations to every context (dotnet ef database update)
./scripts/db.sh reset    # drop + update: clean rebuild with seed
./scripts/db.sh drop     # drop the database only (deletes everything)
./scripts/db.sh add Foo  # create migration "Foo" across every context
```

> For adding a migration to a **single** module (the usual case), prefer the direct
> `dotnet ef migrations add <Name> --project <module>/…Persistence --context <Ctx> --output-dir Migrations`.

### Day-to-day commands

```bash
docker compose up -d      # start (or after rebooting the Mac)
docker compose stop       # stop without deleting the container
docker compose down       # stop and remove the container — data stays in the volume
docker compose down -v    # stop and DELETE the data (volume included) — bootstrap from scratch
docker compose logs -f    # tail the engine logs
```

## Migrating from the previous manual container

The original `dominodo-sqlserver` container was created by hand with `docker run` and **had no volume**: its
data lives in the container's writable layer, not in a persistent volume. To move to compose without
surprises, the simplest thing in dev is to recreate from scratch (the seed re-applies itself):

```bash
docker rm -f dominodo-sqlserver     # removes the manual container (deletes its data!)
docker compose up -d --wait         # recreates the compose-managed container, with a volume
# then: dotnet ef database update ... (both contexts, see above)
```

> If you have data in the old container you want to keep, take a backup with
> `docker exec dominodo-sqlserver /opt/mssql-tools18/bin/sqlcmd ... BACKUP DATABASE ...` **before**
> removing it. For the normal dev flow (seed only), recreating from scratch is the expected path.
