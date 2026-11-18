/**
 * Per-account game saves (2026-11-18): ownership, all-or-nothing commits, retry-safe receipts,
 * revision conflicts, schema guards, limits, account documents, the public leaderboard and
 * account deletion removing every save row.
 */
import assert from 'node:assert/strict';
import { randomUUID } from 'node:crypto';
import { describe, it } from 'node:test';
import request from 'supertest';

import { deleteAccount } from '../src/lib/accountDeletion.js';
import { MAX_DOC_BYTES } from '../src/lib/gameSaves.js';
import { issueTokenPair } from '../src/lib/tokens.js';
import { baseConfig, makeApp, makeUser, pool, useDatabase } from './helpers.js';

useDatabase();

const bearer = (token) => ({ Authorization: `Bearer ${token}` });

/** A user with an access token and a fresh app. */
async function player(name, overrides) {
  const ctx = makeApp(overrides);
  const user = await makeUser({ username: name, email: `${name}@example.com`, country: 'Japan' });
  const pair = await issueTokenPair(pool, baseConfig, user.id);
  return { ...ctx, user, auth: bearer(pair.access) };
}

/** Create a slot and return its id. */
async function newSlot(app, auth, character = 'Cyber Shogun') {
  const res = await request(app).post('/api/game/slots/').set(auth).send({ character });
  assert.equal(res.status, 201);
  return res.body.id;
}

const world = (x) => ({ Version: 1, World: 'la', Position: { X: x, Y: 0 } });

describe('Game save slots', () => {
  it('creates, lists, reads and deletes only the signed-in player\'s slots', async () => {
    const a = await player('alpha');
    const b = await player('bravo');
    const id = await newSlot(a.app, a.auth);
    assert.deepEqual((await request(a.app).get('/api/game/slots/').set(a.auth)).body.map((s) => s.id), [id]);
    assert.deepEqual((await request(b.app).get('/api/game/slots/').set(b.auth)).body, []);
    assert.equal((await request(b.app).get(`/api/game/slots/${id}/`).set(b.auth)).status, 404, 'another player sees nothing');
    assert.equal((await request(b.app).delete(`/api/game/slots/${id}/`).set(b.auth)).status, 404);
    assert.equal((await request(b.app).post(`/api/game/slots/${id}/commit/`).set(b.auth).send({ expectRevision: 0 })).status, 404);
    const got = await request(a.app).get(`/api/game/slots/${id}/`).set(a.auth);
    assert.equal(got.body.slot.character, 'Cyber Shogun');
    assert.equal(got.body.slot.current_level, 1);
    assert.equal(got.body.slot.revision, 0);
    assert.deepEqual(got.body.docs, {});
    assert.equal((await request(a.app).delete(`/api/game/slots/${id}/`).set(a.auth)).status, 200);
    assert.deepEqual((await request(a.app).get('/api/game/slots/').set(a.auth)).body, []);
  });

  it('refuses signed-out requests', async () => {
    const { app } = makeApp();
    assert.equal((await request(app).get('/api/game/slots/')).status, 401);
    assert.equal((await request(app).post('/api/game/slots/').send({ character: 'x' })).status, 401);
    assert.equal((await request(app).get('/api/game/account-docs/')).status, 401);
  });

  it('deletes all of a player\'s slots and nobody else\'s', async () => {
    const a = await player('alpha');
    const b = await player('bravo');
    await newSlot(a.app, a.auth); await newSlot(a.app, a.auth);
    const kept = await newSlot(b.app, b.auth);
    assert.equal((await request(a.app).delete('/api/game/slots/').set(a.auth)).body.deleted, 2);
    assert.deepEqual((await request(b.app).get('/api/game/slots/').set(b.auth)).body.map((s) => s.id), [kept]);
  });
});

describe('Commits', () => {
  it('applies documents, slot changes and a receipt together and bumps the revision', async () => {
    const { app, auth } = await player('alpha');
    const id = await newSlot(app, auth);
    const token = randomUUID();
    const res = await request(app).post(`/api/game/slots/${id}/commit/`).set(auth).send({
      expectRevision: 0, token, receipt: { kind: 'mission_completion', value: { mission: 3, score: 40 } },
      slot: { levelAtLeast: 4, scoreAdd: 40, character: 'Oceanus' },
      docs: [{ key: 'world', schemaVersion: 1, value: world(10) }, { key: 'world:kyoto', value: world(5) }],
    });
    assert.equal(res.status, 200);
    assert.equal(res.body.status, 'committed');
    assert.equal(res.body.slot.revision, 1);
    assert.equal(res.body.slot.current_level, 4);
    assert.equal(res.body.slot.total_score, 40);
    assert.equal(res.body.slot.character, 'Oceanus');
    assert.deepEqual(Object.keys(res.body.docs).sort(), ['world', 'world:kyoto']);
  });

  it('a retry with the same token awards nothing again, even after the revision moved on', async () => {
    const { app, auth } = await player('alpha');
    const id = await newSlot(app, auth);
    const body = { expectRevision: 0, token: randomUUID(), receipt: { kind: 'quest', value: { quest: 'q1' } }, slot: { scoreAdd: 25 } };
    assert.equal((await request(app).post(`/api/game/slots/${id}/commit/`).set(auth).send(body)).body.status, 'committed');
    const retry = await request(app).post(`/api/game/slots/${id}/commit/`).set(auth).send(body);
    assert.equal(retry.status, 200);
    assert.equal(retry.body.status, 'alreadyCommitted');
    assert.equal(retry.body.slot.total_score, 25);
    const reused = await request(app).post(`/api/game/slots/${id}/commit/`).set(auth)
      .send({ ...body, expectRevision: 1, receipt: { kind: 'quest', value: { quest: 'other' } } });
    assert.equal(reused.status, 409);
    assert.equal(reused.body.error, 'receipt_mismatch');
  });

  it('eight concurrent retries of one result award exactly once', async () => {
    const { app, auth } = await player('alpha');
    const id = await newSlot(app, auth);
    const body = { expectRevision: 0, token: randomUUID(), receipt: { kind: 'mission_completion', value: { mission: 6, score: 37 } },
      slot: { levelAtLeast: 7, scoreAdd: 37 } };
    const results = await Promise.all(Array.from({ length: 8 }, () => request(app).post(`/api/game/slots/${id}/commit/`).set(auth).send(body)));
    assert.ok(results.every((r) => r.status === 200));
    assert.equal(results.filter((r) => r.body.status === 'committed').length, 1);
    const slot = (await request(app).get(`/api/game/slots/${id}/`).set(auth)).body.slot;
    assert.equal(slot.total_score, 37);
    assert.equal(slot.current_level, 7);
  });

  it('two different commits from the same revision: one wins, the other gets the current save back', async () => {
    const { app, auth } = await player('alpha');
    const id = await newSlot(app, auth);
    const first = request(app).post(`/api/game/slots/${id}/commit/`).set(auth).send({ expectRevision: 0, docs: [{ key: 'world', value: world(1) }] });
    const second = request(app).post(`/api/game/slots/${id}/commit/`).set(auth).send({ expectRevision: 0, docs: [{ key: 'world', value: world(2) }] });
    const [r1, r2] = await Promise.all([first, second]);
    assert.deepEqual([r1.status, r2.status].sort(), [200, 409]);
    const loser = r1.status === 409 ? r1 : r2;
    assert.equal(loser.body.error, 'revision_conflict');
    assert.equal(loser.body.slot.revision, 1, 'the conflict answer carries the current save');
    assert.ok(loser.body.docs.world);
  });

  it('never lowers the level and never overwrites a newer document version', async () => {
    const { app, auth } = await player('alpha');
    const id = await newSlot(app, auth);
    await request(app).post(`/api/game/slots/${id}/commit/`).set(auth).send({ expectRevision: 0, slot: { levelAtLeast: 9 }, docs: [{ key: 'progression', schemaVersion: 2, value: { a: 1 } }] });
    const lower = await request(app).post(`/api/game/slots/${id}/commit/`).set(auth).send({ expectRevision: 1, slot: { levelAtLeast: 3 } });
    assert.equal(lower.body.slot.current_level, 9);
    const older = await request(app).post(`/api/game/slots/${id}/commit/`).set(auth).send({ expectRevision: 2, docs: [{ key: 'progression', schemaVersion: 1, value: { a: 0 } }] });
    assert.equal(older.status, 409);
    assert.equal(older.body.error, 'schema_newer');
    const after = (await request(app).get(`/api/game/slots/${id}/`).set(auth)).body;
    assert.deepEqual(after.docs.progression, { schemaVersion: 2, value: { a: 1 } });
    assert.equal(after.slot.revision, 2, 'a refused commit changes nothing');
  });

  it('rejects bad keys, bad shapes and oversized documents', async () => {
    const { app, auth } = await player('alpha');
    const id = await newSlot(app, auth);
    const send = (body) => request(app).post(`/api/game/slots/${id}/commit/`).set(auth).send(body);
    assert.equal((await send({ docs: [] })).status, 400, 'expectRevision is required');
    assert.equal((await send({ expectRevision: 0, docs: [{ key: 'user_settings', value: {} }] })).status, 400, 'account keys are not slot keys');
    assert.equal((await send({ expectRevision: 0, docs: [{ key: 'world:../x', value: {} }] })).status, 400);
    assert.equal((await send({ expectRevision: 0, docs: [{ key: 'world', value: 'text' }] })).status, 400);
    assert.equal((await send({ expectRevision: 0, token: 'not-a-uuid', receipt: { kind: 'quest', value: 1 } })).status, 400);
    assert.equal((await send({ expectRevision: 0, token: randomUUID() })).status, 400, 'a token needs a receipt');
    assert.equal((await send({ expectRevision: 0, slot: { scoreAdd: -5 } })).status, 400);
    const big = { blob: 'x'.repeat(MAX_DOC_BYTES) };
    assert.equal((await send({ expectRevision: 0, docs: [{ key: 'world', value: big }] })).status, 413);
  });

  it('limits save writes per player', async () => {
    const { app, auth } = await player('alpha', { gameWritesPerUserPerMinute: 3 });
    const id = await newSlot(app, auth);
    const statuses = [];
    for (let i = 1; i <= 3; i += 1) {
      statuses.push((await request(app).post(`/api/game/slots/${id}/commit/`).set(auth).send({ expectRevision: i - 1 })).status);
    }
    assert.deepEqual(statuses, [200, 200, 429], 'creating the slot counted as the first write');
  });
});

describe('Account documents and leaderboard', () => {
  it('stores whitelisted settings per account', async () => {
    const a = await player('alpha');
    const b = await player('bravo');
    assert.equal((await request(a.app).put('/api/game/account-docs/user_settings/').set(a.auth).send({ display_mode: 2 })).status, 200);
    assert.equal((await request(a.app).put('/api/game/account-docs/operative_progression/').set(a.auth).send({ GlobalSparksBank: 300 })).status, 200);
    assert.equal((await request(a.app).put('/api/game/account-docs/anything/').set(a.auth).send({ x: 1 })).status, 404);
    assert.equal((await request(a.app).put('/api/game/account-docs/user_settings/').set(a.auth).send([1])).status, 400);
    const docs = (await request(a.app).get('/api/game/account-docs/').set(a.auth)).body;
    assert.deepEqual(docs.user_settings, { display_mode: 2 });
    assert.deepEqual((await request(b.app).get('/api/game/account-docs/').set(b.auth)).body, {});
  });

  it('ranks accounts by their best slot and shows only usernames', async () => {
    const a = await player('alpha');
    const b = await player('bravo');
    for (const [p, scores] of [[a, [50, 120]], [b, [90]]]) {
      for (const score of scores) {
        const id = await newSlot(p.app, p.auth);
        await request(p.app).post(`/api/game/slots/${id}/commit/`).set(p.auth).send({ expectRevision: 0, slot: { scoreAdd: score } });
      }
    }
    const board = (await request(makeApp().app).get('/api/game/leaderboard/')).body;
    assert.deepEqual(board.map((r) => [r.rank, r.username, r.score]), [[1, 'alpha', 120], [2, 'bravo', 90]]);
    assert.deepEqual(Object.keys(board[0]).sort(), ['level', 'rank', 'score', 'username']);
  });

  it('account deletion removes every save row for that account only', async () => {
    const a = await player('alpha');
    const b = await player('bravo');
    const id = await newSlot(a.app, a.auth);
    await request(a.app).post(`/api/game/slots/${id}/commit/`).set(a.auth).send({ expectRevision: 0, token: randomUUID(),
      receipt: { kind: 'quest', value: { q: 1 } }, docs: [{ key: 'world', value: world(1) }] });
    await request(a.app).put('/api/game/account-docs/user_settings/').set(a.auth).send({ display_mode: 1 });
    await newSlot(b.app, b.auth);
    await deleteAccount(pool, a.user.id);
    const count = async (table, where = 'true') => (await pool.query(`SELECT count(*)::int AS n FROM ${table} WHERE ${where}`)).rows[0].n;
    assert.equal(await count('game_slot', `user_id = ${a.user.id}`), 0);
    assert.equal(await count('game_slot_doc'), 0);
    assert.equal(await count('game_slot_receipt'), 0);
    assert.equal(await count('game_account_doc'), 0);
    assert.equal(await count('game_slot', `user_id = ${b.user.id}`), 1);
  });

  it('the migration drops the old unowned save tables', async () => {
    const old = await pool.query("SELECT to_regclass('public.game_state') AS a, to_regclass('public.game_sessions') AS b");
    assert.deepEqual(old.rows[0], { a: null, b: null });
  });
});
