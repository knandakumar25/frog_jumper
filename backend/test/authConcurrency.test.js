/** Regression checks for superseded codes, concurrent issuance and refresh-family revocation. */
import assert from 'node:assert/strict';
import { it } from 'node:test';
import { createCode, verifyCode } from '../src/lib/twoFactor.js';
import { issueTokenPair, rotateRefreshToken, revokeAllRefreshTokens } from '../src/lib/tokens.js';
import { baseConfig, makeUser, pool, useDatabase } from './helpers.js';

useDatabase();
const account = () => makeUser({ username: 'racefrog', email: 'race@example.com', country: 'Japan' });

it('never resurrects a superseded code after consuming its successor', async () => {
  const user = await account();
  await createCode(pool, user.id, 'email_change', baseConfig);
  await createCode(pool, user.id, 'email_change', baseConfig);
  // Fixed distinct codes avoid the one-in-a-million random collision in this regression.
  const rows = (await pool.query('SELECT id FROM accounts_twofactorcode ORDER BY id')).rows;
  await pool.query("UPDATE accounts_twofactorcode SET code = '111111' WHERE id = $1", [rows[0].id]);
  await pool.query("UPDATE accounts_twofactorcode SET code = '222222' WHERE id = $1", [rows[1].id]);
  const check = (code) => verifyCode(pool, baseConfig, { userId: user.id, purpose: 'email_change', code });
  assert.equal(await check('222222'), true);
  assert.equal(await check('111111'), false);
  assert.equal(await check('222222'), false);
});

it('enforces the issuance cap across simultaneous requests', async () => {
  const user = await account();
  const config = { ...baseConfig, codesPerUserPer15Min: 2 };
  const outcomes = await Promise.allSettled(Array.from({ length: 12 }, () => createCode(pool, user.id, 'opt_in', config)));
  assert.equal(outcomes.filter((result) => result.status === 'fulfilled').length, 2);
  for (const result of outcomes.filter((result) => result.status === 'rejected')) assert.equal(result.reason.status, 429);
  assert.equal((await pool.query('SELECT count(*)::int AS n FROM accounts_twofactorcode')).rows[0].n, 2);
});

/** Pause a successor insertion while a competing revocation reaches a database lock. */
async function duringRotation(user, competitor) {
  const first = await issueTokenPair(pool, baseConfig, user.id);
  const second = await rotateRefreshToken(pool, baseConfig, first.refresh);
  let release;
  let reached;
  const held = new Promise((resolve) => { release = resolve; });
  const inserted = new Promise((resolve) => { reached = resolve; });
  const wrapped = { async connect() {
    const client = await pool.connect();
    return { release: () => client.release(), async query(sql, values) {
      if (sql.includes('INSERT INTO accounts_refreshtoken')) { reached(); await held; }
      return client.query(sql, values);
    } };
  } };
  const rotation = rotateRefreshToken(wrapped, baseConfig, second.refresh);
  await inserted;
  // Attach rejection handling immediately so a refused replay is never unhandled.
  const competing = competitor(first).then(() => null, (error) => error);
  try {
    let blocked = false;
    for (let attempt = 0; attempt < 100 && !blocked; attempt++) {
      blocked = (await pool.query(`SELECT EXISTS (SELECT 1 FROM pg_stat_activity
        WHERE datname = current_database() AND wait_event_type = 'Lock') AS waiting`)).rows[0].waiting;
      if (!blocked) await new Promise((resolve) => setTimeout(resolve, 5));
    }
    assert.equal(blocked, true, 'the competing request must reach the database before insertion resumes');
  } finally { release(); }
  const next = await rotation;
  const outcome = await competing;
  assert.equal((await pool.query('SELECT count(*)::int AS n FROM accounts_refreshtoken WHERE used_at IS NULL')).rows[0].n, 0);
  await assert.rejects(rotateRefreshToken(pool, baseConfig, next.refresh), (error) => error.status === 401);
  return outcome;
}

it('ancestor replay revokes a successor being rotated concurrently', async () => {
  const user = await account();
  const result = await duringRotation(user, (first) => rotateRefreshToken(pool, baseConfig, first.refresh));
  assert.equal(result.status, 401);
});

it('revoke-all also catches the successor of an in-flight rotation', async () => {
  const user = await account();
  assert.equal(await duringRotation(user, () => revokeAllRefreshTokens(pool, user.id)), null);
});
