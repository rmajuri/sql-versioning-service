# sql-versioning-service

Short notes and developer commands.

## Running tests

This project provides a `test-runner` service in `docker-compose.dev.yaml` that runs tests inside a .NET SDK container and depends on the `postgres` test database.

Recommended: run the `test-runner` via Docker Compose (no SDK required on host):

```bash
# Start test-runner and dependencies; it will exit with the test result code
docker compose -f docker-compose.dev.yaml up --abort-on-container-exit --build test-runner

# Tear down containers and networks after the run
docker compose -f docker-compose.dev.yaml down
```

Notes:

- Test results (TRX) are written to `./TestResults` in the repository because the repo is mounted into the container.
- If tests require Azure Blob Storage behavior locally, provide credentials through environment variables only (not in appsettings):
  - Azurite/local emulator: set `AzureStorage__ConnectionString=UseDevelopmentStorage=true` in your shell or compose environment.
  - Token-based auth (service principal): set `AZURE_TENANT_ID`, `AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET` and `AzureStorage__ContainerUri` in the environment.

Alternative: run tests from the host while using docker-compose only for Postgres:

```bash
# 1) Start only Postgres
docker compose -f docker-compose.dev.yaml up -d postgres

# 2) Wait for Postgres to be ready (container name: sql_versioning_test_db)
echo "Waiting for Postgres to be ready..."
until docker exec sql_versioning_test_db pg_isready -U postgres -d sql_versioning_test; do sleep 1; done

# 3) Run tests locally (requires .NET SDK on host)
dotnet test

# 4) Tear down Postgres
docker compose -f docker-compose.dev.yaml down
```

For CI: prefer running tests in the `test-runner` service or a CI agent that runs `dotnet test`, and inject secret credentials as environment variables (do not commit secrets).
