/**
 * Tests added with the Express port (no Django equivalent): the owner-approved security fixes of
 * 2026-11-15, Django password-hash compatibility, token handling, profile edits, the health
 * route, CORS, removed routes and repeatable migrations.
 */
import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { SignJWT } from 'jose';
import request from 'supertest';

import { loadConfig } from '../src/config.js';
import { checkPassword, hasUsablePassword, makePassword, makeUnusablePassword } from '../src/lib/djangoHash.js';
import { issueTokenPair } from '../src/lib/tokens.js';
import { migrate } from '../src/migrate.js';
import { baseConfig, makeApp, makeUser, pool, useDatabase } from './helpers.js';

useDatabase();

// Made the way Django's PBKDF2PasswordHasher makes them (1,200,000 rounds, Django 6's default).
const DJANGO_HASH = 'pbkdf2_sha256$1200000$axiomfixturesalt$K7IlXH1NiH7eEYCvHSMjO2qpuwAEI2U0ZWeQTxIZ4/U=';
const bearer = (token) => ({ Authorization: `Bearer ${token}` });

describe('Django password hashes', () => {
  it('verifies a hash in Django format, and only with the right password', async () => {
    assert.equal(await checkPassword('Fixture-Password-123!', DJANGO_HASH), true);
    assert.equal(await checkPassword('fixture-password-123!', DJANGO_HASH), false);
  });

  it('signs in an account whose hash Django wrote', async () => {
    const user = await makeUser({ username: 'djangofrog', email: 'djangofrog@example.com', country: 'Japan' }, 'placeholder');
    await pool.query('UPDATE accounts_user SET password = $2 WHERE id = $1', [user.id, DJANGO_HASH]);
    const { app } = makeApp();
    const res = await request(app).post('/api/auth/login/').send({ username: 'djangofrog', password: 'Fixture-Password-123!' });
    assert.equal(res.status, 200);
    assert.equal(res.body.user.username, 'djangofrog');
  });

  it('writes new hashes in the same format, and never accepts an unusable one', async () => {
    const encoded = await makePassword('pw', 1000, 'fixedsalt');
    assert.match(encoded, /^pbkdf2_sha256\$1000\$fixedsalt\$[A-Za-z0-9+/]{43}=$/);
    const unusable = makeUnusablePassword();
    assert.equal(hasUsablePassword(unusable), false);
    assert.equal(await checkPassword(unusable.slice(1), unusable), false);
    assert.equal(await checkPassword('pw', 'md5$abc$def'), false);
  });
});

describe('Login does not reveal accounts', () => {
  it('gives the same answer for an unknown user, a wrong password and an inactive account', async () => {
    await makeUser({ username: 'realfrog', email: 'realfrog@example.com', country: 'Japan' }, 'RightPassword1!');
    const inactive = await makeUser({ username: 'sleepfrog', email: 'sleepfrog@example.com', country: 'Japan' }, 'RightPassword1!');
    await pool.query('UPDATE accounts_user SET is_active = false WHERE id = $1', [inactive.id]);
    const { app } = makeApp();
    const tries = await Promise.all([
      { username: 'nobodyfrog', password: 'RightPassword1!' },
      { username: 'realfrog', password: 'WrongPassword1!' },
      { username: 'sleepfrog', password: 'RightPassword1!' },
    ].map((body) => request(app).post('/api/auth/login/').send(body)));
    for (const res of tries) {
      assert.equal(res.status, 401);
      assert.deepEqual(res.body, { error: 'Invalid credentials' });
    }
  });
});

describe('2FA codes', () => {
  const twoFactorUser = async () => {
    const ctx = makeApp();
    await makeUser({ username: 'codefrog', email: 'codefrog@example.com', country: 'Japan', is_2fa_enabled: true }, 'Password123!');
    const login = await request(ctx.app).post('/api/auth/login/').send({ username: 'codefrog', password: 'Password123!' });
    const code = /code is: (\d{6})/.exec(ctx.outbox().at(-1).text)[1];
    const verify = (value) => request(ctx.app).post('/api/auth/2fa/verify-login/')
      .send({ login_session_id: login.body.login_session_id, code: value });
    return { ...ctx, code, verify };
  };
  const wrong = (code) => String((Number(code) + 1) % 1_000_000).padStart(6, '0');

  it('are six digits and sign the user in when right', async () => {
    const { code, verify } = await twoFactorUser();
    assert.match(code, /^\d{6}$/);
    const res = await verify(code);
    assert.equal(res.status, 200);
    assert.ok(res.body.access);
    assert.equal(res.body.user.username, 'codefrog');
    assert.equal((await verify(code)).status, 400);
  });

  it('stop working after five wrong guesses, even with the right code', async () => {
    const { code, verify } = await twoFactorUser();
    for (let i = 0; i < 5; i += 1) assert.equal((await verify(wrong(code))).status, 400);
    const res = await verify(code);
    assert.equal(res.status, 400);
    assert.equal(res.body.error, 'Invalid or expired code');
  });

  it('still work after fewer than five wrong guesses', async () => {
    const { code, verify } = await twoFactorUser();
    for (let i = 0; i < 4; i += 1) await verify(wrong(code));
    assert.equal((await verify(code)).status, 200);
  });

  it('expire after 10 minutes', async () => {
    const { code, verify } = await twoFactorUser();
    await pool.query("UPDATE accounts_twofactorcode SET created_at = now() - interval '11 minutes'");
    assert.equal((await verify(code)).status, 400);
  });

  it('answer 400, not 500, for a malformed login session id', async () => {
    const { app } = makeApp();
    const res = await request(app).post('/api/auth/2fa/verify-login/').send({ login_session_id: 'not-a-uuid', code: '123456' });
    assert.equal(res.status, 400);
    assert.equal(res.body.error, 'Invalid or expired session');
  });

  it('turn 2FA on with the opt-in code; the profile switch cannot turn it off', async () => {
    const { app, outbox } = makeApp();
    const user = await makeUser({ username: 'togglefrog', email: 'togglefrog@example.com', country: 'Japan' });
    const { access } = await issueTokenPair(pool, baseConfig, user.id);
    const refused = await request(app).patch('/api/auth/profile/').set(bearer(access)).send({ is_2fa_enabled: true });
    assert.equal(refused.status, 400);
    await request(app).post('/api/auth/2fa/enable/').set(bearer(access));
    const code = /is: (\d{6})/.exec(outbox().at(-1).text)[1];
    assert.equal((await request(app).post('/api/auth/2fa/verify-enable/').set(bearer(access)).send({ code })).status, 200);
    assert.equal((await request(app).get('/api/auth/profile/').set(bearer(access))).body.is_2fa_enabled, true);
    // Since the 2026-11-15 review, switching off needs an emailed code (hardening.test.js).
    const off = await request(app).patch('/api/auth/profile/').set(bearer(access)).send({ is_2fa_enabled: false });
    assert.equal(off.status, 400);
    assert.ok(off.body.is_2fa_enabled);
    const same = await request(app).patch('/api/auth/profile/').set(bearer(access)).send({ is_2fa_enabled: true });
    assert.equal(same.status, 200);
  });
});

describe('Tokens', () => {
  it('rotate on refresh, and a refresh token works only once', async () => {
    const { app } = makeApp();
    const user = await makeUser({ username: 'tokenfrog', email: 'tokenfrog@example.com', country: 'Japan' });
    const first = await issueTokenPair(pool, baseConfig, user.id);
    const res = await request(app).post('/api/auth/token/refresh/').send({ refresh: first.refresh });
    assert.equal(res.status, 200);
    assert.ok(res.body.access && res.body.refresh);
    assert.notEqual(res.body.refresh, first.refresh);
    const replay = await request(app).post('/api/auth/token/refresh/').send({ refresh: first.refresh });
    assert.equal(replay.status, 401);
    assert.equal(replay.body.code, 'token_not_valid');
    assert.equal((await request(app).get('/api/auth/profile/').set(bearer(res.body.access))).status, 200);
  });

  it('cannot be swapped: an access token is not a refresh token, nor the reverse', async () => {
    const { app } = makeApp();
    const user = await makeUser({ username: 'swapfrog', email: 'swapfrog@example.com', country: 'Japan' });
    const pair = await issueTokenPair(pool, baseConfig, user.id);
    assert.equal((await request(app).post('/api/auth/token/refresh/').send({ refresh: pair.access })).status, 401);
    const asAccess = await request(app).get('/api/auth/profile/').set(bearer(pair.refresh));
    assert.equal(asAccess.status, 401);
    assert.equal(asAccess.body.code, 'token_not_valid');
  });

  it('refuses tokens signed with another key (such as the old Django key)', async () => {
    const { app } = makeApp();
    const user = await makeUser({ username: 'oldkeyfrog', email: 'oldkeyfrog@example.com', country: 'Japan' });
    const forged = await new SignJWT({ token_type: 'access', user_id: String(user.id) })
      .setProtectedHeader({ alg: 'HS256' }).setIssuedAt().setExpirationTime('1h').setJti('x')
      .sign(new TextEncoder().encode('django-insecure-some-other-key-entirely'));
    assert.equal((await request(app).get('/api/auth/profile/').set(bearer(forged))).status, 401);
  });

  it('give 401 on routes that allow anonymous use when a sent token is bad', async () => {
    const { app } = makeApp();
    const res = await request(app).post('/api/auth/login/').set('Authorization', 'Bearer not.a.real.token').send({});
    assert.equal(res.status, 401);
  });

  it('require a signing key of at least 32 characters', () => {
    assert.throws(() => loadConfig({}), /JWT_SIGNING_KEY/);
    assert.throws(() => loadConfig({ JWT_SIGNING_KEY: 'short' }), /JWT_SIGNING_KEY/);
    assert.doesNotThrow(() => loadConfig({ JWT_SIGNING_KEY: 'x'.repeat(32) }));
  });
});

describe('Profile edits', () => {
  it('update allowed fields and refuse an address another account holds', async () => {
    const { app } = makeApp();
    await makeUser({ username: 'otherfrog', email: 'taken@example.com', country: 'Japan' });
    const user = await makeUser({ username: 'editfrog', email: 'editfrog@example.com', country: 'Japan' });
    const { access } = await issueTokenPair(pool, baseConfig, user.id);
    const ok = await request(app).patch('/api/auth/profile/').set(bearer(access))
      .send({ first_name: 'Edit', phone_number: '+81 3 1234', country: 'Canada' });
    assert.equal(ok.status, 200);
    assert.equal(ok.body.first_name, 'Edit');
    assert.equal(ok.body.country, 'Canada');
    const clash = await request(app).patch('/api/auth/profile/').set(bearer(access)).send({ email: 'taken@example.com' });
    assert.equal(clash.status, 400);
    assert.deepEqual(clash.body.email, ['user with this email already exists.']);
    const put = await request(app).put('/api/auth/profile/').set(bearer(access)).send({ first_name: 'NoName' });
    assert.equal(put.status, 400);
    assert.ok(put.body.username && put.body.email);
  });
});

describe('Service routes', () => {
  it('answers the health check', async () => {
    const { app } = makeApp();
    const res = await request(app).get('/api/health');
    assert.equal(res.status, 200);
    assert.deepEqual(res.body, { status: 'ok' });
  });

  it('allows cross-origin calls without credentials', async () => {
    const { app } = makeApp();
    const res = await request(app).options('/api/auth/login/')
      .set('Origin', 'http://localhost:5173').set('Access-Control-Request-Method', 'POST');
    assert.equal(res.status, 200);
    assert.equal(res.headers['access-control-allow-origin'], '*');
    assert.ok(res.headers['access-control-allow-headers'].includes('authorization'));
  });

  it('no longer serves the removed social sign-in routes or the Django admin', async () => {
    const { app } = makeApp();
    for (const path of ['/api/auth/social/login/google/', '/api/auth/social/callback/', '/admin/']) {
      assert.equal((await request(app).get(path)).status, 404, path);
    }
  });

  it('answers DRF-style 400 for a malformed JSON body', async () => {
    const { app } = makeApp();
    const res = await request(app).post('/api/auth/login/').set('Content-Type', 'application/json').send('{bad');
    assert.equal(res.status, 400);
    assert.match(res.body.detail, /^JSON parse error/);
  });

  it('re-running migrations changes nothing', async () => {
    assert.deepEqual(await migrate(pool), []);
  });
});
