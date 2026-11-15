/**
 * Request limits and the client address they are keyed on.
 *
 * Django had one limit (browser sign-in, 90 a minute per IP). The 2026-11-15 EGM review found
 * login, register, 2FA and token refresh unlimited, and the per-IP keys forgeable, so:
 *
 * - `clientIp` trusts `X-Forwarded-For` only when the request comes from the reverse proxy
 *   (the nginx `website` container, looked up by name, or an address in
 *   `trustedProxyAddresses`). Anyone calling port 8000 directly is keyed on the socket address,
 *   whatever header they send. Compose also publishes 8000 on 127.0.0.1 only.
 * - `rateLimit` is a sliding-window limiter keyed by any function of the request (IP, username).
 *   Stale keys are swept every minute and the map is capped, so rotating keys cannot grow
 *   memory without bound. IPv6 addresses are grouped by /64, the block one client usually holds.
 *
 * Counts live in this process's memory, as Django's default cache did; a restart clears them.
 */
import { lookup } from 'node:dns/promises';
import net from 'node:net';

const MAX_KEYS = 50_000;

/**
 * Reduce `::ffff:1.2.3.4` to `1.2.3.4`; return null for anything that is not an IP address.
 * @param {string | undefined} value
 * @returns {string | null}
 */
export function normalizeIp(value) {
  let ip = String(value ?? '').trim();
  if (ip.startsWith('::ffff:') && net.isIPv4(ip.slice(7))) ip = ip.slice(7);
  return net.isIP(ip) ? ip : null;
}

/**
 * The /64 network of an IPv6 address (first four groups), or the IPv4 address unchanged.
 * @param {string} ip a value `normalizeIp` accepted
 * @returns {string}
 */
export function limitKeyForIp(ip) {
  if (!net.isIPv6(ip)) return ip;
  const [head, tail = ''] = ip.split('::');
  const left = head ? head.split(':') : [];
  const right = tail ? tail.split(':') : [];
  const groups = ip.includes('::') ? [...left, ...Array(8 - left.length - right.length).fill('0'), ...right] : left;
  return `${groups.slice(0, 4).map((g) => g.toLowerCase().replace(/^0+(?=.)/, '')).join(':')}::/64`;
}

/**
 * Build the `clientIp(req)` function for a config.
 *
 * The proxy host name is resolved now and every 30 seconds (the nginx container's address
 * changes when it is recreated). Lookup failures are ignored: outside Compose there is no
 * `website` host, and then only `trustedProxyAddresses` are trusted.
 * @param {{trustedProxyHost: string, trustedProxyAddresses: string[]}} config
 * @returns {((req: import('express').Request) => string | null) & { stop: () => void }}
 */
export function createClientIp(config) {
  const fixed = new Set(config.trustedProxyAddresses.map(normalizeIp).filter(Boolean));
  let resolved = new Set();
  const refresh = () => {
    if (!config.trustedProxyHost) return;
    lookup(config.trustedProxyHost, { all: true })
      .then((found) => { resolved = new Set(found.map((a) => normalizeIp(a.address)).filter(Boolean)); })
      .catch(() => { resolved = new Set(); });
  };
  refresh();
  const timer = setInterval(refresh, 30_000);
  timer.unref();

  /**
   * The caller's address: the last X-Forwarded-For entry (the one nginx appended) when the
   * socket peer is the trusted proxy, otherwise the socket address. Null when unusable.
   * @param {import('express').Request} req
   * @returns {string | null}
   */
  const clientIp = (req) => {
    const peer = normalizeIp(req.socket?.remoteAddress);
    if (peer && (fixed.has(peer) || resolved.has(peer))) {
      const forwarded = (req.get('x-forwarded-for') ?? '').split(',').map((p) => p.trim()).filter(Boolean);
      const last = normalizeIp(forwarded.at(-1));
      if (last) return last;
    }
    return peer;
  };
  clientIp.stop = () => clearInterval(timer);
  return clientIp;
}

/**
 * A sliding-window rate limiter middleware.
 *
 * Requests whose `key(req)` is null are not counted (for example a login with no username, which
 * the route rejects anyway). Over the limit: 429 with DRF's throttled body and `Retry-After`.
 * @param {{ windowMs: number, limit: () => number, key: (req: import('express').Request) => string | null,
 *   headers?: Record<string, string> }} options `limit` is read per request so tests can change it
 * @returns {import('express').RequestHandler & { reset: () => void, size: () => number }}
 */
export function rateLimit({ windowMs, limit, key, headers = {} }) {
  /** @type {Map<string, number[]>} */
  const hits = new Map();
  const sweep = () => {
    const now = Date.now();
    for (const [k, times] of hits) {
      const recent = times.filter((t) => now - t < windowMs);
      if (recent.length) hits.set(k, recent);
      else hits.delete(k);
    }
  };
  const timer = setInterval(sweep, 60_000);
  timer.unref();

  const handler = (req, res, next) => {
    const k = key(req);
    if (k === null || k === undefined) return next();
    const now = Date.now();
    const recent = (hits.get(k) ?? []).filter((t) => now - t < windowMs);
    if (recent.length >= limit()) {
      const wait = Math.max(1, Math.ceil((windowMs - (now - recent[0])) / 1000));
      hits.set(k, recent);
      res.set({ ...headers, 'Retry-After': String(wait) })
        .status(429).json({ detail: `Request was throttled. Expected available in ${wait} seconds.` });
      return undefined;
    }
    recent.push(now);
    hits.delete(k); // re-insert so Map order tracks recent use
    hits.set(k, recent);
    if (hits.size > MAX_KEYS) {
      // Drop the least recently used keys; a flood of new keys cannot grow memory past the cap.
      const excess = hits.size - MAX_KEYS;
      let i = 0;
      for (const oldKey of hits.keys()) {
        if (i++ >= excess) break;
        hits.delete(oldKey);
      }
    }
    return next();
  };
  handler.reset = () => hits.clear();
  handler.size = () => hits.size;
  handler.sweep = sweep;
  return handler;
}

/**
 * Key a limiter on the client's address (grouped by /64 for IPv6).
 * @param {(req: import('express').Request) => string | null} clientIp
 * @param {string} scope separates limiters that share a key space
 * @returns {(req: import('express').Request) => string}
 */
export function byIp(clientIp, scope) {
  return (req) => `${scope}:${limitKeyForIp(clientIp(req) ?? 'unknown')}`;
}
