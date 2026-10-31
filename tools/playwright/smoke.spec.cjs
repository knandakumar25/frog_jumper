/** Browser verification of the running portal and its no-local-storage contract. */
const { test, expect } = require('@playwright/test');

test('portal renders and API responds without browser storage', async ({ page, request }) => {
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  // Block storage APIs before application code runs, so even attempted writes fail this test.
  await page.addInitScript(() => {
    Storage.prototype.setItem = function () { throw new Error('Browser persistence is disabled'); };
    Storage.prototype.getItem = function () { throw new Error('Browser persistence is disabled'); };
  });
  await page.goto(process.env.BASE_URL || 'http://website');
  await expect(page.locator('#root')).not.toBeEmpty();
  await expect(page.locator('body')).toContainText(/Axiom/i);
  const response = await request.get((process.env.API_URL || 'http://backend:8000/api') + '/auth/countries/');
  expect(response.ok()).toBeTruthy();
  expect(errors).toEqual([]);
  await page.reload();
  await expect(page.locator('#root')).not.toBeEmpty();
  expect(errors).toEqual([]);
});
