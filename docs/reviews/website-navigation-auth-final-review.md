# Final navigation/authentication source review

Reviewed 2026-11-15 after the global AuthProvider identity bridge and owned profile setter were added. Graphify was queried and the actual current sources were read. An additional reviewer, `identity_final_audit`, was launched with `fork_turns: none`, read the EGM codebase-details persona, independently queried Graphify, and completed a separate source review. Neither reviewer ran tests or edited application code.

## Remaining major: refresh responses can resurrect or destroy another session

`website/src/api/client.js:45-50` installs the result of an in-flight Bearer refresh without checking who owns the current credential store. `website/src/context/AuthContext.jsx:28-30,107-110` now correctly clears that store when Clerk identity changes or logout occurs, but it cannot prevent an older response from writing afterwards. A token-only session A can begin a refresh; the user then switches to Clerk B or logs out; A's refresh resolves and reinstalls A's access/refresh pair. The failure path at `website/src/api/client.js:67-74` can instead clear a newer session's credentials and redirect it. The retry can also continue with the old session after ownership changes.

Required action: capture a credential/session generation when refresh begins and require it to match before applying success, retrying the old request, clearing credentials on failure, or redirecting. Clearing/replacing credentials must change that generation. A comparison of only the refresh string is less explicit than a generation and does not fully express sign-out followed by session replacement. Add a deterministic delayed-refresh test covering logout/replacement before both success and failure. The independent fresh reviewer reported this same Major separately.

## Remaining major: onboarding credential installation bypasses the owned setter

`website/src/context/AuthContext.jsx:100-103` exposes `loginWithTokens` that always writes both token fields and the user. `website/src/pages/Onboarding.jsx:192-199` awaits completion and invokes the captured function. If Clerk identity changes or logout occurs while completion is pending, this callback restores the completed account's credentials after the new global identity cleanup. The owned setter does not apply to this path.

Required action: bind credential-installation callbacks to the submitting identity/session generation, allowing the deliberate token-only onboarding flow when that generation remains current. Refuse late completion after a conflicting identity change/logout. The same direct writes remain in `AuthContext.jsx:63-66,76-79` for password login/2FA verification; those methods currently have no public login-form consumer in the reviewed redesign but should follow the same rule. Verify delayed onboarding completion around logout/account switch.

## Findings resolved by this round

The global AuthProvider now observes Clerk identity on public routes and clears the previous profile/token pair before paint. This addresses the account-route-unmount gap described in the earlier report. The exposed user is synchronously hidden during an identity transition. `ownedSetUser` checks the current identity and epoch, and AccountAccess includes that stable setter in its effect dependencies, so old account PATCH/2FA profile callbacks no longer repopulate user state through the context setter after an identity change or logout. The remaining findings concern other credential writers that bypass that guard.

## Game authorization boundary

No automatic game approval or game-token issuance was introduced in the reviewed account-access changes. The new backend `requireUser` branch verifies explicit Clerk proof and resolves an existing active linked account. It does not call token issuance or write browser authorization sessions. Existing BrowserAuth still requires the explicit confirmation click; browser-poll remains the existing game-token issuance route. No new browser persistence was found. These conclusions are source-level observations, not runtime test results.

## Verification limits

The backend Clerk-account tests exercise identity lookup and code-protected security contracts but do not prove frontend asynchronous ownership. The existing successful-refresh concurrency test does not cover logout/switch during refresh. A final passing verification record should include the two delayed-response scenarios above; no pass is asserted here.
