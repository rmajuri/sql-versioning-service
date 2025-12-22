-- ============================================
-- 001_create_core_domain_tables.sql
-- ============================================
-- Enable UUID generation if needed
-- (skip if you already handle UUIDs in app code)
-- CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
-- ----------------------------
-- Queries
-- ----------------------------
CREATE TABLE
    queries (
        id UUID PRIMARY KEY,
        name TEXT NOT NULL,
        head_version_id UUID NULL,
        is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
        created_at TIMESTAMPTZ NOT NULL,
        updated_at TIMESTAMPTZ NOT NULL,
        deleted_at TIMESTAMPTZ NULL
    );

-- Only one soft-delete state per query
CREATE INDEX idx_queries_not_deleted ON queries (id)
WHERE
    is_deleted = FALSE;

-- ----------------------------
-- QueryVersions
-- ----------------------------
CREATE TABLE
    query_versions (
        id UUID PRIMARY KEY,
        query_id UUID NOT NULL,
        parent_version_id UUID NULL,
        blob_hash TEXT NOT NULL,
        note TEXT NULL,
        created_at TIMESTAMPTZ NOT NULL,
        updated_at TIMESTAMPTZ NOT NULL,
        CONSTRAINT fk_query_versions_query FOREIGN KEY (query_id) REFERENCES queries (id) ON DELETE CASCADE,
        CONSTRAINT fk_query_versions_parent FOREIGN KEY (parent_version_id) REFERENCES query_versions (id) ON DELETE SET NULL
    );

-- A query cannot have the same blob twice
CREATE UNIQUE INDEX ux_query_versions_query_blob ON query_versions (query_id, blob_hash);

-- Fast lookups by query
CREATE INDEX idx_query_versions_query ON query_versions (query_id);

-- Ordering support
CREATE INDEX idx_query_versions_created_at ON query_versions (created_at DESC);

-- ----------------------------
-- SqlBlobs
-- ----------------------------
CREATE TABLE
    sql_blobs (
        hash TEXT PRIMARY KEY,
        bytes_size INTEGER NOT NULL
    );