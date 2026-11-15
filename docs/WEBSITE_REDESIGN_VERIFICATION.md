# Website redesign verification

Date: 2026-11-15. Implements the owner's approved “Axiom Force Website UI/UX Redesign”
attachment. Changes are local and uncommitted. No deployment, game export, new art generation,
paid credits, commits or pushes were performed.

## Delivered behavior

| Area | Implementation |
| --- | --- |
| Shell | Home, Gameplay, Squad, FAQ; language/account controls; Development footer link keeps `/tech-hub`; Design System remains directly accessible outside player navigation. |
| Home | Existing localized title art and city artwork; Explore gameplay primary action; squad secondary action; campaign/combat/recruitment/world/FAQ sections. |
| Gameplay | Twelve catalog missions grouped 1–4, 5–7, 8–12. Recruitment and Mission 1 world unlock; playable LA districts distinguished from planned LA activities. |
| Squad | Six existing portraits, registry-aligned roles, conservative signature descriptions and recruitment. Surya's Mission 5 restriction stated. Direct IDs retained; no invented ratings/deployment or `hallRoster`. |
| FAQ/Development | Unconfirmed release/platform/hardware status; accounts and browser game sign-in explained. Current stack stated without simulated telemetry/performance claims. |
| Languages | 17 complete showcase trees with 75 strings each, including twelve mission titles and six roles/abilities; all 29 onboarding keys in every language. Old unused promotional page namespaces removed. |
| Accessibility | Skip link; route titles/focus; visible focus; hidden closed mobile links; Escape and language-change focus restoration; scrollable mobile menu; native language select/FAQ disclosures; labelled account/onboarding/code fields; inline status/alerts. |
| Presentation | 17px sans-serif body, supporting text at least 14px, cyan controls, neutral surfaces, responsive grids and RTL logical spacing. Continuous flicker/tickers removed, reduced-motion override, no autoplay. Images have explicit dimensions and below-fold portraits load lazily. |
| Accounts | Profile/security actions and mailed code retry/cancel; loading, signed-out, missing-onboarding, linked, expired and unavailable states. Linked settings reopen after reload using fresh verified Clerk proof. |
| Authorization | Existing bare onboarding and browser-confirm URLs, nested Clerk routes, locale aliases and public request IDs retained. Game confirmation stays explicit and success says Return to the game. |

## Account recovery and race resolutions

Source review found that ordinary Clerk sign-in had no way to recover the game account's
settings after the memory-only API session disappeared. Protected account routes now accept
the explicit `Authorization: Clerk <session proof>` scheme. The existing verifier checks the
configured RS256 issuer/JWKS, lifetime, authorized party and session status; the middleware
validates the subject and resolves only its active linked account. Missing linkage returns
403 `onboarding_required`. The lookup issues no API credentials and approves no game request.
Existing Bearer paths, bodies and success JSON remain unchanged. Schema changes were not needed.

Clerk identity changes clear API memory globally, including when account pages are unmounted.
Profile setters and credential completions check identity plus an ownership version. Installing
or clearing credentials advances the token generation. Refreshes coalesce within a generation;
late responses cannot overwrite, clear, redirect or retry on behalf of a newer session.
Code-based email changes and 2FA removal explain that other game sessions are signed out and
the current session may require a later sign-in. Credentials remain in memory.

## Verification evidence

| Check | Result / local evidence |
| --- | --- |
| Website and backend builds | Passed in Docker; website is served by the normal local Nginx service. |
| Required full suites | Backend 158/158, NUnit 439/439, GdUnit4 366/366 passed; GdUnit4 reported 0 errors, 0 failures and 0 orphans. Final post-edit run is recorded in `.validation/website-redesign-final-unit-tests.log`. |
| Public/browser/client regressions | 14/14 passed: `.validation/website-redesign-final-browser-tests.log`. |
| Real Clerk and captured-mail security | 4/4 passed: `.validation/website-redesign-final-auth-tests.log`. Disposable Clerk users were removed by test cleanup; isolated API/mail/database were shut down through `/__test__/stop`. |
| Asset audit | Existing `audit_images.py` audited 349 images. All seven showcase copies load and retain their source dimensions/hashes. `.validation/website-image-audit.json`. |
| Asset comparison | Existing `compare_images.py` compared all seven copies against their correct sources with 0 failures: `.validation/website-art-comparison.png`. |
| Visual inspection | Public full-page screenshots at 360, 768 and 1440px plus Arabic Gameplay at 720px inspected. `.validation/showcase-360.png`, `showcase-768.png`, `showcase-1440.png`, `showcase-rtl.png`. |
| Source checks | `git diff --check` passed. No engine assets/code changed, so no Godot import refresh was triggered. Unit tests still ran as required. |
| Graphify | Queried before edits; final local `graphify update .` completed with exit 0, re-extracting 63 changed files. `.validation/website-redesign-graphify.log`. No LLM-backed commands used. |

The 14 checks cover every public/localized route and signed-out account route at 360, 768 and
1440 CSS pixels, plus bare onboarding/confirmation. Arabic, Hebrew and German routes reflow at
720 CSS pixels, the content width of a 1440px window at 200% zoom. Keyboard checks cover the
closed menu, opening, Escape, language-change focus and native FAQ disclosure. They also cover
route focus, six squad IDs, twelve missions, matching translation keys/placeholders, successful
image loads and absence of infinite animation.

Real Clerk checks cover login/MFA, signed-in reload, ordinary linked account recovery after
reload while a game request remains pending, switching accounts, cancellation/replay, closing
an unauthenticated browser, fresh signup/verification, captured onboarding email in a separate
context, explicit approval, and actual profile/email/2FA forms. Separate API checks exercise
incorrect/expired/reused codes, password login, rotation and revocation. Client checks exercise
concurrent refresh, failed-refresh locale/request preservation, Clerk-proof separation, and
late successful/failed refresh after credential replacement. Browser state/traces and account
screenshots were not persisted.

## EGM reviews and resolutions

- [Content baseline](reviews/website-content-baseline.md) verified mission acts, roles,
  recruitment, world scope and unsupported claims against game source.
- [Content implementation review](reviews/website-content-implementation-review.md) and its
  fresh reviewer found no Major/Blocker content issue. The platforms FAQ clarity nit was fixed:
  it now states only that release platforms are unconfirmed.
- [Navigation/auth review](reviews/website-navigation-auth-review.md),
  [follow-up](reviews/website-navigation-auth-rereview.md), and
  [race review](reviews/website-navigation-auth-final-review.md) identified account recovery,
  hidden focus, locale loss, stale identity and delayed response ownership issues.
- [Resolution review](reviews/website-auth-resolution-review.md) and an independent fresh
  reviewer confirmed the fixes with no remaining Major/Blocker findings. These are source
  reviews; execution evidence is the test table above.
- Translation recommendations are preserved in `reviews/website-copy-recommendations.json`
  and `reviews/website-onboarding-copy.json`. Application values include the reviewed wording
  and subsequent player-facing FAQ simplification.

## Limits

- Reflow was checked using equivalent CSS viewport widths; this is not an assistive-technology
  certification or a manual browser-toolbar zoom audit on every device.
- Translations have matching coverage and were reviewed for content, but have no native-speaker
  sign-off. Clerk's third-party credential interface retains its own copy/session behavior.
- Vite reports the existing large-bundle warning. This task removes the unused model-viewer
  request and extra font families, but does not replace the existing CDN Tailwind architecture
  or split all language resources into separate downloads.
- Artwork is labelled as artwork; no screenshot is presented as a gameplay capture. All seven
  copied images retain original SpriteCook bytes and manifest provenance.
- Normal backend/database/website containers stay running. No game runtime/Tokyo changes were
  made; prior handoff verification remains the game regression baseline.
- Graphify retains its existing format coverage gaps; it also reported missing `tree_sitter_sql`
  for four SQL files and no symbols from two small entry/test files. Source remains authoritative.
  No dependency installation or LLM-backed relabeling was performed for those warnings.
