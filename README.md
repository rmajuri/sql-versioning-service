# sql-versioning-service

Short notes and developer commands.

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
# 1. Apply the migration (first time only)
psql -f ops/migrations/001_create_api_keys_table.sql <connection_string>

# 2. Generate a new API key
cd ops/create-api-key
dotnet run -- "Host=localhost;Port=5432;Database=sql_versioning;Username=postgres;Password=postgres"
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
