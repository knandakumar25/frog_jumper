/**
 * Access and refresh tokens (HS256 JWTs), shaped like the Django simplejwt tokens they replace.
 *
 * Claims: `token_type` ("access" or "refresh"), `exp`, `iat`, `jti` and `user_id` (a string, as
 * simplejwt 5.5 writes it). Access tokens last 1 hour and refresh tokens 7 days, as before.
 *
 * Change from Django (owner decision, 2026-11-15): refresh tokens are single use. Every refresh
 * token's `jti` and sign-in `family` are stored in `accounts_refreshtoken`; redeeming one spends
 * it and inserts its successor in one transaction, and replaying a spent token revokes the whole
 * family. Rows are deleted a day after they expire (`purgeExpired`).
 *
 * Tokens are signed with `JWT_SIGNING_KEY`. Tokens issued by the Django backend (signed with its
 * old source-code key) are rejected, so everyone signs in once after the switch.
 */
import { randomUUID } from 'node:crypto';
import { SignJWT, jwtVerify, errors as joseErrors } from 'jose';

import { withTransaction } from '../db.js';
import { HttpError } from './errors.js';

const ALG = 'HS256';

/**
 * The signing key as bytes.
 * @param {{jwtSigningKey: string}} config
 * @returns {Uint8Array}
 */
function keyOf(config) {
  return new TextEncoder().encode(config.jwtSigningKey);
}

/**
 * Sign one token.
 * @param {{jwtSigningKey: string}} config
 * @param {'access' | 'refresh'} type
 * @param {number} userId
 * @param {number} ttlSeconds
 * @param {string} jti
 * @returns {Promise<string>}
 */
function sign(config, type, userId, ttlSeconds, jti) {
  const now = Math.floor(Date.now() / 1000);
  return new SignJWT({ token_type: type, user_id: String(userId) })
    .setProtectedHeader({ alg: ALG, typ: 'JWT' })
    .setIssuedAt(now)
    .setExpirationTime(now + ttlSeconds)
    .setJti(jti)
    .sign(keyOf(config));
}

/**
 * Issue an access and refresh token pair for a user and record the refresh token.
 *
 * Every refresh token belongs to a family: one sign-in and all the tokens rotated from it.
 * A new sign-in starts a new family; `rotateRefreshToken` passes the old family on.
 * @param {import('pg').Pool | import('pg').PoolClient} db
 * @param {import('../config.js').Config} config
 * @param {number} userId
 * @param {string | null} [family] null starts a new family
 * @returns {Promise<{access: string, refresh: string}>}
 */
export async function issueTokenPair(db, config, userId, family = null) {
  const refreshJti = randomUUID();
  await db.query(
    `INSERT INTO accounts_refreshtoken (jti, user_id, expires_at, family)
     VALUES ($1, $2, now() + make_interval(secs => $3), $4)`,
    [refreshJti, userId, config.refreshTtlSeconds, family ?? randomUUID()],
  );
  const [access, refresh] = await Promise.all([
    sign(config, 'access', userId, config.accessTtlSeconds, randomUUID()),
    sign(config, 'refresh', userId, config.refreshTtlSeconds, refreshJti),
  ]);
  return { access, refresh };
}

/**
 * Revoke every unused refresh token of a user (after 2FA is switched off or the email changes).
 * Access tokens already issued stay valid until they expire (at most 1 hour).
 * @param {import('pg').Pool} db
 * @param {number} userId
 * @returns {Promise<number>} how many tokens were revoked
 */
export async function revokeAllRefreshTokens(db, userId) {
  return withTransaction(db, async (client) => {
    await client.query('SELECT id FROM accounts_user WHERE id = $1 FOR UPDATE', [userId]);
    const result = await client.query(
      'UPDATE accounts_refreshtoken SET used_at = now() WHERE user_id = $1 AND used_at IS NULL', [userId],
    );
    return result.rowCount;
  });
}

/**
 * Delete rows nobody can use any more: refresh tokens expired over a day ago, 2FA codes older
 * than a day, and browser sign-in requests that expired over a day ago and are not linked to a
 * pending registration (Django-made tables have no ON DELETE action on that link, so linked rows
 * are kept). The server runs this hourly.
 * @param {import('pg').Pool} pool
 * @returns {Promise<{refreshTokens: number, codes: number, browserSessions: number}>}
 */
export async function purgeExpired(pool) {
  const tokens = await pool.query("DELETE FROM accounts_refreshtoken WHERE expires_at < now() - interval '1 day'");
  const codes = await pool.query("DELETE FROM accounts_twofactorcode WHERE created_at < now() - interval '1 day'");
  const sessions = await pool.query(
    `DELETE FROM accounts_browsersession s
      WHERE s.expires_at < now() - interval '1 day'
        AND NOT EXISTS (SELECT 1 FROM accounts_pendingregistration p WHERE p.browser_session_id = s.id)`,
  );
  return { refreshTokens: tokens.rowCount, codes: codes.rowCount, browserSessions: sessions.rowCount };
}

/**
 * Verify a token's signature, lifetime and type. Throws `ExpiredToken` or `InvalidToken`.
 * @param {{jwtSigningKey: string}} config
 * @param {string} token
 * @param {'access' | 'refresh'} type
 * @returns {Promise<import('jose').JWTPayload & {user_id: string, token_type: string}>}
 */
async function verify(config, token, type) {
  try {
    const { payload } = await jwtVerify(token, keyOf(config), { algorithms: [ALG] });
    if (payload.token_type !== type || typeof payload.user_id !== 'string' || typeof payload.jti !== 'string') {
      throw new InvalidToken();
    }
    return /** @type {any} */ (payload);
  } catch (error) {
    if (error instanceof joseErrors.JWTExpired) throw new ExpiredToken();
    throw new InvalidToken();
  }
}

class InvalidToken extends Error {}
class ExpiredToken extends InvalidToken {}

/**
 * Read the user id from an access token, or throw simplejwt's 401 body.
 * @param {{jwtSigningKey: string}} config
 * @param {string} token
 * @param {boolean} routeHasAuth false on routes that answer 403 instead (see `authFailed`)
 * @returns {Promise<number>}
 */
export async function userIdFromAccessToken(config, token, routeHasAuth = true) {
  try {
    const payload = await verify(config, token, 'access');
    const id = Number.parseInt(payload.user_id, 10);
    if (!Number.isSafeInteger(id)) throw new InvalidToken();
    return id;
  } catch (error) {
    const message = error instanceof ExpiredToken ? 'Token is expired' : 'Token is invalid';
    throw new HttpError(routeHasAuth ? 401 : 403, {
      detail: 'Given token not valid for any token type',
      code: 'token_not_valid',
      messages: [{ token_class: 'AccessToken', token_type: 'access', message }],
    }, routeHasAuth ? { 'WWW-Authenticate': 'Bearer realm="api"' } : {});
  }
}

/**
 * Redeem a refresh token once and return a new pair (rotation).
 *
 * Inside one transaction the token's row is locked, spent, and its successor inserted, so a
 * failure in between changes nothing. Replaying a token that was already used means it was
 * copied: the whole family (the sign-in it came from) is revoked, so whoever holds the newer
 * tokens is signed out too, and the request gets 401. An inactive user's token is refused.
 * (2026-11-15 EGM review, issue 6.)
 * @param {import('pg').Pool} pool
 * @param {import('../config.js').Config} config
 * @param {string} token
 * @returns {Promise<{access: string, refresh: string}>}
 */
export async function rotateRefreshToken(pool, config, token) {
  const refused = (detail) => new HttpError(401, { detail, code: 'token_not_valid' },
    { 'WWW-Authenticate': 'Bearer realm="api"' });
  let payload;
  try {
    payload = await verify(config, token, 'refresh');
  } catch (error) {
    throw refused(error instanceof ExpiredToken ? 'Token is expired' : 'Token is invalid');
  }
  const outcome = await withTransaction(pool, async (client) => {
    // A token-row lock cannot serialize replay of an ancestor against rotation of its child.
    // Lock the account first, also shared by revoke-all, before reading the family state.
    await client.query('SELECT id FROM accounts_user WHERE id = $1 FOR UPDATE', [payload.user_id]);
    const row = (await client.query(
      `SELECT t.id, t.user_id, t.family, t.used_at, t.expires_at > now() AS live, u.is_active
         FROM accounts_refreshtoken t JOIN accounts_user u ON u.id = t.user_id
        WHERE t.jti = $1 FOR UPDATE OF t`, [payload.jti],
    )).rows[0];
    if (!row || !row.live || !row.is_active) return null;
    if (row.used_at) {
      await client.query('UPDATE accounts_refreshtoken SET used_at = now() WHERE family = $1 AND used_at IS NULL',
        [row.family]);
      return null;
    }
    await client.query('UPDATE accounts_refreshtoken SET used_at = now() WHERE id = $1', [row.id]);
    return issueTokenPair(client, config, row.user_id, row.family);
  });
  if (!outcome) throw refused('Token is invalid');
  return outcome;
}
