/**
 * Ported from Django `core/tests` for the Resend mail backend (`test_mail_backend.py`, 14 tests).
 * The HTTP call is replaced, so each test checks the exact request Resend would get. No database.
 */
import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { MailConfigError, ResendAPIError, ResendTransport, USER_AGENT } from '../src/lib/mail.js';

const API_URL = 'https://api.resend.com/emails';
const KEY = 're_test_key_not_real';
const quiet = { info: () => {}, error: () => {} };

/** A fake HTTP call that records requests and answers with `answer()`. */
function fakeRequest(answer = () => new Response(JSON.stringify({ id: 'msg_123' }), { status: 200 })) {
  const calls = [];
  const fn = async (url, init, timeoutMs) => {
    calls.push({ url, init, timeoutMs, payload: JSON.parse(init.body) });
    return answer();
  };
  fn.calls = calls;
  return fn;
}

const fullMessage = () => ({
  subject: 'SYS_INIT: Welcome', text: 'plain text body', html: '<p>html body</p>',
  from: 'Axiom Force <welcome@example.com>', to: ['operator@example.com'],
  cc: ['cc@example.com'], reply_to: ['reply@example.com'],
});
const plainMessage = (extra = {}) => ({ subject: 'Plain', text: 'just text',
  from: 'Axiom Force <welcome@example.com>', to: ['operator@example.com'], ...extra });

describe('Payload', () => {
  it('builds the request and JSON body Resend expects', async () => {
    const request = fakeRequest();
    const sent = await new ResendTransport({ apiKey: KEY, request, logger: quiet }).sendMessages([fullMessage()]);
    assert.equal(sent, 1);
    assert.equal(request.calls.length, 1);
    const { url, init, timeoutMs, payload } = request.calls[0];
    assert.equal(url, API_URL);
    assert.equal(init.method, 'POST');
    assert.equal(init.headers.Authorization, `Bearer ${KEY}`);
    assert.equal(init.headers['Content-Type'], 'application/json');
    assert.equal(timeoutMs, 10_000);
    assert.equal(init.headers['User-Agent'], USER_AGENT);
    assert.equal(payload.from, 'Axiom Force <welcome@example.com>');
    assert.deepEqual(payload.to, ['operator@example.com']);
    assert.equal(payload.subject, 'SYS_INIT: Welcome');
    assert.equal(payload.text, 'plain text body');
    assert.equal(payload.html, '<p>html body</p>');
    assert.deepEqual(payload.cc, ['cc@example.com']);
    assert.deepEqual(payload.reply_to, ['reply@example.com']);
    // Empty lists are left out: Resend rejects [].
    assert.equal('bcc' in payload, false);
    assert.equal('attachments' in payload, false);
  });

  it('leaves out html when the message has none', async () => {
    const request = fakeRequest();
    await new ResendTransport({ apiKey: KEY, request, logger: quiet }).sendMessages([plainMessage()]);
    assert.equal('html' in request.calls[0].payload, false);
    assert.equal(request.calls[0].payload.text, 'just text');
  });

  it('lets a From header override the sender', async () => {
    const request = fakeRequest();
    await new ResendTransport({ apiKey: KEY, request, logger: quiet })
      .sendMessages([plainMessage({ headers: { From: 'Someone Else <other@example.com>' } })]);
    assert.equal(request.calls[0].payload.from, 'Someone Else <other@example.com>');
  });

  it('base64-encodes attachments', async () => {
    const request = fakeRequest();
    await new ResendTransport({ apiKey: KEY, request, logger: quiet }).sendMessages([plainMessage({
      attachments: [{ filename: 'notes.txt', content: Buffer.from('hello bytes'), contentType: 'text/plain' }] })]);
    assert.deepEqual(request.calls[0].payload.attachments,
      [{ filename: 'notes.txt', content: 'aGVsbG8gYnl0ZXM=', content_type: 'text/plain' }]);
  });

  it('logs the accepted message id', async () => {
    const lines = [];
    const request = fakeRequest(() => new Response(JSON.stringify({ id: 'abc-9' }), { status: 200 }));
    await new ResendTransport({ apiKey: KEY, request, logger: { info: (l) => lines.push(l), error: () => {} } })
      .sendMessages([fullMessage()]);
    assert.ok(lines.some((line) => line.includes('abc-9')));
  });

  it('reports how many messages were accepted', async () => {
    const request = fakeRequest();
    const sent = await new ResendTransport({ apiKey: KEY, request, logger: quiet }).sendMessages([fullMessage(), fullMessage()]);
    assert.equal(sent, 2);
    assert.equal(request.calls.length, 2);
  });
});

describe('Error handling', () => {
  it('turns an HTTP error into ResendAPIError carrying the body', async () => {
    const request = fakeRequest(() => new Response('{"message":"domain is not verified"}', { status: 403 }));
    await assert.rejects(new ResendTransport({ apiKey: KEY, request, logger: quiet }).sendMessages([plainMessage()]),
      (e) => e instanceof ResendAPIError && e.message.includes('403') && e.message.includes('domain is not verified'));
  });

  it('turns a connection error into ResendAPIError', async () => {
    const request = fakeRequest(() => { throw new TypeError('fetch failed: connection refused'); });
    await assert.rejects(new ResendTransport({ apiKey: KEY, request, logger: quiet }).sendMessages([plainMessage()]), ResendAPIError);
  });

  it('swallows errors when told to fail silently', async () => {
    const request = fakeRequest(() => new Response('{"name":"validation_error"}', { status: 403 }));
    const sent = await new ResendTransport({ apiKey: KEY, request, failSilently: true, logger: quiet }).sendMessages([plainMessage()]);
    assert.equal(sent, 0);
  });

  it('accepts a success answer whose body is not JSON', async () => {
    const request = fakeRequest(() => new Response('not json at all', { status: 200 }));
    const sent = await new ResendTransport({ apiKey: KEY, request, logger: quiet }).sendMessages([plainMessage()]);
    assert.equal(sent, 1);
  });
});

describe('Configuration', () => {
  it('can be built without a key', () => {
    assert.doesNotThrow(() => new ResendTransport({ apiKey: '' }));
  });

  it('keeps an empty key empty rather than falling back to settings', () => {
    assert.equal(new ResendTransport({ apiKey: '' }).apiKey, '');
  });

  it('refuses to send without a key and makes no request', async () => {
    const request = fakeRequest();
    await assert.rejects(new ResendTransport({ apiKey: '', request }).sendMessages([plainMessage()]), MailConfigError);
    assert.equal(request.calls.length, 0);
  });

  it('returns 0 for an empty list without a request', async () => {
    const request = fakeRequest();
    assert.equal(await new ResendTransport({ apiKey: KEY, request }).sendMessages([]), 0);
    assert.equal(request.calls.length, 0);
  });
});
