/**
 * The `accounts_user` table: lookups, the public profile shape, creation and field validation.
 *
 * Validation messages copy Django REST Framework's word for word, because the website shows them.
 * Every insert writes every NOT NULL column: tables made by Django have no column defaults.
 */
import { FieldErrors, HttpError, MESSAGES } from './errors.js';
import { makePassword, makeUnusablePassword } from './djangoHash.js';

const PROFILE_FIELDS = ['id', 'username', 'email', 'first_name', 'last_name', 'profile_picture',
  'is_2fa_enabled', 'phone_number', 'country'];

// Django's UnicodeUsernameValidator: letters, digits and @ . + - _ (Python's \w is Unicode-aware).
const USERNAME_RE = /^[\p{L}\p{N}\p{M}_.@+-]+$/u;
// Close to Django's EmailValidator for real addresses: one @, no spaces, a dotted domain.
const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/u;

/**
 * The profile JSON every sign-in route returns (Django's `UserProfileSerializer`).
 * @param {object} user an `accounts_user` row
 * @returns {object}
 */
export function profileOf(user) {
  return Object.fromEntries(PROFILE_FIELDS.map((field) => [field, user[field] ?? null]));
}

/**
 * @param {import('pg').Pool | import('pg').PoolClient} db
 * @param {number} id
 * @returns {Promise<object | null>}
 */
export async function findUserById(db, id) {
  return (await db.query('SELECT * FROM accounts_user WHERE id = $1', [id])).rows[0] ?? null;
}

/**
 * Exact, case-sensitive username lookup (as `User.objects.get(username=...)`).
 * @param {import('pg').Pool | import('pg').PoolClient} db
 * @param {string} username
 * @returns {Promise<object | null>}
 */
export async function findUserByUsername(db, username) {
  return (await db.query('SELECT * FROM accounts_user WHERE username = $1', [username])).rows[0] ?? null;
}

/**
 * Django's `normalize_email`: lower-case the domain part only.
 * @param {string} email
 * @returns {string}
 */
export function normalizeEmail(email) {
  const at = email.lastIndexOf('@');
  return at < 0 ? email : email.slice(0, at) + '@' + email.slice(at + 1).toLowerCase();
}

/**
 * Django's `normalize_username`: Unicode NFKC, so look-alike forms collapse to one name.
 * @param {string} username
 * @returns {string}
 */
export function normalizeUsername(username) {
  return username.normalize('NFKC');
}

/**
 * Insert a user. `password` null stores an unusable password (Clerk-owned accounts).
 *
 * Does not send the welcome email; callers do that after their transaction commits, with
 * `sendWelcomeEmail` from `emails.js` (Django sent it from a post_save signal).
 * @param {import('pg').Pool | import('pg').PoolClient} db
 * @param {import('../config.js').Config} config
 * @param {object} fields username, email, and optional profile columns
 * @param {string | null} password
 * @returns {Promise<object>} the new row
 */
export async function createUser(db, config, fields, password) {
  const hash = password === null ? makeUnusablePassword() : await makePassword(password, config.passwordIterations);
  const result = await db.query(
    `INSERT INTO accounts_user (password, last_login, is_superuser, username, first_name, last_name,
        is_staff, is_active, date_joined, email, profile_picture, is_2fa_enabled, country,
        phone_number, clerk_user_id, date_of_birth)
     VALUES ($1, NULL, false, $2, $3, $4, false, true, now(), $5, $6, $7, $8, $9, $10, $11)
     RETURNING *`,
    [hash, normalizeUsername(fields.username), fields.first_name ?? '', fields.last_name ?? '',
      normalizeEmail(fields.email ?? ''), fields.profile_picture ?? '', fields.is_2fa_enabled ?? false,
      fields.country ?? null, fields.phone_number ?? null, fields.clerk_user_id ?? null,
      fields.date_of_birth ?? null],
  );
  return result.rows[0];
}

/**
 * Read one string field the way DRF's CharField does: numbers become text, other non-strings are
 * refused, and surrounding whitespace is trimmed.
 * @param {FieldErrors} errors
 * @param {string} name
 * @param {unknown} value
 * @param {{ required?: boolean, allowBlank?: boolean, allowNull?: boolean, maxLength?: number, trim?: boolean }} rules
 * @returns {string | null | undefined} undefined when absent and not required, or when invalid
 */
export function readString(errors, name, value, rules) {
  const { required = false, allowBlank = false, allowNull = false, maxLength, trim = true } = rules;
  if (value === undefined) {
    if (required) errors.add(name, MESSAGES.required);
    return undefined;
  }
  if (value === null) {
    if (!allowNull) errors.add(name, MESSAGES.null);
    return allowNull ? null : undefined;
  }
  if (typeof value !== 'string' && typeof value !== 'number') {
    errors.add(name, MESSAGES.notString);
    return undefined;
  }
  let text = String(value);
  if (trim) text = text.trim();
  if (text === '' && !allowBlank) {
    errors.add(name, MESSAGES.blank);
    return undefined;
  }
  if (maxLength !== undefined && text.length > maxLength) {
    errors.add(name, MESSAGES.maxLength(maxLength));
    return undefined;
  }
  return text;
}

/**
 * Check a username's characters and that no other account holds it.
 * @param {import('pg').Pool} pool
 * @param {FieldErrors} errors
 * @param {string | undefined} username
 * @param {number | null} exceptId the user being edited, so they may keep their own name
 */
async function checkUsername(pool, errors, username, exceptId) {
  if (username === undefined) return;
  if (!USERNAME_RE.test(username)) {
    errors.add('username', MESSAGES.usernameInvalid);
    return;
  }
  const taken = await pool.query('SELECT 1 FROM accounts_user WHERE username = $1 AND id IS DISTINCT FROM $2',
    [normalizeUsername(username), exceptId]);
  if (taken.rowCount) errors.add('username', MESSAGES.usernameTaken);
}

/**
 * Check an email's shape and that no other account holds it.
 * @param {import('pg').Pool} pool
 * @param {FieldErrors} errors
 * @param {string | undefined} email
 * @param {number | null} exceptId
 */
async function checkEmail(pool, errors, email, exceptId) {
  if (email === undefined) return;
  if (!EMAIL_RE.test(email)) {
    errors.add('email', MESSAGES.email);
    return;
  }
  const taken = await pool.query('SELECT 1 FROM accounts_user WHERE email = $1 AND id IS DISTINCT FROM $2',
    [normalizeEmail(email), exceptId]);
  if (taken.rowCount) errors.add('email', MESSAGES.emailTaken);
}

/**
 * Validate a registration body (Django's `UserSerializer`); throws 400 with field errors.
 *
 * Django declared `email` explicitly, which silently dropped its uniqueness check and turned a
 * duplicate into a 400 with a raw database message; this port reports it as a field error.
 * The password is not trimmed, so it matches exactly what login later checks.
 * @param {import('pg').Pool} pool
 * @param {Record<string, unknown>} body
 * @returns {Promise<{username: string, email: string, password: string, first_name: string,
 *   last_name: string, profile_picture: string, country: string}>}
 */
export async function validateRegistration(pool, body) {
  const errors = new FieldErrors();
  const username = readString(errors, 'username', body.username, { required: true, maxLength: 150 });
  const email = readString(errors, 'email', body.email, { required: true, maxLength: 254 });
  const password = readString(errors, 'password', body.password, { required: true, maxLength: 128, trim: false });
  const first_name = readString(errors, 'first_name', body.first_name, { allowBlank: true, maxLength: 150 }) ?? '';
  const last_name = readString(errors, 'last_name', body.last_name, { allowBlank: true, maxLength: 150 }) ?? '';
  const profile_picture = readString(errors, 'profile_picture', body.profile_picture, { allowBlank: true }) ?? '';
  const country = readString(errors, 'country', body.country, { required: true, maxLength: 100 });
  await checkUsername(pool, errors, username, null);
  await checkEmail(pool, errors, email, null);
  errors.throwIfAny();
  return { username, email, password, first_name, last_name, profile_picture, country };
}

/**
 * Validate a profile edit (Django's `UserProfileSerializer`, PUT or PATCH); throws 400.
 *
 * `is_2fa_enabled` may be sent back unchanged; changing it here is a 400, because turning 2FA on
 * or off needs an emailed code (Django ignored the field). A changed `email` is returned in the
 * changes; the route then requires `email_code` (2026-11-15 EGM review, issue 5).
 * @param {import('pg').Pool} pool
 * @param {object} user the current row
 * @param {Record<string, unknown>} body
 * @param {boolean} partial PATCH (true) or PUT (false, username and email required)
 * @returns {Promise<Record<string, unknown>>} only the columns to change
 */
export async function validateProfileUpdate(pool, user, body, partial) {
  const errors = new FieldErrors();
  const changes = {};
  const put = (name, value) => { if (value !== undefined) changes[name] = value; };
  put('username', readString(errors, 'username', body.username, { required: !partial, maxLength: 150 }));
  put('email', readString(errors, 'email', body.email, { required: !partial, maxLength: 254 }));
  put('first_name', readString(errors, 'first_name', body.first_name, { allowBlank: true, maxLength: 150 }));
  put('last_name', readString(errors, 'last_name', body.last_name, { allowBlank: true, maxLength: 150 }));
  put('profile_picture', readString(errors, 'profile_picture', body.profile_picture, { allowBlank: true }));
  put('phone_number', readString(errors, 'phone_number', body.phone_number, { allowBlank: true, allowNull: true, maxLength: 20 }));
  put('country', readString(errors, 'country', body.country, { allowBlank: true, allowNull: true, maxLength: 100 }));
  if (body.is_2fa_enabled !== undefined) {
    // Sending the current value back is fine (a client may resend the whole profile). Changing it
    // here is refused: turning 2FA on or off needs an emailed code (2fa/enable + 2fa/verify-enable,
    // 2fa/disable-request + 2fa/disable), so a stolen access token alone cannot remove 2FA
    // (2026-11-15 EGM review, issue 5).
    const wanted = parseBoolean(body.is_2fa_enabled);
    if (wanted === undefined) errors.add('is_2fa_enabled', MESSAGES.booleanInvalid);
    else if (wanted && !user.is_2fa_enabled) {
      errors.add('is_2fa_enabled', 'Use the emailed code (/api/auth/2fa/enable/) to turn on two-factor sign-in.');
    } else if (!wanted && user.is_2fa_enabled) {
      errors.add('is_2fa_enabled', 'Use the emailed code (/api/auth/2fa/disable-request/) to turn off two-factor sign-in.');
    }
  }
  await checkUsername(pool, errors, changes.username, user.id);
  await checkEmail(pool, errors, changes.email, user.id);
  errors.throwIfAny();
  if (changes.username !== undefined) changes.username = normalizeUsername(changes.username);
  if (changes.email !== undefined) changes.email = normalizeEmail(changes.email);
  // The account page always sends the email; an unchanged one is not a change (no code needed).
  if (changes.email === user.email) delete changes.email;
  return changes;
}

/**
 * DRF BooleanField parsing: true/false, "true"/"false", 1/0 and similar.
 * @param {unknown} value
 * @returns {boolean | undefined}
 */
function parseBoolean(value) {
  if (value === true || value === 1 || ['true', 'True', '1', 'on', 'yes'].includes(value)) return true;
  if (value === false || value === 0 || ['false', 'False', '0', 'off', 'no'].includes(value)) return false;
  return undefined;
}

/**
 * Apply validated profile changes and return the updated row.
 * @param {import('pg').Pool} pool
 * @param {number} userId
 * @param {Record<string, unknown>} changes column → value, from `validateProfileUpdate`
 * @returns {Promise<object>}
 */
export async function updateUser(pool, userId, changes) {
  const columns = Object.keys(changes);
  if (columns.length === 0) return findUserById(pool, userId);
  // Column names come only from validateProfileUpdate's fixed list, never from the request.
  const sets = columns.map((column, i) => `${column} = $${i + 2}`).join(', ');
  try {
    const result = await pool.query(`UPDATE accounts_user SET ${sets} WHERE id = $1 RETURNING *`,
      [userId, ...columns.map((c) => changes[c])]);
    return result.rows[0];
  } catch (error) {
    // A parallel edit took the name or address between the check and this update.
    if (error.code === '23505') {
      const field = String(error.constraint ?? '').includes('email') ? 'email' : 'username';
      throw new HttpError(400, { [field]: [field === 'email' ? MESSAGES.emailTaken : MESSAGES.usernameTaken] });
    }
    throw error;
  }
}
