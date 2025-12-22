-- Migration: Create api_keys table for API key authentication
-- Run this migration against your database before using API key authentication
CREATE TABLE
    IF NOT EXISTS api_keys (
        id UUID PRIMARY KEY,
        hashed_key TEXT NOT NULL UNIQUE,
        created_at TIMESTAMPTZ NOT NULL,
        revoked_at TIMESTAMPTZ NULL
    );

-- Index on hashed_key for fast lookups during authentication
CREATE INDEX IF NOT EXISTS idx_api_keys_hashed_key ON api_keys (hashed_key);

-- Partial index for active (non-revoked) keys
CREATE INDEX IF NOT EXISTS idx_api_keys_active ON api_keys (hashed_key)
WHERE
    revoked_at IS NULL;

COMMENT ON TABLE api_keys IS 'Stores hashed API keys for request authentication';

COMMENT ON COLUMN api_keys.id IS 'Unique identifier for the API key';

COMMENT ON COLUMN api_keys.hashed_key IS 'SHA256 hash of the plaintext API key';

COMMENT ON COLUMN api_keys.created_at IS 'Timestamp when the key was created';

COMMENT ON COLUMN api_keys.revoked_at IS 'Timestamp when the key was revoked (NULL = active)';