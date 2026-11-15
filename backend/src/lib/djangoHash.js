/**
 * Django-compatible password hashes, so accounts created by the Django backend keep signing in.
 *
 * Format (Django's `PBKDF2PasswordHasher`): `pbkdf2_sha256$<iterations>$<salt>$<base64 key>`,
 * where the key is PBKDF2-HMAC-SHA256 of the password with a 32-byte output. Verification reads
 * the iteration count from the stored string, so hashes made at any count still verify. New
 * hashes use the same format, so the table stays readable by Django tooling too.
 *
 * An "unusable" password (accounts created through Clerk onboarding, which own no password)
 * starts with `!`, as Django's `set_unusable_password()` writes; it never verifies.
 */
import { pbkdf2, randomBytes, randomInt, timingSafeEqual } from 'node:crypto';
import { promisify } from 'node:util';

const rawPbkdf2 = promisify(pbkdf2);
const ALGORITHM = 'pbkdf2_sha256';

// At 1.2M rounds one hash takes about a second on one of libuv's 4 worker threads, which DNS
// lookups (Clerk, Resend) share. Capping concurrent hashes keeps a burst of logins from
// starving everything else; extra requests wait their turn (2026-11-15 EGM review, issue 1).
let hashSlots = 4;
let running = 0;
const waiting = [];

/**
 * Set how many hashes may run at once (from `config.maxConcurrentHashes`).
 * @param {number} n at least 1
 */
export function setHashConcurrency(n) {
  hashSlots = Math.max(1, n);
}

/**
 * `crypto.pbkdf2` behind the concurrency cap.
 * @param {...any} args the same arguments as `crypto.pbkdf2` without the callback
 * @returns {Promise<Buffer>}
 */
async function pbkdf2Async(...args) {
  if (running >= hashSlots) await new Promise((resolve) => waiting.push(resolve));
  running += 1;
  try {
    return await rawPbkdf2(...args);
  } finally {
    running -= 1;
    waiting.shift()?.();
  }
}

/**
 * True when `encoded` is a well-formed, usable pbkdf2_sha256 hash this module can check.
 * Login uses it to run a decoy hash for every other case, so all answers take the same time.
 * @param {string | null | undefined} encoded
 * @returns {boolean}
 */
export function isCheckableHash(encoded) {
  if (!hasUsablePassword(encoded)) return false;
  const parts = encoded.split('$');
  const iterations = Number.parseInt(parts[1], 10);
  return parts.length === 4 && parts[0] === ALGORITHM && Number.isSafeInteger(iterations) && iterations >= 1
    && Buffer.from(parts[3], 'base64').length === 32;
}
const SALT_CHARS = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
const UNUSABLE_PREFIX = '!';

/**
 * A random salt from Django's alphabet (22 characters, about 131 bits, as Django 6 makes).
 * @returns {string}
 */
function makeSalt() {
  let salt = '';
  for (let i = 0; i < 22; i += 1) salt += SALT_CHARS[randomInt(SALT_CHARS.length)];
  return salt;
}

/**
 * Hash `password` in Django's pbkdf2_sha256 format.
 * @param {string} password
 * @param {number} iterations
 * @param {string} [salt] fixed only in tests
 * @returns {Promise<string>}
 */
export async function makePassword(password, iterations, salt = makeSalt()) {
  if (salt.includes('$')) throw new Error('salt must not contain "$"');
  const key = await pbkdf2Async(password, salt, iterations, 32, 'sha256');
  return `${ALGORITHM}$${iterations}$${salt}$${key.toString('base64')}`;
}

/**
 * Django's unusable password marker: `!` and 40 random characters.
 * @returns {string}
 */
export function makeUnusablePassword() {
  return UNUSABLE_PREFIX + randomBytes(30).toString('base64url').slice(0, 40);
}

/**
 * True when `encoded` is a usable password hash.
 * @param {string | null | undefined} encoded
 * @returns {boolean}
 */
export function hasUsablePassword(encoded) {
  return Boolean(encoded) && !encoded.startsWith(UNUSABLE_PREFIX);
}

/**
 * Check `password` against a stored Django hash. Unknown algorithms and unusable or malformed
 * hashes return false. The comparison takes the same time whether or not the keys match.
 * @param {string} password
 * @param {string | null | undefined} encoded
 * @returns {Promise<boolean>}
 */
export async function checkPassword(password, encoded) {
  if (typeof password !== 'string' || !hasUsablePassword(encoded)) return false;
  const parts = encoded.split('$');
  if (parts.length !== 4 || parts[0] !== ALGORITHM) return false;
  const iterations = Number.parseInt(parts[1], 10);
  if (!Number.isSafeInteger(iterations) || iterations < 1) return false;
  const expected = Buffer.from(parts[3], 'base64');
  if (expected.length !== 32) return false;
  const actual = await pbkdf2Async(password, parts[2], iterations, 32, 'sha256');
  return timingSafeEqual(actual, expected);
}
