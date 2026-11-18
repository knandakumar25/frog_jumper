-- Player account deletion (owner decision, 2026-11-18; Apple and Google require it).
--
-- Deleting an account removes its accounts_user row and every row linked to it in one
-- transaction (src/lib/accountDeletion.js). The player's Clerk sign-in identity is deleted too,
-- through Clerk's API. If Clerk cannot be reached, the Clerk user id waits here and the hourly
-- cleanup retries it. The row holds only that id and retry bookkeeping, and is removed as soon
-- as Clerk confirms (or reports the user already gone).
CREATE TABLE IF NOT EXISTS accounts_clerkdeletion (
  clerk_user_id varchar(64) PRIMARY KEY,
  attempts integer NOT NULL DEFAULT 0,
  queued_at timestamptz NOT NULL DEFAULT now(),
  next_attempt_at timestamptz NOT NULL DEFAULT now(),
  last_error text
);
CREATE INDEX IF NOT EXISTS accounts_clerkdeletion_due_idx ON accounts_clerkdeletion (next_attempt_at);
