# API Runtime

## Overview

Application composition, configuration, PostgreSQL connections, schema migration and process lifecycle.

## Interface

| File | Boundary |
| --- | --- |
| [app.js](./app.js) | `createApp({config,pool,...overrides})`; exposes `app.locals.deps`, mounts `/api/auth` routers. `GET /api/health` returns 200 `{status:'ok'}` for successful `SELECT 1`, otherwise 503 `{status:'database_unavailable'}`. |
| [config.js](./config.js) | `loadConfig`, `withOverrides`, `secondsToMs`; environment settings without database/network access. |
| [db.js](./db.js) | `createPool`, `withTransaction`, `waitForDatabase`; pooling, commit/rollback and readiness retries. |
| [migrate.js](./migrate.js) | `migrate`, `checkSchema`, `REQUIRED_COLUMNS`; CLI applies SQL then exits. |
| [server.js](./server.js) | Executable startup, listener, cleanup and signal handling. |

- [Routes](./routes/README.md) define HTTP contracts.
- [Libraries](./lib/README.md) supply identity, validation, tokens and transport.
- [Migration SQL](../migrations/README.md) supplies runner inputs.

## Invariants

- `loadConfig` and `withOverrides` freeze top-level settings objects. Normal startup requires a signing key of at least 32 characters; the migration CLI explicitly skips that check.
- `createApp` limits JSON and URL-encoded bodies to 2.5 MB, disables `x-powered-by`, installs CORS before routes and ends with JSON 404/error handlers.
- `withTransaction` uses one dedicated connection, commits success, rolls back failure and releases the connection; row locks last until completion.
- `db.js` sets process-wide `pg` parsers: bigint becomes a number and date stays `YYYY-MM-DD`. IDs must remain within JavaScript's safe integer range.
- `migrate` takes advisory lock `74210315`, sorts SQL filenames, records normalized-line-ending SHA-256 checksums in `node_migrations`, rejects edited applied files and checks `REQUIRED_COLUMNS`. Each new file and its ledger insert share a transaction.
- `server.js` listens after readiness/migration, defaulting to `0.0.0.0:8000`; `shutdown` allows ten seconds before forced exit.

## Side Effects

- `createApp` creates limiters, sets process-wide password-hash concurrency and normally starts trusted-proxy DNS refresh.
- Pool operations contact PostgreSQL; migration reads disk, takes an advisory lock and changes schema/ledger rows.
- Importing `server.js` runs startup, opens a socket, schedules cleanup and deletes eligible expired records; it is not an inert app factory.

## Verification

- From `backend/`: `npm test`. `test/security.test.js` covers health, CORS, parsing and repeat migration; `test/hardening.test.js` checks checksum/schema failures. See [test isolation](../test/README.md).
