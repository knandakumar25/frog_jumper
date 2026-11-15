# Website navigation and authentication review

Reviewed 2026-11-15 against the approved website redesign plan. This is a source and diff review, not a claim of browser or test execution. Graphify returned the relevant Navbar, BrowserAuth, TwoFactor, LanguageProvider and API-client nodes; current source was used to confirm behavior because its Navbar node still described the removed rail ticker.

## Major findings

### 1. Account access cannot distinguish an existing game account from missing onboarding

`website/src/components/AccountAccess.jsx:15-20` only distinguishes a memory-resident API user from a Clerk session. With a Clerk session and no API user, both `/account` and `/account/2fa` display Clerk's UserProfile and a completion link. `website/src/pages/Login.jsx:17` now explicitly sends ordinary successful sign-ins to this state. `website/src/context/AuthContext.jsx:12-27` never exchanges Clerk proof for an API profile, and its credentials disappear on reload by design. Existing linked users following the completion link encounter `already_registered` at `backend/src/routes/onboarding.js:153` or `:175`; signing in again does not repair the missing API state.

Consequences: users who already completed registration cannot reach the game's profile/2FA settings through a fresh ordinary Clerk login. Opening the explicit `/account/2fa` URL instead displays Clerk security controls, which configure a different authentication system from the backend's mailed-code 2FA. The old account redirect also had an onboarding limitation; the changed UI and login destination now present this as an account/security page without identifying that limitation.

Required action: distinguish Clerk settings from game API settings and distinguish unknown/onboarding-required/expired API-session states. Provide a supported recovery path for existing linked users without persistence or automatic game authorization. If backend work is deliberately out of scope, clearly expose the limitation and avoid presenting Clerk security as the requested game 2FA page. Add an existing-linked-user fresh-login/reload case; the current onboarding-based browser test only covers the lifetime in which API tokens remain in memory.

### 2. Switching language in the mobile menu on bare auth routes hides the focused control

`website/src/translations/LanguageContext.jsx:176-181` correctly keeps `/auth/browser-confirm` and `/onboarding` bare and changes only their `locale` query parameter. `website/src/components/Navbar.jsx:20` closes the mobile menu on search changes. `website/src/App.jsx:40-44` moves focus only when the pathname changes. Therefore choosing a language in the mobile menu on either bare route hides the select that still owns focus, with no intentional focus destination. Escape restores toggle focus, but this closing path does not.

Required action: preserve a visible focus destination when menu closure hides its active element, including query-only locale changes. Verify keyboard language changes on `/auth/browser-confirm?session_id=...&locale=en` and `/onboarding?locale=en` at mobile width. Do not move focus on every query change indiscriminately because Clerk nested form interactions also change URLs.

### 3. Expiration redirect drops the locale carried on bare auth/onboarding routes

`website/src/api/client.js:57-62` derives locale exclusively from the first pathname segment. On `/onboarding?locale=ar&session_id=...` that segment is `onboarding`, so failed refresh redirects to `/en/login/game/...?...` rather than Arabic. The same extraction fails for `/auth/browser-confirm?locale=...`, although BrowserAuth's direct Axios request currently bypasses this interceptor. The public request ID survives the onboarding example; the language does not.

Required action: derive locale from the existing locale parser and bare-route query contract, and use the shared validated public-request extraction where possible. Exercise rejected refresh on both localized account URLs and bare onboarding URLs, preserving locale and request ID. The current refresh-concurrency test does not exercise failed refresh or redirects.

## Token-lifetime risk carried into the redesigned account journey

`backend/src/routes/accounts.js:132` and `:204` revoke all refresh tokens after email change and 2FA disable. `website/src/pages/Account.jsx:33` and `website/src/pages/TwoFactor.jsx:86-91` retain the current memory token pair and report success. Current access credentials remain valid until expiry (default one hour, `backend/src/config.js:97`), after which the next API call attempts the already-revoked refresh and navigates to login. The account changes themselves succeed; the delayed logout is not a failed save. This behavior predates the redesign, but the plan's expired-session distinction and clear security-action feedback should address it. Explain that reauthentication is required and verify the post-revocation expiry path; do not silently pretend this page retains a renewable session.

## Test contracts and review limits

- The original `tools/playwright/browser-auth.spec.cjs:259,262` expected the old profile failure/success text. Current working-tree edits update those expectations to the redesigned wording; that resolves the source-level text mismatch. This review did not run the test and cannot claim a pass.
- `tools/playwright/account-security.spec.cjs:6-82` verifies API behavior directly, including revoked refresh tokens at lines 62 and 78. It does not establish that the account UI remains usable after those operations or that Clerk sessions recover API settings after reload.
- `tools/playwright/client-refresh.spec.cjs:8-48` covers concurrent successful refresh only. It cannot verify expired-session copy, locale preservation, or request preservation on rejected refresh.
- The closed menu uses `hidden` and the stylesheet enforces `[hidden] { display: none !important; }`; its closed links are therefore removed from normal keyboard focus. Escape explicitly restores toggle focus. No blocker was found in those paths by source inspection.
- No credentials were persisted, no game authorization was added, and the BrowserAuth confirmation request contract remains unchanged in the reviewed diff. Real Clerk interaction, responsive layout, zoom, and assistive-technology behavior still require the planned browser checks.
