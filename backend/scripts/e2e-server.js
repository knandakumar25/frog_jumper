/**
 * Throwaway API server for the Playwright browser sign-in test (tools/playwright/README.md).
 *
 * Replaces Django's `tools/playwright/backend_server.py`. Run it inside the backend container:
 *
 *   docker compose exec -T backend node scripts/e2e-server.js
 *
 * It recreates its own database, `test_browser_handoff` (never `accounts_db`), applies the
 * migrations, keeps every outgoing email in memory, and serves the real API on container port
 * 8002 with test-only routes that the normal server never has:
 *
 *   GET  /__test__/ready  {ready: true}
 *   GET  /__test__/mail   captured messages as [{to, body}]
 *   POST /__test__/seed   {id, email, username}: create an onboarded account for a Clerk test
 *                         user (only `+clerk_test@example.com` addresses are accepted)
 *   POST /__test__/expire-codes {username, purpose}: age only browsercheck fixture codes
 *   POST /__test__/stop   answer, then shut down and drop the test database
 *
 * It refuses to start without a Clerk development key (`sk_test_...`). Ctrl-C also drops the
 * database. Request bodies and URLs are not logged.
 */
import express from 'express';
import pg from 'pg';

import { createApp } from '../src/app.js';
import { loadConfig, withOverrides } from '../src/config.js';
import { createPool } from '../src/db.js';
import { Mailer, MemoryTransport } from '../src/lib/mail.js';
import { migrate } from '../src/migrate.js';
import { createUser } from '../src/lib/users.js';

const TEST_DB = 'test_browser_handoff';
const base = loadConfig();
if (!base.clerkSecretKey.startsWith('sk_test_')) throw new Error('A Clerk development instance (sk_test_ key) is required');
// Lifecycle cases share the runner IP. Rate-limit behavior has separate backend tests.
const config = withOverrides(base, { db: { ...base.db, database: TEST_DB }, authPerIpPerMinute: 1000 });

/** Drop (and optionally recreate) the throwaway database through the normal one. */
async function resetDatabase(recreate) {
  const admin = new pg.Client(base.db);
  await admin.connect();
  await admin.query(`DROP DATABASE IF EXISTS ${TEST_DB} WITH (FORCE)`);
  if (recreate) await admin.query(`CREATE DATABASE ${TEST_DB}`);
  await admin.end();
}

await resetDatabase(true);
const pool = createPool(config.db);
await migrate(pool);
const mailer = new Mailer(new MemoryTransport());
const api = createApp({ config, pool, mailer });

const app = express();
app.use('/__test__', express.json(), (req, res, next) => { res.set('Cache-Control', 'no-store'); next(); });
app.get('/__test__/ready', (req, res) => res.json({ ready: true }));
app.get('/__test__/mail', (req, res) => res.json(mailer.outbox.map((m) => ({ to: m.to, body: m.text }))));
app.post('/__test__/seed', async (req, res) => {
  const { id, email, username } = req.body ?? {};
  if (typeof email !== 'string' || !email.endsWith('@example.com') || !email.includes('+clerk_test')) {
    res.status(400).json({ error: 'Only disposable Clerk test addresses are allowed' });
    return;
  }
  const found = (await pool.query('SELECT username FROM accounts_user WHERE clerk_user_id = $1', [id])).rows[0];
  const user = found ?? await createUser(pool, config, { username, email, country: 'Japan',
    date_of_birth: '1995-04-12', clerk_user_id: id }, null);
  res.json({ username: user.username });
});
/** Age only an isolated fixture's codes, without waiting ten minutes in browser checks. */
app.post('/__test__/expire-codes', async (req, res) => {
  const { username, purpose } = req.body ?? {};
  if (typeof username !== 'string' || !username.startsWith('browsercheck') ||
      !['opt_in', 'login', 'opt_out', 'email_change'].includes(purpose)) {
    res.status(400).json({ error: 'Only browsercheck fixture codes may be expired' });
    return;
  }
  await pool.query(`UPDATE accounts_twofactorcode SET created_at = now() - interval '11 minutes'
    WHERE user_id IN (SELECT id FROM accounts_user WHERE username = $1) AND purpose = $2`, [username, purpose]);
  res.json({ expired: true });
});
app.post('/__test__/stop', (req, res) => {
  res.json({ stopping: true });
  setTimeout(stop, 200);
});
app.use(api);

const server = app.listen(8002, '0.0.0.0', () => console.log('Isolated browser test backend ready'));

let stopping = false;
/** Close the server and pool, then drop only the throwaway database. */
async function stop() {
  if (stopping) return;
  stopping = true;
  server.close();
  await pool.end().catch(() => {});
  await resetDatabase(false);
  console.log('Isolated browser test database removed');
  process.exit(0);
}
process.on('SIGINT', stop);
process.on('SIGTERM', stop);
