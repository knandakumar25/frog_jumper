# Accounts API Tests

## Overview

Node's built-in test runner and Supertest verify the Express API, transport contracts and PostgreSQL concurrency behavior. This test directory is explicitly included in the documentation scope.

## Interface

- [helpers.js](./helpers.js): `useDatabase` registers lifecycle hooks; `resetDatabase` clears account rows; `makeApp` supplies memory mail/fake Clerk; `makeUser` creates fixtures; exports `pool`, `baseConfig`, `TEST_DB`, `SIGNING_KEY`.
- [views.test.js](./views.test.js): registration, login, profile, country lists, validators and table defaults/uniqueness.
- [security.test.js](./security.test.js): Django hash compatibility, codes, token typing/rotation, generic login failures, service/error behavior and migration repeatability.
- [hardening.test.js](./hardening.test.js): throttles, proxy addresses, emailed security confirmation, refresh families/cleanup and migration integrity.
- [browserAuth.test.js](./browserAuth.test.js): secret proof, approval, expiry/cancel, Clerk signature claims, onboarding handoff and parallel redemption.
- [onboarding.test.js](./onboarding.test.js): start/resend/mail failures, validation/completion, country/date rules, conflicts and stale-token tolerance.
- [authConcurrency.test.js](./authConcurrency.test.js): consumed-successor code supersession, concurrent issuance caps, ancestor replay and revoke-all during rotation.
- [email.test.js](./email.test.js): welcome/code mail through memory transport.
- [clerkClient.test.js](./clerkClient.test.js), [mailTransport.test.js](./mailTransport.test.js): injected HTTP calls and transport failure/payload mapping without a database.

## Invariants

- `package.json:test` runs `node --test --test-concurrency=1 test/`; database files must run serially because each recreates the same `accounts_test` database.
- `helpers.js:useDatabase` uses the configured PostgreSQL server/admin database to create and drop only `accounts_test`. Per-test hooks truncate account tables and restart identities.
- `makeApp` installs `MemoryTransport` and Clerk functions that throw unless replaced. `baseConfig` disables real Clerk/Resend credentials and lowers password rounds.
- Transport tests inject requests; browser signature checks use local RSA keys. These tests do not establish real Clerk/Resend availability.

## Side Effects

- Database suites force-drop/create `accounts_test`, apply migrations, insert/update/delete fixtures, and drop the database after each file. They require create/drop privileges.
- Supertest opens temporary HTTP listeners. Tests can temporarily replace fetch or logging and restore them; fake delivery captures mail in memory.

## Verification

- From `backend/`: `npm test` (Node >=26 and installed development dependencies, PostgreSQL reachable).
- Focused transport check: `node --test test/clerkClient.test.js test/mailTransport.test.js`.
- From repository root using the rebuilt development image: `docker compose exec -T backend npm test`.
