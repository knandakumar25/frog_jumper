/** Showcase routes, responsive layout, keyboard access and URL context preservation. */
const { test, expect } = require('@playwright/test');
const { readFileSync } = require('node:fs');
const base = process.env.BASE_URL || 'http://website';
const root = process.env.CLIENT_SOURCE_DIR || '/website/src';
const routes = ['', '/gameplay', '/squad', '/faq', '/tech-hub', '/design-system', '/login', '/signup', '/account', '/account/2fa'];
const languages = ['en','es','vi','ja','ko','zh','zh_tw','fr','de','pt','it','pl','tr','ar','he','id','th'];
test.setTimeout(180000);

test('all supported languages contain matching showcase keys and placeholders', () => {
  const data = JSON.parse(readFileSync(root + '/translations/ui_translations.json', 'utf8'));
  const flatten = (value, prefix = '') => Object.entries(value).flatMap(([k,v]) => typeof v === 'object' ? flatten(v, prefix+k+'.') : [[prefix+k,v]]);
  const reference = flatten(data.en.showcase);
  for (const language of languages) {
    const entries = flatten(data[language].showcase);
    expect(entries.map(([key]) => key).sort()).toEqual(reference.map(([key]) => key).sort());
    for (const [key,value] of entries) {
      expect(value.trim(), `${language}.${key}`).not.toBe('');
      const original = reference.find(([k]) => k === key)[1];
      expect(value.match(/{{\w+}}/g) || []).toEqual(original.match(/{{\w+}}/g) || []);
    }
  }
  expect(data.en.showcase.explore).toBe('Explore gameplay');
});

for (const width of [360,768,1440]) {
  test(`every route fits at ${width}px with reduced motion`, async ({ page }) => {
    await page.setViewportSize({width,height:900});
    await page.emulateMedia({ reducedMotion:'reduce' });
    const errors=[];
    page.on('pageerror', e=>errors.push(e.message));
    for (const route of routes) {
      await page.goto(`${base}/en${route}`);
      await expect(page.locator('main h1').first()).toBeVisible();
      expect(await page.evaluate(()=>document.documentElement.scrollWidth <= innerWidth+1), route).toBe(true);
      await expect(page).toHaveTitle(/AXIOM FORCE/);
    }
    for (const route of ['/onboarding','/auth/browser-confirm']) {
      await page.goto(base+route);
      await expect(page.locator('main')).not.toBeEmpty();
      expect(await page.evaluate(()=>document.documentElement.scrollWidth <= innerWidth+1), route).toBe(true);
    }
    expect(errors).toEqual([]);
  });
}

test('primary journey, route focus, twelve missions and squad direct links', async ({page}) => {
  await page.goto(base+'/en');
  await page.getByRole('link',{name:'Explore gameplay',exact:true}).first().click();
  await expect(page).toHaveURL(/\/en\/gameplay$/);
  await expect(page.locator('main')).toBeFocused();
  await expect(page.locator('[data-mission]')).toHaveCount(12);
  for (const id of ['shogun','oceanus','apex','phoenicia','mirage','surya']) {
    await page.goto(base+'/en/squad?id='+id);
    await expect(page.locator('[data-operative]')).toHaveAttribute('data-operative',id);
    await expect(page.locator('.dossier img')).toBeVisible();
    expect(await page.locator('.dossier img').evaluate(img=>img.complete && img.naturalWidth>0)).toBe(true);
  }
});

test('keyboard menu, hidden focus, Escape and native accordion', async ({page}) => {
  await page.setViewportSize({width:360,height:800});
  await page.goto(base+'/en');
  const toggle=page.locator('.menu-toggle');
  await expect(page.locator('#mobile-menu')).toBeHidden();
  for(let i=0;i<12;i++) { await page.keyboard.press('Tab'); expect(await page.locator('#mobile-menu').evaluate(el=>el.contains(document.activeElement))).toBe(false); }
  await toggle.focus(); await page.keyboard.press('Enter');
  await expect(toggle).toHaveAttribute('aria-expanded','true');
  await page.keyboard.press('Tab');
  expect(await page.locator('#mobile-menu').evaluate(el=>el.contains(document.activeElement))).toBe(true);
  await page.keyboard.press('Escape');
  await expect(toggle).toBeFocused();
  await expect(page.locator('#mobile-menu')).toBeHidden();
  await page.goto(base+'/auth/browser-confirm?locale=en');
  await toggle.click();
  await page.locator('#mobile-menu select').selectOption('arabic');
  await expect(toggle).toBeFocused();
  await expect(page.locator('#mobile-menu')).toBeHidden();
  await page.goto(base+'/en/faq');
  const summary=page.locator('summary').first();
  await summary.focus(); await page.keyboard.press('Enter');
  await expect(page.locator('details').first()).toHaveAttribute('open','');
  await page.keyboard.press('Enter');
  await expect(page.locator('details').first()).not.toHaveAttribute('open','');
});

test('language changes preserve squad, nested Clerk routes and bare game request URLs', async ({page}) => {
  const id='11111111-2222-4333-8444-555555555555';
  await page.setViewportSize({width:1440,height:900});
  for(const path of [`/en/squad?id=surya`, `/en/login/game/${id}/factor-one?session_id=${id}`, `/auth/browser-confirm?session_id=${id}&locale=en&intent=signup`, '/onboarding?locale=en']) {
    await page.goto(base+path);
    await page.locator('.desktop-account select').selectOption('german');
    await expect.poll(()=>new URL(page.url()).pathname).toBe(path.startsWith('/en/') ? path.split('?')[0].replace('/en/','/de/') : path.split('?')[0]);
    if(path.includes('session_id')) expect(new URL(page.url()).searchParams.get('session_id')).toBe(id);
    if(path.includes('id=surya')) expect(new URL(page.url()).searchParams.get('id')).toBe('surya');
    if(path.includes('intent=')) expect(new URL(page.url()).searchParams.get('intent')).toBe('signup');
  }
});

test('RTL and long text routes reflow at 200 percent zoom', async ({page}) => {
  // A 720px CSS viewport models a 1440px window at 200% browser zoom.
  await page.setViewportSize({width:720,height:450});
  for(const lang of ['ar','he','de']) {
    for(const route of routes) {
      await page.goto(`${base}/${lang}${route}`);
      await expect(page.locator('main h1').first()).toBeVisible();
      await expect(page.locator('html')).toHaveAttribute('dir',lang==='de'?'ltr':'rtl');
      expect(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),lang+route).toBe(true);
    }
  }
});

test('public art loads and showcase contains no continuous animation', async ({page}) => {
  await page.goto(base+'/en');
  await page.locator('.site-footer').scrollIntoViewIfNeeded();
  await expect.poll(()=>page.locator('main img').evaluateAll(images=>images.every(img=>img.complete&&img.naturalWidth>0))).toBe(true);
  expect(await page.evaluate(()=>document.getAnimations().filter(a=>a.effect?.getTiming().iterations===Infinity).length)).toBe(0);
});
