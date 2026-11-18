/**
 * Game save routes under `/api/game/` (2026-11-18). Every route except the leaderboard needs a
 * signed-in user and only ever touches that user's rows (see `lib/gameSaves.js` for the rules).
 *
 * - `GET /slots/`, `POST /slots/` `{character}`, `DELETE /slots/` (all of the player's slots)
 * - `GET /slots/:id/` (slot + documents), `DELETE /slots/:id/`
 * - `POST /slots/:id/commit/`: the one all-or-nothing write (documents, slot changes, receipt)
 * - `GET /account-docs/`, `PUT /account-docs/:key/` (settings and operative progression)
 * - `GET /leaderboard/`: public; usernames and best scores only
 *
 * Rate limits: `gameWritesPerUserPerMinute` on writes per user, and `leaderboardPerIpPerMinute`
 * on the public leaderboard per client IP.
 */
import express from 'express';

import { requireUser } from '../lib/auth.js';
import {
  commitSlot, createSlot, deleteSlots, getAccountDocs, getSlot, leaderboard, listSlots,
  putAccountDoc, validateCommit,
} from '../lib/gameSaves.js';
import { byIp, rateLimit } from '../lib/throttle.js';
import { route } from './methods.js';

/**
 * Build the router.
 * @param {import('../app.js').Deps} deps
 * @returns {import('express').Router}
 */
export function gameRoutes(deps) {
  const router = express.Router();
  const { pool } = deps;
  const signedIn = requireUser(deps);
  // Keyed on the user, so it must run after `signedIn`.
  const writes = rateLimit({ windowMs: 60_000, limit: () => deps.config.gameWritesPerUserPerMinute,
    key: (req) => (req.user ? `game-write:${req.user.id}` : null) });
  const board = rateLimit({ windowMs: 60_000, limit: () => deps.config.leaderboardPerIpPerMinute,
    key: byIp(deps.clientIp, 'leaderboard') });
  deps.limiters.push(writes, board);

  route(router, '/slots/', {
    get: [signedIn, async (req, res) => { res.json(await listSlots(pool, req.user.id)); }],
    post: [signedIn, writes, async (req, res) => { res.status(201).json(await createSlot(pool, req.user.id, req.body?.character)); }],
    delete: [signedIn, writes, async (req, res) => { res.json({ deleted: await deleteSlots(pool, req.user.id) }); }],
  });

  route(router, '/slots/:id/', {
    get: [signedIn, async (req, res) => { res.json(await getSlot(pool, req.user.id, req.params.id)); }],
    delete: [signedIn, writes, async (req, res) => { res.json({ deleted: await deleteSlots(pool, req.user.id, req.params.id) }); }],
  });

  route(router, '/slots/:id/commit/', {
    post: [signedIn, writes, async (req, res) => {
      const commit = validateCommit(req.body);
      res.json(await commitSlot(pool, req.user.id, req.params.id, commit));
    }],
  });

  route(router, '/account-docs/', {
    get: [signedIn, async (req, res) => { res.json(await getAccountDocs(pool, req.user.id)); }],
  });

  route(router, '/account-docs/:key/', {
    put: [signedIn, writes, async (req, res) => {
      await putAccountDoc(pool, req.user.id, req.params.key, req.body);
      res.json({ saved: req.params.key });
    }],
  });

  route(router, '/leaderboard/', {
    get: [board, async (req, res) => { res.set('Cache-Control', 'no-store').json(await leaderboard(pool)); }],
  });

  return router;
}
