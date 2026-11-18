# Accounts HTTP Routes

## Overview

These routers are mounted at `/api/auth` by `src/app.js`. Paths below include that prefix. Shared identity, mail, token and database behavior comes from [libraries](../lib/README.md).

## Interface

| Module | Methods and paths | Identity / result |
| --- | --- | --- |
| [accounts.js](./accounts.js): `accountRoutes` | POST `/api/auth/register/`, `/api/auth/login/`, `/api/auth/2fa/verify-login/` | Optional Bearer authentication: absent allowed, invalid supplied token rejected. Registration returns 201 profile; login returns tokens/profile or a `2fa_required` challenge. Verification requires login session and code. |
| `accountRoutes` | POST `/api/auth/token/refresh/` | Refresh token in body; rotates to a new pair. |
| `accountRoutes` | GET/PUT/PATCH `/api/auth/profile/` | Required active user; reads/updates public profile. |
| `accountRoutes` | POST `/api/auth/profile/email-code/`, `/api/auth/2fa/enable/`, `/api/auth/2fa/verify-enable/`, `/api/auth/2fa/disable-request/`, `/api/auth/2fa/disable/` | Required user; security-code requests and confirmations. |
| `accountRoutes` | POST `/api/auth/account/delete-request/`, `/api/auth/account/delete/` | Required user. Request emails a `delete_account` code; delete needs `{code, confirm: "DELETE"}`, removes the account, its linked rows and all its game saves, emails a notice and deletes the Clerk identity (queued for hourly retry when Clerk is unreachable). Added 2026-11-18. |
| `accountRoutes` | GET `/api/auth/countries/`, `/api/auth/countries/basic/` | No authentication; full dataset or `cca3`, `name.common`, `flag` subset. |
| [game.js](./game.js): `gameRoutes` | GET/POST/DELETE `/api/game/slots/`, GET/DELETE `/api/game/slots/:id/`, POST `/api/game/slots/:id/commit/` | Required user; every query is scoped to that user and another user's slot is 404. Commit body: `expectRevision`, optional `token` (uuid) + `receipt {kind, value}`, `slot {character, levelAtLeast, scoreAdd, collectablesAdd}`, `docs [{key, schemaVersion, value}]`. Answers `committed`/`alreadyCommitted` with the slot, or 409 `revision_conflict` (with the current slot and documents), `receipt_mismatch` or `schema_newer`. Writes are limited per user (`GAME_WRITES_PER_USER_PER_MINUTE`). Added 2026-11-18. |
| `gameRoutes` | GET `/api/game/account-docs/`, PUT `/api/game/account-docs/:key/` | Required user; keys `user_settings`, `user_keybindings`, `input_prompt_settings`, `operative_progression` only. |
| `gameRoutes` | GET `/api/game/leaderboard/` | Public, limited per IP (`LEADERBOARD_PER_IP_PER_MINUTE`); top 50 accounts by best slot score: rank, username, score, level. No ids or emails. |
| [browser.js](./browser.js): `browserRoutes` | POST `/api/auth/browser-start/` | Creates a 15-minute request and returns public ID, private secret, expiry and website approval URL. |
| `browserRoutes` | POST `/api/auth/browser-confirm/` | Verified Clerk session for an active linked account, or local Bearer proof; binds an account once. Unknown Clerk account returns 409 `onboarding_required`. |
| `browserRoutes` | POST `/api/auth/browser-poll/`, `/api/auth/browser-cancel/` | Public ID plus private secret. Poll returns pending or a single token delivery; cancel ends the request. |
| [onboarding.js](./onboarding.js): `onboardingRoutes` | POST `/api/auth/onboarding/start/` | Fetches verified Clerk profile and emails completion link; does not return raw token. Attached browser request requires identity proof. |
| `onboardingRoutes` | GET `/api/auth/onboarding/validate-token/?token=`, POST `/api/auth/onboarding/complete/` | Token proof; validate returns metadata without consumption, complete creates Clerk-only account and returns tokens/profile. |

- `browser.js` exports `sha256Hex` and `liveRequest(client,data,privateCheck)`; the latter must run within a transaction because it locks the browser row.
- `onboarding.js` exports `maskEmail` and `validateCompletion`.
- [methods.js](./methods.js): `route(router,path,handlers)` installs supported methods and a 405 JSON fallback with `Allow`; GET advertises HEAD and OPTIONS is always advertised.

## Invariants

- Protected profile and account-security routes also accept an explicit verified `Clerk`
  Authorization scheme for the active linked user. Missing linkage is 403
  `onboarding_required`; no API token pair or game approval is issued by this lookup.
  Existing Bearer clients, request bodies and success JSON remain unchanged.

- Account login/register/refresh/2FA verification share an IP request budget; login has an additional username budget (`accounts.js:accountRoutes`). Failed credentials use the same 401 body for missing, wrong-password and inactive accounts; unusable hashes perform a decoy check.
- Profile PUT requires username/email; PATCH is partial. Changing email requires an `email_change` code sent to the old address. Changing `is_2fa_enabled` through profile is refused; dedicated code routes must be used (`accounts.js:editProfile`, `users.js:validateProfileUpdate`).
- Browser start accepts intent `login` or `signup` and string locale up to 32 characters; defaults are `login` and `en-US`. The URL contains only public session ID/intent/locale (`browser.js:browserRoutes`).
- `liveRequest` locks the row, rejects missing secret hash/expiry, expired, consumed or cancelled requests, and optionally checks a string secret no longer than 128 characters by hash and timing-safe comparison. Confirm/poll/cancel use transactions; repeat approval is 409, expired/private-proof failures are 410. Browser handlers use no-store headers and an IP limiter.
- Onboarding validate/complete ignore stale local Bearer headers. Token validation does not consume the link; missing/unknown/completed/expired tokens yield 400/404/409/410 (`onboarding.js:onboardingRoutes`).
- Completion requires a known `name.common` country and real `YYYY-MM-DD` date from 1900 through today UTC; there is no age gate (`validateCompletion`). Country restrictions here are stricter than password registration/profile string validation.
- Start applies configuration, verified-email, cooldown and send-budget checks; resends rotate the stored hash. Completion consumes registration with account creation and maps uniqueness clashes to 409. Returning a linked browser ID does not approve that request (`onboardingRoutes`).

## Side Effects

- Account handlers create/edit users, issue/rotate/revoke tokens, create/consume codes, toggle 2FA and send mail. Email changes/2FA disabling revoke refresh tokens and notify the previous address.
- Browser handlers insert sessions, set authorization/user, consume or cancel requests, and insert refresh records when delivering tokens.
- Onboarding start contacts Clerk, sends email and updates pending registration state. Completion creates account/token rows and sends welcome mail; HTML message values are escaped by `emails.js`.

## Verification

- From `backend/`: `npm test`; route coverage is in `views.test.js`, `browserAuth.test.js`, `onboarding.test.js`, `security.test.js` and `hardening.test.js`. See [test contracts](../../test/README.md).
