/** Run the actual website Axios client against an HTTP server that expires concurrent requests. */
const { test, expect } = require('@playwright/test');
const { readFileSync } = require('node:fs');
const { pathToFileURL } = require('node:url');
const http = require('node:http');
const root = process.env.CLIENT_SOURCE_DIR || '/website/src';

test('website client shares one refresh for concurrent expired requests', async () => {
  let initial = 0;
  let refreshes = 0;
  const waiting = [];
  const server = http.createServer((req, res) => {
    const answer = (status, body) => { res.writeHead(status, { 'Content-Type': 'application/json' }); res.end(JSON.stringify(body)); };
    if (req.url.endsWith('/token/refresh/')) {
      refreshes++;
      answer(200, { access: 'fresh-access', refresh: 'rotated-refresh' });
    } else if (req.headers.authorization === 'Bearer fresh-access') answer(200, { ok: true });
    else {
      initial++;
      waiting.push(() => answer(401, { detail: 'expired' }));
      // Release both expired responses together, ensuring both requests overlap.
      if (initial === 2) waiting.splice(0).forEach(send => send());
    }
  });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  try {
    const endpoint = `http://127.0.0.1:${server.address().port}/api`;
    // Resolve imports and Vite's environment substitution only; client logic is unmodified.
    const tokenUrl = 'data:text/javascript;base64,' + Buffer.from(readFileSync(root + '/api/tokens.js', 'utf8')).toString('base64');
    const source = readFileSync(root + '/api/client.js', 'utf8')
      .replace("'axios'", JSON.stringify(pathToFileURL(require.resolve('axios')).href))
      .replace("'./tokens'", JSON.stringify(tokenUrl))
      .replace('import.meta.env.VITE_API_BASE_URL', JSON.stringify(endpoint));
    const { tokens } = await import(tokenUrl);
    const { default: client } = await import('data:text/javascript;base64,' + Buffer.from(source).toString('base64'));
    tokens.access = 'expired-access';
    tokens.refresh = 'original-refresh';
    const responses = await Promise.all([client.get('/auth/profile/'), client.get('/auth/countries/')]);
    expect(responses.every(response => response.data.ok)).toBe(true);
    expect(initial).toBe(2);
    expect(refreshes).toBe(1);
    expect(tokens.access).toBe('fresh-access');
    expect(tokens.refresh).toBe('rotated-refresh');
    tokens.clear();
  } finally {
    await new Promise(resolve => server.close(resolve));
  }
});
