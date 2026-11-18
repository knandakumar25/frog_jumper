/**
 * Emailed 6-digit sign-in codes (two-factor authentication).
 *
 * Changes from Django (owner decision, 2026-11-15):
 * - Codes come from a cryptographic random source (`crypto.randomInt`), not `random.choices`.
 * - Only the newest code for a user and purpose (and login session) counts, and it allows
 *   five wrong guesses. On the sixth try it fails even when the code is right, so the code cannot
 *   be guessed by trying all million values within its 10 minutes. Django accepted any older
 *   matching code and allowed unlimited guesses.
 * - The comparison takes the same time whether or not the code matches.
 * Codes are still stored as plain text, which a 10-minute 6-digit code with a five-try limit
 * makes acceptable.
 */
import { randomInt, randomUUID, timingSafeEqual } from 'node:crypto';

import { withTransaction } from '../db.js';
import { HttpError } from './errors.js';

export const CODE_LIFETIME_MINUTES = 10;

/** @returns {string} six digits, leading zeros kept */
export function generateCode() {
  return String(randomInt(0, 1_000_000)).padStart(6, '0');
}

/**
 * Store a new code and return it with its login session id (null for other purposes).
 *
 * Purposes: `opt_in` (turn 2FA on), `login`, `opt_out` (turn 2FA off), `email_change` and
 * `delete_account` (confirm deleting the account, 2026-11-18).
 * At most `config.codesPerUserPer15Min` codes per user and purpose are issued in 15 minutes;
 * more answers 429. Without this cap, logging in again would hand out a fresh code with five new
 * tries every time, which defeats the five-try limit (2026-11-15 EGM review, issue 1).
 * @param {import('pg').Pool} pool
 * @param {number} userId
 * @param {'opt_in' | 'login' | 'opt_out' | 'email_change' | 'delete_account'} purpose
 * @param {{codesPerUserPer15Min: number}} config
 * @returns {Promise<{code: string, sessionId: string | null}>}
 */
export async function createCode(pool, userId, purpose, config) {
  return withTransaction(pool, async (client) => {
    // Serialize issuance and verification for an account, including the count/insert boundary.
    await client.query('SELECT id FROM accounts_user WHERE id = $1 FOR UPDATE', [userId]);
    const recent = (await client.query(
      `SELECT count(*)::int AS n FROM accounts_twofactorcode
        WHERE user_id = $1 AND purpose = $2 AND created_at > now() - interval '15 minutes'`,
      [userId, purpose],
    )).rows[0].n;
    if (recent >= config.codesPerUserPer15Min) {
      throw new HttpError(429, { detail: 'Too many codes requested. Try again in 15 minutes.' }, { 'Retry-After': '900' });
    }
    const code = generateCode();
    const sessionId = purpose === 'login' ? randomUUID() : null;
    await client.query(
      `INSERT INTO accounts_twofactorcode (user_id, code, created_at, is_verified, purpose, session_id, attempts)
       VALUES ($1, $2, now(), false, $3, $4, 0)`,
      [userId, code, purpose, sessionId],
    );
    return { code, sessionId };
  });
}

/**
 * Check only the newest code, rejecting it if consumed or expired. Its attempt count is raised and it is
 * marked used when it matches. Returns false for no code, expired, too many tries or mismatch.
 * @param {import('pg').Pool} pool
 * @param {import('../config.js').Config} config
 * @param {{userId: number, code: unknown, purpose: 'opt_in' | 'login' | 'opt_out' | 'email_change' | 'delete_account',
 *   sessionId?: string | null}} attempt
 * @returns {Promise<boolean>}
 */
export async function verifyCode(pool, config, { userId, code, purpose, sessionId = null }) {
  const given = Buffer.from(String(code ?? ''));
  return withTransaction(pool, async (client) => {
    await client.query('SELECT id FROM accounts_user WHERE id = $1 FOR UPDATE', [userId]);
    const row = (await client.query(
      `SELECT id, code, attempts, is_verified, created_at > now() - make_interval(mins => $4) AS live
         FROM accounts_twofactorcode
        WHERE user_id = $1 AND purpose = $2
          AND ($3::uuid IS NULL OR session_id = $3::uuid)
        ORDER BY created_at DESC, id DESC LIMIT 1 FOR UPDATE`,
      [userId, purpose, sessionId, CODE_LIFETIME_MINUTES],
    )).rows[0];
    if (!row || row.is_verified || !row.live || row.attempts >= config.twoFactorMaxAttempts) return false;
    const stored = Buffer.from(row.code);
    const match = stored.length === given.length && timingSafeEqual(stored, given);
    await client.query(
      'UPDATE accounts_twofactorcode SET attempts = attempts + 1, is_verified = $2 WHERE id = $1',
      [row.id, match],
    );
    return match;
  });
}

/**
 * True when `value` looks like a UUID (hyphenated or 32 hex digits). Checked before a value is
 * sent to PostgreSQL as a uuid, which would otherwise raise an error.
 * @param {unknown} value
 * @returns {boolean}
 */
export function isUuid(value) {
  return typeof value === 'string'
    && /^(\{?[0-9a-f]{8}-?[0-9a-f]{4}-?[0-9a-f]{4}-?[0-9a-f]{4}-?[0-9a-f]{12}\}?)$/i.test(value);
}
