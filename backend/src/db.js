/**
 * PostgreSQL access: one connection pool plus a transaction helper.
 *
 * The tables are the ones Django created (`accounts_user` and friends), so ids are `bigint` and
 * birthdays are `date`. By default `pg` returns `bigint` as a string and turns `date` into a
 * JavaScript Date at local midnight, which shifts the day in some time zones. The type parsers
 * below return ids as numbers (they stay far below 2^53) and dates as their `YYYY-MM-DD` text,
 * matching the JSON Django produced.
 */
import pg from 'pg';

pg.types.setTypeParser(20, (value) => Number.parseInt(value, 10)); // int8 / bigint
pg.types.setTypeParser(1082, (value) => value); // date stays "YYYY-MM-DD"

/**
 * Create a connection pool for the configured database.
 * @param {{host: string, port: number, database: string, user: string, password: string}} db
 * @param {number} [max] pool size; tests that race two requests need at least two connections
 * @returns {pg.Pool}
 */
export function createPool(db, max = 10) {
  return new pg.Pool({ ...db, max, connectionTimeoutMillis: 5000 });
}

/**
 * Run `work` inside one transaction on a dedicated connection.
 *
 * Commits when `work` resolves and rolls back when it throws, then rethrows. Row locks taken with
 * `SELECT ... FOR UPDATE` inside `work` are held until the commit, which is what serializes the
 * browser sign-in confirm, poll and cancel calls against each other.
 * @template T
 * @param {pg.Pool} pool
 * @param {(client: pg.PoolClient) => Promise<T>} work
 * @returns {Promise<T>}
 */
export async function withTransaction(pool, work) {
  const client = await pool.connect();
  try {
    await client.query('BEGIN');
    const result = await work(client);
    await client.query('COMMIT');
    return result;
  } catch (error) {
    await client.query('ROLLBACK').catch(() => {});
    throw error;
  } finally {
    client.release();
  }
}

/**
 * Wait until PostgreSQL accepts connections (the old `entrypoint.sh` loop: 30 tries, 2 s apart).
 * @param {pg.Pool} pool
 * @param {{ tries?: number, delayMs?: number, log?: (line: string) => void }} [options]
 */
export async function waitForDatabase(pool, { tries = 30, delayMs = 2000, log = console.log } = {}) {
  for (let attempt = 1; ; attempt += 1) {
    try {
      await pool.query('SELECT 1');
      return;
    } catch (error) {
      if (attempt >= tries) throw new Error(`PostgreSQL did not answer after ${tries} tries: ${error.message}`);
      log('PostgreSQL not ready yet, retrying in 2 seconds...');
      await new Promise((resolve) => setTimeout(resolve, delayMs));
    }
  }
}
