/**
 * Start the accounts API: read settings, wait for PostgreSQL, apply migrations, listen on
 * port 8000 (the port nginx, the game and the website already use).
 *
 * Once an hour it deletes expired refresh tokens, old 2FA codes and stale browser sign-in
 * requests (`purgeExpired`), so those tables do not grow without limit, and retries Clerk
 * deletions for deleted accounts that Clerk could not take at the time (`processClerkDeletions`).
 *
 * SIGTERM and SIGINT (`docker compose stop`) close the listener and the pool before exiting.
 */
import { createApp } from './app.js';
import { loadConfig } from './config.js';
import { createPool, waitForDatabase } from './db.js';
import { purgeExpired } from './lib/tokens.js';
import { processClerkDeletions } from './lib/accountDeletion.js';
import { deleteClerkUser } from './lib/clerk.js';
import { migrate } from './migrate.js';

const config = loadConfig();
const pool = createPool(config.db);
await waitForDatabase(pool);
await migrate(pool, { log: console.log });

const server = createApp({ config, pool }).listen(config.port, '0.0.0.0', () => {
  console.log(`Axiom Force accounts API listening on port ${config.port}`);
});

/** Delete expired rows; errors are logged and retried next hour. */
async function cleanUp() {
  try {
    const removed = await purgeExpired(pool);
    if (removed.refreshTokens || removed.codes || removed.browserSessions) {
      console.log(`Cleanup removed ${removed.refreshTokens} refresh token(s), ${removed.codes} code(s), `
        + `${removed.browserSessions} browser request(s)`);
    }
  } catch (error) {
    console.error('Cleanup failed:', error.message);
  }
  try {
    const clerk = await processClerkDeletions(pool, config, deleteClerkUser);
    if (clerk.deleted || clerk.failed) console.log(`Clerk deletions: ${clerk.deleted} done, ${clerk.failed} to retry`);
    if (clerk.stuck) console.warn(`${clerk.stuck} Clerk deletion(s) failed for 14 days; see accounts_clerkdeletion.`);
  } catch (error) {
    console.error('Clerk deletion retry failed:', error.message);
  }
}
cleanUp();
setInterval(cleanUp, 60 * 60_000).unref();

/** Stop accepting requests, let open ones finish, then release database connections. */
function shutdown() {
  server.close(() => pool.end().finally(() => process.exit(0)));
  setTimeout(() => process.exit(1), 10_000).unref();
}
process.on('SIGTERM', shutdown);
process.on('SIGINT', shutdown);
