/**
 * Ported from Django `accounts/tests/test_email.py` (7 tests): the welcome mail and the 2FA code
 * mails. Messages are read from the in-memory outbox; nothing is sent.
 */
import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import request from 'supertest';

import { sendWelcomeEmail, welcomeEmail } from '../src/lib/emails.js';
import { issueTokenPair } from '../src/lib/tokens.js';
import { baseConfig, makeApp, makeUser, pool, useDatabase } from './helpers.js';

useDatabase();

const register = (app, username) => request(app).post('/api/auth/register/')
  .send({ username, email: `${username}@example.com`, password: 'Password123!', country: 'Japan' });

describe('Welcome email', () => {
  it('is sent when an account is created', async () => {
    const { app, outbox } = makeApp();
    await register(app, 'mailfrog');
    assert.equal(outbox().length, 1);
    const message = outbox()[0];
    assert.deepEqual(message.to, ['mailfrog@example.com']);
    assert.equal(message.subject, 'SYS_INIT: Welcome to Frog Jumper, mailfrog');
    assert.ok(message.text.includes('Operator mailfrog'));
    assert.ok(message.text.includes('LEVEL 1 OPERATOR'));
    // The template upper-cases the username.
    assert.ok(message.html.includes('OPERATOR_MAILFROG'));
  });

  it('uses the configured sender', async () => {
    const { app, outbox } = makeApp();
    await register(app, 'fromfrog');
    assert.equal(outbox()[0].from, baseConfig.defaultFromEmail);
  });

  it('is not sent when the account has no address', async () => {
    assert.equal(welcomeEmail({ username: 'noaddressfrog', email: '' }, 'x@example.com'), null);
    const { deps, outbox } = makeApp();
    await sendWelcomeEmail(deps.mailer, baseConfig, { username: 'noaddressfrog', email: '' });
    assert.equal(outbox().length, 0);
  });

  it('never touches the network during registration', async () => {
    const realFetch = globalThis.fetch;
    let calls = 0;
    globalThis.fetch = async () => { calls += 1; throw new Error('network used in a test'); };
    try {
      const { app, outbox } = makeApp();
      await register(app, 'offlinefrog');
      assert.equal(outbox().length, 1);
    } finally {
      globalThis.fetch = realFetch;
    }
    assert.equal(calls, 0);
  });
});

describe('2FA code email', () => {
  const setup = async () => {
    const ctx = makeApp();
    const user = await makeUser({ username: 'twofafrog', email: 'twofafrog@example.com', country: 'Canada',
      is_2fa_enabled: true }, 'CorrectPassword123!');
    return { ...ctx, user };
  };

  it('is sent at login when 2FA is on', async () => {
    const { app, outbox, user } = await setup();
    const res = await request(app).post('/api/auth/login/').send({ username: 'twofafrog', password: 'CorrectPassword123!' });
    assert.equal(res.status, 200);
    assert.equal(res.body.status, '2fa_required');
    assert.equal(outbox().length, 1);
    const message = outbox()[0];
    assert.deepEqual(message.to, ['twofafrog@example.com']);
    assert.equal(message.from, baseConfig.defaultFromEmail);
    assert.ok(message.subject.includes('Login Code'));
    const stored = (await pool.query("SELECT code FROM accounts_twofactorcode WHERE user_id = $1 AND purpose = 'login'", [user.id])).rows[0];
    assert.ok(message.text.includes(stored.code));
  });

  it('is sent when turning 2FA on', async () => {
    const { app, outbox, user } = await setup();
    const { access } = await issueTokenPair(pool, baseConfig, user.id);
    const res = await request(app).post('/api/auth/2fa/enable/').set('Authorization', `Bearer ${access}`);
    assert.equal(res.status, 200);
    assert.equal(outbox().length, 1);
    const message = outbox()[0];
    assert.deepEqual(message.to, ['twofafrog@example.com']);
    assert.equal(message.from, baseConfig.defaultFromEmail);
    assert.ok(message.subject.includes('2FA Setup'));
    const stored = (await pool.query("SELECT code FROM accounts_twofactorcode WHERE user_id = $1 AND purpose = 'opt_in'", [user.id])).rows[0];
    assert.ok(message.text.includes(stored.code));
  });

  it('is not sent at login when 2FA is off', async () => {
    const { app, outbox, user } = await setup();
    await pool.query('UPDATE accounts_user SET is_2fa_enabled = false WHERE id = $1', [user.id]);
    const res = await request(app).post('/api/auth/login/').send({ username: 'twofafrog', password: 'CorrectPassword123!' });
    assert.equal(res.status, 200);
    assert.ok(res.body.access);
    assert.equal(outbox().length, 0);
  });
});
