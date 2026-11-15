# Player website

React/Vite showcase and account UI, served locally by the Compose `website` service at
http://localhost:5173. The Nginx configuration proxies `/api` to the Express backend.

## Boundaries

- `src/App.jsx` keeps localized player routes, Clerk wildcard routes, bare `/onboarding`
  and `/auth/browser-confirm`; it owns route titles and navigation focus.
- `components/Navbar.jsx` and `Footer.jsx` own navigation. Closed mobile content is hidden
  from keyboard focus. Escape and language-driven menu closure restore toggle focus.
- `components/Showcase.jsx` holds campaign/operative presentation identifiers and shared content
  components. Verify changes against `LevelCatalog`, `OperativeRoster` and the game registry.
- `showcase.css` supplies neutral surfaces, readable text, logical spacing, shared forms/cards,
  focus outlines, responsive layouts and reduced-motion behavior. Title imagery remains separate
  from explanatory copy. Existing onboarding and Clerk panels share the form baseline.
- `translations/ui_translations.json` contains all 17 showcase and onboarding translations.
  Every locale must preserve key sets and interpolation placeholders. Language changes retain
  the current query/hash and keep bare auth/onboarding routes bare.
- `components/AccountAccess.jsx` resolves loading, signed-out, linked, onboarding-required,
  expired and unavailable states. It acquires fresh Clerk proof per protected account call.
  `AuthContext` discards old API memory when the Clerk identity changes anywhere in the site.
- `api/tokens.js` retains API credentials only in memory. Install and clear invalidate earlier
  work. `api/client.js` coalesces refresh within one generation and rejects stale writes/retries.
- `pages/BrowserAuth.jsx` alone submits explicit game approval. Visiting account settings,
  changing language, loading a profile or signing in never approves a pending request.
- `public/images/showcase` contains unmodified copies of seven existing SpriteCook images;
  source paths, hashes and asset references are in root `spritecook-assets.json`.

## Verification

Build locally with `docker compose build website`, then `docker compose up -d website`.
After backend edits rebuild that service too. Never export the game as part of a website build.

Run `run_tests.ps1` from the repository root after final code changes. Browser checks use
`tools/playwright/showcase.spec.cjs`, the existing smoke/Clerk/security checks, and
`client-refresh.spec.cjs`. See [browser instructions](../tools/playwright/README.md) and
[redesign verification](../docs/WEBSITE_REDESIGN_VERIFICATION.md).

The application has no access/refresh credential persistence. Clerk's own existing session
handling remains responsible for public sign-in. Password API settings retain their mailed-code
rules; they do not configure Clerk's authentication factors.
