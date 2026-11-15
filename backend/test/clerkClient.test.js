/**
 * Ported from Django `accounts/tests/test_clerk_client.py` (20 tests). The HTTP call is replaced,
 * so each test checks the request that would go on the wire (URL, headers, timeout) and how
 * answers and failures are mapped. No test reaches Clerk. No database is needed.
 */
import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { loadConfig, withOverrides } from '../src/config.js';
import {
  ClerkAPIError, ClerkUnavailable, ClerkUserNotFound, fetchClerkUser, isValidUserId,
} from '../src/lib/clerk.js';
import { USER_AGENT } from '../src/lib/mail.js';

const KEY = 'sk_test_key_not_real';
const USER_ID = 'user_2abcDEF123';
const API_URL = `https://api.clerk.com/v1/users/${USER_ID}`;
const config = withOverrides(loadConfig({}, { requireSigningKey: false }), { clerkSecretKey: KEY });
const quiet = { warn: () => {} };

const payload = (overrides = {}) => ({
  id: USER_ID, primary_email_address_id: 'idn_1', username: 'kaito', first_name: 'Kaito', last_name: 'Kurogane',
  email_addresses: [{ id: 'idn_1', email_address: 'kaito@example.com', verification: { status: 'verified' } }],
  ...overrides,
});

/** A fake HTTP call that records its arguments and answers with `answer()`. */
function fakeRequest(answer) {
  const calls = [];
  const fn = async (url, init, timeoutMs) => {
    calls.push({ url, init, timeoutMs });
    return answer();
  };
  fn.calls = calls;
  return fn;
}
const json = (body, status = 200) => () => new Response(JSON.stringify(body), { status });
const fetchWith = (body, cfg = config) => fetchClerkUser(USER_ID, cfg, { request: fakeRequest(json(body)), logger: quiet });

describe('Request shape', () => {
  it('sends the right URL, method, key, timeout and User-Agent', async () => {
    const request = fakeRequest(json(payload()));
    await fetchClerkUser(USER_ID, withOverrides(config, { clerkTimeoutMs: 7000 }), { request, logger: quiet });
    const { url, init, timeoutMs } = request.calls[0];
    assert.equal(url, API_URL);
    assert.equal(init.method, 'GET');
    assert.equal(init.headers.Authorization, `Bearer ${KEY}`);
    assert.equal(timeoutMs, 7000);
    // Cloudflare in front of api.clerk.com refuses some default client agents with
    // 403 "error code: 1010", which looks like a bad key and is not.
    assert.equal(init.headers['User-Agent'], USER_AGENT);
  });
});

describe('Email resolution', () => {
  it('prefers the primary address', async () => {
    const user = await fetchWith(payload());
    assert.equal(user.email, 'kaito@example.com');
    assert.equal(user.email_verified, true);
    assert.equal(user.username, 'kaito');
    assert.equal(user.first_name, 'Kaito');
  });

  it('falls back to a verified address when there is no primary', async () => {
    const user = await fetchWith(payload({ primary_email_address_id: null, email_addresses: [
      { id: 'idn_1', email_address: 'old@example.com', verification: { status: 'unverified' } },
      { id: 'idn_2', email_address: 'new@example.com', verification: { status: 'verified' } }] }));
    assert.equal(user.email, 'new@example.com');
    assert.equal(user.email_verified, true);
  });

  it('falls back to the first address when none is verified', async () => {
    const user = await fetchWith(payload({ primary_email_address_id: null, email_addresses: [
      { id: 'idn_1', email_address: 'first@example.com', verification: { status: 'unverified' } },
      { id: 'idn_2', email_address: 'second@example.com', verification: { status: 'unverified' } }] }));
    assert.equal(user.email, 'first@example.com');
    assert.equal(user.email_verified, false);
  });

  it('reports an unverified primary as unverified', async () => {
    const user = await fetchWith(payload({ email_addresses: [
      { id: 'idn_1', email_address: 'kaito@example.com', verification: { status: 'unverified' } }] }));
    assert.equal(user.email_verified, false);
  });

  it('counts a missing verification block as unverified', async () => {
    const user = await fetchWith(payload({ email_addresses: [{ id: 'idn_1', email_address: 'kaito@example.com' }] }));
    assert.equal(user.email_verified, false);
  });

  it('turns null profile fields into empty strings', async () => {
    const user = await fetchWith(payload({ username: null, first_name: null, last_name: null }));
    assert.equal(user.username, '');
    assert.equal(user.first_name, '');
    assert.equal(user.last_name, '');
  });

  it('gives an empty email when there are no addresses', async () => {
    const user = await fetchWith(payload({ email_addresses: [], primary_email_address_id: null }));
    assert.equal(user.email, '');
    assert.equal(user.email_verified, false);
  });
});

describe('Error mapping', () => {
  const failWith = (answer) => fetchClerkUser(USER_ID, config, { request: fakeRequest(answer), logger: quiet });

  it('maps 404 to user not found', async () => {
    await assert.rejects(failWith(json({ errors: [{ message: 'not found' }] }, 404)), ClerkUserNotFound);
  });

  it('does not echo the response body for other errors', async () => {
    await assert.rejects(failWith(json({ detail: 'leak-me' }, 500)),
      (e) => e instanceof ClerkAPIError && !e.message.includes('leak-me') && e.message.includes('500'));
  });

  it('never puts the key in an error message', async () => {
    await assert.rejects(failWith(json({}, 500)), (e) => e instanceof ClerkAPIError && !e.message.includes(KEY));
  });

  it('maps a connection error to unavailable', async () => {
    await assert.rejects(failWith(() => { throw new TypeError('fetch failed: certificate verify failed'); }), ClerkUnavailable);
  });

  it('maps a timeout to unavailable', async () => {
    await assert.rejects(failWith(() => { throw new DOMException('timed out', 'TimeoutError'); }), ClerkUnavailable);
  });

  it('maps a non-JSON body to an API error', async () => {
    await assert.rejects(failWith(() => new Response('<html>gateway error</html>', { status: 200 })), ClerkAPIError);
  });

  it('maps a JSON list body to an API error', async () => {
    await assert.rejects(failWith(json([1, 2])), ClerkAPIError);
  });
});

describe('Configuration', () => {
  it('raises without a key and makes no network call', async () => {
    const request = fakeRequest(json(payload()));
    await assert.rejects(fetchClerkUser(USER_ID, withOverrides(config, { clerkSecretKey: '' }), { request }), ClerkAPIError);
    assert.equal(request.calls.length, 0);
  });

  it('falls back to a 10-second timeout when the setting is not a number', () => {
    const cfg = loadConfig({ CLERK_TIMEOUT: 'not-a-number' }, { requireSigningKey: false });
    assert.equal(cfg.clerkTimeoutMs, 10_000);
  });
});

describe('User id validation', () => {
  const BAD_IDS = ['', 'user_', 'user_a/../../x', 'user_a?x=1', `user_${'a'.repeat(61)}`, 'notuser_abc', 'user_a b', null, 42];

  it('never sends a malformed id to the network', async () => {
    const request = fakeRequest(json(payload()));
    for (const bad of BAD_IDS) {
      await assert.rejects(fetchClerkUser(bad, config, { request }), ClerkUserNotFound);
    }
    assert.equal(request.calls.length, 0);
  });

  it('rejects the same ids in the predicate', () => {
    for (const bad of BAD_IDS) assert.equal(isValidUserId(bad), false);
  });

  it('accepts a well-formed id', () => {
    assert.equal(isValidUserId(USER_ID), true);
  });
});
