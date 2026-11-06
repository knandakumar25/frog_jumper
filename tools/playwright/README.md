# Local browser authentication verification

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
docker compose cp tools/playwright/backend_server.py backend:/tmp/browser_backend_server.py
docker compose exec -T -e PYTHONPATH=/app backend python /tmp/browser_backend_server.py
```

It creates only PostgreSQL `test_browser_handoff`, captures all Django mail in
memory, and exposes temporary fixture/mail routes on container-internal port 8002.
No test route is installed in normal Django URLs. Normal unit tests use a separate
test database. Do not run two copies of this harness simultaneously.

From another terminal, set `CLERK_PUBLISHABLE_KEY` to the same **public development
key** as the website, then run:

```powershell
docker compose run --rm -e CLERK_PUBLISHABLE_KEY -e BASE_URL=http://localhost:5173 playwright npx playwright test browser-auth.spec.cjs --workers=1
docker compose run --rm playwright
```

The first command exercises real login/MFA, signed-in reloads, a complete switch to
another account, cancellation/replay, browser closure, fresh signup, email/phone
verification, captured onboarding mail in a fresh browser context, and explicit
game approval using the resulting Django session. Created Clerk users are deleted
by cleanup. The second command runs the existing public-site/storage smoke check.
The Docker browser resolves localhost through `host.docker.internal`; this setup
was verified on Windows Docker Desktop. Other host platforms remain unverified.

For a live Windows game check, optionally publish only the isolated API:

```powershell
docker compose run --rm --no-deps -p 127.0.0.1:18002:8002 -v D:/Projects/frog_jumper/tools/playwright/local_proxy.py:/tmp/handoff_proxy.py:ro --entrypoint python backend /tmp/handoff_proxy.py
```

Adapt the bind path to the checkout. Use `AXIOM_API_BASE_URL=http://127.0.0.1:18002/api`
for that game process. Host port **18002** avoids the local Godot MCP WebSocket
service on 8002. The proxy excludes fixture/mail routes. Optionally pass the game's
**public** request UUID as `LIVE_GAME_SESSION_ID` to the first browser command; the
runner approves it as its disposable logged-in identity and leaves redemption to
the game. Never supply the game's private secret to the browser runner.

Stop the task-owned proxy after verification. Stop the isolated backend gracefully
with Ctrl-C, or POST `/__test__/stop` from inside the backend container; its finalizer
destroys only its test database. Leave the normal backend/db/website running.
