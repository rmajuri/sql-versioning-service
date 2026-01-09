# SQL Versioning Service — Azure Deployment

This document describes how the SQL Versioning Service is deployed and run in Azure, including infrastructure components, authentication, networking, and the deployment flow.

## High-Level Architecture

The system runs on Azure Container Apps and consists of two runtime components:

**API Container App**

- Hosts the ASP.NET Core API
- Serves application traffic

**Migration Container App Job**

- Runs database migrations as a one-shot job
- Executes before API deployments

**Supporting services:**

- Azure Container Registry (ACR) — stores container images
- Azure PostgreSQL Flexible Server — primary relational database
- Azure Blob Storage — object/blob storage
- Azure Log Analytics — logs for containers and jobs
- Azure Virtual Network (VNet) — networking boundary

## Azure Resources

All resources live in a single resource group per environment.

### Core Resources

| Resource                         | Purpose                     |
| -------------------------------- | --------------------------- |
| Azure Container App              | Runs the API                |
| Azure Container App Job          | Runs database migrations    |
| Azure Container Apps Environment | Shared runtime + networking |
| Azure Container Registry         | Container image storage     |
| PostgreSQL Flexible Server       | Application database        |
| Storage Account (Blob)           | Blob/object storage         |
| Log Analytics Workspace          | Logs and diagnostics        |
| Virtual Network + Subnets        | Networking                  |

## Container Image

A single Docker image is built and used for both the API and the migration job.

### Image Contents

- ASP.NET Core API
- Migration runner (MigrateRunner.dll)
- Raw SQL migration files

### Docker Build Summary

- Build stage: `mcr.microsoft.com/dotnet/sdk:8.0`
- Runtime stage: `mcr.microsoft.com/dotnet/aspnet:8.0`
- Framework-dependent publish (no self-contained binaries)
- No Alpine / musl images (to avoid native crashes)

Final filesystem layout inside the container:

```
/app
├─ SqlVersioningService.dll (API)
├─ migrate/
│  └─ MigrateRunner.dll (migration runner)
└─ ops/
   └─ migrations/
      ├─ 001*.sql
      ├─ 002*.sql
      └─ ...
```

## Database Migrations

### Migration Runner

Migrations are executed by a .NET console app (MigrateRunner) that:

- Reads SQL files from `/app/ops/migrations`
- Orders files lexicographically
- Applies each file in its own transaction
- Records applied migrations in a tracking table

### Migration Tracking Table

```sql
CREATE TABLE IF NOT EXISTS schema_migrations (
    id TEXT PRIMARY KEY,
    checksum TEXT NOT NULL,
    applied_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

This ensures:

- Idempotent execution
- Safe re-runs
- Deterministic ordering

## Migration Job (Azure Container Apps Job)

### Purpose

Runs migrations before the API is deployed.

### Execution Model

- Manual trigger
- Run-to-completion
- Fails the deployment pipeline if migrations fail

### Job Command

```yaml
command:
  - dotnet
args:
  - /app/migrate/MigrateRunner.dll
```

### Environment Variables

| Name           | Source                         |
| -------------- | ------------------------------ |
| DB_CONN        | Azure Container App Job secret |
| MIGRATIONS_DIR | /app/ops/migrations            |

### Secrets

Secrets are stored only in Azure Container Apps (not in GitHub).

**db-conn** — PostgreSQL connection string

Example (no quotes stored):

```
Host=pg-sqlversioning-dev.postgres.database.azure.com;
Port=5432;
Database=postgres;
Username=pgadmin;
Password=********;
SSL Mode=Require;
Trust Server Certificate=true;
```

## API Container App

### Purpose

Hosts the ASP.NET Core API.

### Runtime

- Same image as migration job
- Uses default ENTRYPOINT:
  ```
  dotnet SqlVersioningService.dll
  ```

### Deployment

- Image updated only after migrations succeed
- Uses the same Container Apps Environment as the job

## Authentication & Security

### Container Registry

- Managed Identity
- AcrPull role assigned to:
  - API Container App
  - Migration Job

### PostgreSQL

- Username/password auth
- Password stored as ACA Job secret
- TLS enforced
- Certificate trust temporarily bypassed with:
  ```
  Trust Server Certificate=true
  ```

### Blob Storage

- Can be accessed via:
  - Managed Identity (preferred)
  - Or connection string (if configured)
- RBAC controls access

## Networking

- Azure Container Apps Environment attached to a VNet
- PostgreSQL Flexible Server reachable via Azure networking
- DNS resolution handled by Azure-managed DNS
- No inbound public DB access required

## Logging & Observability

### Where Logs Appear

**Azure Portal**

- Container App Job → Execution history → Console

**Log Analytics**

- Console logs (`ContainerAppConsoleLogs_*`)
- System logs (`ContainerAppSystemLogs_CL`)

### Important Notes

- Logs are chunked and interleaved across executions
- Always filter by time or execution when debugging
- Exit codes:
  - 0 → success
  - 139 → native crash (resolved during setup)
  - Non-zero → failure

## Deployment Flow (CI/CD)

### GitHub Actions Pipeline

1. Build Docker image
2. Push image to ACR
3. Update migration job image
4. Start migration job
5. Poll job status
6. Only if migrations succeed:
   - Update API Container App image

No database credentials exist in GitHub.

## Common Failure Modes

| Symptom                | Likely Cause                          |
| ---------------------- | ------------------------------------- |
| exit 139               | Runtime / native dependency issue     |
| 28P01                  | Invalid DB credentials                |
| No logs                | Logging not enabled on environment    |
| Job succeeds instantly | Job running debug command, not runner |

## Verification Checklist

After deployment:

- [ ] Migration job shows `Succeeded`
- [ ] Logs show:
  ```
  MigrateRunner starting...
  Found X migrations
  APPLY ...
  DONE ...
  Migrations complete.
  ```
- [ ] `schema_migrations` table populated
- [ ] API container running

## Summary

This deployment provides:

- Deterministic, idempotent migrations
- Clear separation of schema and runtime concerns
- Secure handling of secrets
- Repeatable, observable deployments

Once configured, deployments are boring — which is the goal.
