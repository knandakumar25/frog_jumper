/** Real Clerk development-account flows against the isolated PostgreSQL/mail test server. */
const { test, expect } = require('@playwright/test');
const { clerkSetup, setupClerkTestingToken, clerk } = require('@clerk/testing/playwright');
const { createClerkClient } = require('@clerk/backend');

const base = process.env.BASE_URL || 'http://website';
const backend = 'http://backend:8002';
const users = [];
let client;
test.describe.configure({ mode: 'default' });
test.setTimeout(120000);
test.use({ actionTimeout: 15000 });
test.afterEach(async ({ page }, info) => {
  // UI text excludes input values and tokens; keep diagnostics bounded and ephemeral.
  if (info.status !== info.expectedStatus) console.log('Browser path/UI:', new URL(page.url()).pathname, (await page.locator('main').innerText()).slice(0,3500));
});

test.beforeAll(async () => {
  if (!process.env.CLERK_SECRET_KEY?.startsWith('sk_test_') || !process.env.CLERK_PUBLISHABLE_KEY?.startsWith('pk_test_'))
    throw new Error('Only a configured Clerk development instance may run these tests');
  await clerkSetup();
  client = createClerkClient({ secretKey: process.env.CLERK_SECRET_KEY });
});

test.afterAll(async () => {
  // Cleanup runs even after a failed assertion; only accounts created here are removed.
  if (client) {
    const results = await Promise.allSettled(users.map(id => client.users.deleteUser(id)));
    const failed = results.filter(result => result.status === 'rejected').length;
    if (failed) throw new Error(`Cleanup could not remove ${failed} disposable Clerk users`);
  }
});

/** Route all browser API traffic to the ephemeral test database, preserving the website/Clerk origin. */
async function prepare(page) {
  await setupClerkTestingToken({ page });
  await page.route('**/api/**', async route => {
    const url = new URL(route.request().url());
    const response = await route.fetch({ url: backend + url.pathname + url.search });
    await route.fulfill({ response });
  });
  await page.addInitScript(() => {
    // Authentication code must never save access/refresh tokens or device secrets.
    const original = Storage.prototype.setItem;
    Storage.prototype.setItem = function(key, value) {
      if (/access.?token|refresh.?token|redemption|device.?secret/i.test(key) ||
          /eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+/.test(String(value)))
        throw new Error('Credential persistence attempted');
      return original.call(this, key, value);
    };
  });
}

/** Create a pending device request without logging or saving private response fields. */
async function device(request, intent = 'login') {
  const response = await request.post(backend + '/api/auth/browser-start/', { data: { intent, locale: 'en' } });
  expect(response.ok()).toBeTruthy();
  return response.json();
}

/** Visit the site's real confirmation and embedded Clerk sign-in screens. */
async function openRequest(page, data) {
  await page.goto(base + new URL(data.auth_url).pathname + new URL(data.auth_url).search);
  await clerk.loaded({ page });
  await expect(page.getByTestId('browser-loading')).not.toBeVisible();
  const link = page.getByTestId('browser-signin');
  if (await link.isVisible()) await link.click();
}

/** Complete each Clerk-rendered email/phone/MFA challenge with reserved test OTPs. */
async function finishVerification(page) {
  for (let step = 0; step < 4; step++) {
    await expect.poll(async () => {
      if (new URL(page.url()).pathname === '/auth/browser-confirm') return 'done';
      return await page.locator('input[autocomplete="one-time-code"]').count() ? 'code' : 'waiting';
    }, { timeout: 20000 }).not.toBe('waiting');
    if (new URL(page.url()).pathname === '/auth/browser-confirm') return;
    // Clerk can split a code into multiple fields. Fill each visible cell when
    // present; filling only the first cell leaves phone/MFA verification pending.
    const fields = page.locator('input[autocomplete="one-time-code"]');
    const count = await fields.count();
    try {
      if (count === 1) await fields.fill('424242', { timeout: 3000 });
      else for (let index = 0; index < count; index++) await fields.nth(index).fill('424242'[index], { timeout: 3000 });
      const submit = page.getByRole('button', { name: /^continue$/i });
      if (await submit.isVisible()) await submit.click();
    } catch (error) {
      // Clerk can complete a challenge and redirect between observing its input
      // and filling it. A completed redirect wins over the now-removed input.
      if (new URL(page.url()).pathname === '/auth/browser-confirm') return;
      throw error;
    }
    await page.waitForTimeout(1500);
  }
  await expect(page).toHaveURL(/browser-confirm/);
}

/** Exercise Clerk's existing credential UI and any configured verification steps. */
async function signInFromForm(page, email, password) {
  await page.locator('input[name="identifier"]').fill(email);
  await page.getByRole('button', { name: /^continue$/i }).click();
  await page.locator('input[name="password"]').fill(password);
  await page.getByRole('button', { name: /^continue$/i }).click();
  await finishVerification(page);
  await expect(page.getByTestId('authorize-game')).toBeVisible();
}

/** Read only this disposable account's most recent code from the in-memory mail capture. */
async function capturedCode(request, email) {
  const messages = await (await request.get(backend + '/__test__/mail')).json();
  const message = [...messages].reverse().find(msg => msg.to.includes(email) && /code.*\d{6}/i.test(msg.body));
  if (!message) throw new Error('Expected captured verification mail was missing');
  return message.body.match(/\b\d{6}\b/)[0];
}

/** Preserve the page-lifetime auth context while exercising another existing SPA route. */
async function navigateWithinSite(page, path) {
  await page.evaluate(path => {
    window.history.pushState({}, '', path);
    window.dispatchEvent(new PopStateEvent('popstate'));
  }, path);
}

test('actual Clerk login, already-signed-in approval, switching and single-use redemption', async ({ page, request }) => {
  await prepare(page);
  const stamp = Date.now();
  const email = `handoff${stamp}+clerk_test@example.com`;
  const password = 'BrowserTest-Only#83Pass';
  const user = await client.users.createUser({ emailAddress: [email], phoneNumber: ['+12015550123'], password, username: `handoff${stamp}` });
  users.push(user.id);
  expect((await request.post(backend + '/__test__/seed', { data: { id: user.id, email, username: `handoff${stamp}` } })).ok()).toBeTruthy();
  const data = await device(request);
  await openRequest(page, data);
  await expect(page.locator('[data-handoff-target]')).toHaveAttribute('data-handoff-target', new RegExp(data.session_id));
  await signInFromForm(page, email, password);
  // Ordinary website account access is restored from verified Clerk proof after
  // a full reload, without issuing API tokens or approving the pending game.
  await page.goto(base + '/en/account');
  await expect(page.locator('input[name="email"]')).toHaveValue(email);
  await page.reload();
  await expect(page.locator('input[name="email"]')).toHaveValue(email);
  expect((await (await request.post(backend + '/api/auth/browser-poll/', { data })).json()).status).toBe('pending');
  await page.goto(base + `/auth/browser-confirm?session_id=${data.session_id}`);
  await expect(page.getByTestId('authorize-game')).toBeVisible();
  expect((await request.post(backend + '/api/auth/browser-poll/', { data })).status()).toBe(200);
  await page.getByTestId('authorize-game').click();
  await expect(page.getByTestId('browser-authorized')).toBeVisible();
  const result = await request.post(backend + '/api/auth/browser-poll/', { data });
  expect((await result.json()).user.username).toBe(`handoff${stamp}`);
  expect((await request.post(backend + '/api/auth/browser-poll/', { data })).status()).toBe(410);
  if (process.env.LIVE_GAME_SESSION_ID) {
    // The external client owns the private proof. Approve only its public handle,
    // leaving redemption and any game-process UI verification to that client.
    await page.goto(base + `/auth/browser-confirm?session_id=${process.env.LIVE_GAME_SESSION_ID}`);
    await page.getByTestId('authorize-game').click();
    await expect(page.getByTestId('browser-authorized')).toBeVisible();
    console.log('External game request approved for disposable account');
  }
  const again = await device(request);
  await openRequest(page, again);
  await expect(page.getByTestId('authorize-game')).toBeVisible();
  await page.getByTestId('switch-account').click();
  await expect(page).toHaveURL(new RegExp(`login/game/${again.session_id}`));
  const secondEmail = `handoff${stamp}b+clerk_test@example.com`;
  const second = await client.users.createUser({ emailAddress: [secondEmail], phoneNumber: ['+12015550124'], password, username: `handoff${stamp}b` });
  users.push(second.id);
  expect((await request.post(backend + '/__test__/seed', { data: { id: second.id, email: secondEmail, username: second.username } })).ok()).toBeTruthy();
  await signInFromForm(page, secondEmail, password);
  await page.getByTestId('authorize-game').click();
  await expect(page.getByTestId('browser-authorized')).toBeVisible();
  const switched = await (await request.post(backend + '/api/auth/browser-poll/', { data: again })).json();
  expect(switched.user.username).toBe(second.username);
  const cancelled = await device(request);
  await openRequest(page, cancelled);
  expect((await request.post(backend + '/api/auth/browser-cancel/', { data: cancelled })).ok()).toBeTruthy();
  await page.getByTestId('authorize-game').click();
  await expect(page.getByRole('alert')).toBeVisible();
});

test('closing an unauthenticated browser leaves only a cancellable pending request', async ({ browser, request }) => {
  const data = await device(request);
  const context = await browser.newContext();
  const page = await context.newPage();
  await prepare(page);
  await openRequest(page, data);
  await context.close();
  expect((await (await request.post(backend + '/api/auth/browser-poll/', { data })).json()).status).toBe('pending');
  expect((await request.post(backend + '/api/auth/browser-cancel/', { data })).status()).toBe(200);
  expect((await request.post(backend + '/api/auth/browser-poll/', { data })).status()).toBe(410);
});

test('fresh Clerk signup, verification, captured onboarding email opened in another tab, explicit approval', async ({ page, request, browser }) => {
  await prepare(page);
  const data = await device(request, 'signup');
  const stamp = Date.now();
  const email = `signup${stamp}+clerk_test@example.com`;
  let emailContext;
  try {
    await openRequest(page, data);
    await expect(page.locator('[data-handoff-target]')).toHaveAttribute('data-handoff-target', new RegExp(data.session_id));
    await expect(page.locator('input[name="emailAddress"]')).toBeVisible();
    const username = page.locator('input[name="username"]');
    if (await username.isVisible()) await username.fill(`signup${stamp}`);
    const first = page.locator('input[name="firstName"]');
    if (await first.isVisible()) await first.fill('Browser');
    const last = page.locator('input[name="lastName"]');
    if (await last.isVisible()) await last.fill('Test');
    await page.locator('input[name="emailAddress"]').fill(email);
    const phone = page.locator('input[name="phoneNumber"]');
    if (await phone.isVisible()) await phone.fill('2015550147');
    await page.locator('input[name="password"]').fill('BrowserSignup-Only#83Pass');
    await page.getByRole('button', { name: /^continue$/i }).click();
    await finishVerification(page);
    await expect(page).toHaveURL(/browser-confirm/, { timeout: 45000 });
    await page.getByTestId('authorize-game').click();
    await expect(page).toHaveURL(/onboarding\?session_id=/);
    await expect(page.getByText(/We sent a link/)).toBeVisible();
    const messages = await (await request.get(backend + '/__test__/mail')).json();
    const message = messages.find(msg => msg.to.includes(email) && msg.body.includes('/onboarding?token='));
    expect(message).toBeTruthy();
    const link = message.body.match(/https?:\/\/[^\s<>]+\/onboarding\?token=[\w-]+/)[0];
    // A separate context proves the emailed link works without the first tab's
    // Clerk cookies; the completed onboarding's in-memory Django session approves.
    emailContext = await browser.newContext();
    const other = await emailContext.newPage();
    await prepare(other);
    const url = new URL(link);
    await other.goto(base + url.pathname + url.search);
    await other.locator('select[name="country"]').selectOption('Japan');
    await other.locator('input[name="date_of_birth"]').fill('1995-04-12');
    await other.getByRole('button', { name: /^create account$/i }).click();
    await expect(other.getByTestId('authorize-game')).toBeVisible();
    const pending = await (await request.post(backend + '/api/auth/browser-poll/', { data })).json();
    expect(pending.status).toBe('pending');
    await other.getByTestId('authorize-game').click();
    await expect(other.getByTestId('browser-authorized')).toBeVisible();
    const profile = await (await request.post(backend + '/api/auth/browser-poll/', { data })).json();
    expect(profile.user.email).toBe(email);
    // Onboarding retains its API session in memory. Use the real account screens,
    // without inventing a password-login UI or persisting credentials on reload.
    await navigateWithinSite(other, '/en-US/account/2fa');
    await other.getByRole('button', { name: /Enable Two.Factor Authentication/i }).click();
    await expect(other.locator('form input')).toBeVisible();
    let otp = await capturedCode(request, email);
    await other.locator('form input').fill(otp === '000000' ? '111111' : '000000');
    await other.getByRole('button', { name: /Verify.*Enable/i }).click();
    await expect(other.getByText('Invalid code or verification failed.', { exact: true })).toBeVisible();
    await other.locator('form input').fill(otp);
    await other.getByRole('button', { name: /Verify.*Enable/i }).click();
    await expect(other.getByText('2FA is currently enabled on your account.', { exact: true })).toBeVisible();
    await other.getByRole('button', { name: 'Disable 2FA', exact: true }).click();
    await expect(other.locator('form input')).toBeVisible();
    otp = await capturedCode(request, email);
    await other.locator('form input').fill(otp);
    await other.getByRole('button', { name: 'Disable 2FA', exact: true }).click();
    await expect(other.getByRole('button', { name: /Enable Two.Factor Authentication/i })).toBeVisible();
    await other.getByRole('button', { name: 'Back to My Account', exact: true }).click();
    await expect(other.locator('input[name="email"]')).toHaveValue(email);
    const changedEmail = `signup${stamp}changed+clerk_test@example.com`;
    await other.locator('input[name="email"]').fill(changedEmail);
    await other.getByRole('button', { name: 'Save Changes', exact: true }).click();
    await expect(other.locator('input[name="email_code"]')).toBeVisible();
    otp = await capturedCode(request, email);
    await other.locator('input[name="email_code"]').fill(otp === '000000' ? '111111' : '000000');
    await other.getByRole('button', { name: 'Save Changes', exact: true }).click();
    await expect(other.getByText(/Could not save your profile\./)).toBeVisible();
    await other.locator('input[name="email_code"]').fill(otp);
    await other.getByRole('button', { name: 'Save Changes', exact: true }).click();
    await expect(other.getByText(/Profile saved\./)).toBeVisible();
    await expect(other.locator('input[name="email"]')).toHaveValue(changedEmail);
    const notices = await (await request.get(backend + '/__test__/mail')).json();
    expect(notices.some(msg => msg.to.includes(email) && msg.body.includes(changedEmail))).toBeTruthy();
  } finally {
    if (emailContext) await emailContext.close();
    // Lookup is scoped to the generated test address, even if signup failed midway.
    const result = await client.users.getUserList({ emailAddress: [email] });
    for (const user of result.data) if (!users.includes(user.id)) users.push(user.id);
  }
});
