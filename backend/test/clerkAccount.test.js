import assert from 'node:assert/strict';
import { it } from 'node:test';
import request from 'supertest';
import { makeApp, makeUser, pool, useDatabase } from './helpers.js';
useDatabase();

it('restores only the verified linked account without issuing API or game credentials', async () => {
  const user = await makeUser({username:'linked',email:'linked@example.com',clerk_user_id:'user_linked'});
  await makeUser({username:'other',email:'other@example.com',clerk_user_id:'user_other'});
  const {app,deps} = makeApp();
  deps.clerk.subject = async token => { assert.equal(token,'proof'); return 'user_linked'; };
  const result = await request(app).get('/api/auth/profile/').set('Authorization','Clerk proof');
  assert.equal(result.status,200); assert.equal(result.body.id,user.id);
  assert.equal(result.body.access,undefined); assert.equal(result.body.refresh,undefined);
  assert.equal(Number((await pool.query('SELECT count(*) FROM accounts_browsersession')).rows[0].count),0);
  assert.equal(Number((await pool.query('SELECT count(*) FROM accounts_refreshtoken')).rows[0].count),0);
  const changed = await request(app).patch('/api/auth/profile/').set('Authorization','Clerk proof').send({first_name:'Updated'});
  assert.equal(changed.status,200); assert.equal(changed.body.first_name,'Updated');
});

it('rejects invalid proof and malformed subjects before account lookup', async () => {
  const {app,deps} = makeApp();
  deps.clerk.subject = async () => { throw new Error('invalid'); };
  assert.equal((await request(app).get('/api/auth/profile/').set('Authorization','Clerk forged')).status,401);
  assert.equal((await request(app).get('/api/auth/profile/').set('Authorization','Clerk')).status,401);
  deps.clerk.subject = async () => 'not_a_user';
  assert.equal((await request(app).get('/api/auth/profile/').set('Authorization','Clerk proof')).status,401);
});

it('distinguishes missing onboarding from an inactive linked account', async () => {
  const {app,deps} = makeApp();
  deps.clerk.subject = async () => 'user_missing';
  const missing = await request(app).get('/api/auth/profile/').set('Authorization','Clerk proof');
  assert.equal(missing.status,403); assert.equal(missing.body.error,'onboarding_required');
  const user = await makeUser({username:'inactive',email:'inactive@example.com',clerk_user_id:'user_missing'});
  await pool.query('UPDATE accounts_user SET is_active = false WHERE id = $1',[user.id]);
  assert.equal((await request(app).get('/api/auth/profile/').set('Authorization','Clerk proof')).status,401);
});

it('keeps email changes and game 2FA behind mailed confirmation codes with Clerk proof', async () => {
  await makeUser({username:'secured',email:'secured@example.com',clerk_user_id:'user_secured'});
  const {app,deps,outbox} = makeApp(); deps.clerk.subject = async () => 'user_secured';
  const call = (path,data) => request(app).post('/api/auth/'+path).set('Authorization','Clerk proof').send(data);
  assert.equal((await request(app).patch('/api/auth/profile/').set('Authorization','Clerk proof').send({email:'changed@example.com'})).status,400);
  assert.equal((await call('2fa/enable/',{})).status,200);
  assert.equal(outbox().length,1);
  assert.equal((await call('2fa/verify-enable/',{code:'wrong'})).status,400);
});
