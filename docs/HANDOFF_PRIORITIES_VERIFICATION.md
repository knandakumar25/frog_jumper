# Handoff priorities verification — 2026-11-15

The approved follow-up covers review/documentation, isolated authentication verification,
and Tokyo runtime acceptance. The owner also required all hero/enemy bodies to use
SpriteCook output. Changes are local and uncommitted. No exports or paid generation ran.

## Implemented changes

- Backend code issuance now locks the account across its count/insert transaction. Verification
  selects the latest code before checking consumed/expired state, so a consumed successor
  cannot expose an older code. Refresh rotation and revoke-all share the account lock, preventing
  concurrent replay/revocation from missing a newly inserted successor. Four regression tests
  include controlled PostgreSQL lock schedules.
- Seven backend README contracts cover runtime, routes, libraries, migrations, tests and
  verification scripts. Environment/Compose precedence, website directory, twelve missions,
  JavaScript test triggers, actual art totals and completed icon/civilian work were corrected.
  `npm start` now uses the server's existing migration path once. The root static page's favicon
  references an existing file. Historical rollback guidance now identifies pre-migration
  commit `1bb6282` instead of assuming current HEAD still contains Django.
- The Names Without Numbers aftermath uses existing SpriteCook hero atlases and civilian
  image regions in place of primitive bodies. Campaign sentries and world hostiles use the
  existing vanguard animation, with no retired stand-in or procedural body fallback.
- Later boss plates replace direct procedural chassis polygons and inherit their visual root's
  facing transform. Ignis no longer draws its old body beneath the plate. Separate shield,
  aura, orbital, beam, health and attack indicators remain. Gameplay collision and combat rules
  are unchanged. No image file or asset-manifest entry was replaced.
- On the owner's deletion request, removed original boss-body constructors and 22 unused
  operative drawing methods rather than retaining hidden geometry. Also deleted the remaining
  commander, guard, captive, civilian fallback, drone and defeated-boss body drawings, plus
  retired stand-in character APIs. Active figures reuse existing SpriteCook art. Boss regression
  coverage now checks that original chassis nodes do not exist. Gameplay scripts, collision,
  shield/attack effects and scenery remain.

## Verification

| Check | Result / evidence |
| --- | --- |
| Build after renderer deletion | 0 errors, 151 existing warnings; `.validation/legacy-deletion-build.log` |
| Backend after renderer deletion | 154 passed, 0 failed; `.validation/legacy-deletion-tests.log` |
| NUnit | 439 passed, 0 failed; same log |
| GdUnit after renderer deletion | 366 passed, 0 errors/failures/orphans; `.validation/legacy-deletion-tests.log` |
| Focused actor art | 8 passed, 0 errors/failures/orphans; `.validation/approved-plan-art-tests.log` |
| Browser authentication | 5 passed: password-account API, Clerk login/signup/verification/onboarding/switch/cancel/approval, actual 2FA/email-change UI and actual Axios single-flight client |
| Public-site browser smoke | 1 passed |
| Proxy game-client contract | Correct disposable identity redeemed through loopback proxy; replay rejected with 410 |
| Tokyo | Standard/wide captures, transition/motion, natural completion, Skip and replay movement passed; no blade offset change |
| Final character/boss visuals | Names heroes/civilians and six-boss tableau captured and inspected; runtime resource paths checked |
| Import after renderer deletion | Exit 0; `.validation/legacy-deletion-import.log`; known EditorSettings/resource/PagedAllocator shutdown diagnostics remain; no missing-art or parse failure |
| Graphify | Local non-LLM update completed: 7,559 nodes, 16,233 edges, 341 communities; `.validation/legacy-deletion-graphify.log` |
| Provenance | All 342 manifest file hashes match; 323 texture caches match current source MD5; all eight Shogun idle cells match the actual SpriteCook download byte for byte after documented packing |

One full GdUnit run during concurrent visual QA failed four assertions in the existing
wall-timed dodge test (`test_world_expansion_phase1.gd`). Its isolated rerun passed all three
cases without a source change. The final quiet full-suite result is recorded above; no gameplay
logic or assertions were relaxed to obtain a pass.

The website exposes Clerk sign-in, so password login/2FA-login were verified through the
isolated HTTP API. Actual website account forms verified 2FA enable/disable and email changes.
The proxy check verifies the game HTTP contract, not a Windows game's browser/focus UI.
Clerk enablement in the served bundle is verified by actual sign-in fields and successful flows,
preserving the contest of first-round Issue 14.

## Runtime evidence and environment

- [Tokyo acceptance](../.validation/tokyo/approved-plan-runtime.md) links standard/wide frames,
  natural/Skip scorecards, movement restoration and final actor captures.
- [Six SpriteCook bosses](../.validation/tokyo/approved-runtime/spritecook-six-bosses.png).
- [Final Names hero and civilians](../.validation/tokyo/approved-runtime/names-mirage-25-final.png).
- [Authentication evidence](../.validation/auth-verification.md) records cleanup and safe logs.
- [Hero provenance](../.validation/hero-art-provenance.md) and [actor audit](../.validation/art-provenance-audit.md).

Godot's editor build hook could not load SDK 11's MSBuild dependencies under its runtime.
QA used SDK 10.0.404 in the task-owned temporary directory `axiom-tokyo-sdk10`, with PATH and
DOTNET_ROOT set only for the QA editor/import process. Global SDK 11 and the net10.0 project
target were not changed. The initial failed launches and QA eval retries are distinguished
from the final successful runtime run, which recorded zero game warnings/errors.

All task-created Clerk users were deleted; the isolated database, mail server and proxy were
removed/stopped. Temporary runtime nodes and the task-owned game/editor were closed. The normal
backend, PostgreSQL and website stack remains running. Credentials, OTPs, browser state and
traces are excluded from retained evidence. `.validation/` evidence is local and git-ignored.

## Review and deferred work

Fresh-context reports: [first-round reconciliation](../.validation/egm/second-backend-review.md),
[final backend/docs review](../.validation/egm/final-review.md), and
[game art review](../.validation/egm/game-art-review.md). The subsequent
[renderer deletion review](../.validation/egm/legacy-deletion-review.md) confirmed the original
body code is deleted, checked civilian crops and found no unresolved Blocker/Major.
Confirmed findings were repaired;
no unresolved Blocker/Major remains in the changed scope. This does not claim two clean EGM
rounds or elimination of the migration's previously accepted architectural risks.

Still owner actions: native title wording acceptance and provider-secret rotation. Deferred
architecture includes legacy-password rehash policy, browser comparison-code UI/account-2FA
semantics, logout/revoke-all API design and a limited database role. No production account or
provider setting was changed.
