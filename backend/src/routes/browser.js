/**
 * Browser sign-in for the desktop game (a device-code style flow), under `/api/auth/browser-*`.
 *
 * 1. The game calls `browser-start` and gets a public `session_id`, a private `secret` (returned
 *    once, stored only as a SHA-256 hash) and an `auth_url` on the website. The secret is never
 *    put in the URL.
 * 2. The website signs the player in (Clerk, or the API's own token after onboarding) and calls
 *    `browser-confirm` with the public id. That binds one account to the request, once.
 * 3. The game polls `browser-poll` with its secret. The first poll after approval gets the tokens
 *    and profile; the request is then spent. `browser-cancel` ends it early.
 *
 * Requests live 15 minutes. Every response carries `Cache-Control: no-store`. These routes ignore
 * the website's own Bearer token except as `browser-confirm`'s fallback proof, and are limited to
 * 90 requests a minute per IP (`throttle.js`). Confirm, poll and cancel lock the request row
 * (`SELECT ... FOR UPDATE`) so they cannot interleave.
 */
import { createHash, randomBytes, randomUUID, timingSafeEqual } from 'node:crypto';
import express from 'express';

import { userFromRequest } from '../lib/auth.js';
import { withTransaction } from '../db.js';
import { issueTokenPair } from '../lib/tokens.js';
import { isUuid } from '../lib/twoFactor.js';
import { findUserById, profileOf } from '../lib/users.js';
import { route } from './methods.js';

/**
 * SHA-256 hex of a secret or token; the plain value is never stored.
 * @param {string} value
 * @returns {string}
 */
export function sha256Hex(value) {
  return createHash('sha256').update(value, 'utf8').digest('hex');
}

/**
 * Lock and return a live request, or null.
 *
 * Not live: malformed id, no row, an old row with no secret, past expiry, already redeemed or
 * cancelled. With `privateCheck`, the caller's `secret` (a string of at most 128 characters) must
 * also hash to the stored value.
 * @param {import('pg').PoolClient} client inside a transaction
 * @param {Record<string, unknown>} data request body
 * @param {boolean} [privateCheck]
 * @returns {Promise<object | null>} the `accounts_browsersession` row
 */
export async function liveRequest(client, data, privateCheck = false) {
  const sessionId = data?.session_id;
  if (!isUuid(String(sessionId ?? ''))) return null;
  const row = (await client.query(
    'SELECT * FROM accounts_browsersession WHERE session_id = $1 FOR UPDATE', [String(sessionId)],
  )).rows[0];
  if (!row || !row.secret_hash || !row.expires_at || row.expires_at <= new Date()) return null;
  if (row.consumed_at || row.cancelled_at) return null;
  if (privateCheck) {
    const secret = data.secret ?? '';
    if (typeof secret !== 'string' || secret.length > 128) return null;
    const given = Buffer.from(sha256Hex(secret));
    const stored = Buffer.from(row.secret_hash);
    if (given.length !== stored.length || !timingSafeEqual(given, stored)) return null;
  }
  return row;
}

/**
 * Build the router.
 * @param {import('../app.js').Deps} deps
 * @returns {import('express').Router}
 */
export function browserRoutes(deps) {
  const router = express.Router();
  const { pool } = deps;
  // Keep device secrets and tokens out of browser and proxy caches, including on errors.
  const noStore = (req, res, next) => {
    res.set({ 'Cache-Control': 'no-store', Pragma: 'no-cache' });
    next();
  };
  const guard = [noStore, deps.browserThrottle];
  const expired = (res) => res.status(410).json({ error: 'request_expired' });

  /** POST browser-start/: create a 15-minute request; the secret is returned this once only. */
  route(router, '/browser-start/', {
    post: [...guard, async (req, res) => {
      const intent = req.body?.intent === undefined ? 'login' : req.body.intent;
      const locale = req.body?.locale === undefined ? 'en-US' : req.body.locale;
      if (!['login', 'signup'].includes(intent) || typeof locale !== 'string' || locale.length > 32) {
        res.status(400).json({ error: 'invalid_request' });
        return;
      }
      const secret = randomBytes(32).toString('base64url');
      const row = (await pool.query(
        `INSERT INTO accounts_browsersession (session_id, created_at, is_authorized, user_id, secret_hash,
            expires_at, consumed_at, cancelled_at, intent, locale)
         VALUES ($1, now(), false, NULL, $2, now() + interval '15 minutes', NULL, NULL, $3, $4)
         RETURNING session_id, expires_at`,
        [randomUUID(), sha256Hex(secret), intent, locale],
      )).rows[0];
      const query = new URLSearchParams({ session_id: row.session_id, intent, locale });
      res.json({
        session_id: row.session_id,
        secret,
        expires_at: row.expires_at,
        auth_url: `${deps.config.frontendUrl.replace(/\/+$/, '')}/auth/browser-confirm?${query}`,
      });
    }],
  });

  /**
   * POST browser-confirm/: approve a request for one account. Proof is a verified Clerk token
   * (`clerk_token`) for an onboarded account, or else the website's own Bearer token. A Clerk user
   * with no game profile gets 409 onboarding_required.
   */
  route(router, '/browser-confirm/', {
    post: [...guard, async (req, res) => {
      const body = req.body ?? {};
      let user;
      if (body.clerk_token) {
        const subject = await deps.clerk.subject(body.clerk_token, deps.config);
        user = (await pool.query(
          'SELECT * FROM accounts_user WHERE clerk_user_id = $1 AND is_active', [subject],
        )).rows[0];
        if (!user) {
          res.status(409).json({ error: 'onboarding_required' });
          return;
        }
      } else {
        user = await userFromRequest(deps, req, false);
        if (!user) {
          res.status(403).json({ detail: 'Sign in to approve this request' });
          return;
        }
      }
      const outcome = await withTransaction(pool, async (client) => {
        const row = await liveRequest(client, body);
        if (!row) return 'expired';
        if (row.is_authorized) return 'already';
        await client.query('UPDATE accounts_browsersession SET user_id = $2, is_authorized = true WHERE id = $1',
          [row.id, user.id]);
        return 'ok';
      });
      if (outcome === 'expired') expired(res);
      else if (outcome === 'already') res.status(409).json({ error: 'already_authorized' });
      else res.json({ status: 'authorized' });
    }],
  });

  /** POST browser-poll/: pending, or (once) the tokens and profile. */
  route(router, '/browser-poll/', {
    post: [...guard, async (req, res) => {
      const result = await withTransaction(pool, async (client) => {
        const row = await liveRequest(client, req.body ?? {}, true);
        if (!row) return { status: 410, body: { error: 'request_expired' } };
        if (!row.is_authorized || !row.user_id) return { status: 200, body: { status: 'pending' } };
        const user = await findUserById(client, row.user_id);
        if (!user?.is_active) return { status: 403, body: { error: 'inactive_account' } };
        const spent = await client.query(
          `UPDATE accounts_browsersession SET consumed_at = now()
            WHERE id = $1 AND consumed_at IS NULL AND cancelled_at IS NULL`, [row.id],
        );
        if (spent.rowCount !== 1) return { status: 410, body: { error: 'request_expired' } };
        const tokens = await issueTokenPair(client, deps.config, user.id);
        return { status: 200, body: { access: tokens.access, refresh: tokens.refresh, user: profileOf(user) } };
      });
      res.status(result.status).json(result.body);
    }],
  });

  /** POST browser-cancel/: end a pending or approved request (needs the secret). */
  route(router, '/browser-cancel/', {
    post: [...guard, async (req, res) => {
      const done = await withTransaction(pool, async (client) => {
        const row = await liveRequest(client, req.body ?? {}, true);
        if (!row) return false;
        await client.query('UPDATE accounts_browsersession SET cancelled_at = now() WHERE id = $1', [row.id]);
        return true;
      });
      if (done) res.json({ status: 'cancelled' });
      else expired(res);
    }],
  });

  return router;
}
