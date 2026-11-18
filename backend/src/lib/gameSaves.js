/**
 * Per-account game saves (2026-11-18). The game calculates rewards and world state; this module
 * stores them per account with the guarantees the game used to get from direct SQL transactions:
 *
 * - **Ownership:** every query is scoped to the signed-in user; another user's slot looks absent.
 * - **All-or-nothing commits:** `commitSlot` applies slot changes, documents and a receipt in one
 *   transaction, holding the slot row lock (`FOR UPDATE`) so commits to one slot run one at a time.
 * - **Retry safety:** a commit may carry a `token`. The first commit stores a receipt under it; a
 *   retry with the same token and the same receipt value returns `alreadyCommitted` without
 *   applying anything again, and the same token with a different value is refused.
 * - **Conflict detection:** each slot has a `revision`. A commit names the revision it read
 *   (`expectRevision`); if another commit landed first, it is refused with the current snapshot so
 *   the game can reload, recompute and retry.
 * - **No downgrades:** a document stored with a newer `schemaVersion` is never overwritten by an
 *   older one.
 */
import { withTransaction } from '../db.js';
import { HttpError } from './errors.js';

/** Per-slot document keys: the Los Angeles world, other cities' worlds, and slot progression. */
export const SLOT_DOC_KEY = /^(world|world:[a-z0-9_-]{1,40}|progression)$/;
/** Account-wide document keys (settings and operative progression). */
export const ACCOUNT_DOC_KEYS = Object.freeze(['user_settings', 'user_keybindings', 'input_prompt_settings', 'operative_progression']);
/** Largest document, in bytes of JSON. */
export const MAX_DOC_BYTES = 256 * 1024;
/** Largest commit (all documents together). */
export const MAX_COMMIT_BYTES = 1024 * 1024;
/** Most documents one commit may write. */
export const MAX_COMMIT_DOCS = 20;
const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/** A 400 with a field-style message. */
const bad = (detail) => new HttpError(400, { detail });

/** True for a whole number between `min` and `max`. */
const isInt = (value, min, max) => Number.isInteger(value) && value >= min && value <= max;

/** Byte length of a value written as JSON. */
const jsonBytes = (value) => Buffer.byteLength(JSON.stringify(value) ?? '', 'utf8');

/**
 * The public shape of a slot row.
 * @param {object} row `game_slot` row
 */
export function slotOf(row) {
  return {
    id: Number(row.id), character: row.character, current_level: row.current_level,
    total_score: Number(row.total_score), collectables: row.collectables,
    created_at: row.created_at, last_played: row.last_played, revision: Number(row.revision),
  };
}

/**
 * A slot with all its documents, as `{slot, docs: {key: {schemaVersion, value}}}`.
 * @param {import('pg').Pool | import('pg').PoolClient} db
 * @param {object} row `game_slot` row
 */
async function snapshot(db, row) {
  const docs = await db.query('SELECT doc_key, schema_version, value FROM game_slot_doc WHERE slot_id = $1 ORDER BY doc_key', [row.id]);
  return {
    slot: slotOf(row),
    docs: Object.fromEntries(docs.rows.map((d) => [d.doc_key, { schemaVersion: d.schema_version, value: d.value }])),
  };
}

/**
 * The player's slots, most recently played first.
 * @param {import('pg').Pool} pool
 * @param {number} userId
 */
export async function listSlots(pool, userId) {
  const rows = await pool.query('SELECT * FROM game_slot WHERE user_id = $1 ORDER BY last_played DESC, id DESC', [userId]);
  return rows.rows.map(slotOf);
}

/**
 * Create a slot for the player.
 * @param {import('pg').Pool} pool
 * @param {number} userId
 * @param {unknown} character starting operative name
 */
export async function createSlot(pool, userId, character) {
  if (typeof character !== 'string' || !character.trim() || character.length > 100) throw bad('character is required (at most 100 characters).');
  const row = await pool.query('INSERT INTO game_slot (user_id, character) VALUES ($1, $2) RETURNING *', [userId, character.trim()]);
  return slotOf(row.rows[0]);
}

/**
 * One slot with its documents, or a 404 when it is missing or belongs to someone else.
 * @param {import('pg').Pool} pool
 * @param {number} userId
 * @param {string} slotId
 */
export async function getSlot(pool, userId, slotId) {
  const row = await pool.query('SELECT * FROM game_slot WHERE id = $1 AND user_id = $2', [slotIdOf(slotId), userId]);
  if (!row.rows[0]) throw new HttpError(404, { detail: 'Save not found.' });
  return snapshot(pool, row.rows[0]);
}

/**
 * Delete one of the player's slots (documents and receipts go with it), or all of them when
 * `slotId` is null. Returns how many slots were deleted.
 */
export async function deleteSlots(pool, userId, slotId = null) {
  const result = slotId === null
    ? await pool.query('DELETE FROM game_slot WHERE user_id = $1', [userId])
    : await pool.query('DELETE FROM game_slot WHERE id = $1 AND user_id = $2', [slotIdOf(slotId), userId]);
  if (slotId !== null && result.rowCount === 0) throw new HttpError(404, { detail: 'Save not found.' });
  return result.rowCount;
}

/** Parse a slot id from the URL; anything else is a 404 (never reveals other users' ids). */
function slotIdOf(value) {
  const id = Number(value);
  if (!Number.isSafeInteger(id) || id <= 0) throw new HttpError(404, { detail: 'Save not found.' });
  return id;
}

/**
 * Check a commit body and return it normalised. Throws 400 or 413 on anything malformed.
 * @param {any} body
 */
export function validateCommit(body) {
  if (!body || typeof body !== 'object') throw bad('A JSON body is required.');
  const { expectRevision, token = null, receipt = null, slot = {}, docs = [] } = body;
  if (!Number.isSafeInteger(expectRevision) || expectRevision < 0) throw bad('expectRevision must be the revision you read.');
  if (token !== null && (typeof token !== 'string' || !UUID.test(token))) throw bad('token must be a UUID.');
  if (token !== null) {
    if (!receipt || typeof receipt.kind !== 'string' || !/^[a-z_]{1,40}$/.test(receipt.kind) || receipt.value === undefined) {
      throw bad('A token needs a receipt {kind, value}.');
    }
    if (jsonBytes(receipt.value) > 16 * 1024) throw new HttpError(413, { detail: 'Receipt is too large.' });
  }
  if (!slot || typeof slot !== 'object') throw bad('slot must be an object.');
  if (slot.character !== undefined && (typeof slot.character !== 'string' || !slot.character.trim() || slot.character.length > 100)) throw bad('slot.character is invalid.');
  if (slot.levelAtLeast !== undefined && !isInt(slot.levelAtLeast, 1, 99)) throw bad('slot.levelAtLeast must be 1-99.');
  if (slot.scoreAdd !== undefined && !isInt(slot.scoreAdd, 0, 1_000_000_000)) throw bad('slot.scoreAdd must be 0 or more.');
  if (slot.collectablesAdd !== undefined && !isInt(slot.collectablesAdd, 0, 1_000_000)) throw bad('slot.collectablesAdd must be 0 or more.');
  if (!Array.isArray(docs) || docs.length > MAX_COMMIT_DOCS) throw bad(`docs must be a list of at most ${MAX_COMMIT_DOCS}.`);
  let total = 0;
  const seen = new Set();
  for (const doc of docs) {
    if (!doc || typeof doc.key !== 'string' || !SLOT_DOC_KEY.test(doc.key)) throw bad('A document key is not allowed.');
    if (seen.has(doc.key)) throw bad('A document key appears twice.');
    seen.add(doc.key);
    if (!isInt(doc.schemaVersion ?? 1, 1, 1000)) throw bad('schemaVersion must be 1 or more.');
    if (!doc.value || typeof doc.value !== 'object') throw bad('A document value must be a JSON object.');
    const size = jsonBytes(doc.value);
    if (size > MAX_DOC_BYTES) throw new HttpError(413, { detail: `Document ${doc.key} is too large.` });
    total += size;
  }
  if (total > MAX_COMMIT_BYTES) throw new HttpError(413, { detail: 'Commit is too large.' });
  return { expectRevision, token, receipt, slot, docs: docs.map((d) => ({ key: d.key, schemaVersion: d.schemaVersion ?? 1, value: d.value })) };
}

/**
 * Apply one all-or-nothing commit to a slot (see the file header for the rules).
 * @param {import('pg').Pool} pool
 * @param {number} userId
 * @param {string} slotId
 * @param {ReturnType<typeof validateCommit>} commit
 * @returns {Promise<{status: 'committed' | 'alreadyCommitted', slot: object, docs: object}>}
 *   Throws 404 for a missing slot and 409 (`receipt_mismatch`, `revision_conflict`, `schema_newer`)
 *   with the current snapshot in the body.
 */
export async function commitSlot(pool, userId, slotId, commit) {
  return withTransaction(pool, async (client) => {
    const locked = await client.query('SELECT * FROM game_slot WHERE id = $1 AND user_id = $2 FOR UPDATE', [slotIdOf(slotId), userId]);
    const row = locked.rows[0];
    if (!row) throw new HttpError(404, { detail: 'Save not found.' });

    // Receipts are checked before the revision: a retry after a lost answer must succeed even
    // though its own first attempt already moved the revision on.
    if (commit.token) {
      const existing = await client.query(
        `SELECT (kind = $3 AND value = $4::jsonb) AS same FROM game_slot_receipt WHERE slot_id = $1 AND token = $2`,
        [row.id, commit.token, commit.receipt.kind, JSON.stringify(commit.receipt.value)]);
      if (existing.rows[0]) {
        if (!existing.rows[0].same) throw new HttpError(409, { error: 'receipt_mismatch', detail: 'This token was already used for a different result.' });
        return { status: 'alreadyCommitted', ...await snapshot(client, row) };
      }
    }
    if (Number(row.revision) !== commit.expectRevision) {
      throw new HttpError(409, { error: 'revision_conflict', detail: 'The save changed since you read it.', ...await snapshot(client, row) });
    }
    for (const doc of commit.docs) {
      const current = await client.query('SELECT schema_version FROM game_slot_doc WHERE slot_id = $1 AND doc_key = $2', [row.id, doc.key]);
      if (current.rows[0] && current.rows[0].schema_version > doc.schemaVersion) {
        throw new HttpError(409, { error: 'schema_newer', detail: `Document ${doc.key} was saved by a newer game version.` });
      }
      await client.query(
        `INSERT INTO game_slot_doc (slot_id, doc_key, schema_version, value) VALUES ($1, $2, $3, $4::jsonb)
         ON CONFLICT (slot_id, doc_key) DO UPDATE SET schema_version = EXCLUDED.schema_version, value = EXCLUDED.value`,
        [row.id, doc.key, doc.schemaVersion, JSON.stringify(doc.value)]);
    }
    const s = commit.slot;
    const updated = await client.query(
      `UPDATE game_slot SET
         character = COALESCE($2, character),
         current_level = GREATEST(current_level, COALESCE($3, current_level)),
         total_score = total_score + COALESCE($4, 0),
         collectables = collectables + COALESCE($5, 0),
         last_played = now(),
         revision = revision + 1
       WHERE id = $1 RETURNING *`,
      [row.id, s.character?.trim() ?? null, s.levelAtLeast ?? null, s.scoreAdd ?? null, s.collectablesAdd ?? null]);
    if (commit.token) {
      await client.query('INSERT INTO game_slot_receipt (slot_id, token, kind, value) VALUES ($1, $2, $3, $4::jsonb)',
        [row.id, commit.token, commit.receipt.kind, JSON.stringify(commit.receipt.value)]);
    }
    return { status: 'committed', ...await snapshot(client, updated.rows[0]) };
  });
}

/**
 * All of the player's account documents as `{key: value}`.
 * @param {import('pg').Pool} pool
 * @param {number} userId
 */
export async function getAccountDocs(pool, userId) {
  const rows = await pool.query('SELECT doc_key, value FROM game_account_doc WHERE user_id = $1', [userId]);
  return Object.fromEntries(rows.rows.map((r) => [r.doc_key, r.value]));
}

/**
 * Replace one account document. Only the whitelisted keys are accepted.
 * @param {import('pg').Pool} pool
 * @param {number} userId
 * @param {string} key
 * @param {unknown} value a JSON object
 */
export async function putAccountDoc(pool, userId, key, value) {
  if (!ACCOUNT_DOC_KEYS.includes(key)) throw new HttpError(404, { detail: 'Unknown setting.' });
  if (!value || typeof value !== 'object' || Array.isArray(value)) throw bad('The value must be a JSON object.');
  if (jsonBytes(value) > MAX_DOC_BYTES) throw new HttpError(413, { detail: 'Value is too large.' });
  await pool.query(
    `INSERT INTO game_account_doc (user_id, doc_key, value, updated_at) VALUES ($1, $2, $3::jsonb, now())
     ON CONFLICT (user_id, doc_key) DO UPDATE SET value = EXCLUDED.value, updated_at = now()`,
    [userId, key, JSON.stringify(value)]);
}

/**
 * The public leaderboard: each active account's best slot score, highest first. Only usernames
 * and scores are returned, never ids or emails.
 * @param {import('pg').Pool} pool
 * @param {number} [limit]
 */
export async function leaderboard(pool, limit = 50) {
  const rows = await pool.query(
    `SELECT u.username, max(s.total_score) AS best, max(s.current_level) AS level
       FROM game_slot s JOIN accounts_user u ON u.id = s.user_id
      WHERE u.is_active AND s.total_score > 0
      GROUP BY u.id, u.username
      ORDER BY best DESC, u.username
      LIMIT $1`, [limit]);
  return rows.rows.map((r, i) => ({ rank: i + 1, username: r.username, score: Number(r.best), level: r.level }));
}
