/**
 * Ported from Django `accounts/tests/test_views.py` (10 tests), `test_serializers.py` (4) and
 * `test_models.py` (7): registration, login, profile, the country lists, field validation and
 * the table rules (defaults, unique keys).
 */
import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import request from 'supertest';

import { checkPassword } from '../src/lib/djangoHash.js';
import { profileOf, validateRegistration } from '../src/lib/users.js';
import { makeApp, makeUser, pool, useDatabase } from './helpers.js';

useDatabase();

describe('RegisterView', () => {
  it('registers a user', async () => {
    const { app } = makeApp();
    const res = await request(app).post('/api/auth/register/').send({
      username: 'cyberfrog', email: 'cyberfrog@example.com', password: 'SecurePassword123!',
      country: 'Palestine (State of)', first_name: 'Cyber', last_name: 'Frog',
    });
    assert.equal(res.status, 201);
    assert.equal(res.body.username, 'cyberfrog');
    assert.equal(res.body.country, 'Palestine (State of)');
    assert.equal(res.body.password, undefined);
    assert.equal((await pool.query("SELECT 1 FROM accounts_user WHERE username = 'cyberfrog'")).rowCount, 1);
  });

  it('refuses a registration with no country', async () => {
    const { app } = makeApp();
    const res = await request(app).post('/api/auth/register/')
      .send({ username: 'nocountryfrog', email: 'nocountry@example.com', password: 'SecurePassword123!' });
    assert.equal(res.status, 400);
    assert.ok(res.body.country);
  });

  it('refuses a duplicate username', async () => {
    await makeUser({ username: 'existingfrog', email: 'existing1@example.com', country: 'Japan' }, 'Password123!');
    const { app } = makeApp();
    const res = await request(app).post('/api/auth/register/')
      .send({ username: 'existingfrog', email: 'existing2@example.com', password: 'Password123!', country: 'Japan' });
    assert.equal(res.status, 400);
    assert.deepEqual(res.body.username, ['A user with that username already exists.']);
  });
});

describe('LoginView', () => {
  it('signs in with the right password', async () => {
    await makeUser({ username: 'loginfrog', email: 'loginfrog@example.com', country: 'Canada' }, 'CorrectPassword123!');
    const { app } = makeApp();
    const res = await request(app).post('/api/auth/login/').send({ username: 'loginfrog', password: 'CorrectPassword123!' });
    assert.equal(res.status, 200);
    assert.ok(res.body.access);
    assert.ok(res.body.refresh);
    assert.equal(res.body.user.username, 'loginfrog');
    assert.equal(res.body.user.country, 'Canada');
  });

  it('refuses a wrong password', async () => {
    await makeUser({ username: 'loginfrog', email: 'loginfrog@example.com', country: 'Canada' }, 'CorrectPassword123!');
    const { app } = makeApp();
    const res = await request(app).post('/api/auth/login/').send({ username: 'loginfrog', password: 'WrongPassword999!' });
    assert.equal(res.status, 401);
  });
});

describe('ProfileView', () => {
  it('returns the signed-in profile', async () => {
    await makeUser({ username: 'profiletester', email: 'profiletester@example.com', country: 'Germany' }, 'Password123!');
    const { app } = makeApp();
    const login = await request(app).post('/api/auth/login/').send({ username: 'profiletester', password: 'Password123!' });
    const res = await request(app).get('/api/auth/profile/').set('Authorization', `Bearer ${login.body.access}`);
    assert.equal(res.status, 200);
    assert.equal(res.body.username, 'profiletester');
    assert.equal(res.body.country, 'Germany');
  });

  it('refuses an anonymous request with 401', async () => {
    const { app } = makeApp();
    const res = await request(app).get('/api/auth/profile/');
    assert.equal(res.status, 401);
    assert.equal(res.body.detail, 'Authentication credentials were not provided.');
  });
});

describe('Country lists', () => {
  it('returns the full list', async () => {
    const { app } = makeApp();
    const res = await request(app).get('/api/auth/countries/');
    assert.equal(res.status, 200);
    assert.ok(Array.isArray(res.body) && res.body.length > 0);
  });

  it('returns only cca3, name.common and flag in the basic list', async () => {
    const { app } = makeApp();
    const res = await request(app).get('/api/auth/countries/basic/');
    assert.equal(res.status, 200);
    assert.deepEqual(Object.keys(res.body[0]).sort(), ['cca3', 'flag', 'name']);
    assert.deepEqual(Object.keys(res.body[0].name), ['common']);
    const full = new Map((await request(app).get('/api/auth/countries/')).body.map((c) => [c.cca3, c]));
    for (const country of res.body) assert.equal(country.flag, full.get(country.cca3).flag ?? '');
  });

  it('covers exactly the same countries as the full list', async () => {
    const { app } = makeApp();
    const full = new Set((await request(app).get('/api/auth/countries/')).body.map((c) => c.name.common));
    const basic = new Set((await request(app).get('/api/auth/countries/basic/')).body.map((c) => c.name.common));
    assert.deepEqual(basic, full);
  });
});

describe('Registration validation (UserSerializer)', () => {
  it('accepts a valid body and stores a checkable password hash', async () => {
    const fields = await validateRegistration(pool, {
      username: 'newfrog', email: 'newfrog@example.com', password: 'StrongPassword123!',
      country: 'Palestine (State of)', first_name: 'Ribbit', last_name: 'Jumper',
    });
    const user = await makeUser(fields, fields.password);
    assert.equal(user.username, 'newfrog');
    assert.equal(user.email, 'newfrog@example.com');
    assert.equal(user.country, 'Palestine (State of)');
    assert.ok(await checkPassword('StrongPassword123!', user.password));
    assert.equal(profileOf(user).password, undefined);
  });

  it('requires a country', async () => {
    await assert.rejects(validateRegistration(pool, { username: 'a', email: 'a@example.com', password: 'x' }),
      (e) => e.status === 400 && e.body.country[0] === 'This field is required.');
  });

  it('refuses a blank country', async () => {
    await assert.rejects(validateRegistration(pool, { username: 'a', email: 'a@example.com', password: 'x', country: '' }),
      (e) => e.status === 400 && e.body.country[0] === 'This field may not be blank.');
  });
});

describe('Profile shape (UserProfileSerializer)', () => {
  it('has the profile fields and no password', async () => {
    const user = await makeUser({ username: 'profilefrog', email: 'profilefrog@example.com', country: 'Germany',
      phone_number: '+49123456789' }, 'Password123!');
    const data = profileOf(user);
    assert.equal(data.username, 'profilefrog');
    assert.equal(data.email, 'profilefrog@example.com');
    assert.equal(data.country, 'Germany');
    assert.equal(data.phone_number, '+49123456789');
    assert.equal(data.is_2fa_enabled, false);
    assert.equal(data.password, undefined);
    assert.equal(typeof data.id, 'number');
  });
});

describe('Tables (Django models)', () => {
  it('creates a user with defaults and a working password check', async () => {
    const user = await makeUser({ username: 'frogtester', email: 'frogtester@example.com',
      country: 'Palestine (State of)', phone_number: '+1234567890' }, 'SecretPassword123!');
    assert.equal(user.phone_number, '+1234567890');
    assert.equal(user.is_2fa_enabled, false);
    assert.equal(user.is_active, true);
    assert.ok(await checkPassword('SecretPassword123!', user.password));
    assert.equal(await checkPassword('WrongPassword!', user.password), false);
  });

  it('lower-cases only the email domain, as Django did', async () => {
    const user = await makeUser({ username: 'casefrog', email: 'Case.Frog@EXAMPLE.COM', country: 'Japan' });
    assert.equal(user.email, 'Case.Frog@example.com');
  });

  it('gives a browser session a unique id and no approval by default', async () => {
    const user = await makeUser({ username: 'sessionuser', email: 'sessionuser@example.com', country: 'Canada' });
    const row = (await pool.query(
      `INSERT INTO accounts_browsersession (session_id, created_at, is_authorized, user_id, secret_hash, intent, locale)
       VALUES (gen_random_uuid(), now(), false, $1, '', 'login', 'en-US') RETURNING *`, [user.id])).rows[0];
    assert.ok(row.session_id);
    assert.equal(row.is_authorized, false);
    await assert.rejects(pool.query(
      `INSERT INTO accounts_browsersession (session_id, created_at, is_authorized, secret_hash, intent, locale)
       VALUES ($1, now(), false, '', 'login', 'en-US')`, [row.session_id]), { code: '23505' });
  });

  it('stores a 2FA code unverified with no attempts', async () => {
    const user = await makeUser({ username: 'twofauser', email: 'twofauser@example.com', country: 'Japan' });
    const row = (await pool.query(
      `INSERT INTO accounts_twofactorcode (user_id, code, created_at, is_verified, purpose)
       VALUES ($1, '123456', now(), false, 'login') RETURNING *`, [user.id])).rows[0];
    assert.equal(row.code, '123456');
    assert.equal(row.is_verified, false);
    assert.equal(row.attempts, 0);
  });

  it('gives a pending registration its defaults', async () => {
    const row = (await pool.query(
      `INSERT INTO accounts_pendingregistration (clerk_user_id, email, token_hash, last_sent_at, expires_at)
       VALUES ('user_abc123', 'operator@example.com', $1, now(), now() + interval '24 hours') RETURNING *`,
      ['a'.repeat(64)])).rows[0];
    assert.equal(row.sent_count, 0);
    assert.equal(row.completed_at, null);
    assert.equal(row.request_ip, null);
    assert.equal(row.first_name, '');
    assert.equal(row.clerk_username, '');
  });

  it('allows one pending registration per Clerk id', async () => {
    const insert = (id, hash) => pool.query(
      `INSERT INTO accounts_pendingregistration (clerk_user_id, email, token_hash, last_sent_at, expires_at)
       VALUES ($1, 'operator@example.com', $2, now(), now() + interval '24 hours')`, [id, hash]);
    await insert('user_abc123', 'a'.repeat(64));
    await assert.rejects(insert('user_abc123', 'b'.repeat(64)), { code: '23505' });
  });

  it('lets two pending registrations share an email (the clash is settled at completion)', async () => {
    const insert = (id, hash) => pool.query(
      `INSERT INTO accounts_pendingregistration (clerk_user_id, email, token_hash, last_sent_at, expires_at)
       VALUES ($1, 'operator@example.com', $2, now(), now() + interval '24 hours')`, [id, hash]);
    await insert('user_abc123', 'a'.repeat(64));
    await insert('user_def456', 'c'.repeat(64));
    const count = await pool.query("SELECT count(*)::int AS n FROM accounts_pendingregistration WHERE email = 'operator@example.com'");
    assert.equal(count.rows[0].n, 2);
  });
});
