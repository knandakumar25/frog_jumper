/**
 * Builds the Express app from its dependencies (settings, database pool, mailer, Clerk calls).
 *
 * Everything the routes need comes in through `deps`, and routes read `deps.clerk` and
 * `deps.mailer` at call time. Tests build the app with an in-memory mailer and fake Clerk calls,
 * and can swap them per test (the Node equivalent of Django's `mock.patch`).
 */
import express from 'express';

import { clerkSubject, deleteClerkUser, fetchClerkUser } from './lib/clerk.js';
import { errorHandler } from './lib/errors.js';
import { mailerFor } from './lib/mail.js';
import { setHashConcurrency } from './lib/djangoHash.js';
import { byIp, createClientIp, rateLimit } from './lib/throttle.js';
import { accountRoutes } from './routes/accounts.js';
import { browserRoutes } from './routes/browser.js';
import { onboardingRoutes } from './routes/onboarding.js';
import { gameRoutes } from './routes/game.js';

/**
 * @typedef {object} Deps
 * @property {import('./config.js').Config} config
 * @property {import('pg').Pool} pool
 * @property {import('./lib/mail.js').Mailer} mailer
 * @property {{ subject: (token: string, config: import('./config.js').Config) => Promise<string>,
 *   fetchUser: (id: unknown, config: import('./config.js').Config) => Promise<object>,
 *   deleteUser: (id: string, config: import('./config.js').Config) => Promise<unknown> }} clerk
 * @property {ReturnType<typeof createClientIp>} clientIp the caller's address (see throttle.js)
 * @property {ReturnType<typeof rateLimit>} browserThrottle 90 a minute per IP on browser-* routes
 * @property {ReturnType<typeof rateLimit>[]} limiters every limiter, so tests can reset them
 */

// django-cors-headers defaults: any origin, no credentials, these headers and methods.
const CORS_HEADERS = 'accept, authorization, content-type, user-agent, x-csrftoken, x-requested-with';
const CORS_METHODS = 'DELETE, GET, OPTIONS, PATCH, POST, PUT';

/**
 * Allow cross-origin calls from any site (the website dev server calls port 8000 directly).
 * The API uses Bearer tokens, not cookies, so no credentials are allowed.
 * @type {import('express').RequestHandler}
 */
function cors(req, res, next) {
  if (!req.get('origin')) return next();
  res.set({ 'Access-Control-Allow-Origin': '*', Vary: 'Origin' });
  if (req.method === 'OPTIONS' && req.get('access-control-request-method')) {
    res.set({
      'Access-Control-Allow-Headers': CORS_HEADERS,
      'Access-Control-Allow-Methods': CORS_METHODS,
      'Access-Control-Max-Age': '86400',
    }).status(200).end();
    return undefined;
  }
  return next();
}

/**
 * Create the app.
 * @param {Partial<Deps> & { config: import('./config.js').Config, pool: import('pg').Pool }} partial
 *   `mailer`, `clerk`, `clientIp` and `browserThrottle` default to the real ones for `config`
 * @returns {import('express').Express & { locals: { deps: Deps } }}
 */
export function createApp(partial) {
  /** @type {Deps} */
  const deps = {
    mailer: mailerFor(partial.config),
    clerk: {
      subject: (token, config) => clerkSubject(token, config),
      fetchUser: (id, config) => fetchClerkUser(id, config),
      deleteUser: (id, config) => deleteClerkUser(id, config),
    },
    clientIp: partial.clientIp ?? createClientIp(partial.config),
    limiters: [],
    ...partial,
  };
  deps.browserThrottle ??= rateLimit({ windowMs: 60_000, limit: () => deps.config.browserThrottlePerMinute,
    key: byIp(deps.clientIp, 'browser') });
  deps.limiters.push(deps.browserThrottle);
  setHashConcurrency(deps.config.maxConcurrentHashes);
  const app = express();
  app.disable('x-powered-by');
  app.locals.deps = deps;
  app.use(cors);
  // Django's default upload limit was 2.5 MB; profile pictures arrive as base64 text.
  app.use(express.json({ limit: '2.5mb' }));
  app.use(express.urlencoded({ extended: false, limit: '2.5mb' }));

  /** GET /api/health: 200 when the database answers (Compose health check). */
  app.get('/api/health', async (req, res) => {
    try {
      await deps.pool.query('SELECT 1');
      res.set('Cache-Control', 'no-store').json({ status: 'ok' });
    } catch {
      res.status(503).json({ status: 'database_unavailable' });
    }
  });

  app.use('/api/auth', accountRoutes(deps), browserRoutes(deps), onboardingRoutes(deps));
  // Per-account game saves (2026-11-18).
  app.use('/api/game', gameRoutes(deps));
  app.use((req, res) => { res.status(404).json({ detail: 'Not found.' }); });
  app.use(errorHandler);
  return app;
}
