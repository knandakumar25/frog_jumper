/**
 * Tests for the fixes from the 2026-11-15 EGM review (issues 1, 2, 5, 6, 7 and 8 in
 * `.validation/egm/goldfish_review_report.md`): rate limits, trusted-proxy client addresses,
 * emailed codes for turning 2FA off and changing email, refresh-token families and cleanup,
 * constant-work login, and the migration checksum and schema checks.
 */
import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import request from 'supertest';

import { isCheckableHash, makeUnusablePassword } from '../src/lib/djangoHash.js';
import { limitKeyForIp, normalizeIp, rateLimit } from '../src/lib/throttle.js';
import { issueTokenPair, purgeExpired } from '../src/lib/tokens.js';
import { checkSchema, migrate } from '../src/migrate.js';
import { baseConfig, makeApp, makeUser, pool, useDatabase } from './helpers.js';

useDatabase();

const bearer = (token) => ({ Authorization: `Bearer ${token}` });
const lastCode = (outbox) => /(\d{6})/.exec(outbox().at(-1).text)[1];

describe('Rate limits (issue 1)', () => {
  it('limits login per IP', async () => {
    const { app } = makeApp({ authPerIpPerMinute: 2 });
    const tryLogin = (n) => request(app).post('/api/auth/login/').send({ username: `nobody${n}`, password: 'x' });
    assert.equal((await tryLogin(1)).status, 401);
    assert.equal((await tryLogin(2)).status, 401);
    const blocked = await tryLogin(3);
    assert.equal(blocked.status, 429);
    assert.match(blocked.body.detail, /^Request was throttled/);
    assert.ok(Number(blocked.headers['retry-after']) > 0);
  });

  it('limits login per username across addresses', async () => {
    const { app } = makeApp({ loginPerUserPer15Min: 2 });
    const tryFrom = (ip) => request(app).post('/api/auth/login/').set('X-Forwarded-For', ip)
      .send({ username: 'targetfrog', password: 'guess' });
    assert.equal((await tryFrom('203.0.113.1')).status, 401);
    assert.equal((await tryFrom('203.0.113.2')).status, 401);
    assert.equal((await tryFrom('203.0.113.3')).status, 429);
  });

  it('limits register, 2fa/verify-login and token/refresh per IP too', async () => {
    const { app } = makeApp({ authPerIpPerMinute: 1 });
    for (const [path, body] of [['/api/auth/register/', {}], ['/api/auth/2fa/verify-login/', {}], ['/api/auth/token/refresh/', {}]]) {
      await request(app).post(path).set('X-Forwarded-For', '198.51.100.9').send(body);
    }
    // The three routes share one per-IP budget, so the second and third were already refused.
    const again = await request(app).post('/api/auth/token/refresh/').set('X-Forwarded-For', '198.51.100.9').send({});
    assert.equal(again.status, 429);
  });

  it('caps emailed login codes, so logging in again cannot reset the five tries', async () => {
    const { app, outbox } = makeApp({ codesPerUserPer15Min: 3 });
    await makeUser({ username: 'codecap', email: 'codecap@example.com', country: 'Japan', is_2fa_enabled: true }, 'Password123!');
    const login = () => request(app).post('/api/auth/login/').send({ username: 'codecap', password: 'Password123!' });
    for (let i = 0; i < 3; i += 1) assert.equal((await login()).body.status, '2fa_required');
    const capped = await login();
    assert.equal(capped.status, 429);
    assert.equal(outbox().length, 3);
  });
});

describe('Client address (issue 2)', () => {
  it('ignores X-Forwarded-For from callers that are not the proxy', async () => {
    const { app } = makeApp({ trustedProxyAddresses: [], browserThrottlePerMinute: 1 });
    const start = (ip) => request(app).post('/api/auth/browser-start/').set('X-Forwarded-For', ip).send({});
    assert.equal((await start('203.0.113.10')).status, 200);
    // A forged, different address does not buy a fresh bucket.
    assert.equal((await start('203.0.113.11')).status, 429);
  });

  it('uses the last X-Forwarded-For entry from the trusted proxy', async () => {
    const { app } = makeApp({ browserThrottlePerMinute: 1 });
    const start = (ip) => request(app).post('/api/auth/browser-start/').set('X-Forwarded-For', `1.1.1.1, ${ip}`).send({});
    assert.equal((await start('203.0.113.20')).status, 200);
    assert.equal((await start('203.0.113.21')).status, 200);
    assert.equal((await start('203.0.113.20')).status, 429);
  });

  it('groups IPv6 addresses by /64 and normalizes mapped IPv4', () => {
    assert.equal(limitKeyForIp('2001:db8:1:2:3:4:5:6'), limitKeyForIp('2001:db8:1:2:ffff::1'));
    assert.notEqual(limitKeyForIp('2001:db8:1:2::1'), limitKeyForIp('2001:db8:1:3::1'));
    assert.equal(normalizeIp('::ffff:10.0.0.5'), '10.0.0.5');
    assert.equal(normalizeIp('not-an-ip'), null);
  });

  it('forgets idle keys, so rotating keys cannot grow memory', async () => {
    let n = 0;
    const limiter = rateLimit({ windowMs: 5, limit: () => 10, key: () => `k${n++}` });
    const res = { set() { return this; }, status() { return this; }, json() {} };
    for (let i = 0; i < 100; i += 1) limiter({}, res, () => {});
    assert.equal(limiter.size(), 100);
    await new Promise((resolve) => setTimeout(resolve, 20));
    limiter.sweep();
    assert.equal(limiter.size(), 0);
  });
});

describe('Emailed code to turn 2FA off or change email (issue 5)', () => {
  const signedIn = async (fields) => {
    const ctx = makeApp();
    const user = await makeUser({ username: 'safefrog', email: 'safe@example.com', country: 'Japan', ...fields });
    const pair = await issueTokenPair(pool, baseConfig, user.id);
    return { ...ctx, user, pair };
  };

  it('turns 2FA off only with the emailed code, then signs other sessions out and notifies', async () => {
    const { app, outbox, pair } = await signedIn({ is_2fa_enabled: true });
    assert.equal((await request(app).patch('/api/auth/profile/').set(bearer(pair.access)).send({ is_2fa_enabled: false })).status, 400);
    assert.equal((await request(app).post('/api/auth/2fa/disable/').set(bearer(pair.access)).send({ code: '000000' })).status, 400);
    assert.equal((await request(app).post('/api/auth/2fa/disable-request/').set(bearer(pair.access))).status, 200);
    assert.ok(outbox().at(-1).subject.includes('Turning Off'));
    const res = await request(app).post('/api/auth/2fa/disable/').set(bearer(pair.access)).send({ code: lastCode(outbox) });
    assert.equal(res.status, 200);
    assert.equal((await request(app).get('/api/auth/profile/').set(bearer(pair.access))).body.is_2fa_enabled, false);
    assert.equal(outbox().at(-1).subject, 'Frog Jumper 2FA Was Turned Off');
    assert.equal((await request(app).post('/api/auth/token/refresh/').send({ refresh: pair.refresh })).status, 401);
  });

  it('refuses a disable request when 2FA is already off', async () => {
    const { app, pair } = await signedIn({});
    assert.equal((await request(app).post('/api/auth/2fa/disable-request/').set(bearer(pair.access))).status, 400);
  });

  it('changes the email only with a code sent to the current address', async () => {
    const { app, outbox, pair } = await signedIn({});
    const noCode = await request(app).patch('/api/auth/profile/').set(bearer(pair.access)).send({ email: 'new@example.com' });
    assert.equal(noCode.status, 400);
    assert.ok(noCode.body.email_code);
    await request(app).post('/api/auth/profile/email-code/').set(bearer(pair.access));
    assert.deepEqual(outbox().at(-1).to, ['safe@example.com']);
    const ok = await request(app).patch('/api/auth/profile/').set(bearer(pair.access))
      .send({ email: 'new@example.com', email_code: lastCode(outbox) });
    assert.equal(ok.status, 200);
    assert.equal(ok.body.email, 'new@example.com');
    assert.deepEqual(outbox().at(-1).to, ['safe@example.com']);
    assert.equal(outbox().at(-1).subject, 'Frog Jumper Email Address Changed');
    assert.equal((await request(app).post('/api/auth/token/refresh/').send({ refresh: pair.refresh })).status, 401);
  });

  it('needs no code when the email is sent back unchanged', async () => {
    const { app, pair } = await signedIn({});
    const res = await request(app).patch('/api/auth/profile/').set(bearer(pair.access))
      .send({ email: 'safe@example.com', first_name: 'Safe' });
    assert.equal(res.status, 200);
    assert.equal(res.body.first_name, 'Safe');
  });
});

describe('Refresh-token families and cleanup (issue 6)', () => {
  it('replaying a used refresh token signs out the newer tokens of that sign-in', async () => {
    const { app } = makeApp();
    const user = await makeUser({ username: 'familyfrog', email: 'family@example.com', country: 'Japan' });
    const first = await issueTokenPair(pool, baseConfig, user.id);
    const second = (await request(app).post('/api/auth/token/refresh/').send({ refresh: first.refresh })).body;
    assert.ok(second.refresh);
    assert.equal((await request(app).post('/api/auth/token/refresh/').send({ refresh: first.refresh })).status, 401);
    assert.equal((await request(app).post('/api/auth/token/refresh/').send({ refresh: second.refresh })).status, 401);
    const other = await issueTokenPair(pool, baseConfig, user.id);
    assert.equal((await request(app).post('/api/auth/token/refresh/').send({ refresh: other.refresh })).status, 200);
  });

  it('deletes expired tokens, old codes and stale unlinked browser requests', async () => {
    const user = await makeUser({ username: 'purgefrog', email: 'purge@example.com', country: 'Japan' });
    await issueTokenPair(pool, baseConfig, user.id);
    await pool.query("UPDATE accounts_refreshtoken SET expires_at = now() - interval '2 days'");
    await pool.query(`INSERT INTO accounts_twofactorcode (user_id, code, created_at, is_verified, purpose)
      VALUES ($1, '123456', now() - interval '2 days', false, 'login')`, [user.id]);
    await pool.query(`INSERT INTO accounts_browsersession (session_id, created_at, is_authorized, secret_hash, expires_at, intent, locale)
      VALUES (gen_random_uuid(), now(), false, 'x', now() - interval '2 days', 'login', 'en-US')`);
    assert.deepEqual(await purgeExpired(pool), { refreshTokens: 1, codes: 1, browserSessions: 1 });
  });
});

describe('Login does the same work for every account (issue 7)', () => {
  it('treats unusable and malformed hashes as not checkable', () => {
    assert.equal(isCheckableHash(makeUnusablePassword()), false);
    assert.equal(isCheckableHash('md5$abc$def'), false);
    assert.equal(isCheckableHash('pbkdf2_sha256$1000$salt$tooShort'), false);
  });

  it('answers a Clerk-only account with the same 401 as a wrong password', async () => {
    const { app } = makeApp();
    await makeUser({ username: 'clerkonly', email: 'clerkonly@example.com', country: 'Japan' }, null);
    const res = await request(app).post('/api/auth/login/').send({ username: 'clerkonly', password: 'anything' });
    assert.equal(res.status, 401);
    assert.deepEqual(res.body, { error: 'Invalid credentials' });
  });
});

describe('Migrations (issue 8)', () => {
  it('refuses to start when an applied migration file was edited', async () => {
    await pool.query("UPDATE node_migrations SET checksum = 'edited' WHERE name = '0001_schema.sql'");
    await assert.rejects(migrate(pool), /was edited after it was applied/);
    await pool.query("UPDATE node_migrations SET checksum = NULL WHERE name = '0001_schema.sql'");
    assert.deepEqual(await migrate(pool), []);
  });

  it('names missing columns instead of failing later', async () => {
    await pool.query('ALTER TABLE accounts_user DROP COLUMN date_of_birth');
    try {
      await assert.rejects(checkSchema(pool), /accounts_user\.date_of_birth/);
    } finally {
      await pool.query('ALTER TABLE accounts_user ADD COLUMN date_of_birth date');
    }
    await checkSchema(pool);
  });
});
