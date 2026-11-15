# Browser Test Tools

## Overview

These executables provide an isolated API for the browser sign-in workflow and a proxy for a desktop game process. They are separate from normal production startup.

## Interface

- [e2e-server.js](./e2e-server.js) starts port 8002, mounts the real API and adds `GET /__test__/ready`, `GET /__test__/mail`, `POST /__test__/seed`, `POST /__test__/expire-codes`, `POST /__test__/stop`.
- [e2e-proxy.js](./e2e-proxy.js) listens on port 8002 and forwards `/api/` paths to port 8002 on `E2E_TARGET_HOST` (default `backend`).

## Invariants

- `e2e-server.js` requires a Clerk development secret beginning `sk_test_` and uses fixed database `test_browser_handoff`, separate from the configured normal database.
- `expire-codes` requires a username beginning `browsercheck` and a supported purpose (`opt_in`, `login`, `opt_out`, `email_change`); it ages matching code timestamps by eleven minutes. The isolated API overrides `authPerIpPerMinute` to 1000 for lifecycle cases; normal rate-limit behavior has separate tests.
- `seed` accepts only addresses ending `@example.com` containing `+clerk_test`; existing Clerk-linked fixtures are reused.
- `e2e-proxy.js` rejects paths outside `/api/`, keeping `/__test__/` routes out of its forwarded interface. It forwards method, URL, body, Content-Type, Authorization and Content-Length, not arbitrary headers.
- The proxy binds `0.0.0.0`; host loopback restriction depends on launch-time port publishing, not that bind address. Test-only routes on the isolated server are not authenticated.

## Side Effects

- `resetDatabase` force-drops and recreates `test_browser_handoff` at startup; `stop` closes pool/server and drops it again. SIGINT/SIGTERM invoke `stop`.
- The isolated API applies migrations, creates fixture accounts, retains email in a `MemoryTransport`, and can contact the configured Clerk development instance.
- Both scripts open listeners. Proxy calls contact the upstream with a fifteen-second timeout and return 502 on connection failure.

## Verification

- From `backend/`: `npm test` covers the shared app, not a live Clerk browser run. The opt-in integration workflow is described in repository file `tools/playwright/README.md`.
- Running `e2e-server.js` is a destructive reset of the fixed disposable database and requires suitable PostgreSQL privileges; it is not part of the default unit-test command.
