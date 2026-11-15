/**
 * Ported from Django `accounts/tests/test_onboarding_views.py` (41 tests): start, validate-token,
 * complete, and stale-token tolerance. Clerk is faked through `deps.clerk.fetchUser`; mail is read
 * from the in-memory outbox. Nothing here talks to Clerk or Resend.
 */
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { describe, it } from 'node:test';
import request from 'supertest';

import { ClerkUnavailable, ClerkUserNotFound } from '../src/lib/clerk.js';
import { hasUsablePassword } from '../src/lib/djangoHash.js';
import { makeApp, makeUser, pool, useDatabase } from './helpers.js';

useDatabase();

const CLERK_ID = 'user_2abcDEF123';
const EMAIL = 'operator@example.com';
const RAW_TOKEN = 'raw-onboarding-token';
const TOKEN_RE = /\/onboarding\?token=([A-Za-z0-9_-]+)/;
const START = '/api/auth/onboarding/start/';
const VALIDATE = '/api/auth/onboarding/validate-token/';
const COMPLETE = '/api/auth/onboarding/complete/';
const STALE = 'Bearer not.a.real.token';

const hash = (raw) => createHash('sha256').update(raw).digest('hex');
const clerkUser = (overrides = {}) => ({ id: CLERK_ID, email: EMAIL, email_verified: true, username: 'kaito',
  first_name: 'Kaito', last_name: 'Kurogane', ...overrides });
const todayUtc = (offsetDays = 0) => new Date(Date.now() + offsetDays * 86_400_000).toISOString().slice(0, 10);

/** App with a Clerk key set (onboarding is inert without one), plus helpers. */
function harness(overrides = {}) {
  const ctx = makeApp({ clerkSecretKey: 'sk_test_not_real', ...overrides });
  let fetchCalls = 0;
  /** POST start with a fake Clerk answer (`user`) or failure (`error`). */
  const start = ({ user = clerkUser(), error, clerkUserId = CLERK_ID, headers = {} } = {}) => {
    ctx.deps.clerk.fetchUser = async () => {
      fetchCalls += 1;
      if (error) throw error;
      return user;
    };
    return request(ctx.app).post(START).set(headers).send({ clerk_user_id: clerkUserId });
  };
  const complete = ({ country = 'Japan', date_of_birth = '1995-04-12', token = RAW_TOKEN, headers = {} } = {}) =>
    request(ctx.app).post(COMPLETE).set(headers).send({ token, country, date_of_birth });
  const validate = (token, headers = {}) => {
    const req = request(ctx.app).get(VALIDATE).set(headers);
    return token === undefined ? req : req.query({ token });
  };
  return { ...ctx, start, complete, validate, fetchCalls: () => fetchCalls };
}

/** Insert a pending registration directly (one send already made, link valid for an hour). */
async function makePending(overrides = {}) {
  const row = { clerk_user_id: CLERK_ID, email: EMAIL, token_hash: hash(RAW_TOKEN), clerk_username: 'kaito',
    first_name: 'Kaito', last_name: 'Kurogane', last_sent_at: new Date(), expires_at: new Date(Date.now() + 3600_000),
    sent_count: 1, completed_at: null, request_ip: null, ...overrides };
  const cols = Object.keys(row);
  await pool.query(`INSERT INTO accounts_pendingregistration (${cols.join(', ')}, created_at)
    VALUES (${cols.map((_, i) => `$${i + 1}`).join(', ')}, now())`, cols.map((c) => row[c]));
}

const pendingRow = async () => (await pool.query('SELECT * FROM accounts_pendingregistration WHERE clerk_user_id = $1', [CLERK_ID])).rows[0];
const userCount = async () => (await pool.query('SELECT count(*)::int AS n FROM accounts_user')).rows[0].n;

describe('onboarding/start', () => {
  it('sends one email and never returns the token', async () => {
    const h = harness();
    const res = await h.start();
    assert.equal(res.status, 200);
    assert.equal(res.body.status, 'email_sent');
    assert.equal(res.body.email, 'o***@example.com');
    assert.equal(res.body.token, undefined);
    assert.equal(h.outbox().length, 1);
    const match = TOKEN_RE.exec(h.outbox()[0].text);
    assert.ok(match, 'the email body must carry the link');
    const pending = await pendingRow();
    assert.equal(pending.token_hash, hash(match[1]));
    assert.notEqual(pending.token_hash, match[1]);
    assert.equal(pending.token_hash.length, 64);
    assert.equal(pending.sent_count, 1);
  });

  it('mails a link that is not locale-prefixed', async () => {
    const h = harness();
    await h.start();
    assert.ok(TOKEN_RE.exec(h.outbox()[0].text)[0].startsWith('/onboarding?token='));
  });

  it('rejects a malformed id before any network call', async () => {
    const h = harness();
    const res = await h.start({ clerkUserId: 'user_a/../../x' });
    assert.equal(res.status, 400);
    assert.equal(res.body.error, 'invalid_clerk_user_id');
    assert.equal(h.fetchCalls(), 0);
    assert.equal(h.outbox().length, 0);
  });

  it('answers 404 for an unknown Clerk user', async () => {
    const h = harness();
    const res = await h.start({ error: new ClerkUserNotFound('nope') });
    assert.equal(res.status, 404);
    assert.equal(res.body.error, 'unknown_clerk_user');
    assert.equal(h.outbox().length, 0);
  });

  it('answers 502 when Clerk is unreachable', async () => {
    const h = harness();
    const res = await h.start({ error: new ClerkUnavailable('certificate verify failed') });
    assert.equal(res.status, 502);
    assert.equal(res.body.error, 'clerk_unreachable');
    assert.equal(h.outbox().length, 0);
  });

  it('refuses an unverified email and mails nothing', async () => {
    const h = harness();
    const res = await h.start({ user: clerkUser({ email_verified: false }) });
    assert.equal(res.status, 403);
    assert.equal(res.body.error, 'email_not_verified');
    assert.equal(h.outbox().length, 0);
    assert.equal(await pendingRow(), undefined);
  });

  it('is inert without a Clerk key', async () => {
    const h = harness({ clerkSecretKey: '' });
    const res = await h.start();
    assert.equal(res.status, 503);
    assert.equal(res.body.error, 'clerk_not_configured');
    assert.equal(h.fetchCalls(), 0);
    assert.equal(h.outbox().length, 0);
  });

  it('does not mail again inside the cooldown', async () => {
    const h = harness();
    await h.start();
    const before = (await pendingRow()).token_hash;
    const res = await h.start();
    assert.equal(res.status, 200);
    assert.equal(res.body.status, 'already_sent');
    assert.ok(res.body.retry_after > 0);
    assert.equal(h.outbox().length, 1);
    assert.equal((await pendingRow()).token_hash, before);
  });

  it('rotates the token after the cooldown, killing the old link', async () => {
    const h = harness();
    await h.start();
    const oldToken = TOKEN_RE.exec(h.outbox()[0].text)[1];
    await pool.query("UPDATE accounts_pendingregistration SET last_sent_at = now() - interval '5 minutes'");
    const res = await h.start();
    assert.equal(res.body.status, 'email_sent');
    assert.equal(h.outbox().length, 2);
    const newToken = TOKEN_RE.exec(h.outbox()[1].text)[1];
    assert.notEqual(oldToken, newToken);
    assert.equal((await h.validate(oldToken)).status, 404);
    assert.equal((await h.validate(newToken)).status, 200);
    assert.equal((await pendingRow()).sent_count, 2);
  });

  it('stops mailing at the send cap', async () => {
    const h = harness();
    await makePending({ sent_count: 99, last_sent_at: new Date(Date.now() - 5 * 60_000) });
    const res = await h.start();
    assert.equal(res.status, 429);
    assert.equal(res.body.error, 'too_many_sends');
    assert.equal(h.outbox().length, 0);
  });

  it('applies the global hourly cap before calling Clerk', async () => {
    const h = harness({ onboardingGlobalHourlyLimit: 1 });
    await makePending();
    const res = await h.start();
    assert.equal(res.status, 429);
    assert.equal(res.body.error, 'rate_limited');
    assert.equal(h.fetchCalls(), 0);
    assert.equal(h.outbox().length, 0);
  });

  it('applies the per-IP hourly cap', async () => {
    const h = harness({ onboardingIpHourlyLimit: 1 });
    await makePending({ request_ip: '203.0.113.7' });
    const res = await h.start({ headers: { 'X-Forwarded-For': '203.0.113.7' } });
    assert.equal(res.status, 429);
    assert.equal(h.outbox().length, 0);
  });

  it('does not use up the cooldown or send cap when sending fails', async () => {
    const h = harness();
    const realSend = h.deps.mailer.send.bind(h.deps.mailer);
    h.deps.mailer.send = async () => { throw new Error('provider down'); };
    const originalError = console.error;
    console.error = () => {};
    let res;
    try { res = await h.start(); } finally { console.error = originalError; }
    assert.equal(res.status, 502);
    assert.equal(res.body.error, 'email_send_failed');
    assert.equal(await pendingRow(), undefined);
    h.deps.mailer.send = realSend;
    const retry = await h.start();
    assert.equal(retry.status, 200);
    assert.equal(retry.body.status, 'email_sent');
    assert.equal(h.outbox().length, 1);
  });

  it('answers already_sent, not 500, when a parallel first request wins the insert', async () => {
    const h = harness();
    // Simulate the race: another request stores its row while this one is mailing.
    h.deps.mailer.send = async () => { await makePending({ token_hash: hash('the-winner') }); return 1; };
    const res = await h.start();
    assert.equal(res.status, 200);
    assert.equal(res.body.status, 'already_sent');
  });

  it('refuses to start again after completion', async () => {
    const h = harness();
    await makePending({ completed_at: new Date() });
    const res = await h.start();
    assert.equal(res.status, 409);
    assert.equal(res.body.error, 'already_registered');
    assert.equal(h.fetchCalls(), 0);
  });

  it('answers 409 for a Clerk id already linked to an account', async () => {
    const h = harness();
    await makeUser({ username: 'existing', email: 'other@example.com', country: 'Japan', clerk_user_id: CLERK_ID });
    const res = await h.start();
    assert.equal(res.status, 409);
    assert.equal(h.outbox().length, 0);
  });
});

describe('onboarding/validate-token', () => {
  it('answers 400 without a token', async () => {
    assert.equal((await harness().validate()).status, 400);
  });

  it('answers 404 for an unknown token', async () => {
    assert.equal((await harness().validate('nope')).status, 404);
  });

  it('returns the profile for a valid token without creating an account', async () => {
    const h = harness();
    await makePending();
    const res = await h.validate(RAW_TOKEN);
    assert.equal(res.status, 200);
    assert.equal(res.body.status, 'valid');
    assert.equal(res.body.email, EMAIL);
    assert.equal(res.body.first_name, 'Kaito');
    assert.equal(await userCount(), 0);
  });

  it('answers 410 for an expired token', async () => {
    const h = harness();
    await makePending({ expires_at: new Date(Date.now() - 60_000) });
    const res = await h.validate(RAW_TOKEN);
    assert.equal(res.status, 410);
    assert.equal(res.body.error, 'token_expired');
  });

  it('leaves the token usable after validating twice', async () => {
    const h = harness();
    await makePending();
    for (let i = 0; i < 2; i += 1) assert.equal((await h.validate(RAW_TOKEN)).status, 200);
    assert.equal((await pendingRow()).completed_at, null);
  });

  it('answers 409 for a completed token', async () => {
    const h = harness();
    await makePending({ completed_at: new Date() });
    const res = await h.validate(RAW_TOKEN);
    assert.equal(res.status, 409);
    assert.equal(res.body.error, 'already_completed');
  });
});

describe('onboarding/complete', () => {
  it('creates the account and returns a token pair', async () => {
    const h = harness();
    await makePending();
    const res = await h.complete();
    assert.equal(res.status, 200);
    assert.ok(res.body.access);
    assert.ok(res.body.refresh);
    assert.equal(res.body.user.username, 'kaito');
    assert.equal(res.body.user.country, 'Japan');
    const user = (await pool.query('SELECT * FROM accounts_user WHERE clerk_user_id = $1', [CLERK_ID])).rows[0];
    assert.equal(user.email, EMAIL);
    assert.equal(user.country, 'Japan');
    assert.equal(user.date_of_birth, '1995-04-12');
    assert.equal(user.first_name, 'Kaito');
    assert.equal(hasUsablePassword(user.password), false);
    assert.ok((await pendingRow()).completed_at);
  });

  it('still sends the welcome email', async () => {
    const h = harness();
    await makePending();
    await h.complete();
    assert.equal(h.outbox().length, 1);
    assert.ok(h.outbox()[0].subject.includes('Welcome'));
  });

  it('creates an account that cannot log in with a password', async () => {
    const h = harness();
    await makePending();
    await h.complete();
    const res = await request(h.app).post('/api/auth/login/').send({ username: 'kaito', password: 'anything' });
    assert.equal(res.status, 401);
  });

  it('answers 409 to a replayed link and creates nothing more', async () => {
    const h = harness();
    await makePending();
    await h.complete();
    const res = await h.complete();
    assert.equal(res.status, 409);
    assert.equal(res.body.error, 'already_completed');
    assert.equal(await userCount(), 1);
  });

  it('answers 404 for an unknown token', async () => {
    assert.equal((await harness().complete({ token: 'nope' })).status, 404);
  });

  it('answers 410 for an expired token', async () => {
    const h = harness();
    await makePending({ expires_at: new Date(Date.now() - 60_000) });
    assert.equal((await h.complete()).status, 410);
  });

  it('answers 400 for an unknown country', async () => {
    const h = harness();
    await makePending();
    const res = await h.complete({ country: 'Atlantis' });
    assert.equal(res.status, 400);
    assert.deepEqual(res.body.country, ['Unknown country.']);
    assert.equal(await userCount(), 0);
  });

  it('does not accept ISO codes as country names', async () => {
    const h = harness();
    await makePending();
    assert.equal((await h.complete({ country: 'JP' })).status, 400);
  });

  it('answers 400 for a future date of birth', async () => {
    const h = harness();
    await makePending();
    const res = await h.complete({ date_of_birth: todayUtc(1) });
    assert.equal(res.status, 400);
    assert.ok(res.body.date_of_birth);
  });

  it("accepts today's date (there is no age gate)", async () => {
    const h = harness();
    await makePending();
    assert.equal((await h.complete({ date_of_birth: todayUtc() })).status, 200);
  });

  it('answers 400 for a date of birth before 1900', async () => {
    const h = harness();
    await makePending();
    assert.equal((await h.complete({ date_of_birth: '0202-01-01' })).status, 400);
  });

  it('answers 400 for a malformed date of birth', async () => {
    const h = harness();
    await makePending();
    assert.equal((await h.complete({ date_of_birth: 'not-a-date' })).status, 400);
  });

  it('answers 409, not 500, when the email belongs to an older account', async () => {
    const h = harness();
    await makeUser({ username: 'legacy', email: EMAIL, country: 'Japan' }, 'Password123!');
    await makePending();
    const res = await h.complete();
    assert.equal(res.status, 409);
    assert.equal(res.body.error, 'already_registered');
    assert.equal(await userCount(), 1);
  });

  it('adds a suffix when the username is taken', async () => {
    const h = harness();
    await makeUser({ username: 'kaito', email: 'someone@example.com', country: 'Japan' }, 'Password123!');
    await makePending();
    const res = await h.complete();
    assert.equal(res.status, 200);
    assert.equal(res.body.user.username, 'kaito_1');
  });

  it('falls back to the email local part when there is no Clerk username', async () => {
    const h = harness();
    await makePending({ clerk_username: '' });
    const res = await h.complete();
    assert.equal(res.status, 200);
    assert.equal(res.body.user.username, 'operator');
  });
});

describe('Stale website tokens do not block anonymous routes', () => {
  it('start ignores a stale Bearer token', async () => {
    const h = harness();
    assert.equal((await h.start({ headers: { Authorization: STALE } })).status, 200);
  });

  it('validate-token ignores a stale Bearer token', async () => {
    const h = harness();
    await makePending();
    assert.equal((await h.validate(RAW_TOKEN, { Authorization: STALE })).status, 200);
  });

  it('complete ignores a stale Bearer token', async () => {
    const h = harness();
    await makePending();
    assert.equal((await h.complete({ headers: { Authorization: STALE } })).status, 200);
  });

  it('the country list ignores a stale Bearer token', async () => {
    const { app } = harness();
    assert.equal((await request(app).get('/api/auth/countries/').set('Authorization', STALE)).status, 200);
  });
});
