/**
 * Apply the SQL files in `backend/migrations/` in name order, each once, then check the schema.
 *
 * Applied names and a SHA-256 of each file are recorded in `node_migrations`. A file edited after
 * it was applied stops start-up with a clear error, because the change would otherwise never run
 * (rows applied before checksums existed get their checksum filled in once). A PostgreSQL
 * advisory lock makes two starting processes wait for each other instead of applying a file
 * twice. Each file runs in its own transaction, so a failing file leaves no partial change.
 *
 * After migrating, `checkSchema` confirms every column the API reads exists. `0001_schema.sql`
 * skips tables that already exist (the ones Django made), so a database Django left at an older
 * migration would otherwise pass and fail later at run time (2026-11-15 EGM review, issue 8).
 *
 * Run directly (`node src/migrate.js`) it waits for the database, migrates and exits. The server
 * (`src/server.js`) calls `migrate` itself before it listens.
 */
import { createHash } from 'node:crypto';
import { readdir, readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { loadConfig } from './config.js';
import { createPool, waitForDatabase, withTransaction } from './db.js';

const MIGRATIONS_DIR = path.join(path.dirname(fileURLToPath(import.meta.url)), '..', 'migrations');
// Any fixed number works; it only has to be the same for every process that migrates.
const LOCK_ID = 74_210_315;

/** Every column the API reads or writes, per table. */
export const REQUIRED_COLUMNS = Object.freeze({
  accounts_user: ['id', 'password', 'last_login', 'is_superuser', 'username', 'first_name', 'last_name',
    'is_staff', 'is_active', 'date_joined', 'email', 'profile_picture', 'is_2fa_enabled', 'country',
    'phone_number', 'clerk_user_id', 'date_of_birth'],
  accounts_browsersession: ['id', 'session_id', 'created_at', 'is_authorized', 'user_id', 'secret_hash',
    'expires_at', 'consumed_at', 'cancelled_at', 'intent', 'locale'],
  accounts_pendingregistration: ['id', 'clerk_user_id', 'email', 'token_hash', 'first_name', 'last_name',
    'clerk_username', 'request_ip', 'sent_count', 'created_at', 'last_sent_at', 'expires_at', 'completed_at',
    'browser_session_id'],
  accounts_twofactorcode: ['id', 'code', 'created_at', 'is_verified', 'purpose', 'session_id', 'user_id', 'attempts'],
  accounts_refreshtoken: ['id', 'jti', 'user_id', 'created_at', 'expires_at', 'used_at', 'family'],
  accounts_clerkdeletion: ['clerk_user_id', 'attempts', 'queued_at', 'next_attempt_at', 'last_error'],
  game_slot: ['id', 'user_id', 'character', 'current_level', 'total_score', 'collectables', 'created_at', 'last_played', 'revision'],
  game_slot_doc: ['slot_id', 'doc_key', 'schema_version', 'value'],
  game_slot_receipt: ['slot_id', 'token', 'kind', 'value', 'created_at'],
  game_account_doc: ['user_id', 'doc_key', 'value', 'updated_at'],
});

/**
 * SHA-256 of a migration file's text, with line endings normalized so a Windows checkout and a
 * Linux image agree.
 * @param {string} sql
 * @returns {string}
 */
function checksum(sql) {
  return createHash('sha256').update(sql.replace(/\r\n/g, '\n')).digest('hex');
}

/**
 * Throw if any table in `REQUIRED_COLUMNS` lacks a column, naming every missing one.
 * @param {import('pg').Pool | import('pg').PoolClient} db
 */
export async function checkSchema(db) {
  const { rows } = await db.query(
    `SELECT table_name, column_name FROM information_schema.columns
      WHERE table_schema = 'public' AND table_name = ANY($1)`, [Object.keys(REQUIRED_COLUMNS)],
  );
  const have = new Set(rows.map((r) => `${r.table_name}.${r.column_name}`));
  const missing = Object.entries(REQUIRED_COLUMNS)
    .flatMap(([table, columns]) => columns.map((c) => `${table}.${c}`))
    .filter((name) => !have.has(name));
  if (missing.length) {
    throw new Error(`The database is missing columns the API needs: ${missing.join(', ')}. `
      + 'It was probably made by an older Django migration; finish those migrations or rebuild the database.');
  }
}

/**
 * Apply every pending migration, verify the applied ones are unchanged, and check the schema.
 * @param {import('pg').Pool} pool
 * @param {{ log?: (line: string) => void }} [options]
 * @returns {Promise<string[]>} names of the files applied by this call
 */
export async function migrate(pool, { log = () => {} } = {}) {
  const lockClient = await pool.connect();
  try {
    await lockClient.query('SELECT pg_advisory_lock($1)', [LOCK_ID]);
    await lockClient.query(`CREATE TABLE IF NOT EXISTS node_migrations (
      name text PRIMARY KEY,
      applied_at timestamptz NOT NULL DEFAULT now()
    )`);
    await lockClient.query('ALTER TABLE node_migrations ADD COLUMN IF NOT EXISTS checksum text');
    const done = new Map((await lockClient.query('SELECT name, checksum FROM node_migrations')).rows
      .map((r) => [r.name, r.checksum]));
    const files = (await readdir(MIGRATIONS_DIR)).filter((f) => f.endsWith('.sql')).sort();
    const applied = [];
    for (const file of files) {
      const sql = await readFile(path.join(MIGRATIONS_DIR, file), 'utf8');
      const sum = checksum(sql);
      if (done.has(file)) {
        const recorded = done.get(file);
        if (recorded === null) {
          await lockClient.query('UPDATE node_migrations SET checksum = $2 WHERE name = $1', [file, sum]);
        } else if (recorded !== sum) {
          throw new Error(`Migration ${file} was edited after it was applied. Put the change in a new migration file instead.`);
        }
        continue;
      }
      await withTransaction(pool, async (client) => {
        await client.query(sql);
        await client.query('INSERT INTO node_migrations (name, checksum) VALUES ($1, $2)', [file, sum]);
      });
      log(`Applied ${file}`);
      applied.push(file);
    }
    await checkSchema(lockClient);
    return applied;
  } finally {
    await lockClient.query('SELECT pg_advisory_unlock($1)', [LOCK_ID]).catch(() => {});
    lockClient.release();
  }
}

// CLI: `node src/migrate.js`. The signing key is not needed just to migrate.
if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const config = loadConfig(process.env, { requireSigningKey: false });
  const pool = createPool(config.db, 2);
  try {
    console.log('Waiting for PostgreSQL to be ready...');
    await waitForDatabase(pool);
    const applied = await migrate(pool, { log: console.log });
    console.log(applied.length ? `Database migrated (${applied.length} file(s)).` : 'Database already up to date.');
  } finally {
    await pool.end();
  }
}
