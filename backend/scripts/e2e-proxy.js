/**
 * Loopback proxy that lets a Windows game process reach the throwaway test API
 * (`e2e-server.js`) for a live sign-in check. Replaces Django's `tools/playwright/local_proxy.py`.
 *
 *   docker compose run --rm --no-deps -p 127.0.0.1:18002:8002 backend node scripts/e2e-proxy.js
 *
 * Only `/api/...` is forwarded to `backend:8002`; the test-only `/__test__/...` routes (mail and
 * fixtures) stay unreachable from the host. Only the method, path, query, Content-Type and
 * Authorization are passed on. Nothing is logged except the ready line.
 */
import http from 'node:http';

const TARGET = { host: process.env.E2E_TARGET_HOST ?? 'backend', port: 8002 };

const server = http.createServer((req, res) => {
  if (!req.url.startsWith('/api/')) {
    res.writeHead(404).end();
    return;
  }
  const headers = { 'Content-Type': req.headers['content-type'] ?? 'application/json' };
  if (req.headers.authorization) headers.Authorization = req.headers.authorization;
  if (req.headers['content-length']) headers['Content-Length'] = req.headers['content-length'];
  const upstream = http.request({ ...TARGET, method: req.method, path: req.url, headers, timeout: 15_000 }, (answer) => {
    res.writeHead(answer.statusCode ?? 502, { 'Content-Type': 'application/json', 'Cache-Control': 'no-store' });
    answer.pipe(res);
  });
  upstream.on('timeout', () => upstream.destroy(new Error('upstream timeout')));
  upstream.on('error', () => { if (!res.headersSent) res.writeHead(502).end(); else res.end(); });
  req.pipe(upstream);
});
server.listen(8002, '0.0.0.0', () => console.log('Loopback test API proxy ready'));
