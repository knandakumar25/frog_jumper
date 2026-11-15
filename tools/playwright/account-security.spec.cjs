/** Real API contracts for password accounts, with mail captured by the isolated test server. */
const { test, expect } = require('@playwright/test');
const backend = 'http://backend:8002';
test.setTimeout(120000);

test('password login, mailed 2FA lifecycle, email change and refresh revocation', async ({ request }) => {
  const username = `browsercheck${Date.now()}`;
  const email = `${username}@example.com`;
  const password = 'BrowserCheck-Only#83Pass';
  const call = (path, data, access, method = 'post') => request[method](backend + '/api/auth/' + path, {
    data, headers: access ? { Authorization: `Bearer ${access}` } : {},
  });
  const code = async address => {
    const messages = await (await request.get(backend + '/__test__/mail')).json();
    return [...messages].reverse().find(m => m.to.includes(address) && /code.*\d{6}/i.test(m.body)).body.match(/\b\d{6}\b/)[0];
  };
  const expire = purpose => request.post(backend + '/__test__/expire-codes', { data: { username, purpose } });
  const wrong = value => value === '000000' ? '111111' : '000000';
  expect((await call('register/', { username, email, password, country: 'Japan' })).status()).toBe(201);
  expect((await call('login/', { username, password: password + 'wrong' })).status()).toBe(401);
  let session = await (await call('login/', { username, password })).json();
  expect(session.user.username).toBe(username);
  expect((await call('2fa/enable/', {}, session.access)).status()).toBe(200);
  let value = await code(email);
  expect((await call('2fa/verify-enable/', { code: wrong(value) }, session.access)).status()).toBe(400);
  expect((await expire('opt_in')).ok()).toBeTruthy();
  expect((await call('2fa/verify-enable/', { code: value }, session.access)).status()).toBe(400);
  expect((await call('2fa/enable/', {}, session.access)).status()).toBe(200);
  value = await code(email);
  expect((await call('2fa/verify-enable/', { code: value }, session.access)).status()).toBe(200);
  expect((await call('2fa/verify-enable/', { code: value }, session.access)).status()).toBe(400);
  let challenge = await (await call('login/', { username, password })).json();
  expect(challenge.status).toBe('2fa_required');
  value = await code(email);
  const verify = otp => call('2fa/verify-login/', { login_session_id: challenge.login_session_id, code: otp });
  expect((await verify(wrong(value))).status()).toBe(400);
  expect((await expire('login')).ok()).toBeTruthy();
  expect((await verify(value)).status()).toBe(400);
  challenge = await (await call('login/', { username, password })).json();
  value = await code(email);
  const verified = await verify(value);
  expect(verified.status()).toBe(200);
  session = await verified.json();
  expect((await verify(value)).status()).toBe(400);
  // Both callers spend the same refresh token: exactly one can rotate it.
  const rotated = await Promise.all([call('token/refresh/', { refresh: session.refresh }), call('token/refresh/', { refresh: session.refresh })]);
  expect(rotated.map(r => r.status()).sort()).toEqual([200, 401]);
  // Replay revokes the winning rotation's family. Start a separate live session
  // so the disable assertion proves revoke-all, rather than that earlier replay.
  challenge = await (await call('login/', { username, password })).json();
  value = await code(email);
  session = await (await verify(value)).json();
  expect((await call('2fa/disable-request/', {}, session.access)).status()).toBe(200);
  value = await code(email);
  expect((await call('2fa/disable/', { code: wrong(value) }, session.access)).status()).toBe(400);
  expect((await expire('opt_out')).ok()).toBeTruthy();
  expect((await call('2fa/disable/', { code: value }, session.access)).status()).toBe(400);
  expect((await call('2fa/disable-request/', {}, session.access)).status()).toBe(200);
  value = await code(email);
  expect((await call('2fa/disable/', { code: value }, session.access)).status()).toBe(200);
  expect((await call('2fa/disable/', { code: value }, session.access)).status()).toBe(400);
  expect((await call('token/refresh/', { refresh: session.refresh })).status()).toBe(401);
  session = await (await call('login/', { username, password })).json();
  expect(session.user.is_2fa_enabled).toBe(false);
  const changed = `${username}changed@example.com`;
  expect((await call('profile/', { email: changed }, session.access, 'patch')).status()).toBe(400);
  expect((await call('profile/email-code/', {}, session.access)).status()).toBe(200);
  value = await code(email);
  expect((await call('profile/', { email: changed, email_code: wrong(value) }, session.access, 'patch')).status()).toBe(400);
  expect((await expire('email_change')).ok()).toBeTruthy();
  expect((await call('profile/', { email: changed, email_code: value }, session.access, 'patch')).status()).toBe(400);
  expect((await call('profile/email-code/', {}, session.access)).status()).toBe(200);
  value = await code(email);
  const updated = await call('profile/', { email: changed, email_code: value }, session.access, 'patch');
  expect(updated.status()).toBe(200);
  expect((await updated.json()).email).toBe(changed);
  expect((await call('profile/', { email, email_code: value }, session.access, 'patch')).status()).toBe(400);
  expect((await call('token/refresh/', { refresh: session.refresh })).status()).toBe(401);
  const notices = await (await request.get(backend + '/__test__/mail')).json();
  expect(notices.some(m => m.to.includes(email) && /turned off|disabled/i.test(m.body))).toBeTruthy();
  expect(notices.some(m => m.to.includes(email) && m.body.includes(changed))).toBeTruthy();
});
