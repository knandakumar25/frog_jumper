/**
 * Ported from Django `accounts/tests/test_browser_auth.py` (8 tests): device proof, single-use
 * redemption, expiry and cancel, Clerk token verification with real RS256 keys, throttling, the
 * onboarding hand-off, and two parallel polls against PostgreSQL row locks.
 */
import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { SignJWT, generateKeyPair } from 'jose';
import request from 'supertest';

import { withOverrides } from '../src/config.js';
import { clerkSubject } from '../src/lib/clerk.js';
import { issueTokenPair } from '../src/lib/tokens.js';
import { baseConfig, makeApp, makeUser, pool, useDatabase } from './helpers.js';

useDatabase();

/**
 * App plus helpers bound to it: start, post, approve.
 * @param {object} [overrides]
 */
async function harness(overrides) {
  const ctx = makeApp(overrides);
  const user = await makeUser({ username: 'device', email: 'device@example.invalid', clerk_user_id: 'user_device' });
  const post = (action, data, token) => {
    const req = request(ctx.app).post(`/api/auth/browser-${action}/`).send(data);
    return token ? req.set('Authorization', `Bearer ${token}`) : req;
  };
  const start = async () => {
    const res = await post('start', { intent: 'signup', locale: 'ja' });
    assert.equal(res.status, 200);
    assert.equal(res.headers['cache-control'], 'no-store');
    return res.body;
  };
  const access = (await issueTokenPair(pool, baseConfig, user.id)).access;
  const approve = (data) => post('confirm', { session_id: data.session_id }, access);
  return { ...ctx, user, post, start, approve };
}

describe('Browser sign-in', () => {
  it('needs the private secret, and redemption is single use', async () => {
    const h = await harness();
    const data = await h.start();
    const row = (await pool.query('SELECT * FROM accounts_browsersession WHERE session_id = $1', [data.session_id])).rows[0];
    assert.notEqual(row.secret_hash, data.secret);
    assert.ok(!data.auth_url.includes(data.secret));
    assert.ok(data.auth_url.includes('intent=signup'));
    assert.ok(Math.abs((row.expires_at - Date.now()) / 1000 - 900) < 3);
    assert.deepEqual((await h.post('poll', data)).body, { status: 'pending' });
    assert.equal((await request(h.app).get('/api/auth/browser-poll/').query({ session_id: data.session_id })).status, 405);
    assert.equal((await h.post('poll', { session_id: data.session_id })).status, 410);
    assert.equal((await h.approve(data)).status, 200);
    const wrong = { ...data, secret: 'wrong' };
    assert.equal((await h.post('poll', wrong)).status, 410);
    assert.equal((await h.post('cancel', wrong)).status, 410);
    const result = await h.post('poll', data);
    assert.equal(result.status, 200);
    assert.equal(result.body.user.username, 'device');
    assert.ok(result.body.access);
    assert.equal((await h.post('poll', data)).status, 410);
    assert.equal((await h.approve(data)).status, 410);
  });

  it('refuses cancelled, expired and secretless requests', async () => {
    const h = await harness();
    for (const approved of [false, true]) {
      const data = await h.start();
      if (approved) await h.approve(data);
      assert.equal((await h.post('cancel', data)).status, 200);
      assert.equal((await h.post('poll', data)).status, 410);
      assert.equal((await h.approve(data)).status, 410);
    }
    const data = await h.start();
    await pool.query("UPDATE accounts_browsersession SET expires_at = now() - interval '1 second' WHERE session_id = $1", [data.session_id]);
    assert.equal((await h.post('poll', data)).status, 410);
    assert.equal((await h.approve(data)).status, 410);
    const legacy = (await pool.query(
      `INSERT INTO accounts_browsersession (session_id, created_at, is_authorized, secret_hash, intent, locale)
       VALUES (gen_random_uuid(), now(), false, '', 'login', 'en-US') RETURNING session_id`)).rows[0];
    assert.equal((await h.approve({ session_id: legacy.session_id })).status, 410);
  });

  it('needs proof to confirm, and binds only once', async () => {
    const h = await harness();
    const data = await h.start();
    assert.ok([401, 403].includes((await h.post('confirm', { ...data, clerk_user_id: 'user_device' })).status));
    assert.equal((await h.approve(data)).status, 200);
    assert.equal((await h.approve(data)).status, 409);
  });

  it('matches a Clerk identity by Clerk id only, and asks new users to onboard', async () => {
    const h = await harness();
    h.deps.clerk.subject = async () => 'user_device';
    let data = await h.start();
    assert.equal((await h.post('confirm', { ...data, clerk_token: 'verified-by-unit-fixture' })).status, 200);
    h.deps.clerk.subject = async () => 'user_missing';
    data = await h.start();
    const res = await h.post('confirm', { ...data, clerk_token: 'verified-by-unit-fixture' });
    assert.equal(res.status, 409);
    assert.equal(res.body.error, 'onboarding_required');
  });

  it('rejects bad input and throttles request creation', async () => {
    const h = await harness();
    assert.equal((await h.post('poll', { session_id: 'bad', secret: 'x' })).status, 410);
    assert.equal((await h.post('start', { intent: 'execute' })).status, 400);
    const limited = makeApp({ browserThrottlePerMinute: 1 }).app;
    assert.equal((await request(limited).post('/api/auth/browser-start/').send({})).status, 200);
    const blocked = await request(limited).post('/api/auth/browser-start/').send({});
    assert.equal(blocked.status, 429);
    assert.ok(Number(blocked.headers['retry-after']) > 0);
  });

  it('keeps the game request through email onboarding without approving it', async () => {
    const h = await harness({ clerkSecretKey: 'sk_test_fixture' });
    const data = await h.start();
    const identity = { id: 'user_fresh', email: 'fresh@example.invalid', email_verified: true,
      username: 'fresh', first_name: 'Fresh', last_name: 'Test' };
    h.deps.clerk.subject = async () => identity.id;
    h.deps.clerk.fetchUser = async () => identity;
    const started = await request(h.app).post('/api/auth/onboarding/start/')
      .send({ session_id: data.session_id, clerk_token: 'test-proof', clerk_user_id: identity.id });
    assert.equal(started.status, 200);
    const pending = (await pool.query(
      `SELECT b.session_id FROM accounts_pendingregistration p JOIN accounts_browsersession b ON b.id = p.browser_session_id
        WHERE p.clerk_user_id = $1`, [identity.id])).rows[0];
    assert.equal(pending.session_id, data.session_id);
    const token = /\/onboarding\?token=([\w-]+)/.exec(h.outbox().at(-1).text)[1];
    const validation = await request(h.app).get('/api/auth/onboarding/validate-token/').query({ token });
    assert.equal(validation.body.browser_locale, 'ja');
    const done = await request(h.app).post('/api/auth/onboarding/complete/')
      .send({ token, country: 'Japan', date_of_birth: '1995-04-12' });
    assert.equal(done.status, 200);
    assert.equal(done.body.browser_session_id, data.session_id);
    assert.equal(done.body.browser_locale, 'ja');
    assert.equal((await h.post('poll', data)).body.status, 'pending');
    assert.equal((await h.post('confirm', { session_id: data.session_id }, done.body.access)).status, 200);
    assert.equal((await h.post('poll', data)).body.user.email, identity.email);
  });
});

describe('Clerk token verification', () => {
  it('checks signature, issuer, origin, lifetime, session state and configuration', async () => {
    const { publicKey, privateKey } = await generateKeyPair('RS256');
    const config = withOverrides(baseConfig, {
      clerkIssuer: 'https://issuer.example',
      clerkJwksUrl: 'https://issuer.example/.well-known/jwks.json',
      clerkAuthorizedParties: ['http://localhost:5173'],
    });
    const now = Math.floor(Date.now() / 1000);
    const good = { iss: 'https://issuer.example', sub: 'user_test', exp: now + 60, nbf: now - 1, iat: now - 1, azp: 'http://localhost:5173' };
    const sign = (claims, key = privateKey, alg = 'RS256') => new SignJWT(claims).setProtectedHeader({ alg }).sign(key);
    assert.equal(await clerkSubject(await sign(good), config, { keys: publicKey }), 'user_test');
    const { exp: _drop, ...missingExp } = good;
    const variants = [{ ...good, exp: now - 10 }, { ...good, iss: 'https://foreign' }, { ...good, azp: 'https://foreign' },
      { ...good, nbf: now + 100 }, { ...good, sts: 'pending' }, missingExp, { ...good, sub: 'org_123' }];
    for (const claims of variants) {
      await assert.rejects(clerkSubject(await sign(claims), config, { keys: publicKey }),
        (e) => e.status === 403 && e.body.detail === 'Invalid Clerk session');
    }
    const hs256 = await sign(good, new TextEncoder().encode('wrong-wrong-wrong-wrong-wrong-32'), 'HS256');
    await assert.rejects(clerkSubject(hs256, config, { keys: publicKey }), (e) => e.status === 403);
    await assert.rejects(clerkSubject('anything', withOverrides(config, { clerkIssuer: '' })),
      (e) => e.body.detail === 'Clerk verification is not configured');
  });
});

describe('Parallel redemption', () => {
  it('gives tokens to exactly one of two simultaneous polls', async () => {
    const { app } = makeApp();
    const user = await makeUser({ username: 'parallel', email: 'parallel@example.invalid' });
    const data = (await request(app).post('/api/auth/browser-start/').send({})).body;
    await pool.query('UPDATE accounts_browsersession SET user_id = $2, is_authorized = true WHERE session_id = $1',
      [data.session_id, user.id]);
    const results = await Promise.all([0, 1].map(() => request(app).post('/api/auth/browser-poll/').send(data)));
    assert.deepEqual(results.map((r) => r.status).sort(), [200, 410]);
  });
});
