# sql-versioning-service

Short notes and developer commands.

## Architecture

For detailed architectural diagrams and system design documentation, see:

🔗 **[Architecture Diagrams (IcePanel)](https://s.icepanel.io/o1cGKR8o8OtDKC/kI0d)**

## Local Development

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/get-started)

### Quick Start

```bash
# 1. Start infrastructure (Postgres + Azurite)
docker compose -f docker-compose.dev.yaml up -d postgres azurite

# 2. Wait for Postgres to be ready
until docker exec sql_versioning_test_db pg_isready -U postgres -d sql_versioning_test; do sleep 1; done

# 3. Run migrations
docker exec -i sql_versioning_test_db psql -U postgres -d sql_versioning_test < ops/migrations/001_core_domain_tables.sql
docker exec -i sql_versioning_test_db psql -U postgres -d sql_versioning_test < ops/migrations/002_create_api_keys_table.sql

# 4. Create an API key
cd ops/create-api-key && dotnet run -- "Host=localhost;Port=5432;Database=sql_versioning_test;Username=postgres;Password=postgres"
cd ../..

# 5. Run the API locally
dotnet run
```

The API will be available at `http://localhost:5000` (or the port shown in console).

### Running with Docker Compose (Full Stack)

To run everything in containers (API + Postgres + Azurite):

```bash
docker compose -f docker-compose.dev.yaml up -d
```

| Service    | Container Name           | Port  | Description                 |
| ---------- | ------------------------ | ----- | --------------------------- |
| `api`      | `sql_versioning_api`     | 5000  | The API service             |
| `postgres` | `sql_versioning_test_db` | 5432  | PostgreSQL 16 database      |
| `azurite`  | `sql_versioning_azurite` | 10000 | Azure Blob Storage emulator |

**Note:** When running the API in Docker, you still need to run migrations and create an API key (see Database Setup below).

### Environment Variables

When running locally with `dotnet run`, configure via `appsettings.Development.json` or environment variables:

| Variable                         | Description                          | Default (Docker)             |
| -------------------------------- | ------------------------------------ | ---------------------------- |
| `ConnectionStrings__Default`     | PostgreSQL connection string         | See docker-compose.dev.yaml  |
| `AzureStorage__ConnectionString` | Azure Blob Storage connection string | `UseDevelopmentStorage=true` |
| `AzureStorage__ContainerName`    | Blob container name                  | `sqlblobs`                   |

---

## Database Setup

### Starting PostgreSQL (Docker)

```bash
# Start the Postgres container
docker compose -f docker-compose.dev.yaml up -d postgres

# Wait for it to be ready
until docker exec sql_versioning_test_db pg_isready -U postgres -d sql_versioning_test; do sleep 1; done
```

### Running Migrations

Migrations are located in `ops/migrations/` and must be run in order:

```bash
# 1. Core domain tables (queries, query_versions, sql_blobs)
docker exec -i sql_versioning_test_db psql -U postgres -d sql_versioning_test < ops/migrations/001_core_domain_tables.sql

# 2. API keys table
docker exec -i sql_versioning_test_db psql -U postgres -d sql_versioning_test < ops/migrations/002_create_api_keys_table.sql
```

Verify the tables were created:

```bash
docker exec sql_versioning_test_db psql -U postgres -d sql_versioning_test -c "\dt"
```

### Resetting the Database

To start fresh (removes all data):

```bash
docker compose -f docker-compose.dev.yaml down -v
docker compose -f docker-compose.dev.yaml up -d postgres
# Then re-run migrations
```

---

## Authentication

API access is protected by API keys. This service is **intentionally single-tenant** and does not implement user, organization, or role concepts.

### How It Works

- All API endpoints (except `/health` and `/swagger`) require a valid API key
- Keys are validated via middleware at the HTTP boundary
- Keys are stored as SHA256 hashes in the database (plaintext keys are never stored)

### Making Authenticated Requests

Include the API key in the `Authorization` header:

```bash
curl -H "Authorization: Bearer <your-api-key>" http://localhost:5000/api/queries
```

### Creating API Keys (Operators Only)

API keys are minted manually using an operator script. This is not exposed via HTTP.

```bash
# Generate a new API key (migrations must be run first)
cd ops/create-api-key
dotnet run -- "Host=localhost;Port=5432;Database=sql_versioning_test;Username=postgres;Password=postgres"
```

The script will:

1. Generate a cryptographically secure random key
2. Hash and store it in the database
3. Print the plaintext key **once** to stdout

⚠️ **Store the key securely** - it cannot be retrieved after creation.

### Revoking Keys

To revoke a key, update the `revoked_at` column directly in the database:

```sql
UPDATE api_keys SET revoked_at = NOW() WHERE id = '<key-id>';
```

---

## Running Tests

This project has two test suites:

| Project                                       | Type                              | Requires Database |
| --------------------------------------------- | --------------------------------- | ----------------- |
| `Tests/SqlVersioningService.UnitTests`        | Unit tests (mocked dependencies)  | No                |
| `Tests/SqlVersioningService.IntegrationTests` | Integration tests (real Postgres) | Yes               |

### CI Pipeline

The GitHub Actions workflow (`.github/workflows/ci-tests.yml`) runs on every push and pull request:

1. **Unit tests** run first (no Docker required)
2. **PostgreSQL** is started via `docker-compose.test.yml`
3. **Integration tests** run against the real database
4. **Teardown** removes containers and volumes

```yaml
# Simplified CI flow
- dotnet test Tests/SqlVersioningService.UnitTests
- docker compose -f docker-compose.test.yml up -d
- dotnet test Tests/SqlVersioningService.IntegrationTests
- docker compose -f docker-compose.test.yml down -v
```

### Running Tests Locally

#### Unit Tests Only (No Docker Required)

```bash
dotnet test Tests/SqlVersioningService.UnitTests
```

#### Integration Tests (Requires Docker)

```bash
# 1. Start PostgreSQL
docker compose -f docker-compose.test.yml up -d

# 2. Wait for Postgres to be ready
echo "Waiting for Postgres..."
until docker exec sql-versioning-service-postgres-1 pg_isready -U postgres -d sql_versioning_test 2>/dev/null; do sleep 1; done

# 3. Run integration tests
dotnet test Tests/SqlVersioningService.IntegrationTests

# 4. Teardown
docker compose -f docker-compose.test.yml down -v
```

#### Run All Tests

```bash
# Start Postgres first
docker compose -f docker-compose.test.yml up -d

# Wait for ready
until docker exec sql-versioning-service-postgres-1 pg_isready -U postgres -d sql_versioning_test 2>/dev/null; do sleep 1; done

# Run entire solution
dotnet test

# Teardown
docker compose -f docker-compose.test.yml down -v
```

#### Filter by Category

Integration tests are marked with `[Trait("Category", "Integration")]`:

```bash
# Run only integration tests
dotnet test --filter "Category=Integration"

# Exclude integration tests (run unit tests only)
dotnet test --filter "Category!=Integration"
```

### Test Database Configuration

The `docker-compose.test.yml` starts PostgreSQL 16 with:

| Setting  | Value                 |
| -------- | --------------------- |
| Host     | `localhost`           |
| Port     | `5432`                |
| Database | `sql_versioning_test` |
| User     | `postgres`            |
| Password | `postgres`            |

Integration tests use xUnit **collection fixtures** to share a single database connection across tests, with data cleanup between test runs.

### Azure Blob Storage (Optional)

If tests require Azure Blob Storage behavior locally, provide credentials through environment variables:

- **Azurite/local emulator**: `AzureStorage__ConnectionString=UseDevelopmentStorage=true`
- **Token-based auth**: Set `AZURE_TENANT_ID`, `AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET` and `AzureStorage__ContainerUri`

> ⚠️ Do not commit secrets to the repository. Use environment variables or CI secrets.
