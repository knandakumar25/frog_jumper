-- Refresh-token families (2026-11-15 EGM review, issue 6).
--
-- A family is one sign-in and every token rotated from it. Replaying a refresh token that was
-- already used revokes its whole family, so a copied token cannot keep a session alive.
-- Tokens issued before this migration each become their own family.
ALTER TABLE accounts_refreshtoken ADD COLUMN IF NOT EXISTS family uuid;
UPDATE accounts_refreshtoken SET family = jti WHERE family IS NULL;
ALTER TABLE accounts_refreshtoken ALTER COLUMN family SET NOT NULL;
CREATE INDEX IF NOT EXISTS accounts_refreshtoken_family_idx ON accounts_refreshtoken (family);
