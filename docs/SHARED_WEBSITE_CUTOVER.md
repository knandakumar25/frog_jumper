# Shared website cutover

The sibling checkout at `D:/Projects/axiom_force_website` owns React, Nginx,
the website Compose stack, website assets and Playwright. It serves
`http://localhost:5173` with localized routes for Axiom Force and Drift Circuit.
The English Drift Circuit outline lists the two cups and eight candidate tracks.
The existing localized navigation and original account URLs are still used.

## Local service boundaries

- The website's `start.ps1` starts the original database and Express service
  and waits for health before starting Nginx. Nginx resolves the original API
  through the shared Docker network and sets the forwarding header from its
  socket peer. Express trusts the website's named proxy alias.
- The original API and racing API retain separate Compose project names,
  database volumes, host ports and export profiles. The shared website uses the
  original API. Racing's API is a standalone local development service.
- The old website copies were retired locally after browser and game checks.
  The shared checkout keeps the Vercel rewrite configuration. Public hosting
  should continue using its current source revision until the hosting provider
  points the existing site URL at the new checkout.

## Verification

- Public Playwright covered the shared homepage, both game pages, localized
  deep links, same-origin API health, Clerk route context, responsive layout and
  refresh behavior.
- Four Clerk and account checks passed using the disposable development
  database. A separate headless Godot process used the original authentication
  and save classes to redeem approval, commit level progress and load it back
  from the disposable API. The temporary harness and fixtures were removed.
- Nginx recovered after an Express restart, then recovered after Express moved
  to another Docker address without restarting Nginx. Ten invalid login
  requests from one Docker client returned 400 before its next request returned
  429. A host client still received 400.
- The racing API returned healthy on port 8001, and the racing launcher passed
  both shared browser smoke checks.
- The hashes of all 25 website files in the SpriteCook manifest match the
  copied assets. The deployment and both export workflows were left idle.
- Public Playwright passed 16 checks. The original API, NUnit and GdUnit4
  passed 182, 526 and 408 tests respectively. GdUnit4 reported zero errors,
  failures and orphans. The racing API passed all 182 tests.
- The extra Python localization check reproduced 18 failures from the existing
  catalog check at Git HEAD. The ordinary editor import reported TLS and
  shutdown diagnostics, while the live Godot verification and GdUnit4 passed.

## Store guard

The repository-root guard returned `BLOCKED` on four matches in the ignored
`.skill-install-staging/android-skills/play/play-policy-insights/resources/scanner_config.json`.
That file defines scanner patterns. The scan matched background location
(line 28), all-files access (423), SMS and call logs (743), and accessibility
service (640). A store audit should scan the generated native app and exclude
local tool staging. The moved Clerk, account and deletion source was copied
without changing its behavior, and no native mobile project was exported.

## Goldfish review

The fresh implementation-readiness reviewer identified the port collision,
intercepted Clerk calls, stale Docker DNS, proxy attribution, the split racing
smoke check and env build-context risks. The migration staged on port 5175
before testing on 5173, used a separate proxy health and throttling probe, and
verified an actual Express address change. The website runner keeps Clerk on
the expected origin while sending fixture traffic to its disposable test API.
Local environment files are excluded from Git and from the website image's
build context. The image receives the public browser key as a build argument.
