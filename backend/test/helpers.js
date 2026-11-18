/**
 * Shared test harness.
 *
 * Each test file runs in its own process (`node --test --test-concurrency=1`, one file at a
 * time). `setupDatabase()` recreates a throwaway database, `accounts_test`, on the same server as
 * the app (the `db` container), applies the migrations, and `resetDatabase()` empties the tables
 * between tests. Nothing touches the real `accounts_db`.
 *
 * `makeApp()` builds the app with an in-memory mailer (`outbox()`), fake Clerk calls that fail
 * unless a test replaces them, and a low PBKDF2 round count so creating users is fast.
 */
import { after, before, beforeEach } from 'node:test';
import pg from 'pg';

import { createApp } from '../src/app.js';
import { loadConfig, withOverrides } from '../src/config.js';
import { createPool } from '../src/db.js';
import { Mailer, MemoryTransport } from '../src/lib/mail.js';
import { migrate } from '../src/migrate.js';
import { createUser } from '../src/lib/users.js';

export const TEST_DB = 'accounts_test';
export const SIGNING_KEY = 'test-signing-key-0123456789abcdefghijklmnop';

/** Settings for tests: the test database, a fixed key, fast hashing, no Clerk, no Resend. */
export const baseConfig = loadConfig({
  ...process.env,
  DB_NAME: TEST_DB,
  JWT_SIGNING_KEY: SIGNING_KEY,
  PASSWORD_ITERATIONS: '1000',
  CLERK_SECRET_KEY: '',
  CLERK_ISSUER: '',
  RESEND_API_KEY: '',
  FRONTEND_URL: 'http://localhost:5173',
  DEFAULT_FROM_EMAIL: 'Axiom Force <onboarding@resend.dev>',
  // supertest connects over loopback; treat it as the trusted proxy so tests can set
  // X-Forwarded-For, and skip the `website` DNS lookup that only resolves inside Compose.
  TRUSTED_PROXY_HOST: '',
  TRUSTED_PROXY_ADDRESSES: '127.0.0.1,::1',
});

const adminConfig = { ...baseConfig.db, database: process.env.DB_NAME ?? 'accounts_db' };

/** @type {pg.Pool} */
export let pool;

/**
 * Register before/after hooks that create the test database and drop it at the end of the file,
 * plus a beforeEach that empties every table.
 */
export function useDatabase() {
  before(async () => {
    const admin = new pg.Client(adminConfig);
    await admin.connect();
    await admin.query(`DROP DATABASE IF EXISTS ${TEST_DB} WITH (FORCE)`);
    await admin.query(`CREATE DATABASE ${TEST_DB}`);
    await admin.end();
    pool = createPool(baseConfig.db, 6);
    await migrate(pool);
  });
  beforeEach(resetDatabase);
  after(async () => {
    await pool?.end();
    const admin = new pg.Client(adminConfig);
    await admin.connect();
    await admin.query(`DROP DATABASE IF EXISTS ${TEST_DB} WITH (FORCE)`);
    await admin.end();
  });
}

/** Empty all account tables and restart their id counters. */
export async function resetDatabase() {
  await pool.query(`TRUNCATE accounts_refreshtoken, accounts_twofactorcode, accounts_pendingregistration,
    accounts_browsersession, accounts_clerkdeletion, game_slot_receipt, game_slot_doc, game_slot,
    game_account_doc, accounts_user RESTART IDENTITY CASCADE`);
}

/** A Clerk call that fails the test if reached without being replaced. */
function unexpectedClerk() {
  throw new Error('Clerk was called but this test did not provide a fake');
}

/**
 * Build an app for one test.
 * @param {Partial<import('../src/config.js').Config>} [overrides] settings to change
 * @returns {{ app: import('express').Express, deps: import('../src/app.js').Deps, outbox: () => object[] }}
 */
export function makeApp(overrides = {}) {
  const mailer = new Mailer(new MemoryTransport());
  const app = createApp({
    config: withOverrides(baseConfig, overrides),
    pool,
    mailer,
    clerk: { subject: unexpectedClerk, fetchUser: unexpectedClerk, deleteUser: unexpectedClerk },
  });
  return { app, deps: app.locals.deps, outbox: () => mailer.outbox };
}

/**
 * Create a user directly (no welcome mail), like Django's `User.objects.create_user` in tests.
 * @param {object} fields username, email and optional profile columns
 * @param {string | null} [password] null stores an unusable password
 * @returns {Promise<object>}
 */
export function makeUser(fields, password = null) {
  return createUser(pool, baseConfig, fields, password);
}
