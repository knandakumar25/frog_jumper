# Authentication resolution review

Reviewed the updated source on 2026-11-15. No remaining Major or Blocker findings in the reviewed navigation/account-authentication changes.

Graphify was queried again; its indexed functions still predate these edits, so conclusions use the actual current files. The independent fresh-context reviewer `identity_final_audit` also re-read the final changes and returned no remaining Major/Blocker findings. Neither reviewer ran tests or reproduced races at runtime.

## Resolutions confirmed in source

- `website/src/api/tokens.js:7-15`: installing a new credential pair and clearing credentials both advance the store generation. Refresh rotation deliberately stays within its original generation.
- `website/src/api/client.js:25-30`: requests retain their original generation and reject stale dispatch/retry or a changed session during asynchronous Clerk proof acquisition.
- `website/src/api/client.js:49-61`: refresh requests coalesce only within one generation and refuse to install an old response into a replacement session. An older request's cleanup cannot clear a newer in-flight refresh reference.
- `website/src/api/client.js:71-80`: changed sessions reject stale 401 handling, stale post-refresh retry, and stale failure cleanup before credentials can be cleared or navigation changed.
- `website/src/context/AuthContext.jsx:19-33`: the global Clerk identity bridge hides and clears the old profile/token pair across routes; owned profile setters reject a changed identity or epoch.
- `website/src/context/AuthContext.jsx:40-44,64-67,78-81,103-107`: startup reads and credential completion callbacks check identity/epoch. Successful password login, password 2FA completion, and emailed onboarding installation advance both the provider epoch and token generation, invalidating earlier profile callbacks and refresh work.
- `website/src/pages/Onboarding.jsx:199`: rejected credential installation aborts completion handling before navigation. Token-only onboarding remains supported when the submitting identity/session generation is still current.

## Authorization boundary

The reviewed account-access additions verify Clerk proof and resolve an existing active linked account. They do not issue API/game credentials, approve a browser game request, or persist credentials in browser storage. The existing explicit BrowserAuth confirmation and browser-poll game flow retain their separate roles. Existing onboarding and password credential issuance remain explicit existing flows; their client-side installation now observes session ownership.

## Verification limits

The current `tools/playwright/client-refresh.spec.cjs` contains delayed-success and delayed-failure replacement-session cases, as well as concurrency and failed-refresh locale/request checks. Their presence was inspected; no test pass is asserted by this review. Parent verification must establish test results, real Clerk interaction, and the planned responsive/keyboard checks.
