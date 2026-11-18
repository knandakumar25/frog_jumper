/**
 * Player account deletion (2026-11-18): the emailed-code request, the delete itself, removal of
 * every linked row, the notice email, refused tokens afterwards, and the Clerk deletion with its
 * retry queue. Clerk is faked; no network call is made.
 */
import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import request from 'supertest';

import { CLERK_RETRY_DAYS, deleteAccount, processClerkDeletions } from '../src/lib/accountDeletion.js';
import { deleteClerkUser } from '../src/lib/clerk.js';
import { issueTokenPair } from '../src/lib/tokens.js';
import { createCode } from '../src/lib/twoFactor.js';
import { baseConfig, makeApp, makeUser, pool, useDatabase } from './helpers.js';

useDatabase();

const bearer = (token) => ({ Authorization: `Bearer ${token}` });
const lastCode = (outbox) => /(\d{6})/.exec(outbox().at(-1).text)[1];
const count = async (table, where = 'true', params = []) =>
  (await pool.query(`SELECT count(*)::int AS n FROM ${table} WHERE ${where}`, params)).rows[0].n;

/** A signed-in user with a token pair, an app, and optional extra user fields. */
async function signedIn(fields = {}) {
  const ctx = makeApp();
  const user = await makeUser({ username: 'leavingfrog', email: 'leaving@example.com', country: 'Japan', ...fields });
  const pair = await issueTokenPair(pool, baseConfig, user.id);
  return { ...ctx, user, pair };
}

describe('Account deletion routes', () => {
  it('deletes only with the emailed code and the typed DELETE, then refuses old tokens', async () => {
    const { app, outbox, pair, user } = await signedIn();
    await createCode(pool, user.id, 'opt_in', baseConfig);
    await pool.query(`INSERT INTO accounts_browsersession (session_id, user_id, expires_at)
      VALUES (gen_random_uuid(), $1, now() + interval '1 hour')`, [user.id]);
    await pool.query(`INSERT INTO accounts_pendingregistration (clerk_user_id, email, token_hash, last_sent_at, expires_at)
      VALUES ('user_pending123', 'LEAVING@example.com', 'hash-1', now(), now() + interval '1 day')`);

    assert.equal((await request(app).post('/api/auth/account/delete/').set(bearer(pair.access))
      .send({ code: '000000', confirm: 'DELETE' })).status, 400);
    assert.equal((await request(app).post('/api/auth/account/delete-request/').set(bearer(pair.access))).status, 200);
    assert.ok(outbox().at(-1).subject.includes('Deleting'));
    const code = lastCode(outbox);
    assert.equal((await request(app).post('/api/auth/account/delete/').set(bearer(pair.access))
      .send({ code, confirm: 'delete' })).status, 400, 'the typed word must be exactly DELETE');
    const res = await request(app).post('/api/auth/account/delete/').set(bearer(pair.access)).send({ code, confirm: 'DELETE' });
    assert.equal(res.status, 200);

    assert.equal(await count('accounts_user'), 0);
    assert.equal(await count('accounts_refreshtoken'), 0);
    assert.equal(await count('accounts_twofactorcode'), 0);
    assert.equal(await count('accounts_browsersession'), 0);
    assert.equal(await count('accounts_pendingregistration'), 0, 'pending registration with the same email is removed');
    assert.equal(await count('accounts_clerkdeletion'), 0, 'a password account queues no Clerk deletion');
    assert.equal(outbox().at(-1).subject, 'Your Frog Jumper Account Was Deleted');
    assert.deepEqual(outbox().at(-1).to, ['leaving@example.com']);
    assert.equal((await request(app).get('/api/auth/profile/').set(bearer(pair.access))).status, 401);
    assert.equal((await request(app).post('/api/auth/token/refresh/').send({ refresh: pair.refresh })).status, 401);
  });

  it('refuses both steps when signed out', async () => {
    const { app } = makeApp();
    assert.equal((await request(app).post('/api/auth/account/delete-request/')).status, 401);
    assert.equal((await request(app).post('/api/auth/account/delete/').send({ code: '123456', confirm: 'DELETE' })).status, 401);
  });

  it('needs a code and ignores a code issued for another purpose', async () => {
    const { app, outbox, pair } = await signedIn({ is_2fa_enabled: true });
    assert.equal((await request(app).post('/api/auth/account/delete/').set(bearer(pair.access)).send({ confirm: 'DELETE' })).status, 400);
    await request(app).post('/api/auth/2fa/disable-request/').set(bearer(pair.access));
    const res = await request(app).post('/api/auth/account/delete/').set(bearer(pair.access))
      .send({ code: lastCode(outbox), confirm: 'DELETE' });
    assert.equal(res.status, 400);
    assert.equal(await count('accounts_user'), 1);
  });

  it('limits emailed deletion codes like the other emailed codes', async () => {
    const { app, pair } = await signedIn();
    const statuses = [];
    for (let i = 0; i <= baseConfig.codesPerUserPer15Min; i += 1) {
      statuses.push((await request(app).post('/api/auth/account/delete-request/').set(bearer(pair.access))).status);
    }
    assert.equal(statuses.at(-1), 429);
    assert.ok(statuses.slice(0, -1).every((s) => s === 200));
  });

  it('deletes a Clerk-only account and its Clerk identity at once when Clerk answers', async () => {
    const { app, outbox, deps, pair } = await signedIn({ clerk_user_id: 'user_abc123' });
    const deletedIds = [];
    deps.clerk.deleteUser = async (id) => { deletedIds.push(id); return 'deleted'; };
    await request(app).post('/api/auth/account/delete-request/').set(bearer(pair.access));
    const res = await request(app).post('/api/auth/account/delete/').set(bearer(pair.access))
      .send({ code: lastCode(outbox), confirm: 'DELETE' });
    assert.equal(res.status, 200);
    assert.deepEqual(deletedIds, ['user_abc123']);
    assert.equal(await count('accounts_clerkdeletion'), 0);
    assert.equal(await count('accounts_user'), 0);
  });

  it('still deletes the account when Clerk is down, and keeps the Clerk id queued for retry', async () => {
    const { app, outbox, deps, pair } = await signedIn({ clerk_user_id: 'user_down456' });
    deps.clerk.deleteUser = async () => { throw new Error('Clerk unreachable'); };
    await request(app).post('/api/auth/account/delete-request/').set(bearer(pair.access));
    const res = await request(app).post('/api/auth/account/delete/').set(bearer(pair.access))
      .send({ code: lastCode(outbox), confirm: 'DELETE' });
    assert.equal(res.status, 200);
    assert.equal(await count('accounts_user'), 0);
    const row = (await pool.query('SELECT attempts, last_error, next_attempt_at > now() AS later FROM accounts_clerkdeletion')).rows[0];
    assert.equal(row.attempts, 1);
    assert.match(row.last_error, /unreachable/);
    assert.equal(row.later, true);
  });
});

describe('Clerk deletion queue', () => {
  const quiet = { warn() {} };

  it('retries due rows, removes them on success, and leaves rows that are not due yet', async () => {
    await pool.query(`INSERT INTO accounts_clerkdeletion (clerk_user_id) VALUES ('user_due1')`);
    await pool.query(`INSERT INTO accounts_clerkdeletion (clerk_user_id, next_attempt_at) VALUES ('user_later', now() + interval '1 hour')`);
    const seen = [];
    const result = await processClerkDeletions(pool, baseConfig, async (id) => { seen.push(id); }, { logger: quiet });
    assert.deepEqual(seen, ['user_due1']);
    assert.deepEqual(result, { deleted: 1, failed: 0, stuck: 0 });
    assert.equal(await count('accounts_clerkdeletion'), 1);
  });

  it('backs off after each failure and stops trying after the retry window', async () => {
    await pool.query(`INSERT INTO accounts_clerkdeletion (clerk_user_id) VALUES ('user_flaky')`);
    const failing = async () => { throw new Error('HTTP 500'); };
    assert.equal((await processClerkDeletions(pool, baseConfig, failing, { logger: quiet })).failed, 1);
    const first = (await pool.query(`SELECT attempts, round(extract(epoch FROM next_attempt_at - now()) / 3600) AS hours
      FROM accounts_clerkdeletion`)).rows[0];
    assert.equal(first.attempts, 1);
    assert.equal(Number(first.hours), 1);
    await pool.query(`UPDATE accounts_clerkdeletion SET next_attempt_at = now(), queued_at = now() - make_interval(days => $1)`,
      [CLERK_RETRY_DAYS + 1]);
    const late = await processClerkDeletions(pool, baseConfig, failing, { logger: quiet });
    assert.deepEqual(late, { deleted: 0, failed: 0, stuck: 1 });
  });

  it('treats a Clerk 404 as already deleted and malformed ids as nothing to delete', async () => {
    const config = { ...baseConfig, clerkSecretKey: 'sk_test_fake', clerkApiBase: 'https://clerk.invalid/v1', clerkTimeoutMs: 1000 };
    const calls = [];
    const answer = (status) => async (url, init) => { calls.push([init.method, url]); return new Response('', { status }); };
    assert.equal(await deleteClerkUser('user_gone', config, { request: answer(404), logger: quiet }), 'already_gone');
    assert.equal(await deleteClerkUser('user_real', config, { request: answer(200), logger: quiet }), 'deleted');
    await assert.rejects(deleteClerkUser('user_err', config, { request: answer(500), logger: quiet }));
    assert.equal(await deleteClerkUser('not a clerk id', config, { request: answer(200), logger: quiet }), 'already_gone');
    assert.deepEqual(calls.map(([m]) => m), ['DELETE', 'DELETE', 'DELETE']);
    assert.ok(calls[0][1].endsWith('/users/user_gone'));
  });

  it('deleteAccount reports an account that is already gone', async () => {
    assert.deepEqual(await deleteAccount(pool, 999999), { deleted: false, email: null, clerkUserId: null });
  });
});
