/**
 * Email-gated onboarding: Clerk signup → country and date of birth → game account.
 *
 *   POST /api/auth/onboarding/start/          email a one-time link
 *   GET  /api/auth/onboarding/validate-token/ is the link still good? (does not use it up)
 *   POST /api/auth/onboarding/complete/       create the account, return tokens
 *
 * Clerk owns the credentials. The account is created only at `complete`, and only by someone who
 * opened the emailed link: the token travels in the email alone and is stored as a SHA-256 hash,
 * so `start` never returns it. A game sign-in request can ride along (its public id and locale
 * survive to `complete`), but approving the game is still a separate click.
 *
 * None of these routes authenticate, so a stale website token never blocks them. The checks in
 * `start` run in the same order as Django's, cheapest and safest first (see the numbered steps).
 */
import { randomBytes } from 'node:crypto';
import express from 'express';

import { withTransaction } from '../db.js';
import { ClerkError, ClerkUnavailable, ClerkUserNotFound, isValidUserId } from '../lib/clerk.js';
import { countryCommonNames } from '../lib/countries.js';
import { onboardingEmail, sendWelcomeEmail } from '../lib/emails.js';
import { FieldErrors, MESSAGES } from '../lib/errors.js';
import { issueTokenPair } from '../lib/tokens.js';
import { createUser, profileOf, readString } from '../lib/users.js';
import { liveRequest, sha256Hex } from './browser.js';
import { route } from './methods.js';

/**
 * `a***@example.com`: enough for the user to recognise the address, not enough to leak it.
 * @param {string} email
 * @returns {string}
 */
export function maskEmail(email) {
  const at = email.indexOf('@');
  if (at < 0 || at === email.length - 1) return '***';
  const local = email.slice(0, at);
  const domain = email.slice(at + 1);
  return local.length <= 1 ? `*@${domain}` : `${local[0]}***@${domain}`;
}

/**
 * Upper bound on onboarding emails sent in the last hour, optionally from one IP. Sums
 * `sent_count` of every row touched in the window, which over-counts on purpose: these numbers
 * only gate rate limits, where erring high is safe.
 * @param {import('pg').Pool} pool
 * @param {string | null} [ip]
 * @returns {Promise<number>}
 */
async function sendsLastHour(pool, ip = null) {
  const result = await pool.query(
    `SELECT COALESCE(SUM(sent_count), 0)::int AS total FROM accounts_pendingregistration
      WHERE last_sent_at >= now() - interval '1 hour' AND ($1::inet IS NULL OR request_ip = $1::inet)`,
    [ip],
  );
  return result.rows[0].total;
}

/**
 * A unique username for the new account: the Clerk username, else the email's local part, reduced
 * to the allowed characters (letters, digits, `_ . @ + -`), else "operator"; `_1`, `_2`, ... added
 * on a clash.
 * @param {import('pg').PoolClient} client
 * @param {{clerk_username: string, email: string}} pending
 * @returns {Promise<string>}
 */
async function deriveUsername(client, pending) {
  let base = (pending.clerk_username ?? '').trim() || pending.email.split('@')[0];
  base = base.replace(/[^\p{L}\p{N}\p{M}_.@+-]/gu, '') || 'operator';
  base = base.slice(0, 150);
  let name = base;
  for (let counter = 1; ; counter += 1) {
    const taken = await client.query('SELECT 1 FROM accounts_user WHERE username = $1', [name]);
    if (!taken.rowCount) return name;
    const suffix = `_${counter}`;
    name = base.slice(0, 150 - suffix.length) + suffix;
  }
}

/**
 * Today's date in UTC as YYYY-MM-DD (Django ran with TIME_ZONE = 'UTC').
 * @returns {string}
 */
function todayUtc() {
  return new Date().toISOString().slice(0, 10);
}

/**
 * Validate the `complete` body like Django's OnboardingCompleteSerializer; throws 400.
 * @param {Record<string, unknown>} body
 * @returns {{token: string, country: string, date_of_birth: string}}
 */
export function validateCompletion(body) {
  const errors = new FieldErrors();
  const token = readString(errors, 'token', body.token, { required: true });
  const country = readString(errors, 'country', body.country, { required: true, maxLength: 100 });
  if (country !== undefined && !countryCommonNames().has(country)) errors.add('country', 'Unknown country.');
  let dob;
  if (body.date_of_birth === undefined) errors.add('date_of_birth', MESSAGES.required);
  else if (body.date_of_birth === null) errors.add('date_of_birth', MESSAGES.null);
  else {
    const text = String(body.date_of_birth).trim();
    const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(text);
    const date = match ? new Date(Date.UTC(+match[1], +match[2] - 1, +match[3])) : null;
    // Reject impossible days such as 2024-02-30 (Date would roll them into March).
    if (!date || date.getUTCMonth() !== +match[2] - 1 || date.getUTCDate() !== +match[3]) {
      errors.add('date_of_birth', MESSAGES.dateInvalid);
    } else if (text > todayUtc()) {
      errors.add('date_of_birth', 'Date of birth cannot be in the future.');
    } else if (+match[1] < 1900) {
      errors.add('date_of_birth', 'Date of birth is not plausible.');
    } else dob = text; // No age gate: sanity checks only, by decision.
  }
  errors.throwIfAny();
  return { token, country, date_of_birth: dob };
}

/**
 * Build the router.
 * @param {import('../app.js').Deps} deps
 * @returns {import('express').Router}
 */
export function onboardingRoutes(deps) {
  const router = express.Router();
  const { pool } = deps;

  route(router, '/onboarding/start/', {
    post: [async (req, res) => {
      const config = deps.config;
      const body = req.body ?? {};
      const clerkUserId = body.clerk_user_id;
      const fail = (status, error) => res.status(status).json({ error });

      // 1. A game request may join only with proof of the same Clerk identity.
      let browserSession = null;
      if (body.session_id) {
        if (await deps.clerk.subject(body.clerk_token ?? '', config) !== clerkUserId) {
          return fail(403, 'identity_mismatch');
        }
        browserSession = await withTransaction(pool, (client) => liveRequest(client, body));
        if (!browserSession) return fail(410, 'request_expired');
      }
      // 2. A made-up id never reaches the network.
      if (!isValidUserId(clerkUserId)) return fail(400, 'invalid_clerk_user_id');
      // 3. Without a key the endpoint is inert.
      if (!config.clerkSecretKey) return fail(503, 'clerk_not_configured');
      // 4. Global cap first: the billing circuit breaker, checked before any Clerk call.
      if (await sendsLastHour(pool) >= config.onboardingGlobalHourlyLimit) return fail(429, 'rate_limited');
      // 5. A finished registration cannot start again.
      const existing = (await pool.query(
        'SELECT * FROM accounts_pendingregistration WHERE clerk_user_id = $1', [clerkUserId],
      )).rows[0];
      if (existing?.completed_at) return fail(409, 'already_registered');
      // 6. Per-IP cap.
      const ip = deps.clientIp(req);
      if (ip && await sendsLastHour(pool, ip) >= config.onboardingIpHourlyLimit) return fail(429, 'rate_limited');
      // 7. Ask Clerk for the user.
      let clerkUser;
      try {
        clerkUser = await deps.clerk.fetchUser(clerkUserId, config);
      } catch (error) {
        if (error instanceof ClerkUserNotFound) return fail(404, 'unknown_clerk_user');
        // Usually this machine, not Clerk: TLS interception by antivirus software, for example.
        if (error instanceof ClerkUnavailable) return fail(502, 'clerk_unreachable');
        if (error instanceof ClerkError) return fail(502, 'clerk_error');
        throw error;
      }
      // 8. Only a verified mailbox gets an account-creation link (Clerk email verification must
      //    stay on, or every signup answers 403).
      if (!clerkUser.email) return fail(403, 'clerk_email_missing');
      if (!clerkUser.email_verified) return fail(403, 'email_not_verified');
      // 9. Already linked to an account. Whether another account uses this email is deliberately
      //    not checked here (that would reveal registered addresses); `complete` handles it.
      const linked = await pool.query('SELECT 1 FROM accounts_user WHERE clerk_user_id = $1', [clerkUserId]);
      if (linked.rowCount) return fail(409, 'already_registered');

      // 10. Inside the cooldown: attach the game request if given, but do not mail again.
      const now = new Date();
      const cooldown = config.onboardingResendCooldown;
      if (existing?.last_sent_at) {
        if (browserSession) {
          await pool.query('UPDATE accounts_pendingregistration SET browser_session_id = $2 WHERE id = $1',
            [existing.id, browserSession.id]);
        }
        const elapsed = (now - existing.last_sent_at) / 1000;
        if (elapsed < cooldown) {
          return res.json({ status: 'already_sent', email: maskEmail(existing.email),
            expires_at: existing.expires_at, retry_after: Math.trunc(cooldown - elapsed) });
        }
      }
      // 11. Lifetime send cap per registration.
      if (existing && existing.sent_count >= config.onboardingMaxSends) return fail(429, 'too_many_sends');

      // 12. Mail BEFORE saving: a failed send leaves the previous row (and its live link) alone,
      //     and the cooldown and cap count deliveries only.
      const rawToken = randomBytes(32).toString('base64url');
      const expiresAt = new Date(now.getTime() + config.onboardingTokenTtlHours * 3600_000);
      try {
        await deps.mailer.send(onboardingEmail(clerkUser.email, rawToken, clerkUser.first_name, config));
      } catch (error) {
        console.error(`Failed to send onboarding email to ${clerkUser.email}:`, error);
        return fail(502, 'email_send_failed');
      }

      // 13. Rotate the stored token (a hash cannot be re-sent; rotating kills the old link) or
      //     insert the first row.
      let pending;
      const fields = [clerkUser.email, sha256Hex(rawToken), clerkUser.first_name, clerkUser.last_name,
        clerkUser.username, ip, now, expiresAt, browserSession?.id ?? null];
      if (existing) {
        pending = (await pool.query(
          `UPDATE accounts_pendingregistration SET email = $2, token_hash = $3, first_name = $4,
              last_name = $5, clerk_username = $6, request_ip = $7, sent_count = sent_count + 1,
              last_sent_at = $8, expires_at = $9, browser_session_id = $10
            WHERE id = $1 RETURNING *`, [existing.id, ...fields],
        )).rows[0];
      } else {
        try {
          pending = (await pool.query(
            `INSERT INTO accounts_pendingregistration (clerk_user_id, email, token_hash, first_name,
                last_name, clerk_username, request_ip, sent_count, created_at, last_sent_at, expires_at,
                completed_at, browser_session_id)
             VALUES ($1, $2, $3, $4, $5, $6, $7, 1, now(), $8, $9, NULL, $10) RETURNING *`,
            [clerkUserId, ...fields],
          )).rows[0];
        } catch (error) {
          if (error.code !== '23505') throw error;
          // A parallel first request won the insert; our link was never stored, so it is already
          // dead. Report what the cooldown would, not a 500.
          return res.json({ status: 'already_sent', email: maskEmail(clerkUser.email), retry_after: cooldown });
        }
      }
      // 14. The token is never in the response: that would bypass the mailbox check.
      return res.json({ status: 'email_sent', email: maskEmail(pending.email), expires_at: pending.expires_at });
    }],
  });

  /** GET onboarding/validate-token/?token=: report whether the link is usable, without using it. */
  route(router, '/onboarding/validate-token/', {
    get: [async (req, res) => {
      const raw = typeof req.query.token === 'string' ? req.query.token : '';
      if (!raw) return res.status(400).json({ error: 'token_required' });
      const pending = (await pool.query(
        `SELECT p.*, b.locale AS browser_locale FROM accounts_pendingregistration p
           LEFT JOIN accounts_browsersession b ON b.id = p.browser_session_id
          WHERE p.token_hash = $1`, [sha256Hex(raw)],
      )).rows[0];
      if (!pending) return res.status(404).json({ error: 'invalid_token' });
      if (pending.completed_at) return res.status(409).json({ error: 'already_completed' });
      if (pending.expires_at <= new Date()) return res.status(410).json({ error: 'token_expired' });
      return res.json({ status: 'valid', email: pending.email, first_name: pending.first_name,
        last_name: pending.last_name, expires_at: pending.expires_at, browser_locale: pending.browser_locale ?? null });
    }],
  });

  /** POST onboarding/complete/: use the token once, create the account, return tokens. */
  route(router, '/onboarding/complete/', {
    post: [async (req, res) => {
      const config = deps.config;
      const data = validateCompletion(req.body ?? {});
      const pending = (await pool.query(
        `SELECT p.*, b.session_id AS browser_session_uuid, b.locale AS browser_locale
           FROM accounts_pendingregistration p
           LEFT JOIN accounts_browsersession b ON b.id = p.browser_session_id
          WHERE p.token_hash = $1`, [sha256Hex(data.token)],
      )).rows[0];
      if (!pending) return res.status(404).json({ error: 'invalid_token' });
      if (pending.completed_at) return res.status(409).json({ error: 'already_completed' });
      if (pending.expires_at <= new Date()) return res.status(410).json({ error: 'token_expired' });
      // Checked before the insert so an older password account with this address is a clean 409.
      const clash = await pool.query('SELECT 1 FROM accounts_user WHERE clerk_user_id = $1 OR email = $2',
        [pending.clerk_user_id, pending.email]);
      if (clash.rowCount) return res.status(409).json({ error: 'already_registered' });

      let user;
      try {
        user = await withTransaction(pool, async (client) => {
          // Compare-and-swap: this conditional UPDATE is the real guard against a double submit.
          const consumed = await client.query(
            `UPDATE accounts_pendingregistration SET completed_at = now()
              WHERE id = $1 AND completed_at IS NULL`, [pending.id],
          );
          if (consumed.rowCount !== 1) return null;
          // No password: Clerk stays the only credential authority.
          return createUser(client, config, {
            username: await deriveUsername(client, pending),
            email: pending.email,
            first_name: pending.first_name,
            last_name: pending.last_name,
            country: data.country,
            date_of_birth: data.date_of_birth,
            clerk_user_id: pending.clerk_user_id,
          }, null);
        });
      } catch (error) {
        // Two submissions of one link, or two Clerk users racing for one address.
        if (error.code === '23505') return res.status(409).json({ error: 'already_registered' });
        throw error;
      }
      if (!user) return res.status(409).json({ error: 'already_completed' });
      await sendWelcomeEmail(deps.mailer, config, user);
      const tokens = await issueTokenPair(pool, config, user.id);
      return res.json({
        refresh: tokens.refresh,
        access: tokens.access,
        user: profileOf(user),
        // Public id only: the player must still approve the game request explicitly.
        browser_session_id: pending.browser_session_uuid ?? null,
        browser_locale: pending.browser_locale ?? null,
      });
    }],
  });

  return router;
}
