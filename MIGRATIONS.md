# MIGRATIONS (How schema changes work)

## Overview

This project uses file-based SQL migrations. A migration is a `.sql` file committed to the repo that contains one logical, forward-only schema change (create table, add column, add index, etc.). Migrations are executed by a small .NET console app called "MigrateRunner," which runs inside a one-shot Azure Container Apps Job. The API container does not apply schema changes at startup.

## Goals

- **Deterministic**: migrations run in a stable, predictable order.
- **Idempotent**: rerunning the job does not reapply the same migration.
- **Auditable**: every applied migration is recorded in the database.

## Where migrations live

**In the repository:**

```
ops/migrations
```

**In the container image at runtime:**

```
/app/ops/migrations
```

The Docker build copies `ops/migrations` into the image so the migration job always uses the migrations embedded in the same image it runs.

## How migrations are ordered

MigrateRunner scans the migrations directory for `*.sql` files, then sorts them lexicographically (simple string ordering). That means file naming is important. Use a consistent numeric prefix with zero-padding:

### Examples

- `001_core_domain_tables.sql`
- `002_create_api_keys_table.sql`
- `003_add_indexes.sql`

### Rules of thumb

- Always zero-pad your numeric prefix (001, 002, 010, 100).
- Don't rely on timestamps unless you standardize them across the project.
- Keep names short and descriptive.

## How migrations are applied

When the migration job runs, MigrateRunner does roughly this:

1. Read DB connection string from the `DB_CONN` environment variable.
2. Read migrations directory from `MIGRATIONS_DIR` (defaults to `/app/ops/migrations`).
3. Verify the migrations directory exists.
4. Create a `schema_migrations` tracking table if it doesn't exist.
5. Load the list of already-applied migration IDs from `schema_migrations`.
6. For each migration file in order:
   - If the migration ID (filename) is already applied, skip it.
   - Otherwise, run the file in a transaction.
   - On success, insert a row into `schema_migrations` with its checksum.
   - On failure, roll back and stop the job with a failure exit code.

## Tracking table

Applied migrations are recorded in a table called `schema_migrations`:

- **id**: the filename (e.g., `001_core_domain_tables.sql`)
- **checksum**: SHA-256 of the file contents at apply time
- **applied_at**: timestamp

## What "idempotent" means here

A migration is considered "already applied" if there is a `schema_migrations` row with the same id (filename). Re-running the job will not run it again.

**Important implication: do not edit old migration files after they've been applied.**

Even though we store checksums, the primary skip logic is filename-based. Editing old files creates confusion and risks inconsistent environments. Treat migrations as immutable history.

## How to add a new schema change

1. Create a new `.sql` file under `ops/migrations`.
2. Choose the next numeric prefix and a descriptive name.
3. Write the SQL for the change.
4. Commit it.
5. Deploy the container image.
6. Run the migration job (CI/CD or manual).
7. Verify `schema_migrations` includes the new migration.

## Guidelines for writing migration SQL

- Prefer additive, forward-only changes (new tables, new columns, new indexes).
- Be careful with destructive changes (`DROP COLUMN`, `DROP TABLE`). If you must, do it deliberately and with a clear reason.
- If you add `NOT NULL` columns to a populated table, give a default or backfill first.
- Use explicit schema names if you introduce them (`public`, etc.).
- Keep each file to one cohesive change when possible.

## Local development

If you run the migration runner locally, set:

- `DB_CONN` to a reachable Postgres connection string
- `MIGRATIONS_DIR` to the local path (or let it default if your working directory layout matches)

## Azure job configuration

The migration job runs the same container image as the API, but with a different command:

```yaml
command: dotnet
args: /app/migrate/MigrateRunner.dll
```

It reads `DB_CONN` from an Azure Container Apps Job secret and uses `MIGRATIONS_DIR=/app/ops/migrations`.

## Where to see logs

There are two useful views:

1. **Azure Portal** → Container App Job → Execution history → Console/System logs
2. **Log Analytics queries** (if enabled)

`ContainerAppConsoleLogs_CL` typically contains stdout/stderr from MigrateRunner.

If you see `ProcessExited` and exit code 139, that's usually a runtime/native crash. If you see `28P01`, that's a Postgres auth failure.

## Common failure modes and what they mean

- **Migrations dir not found**: `MIGRATIONS_DIR` points somewhere wrong or the image didn't include `ops/migrations`.
- **28P01 password authentication failed**: `DB_CONN` is wrong (password changed, username mismatch, secret not updated).
- **SQL error during APPLY**: the migration SQL itself failed; fix by creating a new migration that corrects the issue (don't edit old migrations already applied elsewhere).

## Golden rules

- Never edit or reorder migrations once they've been applied anywhere important.
- Every schema change is a new migration file.
- Always verify via `schema_migrations` after running.
