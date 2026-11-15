/**
 * Outgoing mail: one `Mailer` with three transports.
 *
 * - `resend`: the Resend HTTP API, ported from Django's `core/mail_backends.py`. Used when
 *   `RESEND_API_KEY` is set.
 * - `console`: prints each message; used when there is no key, so local development and tests
 *   never send real mail (Django's console backend).
 * - `memory`: keeps messages in `outbox` for tests and the end-to-end test server (Django's
 *   locmem backend and `mail.outbox`).
 *
 * A message is a plain object:
 * `{subject, text, from, to: [], cc?, bcc?, reply_to?, html?, headers?, attachments?}` where an
 * attachment is `{filename, content: Buffer | string, contentType}`.
 */

export const RESEND_API_URL = 'https://api.resend.com/emails';
// Cloudflare in front of api.resend.com refuses some default client User-Agents with
// HTTP 403 "error code: 1010" before Resend sees the request. Django hit this with urllib; the
// header is kept so every client identifies itself the same way.
export const USER_AGENT = 'AxiomForce-Mailer/1.0';

/** Resend refused or could not be reached. */
export class ResendAPIError extends Error {}
/** Sending was attempted with no API key. */
export class MailConfigError extends Error {}

/**
 * Default HTTP call: `fetch` with a timeout. Tests replace it to inspect the request.
 * @param {string} url
 * @param {RequestInit} init
 * @param {number} timeoutMs
 * @returns {Promise<Response>}
 */
export function timedFetch(url, init, timeoutMs) {
  return fetch(url, { ...init, signal: AbortSignal.timeout(timeoutMs) });
}

/**
 * Map a message onto Resend's JSON body. Empty lists are left out (Resend rejects `[]`), and a
 * `From` header overrides `from` (as Django's EmailMessage did).
 * @param {object} message
 * @returns {object}
 */
export function resendPayload(message) {
  const payload = {
    from: message.headers?.From || message.from,
    to: [...message.to],
    subject: message.subject,
  };
  for (const key of ['cc', 'bcc', 'reply_to']) {
    if (message[key]?.length) payload[key] = [...message[key]];
  }
  if (message.html !== undefined && message.html !== null) payload.html = message.html;
  if (message.text) payload.text = message.text;
  if (message.attachments?.length) {
    payload.attachments = message.attachments.map((a) => ({
      filename: a.filename,
      content: Buffer.from(a.content).toString('base64'),
      content_type: a.contentType,
    }));
  }
  return payload;
}

/** Sends through Resend's HTTP API. */
export class ResendTransport {
  /**
   * @param {{ apiKey?: string, apiUrl?: string, timeoutMs?: number, failSilently?: boolean,
   *   request?: typeof timedFetch, logger?: Pick<Console, 'info' | 'error'> }} options
   *   An empty `apiKey` stays empty; it is never replaced from settings.
   */
  constructor({ apiKey = '', apiUrl = RESEND_API_URL, timeoutMs = 10_000, failSilently = false,
    request = timedFetch, logger = console } = {}) {
    this.apiKey = apiKey;
    this.apiUrl = apiUrl;
    this.timeoutMs = timeoutMs;
    this.failSilently = failSilently;
    this.request = request;
    this.logger = logger;
  }

  /**
   * Send each message. Returns how many Resend accepted. With `failSilently` an error is logged
   * and counted as not sent; otherwise the first error is thrown.
   * @param {object[]} messages
   * @returns {Promise<number>}
   */
  async sendMessages(messages) {
    if (!messages.length) return 0;
    let sent = 0;
    for (const message of messages) {
      try {
        await this.sendOne(message);
        sent += 1;
      } catch (error) {
        if (!this.failSilently) throw error;
        this.logger.error(`Resend: failed to send ${JSON.stringify(message.subject)}:`, error);
      }
    }
    return sent;
  }

  /**
   * POST one message; returns Resend's message id (or null when the body is not JSON).
   * @param {object} message
   * @returns {Promise<string | null>}
   */
  async sendOne(message) {
    if (!this.apiKey) {
      throw new MailConfigError('RESEND_API_KEY is not set, so the Resend mail transport cannot send. '
        + 'Set it in the environment (see .env.example).');
    }
    let response;
    try {
      response = await this.request(this.apiUrl, {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${this.apiKey}`,
          'Content-Type': 'application/json',
          'User-Agent': USER_AGENT,
        },
        body: JSON.stringify(resendPayload(message)),
      }, this.timeoutMs);
    } catch (error) {
      throw new ResendAPIError(`Could not reach Resend: ${error.message}`, { cause: error });
    }
    const body = await response.text();
    if (!response.ok) {
      // The body is the only clue about why Resend refused (unverified domain, bad key, ...).
      throw new ResendAPIError(`Resend rejected the message (HTTP ${response.status}): ${body}`);
    }
    let id = null;
    try { id = body ? JSON.parse(body).id ?? null : null; } catch { id = null; }
    // Accepted is not delivered; the id is the handle for checking delivery in Resend.
    this.logger.info(`Resend accepted ${JSON.stringify(message.subject)} for ${message.to.join(', ')} (id=${id})`);
    return id;
  }
}

/** Prints messages instead of sending them. */
export class ConsoleTransport {
  /** @param {{ write?: (text: string) => void }} [options] */
  constructor({ write = (text) => process.stdout.write(text) } = {}) {
    this.write = write;
  }

  /**
   * @param {object[]} messages
   * @returns {Promise<number>}
   */
  async sendMessages(messages) {
    for (const m of messages) {
      this.write(`--- mail (console transport) ---\nFrom: ${m.from}\nTo: ${m.to.join(', ')}\n`
        + `Subject: ${m.subject}\n\n${m.text}\n--------------------------------\n`);
    }
    return messages.length;
  }
}

/** Keeps messages in memory (tests and the end-to-end test server). */
export class MemoryTransport {
  constructor() {
    /** @type {object[]} */
    this.outbox = [];
  }

  /**
   * @param {object[]} messages
   * @returns {Promise<number>}
   */
  async sendMessages(messages) {
    this.outbox.push(...messages.map((m) => structuredClone(m)));
    return messages.length;
  }
}

/**
 * The mailer the routes use. `send` throws on failure; callers that must not fail (the welcome
 * email) catch and log.
 */
export class Mailer {
  /** @param {{ sendMessages: (messages: object[]) => Promise<number> }} transport */
  constructor(transport) {
    this.transport = transport;
  }

  /**
   * @param {object} message
   * @returns {Promise<number>}
   */
  send(message) {
    return this.transport.sendMessages([message]);
  }

  /** @returns {object[] | undefined} captured messages when the transport is `MemoryTransport` */
  get outbox() {
    return this.transport.outbox;
  }
}

/**
 * The mailer for a config: Resend when a key is set, otherwise the console.
 * @param {import('../config.js').Config} config
 * @returns {Mailer}
 */
export function mailerFor(config) {
  return new Mailer(config.resendApiKey
    ? new ResendTransport({ apiKey: config.resendApiKey, timeoutMs: config.resendTimeoutMs })
    : new ConsoleTransport());
}
