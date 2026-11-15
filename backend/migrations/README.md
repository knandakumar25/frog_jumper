# Accounts Schema Migrations

## Overview

Ordered SQL migrations support Django-created account tables and fresh PostgreSQL databases. [The runner](../src/migrate.js) applies them before the API listens.

## Interface

| File | Schema contract |
| --- | --- |
| [0001_schema.sql](./0001_schema.sql) | Creates absent `accounts_user`, `accounts_browsersession`, `accounts_pendingregistration`, `accounts_twofactorcode` tables and indexes. Existing tables are skipped. |
| [0002_hardening.sql](./0002_hardening.sql) | Adds code `attempts`; creates `accounts_refreshtoken` with unique `jti`, user, expiry and redemption time. |
| [0003_token_families.sql](./0003_token_families.sql) | Adds refresh `family`, fills old rows from `jti`, makes it non-null and indexes it. |

## Invariants

- `0001_schema.sql` checks `to_regclass` per table; existing columns/indexes are not upgraded. `checkSchema` rejects missing required columns afterward.
- Fresh users have unique username, email and nullable Clerk ID; browser UUID and pending token hash/Clerk ID are unique. Pending registration email is not unique (`0001_schema.sql`).
- Fresh browser sessions/codes use user deletion cascades; pending registration browser links use `ON DELETE SET NULL`. Existing Django tables retain their own constraints (`0001_schema.sql`).
- Refresh ownership cascades on user deletion; `family` is non-null after `0003_token_families.sql`.
- Applied SQL is immutable: the runner rejects changed checksums. Add a new ordered SQL file for later changes (`src/migrate.js:migrate`).

## Side Effects

- Files create/alter tables/indexes; `0003_token_families.sql` updates existing refresh rows. The runner records checksums/application state in `node_migrations` and takes an advisory lock.
- `0001_schema.sql` does not create Django administration/permission tables, which this API does not use.

## Verification

- From `backend/`: `npm test`; table defaults/uniqueness, repeat migration and checksum/schema failures run against `accounts_test`.
- `npm run migrate` changes the configured database; it is an operational command, not a read-only check.
