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
dotnet ef database update \
  --project src/Modules/Users/Dominodo.Users.Persistence \
  --startup-project src/Bootstrap/Dominodo.Api --context UsersDbContext

dotnet ef database update \
  --project src/Modules/Admin/Dominodo.Admin.Persistence \
  --startup-project src/Bootstrap/Dominodo.Api --context AdminDbContext
```

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
