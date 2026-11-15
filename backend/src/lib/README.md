# Accounts Libraries

## Overview

Shared authentication, validation, persistence helpers, transport and throttling used by [HTTP routes](../routes/README.md).

## Interface

| File | Exported boundary |
| --- | --- |
| [auth.js](./auth.js) | `bearerToken`, `userFromRequest`, `requireUser`, `optionalUser`. |
| [clerk.js](./clerk.js) | `clerkSubject`, `fetchClerkUser`, `parseClerkUser`, `isValidUserId`; `ClerkError`, `ClerkUserNotFound`, `ClerkUnavailable`, `ClerkAPIError`. |
| [countries.js](./countries.js) | `loadCountries`, `countryCommonNames`, `basicCountries` read/cache `backend/data/countries.json`. |
| [djangoHash.js](./djangoHash.js) | `makePassword`, `checkPassword`, `makeUnusablePassword`, `hasUsablePassword`, `isCheckableHash`, `setHashConcurrency`. |
| [errors.js](./errors.js) | `HttpError`, `FieldErrors`, `authFailed`, `validationError`, `MESSAGES`, `errorHandler`. |
| [users.js](./users.js) | `profileOf`, ID/username lookup, email/username normalization, `createUser`, `readString`, registration/profile validation and `updateUser`. |
| [tokens.js](./tokens.js) | `issueTokenPair`, `userIdFromAccessToken`, `rotateRefreshToken`, `revokeAllRefreshTokens`, `purgeExpired`. |
| [twoFactor.js](./twoFactor.js) | `generateCode`, `createCode`, `verifyCode`, `isUuid`, `CODE_LIFETIME_MINUTES`. |
| [throttle.js](./throttle.js) | `normalizeIp`, `limitKeyForIp`, `createClientIp`, `rateLimit`, `byIp`; limiter reset/size/sweep helpers, client-IP stop helper. |
| [mail.js](./mail.js) | `Mailer`, Resend/Console/Memory transports, `mailerFor`, `timedFetch`, `resendPayload`, transport errors, `RESEND_API_URL`, `USER_AGENT`. |
| [emails.js](./emails.js) | `welcomeEmail`, `sendWelcomeEmail`, `sendSecurityNotice`, `twoFactorEmail`, `onboardingEmail`. |

## Invariants

- Required auth resolves an active database user; optional auth allows no token but rejects invalid supplied Bearer credentials. Exact `Bearer` scheme is recognized; other schemes are anonymous. `authFailed` uses 401 with challenge for authenticated routes and 403 when `routeHasAuth=false` (`auth.js`, `errors.js`).
- `clerkSubject` verifies RS256 with configured trusted issuer/JWKS, required lifetime/subject/origin claims, allowed `azp` and non-pending session. It checks a `user_` subject prefix; `fetchClerkUser` separately enforces the stricter alphanumeric ID pattern before a URL/network call. JWKS URLs never come from token input (`clerk.js`).
- Passwords use Django `pbkdf2_sha256` with stored iteration count and constant-time key comparison. `!` passwords cannot authenticate. Async hashing shares a process-wide concurrency cap (`djangoHash.js`).
- `profileOf` emits only ID, username, email, first/last name, picture, 2FA flag, phone and country. Creation normalizes username NFKC and email domain case; updates take validated fixed column names (`users.js`).
- Tokens are HS256 with distinct access/refresh type; default lifetimes are one hour/seven days. Rotation spends and inserts in one transaction. Replay revokes the sign-in family; revoke-all leaves existing access tokens valid until expiry (`tokens.js`).
- Code issuance serializes on the user row before counting/inserting within its transaction. Verification takes the same user lock, selects the newest code for purpose/session including verified or expired rows, then refuses consumed/expired/exhausted codes. Thus consuming a successor cannot revive an older code. Codes are six cryptographic digits, live ten minutes and allow five attempts by default (`twoFactor.js`).
- Refresh rotation locks the user before the token row; `revokeAllRefreshTokens(pool,userId)` uses its own transaction and the same user lock. Callers supply a pool, not an existing transaction client (`tokens.js`).
- `createClientIp` trusts only configured/resolved proxy peers and chooses their last forwarded address; other callers use socket address. Limits group IPv6 by /64, store sliding windows per process and cap keys at 50,000; restarting clears request counters (`throttle.js`).
- Mail delivery acceptance is not confirmed inbox delivery. `mailerFor` chooses Resend only when configured; memory transport clones captures; welcome/security-notice failures are logged rather than thrown (`mail.js`, `emails.js`).

## Side Effects

- User/token/code helpers query/update PostgreSQL and take locks. `purgeExpired` deletes refresh rows expired over one day, codes older than one day and unlinked browser sessions expired over one day.
- Clerk verification may fetch JWKS; user lookup and Resend send HTTP with timeouts. Console transport prints complete mail; other helpers log transport failures/acceptance.
- Country loading synchronously reads/caches disk data. Hashing uses crypto worker resources and global queue state. Throttling starts unreferenced sweep/DNS timers and caches counts/resolved peers.
- Email constructors are pure message builders; send helpers invoke the supplied transport.

## Verification

- From `backend/`: `npm test`. Transport tests inject HTTP; security/hardening suites exercise persistence and token/code rules. `test/authConcurrency.test.js` adds deterministic issuance/refresh races and code supersession regressions.
