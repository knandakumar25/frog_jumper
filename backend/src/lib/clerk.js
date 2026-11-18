/**
 * Clerk: verify a browser's Clerk session token, and look a user up in Clerk's Backend API.
 *
 * Ported from Django's `accounts/browser_auth.py` (`clerk_subject`) and `accounts/clerk_client.py`.
 * Nothing here reads a secret or touches the network until a function is called.
 */
import { createRemoteJWKSet, jwtVerify } from 'jose';

import { authFailed } from './errors.js';
import { USER_AGENT, timedFetch } from './mail.js';

// Clerk user ids look like "user_2abc...". Checked before the id goes into a URL path, so
// "user_a/../../x" cannot reshape the request, and a made-up id never reaches the network.
const USER_ID_RE = /^user_[A-Za-z0-9]{1,60}$/;

/** Base class for Clerk transport failures. */
export class ClerkError extends Error {}
/** Clerk answered 404 for a well-formed id (or the id was malformed). */
export class ClerkUserNotFound extends ClerkError {}
/** The request never got an answer: DNS, TLS, timeout, connection. */
export class ClerkUnavailable extends ClerkError {}
/** Clerk answered with an unexpected status or an unreadable body. */
export class ClerkAPIError extends ClerkError {}

/**
 * True when `userId` is a well-formed Clerk id.
 * @param {unknown} userId
 * @returns {boolean}
 */
export function isValidUserId(userId) {
  return typeof userId === 'string' && USER_ID_RE.test(userId);
}

const keySets = new Map();

/**
 * The trusted key set for a JWKS URL, cached per URL. The URL comes from settings only, never
 * from a token.
 * @param {string} url
 * @returns {ReturnType<typeof createRemoteJWKSet>}
 */
function trustedKeys(url) {
  if (!keySets.has(url)) keySets.set(url, createRemoteJWKSet(new URL(url), { timeoutDuration: 5000 }));
  return keySets.get(url);
}

/**
 * Verify a Clerk session token and return its user id (`sub`).
 *
 * Checks the RS256 signature against the trusted key set, the issuer, the lifetime claims
 * (`exp`, `nbf`, `iat` must be present), that `azp` is an allowed origin, that the session is not
 * `pending`, and that `sub` is a Clerk user id. The audience is not checked (Clerk sets none).
 * Any failure throws DRF's AuthenticationFailed, which these anonymous routes answer with 403.
 * @param {string} token
 * @param {import('../config.js').Config} config
 * @param {{ keys?: Parameters<typeof jwtVerify>[1] }} [options] tests pass a local key
 * @returns {Promise<string>}
 */
export async function clerkSubject(token, config, { keys } = {}) {
  if (!config.clerkIssuer || !config.clerkAuthorizedParties.length) {
    throw authFailed({ detail: 'Clerk verification is not configured' }, false);
  }
  try {
    const { payload } = await jwtVerify(String(token ?? ''), keys ?? trustedKeys(config.clerkJwksUrl), {
      algorithms: ['RS256'],
      issuer: config.clerkIssuer,
      requiredClaims: ['exp', 'iat', 'nbf', 'iss', 'sub', 'azp'],
    });
    if (!config.clerkAuthorizedParties.includes(payload.azp) || payload.sts === 'pending') {
      throw new Error('Unapproved session');
    }
    if (typeof payload.sub !== 'string' || !payload.sub.startsWith('user_')) throw new Error('Invalid subject');
    return payload.sub;
  } catch {
    throw authFailed({ detail: 'Invalid Clerk session' }, false);
  }
}

/**
 * The Clerk email status: "verified", "unverified" or "".
 * @param {object} address
 * @returns {string}
 */
function verificationStatus(address) {
  return address?.verification?.status || '';
}

/**
 * Reduce Clerk's user record to what onboarding needs. Email choice, most trusted first: the
 * primary address, then any verified address, then the first listed.
 * @param {object} payload
 * @returns {{id: string, email: string, email_verified: boolean, username: string, first_name: string, last_name: string}}
 */
export function parseClerkUser(payload) {
  const addresses = Array.isArray(payload.email_addresses) ? payload.email_addresses : [];
  let chosen = null;
  if (payload.primary_email_address_id) {
    chosen = addresses.find((a) => a.id === payload.primary_email_address_id) ?? null;
  }
  chosen ??= addresses.find((a) => verificationStatus(a) === 'verified') ?? addresses[0] ?? {};
  return {
    id: payload.id || '',
    email: chosen.email_address || '',
    email_verified: verificationStatus(chosen) === 'verified',
    username: payload.username || '',
    first_name: payload.first_name || '',
    last_name: payload.last_name || '',
  };
}

/**
 * Fetch a Clerk user by id.
 *
 * Throws `ClerkUserNotFound` (404 or malformed id; no network call for a malformed id),
 * `ClerkUnavailable` (no answer), or `ClerkAPIError` (no key, other status, unreadable body).
 * Error messages never include the response body or the key; the body is logged instead.
 * @param {unknown} userId
 * @param {import('../config.js').Config} config
 * @param {{ request?: typeof timedFetch, logger?: Pick<Console, 'warn'> }} [options]
 * @returns {Promise<ReturnType<typeof parseClerkUser>>}
 */
export async function fetchClerkUser(userId, config, { request = timedFetch, logger = console } = {}) {
  if (!isValidUserId(userId)) throw new ClerkUserNotFound('Malformed Clerk user id.');
  if (!config.clerkSecretKey) throw new ClerkAPIError('CLERK_SECRET_KEY is not configured.');
  const url = `${config.clerkApiBase.replace(/\/+$/, '')}/users/${encodeURIComponent(userId)}`;
  let response;
  try {
    response = await request(url, {
      method: 'GET',
      headers: {
        Authorization: `Bearer ${config.clerkSecretKey}`,
        Accept: 'application/json',
        'User-Agent': USER_AGENT,
      },
    }, config.clerkTimeoutMs);
  } catch (error) {
    throw new ClerkUnavailable(`Could not reach Clerk: ${error.message}`);
  }
  const body = await response.text().catch(() => '');
  if (!response.ok) {
    logger.warn(`Clerk refused GET /users/<id> (HTTP ${response.status}): ${body}`);
    if (response.status === 404) throw new ClerkUserNotFound('No such Clerk user.');
    throw new ClerkAPIError(`Clerk returned HTTP ${response.status}.`);
  }
  let payload;
  try {
    payload = JSON.parse(body);
  } catch {
    throw new ClerkAPIError('Clerk returned a non-JSON body.');
  }
  if (!payload || typeof payload !== 'object' || Array.isArray(payload)) {
    throw new ClerkAPIError('Clerk returned an unexpected body.');
  }
  return parseClerkUser(payload);
}

/**
 * Delete a user from Clerk through the Backend API (`DELETE /users/{id}`), used when a player
 * deletes their account. A 404 means the user is already gone and counts as success.
 * Throws `ClerkUnavailable` when Clerk cannot be reached and `ClerkAPIError` for any other
 * refusal or a missing secret key; callers keep the id queued and retry later.
 * @param {string} userId Clerk user id (`user_...`)
 * @param {import('../config.js').Config} config
 * @param {{ request?: typeof timedFetch, logger?: Pick<Console, 'warn'> }} [options]
 * @returns {Promise<'deleted' | 'already_gone'>}
 */
export async function deleteClerkUser(userId, config, { request = timedFetch, logger = console } = {}) {
  // A malformed id can never exist in Clerk, so there is nothing to delete.
  if (!isValidUserId(userId)) return 'already_gone';
  if (!config.clerkSecretKey) throw new ClerkAPIError('CLERK_SECRET_KEY is not configured.');
  const url = `${config.clerkApiBase.replace(/\/+$/, '')}/users/${encodeURIComponent(userId)}`;
  let response;
  try {
    response = await request(url, {
      method: 'DELETE',
      headers: {
        Authorization: `Bearer ${config.clerkSecretKey}`,
        Accept: 'application/json',
        'User-Agent': USER_AGENT,
      },
    }, config.clerkTimeoutMs);
  } catch (error) {
    throw new ClerkUnavailable(`Could not reach Clerk: ${error.message}`);
  }
  if (response.status === 404) return 'already_gone';
  if (!response.ok) {
    const body = await response.text().catch(() => '');
    logger.warn(`Clerk refused DELETE /users/<id> (HTTP ${response.status}): ${body}`);
    throw new ClerkAPIError(`Clerk returned HTTP ${response.status}.`);
  }
  return 'deleted';
}
