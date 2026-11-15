# Accounts API Blueprint

## System Overview

Express 5 on Node 26 serves password sign-in, Clerk browser sign-in, onboarding, profiles and emailed security codes. It replaces Django while retaining `/api/auth/` paths, public JSON shapes, `accounts_*` tables and PBKDF2 password hashes. Old Django-signed JWTs are rejected (`src/lib/tokens.js`).

## Interface

- [Runtime](./src/README.md): app composition, environment settings, pool, startup and migration runner.
- [Migrations](./migrations/README.md): ordered account schema changes.
- [Tests](./test/README.md): HTTP assertions and isolated PostgreSQL fixtures.
- [Browser test tools](./scripts/README.md): disposable API and restricted proxy.
- [package.json](./package.json) declares `start`, `migrate` and `test`. [Dockerfile](./Dockerfile) has `runtime` and `dev` targets; `dev` adds tests and browser helpers. Both run as `node`.

## Invariants

- `loadConfig` requires `JWT_SIGNING_KEY` of at least 32 characters for normal startup (`src/config.js`).
- `server.js` waits for PostgreSQL and completes migration before listening. Migration preserves existing account tables and checks required columns.
- Refresh rotation and browser redemption use PostgreSQL transactions. Revoking refresh tokens leaves issued access tokens valid until expiry (`src/lib/tokens.js`).
- `createApp` uses Bearer authentication and allows any CORS origin without credentials. Required, optional and absent authentication differ; see [libraries](./src/lib/README.md).

## Side Effects

- Account operations read/write PostgreSQL. Startup applies pending migrations. The server purges eligible expired tokens, codes and browser sessions at startup and hourly (`src/server.js:cleanUp`).
- Clerk calls and Resend delivery can make external HTTP requests. Without `RESEND_API_KEY`, mail is printed to stdout (`src/lib/mail.js:mailerFor`).
- Startup opens an HTTP listener (default port 8000), timers and a database pool. Signal handlers close the listener and pool (`src/server.js:shutdown`).

## Verification

- From `backend/`: `npm test` runs Node tests serially and requires PostgreSQL with create/drop permission for `accounts_test`.
- From repository root with the development container: `docker compose exec -T backend npm test`. Backend source is baked into the image; rebuild after source changes before verifying in that container.
- Documentation-only edits do not require tests; listed commands do not imply a completed test run.
