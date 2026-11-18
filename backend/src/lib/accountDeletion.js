/**
 * Player account deletion (owner decision, 2026-11-18; required by Apple and Google for any app
 * that lets people create an account).
 *
 * `deleteAccount` removes the account and every row that belongs to it in one transaction:
 * refresh tokens, 2FA codes, browser sign-in requests, pending registrations made with the same
 * Clerk id or email, and the `accounts_user` row. Child rows are deleted explicitly rather than
 * relying on ON DELETE CASCADE, because a database first made by Django has foreign keys with no
 * ON DELETE action. The user row is locked first, the same lock that refresh-token rotation takes,
 * so a refresh racing the deletion either finishes first or finds no user.
 *
 * The player's Clerk identity is deleted too. Its id is queued in `accounts_clerkdeletion` inside
 * the same transaction, so it cannot be lost; `processClerkDeletions` then calls Clerk (once right
 * after the request, then from the hourly cleanup) and removes the row when Clerk confirms. Until
 * then a Clerk session for that identity finds no account and would be sent to onboarding.
 *
 * The account's game saves are deleted too (2026-11-18): receipts, slot documents, slots and
 * account documents, explicitly and before the user row.
 */

/** Retry delays after each failed Clerk attempt: 1 hour, 6 hours, then daily. */
const RETRY_DELAYS_HOURS = [1, 6, 24];
/** Stop retrying after this many days; the row stays for the owner to resolve. */
export const CLERK_RETRY_DAYS = 14;

/**
 * Delete one account and everything linked to it.
 * @param {import('pg').Pool} pool
 * @param {number | string} userId `accounts_user.id`
 * @returns {Promise<{deleted: boolean, email: string | null, clerkUserId: string | null}>}
 *   `deleted` is false when the account was already gone.
 */
export async function deleteAccount(pool, userId) {
  const client = await pool.connect();
  try {
    await client.query('BEGIN');
    const found = await client.query(
      'SELECT id, email, clerk_user_id FROM accounts_user WHERE id = $1 FOR UPDATE', [userId]);
    const user = found.rows[0];
    if (!user) {
      await client.query('ROLLBACK');
      return { deleted: false, email: null, clerkUserId: null };
    }
    await client.query('DELETE FROM accounts_refreshtoken WHERE user_id = $1', [user.id]);
    await client.query('DELETE FROM accounts_twofactorcode WHERE user_id = $1', [user.id]);
    // Pending registrations hold the same person's name, email and sign-in IP.
    await client.query(
      `DELETE FROM accounts_pendingregistration
        WHERE ($1::text IS NOT NULL AND clerk_user_id = $1) OR lower(email) = lower($2)
           OR browser_session_id IN (SELECT id FROM accounts_browsersession WHERE user_id = $3)`,
      [user.clerk_user_id, user.email, user.id]);
    await client.query('DELETE FROM accounts_browsersession WHERE user_id = $1', [user.id]);
    // Game saves (2026-11-18): children first, so this also works without ON DELETE CASCADE.
    await client.query('DELETE FROM game_slot_receipt WHERE slot_id IN (SELECT id FROM game_slot WHERE user_id = $1)', [user.id]);
    await client.query('DELETE FROM game_slot_doc WHERE slot_id IN (SELECT id FROM game_slot WHERE user_id = $1)', [user.id]);
    await client.query('DELETE FROM game_slot WHERE user_id = $1', [user.id]);
    await client.query('DELETE FROM game_account_doc WHERE user_id = $1', [user.id]);
    if (user.clerk_user_id) {
      await client.query(
        `INSERT INTO accounts_clerkdeletion (clerk_user_id) VALUES ($1)
         ON CONFLICT (clerk_user_id) DO UPDATE SET next_attempt_at = now()`, [user.clerk_user_id]);
    }
    await client.query('DELETE FROM accounts_user WHERE id = $1', [user.id]);
    await client.query('COMMIT');
    return { deleted: true, email: user.email, clerkUserId: user.clerk_user_id };
  } catch (error) {
    await client.query('ROLLBACK').catch(() => {});
    throw error;
  } finally {
    client.release();
  }
}

/**
 * Try every due queued Clerk deletion (or just `onlyId`). Success, or Clerk saying the user is
 * already gone, removes the row; a failure counts an attempt and schedules the next one. Rows
 * older than `CLERK_RETRY_DAYS` are no longer tried and are reported in `stuck`.
 * @param {import('pg').Pool} pool
 * @param {import('../config.js').Config} config
 * @param {(id: string, config: import('../config.js').Config) => Promise<unknown>} deleteUser
 * @param {{ onlyId?: string, logger?: Pick<Console, 'warn'> }} [options]
 * @returns {Promise<{deleted: number, failed: number, stuck: number}>}
 */
export async function processClerkDeletions(pool, config, deleteUser, { onlyId, logger = console } = {}) {
  const due = await pool.query(
    `SELECT clerk_user_id, attempts FROM accounts_clerkdeletion
      WHERE next_attempt_at <= now() AND queued_at > now() - make_interval(days => $1)
        AND ($2::text IS NULL OR clerk_user_id = $2)
      ORDER BY next_attempt_at LIMIT 100`, [CLERK_RETRY_DAYS, onlyId ?? null]);
  let deleted = 0;
  let failed = 0;
  for (const row of due.rows) {
    try {
      await deleteUser(row.clerk_user_id, config);
      await pool.query('DELETE FROM accounts_clerkdeletion WHERE clerk_user_id = $1', [row.clerk_user_id]);
      deleted += 1;
    } catch (error) {
      failed += 1;
      const hours = RETRY_DELAYS_HOURS[Math.min(row.attempts, RETRY_DELAYS_HOURS.length - 1)];
      await pool.query(
        `UPDATE accounts_clerkdeletion
            SET attempts = attempts + 1, last_error = $2, next_attempt_at = now() + make_interval(hours => $3)
          WHERE clerk_user_id = $1`, [row.clerk_user_id, String(error.message).slice(0, 500), hours]);
      logger.warn(`Clerk deletion for a deleted account failed (attempt ${row.attempts + 1}): ${error.message}`);
    }
  }
  const stuck = await pool.query(
    'SELECT count(*)::int AS n FROM accounts_clerkdeletion WHERE queued_at <= now() - make_interval(days => $1)',
    [CLERK_RETRY_DAYS]);
  return { deleted, failed, stuck: stuck.rows[0].n };
}
