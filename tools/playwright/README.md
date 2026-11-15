# Local browser authentication verification

The showcase redesign adds `showcase.spec.cjs` for route layouts at 360/768/1440 CSS pixels,
200% equivalent reflow, Arabic/Hebrew/German, keyboard menu/focus, native FAQ accordions,
six direct operative links, twelve missions, translation parity and URL context preservation.
`client-refresh.spec.cjs` additionally covers failed refresh locale preservation and late
success/failure after credentials are replaced. The Clerk flow now checks linked account
settings after reload while the game request remains pending.

To run public checks with current source mounted:

```powershell
docker compose build playwright
docker compose run --rm -v "${PWD}/website/src:/website/src:ro" playwright npx playwright test showcase.spec.cjs client-refresh.spec.cjs smoke.spec.cjs --workers=1
```

These checks do not replace real Clerk tests below. Full results and limits are recorded in
[`docs/WEBSITE_REDESIGN_VERIFICATION.md`](../../docs/WEBSITE_REDESIGN_VERIFICATION.md).

Use the existing local backend/db/website stack and a **Clerk development instance**.
The runner refuses production keys and uses reserved `+clerk_test@example.com`
addresses, fictional phone numbers, and `424242` verification codes. It preserves
the instance's required fields and authentication methods. It never sends real
email/SMS, writes credential-bearing browser state/traces, or changes Clerk settings.

Build the backend, website and opt-in runner with `docker compose build backend
website playwright`, then `docker compose up -d`. The website's existing
`website/.env.local` supplies its public Clerk key. Backend issuer, JWKS URL and
authorized-party configuration are described in root `.env.example`.

Start the isolated backend from a dedicated terminal:

```powershell
docker compose exec -T backend node scripts/e2e-server.js
```

`backend/scripts/e2e-server.js` (Express; it replaced Django's `backend_server.py`
on 2026-11-15) creates only PostgreSQL `test_browser_handoff`, captures all mail in
memory, and exposes temporary fixture/mail routes on container-internal port 8002.
No test route is installed in the normal API. Normal unit tests use a separate
test database (`accounts_test`). Do not run two copies of this harness simultaneously.

From another terminal, set `CLERK_PUBLISHABLE_KEY` to the same **public development
key** as the website, then run:

```powershell
docker compose run --rm -e CLERK_PUBLISHABLE_KEY -e BASE_URL=http://localhost:5173 playwright npx playwright test browser-auth.spec.cjs --workers=1
docker compose run --rm playwright
```

The account security and client refresh checks use the same isolated server:

```powershell
docker compose run --rm -v "${PWD}/website/src:/website/src:ro" playwright npx playwright test account-security.spec.cjs client-refresh.spec.cjs --workers=1
```

`account-security.spec.cjs` checks real password login, emailed 2FA setup/login/removal,
incorrect/expired/reused codes, email changes, and revoked refresh tokens through the API.
The public website exposes Clerk sign-in only; these password checks do not claim a
password-login browser screen exists. The Clerk signup browser case also exercises the
actual account 2FA and email-change screens. `client-refresh.spec.cjs` loads the website's
actual Axios client and verifies two concurrent expired requests spend one refresh token.
Only import resolution and Vite's environment substitution are adapted for the Node runner.
The test-only `/__test__/expire-codes` route ages codes belonging to `browsercheck*`
fixtures in the isolated database; it is absent from the normal server.

The first command exercises real login/MFA, signed-in reloads, a complete switch to
another account, cancellation/replay, browser closure, fresh signup, email/phone
verification, captured onboarding mail in a fresh browser context, and explicit
game approval using the resulting API session. Created Clerk users are deleted
by cleanup. The second command runs the existing public-site/storage smoke check.
The Docker browser resolves localhost through `host.docker.internal`; this setup
was verified on Windows Docker Desktop. Other host platforms remain unverified.

For a live Windows game check, optionally publish only the isolated API:

```powershell
docker compose run --rm --no-deps -p 127.0.0.1:18002:8002 backend node scripts/e2e-proxy.js
```

The proxy (`backend/scripts/e2e-proxy.js`) ships in the backend image, so no bind mount
is needed. Use `AXIOM_API_BASE_URL=http://127.0.0.1:18002/api`
for that game process. Host port **18002** avoids the local Godot MCP WebSocket
service on 8002. The proxy excludes fixture/mail routes. Optionally pass the game's
**public** request UUID as `LIVE_GAME_SESSION_ID` to the first browser command; the
runner approves it as its disposable logged-in identity and leaves redemption to
the game. Never supply the game's private secret to the browser runner.

Stop the task-owned proxy after verification. Stop the isolated backend gracefully
with Ctrl-C, or POST `/__test__/stop` from inside the backend container; it then
drops only its test database. Leave the normal backend/db/website running.
