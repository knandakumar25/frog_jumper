# Accounts Schema Migrations

## Overview

Ordered SQL migrations support Django-created account tables and fresh PostgreSQL databases. [The runner](../src/migrate.js) applies them before the API listens.

## Interface

| File | Schema contract |
| --- | --- |
| [0001_schema.sql](./0001_schema.sql) | Creates absent `accounts_user`, `accounts_browsersession`, `accounts_pendingregistration`, `accounts_twofactorcode` tables and indexes. Existing tables are skipped. |
| [0002_hardening.sql](./0002_hardening.sql) | Adds code `attempts`; creates `accounts_refreshtoken` with unique `jti`, user, expiry and redemption time. |
| [0003_token_families.sql](./0003_token_families.sql) | Adds refresh `family`, fills old rows from `jti`, makes it non-null and indexes it. |
| [0004_account_deletion.sql](./0004_account_deletion.sql) | Creates `accounts_clerkdeletion` (Clerk user id, attempts, queued/next-attempt times, last error): the retry queue for deleting a deleted account's Clerk identity. Added 2026-11-18. |
| [0005_game_saves.sql](./0005_game_saves.sql) | Drops the old global `game_state`/`game_sessions` tables (old saves discarded on the owner's instruction, after a backup) and creates per-account saves: `game_slot` (owned by `user_id`, `revision`), `game_slot_doc`, `game_slot_receipt` and `game_account_doc`. Added 2026-11-18. |

## Invariants

- `0001_schema.sql` checks `to_regclass` per table; existing columns/indexes are not upgraded. `checkSchema` rejects missing required columns afterward.
- Fresh users have unique username, email and nullable Clerk ID; browser UUID and pending token hash/Clerk ID are unique. Pending registration email is not unique (`0001_schema.sql`).
- Fresh browser sessions/codes use user deletion cascades; pending registration browser links use `ON DELETE SET NULL`. Existing Django tables retain their own constraints (`0001_schema.sql`).
- Refresh ownership cascades on user deletion; `family` is non-null after `0003_token_families.sql`.
- Account deletion (`src/lib/accountDeletion.js`) deletes child rows explicitly before the user row, so it also works on Django-made tables whose foreign keys have no ON DELETE action.
- Applied SQL is immutable: the runner rejects changed checksums. Add a new ordered SQL file for later changes (`src/migrate.js:migrate`).

## Side Effects

- Files create/alter tables/indexes; `0003_token_families.sql` updates existing refresh rows. The runner records checksums/application state in `node_migrations` and takes an advisory lock.
- `0001_schema.sql` does not create Django administration/permission tables, which this API does not use.

## Verification

- From `backend/`: `npm test`; table defaults/uniqueness, repeat migration and checksum/schema failures run against `accounts_test`.
- `npm run migrate` changes the configured database; it is an operational command, not a read-only check.
