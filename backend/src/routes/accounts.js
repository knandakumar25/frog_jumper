/**
 * Account routes under `/api/auth/`: register, password login, token refresh, profile, 2FA and
 * the country lists. Paths, bodies and status codes match the Django views they replace, except
 * for the owner-approved security changes noted on each route.
 *
 * Rate limits (2026-11-15 EGM review, issue 1; numbers from settings):
 * - login, register, 2fa/verify-login and token/refresh: `authPerIpPerMinute` per client IP;
 * - login also `loginPerUserPer15Min` per username (so spread-out guessing still stops);
 * - every emailed code: `codesPerUserPer15Min` per user and purpose (`twoFactor.createCode`).
 *
 * Account deletion (2026-11-18): `account/delete-request/` emails a code and `account/delete/`
 * deletes the account with it (`lib/accountDeletion.js`).
 */
import express from 'express';

import { optionalUser, requireUser } from '../lib/auth.js';
import { checkPassword, isCheckableHash, makePassword } from '../lib/djangoHash.js';
import { sendSecurityNotice, sendWelcomeEmail, twoFactorEmail } from '../lib/emails.js';
import { FieldErrors, HttpError, MESSAGES } from '../lib/errors.js';
import { basicCountries, loadCountries } from '../lib/countries.js';
import { byIp, rateLimit } from '../lib/throttle.js';
import { issueTokenPair, revokeAllRefreshTokens, rotateRefreshToken } from '../lib/tokens.js';
import { createCode, isUuid, verifyCode } from '../lib/twoFactor.js';
import { deleteAccount, processClerkDeletions } from '../lib/accountDeletion.js';
import {
  createUser, findUserById, findUserByUsername, profileOf, readString, updateUser,
  validateProfileUpdate, validateRegistration,
} from '../lib/users.js';
import { route } from './methods.js';

/**
 * Build the router.
 * @param {import('../app.js').Deps} deps
 * @returns {import('express').Router}
 */
export function accountRoutes(deps) {
  const router = express.Router();
  const { pool } = deps;
  const anonymousOk = optionalUser(deps);
  const signedIn = requireUser(deps);
  const perIp = rateLimit({ windowMs: 60_000, limit: () => deps.config.authPerIpPerMinute,
    key: byIp(deps.clientIp, 'auth') });
  const perUsername = rateLimit({ windowMs: 15 * 60_000, limit: () => deps.config.loginPerUserPer15Min,
    key: (req) => (typeof req.body?.username === 'string' && req.body.username ? `login:${req.body.username}` : null) });
  deps.limiters.push(perIp, perUsername);
  // A real hash, made at start-up, checked whenever there is no usable stored hash (unknown user,
  // Clerk-only account, malformed hash), so every failed login takes as long as a wrong password
  // and timing does not reveal which usernames exist (issue 7).
  const decoyHash = makePassword('not-a-real-password', deps.config.passwordIterations);
  decoyHash.catch(() => {});

  /** POST register/: create a password account; 201 with the new profile (no password). */
  route(router, '/register/', {
    post: [perIp, anonymousOk, async (req, res) => {
      const fields = await validateRegistration(pool, req.body ?? {});
      let user;
      try {
        user = await createUser(pool, deps.config, fields, fields.password);
      } catch (error) {
        if (error.code === '23505') {
          const field = String(error.constraint ?? '').includes('email') ? 'email' : 'username';
          throw new HttpError(400, { [field]: [field === 'email' ? MESSAGES.emailTaken : MESSAGES.usernameTaken] });
        }
        throw error;
      }
      await sendWelcomeEmail(deps.mailer, deps.config, user);
      const { id, username, email, first_name, last_name, profile_picture, country } = user;
      res.status(201).json({ id, username, email, first_name, last_name, profile_picture, country });
    }],
  });

  /**
   * POST login/: username and password. Returns tokens and the profile, or a 2FA challenge.
   * An unknown user, a wrong password and an inactive account all get the same 401
   * "Invalid credentials", after the same amount of hashing work.
   */
  route(router, '/login/', {
    post: [perIp, perUsername, anonymousOk, async (req, res) => {
      const { username, password } = req.body ?? {};
      if (!username || !password) {
        res.status(400).json({ error: 'Username and password are required' });
        return;
      }
      const user = typeof username === 'string' ? await findUserByUsername(pool, username) : null;
      let ok = false;
      if (user && isCheckableHash(user.password)) {
        ok = await checkPassword(String(password), user.password) && user.is_active;
      } else {
        await checkPassword(String(password), await decoyHash);
      }
      if (!ok) {
        res.status(401).json({ error: 'Invalid credentials' });
        return;
      }
      if (user.is_2fa_enabled) {
        const { code, sessionId } = await createCode(pool, user.id, 'login', deps.config);
        await deps.mailer.send(twoFactorEmail(user, code, 'login', deps.config.defaultFromEmail));
        res.json({ status: '2fa_required', login_session_id: sessionId });
        return;
      }
      const tokens = await issueTokenPair(pool, deps.config, user.id);
      res.json({ refresh: tokens.refresh, access: tokens.access, user: profileOf(user) });
    }],
  });

  /**
   * POST token/refresh/: trade a refresh token for a new pair. Each refresh token works once;
   * replaying a used one signs out its whole sign-in family (see `rotateRefreshToken`).
   */
  route(router, '/token/refresh/', {
    post: [perIp, async (req, res) => {
      const errors = new FieldErrors();
      const refresh = readString(errors, 'refresh', req.body?.refresh, { required: true, trim: false });
      errors.throwIfAny();
      res.json(await rotateRefreshToken(pool, deps.config, refresh));
    }],
  });

  /**
   * GET/PUT/PATCH profile/: the signed-in user's profile.
   * Changing the email needs `email_code`, a code mailed to the current address by
   * POST profile/email-code/. Afterwards the old address gets a notice and all refresh tokens
   * are revoked (issue 5), so a stolen access token cannot quietly redirect future 2FA codes.
   */
  const editProfile = (partial) => [signedIn, async (req, res) => {
    const changes = await validateProfileUpdate(pool, req.user, req.body ?? {}, partial);
    const emailChanging = changes.email !== undefined;
    if (emailChanging && !(req.body.email_code
        && await verifyCode(pool, deps.config, { userId: req.user.id, code: req.body.email_code, purpose: 'email_change' }))) {
      throw new HttpError(400, { email_code: [req.body.email_code
        ? 'Invalid or expired code.'
        : 'Request a code with /api/auth/profile/email-code/ and send it as email_code to change your email.'] });
    }
    const updated = await updateUser(pool, req.user.id, changes);
    if (emailChanging) {
      await revokeAllRefreshTokens(pool, req.user.id);
      await sendSecurityNotice(deps.mailer, deps.config, req.user.email, 'email_changed', updated.email);
    }
    res.json(profileOf(updated));
  }];
  route(router, '/profile/', {
    get: [signedIn, (req, res) => { res.json(profileOf(req.user)); }],
    put: editProfile(false),
    patch: editProfile(true),
  });

  /** POST profile/email-code/: mail a code to the current address, needed to change it. */
  route(router, '/profile/email-code/', {
    post: [signedIn, async (req, res) => {
      const { code } = await createCode(pool, req.user.id, 'email_change', deps.config);
      await deps.mailer.send(twoFactorEmail(req.user, code, 'email_change', deps.config.defaultFromEmail));
      res.json({ message: 'Verification code sent to your current email.' });
    }],
  });

  /** POST 2fa/enable/: email an opt-in code to the signed-in user. */
  route(router, '/2fa/enable/', {
    post: [signedIn, async (req, res) => {
      const { code } = await createCode(pool, req.user.id, 'opt_in', deps.config);
      await deps.mailer.send(twoFactorEmail(req.user, code, 'opt_in', deps.config.defaultFromEmail));
      res.json({ message: 'Verification code sent to your email.' });
    }],
  });

  /** POST 2fa/verify-enable/: turn 2FA on with the emailed opt-in code. */
  route(router, '/2fa/verify-enable/', {
    post: [signedIn, async (req, res) => {
      const code = req.body?.code;
      if (!code) {
        res.status(400).json({ error: 'Code is required' });
        return;
      }
      if (await verifyCode(pool, deps.config, { userId: req.user.id, code, purpose: 'opt_in' })) {
        await pool.query('UPDATE accounts_user SET is_2fa_enabled = true WHERE id = $1', [req.user.id]);
        res.json({ message: '2FA enabled successfully.' });
        return;
      }
      res.status(400).json({ error: 'Invalid or expired code' });
    }],
  });

  /** POST 2fa/disable-request/: email a code that confirms turning 2FA off (issue 5). */
  route(router, '/2fa/disable-request/', {
    post: [signedIn, async (req, res) => {
      if (!req.user.is_2fa_enabled) {
        res.status(400).json({ error: '2FA is not enabled' });
        return;
      }
      const { code } = await createCode(pool, req.user.id, 'opt_out', deps.config);
      await deps.mailer.send(twoFactorEmail(req.user, code, 'opt_out', deps.config.defaultFromEmail));
      res.json({ message: 'Verification code sent to your email.' });
    }],
  });

  /**
   * POST 2fa/disable/: turn 2FA off with the emailed code. Afterwards all refresh tokens are
   * revoked and the account's address gets a notice (issue 5).
   */
  route(router, '/2fa/disable/', {
    post: [signedIn, async (req, res) => {
      const code = req.body?.code;
      if (!code) {
        res.status(400).json({ error: 'Code is required' });
        return;
      }
      if (await verifyCode(pool, deps.config, { userId: req.user.id, code, purpose: 'opt_out' })) {
        await pool.query('UPDATE accounts_user SET is_2fa_enabled = false WHERE id = $1', [req.user.id]);
        await revokeAllRefreshTokens(pool, req.user.id);
        await sendSecurityNotice(deps.mailer, deps.config, req.user.email, '2fa_off');
        res.json({ message: '2FA disabled.' });
        return;
      }
      res.status(400).json({ error: 'Invalid or expired code' });
    }],
  });

  /**
   * POST account/delete-request/: email a code that confirms deleting the account. Works for
   * password and Clerk-only accounts alike, because it needs only the account's email.
   */
  route(router, '/account/delete-request/', {
    post: [signedIn, async (req, res) => {
      const { code } = await createCode(pool, req.user.id, 'delete_account', deps.config);
      await deps.mailer.send(twoFactorEmail(req.user, code, 'delete_account', deps.config.defaultFromEmail));
      res.json({ message: 'Verification code sent to your email.' });
    }],
  });

  /**
   * POST account/delete/: permanently delete the account with the emailed code and the typed
   * word DELETE. Removes the account and its linked rows in one transaction, then sends a notice
   * to the old address and asks Clerk to delete the sign-in identity (retried hourly if Clerk is
   * unreachable). The account's game saves are deleted with it.
   */
  route(router, '/account/delete/', {
    post: [signedIn, async (req, res) => {
      const code = req.body?.code;
      if (!code) {
        res.status(400).json({ error: 'Code is required' });
        return;
      }
      if (req.body?.confirm !== 'DELETE') {
        res.status(400).json({ error: 'Type DELETE to confirm' });
        return;
      }
      if (!await verifyCode(pool, deps.config, { userId: req.user.id, code, purpose: 'delete_account' })) {
        res.status(400).json({ error: 'Invalid or expired code' });
        return;
      }
      const result = await deleteAccount(pool, req.user.id);
      if (result.deleted) {
        await sendSecurityNotice(deps.mailer, deps.config, result.email, 'account_deleted');
        if (result.clerkUserId) {
          // One immediate try; a failure stays queued for the hourly cleanup.
          await processClerkDeletions(pool, deps.config, deps.clerk.deleteUser, { onlyId: result.clerkUserId })
            .catch((error) => console.error('Clerk deletion attempt failed:', error.message));
        }
      }
      res.json({ message: 'Account deleted.' });
    }],
  });

  /** POST 2fa/verify-login/: finish a 2FA login with the emailed code; returns tokens. */
  route(router, '/2fa/verify-login/', {
    post: [perIp, anonymousOk, async (req, res) => {
      const { login_session_id: sessionId, code } = req.body ?? {};
      if (!sessionId || !code) {
        res.status(400).json({ error: 'login_session_id and code are required' });
        return;
      }
      const owner = isUuid(sessionId) ? (await pool.query(
        `SELECT user_id FROM accounts_twofactorcode
          WHERE session_id = $1 AND is_verified = false AND purpose = 'login' LIMIT 1`, [sessionId],
      )).rows[0] : undefined;
      if (!owner) {
        res.status(400).json({ error: 'Invalid or expired session' });
        return;
      }
      const user = await findUserById(pool, owner.user_id);
      if (user?.is_active && await verifyCode(pool, deps.config, { userId: user.id, code, purpose: 'login', sessionId })) {
        const tokens = await issueTokenPair(pool, deps.config, user.id);
        res.json({ refresh: tokens.refresh, access: tokens.access, user: profileOf(user) });
        return;
      }
      res.status(400).json({ error: 'Invalid or expired code' });
    }],
  });

  /** GET countries/: the full dataset (the account page needs `idd`). No auth. */
  route(router, '/countries/', { get: [(req, res) => { res.json(loadCountries()); }] });
  /** GET countries/basic/: cca3, name.common and flag only (onboarding). No auth. */
  route(router, '/countries/basic/', { get: [(req, res) => { res.json(basicCountries()); }] });

  return router;
}
