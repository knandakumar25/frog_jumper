# Website navigation and authentication follow-up review

Reviewed the changed sources on 2026-11-15, including `AccountAccess`, the API client, Navbar, AuthContext, Account, TwoFactor, backend authentication middleware, Clerk verifier, and `backend/test/clerkAccount.test.js`. Graphify was queried again; the current source is authoritative because the graph does not yet expose the new account proof flow. This is a source review; no tests or browser interactions were run by this reviewer.

## Remaining major findings

### 1. Identity ownership is scoped to the account route, leaving stale users and Bearer credentials outside it

`website/src/components/AccountAccess.jsx:18-24,30-34,38,43` stores the previous Clerk identity in a component-local ref. Its cleanup clears the proof getter but does not clear the globally stored profile. `website/src/context/AuthContext.jsx:9,77-87` does not observe Clerk identity changes. The Navbar's Clerk UserButton has no application-memory cleanup (`website/src/components/Navbar.jsx:29`).

Reproduction from the source: sign in as linked user A, open Account so Clerk proof populates AuthContext.user, navigate Home so AccountAccess unmounts, let Clerk become signed out without reloading this document (for example, session expiration or sign-out propagated from another tab), then visit Account. The new gate starts with `previousIdentity.current === null`, so the signed-out branch does not call logout. Its render expressly permits `user && !session.isSignedIn`, displaying A's profile while Clerk is signed out. With no memory token, subsequent requests fail; the old profile is still exposed. If an API token pair remains from token-only onboarding, the stale Bearer credentials can also continue authenticating after Clerk sign-out outside the gate. A full document navigation during the local UserButton sign-out would reset memory and avoid that particular reproduction; it does not cover external Clerk session changes.

A related mismatch occurs when a different Clerk account B first mounts the gate while memory tokens still belong to A: the initial previous identity is null, so the gate replaces the profile with B without clearing A's token pair. Account requests use B's Clerk proof, but other consumers such as BrowserAuth still retain A's Bearer fallback once Clerk is signed out.

Required action: track the owner/source of API credentials and profile at a scope that survives route changes. Clear profiles and any associated memory token pair on the applicable Clerk sign-out/account switch, while preserving the deliberate fresh-tab token-only onboarding flow. Never infer identity compatibility merely because the route-local previous identity is null. Add browser coverage for account → Home → externally observed sign-out without reload → account, and for switching from a token-owning A to Clerk B.

### 2. Late account-action responses can restore a previous identity after logout/switch

The gate correctly ignores its own stale profile fetch using `active` at `website/src/components/AccountAccess.jsx:28-38`. That guard does not cover its children. `website/src/pages/Account.jsx:32-33` writes a completed PATCH profile to the global user unconditionally. `website/src/pages/TwoFactor.jsx:49-50,87-88` does the same with follow-up GETs. A request authorized for A can complete after sign-out or an account switch and call `setUser(A)` even if its page has unmounted and the gate already called logout. During a token-only/signed-out state, the gate's `user && !isSignedIn` branch can then render that restored stale user.

Required action: bind account action responses to the identity/session generation that submitted them and discard results after logout, account switch, or unmount. A route cleanup alone cannot undo an already-sent server operation; the necessary requirement is that late responses cannot repopulate another session's UI. Cover delayed PATCH/profile responses around sign-out and switching.

## Changes that address the initial findings

- The additive `Authorization: Clerk ...` branch verifies proof through the existing trusted `deps.clerk.subject` verifier and selects by verified `clerk_user_id`; malformed subjects and inactive users are rejected. It does not issue API tokens or touch game sessions. Bearer processing is still separate. No additional server-side blocker was found in this branch by source inspection.
- Account proof is obtained for each applicable request, with no browser persistence. Clerk-authenticated requests do not enter the Bearer refresh retry path. This removes the old revoked-refresh expiry problem for requests using live Clerk proof; token-only sessions still follow the existing backend revocation contract.
- AccountAccess now reports explicit loading, onboarding-required, unavailable, expired, and signed-out branches instead of substituting Clerk UserProfile for the game's 2FA page. Existing linked users can fetch their API account directly.
- Navbar now restores toggle focus when route/search changes close an open menu. The original bare-route language-change focus defect is addressed in source.
- Refresh redirects now consider the locale query, fixing the specific bare-onboarding locale-loss example from the first review.

## Verification limits

`backend/test/clerkAccount.test.js` adds useful coverage for linked identity, malformed proof/subject, missing onboarding, inactivity, no token/game issuance, profile PATCH, and mailed-code requirements. These tests mock the Clerk verifier; the separate Clerk verifier tests remain necessary for signature/issuer/authorized-party behavior. None of these backend tests establishes frontend identity cleanup or late-response safety. This reviewer did not claim any test passes.
