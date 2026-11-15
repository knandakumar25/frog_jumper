/**
 * Bearer-token authentication middleware, matching DRF's JWTAuthentication behaviour.
 *
 * Three kinds of route existed in Django, and the website relies on the difference:
 *
 * - `requireUser`: signed-in only (profile, 2FA enable). No token gives 401.
 * - `optionalUser`: anonymous allowed, but a bad or expired token still gives 401 (register,
 *   login, 2fa/verify-login). The website reacts to that 401 by refreshing its token.
 * - routes with no auth middleware at all ignore the header (browser sign-in, onboarding,
 *   countries), so a stale website token never blocks them.
 */
import { authFailed, HttpError } from './errors.js';
import { isValidUserId } from './clerk.js';
import { userIdFromAccessToken } from './tokens.js';
import { findUserById } from './users.js';

/**
 * Pull the raw token out of the Authorization header.
 *
 * Returns null when there is no header or it uses another scheme (DRF then treats the request as
 * anonymous). Throws for "Bearer" with no token or with spaces in it, as DRF did.
 * @param {import('express').Request} req
 * @param {boolean} routeHasAuth
 * @returns {string | null}
 */
export function bearerToken(req, routeHasAuth = true) {
  const header = req.get('authorization');
  if (!header) return null;
  const parts = header.trim().split(/\s+/);
  if (parts[0] !== 'Bearer') return null;
  if (parts.length === 1) {
    throw authFailed({ detail: 'Invalid Authorization header. No credentials provided.' }, routeHasAuth);
  }
  if (parts.length > 2) {
    throw authFailed({ detail: 'Invalid Authorization header. Credentials string should not contain spaces.' }, routeHasAuth);
  }
  return parts[1];
}

/**
 * Resolve the user for a request's Bearer token, or null when no token was sent.
 * @param {{pool: import('pg').Pool, config: import('../config.js').Config}} deps
 * @param {import('express').Request} req
 * @param {boolean} [routeHasAuth] false makes failures 403 (see `authFailed`)
 * @returns {Promise<object | null>} the `accounts_user` row
 */
export async function userFromRequest(deps, req, routeHasAuth = true) {
  const token = bearerToken(req, routeHasAuth);
  if (token === null) return null;
  const userId = await userIdFromAccessToken(deps.config, token, routeHasAuth);
  const user = await findUserById(deps.pool, userId);
  if (!user) throw authFailed({ detail: 'User not found', code: 'user_not_found' }, routeHasAuth);
  if (!user.is_active) throw authFailed({ detail: 'User is inactive', code: 'user_inactive' }, routeHasAuth);
  return user;
}

/**
 * Middleware: a valid token is required; the user row is put on `req.user`.
 * @param {{pool: import('pg').Pool, config: import('../config.js').Config}} deps
 * @returns {import('express').RequestHandler}
 */
export function requireUser(deps) {
  return async (req, res, next) => {
    // An explicit Clerk scheme restores website account access after a reload.
    // Bearer semantics for the desktop and password clients stay unchanged.
    const header = req.get('authorization') || '';
    if (/^Clerk(?:\s|$)/i.test(header)) {
      const parts = header.trim().split(/\s+/);
      if (parts.length !== 2) throw authFailed({ detail: 'Invalid Clerk session' });
      let subject;
      try { subject = await deps.clerk.subject(parts[1], deps.config); }
      catch { throw authFailed({ detail: 'Invalid Clerk session' }); }
      if (!isValidUserId(subject)) throw authFailed({ detail: 'Invalid Clerk session' });
      const result = await deps.pool.query('SELECT * FROM accounts_user WHERE clerk_user_id = $1', [subject]);
      req.user = result.rows[0];
      if (!req.user) throw new HttpError(403, { error: 'onboarding_required' });
      if (!req.user.is_active) throw authFailed({ detail: 'User is inactive', code: 'user_inactive' });
    } else req.user = await userFromRequest(deps, req);
    if (!req.user) throw authFailed({ detail: 'Authentication credentials were not provided.' });
    next();
  };
}

/**
 * Middleware: anonymous is fine, but a token that is sent must be valid.
 * @param {{pool: import('pg').Pool, config: import('../config.js').Config}} deps
 * @returns {import('express').RequestHandler}
 */
export function optionalUser(deps) {
  return async (req, res, next) => {
    req.user = await userFromRequest(deps, req);
    next();
  };
}
