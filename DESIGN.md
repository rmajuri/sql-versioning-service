# DESIGN.md

## Overview

**SQL Versioning Service** is a backend system for versioning SQL queries and their content, supporting content-addressed storage and API-key-based authentication. It is designed for single-tenant use, with a focus on simplicity, security, and clear separation of concerns.

---

## Architecture

### System Context

- **API Service**: .NET 8 Web API (ASP.NET Core)
- **PostgreSQL**: Stores queries, versions, blob metadata, and API keys
- **Azure Blob Storage (or Azurite)**: Stores SQL content as blobs

### Containers

- **API Service**: Handles all HTTP requests, authentication, and business logic
- **PostgreSQL**: Relational database for all metadata and authentication
- **Blob Storage**: Content-addressed storage for SQL text

---

## Major Components

### 1. Controllers

- **HealthController**: Health check endpoint
- **QueriesController**: CRUD for queries
- **VersionsController**: CRUD for query versions and SQL content

### 2. Middleware

- **ApiKeyAuthenticationMiddleware**: Validates API keys on all requests (except health and Swagger)

### 3. Services

- **QueryService**: Query CRUD logic
- **QueryVersioningService**: Handles version creation, content hashing, and blob storage
- **QueryCreationService**: Orchestrates creation of a query and its initial version
- **HashingService**: SHA256 hashing for API keys and SQL content
- **AzureBlobStorageService**: Uploads/downloads SQL blobs

### 4. Repositories

- **QueryRepository**: Data access for queries
- **VersionRepository**: Data access for query_versions
- **SqlBlobRepository**: Data access for sql_blobs
- **ApiKeyRepository**: Data access for api_keys

### 5. Infrastructure

- **DatabaseContext**: Manages PostgreSQL connections

---

## Design Principles

- **Immutability over mutation**: Query versions and SQL blobs are append-only.
- **Content-addressed storage**: SQL content is identified by hash, not by version number.
- **Explicit orchestration**: Multi-step operations are coordinated in dedicated services.
- **Minimal surface area**: No user model, UI, or runtime key management APIs.
- **Evolvable by extension**: Future features should not require breaking existing contracts.

---

## Data Model

- **queries**: Query metadata (id, name, head_version_id, timestamps)
- **query_versions**: Version metadata (id, query_id, parent_version_id, blob_hash, note, timestamps)
- **sql_blobs**: Blob metadata (hash, bytes_size)
- **api_keys**: API key hashes, creation/revocation timestamps

---

## Authentication

- All endpoints (except health and Swagger) require a valid API key in the `Authorization: Bearer <key>` header
- API keys are stored hashed (SHA256) in the database
- Keys are minted by an operator script, not via API
- Revocation is handled by setting `revoked_at` in the database

---

## Storage

- **PostgreSQL**: All metadata, API keys
- **Azure Blob Storage**: SQL content, addressed by SHA256 hash

---

## Local Development

- Use Docker Compose to run Postgres and Azurite
- Migrations are SQL files in `ops/migrations/`
- API keys are created with a .NET console app in `ops/create-api-key/`

---

## Notable Constraints

- No user/org/tenant model (single-tenant only)
- No JWT/OAuth or runtime key management APIs
- No background jobs, message queues, or caching
- No UI or frontend

---

## Extensibility

The system is intentionally single-tenant and minimal.

Multi-tenancy, user management, and advanced authentication can be introduced later by:

- Adding tenant identifiers to domain models
- Introducing an external gateway or identity service
- Extending the authentication middleware

These changes do not require reworking the core versioning or storage model.

---

## Security

- API keys are never stored in plaintext
- All sensitive operations require a valid API key
- Operator scripts do not log or expose plaintext keys after creation

---

## Deployment

- API and infrastructure can be run locally or in containers
- See `docker-compose.dev.yaml` for service definitions
- See `README.md` for setup and usage instructions
