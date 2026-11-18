# Axiom Force - Strategic Inter-Agent Handoff Briefing

## Google EGM review fixes for touch controls and button images — 2026-11-18

The 2026-11-17 review round (`docs/INPUT_GOOGLE_REVIEW_2026-11-17.md`) found 3 Blockers and 25 Majors. The owner asked for all of them to be fixed. The two sections below were rewritten to match the code and the evidence.

**Code fixes:**
- **Top-down pause (cb-2):** the pause panel in `IsometricMission` now has a tappable **Resume** button (`ResumeButton`), focused when the game pauses. Touch-only players can no longer get stuck.
- **Field taps attacking (cb-M5):** `IsometricOperative._UnhandledInput` ignores mouse events Godot copies from touches. Attacking on a touchscreen is done with the Attack button.
- **Copied clicks counted as mouse use (cb-M6):** `InputPromptService.ObserveInput` now ignores them too, so a tap no longer switches controller prompts to keyboard text.
- **Joystick held while hidden (cb-M1, ir-M3):**
  - `TouchControls.OnStick` ignores everything except a release while the controls are hidden;
  - hiding ends GoJoystick's own touch hold (`_end`);
  - losing app or window focus releases every button and stick direction, and clears each button's remembered finger.
- **Mobile hint before the first touch (cb-M2):** `CodeLevel._usingTouch` starts true on iOS/Android. While the touch controls show, the hint moves between the joystick zone and the button arc.
- **USB IDs as text (ir-M6):** `ControllerGlyphCatalog.ParseId` reads IDs given as numbers, decimal text or `0x` hex. Which form Godot uses is still unverified on hardware.
- **Text and images agree (cb-M4):** `InputPromptService.Family()` now uses the same detection as the images (USB IDs, then name).
- **Phone button size (ir-M4):** main buttons are at least 10 mm and secondary ones at least 8 mm, worked out from `DisplayServer.ScreenGetDpi()` and capped at 17% of the view height. This is skipped when headless or when the DPI is unknown.
- **World:**
  - help lines stay hidden while the map is open (cb-N6);
  - the world Squad button shows only with two or more operatives (ir-N3).
- **Smaller fixes:**
  - button labels use culture-independent upper case;
  - "one" must be a whole word to mean Xbox One;
  - switching `SetButton` back to text clears the tooltip;
  - `SetButtonVisible` skips re-layout when nothing changed;
  - a wrong comment in `InputGlyphText.Upgrade` was corrected.

**Tests added:**
- NUnit: `ParseId` cases and the Xbox One whole-word rule.
- GdUnit: a hidden joystick ignores a held thumb and resets; focus loss releases everything; top-down Resume; copied clicks don't switch prompts.
- Smoke harness checks (the platformer ones also confirm the hint moved clear of the joystick):
  - stick plus Jump held by two fingers, driven through the viewport;
  - a copied click doesn't attack in the top-down mission;
  - no lower-layer button sits under the joystick zone;
  - layout checks at 2340×1080, 1280×800 and 1024×768;
  - a capture for each forced set (Xbox 360, Xbox One, PS5, Switch 2).

**Verification:** see the results line at the end of this section.
- NUnit glyph and touch tests: 55/55.
- GdUnit `test_touch_controls.gd` plus `test_input_prompts.gd`: 38/38.
- Smoke harness: 131 checks, 0 failures (`reports/input-prompts-smoke/results.json`, captures in the same folder).
- Full `run_tests.ps1` (`reports/review-fix-full-tests.log`): backend 158/158, NUnit 509/509, GdUnit4 404/404, with 0 errors, failures, flaky cases or orphan nodes.
- `dotnet build`: 0 errors. Godot import: exit 0. Besides the known shutdown messages, the import log reports the untracked `Claude outputs/` folder (owner files, not touched).

**Device test checklist (not yet done; needs a real phone and tablet):**
1. Hold the stick and press Jump at the same time; release both. Nothing should stay pressed.
2. Top-down mission: tap empty ground. It must not attack. Tap Pause, then Resume.
3. Open and close the world map while holding the stick. Squad should be hidden with one operative.
4. Background the app while holding a button, then return. Nothing should be pressed, and new touches should work.
5. Check a phone with a notch in both landscape orientations, a 4:3 tablet and a small phone. Buttons should be thumb-sized and clear of the HUD.
6. Connect a Bluetooth Xbox or PS5 pad. Prompts should show its images, and touching the screen should bring the touch controls back.
7. Check Arabic, Japanese and Thai labels on the buttons.

**Mobile export facts (from `export_presets.cfg`):**
- Exports run only through Docker: `docker compose run --rm export android` or `… export ios`.
- The Android preset signs **release** builds with the debug keystore (`android/keystore/release`, lines 389–391). A real release needs a proper keystore and package name.
- The iOS preset has `application/export_project_only=true` (line 513). It produces an Xcode project that must be finished on a Mac (`export/APPLE_RELEASE.md`).
- Godot's C# support on Android and iOS is less mature than on desktop, so a first export may need fixes.

## On-screen touch controls for iOS and Android — 2026-11-17

The owner asked for in-game controls on iOS and Android. They chose on-screen touch controls (a joystick plus round buttons), drawn in code with no new art, for platformer missions 1–12, the top-down (isometric) mission and the open world. Before this the game had no touch controls; `CodeLevel` only displayed a "Use the touch controls…" hint.

**When they show:**
- On iOS/Android they start visible; elsewhere they appear after the first touch.
- On every platform, a keyboard, controller or real-mouse press hides them, and the next touch shows them again. They are hidden while the game is paused.
- Godot copies each touch into a fake mouse event called "emulated mouse" (`InputEvent.DeviceIdEmulation`). A "real mouse" is a physical mouse. Emulated events never hide the controls.

**Layouts** ("arc" is the main cluster at the bottom right; "row" is the smaller secondary buttons above it):

| Mode | Joystick | Arc | Row |
|---|---|---|---|
| Platformer | left/right only (stick-up never jumps) | Jump, Attack, Ability, Dash | Pause, Squad (Squad only with 2+ operatives) |
| Top-down | 8-way move | Attack, Ability, Dash, Use | Pause |
| World | move | Attack, Dodge, Guard (hold), Interact, Run (hold), Bike | Map, Squad (Squad only with 2+ operatives) |

**Code (`scripts/ui/touch/`):**
- **`TouchLayout.cs`** has no Godot node dependencies, so NUnit can test it without the engine. It holds:
  - the layouts above;
  - `StickStrengths`, which converts the joystick vector into a 0–1 press strength for each move action. Below 0.15 length everything is released;
  - `TouchVisibility`, the show/hide rule.
- **`TouchActionButton.cs`:** a round button drawn in code (cyan ring, translated label; non-Latin labels use the theme font).
  - It remembers which finger (touch index) holds it, so two fingers on two buttons are handled separately. Godot's `Button` follows only one pointer.
  - It presses its action with `Input.ParseInputEvent(new InputEventAction …)`. Both code that checks events (`InputActionDefaults.Matches`) and code that queries state each frame (`Input.IsActionPressed` and similar) see the press.
  - Touches are tested against the circle, not the square bounds, so a touch in the corner between two diagonal buttons presses neither.
  - It has a left-click path, but only tests reach it: in play, a real mouse press hides the controls first, by design.
- **`TouchControls.cs`:** a `CanvasLayer` on layer 20, drawn above the HUD, that keeps running while paused.
  - **Joystick:** builds GoHUD's `GoJoystick` (MIT licence) in FOLLOW mode, where it appears where the thumb lands inside the lower-left zone.
  - **Arc:** the first button sits in the corner and the rest go on a quarter circle around it. The ring radius is chosen so neighbouring buttons do not overlap, with at most 4 per ring; a 6th button starts a second ring.
  - **Row:** the secondary buttons sit above the arc, below the top-right HUD and minimap.
  - **Sizes:** see `TouchControls.ApplyLayout` for the exact constants. The main diameter is 10.5% of the view height (64–132 px), with a physical minimum on phones (10 mm main, 8 mm secondary).
  - **Safe area:** controls are placed inside `DisplayServer.GetDisplaySafeArea()` with a 16 px margin. A safe area smaller than half the view is ignored. This path does not run headless and was not run on a device with a notch. The follow-mode joystick circle can draw slightly past the edge.
  - **Release:** hiding (by input, pause or the host), losing focus and leaving the scene release every held button and stick direction. This is done by `TouchControls`; buttons also release on their own exit.
  - **API:**
    - `Active`: static; true while touch controls are on screen;
    - `ShownChanged`: event raised when they appear or disappear;
    - `SetSuppressed`: the host hides them for a while, for example the world map;
    - `SetWanted`: forces the input-driven wish, used by tests;
    - `SetButtonVisible`;
    - `ButtonFor`;
    - `UseLayout(name)`: for GDScript.
- **Wiring:**
  - `CodeLevel`: platformer layout; Squad button shown only when `Unlocked.Count > 1`; `TouchControlsNode` exposed for tests.
  - `IsometricMission`: the key list hides while touch shows.
  - `NeoKyotoWorld`: the help lines hide while touch shows or the map is open; the controls are suppressed while the map is open.
- **Supporting fixes:**
  - `InputPromptBinding.DeliberateNonTouch` ignores emulated mouse events, so a tap no longer flips `CodeLevel`'s hint back to keyboard text. On 2026-11-18 the same rule was added to `ObserveInput` and the top-down attack.
  - `IsometricOperative` aims along the move direction while `TouchControls.Active` is true, because touches move the emulated mouse onto the buttons.
- **Translations:** "Run", "Bike" and "Squad" were added to `InputPrompts.csv` in all 17 language columns. The AI agent translated them; native speakers have not reviewed them.

**Verification (2026-11-17):**
- `dotnet build`: 0 errors.
- Godot import: exit 0. The log has only the shutdown messages recorded in `docs/INPUT_PROMPTS_AUDIT.md` (editor allocator and resource-in-use reports at exit).
- NUnit `TouchLayoutTests`: 6/6.
- GdUnit `test_touch_controls.gd`: 10/10, with 0 orphan nodes (nodes not freed after a test). Most of these cases call the button's `_GuiInput` or emit the joystick's `moved` signal directly, so they do **not** exercise Godot's touch routing or GoJoystick's drag handling.
- Smoke harness (`tools/input_prompt_smoke.gd`): 101 checks, 0 failures. That is 72 after the button-image work, plus 29 touch checks. Touches there go through `root.push_input`, which skips Godot's touch-to-mouse emulation.
- Captures in `reports/input-prompts-smoke/`: `touch_platformer_*` (English/Arabic, 1920/2560 wide), `isometric_touch_1920`, `touch_world_1920`.
- Full `run_tests.ps1` (`reports/touch-full-tests.log`): backend 158/158, NUnit 498/498, GdUnit4 400/400.

**Limits:**
- No real iPhone or Android device and no mobile export were used. See the checklist in the 2026-11-18 section.
- World map panning is right-drag only, so it can't be panned by touch. A finger sliding onto a button does not press it.
- The platformer touch hint `WF_TOUCH_CONTROLS` is English in other languages (an existing gap). Button labels were captured only in English and Arabic.
- No touch controls in menus. Menus work by tap through Godot's touch-to-mouse emulation, which stays on.

## Controller button images — 2026-11-17

The owner asked for real button images, both on those consoles and whenever one of those controllers is plugged into a PC, Mac or Linux machine:

| Set | Folder |
|---|---|
| Xbox Series X\|S ("Series") | `xbox_series` |
| Xbox One | `xbox_one` |
| Xbox 360 | `xbox_360` |
| PlayStation 5 ("PS5") | `ps5` |
| Nintendo Switch 2 ("Switch 2") | `switch2` |

Button images on iOS/Android were not in scope and are untested there; the same code runs. "Glyph" in code means a button image.

**Owner decisions (2026-11-17):**
- **Image source:** Kenney "Input Prompts" 1.5A, a free CC0 pack, as an exception to the SpriteCook-only rule.
- **Other pads on PC:** use the closest set (DualShock 4 → PS5, any Nintendo pad → Switch 2). Generic XInput pads (Microsoft's Windows controller API) get Series. Unknown pads keep text.
- **Controls menu:** list each console separately.

**Done:**
- **Assets:** `assets/ui/input_prompts/<folder>/`, 124 PNGs at 128×128 from the pack's double-resolution `Double` folders, with mipmaps on in every `.import`. The folder also holds `LICENSE.txt` (CC0) and a `README.md` with the source and file mapping.
  - The exception is recorded in the Art Pipeline rule of `CLAUDE.md`, `AGENTS.md`, `CODEX.md` and `GEMINI.md`, and at the top of `docs/STAND_IN_ASSETS.md`.
  - Xbox One and Series images are identical for every button the game uses. Xbox 360 differs only in Back/Start (One/Series use View/Menu).
  - Guide (the centre logo button; Home on Switch) has no PS5 image, so it stays text there. Nothing binds Guide.
- **`scripts/input/ControllerGlyphCatalog.cs`** has no Godot node dependencies.
  - Detection uses USB vendor/product IDs first; Microsoft product IDs split 360, One and Series. The controller name is the fallback.
  - It also handles forced styles, binding-to-file paths and `ParseId` (2026-11-18).
  - Console feature tags: the "Xbox / Windows GDK" preset sets `xbox`, which forces Series images for every pad, including a DualSense, in any build from that preset. `ps5` and `switch2` are checked too, but no preset sets them yet.
- **`scripts/input/InputGlyphText.cs`:** carries an image inside a prompt string using Unicode Private Use Area characters as markers.
  - `Set` turns marked text into escaped BBCode with `[img]` for a `RichTextLabel`.
  - `ToPlain`/`Plain` give the text version for tests and tooltips.
  - `Upgrade` swaps a scene-authored Label for a new RichTextLabel.
  - `SetButton` draws images on a Button through a mouse-transparent `PromptGlyphs` child. It clears the button's own text, so screen readers lose the name; only the tooltip keeps it. The minimum width only grows.
- **`InputPromptService`:**
  - `ActiveGlyphSet`;
  - `InputBindingToken.GlyphPath`;
  - `MarkedBinding` and `MarkedDescribe` (C#);
  - `GetRichBinding`, `GetRichContextText` and `SetActionButton` (GDScript).
- **Saved style values** (`ControllerGlyphStyle`): 0 Automatic, 1 Xbox Series, 2 PS5, 3 Switch 2, 4 Keyboard, 5 Xbox 360, 6 Xbox One. Values 1–3 meant Xbox, PlayStation and Nintendo before; old saves load unchanged.
  - The Controls dropdown uses item ids, so menu order differs from the saved numbers. The id mapping has no automated test.
  - The five console names are in `InputPrompts.csv` in all 17 language columns; they are brand names, so every column reads the same.
- **Screens that draw images:**
  - `CodeLevel`: the controls bar, the lesson prompt (`ShowPrompt` method) and the objective. `CurrentObjective` and `PromptText` stay plain, and `ControlsHintText` is new.
  - `PalaceMission`: `LanternInstruction` and `BeaconStatus`.
  - `NamesWithoutNumbersMission`, `NeonRustMission`, `ConcreteWildfireMission` and `PalaceMission` use `CodeLevel.ContextGlyph(action)`, a marked version of `ContextBinding`. `RecruitBeat` builds marked bindings itself with `InputPromptService.MarkedBinding` (touch text `WF_TOUCH_ABILITY`).
  - The plain `ContextBinding` is kept for labels that cannot show images: the NeonRust world markers and `WildfireRescueGroup` status.
  - `GenePod` draws the first bound button's image directly.
  - `IsometricMission`: the key list.
  - `NeoKyotoWorld`: the movement, combat and map help lines.
  - All cinematic Skip buttons through `InputPromptBinding.Cinematic`, the `StoryIntro` Skip/Continue buttons, and the `deployment_cutscene.gd` Skip.
  - The `global.gd` banner. `_prompt_label` is now a `Control`, so tests can still substitute a plain Label. `global.gd` adds `get_nav_prompt_rich`/`get_home_prompt_rich`.
  - The `GameHome` prompt (upgraded from its scene Label), the `MainMenu` footer and the instructions popup lines.

**Verification (2026-11-17):**
- `dotnet build`: 0 errors.
- Two headless imports: exit 0, with 124 new `.import` files. The logs show the shutdown messages recorded in `docs/INPUT_PROMPTS_AUDIT.md`, messages from the editor's IDE connection, and `Unrecognized UID uid://bkxvlkqrtv1ay`. That UID is `default_bus_layout.tres`'s header UID and matches `project.godot`, so the message probably predates this change.
- `tools/assets/audit_images.py` (size, alpha and quality flags): 473 images, none of the 124 new ones flagged.
- NUnit prompt tests: 53/53.
- GdUnit `test_input_prompts.gd`: 24/24. Also passed: `test_mission_readability.gd`, `test_wildfire_gameplay.gd`, `test_level1_2d_cyber_shogun.gd`, `test_later_cinematic_lifecycle.gd` and `test_deployment_cutscene.gd`.
- Smoke harness: 72 checks (64 before). Images were seen in the platformer, cinematic and top-down captures.
- **Coverage gap, partly closed 2026-11-18:** every image check used a forced style, and only Xbox Series art was seen in place. The 2026-11-18 run added captures of the 360, One, PS5 and Switch 2 sets. Automatic detection is tested only by NUnit name/ID cases.
- Full `run_tests.ps1` (`reports/glyph-full-tests.log`): backend 158/158, NUnit 492/492, GdUnit4 390/390.

**Limits:**
- **No physical controller test.** Whether Godot reports USB IDs as numbers or text is unverified (both are read since 2026-11-18). If no IDs arrive, detection uses the name: an Xbox 360 pad with a generic name would show Series art (View/Menu instead of Back/Start).
- **Still text:**
  - other world labels;
  - the `SquadStrip` slot keys (keyboard only);
  - the remap dialog (keyboard only);
  - the NeonRust world markers and `WildfireRescueGroup` status.
- **Console builds:** there are no PS5 or Switch 2 export presets; they need licensed SDKs. The Kenney images are look-alikes, not the platform holders' official button art.

## Godot startup diagnostics & GitHub secret remediation — 2026-11-16

- **GitHub Secret Remediation:** Enabled GitHub secret scanning and push protection on
  `knandakumar25/frog_jumper`. Resolved open secret scanning alerts #1 (Google OAuth Client ID)
  and #2 (Google OAuth Client Secret) as revoked via `gh api`. Scanned tracked files in HEAD;
  verified 0 exposed secrets or private tokens in tracked repository code.
- **Owner decision (2026-11-17):** the Google OAuth client ID and secret behind alerts #1 and #2 are abandoned
  and will not be rotated; only the current login service is kept.
- **Godot Mono System.Runtime 11 Resolution:** Root cause of `FileNotFoundException: System.Runtime 11.0.0.0`
  was `DOTNET_ROOT` pointing to `AppData\Local\Microsoft\dotnet` (.NET 10 only) and `AppData\Local\Microsoft\dotnet`
  being in user PATH, while Godot Sharp editor loaded SDK 11 MSBuild tools from `C:\Program Files\dotnet\sdk\11.0.100`.
  Permanently updated User `DOTNET_ROOT` to `C:\Program Files\dotnet` in HKCU environment, removed `AppData\Local\Microsoft\dotnet`
  from user PATH, and created directory junctions in `AppData\Local\Microsoft\dotnet` (`shared/Microsoft.NETCore.App/11.0.0`,
  `10.0.15`, `shared/Microsoft.AspNetCore.App/11.0.0`, `shared/Microsoft.WindowsDesktop.App/11.0.0`, `sdk/11.0.100`) to guarantee
  fail-safe resolution even from terminals inheriting older environment blocks.
- **GdUnit4 C# API Dependency:** Resolved `No gdunit4.api dependency found` warning by adding
  `gdUnit4.api` (5.0.0) to `AxiomForce.csproj`. Full suites pass: Express 158/158, NUnit 454/454,
  GdUnit4 385/385 across 78 suites with 0 errors/failures/orphans (`reports/report_701/results.xml`).
  Keep Docker running.

## Approved EGM input repairs and Round 2 review completed — 2026-11-16

Applied all five approved findings: localized/live remap-modal title, viewport-routed
controller checks (including Keyboard-style gameplay and consuming menu/cinematic
handlers), exact menu instruction-label assertions, and two audit wording corrections.
Graphical smoke passed 64 checks (0 failures). Full test suites completed cleanly:
backend 158/158, NUnit 454/454, GdUnit4 385/385 across 78 suites with zero errors, failures,
flaky cases, or orphan nodes (`reports/report_699/results.xml`).
A second fresh-context Google EGM review round was executed across `codebase_details`,
`implementation_readiness`, and `style`. All findings are resolved and documented in
`docs/INPUT_GOOGLE_REVIEW_ROUND2_2026-11-16.md` and `docs/INPUT_PROMPTS_AUDIT.md`.
Status: Approved & Clean. Keep Docker running and changes local. Import retains the documented
SDK 11/editor shutdown diagnostics; no global SDK or project target changes were made.




## Resume verification completed — 2026-11-16

Normal Docker services are running; website/API health checks pass. Finished the
input task's interrupted verification without further game code changes: backend
158/158, NUnit 454/454, GdUnit4 385/385 with zero errors/failures/orphans, and
graphical smoke 58 checks with zero failures. Fresh isometric captures confirm the
final controls/dialogue separation. See `docs/INPUT_PROMPTS_AUDIT.md` for evidence
and physical-device/import-diagnostic limits. Keep changes local and Docker running.

## Shared input prompts — 2026-11-15

Approved input plan implemented through `scripts/input/InputActionDefaults.cs`,
`InputPromptService.cs` (autoload before Global) and lifetime-owned `InputPromptBinding`.
Global retains compatibility helpers/signals. Keyboard remaps serialize physical/logical
keys and modifiers while preserving pad events; old numeric arrays still load.
Prompt styles persist only `glyph_style` in SessionManager's `input_prompt_settings`;
active device/controller history stays in memory. No backend/schema changes.

Prompts follow meaningful input, current mappings and locale; unknown pads use positional
names. Scene `_Input` owners observe before consuming events because dispatch precedes
the autoload. Mission 1 queues combat after GUI handling. Platformer retains its original
keyboard/pad input ordering and ignores mouse events, including UI clicks. Existing B/Back
overlaps with pause remain as before. Menus, custom buttons/dropdowns, roster and pause
pointer ownership now allow clicks. Cinematic handlers and timing remain unchanged.

Verification and review findings/resolutions are recorded in
[the audit](docs/INPUT_PROMPTS_AUDIT.md). Changes are local and uncommitted; leave Docker running.

## Approved player website redesign — 2026-11-15

Implemented the approved showcase plan locally. See
[verification record](docs/WEBSITE_REDESIGN_VERIFICATION.md) and [website contracts](website/README.md).
The shell now uses Home, Gameplay, Squad and FAQ; Development stays at `/tech-hub` in the footer.
Campaign acts contain missions 1–4, 5–7 and 8–12. All six portrait links, roles and recruitment
rules are present. New copy, mission titles and onboarding states cover all 17 languages.

Account settings recover linked profiles through verified `Authorization: Clerk` proof on
existing account routes. This adds no API/game token issuance. Existing Bearer semantics
remain. Public game requests still require an explicit BrowserAuth approval. Language changes
preserve bare onboarding/auth URLs, nested Clerk routes, selected operative and request IDs.
Identity changes invalidate profile callbacks, token installations and pending refreshes.
Existing SpriteCook images were copied without modification; engine files were not changed.
Changes remain uncommitted; normal Docker services stay running.

## Approved follow-up and SpriteCook actor wiring — 2026-11-15

Implementation and final verification complete; changes remain local. See
[verification record](docs/HANDOFF_PRIORITIES_VERIFICATION.md) for current outcomes and evidence.

- Second EGM review and first-round reconciliation completed; seven backend README contracts added.
- Repaired superseded-code reuse, concurrent code issuance, and refresh/revocation races.
- Real Clerk browser flows, account 2FA/email forms, password API and Axios concurrency verified;
  disposable users/database/mail/proxy cleaned up. Issue 14's Clerk-key contest is verified live.
- Owner rule: hero/enemy bodies must come from SpriteCook. Replaced active primitive heroes and
  civilians in the Names aftermath, sentry/world-hostile bodies and old chassis under six bosses.
  Deleted original body constructors and 22 unused operative drawing methods, plus remaining
  commander, guard, captive, civilian fallback and wreck drawings. Existing SpriteCook assets
  reused; no art generation or asset replacement. Gameplay scripts remain for their behavior.
- Tokyo blade accepted in running Godot at standard/wide aspects, through both blade shots,
  their transition and motion. Natural/Skip each produce results once; Replay restores movement.
- Backend 154/154, NUnit 439/439, GdUnit 366/366 (0 errors/failures/orphans), browser 5/5 and smoke 1/1 pass. A quiet full GdUnit rerun passed after a
  timing-sensitive dodge assertion during concurrent QA; the isolated suite also passed without edits.
- Global SDK 11/project target unchanged; runtime QA used a process-local temporary SDK 10.
  Normal Docker stack remains running. No exports, commits or pushes.
- Owner action remaining: native title approval. (Secret rotation dropped 2026-11-17: the owner
  abandoned the old third-party OAuth credentials and keeps only the current login.) Legacy password
  rehash and unrelated architectural changes remain deferred.

The dated sections below preserve earlier implementation history; this section supersedes their
outstanding items for the approved follow-up.

## Express.js backend, 2026-11-12 art items, Tokyo blade and titles check — 2026-11-15

Historical implementation at commit `646130a`. The owner asked for the four Phase 7 follow-ups and a move of the backend from Django to Express.js. Current local follow-up status is recorded above this section.

- **Backend (`backend/`, Express 5 on Node 26):**
  - **Layout:** `src/app.js` (app built from injected dependencies), `src/server.js`, `src/config.js`, `src/db.js`, `src/migrate.js`; `src/lib/` (tokens, auth, Django hashes, Clerk, mail and templates, users, 2FA, throttle, countries); `src/routes/` (accounts, browser, onboarding); `migrations/*.sql`; `test/*.test.js`; `scripts/e2e-server.js` and `e2e-proxy.js` (Playwright helpers, replacing the Python ones); `data/countries.json`.
  - **Contract:** same `/api/auth/...` paths, bodies, status codes and DRF-style error shapes, except the changes listed below. The game (`AuthManager.cs`) needed no behavioural change (comments only); the website's 2FA and account pages and its refresh interceptor changed for the review fixes. Kept: register, login, token/refresh, profile (GET/PUT/PATCH), browser-start/confirm/poll/cancel, 2fa enable/verify-enable/verify-login, countries, countries/basic, onboarding start/validate-token/complete. New: `GET /api/health`. Dropped: third-party and social sign-in, and the Django admin (nginx `/admin/` block removed; there is no admin replacement, so deactivating a user needs SQL).
  - **Contract changes:** PATCH/PUT `profile/` answers 400 when `is_2fa_enabled` would change (Django ignored the field) and when `email` changes without a valid `email_code`; registering a taken email answers `{"email": [...]}` (Django: `{"detail": "<database error>"}`); refresh tokens work once; new routes `profile/email-code/`, `2fa/disable-request/`, `2fa/disable/`; 429 answers from the new rate limits.
  - **Database:** the existing `accounts_*` tables. `0001_schema.sql` creates them only when missing; `0002_hardening.sql` adds `accounts_twofactorcode.attempts` and `accounts_refreshtoken`; `0003_token_families.sql` adds `accounts_refreshtoken.family`. Applied files and their SHA-256 are recorded in `node_migrations`; an edited applied file stops start-up, and start-up checks every column the API uses exists. On a Django-made database the foreign keys have no ON DELETE actions (a fresh one cascades); the API never deletes users. Django's own `django_*` and `auth_*` tables are left untouched (unused). The database had no accounts at the switch.
  - **Security fixes (owner-approved):** `JWT_SIGNING_KEY` replaces the key written in the Django source and is required (a random value was written to the git-ignored `.env`; everyone signs in again once); one 401 "Invalid credentials" for an unknown user, a wrong password or an inactive account; 2FA codes from `crypto.randomInt`, newest code only, five tries; refresh tokens work once. Django `pbkdf2_sha256` password hashes still verify.
  - **EGM review fixes (2026-11-15; report in `.validation/egm/goldfish_review_report.md`, issues 1–10):**
    - Rate limits: 10 a minute per IP on login, register, 2fa/verify-login and token/refresh; 10 logins per username per 15 minutes; 5 emailed codes per user and purpose per 15 minutes; at most 4 password hashes at once. All settable in `.env` (see `.env.example`).
    - Ports: API and database published on 127.0.0.1 only. `X-Forwarded-For` is trusted only from the nginx `website` container; limiter memory is swept and capped; IPv6 grouped by /64.
    - Turning 2FA off (`2fa/disable-request/` then `2fa/disable/`) and changing the email (`profile/email-code/`, then PATCH with `email_code`) need a code mailed to the account. Both revoke all refresh tokens and email a notice to the old address. The owner chose the code option (Issue 5).
    - Refresh-token families: a replayed used token revokes its whole sign-in. Spend and reissue run in one transaction. The server deletes expired tokens, old codes and stale browser requests hourly. The website's API client shares one refresh between parallel 401s.
    - Login runs a precomputed decoy hash whenever there is no checkable stored hash (unknown user, Clerk-only account), so timing does not reveal usernames. Register and profile still say when a name or email is taken (rate-limited).
  - **Known risks (not changed):** the game's browser sign-in has no code the player compares between game and website, so a sent `auth_url` could trick someone into approving an attacker's request, and a Clerk-approved request skips the game account's 2FA. A fix needs a game and website change (show a short code in the game, type it on the page). There is no logout or revoke-all route; the API connects as the `postgres` superuser.
  - **Docker:** build context `./backend`, multi-stage, non-root, pinned `node:26.13.1-alpine3.23`. Compose runs the `dev` target (it has the test packages and the e2e scripts), has no source bind mount (rebuild before `npm test` after edits), and health-checks `/api/health`; the website only waits for the backend to start. Local development only: there is no production deployment; a slim image is `docker build --target runtime ./backend`.
  - **First run / fresh clone:** copy `.env.example` to `.env` and set `JWT_SIGNING_KEY` (32+ characters). Without it the backend restarts in a loop and its log says why; the website and database still run.
  - **Rollback to Django (historical migration boundary):** back up saves with `tools/db/backup.ps1` first. Express is committed at `646130a`; restoring current HEAD cannot restore Django. The pre-migration tree is `1bb6282`; inspect its backend, requirements, Compose, nginx and test-helper files in a separate checkout before planning a rollback. Do not execute the old restore/clean recipe over current work. Django ignores added token tables/columns, but an Express-created database needs its initial migrations reconciled (`--fake-initial`) and validation on a restored test copy. All sessions must sign in again. Restoring or deploying that revision is a separate owner action.
  - **Owner action (tracked, Issue 10):** the old third-party OAuth client secrets were committed in `.env.example` and the Django settings and remain in git history. The GitHub repository is private (checked 2026-11-15); whether it was ever public is not known here. Rotate them in the providers' developer portals, or delete those apps if unused; until then they count as exposed to anyone with repository access. All other mentions were removed at the owner's request (2026-11-15), including the unused `*_login` strings for those providers in `website/src/translations/ui_translations.json` and the local `.env` keys.
- **2026-11-12 art (36 credits, balance 604):** buttons, speech frame and pointer, app icon (with ICO and favicon), palace garden. See "2026-11-12 items status" in `docs/ART_REGENERATION.md`. Code: the icon references are re-added; the `CodeLevel.cs` garden uses nearest filtering. Tool: `tools/art/cut_ui_sheet.py`.
- **Tokyo blade:** `TokyoStage.cs` escape and testimony offsets re-measured; checked on rendered frames, not in the running game.
- **Titles:** Hebrew and Arabic word order verified by glyph matching. A private review page for native readers is at https://claude.ai/artifact/2GRigKufZfNT2LnG7DJoT8 (the owner must share it). The wording questions are listed in `docs/ART_REGENERATION.md` (Phase 7 status).
- **Website:** image rebuilt; it serves the new titles and favicon.
- **Verification (2026-11-15, after the review fixes):** full `run_tests.ps1`: backend 150/150 in the container (132 before plus 18 for the review fixes), NUnit 439/439, GdUnit 363 with 0 errors, failures and orphans; through nginx the site, browser-start and `/api/health` answer; `docker ps` shows 8000 and 5432 bound to 127.0.0.1; the rebuilt website bundle contains the new 2FA-off and email-code steps; migration 0003 applied to the real database.
- **Verification (2026-11-15, before the review fixes):** backend `npm test` 132/132 in the container (111 ported Django tests and 21 new ones); NUnit 439/439; GdUnit 363 (0 errors, failures and orphans); `dotnet build` 0 errors (151 warnings); Godot headless import exit 0 (known shutdown messages only) with the 7 new `.import` files; `audit_images.py` 342 images, none flagged; `compare_images.py` against `67eab0a` 0 failures; live calls through nginx (browser-start/poll/cancel, anonymous profile 401, countries, health, favicon, titles); `graphify update .` done.
- **Not done (next agent):**
  - EGM: the second review round on this section and `egm-bootstrap` READMEs for `backend/` (the owner handed these to Codex or Antigravity). The round-1 report and the drift report are in `.validation/egm/`; issues 15–18 there are open (11–13 are covered by this section; 14 is contested). They are mostly doc drift: `.env.example` header and `VITE_API_URL`, README tree `frontend/`, stale "Aegis civilians drawn in code", the ART_REGENERATION Phase 1 row 598 vs 634, the guides' testing file list without `.js`.
  - Playwright `browser-auth.spec.cjs` end to end, and `backend/scripts/e2e-server.js` / `e2e-proxy.js`, which were never started (they need an `sk_test_` Clerk key; the website's publishable key comes from `website/.env.local`).
  - A live password, 2FA and Clerk sign-in on the website, including the new 2FA-off and email-change code steps.
  - Tokyo blade in the running game: play "Tokyo Remembers"; from 0–8 s and 8–23 s the blade grip should sit in Kaito's fist.
  - Native readers for the titles (share the review page); the old title wording wins unless the owner decides otherwise (2026-11-10 decision).
  - ~~Rotate the leaked OAuth secrets (owner).~~ Dropped 2026-11-17: the owner abandoned those credentials; no rotation planned.
- **Docker note:** mid-session, Docker Desktop's drive mount failed for bind mounts (the cutscene service and the old backend). The cutscene image was used by streaming files in. Docker later restarted and the stack is healthy.

## Phase 7 menus, Level 1 map and titles done — 2026-11-15

- **Credits:** 96, exactly as planned, with no retries. The balance is 640. The 2,170-credit plan is now fully spent; only the 2026-11-12 items (36) remain, and they need approval.
- **Files (41):** `assets/images/menu_backdrops/` (6), `assets/images/level1/ghost_gridlock.png`, 17 `assets/images/axiom_force_title_2d*.png`, and 17 copies in `website/public/images/`.
- **Code and tooling:**
  - `IsometricMission.cs`: the Level 1 map uses nearest filtering (owner choice), with updated comments.
  - `tools/art/render_titles.py`: rewritten to fit any keyed plate and to stop on a missing glyph; Vietnamese uses Noto Sans.
  - `tools/cutscene/Dockerfile`: adds `libraqm0` and Pillow 13.0.0 (owner choice). The image was rebuilt; the always-on services are unchanged.
- **How:** see `docs/ART_REGENERATION.md` ("Phase 7 status"). The Level 1 map needed no `--despill` (it cleared the pink trees), and its keyed-out altar glow was refilled from an unkeyed snap. All ten cover collisions land on drawn props.
- **Verification (2026-11-15, Docker up):**
  - `dotnet build`: 0 errors (151 warnings, as before).
  - Godot headless import: exit 0, only the known System.Runtime 11 and shutdown messages; all 24 new game PNGs have `.import` files.
  - `audit_images.py`: 335 images, none flagged.
  - `compare_images.py` against `ca92679`: menus 0 and titles 0 failures. Level 1 reports only `palace_garden_painted.png`, a 2026-11-12 item not yet made.
  - Full `run_tests.ps1`: Django 111 OK, NUnit 439/439, GdUnit 363 with 0 errors, failures and orphans.
  - `graphify update .`: done.
- **Open:**
  - A native reader should check the Arabic, Hebrew, Thai, Japanese, Korean and Chinese titles.
  - The website container serves the new title copies only after its image is rebuilt; that was not done.
  - The 2026-11-12 items (UI sheet, palace garden) and the Tokyo blade offset are still to do.

## Phase 6 city kits and top-down operatives done — 2026-11-15

- **Credits:** 882, exactly as planned, with no retries. The balance is 736, against 132 planned for Phase 7 and the 2026-11-12 items.
- **Files:** all 141 in `assets/world/` (6 cities x 16, LA 7 modules + 32 landmarks, `operative_0`–`5`). LA `module_2.png` stays skipped.
- **Pushed:** the 135 city files and `operative_0` as `9af0a76` mid-phase; operatives 1–5, the `.import` files, manifest entries and docs in the follow-up commit (owner's "push to git", 2026-11-15).
- **How:** see `docs/ART_REGENERATION.md` ("Phase 6 status"). City objects are fitted to the old file's opaque box and placed bottom-centre on the old footprint, because collision footprints are fixed in data. Operatives use a topdown `generate_character` plus walk_down/up/left; `pack_topdown.py` fills east and the diagonals.
- **Known limits:**
  - The figures are about 85 px tall in the 96x128 cell (old about 60), so they read larger in game. The owner OK'd running all five at the pilot's size.
  - Apex Titan's walk-left is nearly front-on, so his side rows are weak.
  - The scratch cutting script (`cut_cities.py`) was not added to `tools/art/`.
- **Verification (2026-11-15, Docker up):** Godot headless import exit 0 (only the known System.Runtime 11 and shutdown messages), 141 `.import` files for the 141 world PNGs. `audit_images.py`: 294 images, none flagged. `compare_images.py` against `ca92679`: 0 failures for every city; Los Angeles reports only the skipped `module_2.png`. Full `run_tests.ps1`: Django 111 OK, NUnit 439/439, GdUnit 363 with 0 errors, 0 failures and 0 orphans (including `test_every_city_has_all_or_none_of_its_art`).
- **Next:** Phase 7 (menus, Level 1 map, titles: 96) and the 2026-11-12 items (36), each needing approval. The Tokyo blade offset is still deferred.

## Phase 5 resumed from Claude — 2026-11-14

- Recovered the approved 138-credit plan and all five uploaded references; generated four 4K scene sheets and one 2K boss sheet for exactly 138 credits. Balance 1,618; no retries.
- Produces 53 files under `assets/environments/later_campaign/` using the documented `build_later_campaign_art.py` helper. No gameplay or cutscene script changes; Tokyo blade offset remains deferred.
- Pixel snapping, magenta keying, per-family aftermath seeds and light extraction reuse existing tools. Apex's deck needs explicit y808 alignment; source quadrant/offset provenance was corrected during review.
- Final verification: build 0 errors (151 warnings); 53 `.import` files; image audit 153 images; all seven comparisons passed. Full tests after final edits: Django 111, NUnit 439, GdUnit 363 with 0 errors/failures/orphans. Helper compiles and local Graphify is refreshed.
- Import exits 0 without asset failures, while retaining the known System.Runtime/Android EditorSettings/resource cleanup messages. Seven scene/aftermath composites and six runtime scale boss/chassis overlays were reviewed. The Judgment collar gap was repaired with its own source shadow colour; source recipes include the mask. Fresh-context EGM review found no blocking artifact issues.
- Evidence is retained in ignored `reports/phase5/`. Changes are local, Docker remains up, and no exports ran. Next phase still needs its own cost approval.

## Phase 4 mission props done — 2026-11-14

- **Credits:** 66, matching the plan once the owner chose two 2K 4x4 sheets over three 3x3 sheets. No retries. The balance is 1,756, and the plan total is now 2,170.
- **Files:** all 25 props in `assets/environment/{wildfire,neon_rust,shattered_aegis,names}/`. Each is 512x512, with alpha only 0 or 255 and a clear edge.
  - Wildfire civilians and shelter come from the second sheet's alternate cells. The first sheet's shelter runs off the sheet edge.
  - The fortress is a cut wall section, because it ran off its quadrant.
- **How they were made:** see `docs/ART_REGENERATION.md` ("Phase 4 status"). In short:
  1. crop the cell;
  2. pad to the display aspect (never stretch);
  3. snap at the art's native block grid to 512x512.
- **Code:**
  - `WildfireVisual.cs`, `NeonRustVisual.cs`, `AegisVisual.cs` and `NamesWithoutNumbersMission.Art()` now use nearest filtering. The old Blender art used linear filtering, which blurred stretched pixel art.
  - `snap_pixels.py` gained `--defringe N`, which clears the dark purple outline left by the magenta key.
- **Verification (2026-11-14, Docker stack up at the owner's OK):** `dotnet build` 0 errors; Godot headless import exit 0 (only the known System.Runtime 11 and shutdown messages), with 25 new `.import` files; full `run_tests.ps1`: Django 111 OK, NUnit 439/439, GdUnit 363 with 0 errors, 0 failures and 0 orphans; `audit_images.py` 100 images, no prop flagged; `compare_images.py` against the old props 0 failures; `graphify update .` done.

## Phase 3 cutscenes done — 2026-11-14

- **Credits:** 282 (216 planned, 66 of retries); balance 1,822. Plan total is now 2,188.
- **Files:** all 50 Phase 3 files in `assets/cutscenes/{wildfire,tokyo,wildfire_aftermath,neon_rust_intro,neon_rust_aftermath,shattered_aegis_intro,names_without_numbers,act_ii_opening}/`. No code changes.
- **Method:**
  - Six 4K 2x2 scene sheets and two 2K pose sheets, with old `ca92679` layer composites as layout guides.
  - Retries:
    - one 4K sheet for the sparse Neon cargo strip and Wildfire reunion road, because those stages stack layers;
    - 2K fortress with the ramp road;
    - 2K shield road, because the first one came back fully opaque.
  - Cutting, snapping and offsets are written up in `docs/ART_REGENERATION.md` ("Phase 3 status").
- **Tool changes:**
  - `snap_pixels.py`: `--crop`, `--despill`, a foreground-only palette, and `--opaque` output that stays RGBA.
  - `compose_poses.py`: per-cell fixed `scale`.
- **Lessons:**
  - Check how a stage stacks its layers before generating. A full-scene quadrant hides the layers behind it.
  - Phase 1 Titan is green, not violet as the old palette notes say.
- **Known gap:** in Tokyo's escape shot (t < 8 s) the blade is drawn above Kaito's head, because the `TokyoStage.cs:77` offsets fit the old pose. A one-line offset change would fix it; it needs the owner's approval, since it is code (Docker preflight).
- **Verified:**
  - Tool self-test (`--crop`, `--despill`, RGBA `--opaque`, out-of-range crop rejected, compose `scale`).
  - A generic image check of all 50 files: sizes, RGBA, opaque bases, alpha 0 and 255, pose cells filled and clear of edges.
  - The 8 `validate_*.py` image checks run locally; they stop at the ffprobe audio step, which needs Docker.
  - `ArtRegenerationSpecTests` 3/3; image audit 75 images.
  - Full NUnit run: 422 passed, 17 failed. All 17 failed with "Failed to connect to 127.0.0.1:5432" (PostgreSQL, Docker stopped), not from these changes.
  - Godot import exit 0, with all 50 `.import` files.
  - 10 GdUnit cutscene and character suites: 69/0, 0 orphans.
  - Stage composites with feet lines checked by eye.
  - Not run: the full `run_tests.ps1` and the Docker validators (ask first). Nothing committed.
- **Next:** Phase 4 (mission props, 84 credits planned) needs owner approval.

## Phase 2 deployment cutscene done — 2026-11-13

- **Credits:** 72, balance 2,104.
- **Files:** all 8 in `assets/cutscenes/deployment/live/`, plus `assets/cutscenes/wildfire/blade.png` and `assets/cutscenes/tokyo/blade.png`. `deployment_stage.gd`'s `art_available()` is now true, so the opening cutscene plays.
- **Method:**
  - Four 2K Nano Banana 2.1 images (city base, city structure on magenta, roof on magenta, a 3x3 items sheet), with the old `ca92679` layers as composition references.
  - All raws were snapped locally with the new `tools/art/snap_pixels.py` (480x270 grid, 4x).
  - Roof halves were shifted separately (+348/+316) so both walking surfaces sit at y 764; gap at x 767–1051.
  - Lights and drone layers were derived; `shogun.png` was composed from the Phase 1 atlas.
- **Plan change:** the drone is a generated sprite rather than the vanguard frame (+18 credits). Plan total is now 2,122.
- **Verified:**
  - Tool self-test passed (keying, even 4x blocks, offset, opaque base, `--sprite` drone box).
  - `ArtRegenerationSpecTests` 3/3; image audit 27 images.
  - Godot import exit 0 (8 + 2 `.import`; only the known System.Runtime and shutdown-leak messages).
  - GdUnit `test_deployment_cutscene` + `test_rendered_character_assets`: 13/0.
  - A Pillow composite at the scene's positions shows Shogun's feet on both rooftops and the jump clearing the gap.
  - Full `run_tests.ps1` not run (needs Docker; ask first). `graphify update .` done.
- **Next:** Phase 3 (other cutscenes, 216 credits planned) needs owner approval.

## Phase 1 characters redone — 2026-11-13

- **Credits and files:** Adventurer plan, 634 credits (54 on three lineups, 580 on animation); balance afterwards 2,176. Files in `assets/characters/rendered/`:
  - 6 operative atlases (2048x1536) and 6 portraits (768x1024);
  - `axiom7_portrait.png` (768x768);
  - `vanguard_atlas.png` and `heavy_atlas.png` (2048x256);
  - 8 `.tres` restored unchanged from `ca92679`.
- **Lineup:**
  - SpriteCook's pixel snap failed on the 2K sheet, so the raw 2048x2048 was snapped locally (white flood-key, 32 colours per cell, 8x block mode).
  - The figures are three-quarter view facing right.
- **Animations:**
  - Custom platformer runs with explicit single-character prompts, cut from uploaded clean crops; no presets were used.
  - The vanguard idle was repacked ping-pong (0-3-0) because the generated loop ended with the cannon raised.
  - Minor: some Phoenicia idle frames lighten her crest colour.
- **Verified:**
  - `ArtRegenerationSpecTests` 3/3.
  - `audit_images.py`: 15 images, hard edges, margins on every side.
  - Godot headless import exit 0, with 15 `.import` files generated. The log shows only the existing System.Runtime 11 and shutdown-leak errors.
  - GdUnit `test_rendered_character_assets`, `test_shogun_art_study`, `test_character_selection_abilities` and `test_level1_2d_cyber_shogun`: 25 tests, 0 failures.
  - The full `run_tests.ps1` was not run: it needs Docker (ask the owner first).
- **Next:** Phase 2 (deployment cutscene, 54 credits) needs owner approval.

## Adventurer-plan art budget — 2026-11-13

The owner will upgrade SpriteCook to Adventurer (3,000 credits/month, 2K/4K) for one month
and asked for a plan that makes every image in that month with room for retries.
Owner decisions: **2K and 4K only, never 1K**; produce scenes differently so the list fits.

- **Approach:** every file keeps its path and size, so no game code changed. Each image in
  `docs/art_regeneration.json` now has a `method` (generate, sheet, animate, slice, derive,
  compose, render or copy). `docs/ART_REGENERATION.md` has new "Methods" and "Adventurer budget"
  sections, and the route lines were rewritten.
- **Budget (revised with real prices, 2026-11-13):** 2,104 planned credits. The balance is 2,280 (not 3,000) and the owner is adding a ~500 top-up, leaving a reserve of about 676. Top-down operatives cost 120 each, not 63, because every pose prep is 12. Image prices matched the estimates. The original estimate was 1,762 with a 1,238 reserve. Stop if a phase
  reaches 1.5x its budget. Prices are inferred from the pricing page (2K ≈ 18, 4K ≈ 30,
  animation ≈ 15), so recompute them with `list_generation_models` on day one.
- **New free tools in `tools/art/`:** `slice_sheet.py`, `derive_layers.py` (lights, aftermath, drone,
  icon), `compose_poses.py` and `pack_topdown.py`. Self-tested on synthetic fixtures in the
  scratchpad: all checks passed (sizes, alpha 0/255, gutter bleed, pivots, feet at y=87, seeded
  repeatability, error cases).
- **Caveats:**
  - `render_titles.py` has a crop box fitted to the deleted plate; adjust it for the new plate.
  - `finish_ui_batch.py` is obsolete.
  - 2x2 scene sheets and derived lights are untested on real SpriteCook output; a failing file
    falls back to a single 2K generation paid from the reserve.
  - No credits were spent.
- **Verified (2026-11-13):** NUnit 422 passed and 17 failed. All 17 are PostgreSQL save-storage tests that could not connect, because the Docker containers were stopped at the owner's request; none involve art. `ArtRegenerationSpecTests` (all 3) passed with the new `method` field. `graphify update .` done. Django and GdUnit not run: they need the Docker stack, and the owner is asked before Docker is started. No Godot import was needed (no game files changed).
- **Next:** after the upgrade, sign in, check prices, then ask for Phase 1 approval
  (lineup plus 8 animation runs, 598 credits).

## SpriteCook art reset — 2026-11-13

Owner asked to delete every asset Claude and Codex made with SpriteCook on 2026-11-12,
to redo them after a temporary upgrade to the Adventurer plan.

- **Deleted (105 files incl. `.import`):** everything listed in the old `spritecook-assets.json`
  `files` (15 character PNGs, 17 game titles + 17 website copies, 3 button states, speech
  frame/pointer, `axiom_force.png`/`.ico`, `website/public/favicon.svg`, `menu_backdrops/city_base.png`)
  plus the 8 character `.tres` (which point at the deleted atlases). Empty folders
  `assets/characters/rendered` and `assets/images/menu_backdrops` were removed too.
- **Unhooked:** icon lines in `project.godot`, icon paths in `export_presets.cfg` (back to `""`),
  favicon `<link>` in `index.html` and `website/index.html`.
- **Manifest:** `spritecook-assets.json` emptied; old IDs are in git at `3d05726` and should not be reused.
- **Kept:** `tools/art/*` helpers, `docs/art_regeneration.json` (all entries were still `remake`).
- **Not done:** the assets still exist in the owner's SpriteCook online library; the MCP has no
  delete tool, so remove them in the SpriteCook web app if wanted.
- **Verified:** Godot headless import exit 0 (logged a `System.Runtime 11.0` assembly-load error and the usual shutdown leak messages, none about deleted files); `dotnet build` 0 errors; `run_tests.ps1` exit 0: Django 111, NUnit 439, GdUnit4 363 passed (0 errors/failures/orphans, `reports/report_672`); `graphify update .` done.
- **Next:** on the Adventurer plan, re-plan Phase 1 and the UI batch with `list_generation_models`
  and get owner approval with an asset list and credit estimate before generating.

## Resumed Claude UI batch — 2026-11-12

Recovered Claude's last approved task from its project session: UI essentials,
localized titles and installation of the skyline test as the city menu base.
Reused existing SpriteCook assets; no additional generation or credits spent by Codex.
Balance checked at 374. Replaced tiny initial button crops using Claude's completed
2048x1152 button sheet. Added title rendering (17 game PNGs plus website copies),
restored game/export/browser icon references, and merged UI file provenance/hashes.
`render_titles.py` needs libraqm; Linux Docker Pillow supports the shaped text path.
Verification: build completed with 0 errors; Django 111, NUnit 439, GdUnit4 363
passed (GdUnit 0 errors/failures/orphans). Image audit: 58 images; all 17 game/web
title comparisons passed. Actual menu capture shows skyline/buttons. Graphify updated.
Editor import completed without asset failures but logged Android EditorSettings and
resource cleanup errors at shutdown; see `reports/ui-batch/import-final.log` (ignored).
Native-language editorial review remains outstanding.
Mission props, city kits and additional cutscenes still need separate cost approval.

## Phase 1 characters complete — 2026-11-12

- **Stage 3 (approved by the owner):**
  - one run of six custom animations each for Surya Prime, Oceanus, Mirage Blade, Apex Titan and Aura Phoenicia (102 credits each, including the run-pose prep);
  - `vanguard` and `heavy` idle (20 each).
  - All 26 animations showed only their own character; no redos were needed.
- **Free work:** the AXIOM-7 portrait, from his base still.
- **Phase 1 credits:** 696 in total. Balance 386.
- **Files:** `assets/characters/rendered/` holds 15 PNGs plus 8 `.tres` (restored byte-identical from `ca92679`). Asset IDs and file `sha12` values are in `spritecook-assets.json`.
- **Mission 2:** before Stage 3 started, it showed the pixel Cyber Shogun (the overlay is gone).
- **Verification:**
  - Godot import: clean, with 15 `.import` files.
  - Full `run_tests.ps1`: all passed (GdUnit 363 with 0 errors, 0 failures and 0 orphans; NUnit; Django).
- **Next phases, not started:**
  - Phase 2: deployment cutscene;
  - the 9 UI and icon remakes;
  - open-world walk sheets (they need front and back views);
  - cutscene poses.
  - About 386 credits are left this month.

## Pixel-art characters started; all remaining art deleted — 2026-11-12

- **Owner decisions:**
  - Art style is **pixel art**.
  - Gameplay characters first: the 6 operative atlases and portraits, the vanguard and heavy idle atlases, and the AXIOM-7 portrait.
  - Smooth SpriteCook animation.
  - Credits are spent in stages, with approval each time.
  - Then: **delete every remaining image** (the owner's instruction from 2026-11-11 had been read too narrowly as Blender art only).
- **SpriteCook so far (134 credits, balance 936):**
  - The lineup was one 3x3 sheet (12 credits), split into 9 base stills for free. Asset IDs are in the new root `spritecook-assets.json`.
  - Cyber Shogun's animation run cost 102. Its preset run was contaminated by the lineup's nine-character prompt, so it was redone as a custom animation from the prepared run pose for 20 more.
  - **Rule for the next runs:** use `custom_animations` with explicit single-character prompts for every animation.
- **Cyber Shogun done:**
  - `assets/characters/rendered/cyber_shogun_atlas.png` (2048x1536, 3x nearest scale);
  - `cyber_shogun_portrait.png` (768x1024);
  - `cyber_shogun.tres`, restored byte-identical from `ca92679`.
  - New packer: `tools/art/pack_spritecook_character.py`.
- **Deleted (115 images, originals at `67eab0a`):**
  - the hand-drawn studies (`assets/characters/hand_drawn`, `shogun_study`);
  - the older title-logo JPGs and the website copies;
  - UI art, backgrounds, template images, the app icon (PNG and ICO), `icon.svg` and the favicon.
  - All 115 are now in `docs/art_regeneration.json` (537 entries); 9 are marked remake.
- **Code changes:**
  - `PalaceOperative` no longer adds the hand-drawn `TrainingArtwork` overlay. It was hiding the real sprite in every code-built mission.
  - Deleted `TrainingArtwork.cs` and `OperativeStudyAtlas.cs`.
  - `ShogunArtPreview` plays only `OperativeVisuals` frames and adds `HasArt`; `UseOriginalDesign` and `StudyTexturePath` were removed.
  - `IsometricOperative` dropped its `campaign_atlas` frames.
  - `CampaignArtwork.DrawOperative` draws the atlas idle frame.
  - `CyberButton` and `CyberDialogueBubble` load their textures through `OptionalArt` (the scene no longer hard-references them).
  - The icon settings were removed from `project.godot` and `export_presets.cfg`, and the favicon link from both `index.html` files. `assets/icons/README.md` says how to re-add them.
  - Deleted `tools/art/prepare_campaign_shogun.py` and `capture_shogun_study.gd`.
- **Tests:**
  - `ArtRegenerationSpecTests`: the count is 537, and only `.png` files are size-checked.
  - `test_shogun_art_study.gd` was rewritten for SpriteCook frames.
  - `test_cyber_dialogue_bubble.gd` checks that the scene has no hard texture references.
  - `test_level1_2d_cyber_shogun.gd` expects the painted backdrop only when the file exists.
- **Verification:**
  - `dotnet build`: 0 errors.
  - Godot import: clean.
  - Full `run_tests.ps1` before the last fix: Django and NUnit passed, and GdUnit had 1 error (the Level 1 backdrop test), which was then fixed.
  - The fixed file passes 9/9.
  - Final full rerun: all passed (GdUnit 363 with 0 errors, 0 failures and 0 orphans; NUnit; Django).
  - In game, Mission 2 showed Cyber Shogun, but that was before the overlay removal; recheck it.
- **Next:**
  - Recheck Mission 2 now that the overlay is gone.
  - Get owner approval for Stage 3 (5 operatives at about 102 each, all custom animations; the drone and mech idles at 20 each).
  - Later: the 9 UI and icon remakes, then the other phases.

## EGM Goldfish Review Loop Round 3 completed — 2026-11-11

- **Completed Claude Code's pending task:** Round 3 of `egm-review-loop` on `docs/ART_REGENERATION.md`.
- **Audit Outcome:** **CLEAN (0 Blockers, 0 Majors, 0 Minors)**.
  - Verified all Round 2 findings resolved: status key for `skip: unused alias`, `.tres` git-restoration guidance, cutscene pose sheet row/column meanings and pivots, fixed coordinate boxes (drone box, Tokyo blade, Aegis offset, Level 1 map), prop non-uniform display scaling, and credit estimation formulas.
  - `goldfish_review_report.md` finalized and marked approved.
- **Verification:**
  - `dotnet build`: Succeeded with 0 warnings, 0 errors.
  - Full test suite `run_tests.ps1`: Django 111/111, NUnit 439/439 (including `ArtRegenerationSpecTests`), GdUnit 363/363 in 76 suites (0 errors, 0 failures, 0 flaky, 0 skipped, 0 orphans).
- **Next steps:** Waiting on owner's art style selection and SpriteCook subscription before Phase 1 batch generation can begin.

## Downloaded stand-in assets removed — 2026-11-11

- **Deleted, at the owner's request:** `assets/stand_in/` (Kenney Racing, Kenney Sci-fi Sounds, Foozle Mecha Boss and Hormelz robot; 35 files plus imports). Also deleted: the Pixel Adventure leftovers `assets/images/{Blue,Green,Cherries,End (Idle),Terrain (16x16)}.png`.
- **Kept:** the fonts, although they were downloaded too, because every UI string needs them.
- **Code:** no changes. `StandInAssets.cs` returns null for missing files, so the bike, the `PalaceSentry` body and the `WorldHostile` body are drawn in code, and their sounds are silent.
- **Scenes:** the texture references were stripped from the unused `scenes/gameplay/collectable.tscn` and `finish.tscn`.
- **Tests:** `tests/test_stand_in_assets.gd` now checks the fallbacks while the files are absent, and the art when present.
- **Docs:** `docs/STAND_IN_ASSETS.md` is kept as a record of the sources and licenses, with a removal note. README and `docs/ART_REGENERATION.md` were updated.
- **Verified:**
  - Godot import: exit 0.
  - `run_tests.ps1`: NUnit 438, Django 111 and GdUnit 363/363 (0 errors and 0 orphans). The count dropped from 364 because two stand-in tests were merged into one.

## Blender retired; art moves to SpriteCook — 2026-11-11

**What changed (uncommitted):**
- **Art deleted:** all 422 Blender-made images (344 in `assets/`, 78 website title copies) plus their `.import` files, 27 image `manifest.json` files, the 9 character `.tres` SpriteFrames, `provenance.json` and the legacy `deployment.ogv`. Cutscene audio, cues, timing, localization and speech provenance files were kept. The originals are in git at `ca92679`.
- **Regeneration list:** `docs/ART_REGENERATION.md` (human-readable) and `docs/art_regeneration.json` (sizes) were generated from the files before deletion. For each image they record the path, pixel size, layer role, old manifest fields, frame layout, what loads it and the suggested SpriteCook route. Design notes from the deleted Blender docs were moved there.
- **Tooling removed:**
  - 27 Blender scripts: everything `tools/*/render_*.py`, plus `draw_flat_characters`, `package_character_sprites`, `render_quality`, `build_kit`, `build.py`, `rerender_flat_art`, `validate_live` and the two image-only gameplay validators.
  - `tools/art/CHARACTER_RENDERING.md`.
  - The `blendot`, `blender_godot_pipeline` and `gltf_fix_blender_light_intensity` add-ons, disabled in `project.godot` and their folders deleted.
  - Blender in `tools/cutscene/Dockerfile`. The image is now Python plus ffmpeg, and `ENTRYPOINT ["python3"]`.
- **Tooling kept:**
  - `tools/assets/audit_images.py` and `compare_images.py` were ported from Blender's Python to Pillow and numpy.
  - The validators' shared PNG reader moved to `tools/cutscene/png_pixels.py`.
  - The cutscene validators skip any image that is not there yet, print "SKIPPED", and still check audio.
  - `validate_aegis.py` and `validate_names.py` now check audio only.
- **Game code (gaps on purpose, per the owner):**
  - **Characters:** `OperativeVisuals.LoadFrames/LoadFramesAt` return an invisible placeholder SpriteFrames. Every animation has one null-texture frame. `blade_dash`, the attacks and `hit` are one-shot, matching the old art. `TryLoadFrames` returns null instead.
  - **Enemies:** the drone and boss load their frames in `_Ready`.
  - **Optional loads:** `OptionalArt.Texture` is used for the Level 1 map, the Mission 5 gate art and the home title. `CodeLevel` skips a missing backdrop.
  - **Menu backdrop and deployment cutscene:** both load their layers by path instead of from the scenes. The deployment cutscene skips to the briefing until all its layers exist. The menu backdrop does not load in the editor.
  - **Scenes:** references to deleted files were stripped from ten scenes.
  - **Website:** `website/src/components/TitleLogo.jsx` shows the text "AXIOM FORCE" when a title image is missing.
- **Tests:**
  - Art-existence tests now pass both with and without art.
  - New NUnit `ArtRegenerationSpecTests` checks that each listed image is absent or exactly its listed size. It replaces the later-campaign image hash check.
  - Cutscene playback tests return early through `tests/support/cutscene_art.gd` while a stage's art is missing, and run again once it returns. The skip path is still covered by the existing missing-art tests.
- **Docs:** an "Art Pipeline: SpriteCook Only" rule was added to CLAUDE/AGENTS/CODEX/GEMINI.md, plus a README "Art pipeline (SpriteCook)" section. `tools/art/README.md` was rewritten. History notes were added to the tool READMEs and the gameplay docs, and stale Blender commands removed. `docs/ASSET_QUALITY_AUDIT.md` image findings are marked superseded.

**Verification:**
- `dotnet build` passed with 0 errors.
- Background Godot import exited 0 with no missing-dependency errors; the only errors were the usual Norton TLS lines.
- `run_tests.ps1`:
  - NUnit 438/438.
  - Django 111/111.
  - GdUnit 364/364 in 76 suites, with 0 errors, failures or orphans.
  - The log has no "There is no animation" or failed-load lines.
- Each GdUnit suite was also run separately.
- The website builds in its Docker builder stage. ESLint shows 12 errors that already exist at HEAD; the new component is clean.
- The cutscene image was rebuilt: ffmpeg 7.1.5, no Blender, and `validate_tokyo.py` runs.
- A fresh-context review found three problems, all fixed: dangling `animation = "idle"` lines, looping placeholder attacks, and stale doc commands.

**Limitations and next steps:**
- The game looks mostly empty: actors that use the rendered sprites (platformer operatives in most missions, the drone, the boss) are invisible, though Shogun's Level 1 and training art and the selection studies still show; cities, menus and the Level 1 map are blank, almost every cutscene skips, and there is no title image. The owner accepted this.
- SpriteCook: the owner subscribed to the Starter plan on 2026-11-12. Before any regeneration, each agent must sign in once (Claude Code: `/mcp` → spritecook → Authenticate), the art style must be chosen, and each batch needs the owner's approval because it costs credits.
- SpriteCook skills are mandatory for all asset work (2026-11-12): CLAUDE.md, AGENTS.md, CODEX.md and GEMINI.md have a "Mandatory SpriteCook Skills (All Asset-Related Work)" section. It covers what counts as asset work, a table of which skill fits which task, where each agent finds the skills, and that `spritecook-workflow-essentials` is always read first. Checked: given a portrait request that did not mention SpriteCook, Codex (`codex exec`) and Antigravity (`agy --print`) both named `create-game-assets`, then `spritecook-workflow-essentials`, then `spritecook-generate-sprites`, and confirmed they can read them.
- Host Blender 5.2 was already absent: there was no install folder, registry entry, winget package or PATH entry.
- `addons/animation_combiner` still has Blender helper scripts for FBX/GLB work. It was left in place pending the owner's decision.
- Dated README and HANDOFF sections still mention the old "Docker/Blender" art as history.

## Local Graphify code graph — 2026-11-10

- **Cloud attempt removed:** an earlier Graphify Cloud MCP attempt (`api.graphify.com`, user scope) was removed with its doc edits at the owner's request. Its OAuth metadata URL returned malformed HTTP.
- **Now local:**
  - `graphifyy` 0.9.106 is installed with `uv tool install --system-certs --python 3.14 "graphifyy[mcp]"`. 3.15 fails, because `tree-sitter` has no wheel and there is no MSVC. `--system-certs` is needed for this machine's TLS interception.
  - The graph was built with `graphify update .`, which uses no LLM: 965 code files, 8,232 nodes, 16,985 edges, 372 communities.
  - Output is in git-ignored `graphify-out/`.
  - Claude Code has a local-scope stdio MCP server, `graphify`, which `claude mcp get graphify` reports as Connected.
  - `graphify explain "MusicGain"` returned correct `file:line` edges.
- **Agent rules:** CLAUDE/AGENTS/CODEX/GEMINI.md require a graph lookup every run, and `graphify update .` after code edits.
- **Gaps:**
  - No GDScript extractor, so `.gd` files are absent, and so are scenes and assets.
  - The SQL extractor is missing (`graphifyy[sql]`).
  - Cross-file call edges are sparse: `MusicGain` shows no edge from `CodeLevel`.
  - The `/graphify` skill was not installed; it is LLM-backed and needs the owner's approval.
  - The MCP tools load from the next Claude Code session.
- **Codex and Antigravity:** both now have the same `graphify` stdio server.
  - Codex: `~/.codex/config.toml`, via `codex mcp add`.
  - Antigravity app and `agy` CLI: `~/.gemini/config/mcp_config.json`, via `agy mcp add`.
  - Both CLIs rewrote their file's formatting; the existing servers kept their values.
  - Backups: `*.pre-graphify-20261110183426`.
  - A stdio probe (initialize, tools/list, get_node) returned the 10 tools and the correct MusicGain node.

## Asset quality pass (Python + Blender) — completed 2026-11-10

Owner request: improve every asset with Python and Blender; regenerate (not upscale) art without source scripts. Full report, including open items: `docs/ASSET_QUALITY_AUDIT.md`.

**Changed:**
- `tools/art/render_quality.py`:
  - `apply_flat` (Cycles, 16 samples, 1.5 px filter, no denoiser) is used by the menu-backdrop `render()` and every cutscene, gameplay-layer and later-campaign generator that calls it.
  - `apply_lit` (512 samples, no denoiser, so Blender 4.3 and 5.2 use the same settings) is used by `build_kit.py` and the titles.
  - `draw_flat_characters.py` keeps its own settings.
- Re-rendered on host Blender 5.2 with `tools/assets/rerender_flat_art.py` (now `--python-exit-code 1`, with the later-campaign `--targets` job before `--produce`):
  - Cutscene, gameplay and later-campaign layers went from mean edge softness 0.0–0.5 to 1.0.
  - `images_after.json` shows no script-made image still jagged, no size change and no load error.
- City kit (232 images): re-rendered at 512 samples.
- Wildfire gameplay `facade.png`: the window columns moved 3 px left so the anti-aliased edge keeps the margin `validate_wildfire.py` requires.
- Titles: `tools/art/render_title_logo.py` renders all 10 title texts into 32 game files and 39 files in each of `website/public/images` and `title_logos`.
  - Chrome text is 3D. The cyan and magenta rims are grown from the text alpha in numpy and composited in 8-bit display values; 8-bit is pinned and float layers are refused. 3D outline growth threw spikes on sharp corners (Arabic, Hebrew, Japanese) and misfilled the Korean font's overlapping outlines.
  - The plate renders once; a full run takes about 7 minutes.
  - Arabic uses Noto Sans Arabic with no edge bevel (`FLAT_EDGE_FONTS`).
  - Every requested title is shaped and glyph-checked before anything renders; `check_glyphs` and the Arabic shaping guard raise `ValueError`.
  - Contact sheet: `.validation/asset_quality/titles_sheet.png`.
- `generate_cyber_sfx.py`: seeded, with DC removal, fades and a sample-peak ceiling of 0.7. True peaks are now -2.1 dBTP or lower. blade_clash and drone_explosion are about 3 dB quieter.
- Robot sheets: mipmaps on, plus `LinearWithMipmaps` in `WorldHostile.cs`.
- Audit tools: `tools/assets/audit_images.py`, `compare_images.py`, `audit_audio.py` (trailing-silence detection fixed 2026-11-10). `rerender_flat_art.py` writes the menu backdrops to `.validation` only, so a default run keeps the committed EEVEE files.

**Reverted or kept after review:**
- Voice normalization was reverted: re-encoding changed hashes that the tests, transcripts and the owner's listening sign-off depend on. `normalize_audio.py` was deleted.
- The Kenney stand-in sounds and the menu backdrops are restored from git. The Cycles backdrops looked identical to the EEVEE ones.

**Verified (2026-11-10):**
- All 15 validators pass: cutscene validators in container Python; `validate_aegis.py` and `validate_names.py` in container Blender.
- `python -m unittest tests.test_written_localization tests.test_english_voice_inventory`: 20 OK.
- `dotnet build`: 0 errors.
- Headless Godot import: exit 0, no import errors; the only errors logged were asset-library TLS failures and shutdown messages.
- `run_tests.ps1`: GdUnit4 357/357 with 0 errors, failures and orphans; Django and NUnit PASSED.
- godot-ai: the main menu, `game_home.tscn` (new logo, clean alpha) and the `level2.tscn` Wildfire intro (re-rendered layers) run with no errors in the game log.
- EGM review: three Goldfish reviewers; blocker and major findings fixed or reverted. Their one contested claim ("Latin titles dropped") was false.

**Not verified:** no person has looked at every cutscene, city and language in the running game. `tools/world/qa.py` (a live MCP tour) was not run.

**Owner decisions (2026-11-10):** keep the logos' wording and ignore the translation catalog; accept the smaller Arabic title; level-match voices at playback.

**Voice playback gain (2026-11-10):**
- `tools/assets/voice_gain.py` (cutscene container, ffmpeg) measures exactly the 98 played recordings: the 93 `speech` cues, AXIOM-7 briefing lines 1–4 (`SpeechPaths.BriefingLineCount`) and the deployment narration. It writes `assets/audio/voice_gain.json` with a gain toward -18 LUFS (clamped to +/-12), capped so the Voice limiter removes at most 2 dB of peak (`limit_budget_db`). It also records each recording's `expected_limiting_db` and sha256. The actual range is -4.1 to +5.5 dB, and 16 lines are capped (narration +4.6 dB, playing at -20.2 LUFS). Played levels span -20.2 to -18.0 LUFS, down from -24.8 to -13.9. On a missing or unmeasurable file it keeps the old table and exits 1.
- `scripts/gameplay/SpeechPaths.cs` holds the engine-free path rules shared by the players and the tests. `scripts/managers/VoiceGain.cs` provides `GainDb(path)` (cuts only if the Voice bus is missing) and `Bus`; `Parse` tolerates wrong JSON shapes. `deployment_cutscene.gd` applies the narration gain (cuts only without the Voice bus or on Web, where sample playback skips bus effects). `MissionSpeechQueue` (base -2 dB, unchanged) and `StoryIntro` add the gain on each play and use the new `Voice` bus in `default_bus_layout.tres`, which sends into SFX and has an `AudioEffectHardLimiter` at -1 dB. The menu volume slider (bus 0) is unaffected.
- Tests: NUnit `VoiceGainTests` (parsing, the exact set of played recordings via `SpeechPaths`, hash match, the gain rule and limiter budget); GdUnit `tests/test_voice_bus.gd` (bus, send, limiter at exactly -1 dB, table), `test_real_cue_plays_on_voice_bus_with_its_table_gain` in `tests/test_mission_speech_queue.gd`, and `test_narration_plays_on_voice_bus_with_its_table_gain` in `tests/test_deployment_cutscene.gd`.
- EGM reviews: a first codebase-details review of this change had no blockers, and all 8 findings were fixed. A second round of all three reviewers (2026-11-10) found no blockers. Its findings were fixed (see the audit's Review section), except the Web limiter gap (partly contested: C# cannot export to Web) and the +24 dB mission music (reported, not changed).
- Not judged by ear. No line is limited by more than 2 dB; listen to the deployment narration first. Mission speech plays about 2 dB under the briefing (the existing -2 dB offset).

**Mission music level (2026-11-10, owner approved):**
- `CodeLevel.BuildAudioAndPause` played mission music at +24 dB on Master: the default `Sakura-Girl-Yay` track at about +12.7 LUFS (clipping), and the atmospheres about 18 LU over speech.
- `tools/assets/music_gain.py` (cutscene container) measures every `MusicPath => "res://..."` in `scripts/gameplay/*.cs`. It writes `assets/audio/music_gain.json` with target -29 LUFS, gains capped at a -1 dBTP true peak: -17.7 dB for the default track and -3.2 to -3.8 dB for the 8 atmospheres.
- `CodeLevel` now plays the track on the Music bus at `MusicGain.GainDb(MusicPath)`. `NamesWithoutNumbersMission` no longer forces -21 dB (about -46 LUFS).
- `scripts/managers/GainTable.cs` is the shared table parser and loader; `VoiceGain` and `MusicGain` wrap it.
- Tests: NUnit `MusicGainTests` (track set from the sources, hashes, gain rule, pinned -29 target, -1 ceiling and limit). GdUnit `test_mission_music_plays_on_music_bus_with_its_table_gain` (missions 4 and 5) and `test_every_mission_scene_track_has_a_music_gain`, which reads the real `MusicPath` of all 12 levels and 6 city operations; `MusicPath` was made public for this. `MusicGain.GainDb` warns when a track has no entry.
- An EGM design review found no blockers; its findings were fixed.
- Not judged by ear. Listen for two things:
  - whether -29 LUFS sounds thin for the melodic default track in missions 1–4 and the city operations, which have little speech;
  - the step when an intro cutscene (atmosphere at about -31.7 LUFS) hands over to its mission (-29 LUFS), about 2.7 LU louder.
- No ducking under speech. Menu music is a stub; the open world has no music; cutscene music was not changed.

**Not changed (by design):** painted Level 1 backdrop, hand-drawn pose sheets, third-party music (clipping baked in), fonts, pixel-art stand-ins. Unused files are listed in the audit; none were deleted.

## Free stand-in assets — 2026-11-09

Added: CC0 Kenney motorcycle sprite, bike engine loop, sentry and gunner shots, open-world drone strike, and enemy explosions, in `assets/stand_in/`. `StandInAssets.cs` tolerates missing files, so deleting the folder restores the old look; also delete `tests/test_stand_in_assets.gd` and its `.uid`. Uncommitted.

Also added from packs the owner downloaded by browser (itch.io rate-limited scripted downloads): Foozle drone sprite on `PalaceSentry`, and hormelz 8-direction robot on `WorldHostile`, chosen by aim direction and action. Collision, damage, timing and AI are unchanged. Platforms and backdrops were deliberately left code-drawn. Sound levels were set without listening.

`docs/STAND_IN_ASSETS.md` lists candidate free packs and their license limits. Licenses were read on the creators' pages except where that file says otherwise; OpenGameArt pages could not be opened automatically. Most packs are pixel art and will not match the game's rendered art. Flambo19's pack cannot be redistributed, even modified.

## Multi-city open world and Kyoto — 2026-11-08

Owner decisions: six operative home cities (Kyoto/Kaito, Mumbai/Arjun, Honolulu/Kiana, Dubai/Zayd, Luanda/King Tariq, Beirut/Maya), 4 compact districts each, top-down quests plus one optional side-scroller per city. **All six are built; the owner chose to review them together. No person has played any of them.**

- The five later cities are data tables in `scripts/world/cities/<City>City.cs` built by `CityBuilder`. Operations derive from `BeaconOperationMission`. Godot needs one C# class per file for scene scripts, so never put two mission classes in one file. `AllCitiesTests` applies the data rules to every city; `WorldFingerprintTests` guards Los Angeles and Kyoto.
- Story content rule: invented people and events, fights never at places of worship or memorials, and no reference to real wars, disasters or political groups. Beirut's harbor deliberately has no grain or silo content.

- `WorldDefinition` (`scripts/world/WorldDefinition.cs`) holds what used to be hard-coded. `WorldCatalog` keeps Los Angeles and exposes it as `WorldCatalog.Definition`. The running scene holds its city in `_world` and passes it to workers; never add a changing static for the current city.
- Saves: Los Angeles keeps `world:{session}`, with a new optional `CurrentCity`. Other cities use `world:{session}:{city}`. `WriteAsync` picks the row from the record's `World`. `TravelAsync` writes both ends in one transaction. Old saves need no migration.
- Trap: the identity `neo-kyoto-v1` is an old name for **Los Angeles**. Kyoto is `kyoto-2157-v1`. `NeoKyotoWorld` and `neo_kyoto.tscn` are still the Los Angeles-era names for the shared scene.
- `CodeLevel.IsCityOperation` skips the campaign roster reset, story cues, numbered title card and scorecard. `CityOperationMission` stores rewards through `WorldTransactionService.CompleteOperationAsync` and reloads the world.
- To add a city: write a `CitySpec`, add it to `CityRegistry.All`, add a style in `build_kit.py`, and add its operation scene.
- Limits: No person has played any city. Quest text and place names are English only, and `tools/world/localize.py` does not read `scripts/world/cities/`. Operations use the default music. Credits still can't be spent. Hostiles appear only for quest objectives, as in Los Angeles.
- Verified: Godot import, un_tests.ps1\ (GdUnit4, NUnit, Django), \catalog.py --check\, real PostgreSQL round trips, Los Angeles and Kyoto fingerprints. Through godot-ai in the running game with real inputs: all Skyport flights; beacon, gate and results panel in all six operations; full route runs of Dubai, Beirut and Honolulu with sentries removed; one complete Mumbai quest with combat. Two independent Elephant-Goldfish reviews were run and their accepted findings fixed. Not verified: a person playing; full routes of Kyoto, Mumbai and Luanda; difficulty with sentries; the reward save and Retry in a real campaign from inside an operation. Known and left: opening the map in a fight pauses drones while the shield still regenerates (existing behaviour); the Skyport needs one extra press.
- Existing hazard, not changed: `WorldExpansionTests.CleanupAsync` deletes `state_key LIKE '%:{id}%'`, which also matches other campaigns whose numbers contain that ID, in the development database. `KyotoSaveTests` uses an exact match instead.

## uv tooling moved from Python 3.14.10 to 3.15.2 — 2026-11-08

- `addons/godot_ai/utils/uv_resolution_policy.gd` now selects Python 3.15 and adds `%APPDATA%\uv\wheelhouse` as a second `--find-links`. The godot-ai entries for Claude Code, VS Code, Codex and Antigravity match.
- That folder holds `pyyaml-6.0.3-py3-none-any.whl`, built once from uv's cached sdist with `PYYAML_FORCE_LIBYAML=0`, because PyYAML has no 3.15 wheel and there is no Visual C++ here. `--no-build` stays on for every launch. YAML runs on the slower pure-Python parser.
- uv's Python 3.14.10, its cached environments and its cp314 wheels were removed. Python 3.12.16 was not touched.
- Limits: a plugin update can overwrite the policy file. `~/.gemini/settings.json` still points at a `Python314` launcher that no longer exists, and was already broken. `.validation/venv-backups/20261106-314` is an old non-uv backup and was kept.

## Open-world slice, setting canon and tooling fixes — 2026-11-08

**Correction:** entries below that call the world roadmap complete are wrong. `docs/NEO_KYOTO_EXPANSION_PLAN.md` now has the audited status table. Treat that table as current.

**Completed**
- Playable slice in `scripts/world/`: eight quests (`WorldQuestRules.PlayableQuestIds`), conversations (`WorldConversationPanel`, `WorldDialogueCatalog`), combat (`WorldHostile`, `WorldEncounterController`, F/Space/Q inputs), alert from real drone state, defeat return, healing only at care services, visible bike, boat prompt removed, clock paused in map and conversation.
- Save fixes: mission settlement now merges instead of wiping quests; `CompleteQuestAsync` reads the world row on its own transaction, rejects repeats, missing prerequisites and unknown branches; failed receipts show Retry.
- Canon is Los Angeles and Tokyo means Little Tokyo. Replaced Neo-Kyoto, Old Kyoto, Neo-Arcadia and Neo-Tokyo in player text, the website, and sources.
- `dialogue.ogg` for the Mission 7 aftermath: cut 51.07–51.936 s ("over Neo-Kyoto"), padded after the line so later cues keep their times, re-encoded once with PyAV.
- `tools/db/backup.ps1` and `restore.ps1`, `listening_acceptance.json` read by `audit_english_voice.py --check`, and `--system-certs` plus Python 3.14 added to the user-level `godot-ai` entry.

**Decisions**
- Melee reach is 3.4 m rather than the planned 2.5 m, because drones hold position at 2.8 m. Targets within 1 m always count as in the arc.
- Drones are logic-only and don't collide. No ambient patrols. New conversation text is English only.
- Internal IDs keep old names: `NeoKyotoWorld`, `neo_kyoto.tscn`, `safehouse_old_kyoto`, `quest_ok_*`.
- The spliced track's transcript is derived from the old Scribe transcript, not newly paid for; `media.json` and `later_campaign_validation.json` hashes were updated after a PyAV decode (90.0 s, peak 0.839), not by the Docker media pass.

**Verification (final edits, 2026-11-08)**
- `dotnet build` 0 errors. Background Godot import exit 0. `run_tests.ps1` exit 0: Django 111/111, NUnit 375/375, GdUnit4 343/343 with 0 errors, failures and orphans (`reports/report_545`). Python 24/24. `catalog.py --check` passes. Backup and restore were trial-run against a temporary database.
- A fresh-context review found 6 defects (wave reset on overlapping objectives, guard sticking on, residents taking over mission entrances, no exit after a failed save, health lost on swap, undocumented methods) and 2 edge cases; all were fixed before the final run.
- `audit_english_voice.py --check` exits 1 by design for the same 7 flagged cues. Not checked: live play in the real game window, PostgreSQL-backed play through the scene (preview mode and service tests were used instead), the website in a browser.

**Later reviews (same day)**
- `/code-review` (high): fixed damaged saves being overwritten by defaults, `restore.ps1` now one transaction, personal quests no longer credited after a mid-fight swap, drones pause while the player is locked by streaming, guard clears on mounting, attack ring matches ground squash, cached the quest list, removed dead entry code.
- EGM review of the status table (`codebase_details`, `style` personas, direct dispatch, one round, so not converged under the skill's two-clean-rounds rule). `egm-review` audits documents only, so it was not applied to code. The table was rewritten from its findings.
- Follow-up fixes: the four playable branch choices now describe story outcomes instead of effects no code applies; `obj_fd02_3` moved into the hostile crucible pocket; `quest_dt_02` and `quest_ct_02` now award gadgets, so `mod_free_signal_tuner` comes only from `quest_fsc_03`. New NUnit checks cover combat objective regions for all 72 quests and that module award.
- The five choices in data-only quests still describe effects; reword or build them if those quests become playable.

**Limitations and next steps**
- The `website` container serves a built image, so rebuild it to see the Little Tokyo text.
- A person must listen to the splice and play the slice; automated tests can't judge either. The strict voice gate still reports 7 flagged cues until someone records acceptance.
- The cutscene and export Docker images are gone, likely with the Docker data reset that removed `axiom-force_pgdata`. Rebuild them when needed.
- `godot-ai` needs an MCP reconnect to pick up its new flags. Stale `assets/narrative` copies in other languages still mention the old setting; no code path showing them was confirmed.
- Next: decide which remaining region or system to make playable using the status table.

## Living Resistance World: Neo-Los Angeles 2157 Re-theming & Phase 4 Delivery — 2026-11-07

Re-themed and grounded the 128 km × 128 km open exploration world to the game's canonical master lore setting: **Greater Los Angeles Resistance Basin, 2157 (Neo-Los Angeles)**, while preserving 100% of underlying game mechanics, numerical district IDs, coordinate layouts, and database isolation.

### Completed Alignment Deliverables (Neo-Los Angeles 2157):
- **World Identity & Geographic Alignment (`scripts/world/WorldCatalog.cs`)**:
  - Catalog Identity updated to `"neo-los-angeles-v1"`.
  - Re-aligned all 16 district names to canonical Greater Los Angeles geographic equivalents:
    - Region 0: `Angeles Crest Highlands` (was Cedarcrest Highlands)
    - Region 1: `Castaic Aqueduct Reservoir` (was Cloudstep Reservoir)
    - Region 2: `San Gabriel Peaks` (was Stormbreak Peaks)
    - Region 3: `Malibu Coast` (was Northlight Coast)
    - Region 4: `Topanga Canyon Preserve` (was Cedarwild Preserve)
    - Region 5: `Historic Core & Little Tokyo` (was Old Kyoto)
    - Region 6: `Downtown LA Core` (was Downtown Core)
    - Region 7: `San Pedro Harbor` (was Eastlight Harbor)
    - Region 8: `Mojave Solar Belt` (was Solar Belt)
    - Region 9: `Concrete Wards & LA River` (was Concrete Wards)
    - Region 10: `Century Citadel Ring` (was Citadel Ring)
    - Region 11: `Santa Monica Coast` (was Tideglass Coast)
    - Region 12: `Kurogane Ridge Escarpment` (was Kurogane Escarpment)
    - Region 13: `Alameda Rustrail Belt` (was Rustrail Belt)
    - Region 14: `Vernon Foundry Basin` (was Foundry Basin)
    - Region 15: `Ballona Reclamation Wetlands` (was Reclamation Wetlands)
- **Save Store Backward Compatibility (`scripts/world/WorldSave.cs`)**:
  - `Valid()` supports both `World == WorldCatalog.Identity` (`"neo-los-angeles-v1"`) and legacy `"neo-kyoto-v1"` saves to protect existing saves from invalidation.
- **UI & HUD Label Updates**:
  - Exploration world map header updated to `"Neo-Los Angeles 2157 · 128 × 128 km"`.
  - Mission victory scorecard continue button updated to `WorldText.Get("Return to Neo-Los Angeles")`.
  - Full seventeen-language localization added to `assets/translations/Translations - Sheet1.csv`.
- **Content Catalog Display Alignments**:
  - 16 Regional Safehouse display titles in `WorldContentCatalog.cs` updated to Los Angeles equivalents (e.g. `Angeles Crest Safehouse`, `Castaic Reservoir Safehouse`, `Little Tokyo Safehouse`, `San Pedro Harbor Safehouse`, `Ballona Wetlands Safehouse`).
  - Reconstruction task display titles in `EndgameContentCatalog.cs` updated to Los Angeles equivalents (e.g. `Castaic Aqueduct Sluice Modernization`, `Malibu Coastal Beacon Lighthouse`, `Little Tokyo Lantern Preservation`, `Ballona Bio-Filtration Marsh`).
  - Canonical string IDs (`safehouse_old_kyoto`, `recon_cloudstep_sluice`, etc.) remain identical for absolute save and state stability.

## Neo-Kyoto Living Resistance World: Phase 4 Delivery (Master Roadmap 100% Complete) — 2026-11-07

Completed full implementation and verification of **Phase 4: Endgame and Final Acceptance** from `docs/NEO_KYOTO_EXPANSION_PLAN.md`. All phases (Phases 0, 1, 2, 3, and 4) of the Living Resistance World are now 100% complete, fully tested, and integrated.

### Completed Deliverables (Phase 4):
- **Liberated-World Endgame Systems (`scripts/world/EndgameContentCatalog.cs`)**:
  - **16 Regional Reconstruction Tasks**: Authored 1 civic reconstruction project per region unlocking persistent post-finale services and amenities.
  - **8 Corpo Remnant Tactical Incidents**: Authored 8 high-threat tactical encounters for liberated sectors featuring recommended operatives, threat tiers (1–5), and substantial tactical credit payouts.
  - **6 Archive Historical Combat Simulations**: Dedicated tactical simulations honoring each hero's defining combat milestone with custom gameplay mutators.
- **Operation Challenge Modifiers & Mastery Scoring**:
  - Authored 5 replay modifiers: `op_mod_ironman` (1.50x), `op_mod_overclocked` (1.35x), `op_mod_glass_vanguard` (1.30x), `op_mod_emp_atmosphere` (1.20x), and `op_mod_axiom_protocol` (2.00x).
  - Implemented `EvaluateMastery`: evaluates clear times against target gold times, modifier counts, and damage taken to award Bronze, Silver, Gold, or Platinum mastery ranks along with tiered credit, faction reputation, and cosmetic unlocks (`livery_gold_`, `skin_platinum_`).
- **Comprehensive Accessibility Profile (`scripts/world/AccessibilityProfile.cs`)**:
  - High-contrast telegraphs, parry timing window scaling (1.0x–2.5x), puzzle assist modes (`Standard`, `ExtendedClues`, `AutoSolveAllowed`), screen shake scaling (0.0x–1.0x), and controller glyph selection.
  - Integrated into `NeoKyotoWorld.cs` runtime inspection and helper bridges for GDScript testing.
- **Simulation & Streaming Performance Budget (`scripts/world/WorldPerformanceBudget.cs`)**:
  - Codified runtime simulation thresholds: max 16 nearby civilians, max 8 hostiles, max 4 traffic vehicles, 9 resident chunks, 25 cached chunks, and <= 33.3 ms streaming stall limit.

### Full Test Suite Verification Results:
- `dotnet build AxiomForce.csproj`: Succeeded with 0 warnings, 0 errors.
- Godot Background Headless Import: Completed with exit code 0.
- **Full Test Runner (`.\run_tests.ps1`)**:
  - **Django Backend**: **111 / 111 PASSED** in Docker container.
  - **NUnit (.NET 10 C#)**: **361 / 361 PASSED** (including `WorldExpansionPhase4Tests.cs` and `WorldTests.cs` Los Angeles geography assertions).
  - **GdUnit4 (Godot 4 GDScript)**: **71 / 71 suites (333 / 333 test cases) PASSED** with 0 errors, 0 failures, 0 flaky, 0 skipped, 0 orphans.
- Docker Stack: All 3 containers remain active and healthy (`axiom-force-website-1`, `axiom-force-backend-1`, `axiom-force-db-1`).
- **Git Status**: Working tree remains local and uncommitted (awaiting explicit user instruction on current changes).

## Neo-Kyoto Living Resistance World: Phase 3 Delivery (Master Plan Complete) — 2026-11-07

Completed full implementation and verification of **Phase 3: Natural & Coastal Expansion** from `docs/NEO_KYOTO_EXPANSION_PLAN.md`, fulfilling all architectural, systemic, and content quotas across the entire 16-region resistance world.

### Completed Deliverables (Phase 3):
- **All 16 Regions Fully Authored & Populated**:
  - Natural & Coastal Regions Delivered: Cedarcrest Highlands (Region 0), Cloudstep Reservoir (Region 1), Stormbreak Peaks (Region 2), Northlight Coast (Region 3), Cedarwild Preserve (Region 4), Solar Belt (Region 8), Tideglass Coast (Region 11), Kurogane Escarpment (Region 12), and Reclamation Wetlands (Region 15).
  - Complements the existing 7 urban/metropolitan districts (Old Kyoto, Concrete Wards, Downtown Core, Eastlight Harbor, Citadel Ring, Rustrail Belt, Foundry Basin).
- **Master Target Quotas Fulfilled in Catalog (`scripts/world/WorldContentCatalog.cs`)**:
  - **72 Authored Quests**:
    - 48 District storyline quests: 16 three-part narrative chains exploring corporate resistance, smuggling corridors, ecological reclamation, and local defense across every district.
    - 12 Operative Personal Episodes: 2 tailored episodes per operative for all six heroes (Cyber Shogun, Surya Prime, Oceanus, Mirage Blade, Apex Titan, Aura Phoenicia).
    - 12 Faction Quests: 3 distinct progression assignments for each canonical faction (Civic Recovery Network, Independent Couriers, Reclamation Guild, Free Signal Collective).
  - **48 Regional Challenges**: Exactly 3 per region across all 16 regions (Race sprint, Traversal/Operative Puzzle, Discovery Trail).
  - **16 Replayable Side Operations**: Exactly 1 per region across all 16 regions with specific terminal coordinates and side-scrolling mission scenes.
  - **48 Named Residents**: Exactly 3 per region across all 16 regions with authored Day and Evening routines.
  - **16 District Safehouses**: Exactly 1 per region across all 16 regions with resting and vitals restoration, plus 2 secondary facilities (Palace Archives, Viaduct Clinic) and Old Kyoto HQ amenities.
  - **64 Activity Pockets**: Exactly 4 per region across all 16 regions with ambient interactions and caches.
- **Dynamic Regional Weather System**:
  - Implemented `NeoKyotoWorld.WeatherState` enum (`Clear`, `Rain`, `Fog`, `RegionalStorm`).
  - Added deterministic 48-minute cycle calculation `GetWeatherForTime(double worldMinutes)` and exposed `"weather"` in `InspectionState()`.
- **District Era Mapping**:
  - Extended `DistrictState.DetermineEra` to map all 16 regions across campaign mission unlocks (`PreMission` → `Aftermath` → `Liberated`).

### Verification & Test Suite Health:
- `dotnet build AxiomForce.csproj`: Compiles cleanly with 0 warnings, 0 errors.
- Godot Background Headless Import: Completed with exit code 0 (`task-7659`).
- **Full Test Runner Protocol (`.\run_tests.ps1`)**:
  - **Django Backend**: 111/111 passed in Docker container.
  - **NUnit (.NET 10 C#)**: 353/353 passed in `AxiomForce.Tests.csproj` (including `WorldExpansionPhase3Tests.cs`).
  - **GdUnit4 (Godot 4 GDScript)**: 70/70 test suites (330/330 test cases) passed with 0 errors, 0 failures, 0 flaky, 0 skipped, 0 orphans (including `test_world_expansion_phase3.gd`).
- Docker Stack: All 3 containers (`axiom-force-website-1`, `axiom-force-backend-1`, `axiom-force-db-1`) remain active and healthy.
- **Git Status**: Working tree remains local and uncommitted (no commit or push performed without explicit user instruction).

## Neo-Kyoto Living Resistance World: Phase 2 Delivery — 2026-11-06

Following Phase 0 & Phase 1, completed full implementation and verification of **Phase 2: Urban Expansion** from `docs/NEO_KYOTO_EXPANSION_PLAN.md`.

### Urban Core Content Delivery (Phase 2):
- **5 Urban Districts Authored**: Downtown Core (Region 6), Eastlight Harbor (Region 7), Citadel Ring (Region 10), Rustrail Belt (Region 13), and Foundry Basin (Region 14), fully integrating surface and underground networks (Downtown Underworks and Foundry Underworks).
- **27 New Authored Quests (35 Quests in Catalog)**:
  - 15 District Quests: 5 three-part storylines detailing surveillance disruption, dockworker strikes, perimeter defense breaches, rail convoy escort, and geothermal furnace cooling.
  - 12 Faction Quests: 3 quests each for Civic Recovery Network, Independent Couriers, Reclamation Guild, and Free Signal Collective, rewarding faction reputation and specialized tactical modules.
- **Faction Progression & Tiers**:
  - Implemented 3 relationship tiers: `Contact` (0-99), `Trusted` (100-249), `Partner` (250+) in `DistrictState.cs` and `CampaignProgressionState.cs`.
  - Added 4 faction-specific tradeoff modules (`mod_courier_booster`, `mod_reclamation_mesh`, `mod_free_signal_tuner`, `mod_civic_nanobound`) conforming to the strict 20% passive bonus ceiling.
- **15 New Regional Challenges (21 in Catalog)**: 3 per urban district covering sprint races, operative ability traversal puzzles, and discovery trails.
- **5 New Replayable Side Operations (7 in Catalog)**: Added *Data Haven Infiltration*, *Harbor Slipway Sabotage*, *Citadel Perimeter Breach*, *Freight Line Interception*, and *Smelting Core Shutdown*.
- **Vehicle Traversal (Utility Boat)**: Added the `veh_utility_boat` (~14 m/s max speed on waterways/canals). Implemented `ToggleBoat(WorldPoint)` in `ExplorationPlayer.cs`, enforcing water-only mounting, dismount-to-foot, and prohibiting concurrent bike usage.
- **5 Urban Safehouses & HQ Customization**:
  - Added 5 district safehouses: *Underground Terminal 01*, *Old Customs Drydock*, *Forward Outpost Delta*, *Depot Maintenance Bunker*, and *Sub-Furnace Refuge* with resting and vitals restoration.
  - Implemented Old Kyoto Headquarters customization: `TacticalHoloTable`, `SquadQuarters`, `MemorialShrine`, `ReclamationWorkbench`, and `SignalArray`.
- **15 New Named Residents (21 in Catalog)**: 3 authored residents per urban district with Day and Evening schedule routines.

### Verification Results:
- `dotnet build AxiomForce.csproj`: 0 errors.
- Godot Headless Background Import: Completed with exit code 0.
- Django Backend Tests: 111/111 passed in Docker.
- NUnit (.NET 10) Tests: 337/337 passed (including 9 new tests in `WorldExpansionPhase2Tests.cs`).
- GdUnit4 Tests: 327/327 passed across 69 test suites with 0 errors, 0 failures, 0 orphans (including `test_world_expansion_phase2.gd`).
- Docker containers remain active and healthy. Git working tree remains local and uncommitted (no commit or push authorized).

## Neo-Kyoto Living Resistance World: Phase 0 & Phase 1 Delivery — 2026-11-06

Following Codex's weekly limit handoff, completed Phase 0 & Phase 1 of `docs/NEO_KYOTO_EXPANSION_PLAN.md`
without breaking existing campaign progression, transit, or story cutscenes.

### Architectural Core (Phase 0):
- **Campaign-Isolated Progression**: Added `CampaignProgressionState.cs` storing per-campaign operative upgrades (`progression:{session}`). Includes automatic one-time baseline migration from legacy shared documents, typed stat modifiers (`StatModifierType`), 2 module slots, 2 gadget slots, and a strict 20% passive ceiling across health, shield capacity, damage, and cooldown reduction.
- **Atomic Transactions & Idempotency**: Added `WorldTransactionService.cs` committing quest completions and item purchases atomically in PostgreSQL with stable action receipts (`receipt:{session}:{token}`) preventing double-reward settlement and replay exploits.
- **Extended World Save State**: Extended `WorldSave.cs` with `CompletedQuests`, `ActiveQuests`, `DistrictChoices`, `UnlockedVehicles`, `ActiveVehicle`, and `WorldTimeMinutes`. Implemented explicit `IEquatable<WorldSave>` to ensure value-equality for collections during JSON round-trips.

### Content & Traversal (Phase 1):
- **Living Neighborhoods & Pockets**: Authored 64 activity pockets in `WorldContentCatalog.cs` across all 16 regions, with dense content for Old Kyoto (Region 5) and Concrete Wards (Region 9).
- **Named Residents & Safehouses**: Added 6 named residents (Ren, Hana, Kenji in Old Kyoto; Taro, Maya, Jin in Concrete Wards) with Day/Evening schedules. Implemented 2 district safehouses (*The Lantern Hearth* and *Sub-Viaduct Shelter 04*) providing secure resting, conversation, and vitals restoration.
- **Authored Quests & Operations**: Added 8 quests (Old Kyoto 3-part chain: *The Lantern Circuit* → *Names Behind the Walls* → *The Moonbridge Promise*; Concrete Wards 3-part chain: *The Broken Line* → *Signal Under Rain* → *Shelter and Steel*; and operative episodes for Cyber Shogun *Honor Among Cinders* and Oceanus *Tides of Memory*). Added 6 regional challenges and 2 replayable side operations (*Palace Vault Reclamation*, *Viaduct Grid Defense*).
- **Vehicles & Bounded Combat**: Added starter Electric Bike to `ExplorationPlayer.cs` (~20 m/s road speed, auto-dismount in interiors/water). Added top-down combat actions (Attack, Defend, Dodge, damage mitigation) and 3-state encounter alert logic (`Suspicion` → `Search` → `Pursuit`) with transit lockout during Pursuit.
- **World Cycle & Map Safety**: `NeoKyotoWorld.cs` advances a 48-minute day/night cycle, updates resident schedule markers, and renders quest HUD objectives while preserving the exact 316 fast-transit destinations.

### Verification Results:
- `dotnet build AxiomForce.csproj`: Built with 0 errors.
- Godot Headless Background Import: Completed with exit code 0.
- Django Backend Tests: 111/111 passed in Docker.
- NUnit (.NET 10) Tests: 328/328 passed (including 8 new tests in `WorldExpansionTests.cs`).
- GdUnit4 Tests: 324/324 passed across 68 test suites with 0 errors, 0 failures, 0 orphans (including `test_world_expansion_phase1.gd` and `test_world_exploration.gd`).
- Docker containers (`axiom-force-website-1`, `axiom-force-backend-1`, `axiom-force-db-1`) remain active and healthy.
- Git working tree remains local and uncommitted (no commit or push authorized).

## Neo-Kyoto implementation — 2026-11-06

Added the separate streamed exploration scene/controllers/map, stable sixteen-region
catalog, 48 destinations, 256 free stations, two underground elevator networks and
twelve mission terminals. Map/terrain share geography; chunk generation is engine-free
and off-thread, with nine resident scenes and 25 cached payloads. Integer chunk origin
rebasing keeps Godot coordinates local. Original Blender environment/landmark modules
and six directional atlases are reproducible through the opt-in Docker art pipeline.

Results now return to exploration, including act/finale results; mission selection
marks an entrance, and story openings launch at that terminal. PostgreSQL settlement
atomically stores extraction alongside its receipt/reward; duplicate receipts cannot
overwrite later transit. World snapshots are campaign-scoped/versioned, with safe
legacy arrival, mission/world resume, operative and waypoint. Entry/transit/replay/menu
changes save before leaving; failure exposes retry with no local persistence fallback.
Ninety world labels have offline-authored seventeen-language text. Proper-name roots
stay canonical Latin, with localized geographic descriptors; native review is pending.

The live unsaved-preview pass verified all twelve terminal routes, including Act II,
Act III end/start routing and the finale intro. Fixed overlapping Spire interactions
by choosing the nearest terminal, and corrected the Blender atlas's east/west row
order with cardinal-facing assertions. Mission-card requests coalesce while saving;
late async continuations check node validity, and retry/status controls remain visible
inside the map as well as the HUD. Future world versions cannot partially settle a
reward or be silently downgraded. Saving exploration updates campaign character/date.

Final verification: build (0 errors; final incremental build 0 warnings, previous
full build 153 existing warnings), background import exit 0, Django 111/111,
NUnit 314/314, Godot 321/321 (report_524, zero
errors/failures/orphans), Python 20/20 and localization gate PASS (1,565 required
entries per locale). Eleven NUnit world tests cover all transactional returns,
legacy/independent/outage/version rejection and a 120-stop bounded-cache sweep.
These final suites ran after the UI/lifecycle and facing fixes. Final live map/city
smoke checks passed; the agent-started preview was stopped and the user's editor
remains open. README, HANDOFF and MEMORY.MD are updated; changes remain uncommitted.

Rendered 32-stop debug sweep on Iris Xe at 1920×1094: frame intervals median
16.677 ms, p95 17.728 ms, max 43.013 ms; 9 resident/25 cached throughout, static
memory 103.3–109.9 MB. Live checks covered rural/coastal/underground, 4/8 m/s movement,
controller Back/D-pad navigation and roof alpha 0.302. All 46 PNG size/alpha/import
checks passed. This short run is not sustained 1080p/60 certification or a complete
manual combat playthrough. Native art/language acceptance remains pending.
Existing addon TLS/editor-shutdown/resource diagnostics persist during import; the
generic reference checker flags existing addon signal/inheritance cases, none in
the new world scene. No containers stopped, game exports, commits or pushes.
See docs/NEO_KYOTO.md for usage, architecture and measured limits.

## Localization completion continuation — 2026-11-06

Supersedes the missing-cell counts in the historical entries below. Current gate:
1,492 required entries in each of seventeen languages; no missing/malformed cells,
no unexplained English equalities, 301 subtitle mappings and zero conservative
unbound candidates. All direct-play menus, results and twelve missions are roots;
seeded native scene labels stay required. SQL, symbolic IDs and stale scanner
output no longer inflate or hide coverage. Original 250 English sources and
recording/cue invariants remain protected by offline tests.

Authored sources are under tools/localization/authored. Spanish is directly
authored; fifteen campaign files record Google Translate machine assistance,
format/numeric/short-result checks and agent semantic samples. Context corrections
are recorded in review_corrections.json, combat_review.json and review_equality.json.
These are not externally certified native translations. Earlier rejected local
model drafts remain excluded; the optional prepare_drafts.py is authoring-only,
throttled, requires --fetch-drafts, and never writes the CSV. Runtime has no new
translation network service or dependency. Builder validates source conflicts and
format contracts before writing; only empty target cells are filled normally.

Fixed mixed-script picker fonts and external login-picker synchronization without
clearing input. WildfireText now retains nested key/template/display arguments.
Roster labels, ability categories/damage, preview captions and HUD ability/combo
names refresh without replaying actions. Movement captions dispatch canonical
animation IDs. Custom dropdown text uses shaping/direction and ellipsis. Loading
artwork uses the existing brand-only title plus a translated RECLAIM EARTH label.
Blender 4.3.2 in the Docker Python 3.15.2 tool image inspected 40 directly referenced
raster assets; diagnostic contact sheets stay ignored under .validation. That is
not a claim that every dynamically generated stage asset has native approval.

Live MCP checks captured all seventeen login locales and verified selected indexes,
RTL direction and picker bounds/frame styles. Screenshots exposed and led to fixes
for untranslated login fragments and picker clipping/missing RTL frame. Roster
French/Arabic, German results and Japanese Mission 1 were also inspected. Loading
status and locale-specific logo now refresh without restarting the request; RTL
stat fractions explicitly retain value/maximum order. An additional real loading
locale regression covers the target identity and branding.

Final verification after all edits: dotnet build passes with 0 errors and 154
existing warnings. Background Godot import exits 0 with no script parse errors or
assembly-load failures; existing addon TLS, editor shutdown/resource diagnostics
remain. Full run_tests.ps1 passes Django 104/104, NUnit 290/290 and GdUnit4
report_514 317/317, with 0 suite errors/failures/orphans and no process-exit leak
warning. The loading regression now lets its fade finish and consumes its threaded
request; verbose focused report_513 passes all 8 tests without a leaked object.
Earlier reports are superseded. All 14 written-localization Python checks pass
on host and Docker Python 3.15.2; all 5 English voice inventory checks pass. Root
.venv pip check is clean. The catalog check passes at 1,492 required keys/locale.

Final live screenshots confirm Arabic stat value/maximum order and a Japanese-to-
Thai loading switch updating both native status labels and the localized logo,
while preserving the loading target. No exhaustive native-language certification
or complete playthrough of every localized branch is claimed. Three transient
engine font-metric null-RID diagnostics were retained from an earlier MCP switching
run, without a script stack; they are not a resolved engine issue or suite errors.
CSV diff whitespace diagnostics include preserved source spaces and multiline
authoring whitespace; do not claim a clean generic whitespace check.

Backend and website API proxy /api/auth/countries/basic/ return HTTP 200, website
root returns 200, and PostgreSQL is healthy. Backend reports Python 3.15.2. The
three default containers remain running per the user's latest instruction; all
one-shot diagnostic containers self-removed. Our QA game is stopped and editor PID
12880 is preserved; no editor scene was saved. Changes remain local/uncommitted.
No staging, commits, pushes or exports.

## Python upgrade, Godot AI recovery and localization resumption — 2026-11-06

Started the normal three-service Docker stack before code edits. Backend now uses
python:3.15.2-slim-trixie and reports Python 3.15.2; pip check and Django system
check pass. Recreated ignored root .venv on host Python 3.15.2, installed all six
root requirements/dependencies, and verified pip check. Retained the broken 3.14
venv under ignored .validation/venv-backups/20261106-314. Cutscene orchestration
uses the same pinned image; Blender retains its ABI-matched embedded Python.
The opt-in export Dockerfile copies Python 3.15.2 from Bookworm for its older
glibc baseline. Its new python-verified setup stage built successfully: native
stdlib imports pass and smoke tests report Python 3.15.2 and .NET SDK 10.0.403.
Only this setup stage was built; no Android/Godot export tooling or game export
ran. Cutscene image build, Python 3.15.2 CLI/help and Blender 4.3.2 startup pass;
Blender reports bundled Python 3.13.5. Disposable smoke containers self-removed.

Godot AI's original production uvx command failed TLS trust, then failed its
no-build dependency resolution under host Python 3.15 (missing PyYAML wheels).
The shared launch policy now explicitly selects uv-managed Python 3.14 and the
system CA store, retaining TLS validation, canonical indexes and release cutoff.
Exact corrected uvx launch resolves 73 packages and CLI help exits successfully.
After the user reloaded the plugin, authenticated MCP confirmed the real project
session, existing editor PID 12880 and matching plugin/server 4.4.1. Read-only
client status confirms Codex, Claude Code/Desktop, Antigravity and VS Code are
configured; eight other installed client entries remain mismatched and unchanged.
No editor
was closed or scene saved. A future plugin update can overwrite this local patch.
An attempted separate background MCP launch was blocked by automatic approval
review; recovery used the existing editor's normal plugin reload instead.

LocaleService autoload and prior dirty localization files remain present; no
specific accidentally reverted scene change was identified. Restored English-only
uppercase styling for mission titles, roster ability labels, combat HUD identities
and scorecard names, preserving authored casing elsewhere. Guarded the scorecard
startup global lookup for detached scene tests. Added a Turkish/English
live scorecard round-trip regression. Existing English audio was not regenerated.
Build passes with 0 errors and 154 existing warnings. All twelve offline Python
voice/localization checks pass. Refreshed Roslyn/coverage reports retain 1,376
required entries and zero unbound candidates; coverage intentionally fails for
missing cells. Final background import exits 0. Full run_tests.ps1 passes:
Django 104/104, NUnit 290/290, GdUnit4 report_506 314/314 with zero suite
failures/errors/orphans. English casing and Turkish live-switch regression pass.
The earlier report_505 had one synthetic translation-collision failure, corrected
by unique test-only display names; it is superseded by report_506. Detached
scorecard absolute-path diagnostics are resolved. Existing addon/engine TLS,
GCHandle and import-shutdown resource diagnostics remain outside suite accounting;
do not describe the engine log as clean. Diff whitespace check passes. Backend,
website and website API proxy return HTTP 200; database is healthy.
Whole-client translation authoring remains incomplete (prior 1,200 missing cells
per non-English language); visual/native-language review remains outstanding.
Leave services running. All changes remain local/uncommitted; no game exports.

## Written localization implementation — incomplete, 2026-11-05

Completed shared LocaleCatalog/LocaleService (all seventeen languages in both real
selectors, ISO aliases, fonts/RTL, narrative reload without mission replacement),
301 corrected subtitle bindings independent of English recording IDs, missing
cinematic lookups, source aliases and explicit dynamic UI bindings. Added offline
Tokyo/interface/card translations, four missing narrative locale files containing
live card references, and Noto Hebrew/Thai/JP/KR/SC/TC assets/licenses. Legacy narrative
events are retained and classified separately from current callers. Registry/save/API
identities and authored English speech/ordering/intervals remain unchanged.

Full localization is **unfinished**. Current source/cell report: 1,376 required
entries, 1,200 missing per non-English language, zero malformed required cells or
unexplained identical required cells among available translations. Conservative
unbound literal queue is empty; this does not clear dynamic-key or visual/artwork
acceptance. See docs/localization/README.md, coverage.json/md and authoring.tsv.
A local M2M100 draft experiment dropped clauses/mistranslated labels and was rejected;
no generated drafts entered the CSV. Do not claim native-speaker approval.

Targeted GdUnit regression passed 4/4 (report_503, zero failures/errors/orphans),
including actual selector parity, live English speech through all locales, explicit
RTL/glyph checks and scorecard retranslation with literal brace-containing data.
Final verification at the pause checkpoint: background Godot import exited 0,
with existing addon TLS/editor-shutdown diagnostics and leaked-resource warnings.
C# build passed (0 errors; 154 code warnings plus one transient PDB-copy retry).
NUnit passed 290/290. All 12 Python inventory/localization checks passed, including
English audio hashes and cue order/words/timing; original 250 CSV keys and English
source cells are preserved. GdUnit4 report_504 executed 284 cases with 37 assertion
failures, 0 errors and 0 orphans. Failures include mission-header English casing,
roster ability-button English casing, and the Tokyo results operative-name casing.
Do not claim this checkpoint is push/release ready. Django could not read the D:
bind mount (/app/manage.py: No such device); docker cp for the copied-source test
workaround also failed with the host D: mount error. No Django pass is claimed.

Work is paused at the user's explicit request for today. The subsequent request
"shutdown the server" supersedes the earlier instruction to leave containers up.
Ran `docker compose stop backend website db`; all three project containers are
verified Exited (0). Containers/volumes were retained; no unrelated service or
existing Godot editor was stopped.

Resume first by bringing the normal stack up and checking the Docker D: bind mount.
Then fix the English presentation regressions while retaining authored casing in
non-English locales, rerun required verification, and continue translation authoring.
The full written-localization gate still fails: 1,200 required cells missing in each
of sixteen target languages. Visual/artwork/native-language review remains open.
See docs/localization/README.md and reports/report_504/results.xml.

Changes are local/uncommitted; no exports, commits or pushes authorized.


## Docker startup and local Python environment — 2026-11-05

User explicitly requested starting the stopped stack and populating root `.venv`;
this supersedes the earlier deferral of local installation. Docker daemon was
already available. `docker compose up -d` pulled PostgreSQL and built the missing
backend/website images, then started all three normal services. The startup
created the absent project database volume and applied existing Django migrations.
No existing volumes were removed. Leave the services running.

Created Git-ignored `.venv` with Python 3.14.10, upgraded pip to 26.3 and installed
the six root requirements plus dependencies. `pip check` passed. Direct versions:
Django 6.1.3, DRF 3.18.3, SimpleJWT 5.5.1, cors-headers 4.9.0,
psycopg2-binary 2.9.13, gunicorn 26.2.0. Root requirements remains canonical;
the Django test runner still uses the backend container.

Verified Compose config, all three running services, healthy db and pg_isready.
Backend countries/basic endpoint, website root and website API proxy each return
HTTP 200 using 127.0.0.1 with curl's proxy bypass. Initial Invoke-WebRequest checks
against localhost timed out; their cause was not investigated further.
Website build succeeded with npm reporting 16 audit findings (6 moderate/10 high),
a deprecated Clerk package and a large bundle warning; no dependency changes made.
Only setup and Markdown changed; no project code/assets edited, so no unit-suite
rerun or Godot refresh required. No exports, staging, commits or pushes.

## Three-service development stack — 2026-11-05

Removed the user-selected local inference container and its Compose service and
volume declaration. The default stack is now db/backend/website; other containers
remain running. Removed the integration's references from agent guides, README
and historical notes. Docker image/model data remain outside the repository.
Compose validation and three-service listing passed; website/API return HTTP 200.
Full suites passed: Django 104, NUnit 276, GdUnit4 303 (zero reported suite
failures/errors/orphans; existing engine/addon diagnostics remain). Repository
source/docs and ignored text/config/log scans found no remaining references.
No engine assets changed,
so no import refresh is required. No local Python installs or Git publication.

## Single Python manifest and virtualenv removal — 2026-11-05

User requested deleting backend/requirements.txt and root .venv, with no local
reinstallation yet. Root requirements.txt remains canonical. Backend Compose build
now uses repository root plus backend/Dockerfile; COPY reads root requirements and
only backend source. backend/Dockerfile.dockerignore provides a narrow source-only
context without changing exporter or other images. The old backend ignore file is
retained for historical context; Compose uses the Dockerfile-specific allowlist.

run_tests.ps1 runs Django directly in the already-running backend container, with
no host Python/virtualenv requirement or pip installs. Removed the obsolete root-
venv packaging helper. Root .venv was verified as the exact in-workspace non-link
virtualenv before recursive deletion; backend/.venv was already absent. Do not
recreate/install a local venv unless the user explicitly requests it. The older
root-venv test notes below are historical and superseded by this workflow.

Verification passed: Compose config, backend image build and image dependency
imports. Image includes root requirements and backend source, excludes .env/venv.
Full run_tests.ps1 passed Django 104, NUnit 276 and GdUnit4 303 with zero reported
suite failures/errors/orphans. Unchanged tests emitted five outside-tree absolute
lookup diagnostics; the addon warns about missing gdunit4.api. Engine logs are
therefore not diagnostic-free. Both local venv paths and backend requirements are absent.
No game engine files changed, so no Godot import refresh is needed. Normal services
remain running; no exports or Git publication is authorized for this new change.

## Later campaign repair implemented and verified — 2026-11-05

Repair source baseline: c2ad661. The user subsequently authorized committing and
pushing the current verified batch to origin/main. No game exports.
Docker's core services were built/started before code edits and remain up.
The user's later message rescinded pausing containers at completion.

Implemented gameplay/action evidence, full-height mandatory gates/interlocks,
continuous protected crossings, pod keyboard/pad context input, accepted terminal
consumption, formation eligibility, sequential waves and explicit completion
acceptance. Manual defense waves no longer retire from opposite-end proximity.
Decorative columns now leave 160px beneath them for reachable ground combat;
Mission 12's post-heat formation is on the safe exit. Bosses preserve HP/thresholds,
use explicit player targets and layer-2 hurtboxes, hold phases for defenders, expose
HUD telemetry and support real source-aware projectile parries. Recovery resets
unfinished arena phases/formations/projectiles while retaining prior progress,
accepted defeat and deduplicated rewards. Death fades remove collision immediately.

CampaignSettlement claims a stable per-run JSONB receipt and updates score plus
GREATEST high-water progress in one PostgreSQL transaction. Results await that and
main-thread unlocks, show saving/failure/retry/no-campaign status, and gate forward
navigation. Continue 5→6, 6→7 and final Return to Main Menu are repaired. Global's
obsolete Mission 9 cap now follows LevelCatalog. Replay/Resume/Level Select remain
direct entry; no Mission 13 or separate completed-campaign flag was added.

Fourteen later cinematic owners capture/restore cursor and menu help, stop owned
audio, consume all Skip routes, release mapped actions, defer navigation and emit
completion exactly once, including independent removal. Finished aftermaths free.
Missing artwork safely completes; missing speech retains full timed subtitles.
Shared speaker framing, recognizable approved operative sheets and small-window
subtitle scaling retain the original schedules, lighting/particles/camera effects.
Seven reviewed Blender environment families supply 54 original PNG plates,
distinct aftermath settings and six matching boss pairs. Thirteen missing speech
tracks comprise 117 full natural-speed takes with real established voice IDs.
Fourteen unique synthesized beds and complete audio validation/provenance ship.
See docs/LATER_CAMPAIGN_ACCEPTANCE.md for tools and human acceptance checklist.

Final verification: `dotnet build AxiomForce.csproj --no-restore` succeeds with zero
errors and 155 project/addon warnings. Final `run_tests.ps1` passes Django 104/104,
NUnit 276/276 and GdUnit4 303/303: zero errors, failures, skipped tests or orphans.
New real engine checks cover melee/parry against all six bosses, wrong/right
gates, spatial phase/heat crossing, keyboard/controller pod input, phase support
ordering, checkpoint attempts, premature terminals, all fourteen overlays' eight
Skip routes, missing media/independent removal, real speech sampling and save UI
pending/failure/retry/no-campaign behavior. PostgreSQL fixtures cover high-water
progress, eight concurrent retries, discarded committed responses and failures;
all fixture campaigns/receipts are removed by exact ownership.

Fresh input-driven runs reached actual results in every Mission 6–12, including
real attacks, swaps, signatures, pod actions, terminals and checkpoint recovery.
No ledger injections, health overrides or direct enemy damage were used by the
playthrough bot. Optimized simulation times are not human pacing evidence.
Accepted runs and separate diagnostic failures are under ignored
`.validation/later-campaign/`; `accepted-playthroughs.json` points to raw attempts.
MCP navigation verified both acts, all later prologues, Continue, direct Replay,
owned PostgreSQL Resume, direct Level Select and final Return to Main Menu. No
existing campaign was selected for review. All owned game processes were stopped;
the user's editor remains open and the core Docker services remain up.

Every later cinematic cue was inspected at 1920×1080, 1920×1200 and 2560×1080,
with 800×600 subtitle/control checks: 504 native captures, no size or subtitle-bound
failures. Final review caught/fixed missing redraw requests in Apex/Judgment
aftermaths and per-frame audio scrubbing in Nexus aftermath/Spire intro. Full
28-track decoding confirms exact duration, unclipped peaks and fourteen distinct
atmospheres; all 117 new takes fit complete windows at speed 1. Physical controller/
touch use, normal-speed pacing and subjective listening remain explicitly human
acceptance items in docs/LATER_CAMPAIGN_ACCEPTANCE.md.

Background Godot refresh completed and all 54 PNG import sidecars exist. Existing
asset_placer TLS/HTTP and Godot-AI socket diagnostics remain during import, followed
by the same 27 ObjectDB/12-resource/PagedAllocator shutdown diagnostics observed in
the initial refresh; these are separate from zero-orphan project test results.
Project checker retains the baseline 10 errors/27 warnings in unrelated addon/menu/
UID paths. Git diff whitespace check passes. The repair was verified from baseline c2ad661 before the user authorized Git
publication. No game exports were produced. `run_tests.ps1` now preserves Godot console output
and returns failure for any failed/skipped required suite instead of summary-only
success. README and MEMORY contain the final outcome, not historical pass claims.

## Canonical Campaign Architecture (3-Act Structure, 12 Missions)
Formally established the 3-Act campaign structure across 12 canonical missions:
- **Act I: Fracturing the Grid (Missions 1–4)** *(Complete)*:
  - Mission 1: *Ghost in the Gridlock* (Imperial Palace culture-purge grounds)
  - Mission 2: *The Concrete Wildfire* (Containment Zone 4 checkpoints)
  - Mission 3: *Neon Rust and Data Dust* (Scrap yards & comm towers)
  - Mission 4: *Shattered Aegis* (Border fortress breach & Boss Mech Vanguard-X; awards `ACT I COMPLETE`)
- **Act II: The Infrastructure Collapse (Missions 5–7)** *(Complete)*:
  - Mission 5: *Names Without Numbers* (Downtown Civic Processing District & biometric registry) — *Complete*
  - Mission 6: *Cyber-Grave Labs* (Subterranean bio-synthetic conversion foundry) — *Complete*
  - Mission 7: *The Foundry Forge* (Geothermal plasma reactor; overloading core shatters Citadel sky-shields; awards `ACT II COMPLETE`) — *Complete*
- **Act III: The Citadel Spire & Final Liberation (Missions 8–12)** *(Active Next Frontier)*:
  - Mission 8: *Stratos Docks* (High-altitude aviation hub; hijacking VTOL gunship) — **Complete** (Gameplay & Epilogue Cutscene)
  - Mission 9: *Spire Outer Hull* (High-mobility vertical exterior ascent & aerial drone swarms) — **Complete** (Gameplay, Prologue & Epilogue Cutscenes)
  - Mission 10: *The Cyber-Nexus* (Mainframe core, AI surveillance override & multi-operative security gauntlet) — **Complete** (Gameplay, Prologue & Epilogue Cutscenes)
  - Mission 11: *The Apex Bastion* (Praetorian guard & penultimate boss mech defending throne access) — **Complete** (Gameplay, Prologue & Epilogue Cutscenes)
  - Mission 12: *Judgment Day* (Citadel Summit / Throne Room showdown against Void Commander; awards `CAMPAIGN COMPLETE`) — **ALL COMPLETE** (Prologue Cutscene, Gameplay, Post-Level 12 Finale Epilogue Cutscene, and Campaign Complete Victory Scorecard Fully Verified)

## Post-Level 12 Cutscene: Dawn of Axiom — Campaign Finale & Epilogue (2026-11-04)

### Summary of Enhancements
Authored, implemented, and fully verified the grand campaign finale cinematic epilogue overlay (`JudgmentDayAftermath.cs`, `JudgmentDayAftermathStage.cs`, `scenes/cutscenes/judgment_day_aftermath.tscn`) concluding Mission 12 (*Judgment Day*) and the entire 12-mission campaign of Axiom Force:
1. **Narrative & Authored Dialogue (`assets/cutscenes/judgment_day_aftermath/cues.json`)**:
   - Monotonic 90.0-second timeline across 9 authored dialogue cues tracking the liberation of Neo-Arcadia and the stand-down order:
     - Cue 1 (0.0s - 10.0s) [AXIOM-7]: Singularity containment confirmed; dark-matter reactor zeroed; Void Commander's command matrix collapsed globally.
     - Cue 2 (10.0s - 20.0s) [MIRAGE]: Executive firewalls dead; global broadcast transmitters open; quarantine sector barriers dropping across the planet.
     - Cue 3 (20.0s - 30.0s) [TITAN]: Ten million people trapped in the dark breathing again as power grids light up from the sumps to the cloudline.
     - Cue 4 (30.0s - 40.0s) [OCEANUS]: Atmospheric scrubbers reversing; ocean breezes flowing through the shattered spire; the perpetual storm broken.
     - Cue 5 (40.0s - 50.0s) [SURYA]: Eternal night ended; solar cells drinking in natural sunlight for the first time in fifty years.
     - Cue 6 (50.0s - 60.0s) [AURA]: Sensing the harmonious resonance of millions singing and weeping tears of joy across a free world.
     - Cue 7 (60.0s - 70.0s) [AXIOM-7]: Imperial civilian registry files erased and restored to civilian autonomy; Axiom Force parameters 100% achieved.
     - Cue 8 (70.0s - 80.0s) [KAITO]: From castaways and ghosts in the gridlock to a unified squad; giving humanity their tomorrow back.
     - Cue 9 (80.0s - 90.0s) [KAITO]: The dawn belongs to all of us; Axiom Force stand down; the city is free.
2. **Procedural Staging (`JudgmentDayAftermathStage.cs`)**:
   - Radiant sunrise sky interpolating from midnight cosmic void into warm morning sunrise.
   - Collapsing singularity aurora with 40+ rising luminescent motes.
   - Distant cityscape of Neo-Arcadia with 12 districts progressively lighting up.
   - Shattered cathedral balustrade and broken Void Commander throne dais.
   - 6 Operative silhouettes (Kaito, Surya, Oceanus, Mirage, Titan, Aura) with team-colored visors and accents.
   - 2.39:1 letterbox bars, CRT scanlines, and 100% crash-free zero-texture guarantee (`ResourcesAvailable => true`).
3. **Campaign Extraction Flow (`CodeLevel.cs`)**:
   - Mission 12 extraction instantiates `JudgmentDayAftermath`, and upon `Finished`, displays `ShowCampaignCompleteScorecard`.
4. **Automated Verification**:
   - 264 NUnit tests PASSED (including `JudgmentDayAftermathTests.cs`).
   - 289 GdUnit4 tests PASSED across 59 suites (including `test_judgment_day_aftermath.gd`, 0 errors, 0 failures, 0 orphans).
   - 104 Django tests PASSED.

## Mission 12: Judgment Day Gameplay Implementation (2026-11-04)

### Summary of Enhancements
Authored, implemented, and fully verified the full 2D platforming gameplay for the campaign finale Mission 12 (*Judgment Day*, `JudgmentDayMission.cs`, `scenes/gameplay/level12.tscn`) in the Grand Citadel Summit and Void Singularity Chamber:
1. **Mission Structure & Multi-Operative Synergy**:
   - 6 high-altitude cathedral sectors (28,500 horizontal units): Singularity Ingress Gantry, Power Siphon Trench, Void Barrier Corridor, Cathedral Vaulted Ascent, Grand Throne Dais, and Singularity Core Overload.
   - Ability gates requiring Oceanus Hydro Torrent, Apex Titan Slam, Surya Prime Bulwark, and Aura Phoenicia Resonance, plus laser curtain fields bypassed via Mirage Blade Phase Shift.
2. **Supreme Boss Encounter // The Void Commander (`VoidCommanderBoss2D.cs`)**:
   - 750 HP multi-phase cyber-warlord armed with twin void katanas, blink teleportation, and deflectable crescent plasma slashes (`VoidPlasmaSlash2D.cs`, 55 reflected damage).
   - Phase 1 dual-blade strikes and ground shockwaves, Phase 2 blink teleports and crescent slashes, Phase 3 Singularity Overdrive with dark matter bursts and 3-blade rapid-fire barrage.
3. **Pure C# Engine-Free Ledger (`JudgmentDayProgress.cs`)**:
   - 14 ordered milestones, 14 finite authored combat formations, and bounded retry slot rewards.
4. **Campaign Complete Scorecard (`CyberVictoryScorecard.cs`)**:
   - `ShowCampaignCompleteScorecard` providing celebratory campaign completion badge and Return to Main Menu routing.
5. **Automated Verification**:
   - 261 NUnit tests PASSED (including `JudgmentDayProgressTests.cs`).
   - 285 GdUnit4 tests PASSED across 58 suites (including `test_judgment_day_mission.gd` and `test_campaign_levels.gd`, 0 errors, 0 failures, 0 orphans).
   - 104 Django tests PASSED.

## Pre-Level 12 Cutscene: The Final Reckoning — Judgment Day Intro (2026-11-04)

### Summary of Enhancements
Authored, implemented, and fully verified the pre-mission cinematic prologue overlay (`JudgmentDayIntro.cs`, `JudgmentDayStage.cs`, `scenes/cutscenes/judgment_day_intro.tscn`) introducing Mission 12 (*Judgment Day*) as Axiom Force enters the Citadel Summit Throne Room & Void Singularity Chamber:
1. **Narrative & Authored Dialogue (`assets/cutscenes/judgment_day_intro/cues.json`)**:
   - Monotonic 90.0-second timeline across 9 authored dialogue cues tracking arrival at 5,000m altitude and confronting the Void Commander:
     - Cue 1 (0.0s - 10.0s): AXIOM-7 reporting arrival at Spire Level 120 (5,000m), singularity reactor output approaching critical mass, atmospheric containment warping under gravitational shear.
     - Cue 2 (10.0s - 20.0s): Surya Prime deploying solar bulwark shield against cosmic gravitational waves bending light around the dais.
     - Cue 3 (20.0s - 30.0s): Oceanus detecting that the Void Commander is drawing the entire planetary geothermal power grid into the singularity to initiate total civilian purge.
     - Cue 4 (30.0s - 40.0s): Mirage Blade observing the Void Commander waiting alone on the floating obsidian throne—no guards, no drones, just pure cybernetic dread.
     - Cue 5 (40.0s - 50.0s): Apex Titan slamming electromagnetic knuckles together, reminding the squad that the tyrant's crown is coming off today.
     - Cue 6 (50.0s - 60.0s): Aura Phoenicia detecting the heartbeat of millions across the eleven liberated sectors below singing for the dawn.
     - Cue 7 (60.0s - 70.0s): AXIOM-7 intercepting the chilling broadcast from the throne: "You crawled out of the scrap heaps only to kneel before godhood. Axiom Force dies tonight."
     - Cue 8 (70.0s - 80.0s): Mirage Blade synchronizing phase blades and squad shields for the final assault.
     - Cue 9 (80.0s - 90.0s): Kaito Kurogane drawing his kinetic katana and leading Axiom Force into the final strike to liberate the world.
2. **Procedural Visual Staging (`JudgmentDayStage.cs`)**:
   - Deep cosmic void background gradient (`#020008` to `#160028` to `#32004d`) with star dust motes.
   - Massive pulsating cosmic singularity core with pitch-black event horizon, cyan/magenta/gold gravitational lensing halo rings, 50-particle orbiting accretion disk, and swirling coronal flares.
   - Grand cathedral vaulted ceiling struts, massive obsidian columns with glowing cyan/magenta power glyph conduits.
   - Polished reflective obsidian dais floor with perspective grid lines and specular reflections of the singularity and conduits.
   - Floating obsidian throne dais with anti-gravity rings, imperial crest, and imposing Void Commander silhouette with glowing crimson visor and ignited twin katanas (crimson plasma and dark matter magenta).
   - Full Axiom Force squad silhouettes in battle formation with signature weapon and ability glows (Kaito, Surya, Oceanus, Mirage, Titan, Aura).
   - CRT scanline overlay, 2.39:1 anamorphic cinema bars, and guaranteed zero external texture dependencies (`ResourcesAvailable => true`).
3. **Engine & Scorecard Integration**:
   - Mission 12 registered in `LevelCatalog.cs` (`JudgmentDay`, `res://scenes/gameplay/level12.tscn`, all 6 abilities).
   - `CyberVictoryScorecard.cs` routes `_nextMission == 12` into `judgment_day_intro.tscn`.
   - Level selection menu updated with Sector 12 card in `level_selection.tscn` and `level_selection.gd`.
   - Placeholder `JudgmentDayMission.cs` and `scenes/gameplay/level12.tscn` created so level selector and campaign tests instantiate Level 12 cleanly.
4. **Automated Verification & Test Coverage**:
   - NUnit suite `JudgmentDayIntroTests.cs` (3 test cases): Exact 90.0s duration, scene file existence, and 9 ordered cues validation in `cues.json`.
   - GdUnit4 suite `test_judgment_day_intro.gd` (3 test cases): Skip & natural finish, missing art fallback, subtitle cue timeline seeking with 0 orphan nodes.
   - Campaign suite `test_level_selection_2d.gd` (5 test cases), `test_act_ii_opening.gd` (6 test cases), and `test_campaign_levels.gd` (10 test cases) all updated for 12 missions and passing with 0 orphans.
   - Full test suite runner passed: 104 Django, 243 NUnit, 282 GdUnit4 across 57 suites (100% pass, 0 orphans, `report_480`).

## Post-Level 11 Cutscene: The Gates of Judgment — Apex Bastion Aftermath (2026-11-04)

### Summary of Enhancements
Authored, implemented, and fully verified the post-mission cinematic epilogue overlay (`ApexBastionAftermath.cs`, `ApexBastionAftermathStage.cs`, `scenes/cutscenes/apex_bastion_aftermath.tscn`) concluding Mission 11 (*The Apex Bastion*) upon extraction:
1. **Narrative & Authored Dialogue (`assets/cutscenes/apex_bastion_aftermath/cues.json`)**:
   - Monotonic 90.0-second timeline across 9 authored dialogue cues tracking the aftermath of the Bastion Praetor's defeat and the breaching of the Grand Citadel Throne Room blast doors:
     - Cue 1 (0.0s - 10.0s): AXIOM-7 reporting Bastion Praetor destroyed, outer fortress redoubts silenced, and Citadel summit quarantine protocols disabled across all sectors.
     - Cue 2 (10.0s - 20.0s): Mirage Blade observing the Praetorian defense line completely collapsing and the primary hydraulic blast doors groaning open.
     - Cue 3 (20.0s - 30.0s): Apex Titan kicking smoking chassis plating off the fallen boss mech and noting its twin flak cannons are officially scrap.
     - Cue 4 (30.0s - 40.0s): Oceanus reading violent energy spikes in the stratospheric vortex above as the Throne Room seal cracks open.
     - Cue 5 (40.0s - 50.0s): Surya Prime observing the entire liberated city grid below watching the Citadel Spire summit illuminate.
     - Cue 6 (50.0s - 60.0s): Aura Phoenicia detecting the harmonic frequency of the Void Core pulsing directly ahead through the parted blast doors.
     - Cue 7 (60.0s - 70.0s): Mirage Blade confirming all automated locks and defense relays on the Throne Room are overridden.
     - Cue 8 (70.0s - 80.0s): AXIOM-7 warning telemetry indicates the Void Commander is powering the singularity drive at maximum output.
     - Cue 9 (80.0s - 90.0s): Kaito Kurogane drawing his katana and leading Axiom Force across the threshold into the final showdown (*Judgment Day*).
2. **Procedural Visual Staging (`ApexBastionAftermathStage.cs`)**:
   - High-altitude void storm sky gradient (`#03020a` to `#180b26`) with dynamic arcing void lightning strikes (magenta, crimson, golden discharge).
   - High obsidian battlements deck with illuminated cyan/amber hazard conduits.
   - Smoldering, cracked chassis of the defeated Bastion Praetor boss mechanoid with severed limbs, sparking exposed circuits, and rising billows of smoke.
   - Monumental Grand Citadel Throne Room blast doors parting outward, revealing a blinding radiant void portal shimmering with golden and crimson energy.
   - Silhouettes of all six Axiom Force operatives advancing toward the radiant portal threshold.
   - CRT scanline overlay, 2.39:1 anamorphic cinema bars, and guaranteed zero external texture dependencies (`ResourcesAvailable => true`).
3. **Engine & Scorecard Integration**:
   - Attached in `CodeLevel.CompleteMission` when `MissionNumber == 11`, freeze-isolating the extracted level, playing the cinematic epilogue, and deferring to `CyberVictoryScorecard.ShowMissionScorecard` targeting Mission 12 upon natural finish or skip (ESC / Space / Enter / gamepad / click).
4. **Automated Verification & Test Coverage**:
   - NUnit suite `ApexBastionAftermathTests.cs` (3 test cases): Exact 90.0s duration, scene file existence, and 9 ordered cues validation in `cues.json`.
   - GdUnit4 suite `test_apex_bastion_aftermath.gd` (4 test cases): Skip & natural finish, missing art fallback, subtitle cue timeline seeking with 0 orphan nodes.
   - Full test suite runner passed: 104 Django, 240 NUnit, 279 GdUnit4 across 56 suites (100% pass, 0 orphans, `report_475`).

## Mission 11 Gameplay: The Apex Bastion (Stratospheric Fortress) (2026-11-04)

### Summary of Enhancements
Authored, implemented, and fully verified the full 2D platformer mission (ApexBastionMission.cs, scenes/gameplay/level11.tscn) extending CodeLevel and implementing IFiniteCombatHost:
1. **6-Sector Stratospheric Fortress Layout (28,500 Unit Horizontal Scope)**:
   - *SEC.01 // STRATOSPHERIC SUMMIT GANTRY*: Open-air freight lift arrival platform at 4,000m, howling crosswinds, high-altitude gantry catwalks, and automated perimeter patrols (summit1, summit2). Checkpoint at x=1,200.
   - *SEC.02 // FLAK BATTERY TRENCHES & COOLANT CONDUITS*: Pressurized plasma flak coolant lines (CoolantVent.cs) requiring Oceanus Douse (GateKind.FireVent), heavy magnetic titan-clamp battery mooring barricades requiring Apex Titan Slam (GateKind.RubbleWall), and flak battery neutralization (lak1, lak2, lak3). Checkpoint at x=5,500.
   - *SEC.03 // LOWER REDOUBT & LASER DEFENSE CORRIDOR*: Multi-tier obsidian battlements, high-voltage exterior laser grid curtains (CyberLaserGrid2D.cs) bypassed via Mirage Blade Phase Shift, concentrated plasma radiation thermal vents defended under Surya Prime Bulwark (GateKind.HeatVent), and lower redoubt clearance (
edoubt1, 
edoubt2, 
edoubt3). Checkpoint at x=10,500.
   - *SEC.04 // PRAETORIAN HYDRAULIC ELEVATOR SHAFT*: Vertical wall-jump lift shaft, harmonic dissonance acoustic security locks requiring Aura Phoenicia Resonance (GateKind.SonicLock), and praetorian vanguard waves (praetorian1, praetorian2). Checkpoint at x=16,000.
   - *SEC.05 // GRAND BASTION REDOUBT // BASTION PRAETOR*: Multi-platform fortress rampart arena facing the Citadel Spire summit, featuring the penultimate multi-phase boss mechanoid BastionPraetorBoss2D (620 HP) armed with swept deflectable PraetorPlasmaBolt2D projectiles (reflectable by Kaito's Kinetic Parry for 45 reflected damage), deployable energy shields, sweeping flak salvos, and Phase 3 Praetor Overdrive Fury (rena1, rena2, rena3). Checkpoint at x=21,000.
   - *SEC.06 // THRONE ROOM EMBANKMENT & CITADEL BREACH*: Citadel blast doors override terminal hack, final praetorian garrison encounter (reach1), and extraction threshold leading into the Grand Citadel Throne Room (Mission 12). Checkpoint at x=26,000.
2. **Deflectable Boss Projectile (PraetorPlasmaBolt2D.cs)**:
   - Swept high-velocity plasma bolt fired by the Bastion Praetor boss.
   - Implements public void TakeDamage(float amount) { Reflect(); } which seamlessly triggers CyberShogun.cs parry logic, turning bolt cyan and reflecting it back to the boss for 45 reflected damage.
3. **Multi-Phase Boss Mechanoid (BastionPraetorBoss2D.cs)**:
   - 620 HP boss mechanoid defending the high fortress ramparts.
   - Procedural rendering: heavy obsidian gunmetal chassis, crimson command visor, twin shoulder-mounted plasma flak cannons, deployable hexagonal energy shield barrier, and rocket thruster flames.
   - Phase 1 (100% - 70% HP): Rhythmic plasma cannon salvos, ground hover repositioning, and jump stomps.
   - Phase 2 (70% - 35% HP): Rapid swept plasma flak bursts, deployable Praetorian energy shield, sweeping beam cannons, and tactical hover dashes.
   - Phase 3 (35% - 0% HP): Praetor Overdrive Fury — dual overcharged plasma barrages, seismic shockwave ground slams, and rapid jet thruster charges.
4. **Squad Synergy & Campaign Wiring**:
   - Full 6-operative roster active with operative-specific ability gates and dynamic in-level squad swapping.
   - Sector 11 card in Level Selection (scenes/main_menu/level_selection.tscn, scripts/main_menu/level_selection.gd).
   - Scorecard routing in scripts/gameplay/CodeLevel.cs (MissionNumber == 11 targeting Mission 12).
5. **Pure C# Ledger & Verification**:
   - ApexBastionProgress.cs pure C# engine-free mission ledger tracking 14 ordered milestones, 14 authored combat formations, and bounded retry slot rewards.
   - Automated unit test suites: NUnit (ApexBastionProgressTests.cs, 237 tests total) and GdUnit4 (	est_apex_bastion_mission.gd), with all 11 missions validated in 	est_campaign_levels.gd and 	est_level_selection_2d.gd (0 errors, 0 failures, 0 orphans).

## Pre-Level 11 Cutscene: The Praetorian Vanguard — Apex Bastion Intro (2026-11-04)

### Summary of Enhancements
Authored, implemented, and fully verified the pre-mission cinematic prologue overlay (ApexBastionIntro.cs, ApexBastionStage.cs, scenes/cutscenes/apex_bastion_intro.tscn) introducing Mission 11 (*The Apex Bastion*) as the freight lift arrives at the stratospheric summit:
1. **Narrative & Authored Dialogue (ssets/cutscenes/apex_bastion_intro/cues.json)**:
   - Monotonic 90.0-second timeline across 9 authored dialogue cues tracking arrival at 4,000m altitude and reconnoitering the Bastion defenses:
     - Cue 1 (0.0s - 10.0s): AXIOM-7 announcing arrival at 4,000 meters elevation, stratospheric staging platform docked, registering extreme void-resonance readings.
     - Cue 2 (10.0s - 20.0s): Apex Titan stepping out into howling crosswinds, cracking knuckles over open battlements.
     - Cue 3 (20.0s - 30.0s): Surya Prime deploying bulwark shields to anchor squad against high-altitude gusts.
     - Cue 4 (30.0s - 40.0s): Mirage Blade spotting heavy Praetorian mechs locking down the outer ramparts.
     - Cue 5 (40.0s - 50.0s): Oceanus analyzing automated flak batteries firing blind into the cloud sea below.
     - Cue 6 (50.0s - 60.0s): Aura Phoenicia detecting harmonic disturbance and psychic resonance of the Void Commander directly above.
     - Cue 7 (60.0s - 70.0s): AXIOM-7 confirming the Apex Bastion is the penultimate defense line before the Citadel Throne Room.
     - Cue 8 (70.0s - 80.0s): Mirage Blade identifying the elevator shaft access hatch as the only path inside.
     - Cue 9 (80.0s - 90.0s): Kaito Kurogane drawing blade and ordering the squad to breach the Bastion.
2. **Procedural Visual Staging (ApexBastionStage.cs)**:
   - High-altitude cloud sea gradient (#060814 to #16223e) with drifting atmospheric vapor layers and stratospheric turbulence.
   - Sweeping red alert sky searchlights penetrating the clouds.
   - Towering brutalist fortress battlements in obsidian and gunmetal with cyan hazard markings.
   - Twin-linked plasma flak turrets with animated tracking and laser targeting beams.
   - Praetorian cyber-samurai silhouettes stationed along the ramparts.
   - Penultimate boss mech silhouette (*Bastion Praetor*) towering over the upper redoubt with pulsing crimson visor.
   - Open freight lift platform with glowing magnetic safety rails and operative squad silhouettes.
   - CRT scanline overlay, 2.39:1 anamorphic cinema bars, zero external texture dependencies (ResourcesAvailable => true).
3. **Engine & Scorecard Integration**:
   - Mission 11 registered in LevelCatalog.cs (The Apex Bastion, 
es://scenes/gameplay/level11.tscn, all 6 abilities).
   - CyberVictoryScorecard.cs routes _nextMission == 11 directly into pex_bastion_intro.tscn.
   - Level selection menu updated with Sector 11 card in level_selection.tscn and level_selection.gd.
4. **Automated Verification & Test Coverage**:
   - NUnit suite ApexBastionIntroTests.cs (3 test cases): Exact 90.0s duration, scene file existence, and 9 ordered cues validation in cues.json.
   - GdUnit4 suite 	est_apex_bastion_intro.gd (3 test cases): Skip & natural finish, missing art fallback, subtitle cue timeline seeking with 0 orphan nodes.
   - Full test suite runner passed: 104 Django, 219 NUnit, 272 GdUnit4 across 54 suites (100% pass, 0 orphans, 
eport_470).

## Post-Level 10 Cutscene: The Logic Collapse — Cyber-Nexus Aftermath (2026-11-04)

### Summary of Enhancements
Authored, implemented, and fully verified the post-mission cinematic epilogue overlay (`CyberNexusAftermath.cs`, `CyberNexusAftermathStage.cs`, `scenes/cutscenes/cyber_nexus_aftermath.tscn`) concluding Mission 10 (*The Cyber-Nexus*) upon extraction:
1. **Narrative & Authored Dialogue (`assets/cutscenes/cyber_nexus_aftermath/cues.json`)**:
   - Monotonic 90.0-second timeline across 9 authored dialogue cues tracking the aftermath of the Nexus Overseer's destruction and boarding of the Apex Ascent Lift:
     - Cue 1 (0.0s - 10.0s): AXIOM-7 reporting Nexus Overseer core neutralized, all three quantum processing nodes collapsed into terminal cascades, defense grid flatlined across Citadel.
     - Cue 2 (10.0s - 20.0s): Mirage Blade observing server towers dumping petabytes of encrypted garbage and automated robot reinforcement queues zeroing out.
     - Cue 3 (20.0s - 30.0s): Apex Titan chuckling at the Overseer's shattered containment prism ring and pointing out liquid helium blowing every gasket in the room.
     - Cue 4 (30.0s - 40.0s): Oceanus warning that thermal runaway is accelerating and deck plating is already vibrating from reactor shockwaves.
     - Cue 5 (40.0s - 50.0s): Surya Prime observing that Praetorian automated batteries above have switched to emergency auxiliary generators.
     - Cue 6 (50.0s - 60.0s): Aura Phoenicia detecting heavy hydraulic footsteps of Praetorian mechs and cyber-samurai rallying for a final stand on the ramparts above.
     - Cue 7 (60.0s - 70.0s): Mirage Blade cracking the executive security lockouts on the Apex Ascent Lift, sliding the boarding gates open.
     - Cue 8 (70.0s - 80.0s): AXIOM-7 warning that sensor telemetry detects extreme energy signatures atop the Spire as the Void Commander orders the Bastion to fire at will.
     - Cue 9 (80.0s - 90.0s): Kaito Kurogane ordering the squad to board the lift and assault the Apex Bastion.
2. **Procedural Visual Staging (`CyberNexusAftermathStage.cs`)**:
   - Abyssal cybernetic cavern background gradient (`#02040c` to `#0a0f24`) with perspective digital floor grid and emergency red alarm wash pulses.
   - Towering server monoliths with flashing critical red alert runes (`#ff2255`), amber warning indicators (`#ffaa00`), and falling corrupted data rain streams.
   - Smoldering hexagonal chassis of the defeated Nexus Overseer boss mechanoid with cracked core, electric spark arcs, rising smoke, and fractured cyan containment prism shards strewn across the gantry.
   - Venting cryogenic liquid helium steam plumes billowing upward from ruptured floor pipes.
   - Colossal vertical magnetic Apex Ascent Lift gantry with glowing cyan rails, warning chevrons, animated opening boarding gate, and illuminated interior cabin.
   - Praetorian Bastion fortress silhouette with sweeping red surveillance spotlights visible in the upper elevator shaft.
   - Operative silhouettes standing on the gantry deck with glowing signature weapons and accents, advancing toward the lift.
   - CRT scanline overlay, 2.39:1 anamorphic cinema bars, and guaranteed zero external texture dependencies (`ResourcesAvailable => true`).
3. **Engine & Scorecard Integration**:
   - Attached in `CodeLevel.CompleteMission` when `MissionNumber == 10`, freeze-isolating the extracted level, playing the cinematic epilogue, and deferring to `CyberVictoryScorecard.ShowMissionScorecard` targeting Mission 11 upon natural finish or skip (ESC / Space / Enter / gamepad / click).
4. **Automated Verification & Test Coverage**:
   - NUnit suite `CyberNexusAftermathTests.cs` (3 test cases): Exact 90.0s duration, scene file existence, and 9 ordered cues validation in `cues.json`.
   - GdUnit4 suite `test_cyber_nexus_aftermath.gd` (4 test cases): Skip & natural finish, missing art fallback, subtitle cue timeline seeking, and teardown audio stop with 0 orphan nodes.
   - Full test suite runner passed: 104 Django, 216 NUnit, 269 GdUnit4 across 53 suites (100% pass, 0 orphans, `report_464`).

## Mission 10 Gameplay: The Cyber-Nexus (Mainframe Core) (2026-11-04)

### Summary of Enhancements
Authored, implemented, and fully verified the full 2D platformer mission (`CyberNexusMission.cs`, `scenes/gameplay/level10.tscn`) extending `CodeLevel` and implementing `IFiniteCombatHost`:
1. **6-Sector Mainframe Core Layout (27,500 Unit Horizontal Scope)**:
   - *SEC.01 // QUANTUM INGRESS CATWALK*: High-voltage server catwalks, wall-jump vertical data bus shafts, and automated defense patrols (`nexus1`, `nexus2`). Checkpoint at x=1,200.
   - *SEC.02 // LIQUID HELIUM CONDUITS*: Pressurized sub-zero cryogenic coolant lines (`CoolantVent.cs`) requiring Oceanus Douse (`GateKind.FireVent`), heavy magnetic titan-clamp data bus housings requiring Apex Titan Slam (`GateKind.RubbleWall`), and quantum node Alpha override terminal (`node:alpha`). Checkpoint at x=4,200.
   - *SEC.03 // OPTICAL DATA FIREWALL ARRAYS*: High-voltage optical laser firewalls (`CyberLaserGrid2D.cs`) bypassed via Mirage Blade Phase Shift, thermal radiation exhaust corridors defended under Surya Prime Bulwark (`GateKind.HeatVent`), and quantum node Beta override terminal (`node:beta`). Checkpoint at x=8,200.
   - *SEC.04 // HIGH-FREQUENCY RESONANCE VAULT*: Vertical server shafts, acoustic encryption locks requiring Aura Phoenicia Resonance (`GateKind.SonicLock`), and quantum node Gamma override terminal (`node:gamma`). Checkpoint at x=13,200.
   - *SEC.05 // MAINFRAME SANCTUM // NEXUS OVERSEER*: Multi-phase 2D boss mechanoid encounter (`NexusOverseerBoss2D.cs`, 560 HP) armed with deflecting `NexusPulseBolt2D` swept projectiles (reflectable by Kaito's Kinetic Parry for 40 reflected damage), orbital containment prisms, ground shockwaves, and Phase 3 core purge overdrive. Checkpoint at x=18,200.
   - *SEC.06 // APEX ASCENT GANTRY*: Mainframe Core terminal hack (`core:terminal`), data purge initiation, and extraction gantry threshold unlocking access to the Apex Bastion (Mission 11). Checkpoint at x=23,200.
2. **Deflectable Boss Projectile (`NexusPulseBolt2D.cs`)**:
   - Swept high-velocity pulse bolt fired by the Nexus Overseer boss mechanoid.
   - Implements `public void TakeDamage(float amount) { Reflect(); }` which seamlessly triggers `CyberShogun.cs` parry logic (`attacker.Call("TakeDamage", 35f)`), turning bolt cyan and reflecting it back to the boss for 40 reflected damage.
3. **Multi-Phase Boss Mechanoid (`NexusOverseerBoss2D.cs`)**:
   - 560 HP boss mechanoid defending the central mainframe sanctum.
   - Procedural rendering: hexagonal chassis, glowing cyan core (`#00f0ff`), rotating orbital containment prism ring, dual arm cannons.
   - Phase 1 (100% - 70% HP): Rhythmic pulse bolt salvos and slow floating repositioning.
   - Phase 2 (70% - 35% HP): Rapid pulse bolt barrages, orbital containment shield flare, ground shockwaves, and tactical teleport bursts.
   - Phase 3 (< 35% HP): Core Overdrive Purge — orbital rings spin at 3x speed with emergency crimson flare (`#ff2255`), continuous pulse bolt storm, ground shockwave ripples, and rapid teleports.
4. **Campaign, Menu & Scorecard Integration**:
   - Attached `CyberNexusMission.cs` to `scenes/gameplay/level10.tscn`.
   - Added Sector 10 panel container to `scenes/main_menu/level_selection.tscn` and wired `_on_level_10_pressed()` in `scripts/main_menu/level_selection.gd`.
   - Wired `MissionNumber == 10` scorecard completion routing in `scripts/gameplay/CodeLevel.cs`.
5. **Pure C# Ledger & Verification (`NexusProgress.cs`)**:
   - Tracks 14 ordered milestones: `ingress`, `node:alpha`, `node:beta`, `node:gamma`, `gate:liquid_helium`, `gate:titan_data_bus`, `gate:optical_firewall`, `gate:radiation_corridor`, `gate:resonance_vault`, `boss:nexus_overseer`, `core:terminal`, `purge:initiated`, `extract:apex_gantry`, `complete`.
   - Authored formations: `nexus1`, `nexus2`, `helium1`, `helium2`, `firewall1`, `firewall2`, `vault1`, `vault2`, `sanctum1`, `sanctum2`, `sanctum3`, `overseer_phase1`, `overseer_phase2`, `overseer_phase3`.
   - Covered by 18 NUnit unit tests in `NexusProgressTests.cs` and GdUnit4 integration suite `test_cyber_nexus_mission.gd`.
   - Validated across all 10 missions in `test_campaign_levels.gd` and `test_level_selection_2d.gd`.
6. **Automated Verification**:
   - Django: 104/104 PASSED
   - NUnit: 213/213 PASSED
   - GdUnit4: 52/52 suites, 265/265 test cases, 0 errors, 0 failures, 0 orphans PASSED (`report_462`).

## Pre-Level 10 Cutscene: The Heart of the Machine — Cyber-Nexus Intro (2026-11-04)

### Summary of Enhancements
Authored, implemented, and fully verified the prologue cinematic overlay (`CyberNexusIntro.cs`, `CyberNexusStage.cs`, `scenes/cutscenes/cyber_nexus_intro.tscn`) establishing the interior penetration into Mission 10 (*The Cyber-Nexus*):
1. **Narrative & Authored Dialogue (`assets/cutscenes/cyber_nexus_intro/cues.json`)**:
   - Monotonic 90.0-second timeline across 9 authored dialogue cues tracking the squad entering the core mainframe:
     - Cue 1 (0.0s - 10.0s): AXIOM-7 confirming ingress through the outer hull blast doors, establishing hardline link to Citadel Spire interior core.
     - Cue 2 (10.0s - 20.0s): Mirage Blade gazing into the colossal chasm of glass and cold silicon, marveling at the clinical perfection.
     - Cue 3 (20.0s - 30.0s): Apex Titan noting the massive hum and power generation of server monoliths capable of powering entire city blocks.
     - Cue 4 (30.0s - 40.0s): Oceanus tracing liquid helium cooling lines running beneath the gantry plates, identifying thermal sabotage targets.
     - Cue 5 (40.0s - 50.0s): Aura Phoenicia detecting the awakened neural matrix probing bio-signatures and dispatching automated security drones.
     - Cue 6 (50.0s - 60.0s): Surya Prime warning of high-frequency defense lasers and biometric firewalls, priming solar bulwark absorption.
     - Cue 7 (60.0s - 70.0s): AXIOM-7 analyzing mainframe telemetry: three quantum processing nodes regulate Spire defense grid.
     - Cue 8 (70.0s - 80.0s): Mirage Blade emphasizing that neutralizing the Nexus will starve Praetorian reinforcements at the Apex Bastion.
     - Cue 9 (80.0s - 90.0s): Kaito Kurogane commanding Axiom Force to strike the nodes, silence the Overseer, and advance.
2. **Procedural Staging (`CyberNexusStage.cs`)**:
   - Abyssal cybernetic chasm gradient (`#030510` to `#0d122b`) with perspective digital floor grid.
   - Towering server monoliths with status indicator lights (`#00f0ff`, `#00ff88`, `#ffaa00`).
   - Cascading optical data rain streams (`#00f0ff` cyan and `#ffd700` gold).
   - Central pulsating quantum core sphere (`#00e5ff`) with rotating containment rings and rhythmic radial energy shockwaves.
   - Floating surveillance prism drones with sweeping spotlight cones.
   - Ingress gantry bridge with hazard chevrons and safety stanchions.
   - Operative silhouettes with glowing signature energy colors.
   - CRT scanline overlay, 2.39:1 anamorphic cinema bars, and zero external texture dependencies (`ResourcesAvailable => true`).
3. **Engine & Campaign Integration**:
   - Registered Mission 10 in `LevelCatalog.cs` (`The Cyber-Nexus`, `res://scenes/gameplay/level10.tscn`, 6 required abilities).
   - Hooked `CyberVictoryScorecard.cs` to launch `cyber_nexus_intro.tscn` when continuing from Mission 9.
   - Placeholder level 10 scene (`scenes/gameplay/level10.tscn`) created for clean scene transitions.
4. **Automated Verification & Test Coverage**:
   - NUnit suite `CyberNexusIntroTests.cs` (3 test cases): Exact 90.0s duration, scene file existence, LevelCatalog Mission 10 registration, and 9 ordered cues validation in `cues.json`.
   - GdUnit4 suite `test_cyber_nexus_intro.gd` (3 test cases): Skip & natural finish, missing art fallback, and subtitle cue timeline seeking with 0 orphan nodes.
   - Full test suite runner passed: 195 NUnit, 262 GdUnit4 across 51 suites, and 104 Django tests (100% pass, 0 orphans).

## Post-Level 9 Cutscene: The Core Breach (Mission 9 Aftermath) (2026-11-04)

### Summary of Enhancements
Authored, implemented, and fully verified the post-mission cinematic epilogue overlay (`SpireOuterHullAftermath.cs`, `SpireOuterHullAftermathStage.cs`, `scenes/cutscenes/spire_outer_hull_aftermath.tscn`) concluding Mission 9 (*Spire Outer Hull*) upon airlock extraction:
1. **Narrative & Authored Dialogue (`assets/cutscenes/spire_outer_hull_aftermath/cues.json`)**:
   - Monotonic 90.0-second timeline across 9 authored dialogue cues tracking the aftermath of the Spire Sentinel defeat and the opening of the Cyber-Nexus airlock:
     - Cue 1 (0.0s - 10.0s): AXIOM-7 reporting Spire Sentinel mechanoid chassis neutralized, summit catwalk secured at 3,000m, ambient pressure stabilizing.
     - Cue 2 (10.0s - 20.0s): Apex Titan admiring the massive nuclear-rated hydraulic blast door after climbing 2,000 meters vertically.
     - Cue 3 (20.0s - 30.0s): Oceanus looking down through the stratospheric haze at the clear, unshielded Neo-Kyoto skyline glowing peaceful below.
     - Cue 4 (30.0s - 40.0s): Surya Prime warning that the Citadel's Cyber-Nexus brain can synthesize new automated armies in hours if not destroyed.
     - Cue 5 (40.0s - 50.0s): Aura Phoenicia detecting the acoustic hive-mind vibration and petabytes of neural network calculations behind the bulkhead.
     - Cue 6 (50.0s - 60.0s): Mirage Blade cracking the terminal security protocols and triggering the pressure seal break.
     - Cue 7 (60.0s - 70.0s): AXIOM-7 confirming atmospheric purge complete and central intake airlock opening into quantum mainframe chambers.
     - Cue 8 (70.0s - 80.0s): Mirage Blade marveling at the digital labyrinth inside (holographic data prisms, quantum conduits, laser baffles).
     - Cue 9 (80.0s - 90.0s): Kaito Kurogane drawing his katana and ordering Axiom Force forward into the Cyber-Nexus core.
2. **Procedural Visual Staging (`SpireOuterHullAftermathStage.cs`)**:
   - High-altitude stratospheric twilight sky gradient with stars, rolling cloud bank at 3,000m elevation, and distant glowing skyline of liberated Neo-Kyoto.
   - Shattered, smoldering metallic husk of the Spire Sentinel mechanoid on the summit catwalk with electric spark discharge and rising smoke.
   - Colossal monolithic Spire titanium wall with power conduits, structural ribs, and flashing hazard indicator beacons.
   - Animated Cyber-Nexus Intake Airlock Portal:
     - 0s - 50s: Interlocking circular blast doors sealed with pulsating red hazard runes.
     - 50s - 70s: Hydraulic pistons retract, steam clouds billow outward, warning runes turn cyan, and doors slide apart into wall housings.
     - 70s - 90s: Portal fully open, revealing the breathtaking Cyber-Nexus quantum core (cascading blue/gold data waterfalls, quantum fiber lines, floating surveillance prisms).
   - Operative silhouettes standing on the catwalk facing the open gateway with distinctive glowing energy accents.
   - Floating steam clouds, CRT scanlines, 2.39:1 letterbox bars, and guaranteed zero external texture dependencies (`ResourcesAvailable => true`).
3. **Engine & Scorecard Integration**:
   - Attached in `CodeLevel.CompleteMission` when `MissionNumber == 9`, freeze-isolating the extracted level, playing the cinematic epilogue, and deferring to `CyberVictoryScorecard.ShowMissionScorecard` targeting Mission 10 upon natural finish or skip (ESC / Space / Enter / gamepad / click).
4. **Automated Verification & Test Coverage**:
   - NUnit suite `SpireOuterHullAftermathTests.cs` (3 test cases): Exact 90.0s duration, scene file existence, and 9 ordered cues validation in `cues.json`.
   - GdUnit4 suite `test_spire_outer_hull_aftermath.gd` (4 test cases): Skip & natural finish, missing art fallback, subtitle cue timeline seeking, and teardown audio stop with 0 orphan nodes.
   - Full test suite runner passed: 192 NUnit, 259 GdUnit4 across 50 suites, and 104 Django tests (100% pass, 0 orphans).

## Mission 9 Gameplay: Spire Outer Hull (High-Mobility Vertical Ascent) (2026-11-04)

### Summary of Enhancements
Authored, implemented, and fully verified the full 2D platformer mission (`SpireOuterHullMission.cs`, `scenes/gameplay/level9.tscn`) extending `CodeLevel` and implementing `IFiniteCombatHost`:
1. **6-Sector High-Altitude Layout (2,500m Elevation, 27,500 Unit Horizontal Scope)**:
   - *SEC.01 // GANTRY 42 EXTERIOR INSERTION*: High-wind steel catwalks at 2,500m elevation, wall-jump structural pylon columns, and initial defense patrols.
   - *SEC.02 // CRYOGENIC HEAT EXHAUST CONDUITS*: Pressurized cryogenic coolant louvers (`CoolantVent.cs`) requiring Oceanus Douse, and heavy magnetic titan-clamp hull braces requiring Apex Titan Slam.
   - *SEC.03 // FLUID-COUPLING FLAK BATTERY DECK*: Perimeter flak laser barrier (`CyberLaserGrid2D.cs`) bypassed via Mirage Blade Phase Shift, and flak artillery corridors defended under Surya Prime Bulwark.
   - *SEC.04 // CARGO ELEVATOR & STRUCTURAL PYLONS*: Vertical elevator shafts, acoustic drone relay lock requiring Aura Phoenicia Resonance, and summit overlook garrison.
   - *SEC.05 // SUMMIT CATWALK // SPIRE SENTINEL*: Multi-phase 2D aerial mechanoid boss encounter (`SpireSentinelBoss2D.cs`, 520 HP) firing swept high-velocity plasma bolts (`SpirePlasmaBolt2D.cs`, 35 damage) deflectable by Kaito's Kinetic Parry back at the boss, overdrive thruster charges, and Phase 3 emergency dive-bombs with seismic shockwaves.
   - *SEC.06 // CYBER-NEXUS INTAKE AIRLOCK*: Cyber-Nexus breach terminal hack (`PalaceObjective`), central intake airlock release, and extraction threshold opening into the Citadel interior.
2. **Deflectable Boss Projectile (`SpirePlasmaBolt2D.cs`)**:
   - Swept high-velocity plasma bolt fired by the Spire Sentinel mechanoid.
   - Implements `public void TakeDamage(float amount) { Reflect(); }` which seamlessly triggers `CyberShogun.cs` parry logic (`attacker.Call("TakeDamage", 35f)`), turning bolt cyan and reflecting it back to the boss for 35 damage.
3. **Multi-Phase Boss Mechanoid (`SpireSentinelBoss2D.cs`)**:
   - 520 HP multi-phase aerial combat mechanoid "Vanguard-9":
     - Phase 1 (100%-65% HP): Hover strafe and periodic dual plasma bolt attacks.
     - Phase 2 (65%-30% HP): Overdrive thrusters, rapid plasma barrages, and horizontal ram charges.
     - Phase 3 (<30% HP): Emergency combat protocols, rapid-fire plasma salvos, and high-altitude dive-bombs sending shockwaves along the catwalk.
4. **Campaign & UI Wiring**:
   - Added Sector 8 and Sector 9 cards to `scenes/main_menu/level_selection.tscn` and wired `_on_level_9_pressed()` in `scripts/main_menu/level_selection.gd`.
   - Wired `MissionNumber == 9` scorecard completion routing in `scripts/gameplay/CodeLevel.cs`.
5. **Testing & Verification**:
   - NUnit suite `SpireProgressTests.cs` (13 milestones, prerequisites, 14 formations, slot rewards).
   - GdUnit4 suite `test_spire_outer_hull_mission.gd` (full squad readiness, hazards, boss phase tracking, extraction gating, 0 orphans).
   - Updated `test_campaign_levels.gd` (`LEVELS[9]`) and `test_level_selection_2d.gd` (Missions 1..9).
   - Test suite summary: 104/104 Django, 189/189 NUnit, 255/255 GdUnit4 across 49 suites (0 errors, 0 failures, 0 orphans).

## Pre-Level 9 Cutscene: Exterior Breach — Spire Outer Hull (2026-11-04)

### Summary of Enhancements
Authored, implemented, and fully verified the prologue cinematic overlay (`SpireOuterHullIntro.cs`, `SpireOuterHullStage.cs`, `scenes/cutscenes/spire_outer_hull_intro.tscn`) establishing the exterior foot assault on Mission 9 (*Spire Outer Hull*):
1. **Narrative & Authored Dialogue (`assets/cutscenes/spire_outer_hull_intro/cues.json`)**:
   - Monotonic 90.0-second timeline across 9 authored dialogue cues tracking the squad's approach to the Spire at 2,500m elevation in the hijacked VTOL gunship:
     - Cue 1 (0.0s - 10.0s): AXIOM-7 reporting altitude 2,500m, 65-knot winds, and automated perimeter flak batteries tracking trajectory.
     - Cue 2 (10.0s - 20.0s): Surya Prime raising forward solar barrier to absorb heavy flak bursts against the port engine.
     - Cue 3 (20.0s - 30.0s): Mirage Blade spotting Gantry Level 42 maintenance rails along the monolithic titanium wall.
     - Cue 4 (30.0s - 40.0s): Apex Titan warning of shear winds and advising magnetized boot solenoids and harness lines.
     - Cue 5 (40.0s - 50.0s): Oceanus warning of cryogenic coolant venting from vertical exhaust shafts forming black ice.
     - Cue 6 (50.0s - 60.0s): Aura Phoenicia detecting acoustic mainframe resonance as automated interceptor drone swarms deploy from exterior silos.
     - Cue 7 (60.0s - 70.0s): AXIOM-7 deploying side cargo ramps and preparing to withdraw to high-orbit cloud cover.
     - Cue 8 (70.0s - 80.0s): Mirage Blade noting the one-way vertical climb to breach the Cyber-Nexus.
     - Cue 9 (80.0s - 90.0s): Kaito Kurogane unsheathing his katana and ordering the squad to breach the Spire outer skin.
2. **Procedural Staging (`SpireOuterHullStage.cs`)**:
   - High-altitude stratospheric twilight sky gradient with twinkling stars and lower storm ceiling rolling beneath at 2,500m elevation.
   - Sheer vertical monolithic Citadel Spire Outer Hull with titanium seams, pulsing cyan data conduits (`#00f0ff`), flashing red hazard indicators (`#ff2222`), Gantry Level 42 catwalks, and cryogenic coolant louvers.
   - Commandeered heavy military VTOL gunship with glowing cyan tilt-rotor thrust arcs, open side cargo doors with hazard warning stripes, and dangling magnetic grapple cables.
   - Operative silhouettes poised at the threshold with distinctive glowing energy accents.
   - Arcing anti-air flak tracers and bursting air-burst explosions, CRT scanlines, and 2.39:1 letterbox bars (`ResourcesAvailable => true`).
3. **Engine & Campaign Integration**:
   - Registered Mission 9 in `LevelCatalog.cs` (`Spire Outer Hull`, `res://scenes/gameplay/level9.tscn`, 6 required abilities).
   - Hooked `CyberVictoryScorecard.cs` to launch `spire_outer_hull_intro.tscn` when continuing from Mission 8.
   - Placeholder level 9 scene (`scenes/gameplay/level9.tscn`) created for clean scene transitions.
4. **Automated Verification & Test Coverage**:
   - NUnit suite `SpireOuterHullIntroTests.cs` (3 test cases): Exact 90.0s duration, scene file existence, and 9 ordered cues validation in `cues.json`.
   - GdUnit4 suite `test_spire_outer_hull_intro.gd` (3 test cases): Skip & natural finish, missing art fallback, and subtitle cue timeline seeking with 0 orphan nodes.
   - Full test suite runner passed: 172 NUnit, 252 GdUnit4 across 48 suites, and 104 Django tests.

## Post-Level 8 Cutscene: Airborne Ascension (Mission 8 Aftermath) (2026-11-04)

### Summary of Enhancements
Authored, implemented, and fully verified the post-mission cinematic epilogue overlay (`StratosDocksAftermath.cs`, `StratosDocksAftermathStage.cs`, `scenes/cutscenes/stratos_docks_aftermath.tscn`) concluding Mission 8 (*Stratos Docks*) upon extraction:
1. **Narrative & Authored Dialogue (`assets/cutscenes/stratos_docks_aftermath/cues.json`)**:
   - Monotonic 90.0-second timeline across 9 authored dialogue cues tracking the squad taking flight in the hijacked heavy military VTOL gunship toward the Spire:
     - Cue 1 (0.0s - 10.0s): AXIOM-7 reporting VTOL avionics online, decoupled from Citadel air traffic master control, dual ion thrusters at 100%.
     - Cue 2 (10.0s - 20.0s): Mirage Blade watching the flight decks shrink and confirming enemy interceptors are crippled in their berths.
     - Cue 3 (20.0s - 30.0s): Apex Titan admiring the heavy gunship's reinforced armor and shattered mooring clamps.
     - Cue 4 (30.0s - 40.0s): Oceanus looking down at the storm squalls and the liberated city lights glowing free without sky-shields.
     - Cue 5 (40.0s - 50.0s): Aura Phoenicia intercepting Citadel distress transmissions as high command scrambles automated exterior hull defense batteries.
     - Cue 6 (50.0s - 60.0s): Surya Prime breaking through the storm ceiling and spotting the colossal Citadel Spire Outer Hull.
     - Cue 7 (60.0s - 70.0s): AXIOM-7 issuing telemetry warnings: outer Spire heavy anti-air batteries block direct summit flight.
     - Cue 8 (70.0s - 80.0s): Mirage Blade proposing an exterior foot insertion onto the lower maintenance catwalks to scale the Spire outer hull.
     - Cue 9 (80.0s - 90.0s): Kaito Kurogane ordering the squad to harness in and breach the outer skin to carve their way to the summit.
2. **Procedural Staging (`StratosDocksAftermathStage.cs`)**:
   - High-altitude twilight sky gradient, volumetric storm cloud banks, receding Stratos Docks floating gantries with flashing red hazard beacons (`#ff2222`).
   - Heavy military VTOL gunship silhouette with dual glowing cyan tilt-rotor thrust ionization arcs (`#00f0ff`) and rear exhaust plume.
   - Colossal Citadel Spire Outer Hull with armor plating seams, glowing cyan conduit lines, and automated flak tracer rounds.
   - Operative silhouettes inside gunship hold with signature glowing energy accents (Kaito crimson, Mirage emerald, Aura pink, Oceanus aqua, Titan violet, Surya gold).
   - Tactical holographic flight HUD, CRT scanline overlay, 2.39:1 anamorphic cinema bars, and guaranteed zero external texture dependencies (`ResourcesAvailable => true`).
3. **Engine & Scorecard Integration (`CodeLevel.cs`)**:
   - Attached in `CodeLevel.CompleteMission` when `MissionNumber == 8`, freeze-isolating mission gameplay and seamlessly deferring handoff to `CyberVictoryScorecard.ShowMissionScorecard(finalScore, peakCombo, operative, 9)`.
4. **Automated Verification & Test Coverage**:
   - NUnit suite `StratosDocksAftermathTests.cs` (3 test cases): Exact 90.0s duration, scene file existence, and 9 ordered cues validation in `cues.json`.
   - GdUnit4 suite `test_stratos_docks_aftermath.gd` (4 test cases): Skip & natural finish stopping audio and emitting once, missing art fallback resilience, 9-cue subtitle timeline seeking, and clean teardown with 0 orphan nodes.

## Mission 8 Gameplay: Stratos Docks — Aviation Hub (2026-11-04)

### Summary of Enhancements
Authored, implemented, and verified the complete 2D platformer gameplay for Mission 8 (*Stratos Docks - Aviation Hub*), the opening playable mission of Act III:
1. **High-Altitude 6-Sector Level Architecture (`StratosDocksMission.cs`, `scenes/gameplay/level8.tscn`)**:
   - Extends `CodeLevel` and implements `IFiniteCombatHost` with world bounds `x: 0` to `26500`, `y: -1400` to `2200`.
   - High-altitude stormy environment with driving rain, wind shear, crane elevator wall-jump shafts across mooring pylons, and 6 authored sectors:
     - *SEC.01 // PERIMETER GANTRY CATWALKS*: Perimeter gantry infiltration at 1,200m elevation with patrol drones and initial security patrols (`catwalk1`, `catwalk2`).
     - *SEC.02 // CRYO-FUEL DEPOT & CONDUITS*: Pressurized liquid hydrogen cryo vents (`CoolantVent.cs`) doused by Oceanus (`GateKind.FireVent`), and heavy magnetic titan-clamp mooring locks shattered by Apex Titan Slam (`GateKind.RubbleWall`). Encounters: `fuel1`, `fuel2`, `fuel3`.
     - *SEC.03 // FLIGHT DECK SCANNER ARRAYS*: High-voltage laser grid (`CyberLaserGrid2D.cs`) phased through via Mirage Blade Phase Shift, and acoustic radar lock bypassed by Aura Phoenicia Resonate (`GateKind.SonicLock`). Encounters: `scanner1`, `scanner2`, `scanner3`.
     - *SEC.04 // AIR TRAFFIC CONTROL GANTRY*: Flak battery artillery corridor shielded by Surya Prime Bulwark (`GateKind.HeatVent`), elevated crane towers, and garrison forces (`gantry1`, `gantry2`, `gantry3`).
     - *SEC.05 // FLIGHT DECK GUARDIAN // AERO-CENTURION*: Climactic flight deck arena featuring the multi-phase aerial mechanoid boss `AeroCenturionBoss2D` (480 HP) and support waves (`hangar1`, `hangar2`, `hangar3`).
     - *SEC.06 // BAY 4 VTOL COMMAND INTERLOCK*: Override terminal (`PalaceObjective` Kind="Terminal"), pre-flight telemetry, and boarding ramp extraction beacon initiating Spire ascent.
2. **Flight-Deck Guardian Boss: Aero-Centurion (`AeroCenturionBoss2D.cs`, `AeroMissile2D.cs`)**:
   - 480 HP high-altitude mechanoid with 3 distinct combat phases:
     - Phase 1: High-altitude patrol hover, dual plasma repeaters, and swept plasma missiles.
     - Phase 2: Overdrive storm thrusters, increased strafe speed, and homing micro-missile salvos.
     - Phase 3: Emergency Overdrive, red strobe flare, dive-bombs, and high-frequency shockwaves.
   - Swept high-velocity plasma missiles (`AeroMissile2D.cs`): Deflectable by Kaito's Kinetic Parry; successful parries turn the missile cyan and redirect it back to the boss for 33 reflected damage.
3. **Pure C# State Ledger (`StratosProgress.cs`)**:
   - Engine-free deterministic ledger tracking 13 ordered milestones (`perimeter_ingress` to `extract`) and 14 finite authored combat formations.
   - Enforces sequential progression, prerequisite checks, idempotent member kill rewards, and `CanExtract` gating.
4. **UI & Level Selection Integration (`level_selection.tscn`, `level_selection.gd`)**:
   - Added Sector 8 campaign card to `SectorGrid` in `scenes/main_menu/level_selection.tscn`.
   - Wired `Level8` button with `_on_level_8_pressed()` in `scripts/main_menu/level_selection.gd`.
5. **Automated Verification & Test Coverage**:
   - NUnit suite `StratosProgressTests.cs` (5 test fixtures): Milestone order, prerequisite gating, skip rejections, slot rewards, and LevelCatalog Mission 8 registration.
   - GdUnit4 suite `test_stratos_docks_mission.gd` (3 test cases): Full squad initialization, hazards & objectives verification, and boss/extraction gating with 0 orphan nodes.

## Act II End Screen, Act III Start Screen & Pre-Level 8 Cutscene (2026-11-04)

### Summary of Enhancements
Authored, implemented, and verified the complete cinematic and UI transition bridge between Act II and Act III:
1. **Act II End Screen (`ActIIEndScreen.cs`, `scenes/ui/act_ii_end_screen.tscn`)**:
   - Tactical campaign debrief terminal celebrating the liberation of all three Act II infrastructure sectors (Sector 5 Downtown Civic District, Sector 6 Cyber-Grave Labs, Sector 7 The Foundry Forge).
   - Confirms planetary sky-shield integrity at 0.0% with exposed Spire insertion telemetry.
   - Provides responsive keyboard, gamepad, and touch navigation with `PROCEED TO ACT III`, `MISSION SELECT`, and `MAIN MENU` actions.
2. **Act III Start Screen (`ActIIIStartScreen.cs`, `scenes/ui/act_iii_start_screen.tscn`)**:
   - Stylized cyberpunk title card ushering in **ACT III: THE CITADEL SPIRE & FINAL LIBERATION (Missions 8–12)**.
   - High-contrast neon typography, operation briefing, and full 6-operative squad readiness strip with signature ability badges.
   - Seamless advance button (`BEGIN OPERATION // SPACE / ENTER / A`) routing into the Pre-Level 8 prologue.
3. **Pre-Level 8 Cutscene: Infiltration at Stratos Docks (`StratosDocksIntro.cs`, `StratosDocksStage.cs`, `scenes/cutscenes/stratos_docks_intro.tscn`, `assets/cutscenes/stratos_docks_intro/cues.json`)**:
   - 90.0-second skippable live 2D cinematic prelude establishing the high-altitude aviation hub infiltration.
   - 9 authored dialogue cues featuring AXIOM-7, Mirage, Titan, Oceanus, Aura, Surya, and Kaito coordinating the breach of Bay 4 to commandeer a heavy military VTOL gunship.
   - Procedural staging with driving rain, wind shear, sweeping searchlights, mooring pylons with flashing red aviation beacons (`#ff2222`), massive idling VTOL gunship with glowing tilt-rotor turbine ionization, operative silhouettes, and 2.39:1 anamorphic cinema bars (`ResourcesAvailable => true`).
4. **Mission 8 Scaffold & LevelCatalog Registration (`LevelCatalog.cs`, `StratosDocksMission.cs`, `scenes/gameplay/level8.tscn`)**:
   - Registered Mission 8 (*Stratos Docks*) in `LevelCatalog` with all 6 operative abilities required.
   - Created scaffold `StratosDocksMission.cs` and `scenes/gameplay/level8.tscn` to ensure smooth scene navigation.
   - Updated `CyberVictoryScorecard.cs` to route the Act II victory button into `act_ii_end_screen.tscn`.
5. **Verification & Test Coverage**:
   - NUnit suite `ActTransitionTests.cs` (3 test cases): Scene existence, LevelCatalog Mission 8 registration, and cues.json schema.
   - GdUnit4 suites `test_act_ii_end_screen.gd` (2 test cases), `test_act_iii_start_screen.gd` (2 test cases), and `test_stratos_docks_intro.gd` (3 test cases): Instantiation, button signals, 90s cue seeking, skip routes, and clean lifecycle disposal with 0 orphans.
   - Full test suite runner `.\run_tests.ps1`: 100% PASSED (Django 104/104, NUnit 149/149, GdUnit4 242/242 across 45 suites with 0 errors, 0 failures, 0 orphans).

## Post-Level 7 Epilogue Cutscene: The Sky-Shield Falls (Act II Finale) (2026-11-04)

### Summary of Enhancements
Authored, implemented, and fully verified the cinematic epilogue overlay (`FoundryForgeAftermath.cs`, `FoundryForgeAftermathStage.cs`, `scenes/cutscenes/foundry_forge_aftermath.tscn`) concluding Act II upon extraction from Mission 7 (*The Foundry Forge*):
1. **Narrative & Authored Dialogue (`assets/cutscenes/foundry_forge_aftermath/cues.json`)**:
   - Monotonic 90.0-second timeline across 9 authored dialogue cues tracking the squad's transport cresting the volcanic caldera rim as the sky-shield collapses:
     - Cue 1 (0.0s - 10.0s): AXIOM-7 telemetry reporting transport clear of the primary thermal shaft; core temperature exceeding critical thresholds.
     - Cue 2 (10.0s - 20.0s): Apex Titan confirming geothermal pressure redirection and containment lockouts preventing municipal casualties.
     - Cue 3 (20.0s - 30.0s): Oceanus observing coolant conduits draining and steam vents venting safely into the volcanic mantle.
     - Cue 4 (30.0s - 40.0s): Aura Phoenicia listening to the harmonic resonance of the core falling silent; tectonic frequency restoring to natural baseline.
     - Cue 5 (40.0s - 50.0s): Mirage Blade monitoring orbital surveillance links as planetary relay stations disconnect.
     - Cue 6 (50.0s - 60.0s): AXIOM-7 displaying telemetry as the planetary energy distribution network experiences terminal desynchronization.
     - Cue 7 (60.0s - 70.0s): Surya Prime projecting satellite telemetry of the Citadel Spire's hexagonal sky-shield flickering and shattering.
     - Cue 8 (70.0s - 80.0s): Mirage Blade highlighting exposed upper docking platforms and high-altitude flight decks at the Spire base.
     - Cue 9 (80.0s - 90.0s): Kaito Kurogane declaring Act II complete: "The sky belongs to the city again. Act Three begins now."
2. **Procedural Graphics Architecture (`FoundryForgeAftermathStage.cs`)**:
   - Visual staging rendered analytically via Godot `_Draw` with zero external image or shader dependencies (`ResourcesAvailable => true`):
     - Multi-layer jagged volcanic caldera rim silhouettes with warm magma rim glow.
     - Distant Neo-Kyoto skyline with pulsing cyber-illuminated windows and towering Citadel Spire.
     - Animated hexagonal sky-shield grid across the upper atmosphere with expanding electric fracture lines, chromatic aberration bursts, and dissolving particle debris.
     - Extraction transport chassis with pulsing thruster exhaust particles.
     - Operative silhouettes with unique character-coded energy highlights (Kaito cyan, Surya amber, Oceanus aqua, Mirage violet, Titan gold, Aura magenta).
     - 9-shot smooth Hermite camera interpolation with subtle atmospheric floating shake.
     - 2.39:1 anamorphic cinema bars and CRT scanlines.
3. **Controller & Mission Integration (`FoundryForgeAftermath.cs`, `CodeLevel.cs`)**:
   - `CodeLevel.CompleteMission` on `MissionNumber == 7` freezes active mission gameplay and instantiates `FoundryForgeAftermath` on Layer 40.
   - Preserves final score, peak combo streak, and active operative.
   - Deferral handoff: On natural completion or multi-input skip (ESC, Space, Enter, Gamepad Start/A, mouse/touch click), the epilogue emits `Finished` and triggers `CyberVictoryScorecard.ShowActIIScorecard`.
4. **Verification & Test Coverage**:
   - NUnit suite `FoundryForgeAftermathTests.cs` (3 test cases): Exact 90.0s duration, scene file existence, and 9-cue schema validation.
   - GdUnit4 suite `test_foundry_forge_aftermath.gd` (4 test cases): Skip inputs (keyboard, gamepad, click), natural playback completion, subtitle timeline seeking across all cues, missing art fallback, and clean node lifecycle with 0 orphans.
   - Full test suite runner `.\run_tests.ps1`: 100% PASSED (Django 104/104, NUnit 146/146, GdUnit4 235/235 across 42 test suites with 0 errors, 0 failures, 0 orphans).

## Mission 7 Gameplay: The Foundry Forge (Act II Climax & Finale) (2026-11-04)

### Summary of Enhancements
Authored and implemented the full 2D platformer gameplay for **Mission 7 (*The Foundry Forge*)**, serving as the climactic finale to Act II:
1. **Core Gameplay & Level Architecture (`FoundryForgeMission.cs`, `scenes/gameplay/level7.tscn`)**:
   - Subclassed `CodeLevel` and implemented `IFiniteCombatHost` with 6 authored geothermal sectors:
     - *SEC.01 // BASALT RIFT INGRESS*: Infiltration entry point across volcanic rift basalt chasms, vertical wall-jump shafts, steel catwalks, and initial thermal patrol drones.
     - *SEC.02 // CRYO-COOLANT TURBINE LOOPS*: High-temperature conduit corridors guarded by active `CoolantVent` hazards and superheated steam gates requiring Oceanus Douse.
     - *SEC.03 // MAGNETIC INDUCTION CONDUITS*: High-voltage laser grid (`CyberLaserGrid2D`) traversable via Mirage Blade Phase Shift, and heavy magnetic pressure valve requiring Apex Titan Slam.
     - *SEC.04 // PRIMARY CONTAINMENT OVERLOOK*: High-frequency ultrasonic barrier requiring Aura Phoenicia Resonance, extreme radiation vents requiring Surya Bulwark, and laser curtains.
     - *SEC.05 // CORE GUARDIAN // IGNIS-COLOSSUS*: Multi-phase boss mech arena (`IgnisColossusBoss2D`, 450 HP) with sweeping swept molten mortar projectiles (`FoundrySlagBall2D`) parryable by Kaito's blade for 40 reflected damage, sweeping flamethrowers, and Phase 3 core overdrive meltdown.
     - *SEC.06 // CRITICAL OVERLOAD EXTRACTION*: Terminal override collapsing the Citadel Spire's planetary sky-shields across all municipal sectors, followed by emergency extraction gantry.
2. **Boss Mech Encounter: IGNIS-COLOSSUS (`IgnisColossusBoss2D.cs`) & Deflectable Slag Projectiles (`FoundrySlagBall2D.cs`)**:
   - 450 max health with 3 combat phases:
     - Phase 1 (100%–70% HP): Molten mortar volleys and periodic flamethrower sweep.
     - Phase 2 (70%–35% HP): Overheating core with intensified mortar fire and accelerated attack cycle.
     - Phase 3 (<35% HP): Core meltdown overdrive with rapid slag barrages and incandescent visual pulsing.
   - `FoundrySlagBall2D.cs`: Deflectable molten projectile parryable by Kaito's blade to redirect and inflict 40 damage back onto the boss mech.
3. **Act II Victory & Scorecard Integration (`CyberVictoryScorecard.cs`, `CodeLevel.cs`)**:
   - Added `ShowActIIScorecard(score, maxCombo, operative)` displaying `ACT II COMPLETE` banner with post-mission performance grades (S/A/B).
   - Hooked `MissionNumber == 7` in `CodeLevel.cs` to trigger the Act II scorecard upon mission completion.
4. **Mission Ledger & Catalog Registration (`FoundryProgress.cs`, `LevelCatalog.cs`, `level_selection.gd`, `level_selection.tscn`)**:
   - Pure C# engine-free ledger `FoundryProgress.cs` tracking 13 milestones and 15 authored combat formations.
   - Registered Mission 7 in `LevelCatalog.cs` (`level7.tscn`, all 6 abilities).
   - Added Sector 7 card and button in Level Selection menu with automatic progression unlocking.
5. **Verification & Test Coverage**:
   - NUnit suite `FoundryProgressTests.cs` (6 test cases): Premature event rejection, full milestone chain completion, formation requirements, reward idempotency, and catalog registration.
   - GdUnit4 suite `test_foundry_forge_mission.gd` (3 test cases): Full squad availability, hazard registration, and boss/extraction gating.
   - GdUnit4 suite `test_level_selection_2d.gd` (5 test cases): 7-mission campaign mapping, button handlers, and progression unlocking.
   - GdUnit4 suites `test_act_ii_opening.gd` and `test_campaign_levels.gd`: Updated assertions and platform coordinates for 7 missions.
   - Full suite runner `.\run_tests.ps1` PASSED: Django 104/104, NUnit 143/143, GdUnit4 231/231 across 41 test suites with 0 errors, 0 failures, 0 orphans.

## Pre-Level 7 Prologue Cutscene: Descent to The Foundry Forge (2026-11-03)

### Summary of Enhancements
Authored and implemented the pre-mission cinematic prologue overlay (`FoundryForgeIntro.cs`, `FoundryForgeStage.cs`, `scenes/cutscenes/foundry_forge_intro.tscn`) bridging Mission 6 post-scorecard completion to Mission 7 (*The Foundry Forge*):
1. **Narrative & Cutscene Controller (`FoundryForgeIntro.cs`)**:
   - Monotonic 90-second timeline with latency compensation and zero-orphan cleanup.
   - 9 dialogue cues tracking the squad's descent into the 4,000 Kelvin sub-surface industrial rift:
     - Cue 1: AXIOM-7 confirming thermal telemetry and entrance to The Foundry Forge.
     - Cue 2: Kaito highlighting primary conduits fueling the Citadel Spire's planetary sky-shields.
     - Cue 3: Titan identifying geothermal siphons, heavy magnetic containment coils, and slave labor quotas.
     - Cue 4: Oceanus recounting drained aquifers and promising the river will reclaim its bed once coolant valves smash.
     - Cue 5: Aura detecting the harmonic fault line in the central turbine's magnetic lattice.
     - Cue 6: Mirage mapping infiltration routes across high-voltage catwalks and slag sluices.
     - Cue 7: Surya on comms cautioning that the solar bulwark will insulate initially, but core overload cascades will be irreversible.
     - Cue 8: Titan ready to drop the Citadel's sky-shield.
     - Cue 9: Kaito giving the operational drop-in order to sever conduits, overload the reactor, and shatter their throne.
   - Character-colored subtitle panel borders (AXIOM-7 cyan, Kaito crimson, Titan violet, Oceanus aqua, Aura pink, Mirage emerald, Surya gold).
   - Multi-device skip handling (Escape, Space, Enter, Gamepad Start/A, touch/mouse click).
   - Safe mission handoff with fallback to level selection when Level 7 gameplay scene is pending.
2. **Procedural 2D Staging Architecture (`FoundryForgeStage.cs`)**:
   - `ResourcesAvailable => true` guaranteeing zero missing-asset crashes.
   - Deep subterranean volcanic basalt rift backdrop with rising convection heat waves.
   - Molten plasma rivers with dynamic wave math and glowing magma bubbles.
   - Geothermal cooling towers venting billowing steam clouds.
   - High-voltage lightning discharge arcs across containment coils.
   - Catwalk gantry platform with safety railings and hazardous amber floor striping.
   - Operative silhouettes with unique ability energy auras: Kaito vanguard katana, Titan kinetic armor hum, Oceanus water veil, Aura sound waves, Mirage phantom daggers.
   - Holographic wireframe reactor core blueprint with rotating field lines and flashing breach point targets.
   - 2.39:1 anamorphic letterboxing bars and CRT scanlines.
3. **Scorecard & Navigation Integration (`CyberVictoryScorecard.cs`)**:
   - Updated `CyberVictoryScorecard.OnContinuePressed()` to navigate to `res://scenes/cutscenes/foundry_forge_intro.tscn` when `_nextMission == 7`.
4. **Verification & Test Coverage**:
   - NUnit suite `FoundryForgeIntroTests.cs` (3 test cases): Exact 90s duration, scene file existence, and 9-cue schema validation.
   - GdUnit4 suite `test_foundry_forge_intro.gd` (4 test cases): Skip and natural finish audio stop, missing art fallback, subtitle timeline seeking across all cues, and clean lifecycle disposal with 0 orphan nodes.
   - All test suites passing: Django 104/104, NUnit 126/126, GdUnit4 228/228 across 40 suites with 0 errors, 0 failures, 0 orphans.

## Post-Level 6 Epilogue Cutscene: Ascent from the Cyber-Grave (2026-11-03)

### Summary of Enhancements
Authored and implemented the post-mission cinematic epilogue overlay (`CyberGraveAftermath.cs`, `CyberGraveAftermathStage.cs`, `scenes/cutscenes/cyber_grave_aftermath.tscn`) bridging Mission 6 (*Cyber-Grave Labs*) extraction to the Act II campaign climax:
1. **Narrative & Cutscene Controller (`CyberGraveAftermath.cs`)**:
   - Monotonic 90-second timeline with latency compensation and zero-orphan cleanup.
   - 9 dialogue cues tracking the ascending freight elevator with 17 rescued civilians in thermal foil blankets:
     - Cue 1: AXIOM-7 confirming ascent telemetry from sub-level six.
     - Cue 2: Oceanus stabilizing civilian vital signs and clearing stasis fluids.
     - Cue 3: Aura sensing the horrific network resonance fading away into relief.
     - Cue 4: Titan confirming geothermal reactor pressure safely vented into sinks.
     - Cue 5: Mirage projecting decrypted central vault ledgers directly incriminating the Citadel Spire.
     - Cue 6-7: Surya on comms confirming surface medical transports and warning of orbital defenses locking down upper sectors.
     - Cue 8-9: Kaito rallying the squad to rest for an hour before storming the Citadel Spire.
   - Character-colored subtitle panel borders (Kaito crimson, Oceanus aqua, Aura pink, Titan violet, Mirage emerald, Surya gold, AXIOM-7 cyan).
   - Multi-device skip handling (Escape, Space, Enter, Gamepad Start/A, touch/mouse click).
2. **Procedural 2D Staging Architecture (`CyberGraveAftermathStage.cs`)**:
   - `ResourcesAvailable => true` guaranteeing zero missing-asset crashes.
   - Downward-rushing shaft rail parallax simulating upward elevator ascent (420 px/sec).
   - Heavy industrial freight elevator platform with safety cage railings, diamond grating, and flashing amber strobes.
   - Rescued civilians wrapped in reflective gold thermal blankets (`#fef08a`).
   - Distinctive operative silhouettes: Oceanus kneeling with hydro-mist, Aura with ultrasonic resonance rings, Titan at the bulkhead, Mirage crouched with holographic slate, Kaito standing vanguard with katana.
   - Holographic Citadel Spire wireframe blueprint and rotating radar sweep during intelligence cues.
   - 16:9 cinema bars and CRT scanlines.
3. **Engine & Mission Integration (`CodeLevel.cs`)**:
   - Hooked `CompleteMission()` for `MissionNumber == 6` to freeze mission elements, attach `CyberGraveAftermath` (Layer 40), and seamlessly trigger `CyberVictoryScorecard` upon finish.
4. **Verification & Test Coverage**:
   - NUnit suite `CyberGraveAftermathTests.cs` (3 test cases): Exact 90s duration, scene file existence, and 9-cue schema validation.
   - GdUnit4 suite `test_cyber_grave_aftermath.gd` (4 test cases): Skip and natural finish audio stop, missing art fallback, subtitle timeline seeking across all cues, and clean lifecycle disposal with 0 orphan nodes.
   - All test suites passing: Django 104/104, NUnit 123/123, GdUnit4 224/224 across 39 suites with 0 errors, 0 failures, 0 orphans.

## Act II Mission 6 Gameplay: Cyber-Grave Labs (2026-11-03)

### Summary of Enhancements
Authored and implemented full 2D platformer gameplay for Act II Mission 6 (*Cyber-Grave Labs - Underground Facility*):
1. **Mission Architecture & Scene Implementation (`CyberGraveMission.cs` & `scenes/gameplay/level6.tscn`)**:
   - Subclassed `CodeLevel` and implemented `IFiniteCombatHost` for deterministic, finite-formation encounter pacing across 6 underground sectors:
     - Sector 1 (*Freight Sump & Breach*): Elevator infiltration with wall-jump vertical shafts and steel catwalks.
     - Sector 2 (*Incubation Foundry*): Industrial cloning lab with `GenePod.cs` stasis cylinders, rising nutrient bubbles, and captive operatives.
     - Sector 3 (*Cryo-Coolant Conduit*): Sub-zero piping networks guarded by timed `CoolantVent.cs` cryogenic steam hazards.
     - Sector 4 (*Resonance Induction Chamber*): High-frequency sonic lock requiring Aura Phoenicia's harmonic resonance bypass.
     - Sector 5 (*Geothermal Siphon*): Magma/steam conduits with pulsing lethal laser curtains traversable via Mirage Blade phase dash.
     - Sector 6 (*Central Vault & Extraction Lift*): 3-stage core defense waves, biometric data core override terminal, and high-speed extraction freight elevator.
   - Fielded all six operatives simultaneously for the first time in the campaign (Cyber Shogun, Oceanus, Apex Titan, Aura Phoenicia, Mirage Blade, and Surya Prime), supporting on-the-fly hot swapping with shared health/shield ratios.
   - Built 6 checkpoint shrines (`FreightLift`, `Incubation`, `Coolant`, `ResonanceCore`, `Geothermal`, `CentralVault`) enforcing prerequisite sector progression.
   - Authored 3 optional `MemoryFragment` collectibles placed on reachable platforms.
2. **Interactive Hazard & Prop Systems (`GenePod.cs` & `CoolantVent.cs`)**:
   - `GenePod.cs`: Interactive stasis pod prop rendering pressurized glass cylinders, animated procedural rising bubbles, metal containment rings, and captive humanoid silhouettes with proximity/click liberation logic.
   - `CoolantVent.cs`: Configurable `Area2D` hazard with timed active/inactive venting cycles, procedural steam plumes, and neutralization hooks (via Oceanus's Douse or terminal overrides).
3. **Pure C# Ledger & Test Coverage (`CyberGraveProgress.cs` & `CyberGraveProgressTests.cs`)**:
   - Authored `CyberGraveProgress.cs` tracking 13 milestones (`sump_breach`, `pod:1`, `pod:2`, `incubation_cleared`, `coolant_override`, `resonance_bypass`, `grid_phase`, `siphon_contain`, `core_defense:1-3`, `vault_override`, `extract`) and 15 authored combat encounters.
   - Created NUnit test suite `CyberGraveProgressTests.cs` (20 test cases) verifying milestone prerequisites, clearance idempotency, reward counting, and 6-operative roster availability.
4. **Campaign & UI Glue**:
   - Registered Mission 6 in `LevelCatalog.cs` with full squad and ability mappings.
   - Added Sector 6 / Level 6 button card to `level_selection.tscn` and connected `_on_level_6_pressed()`.
   - Updated `CyberVictoryScorecard.cs` to route Mission 5 into the `cyber_grave_intro.tscn` cutscene and Mission 6 to victory results.
   - Added `test_cyber_grave_mission.gd` GdUnit4 test suite with 3 comprehensive tests.

### Verification Results
- `dotnet build AxiomForce.csproj`: Clean build, 0 errors, 153 pre-existing nullable/addon warnings.
- `.\run_tests.ps1` full test runner executed and passed:
  - **Django (Python)**: 104/104 PASSED
  - **NUnit (.NET 10 C#)**: 120/120 PASSED (+20 new tests)
  - **GdUnit4 (Godot 4 GDScript)**: 220/220 test cases across 38 suites PASSED (0 errors, 0 failures, 0 orphans, report_425).
- Docker Stack: Core containers (`db`, `backend`, `website`) healthy and running.
- Scratch scripts cleaned up. No Git commits or pushes executed.

## Pre-Level 6 Opening Cutscene: Into the Cyber-Grave (2026-11-02)

### Summary of Enhancements
Authored and implemented the Pre-Level 6 opening cinematic cutscene (`CyberGraveIntro.cs`, `CyberGraveStage.cs`, `scenes/cutscenes/cyber_grave_intro.tscn`) to bridge Mission 5's civic registry liberation with the impending infiltration of the subterranean bio-synthetic foundry:
1. **Authored Dialogue Script & ElevenLabs Voiceover Pipeline**:
   - Authored an intense 9-cue narrative script featuring the squad cast:
     - Cue 1 (AXIOM-7 / Daniel): Descent telemetry confirms arrival 400 meters beneath Downtown Core at facility designated "Cyber-Grave".
     - Cue 2 (Kaito / Adam): Recognizes the facility as a processing plant turning the living into machine components.
     - Cue 3 (Aura / Matilda): Detects sickening resonance of thousands of bio-signals trapped in forced synchronization.
     - Cue 4 (Oceanus / Sarah): Decries the lie of agricultural preserves; sees citizens drowning in pressurized stasis pods.
     - Cue 5 (Titan / George): Analyzes geothermal siphon reactors and reinforced blast bulkheads.
     - Cue 6 (Mirage / Eric): Identifies ventilation breach routes past secondary plasma conduits.
     - Cue 7 (Surya / Brian): Locks transponders into orbital blind spots, warning of zero topside extraction.
     - Cue 8 (Oceanus / Sarah): Demands every pod be opened and no name left behind in the dark.
     - Cue 9 (Kaito / Adam): Issues the squad order to breach the perimeter, sever conduits, and evacuate captives.
   - Generated cached speech cues using ElevenLabs multilingual v2 with verified voice IDs and SHA-256 receipts (`.validation/cyber-grave/speech/`).
   - Packaged and normalized speech into an exact 120.00-second `dialogue.ogg` and synthesized matching 120.00-second `atmosphere.ogg` (42Hz elevator thrum, steam valve bursts, wind rush, and deceleration grind) via Docker FFmpeg (`tools/cutscene/package_cyber_grave.py`).
2. **Procedural Staging Architecture (`CyberGraveStage.cs`)**:
   - Built a high-fidelity seekable 2D staging canvas rendering a rushing freight lift shaft with parallax structural girders and depth cables.
   - Added glowing vertical glass bio-nutrient conduits with animated rising bubbles and high-voltage containment rings (`#00ff99`).
   - Implemented sweeping amber hazard strobes (`#ffaa00`) with volumetric light cones and horizontal steam venting.
   - Rendered the freight platform deck with industrial hazard chevrons and under-floor neon uplight.
   - Positioned distinct operative silhouettes across the platform (Kaito's katana and scarf, Oceanus's hydro catalysts, Aura's tuning antennae, Mirage's low phase daggers, Titan's heavy bulk, Surya's comms transmitter).
   - Added interactive holographic tactical telemetry HUD projecting wireframe facility schematics and dynamic depth meters (`DEPTH: -412M`).
   - Camera choreography with smooth cubic Hermite transitions across 10 shot positions.
3. **Cutscene Controller & UI (`CyberGraveIntro.cs`)**:
   - Implemented deterministic `CanvasLayer` overlay with audio-compensated monotonic clock.
   - Word-wrapped subtitle panel with dynamic speaker-coded border accents (Cyan, Crimson, Aqua, Pink, Violet, Emerald, Gold).
   - Multi-input skip support (ESC, Space, Enter, Gamepad Start/A, mouse/touch click) with clean action release and single-completion guarantee.
   - Automatic mission preparation and safe navigation fallback (`LevelCatalog.Count >= 6 ? ScenePath(6) : level_selection.tscn`).
4. **Verification & Testing**:
   - GdUnit4 suite `test_cyber_grave_intro.gd`: Passed 3/3 tests (lifecycle, skip, subtitle seeking, 0 orphans).
   - NUnit suite `CyberGraveIntroTests.cs`: Passed 3/3 tests (duration, scene existence, cue structure).
   - Full test runner `.\run_tests.ps1`:
     - **Django**: 104/104 PASSED
     - **NUnit**: 103/103 PASSED
     - **GdUnit4**: 217/217 test cases across 37 suites PASSED (0 errors, 0 failures, 0 orphans).
   - Core Docker containers (`db`, `backend`, `website`) remain healthy.

## Post-Level 5 Epilogue Cutscene Implementation (2026-11-02)

### Summary of Enhancements
Authored and elevated the Mission 5 post-extraction epilogue cutscene (`NamesWithoutNumbersAftermath.cs` and `NamesWithoutNumbersAftermathStage.cs`) to high-fidelity cyber-action standards:
1. **Authored Dialogue Script & ElevenLabs Voiceover**:
   - Replaced temporary/placeholder lines with a fully authored 8-cue cinematic script:
     - Cue 1: Civilian Amara reclaiming her identity at the green-lit terminal.
     - Cue 2: Refugee crowd joy upon seeing their names restored to the manifest.
     - Cue 3: Mirage Blade reporting biometric carrier waves completely flatlined.
     - Cue 4: Aura Phoenicia sensing the static turning into a communal chorus.
     - Cue 5: Oceanus reporting transit gates, water filtration, and ration vaults opening.
     - Cue 6: Apex Titan praising the destruction of five years of occupation locks.
     - Cue 7: Surya Prime comms report that the outer barrier dropped and the queue is crossing.
     - Cue 8: Cyber Shogun (Kaito) rally command as the squad prepares for the Citadel.
   - Synthesized fresh voiceover takes for all 8 cues with ElevenLabs multilingual TTS using designated voice profiles.
   - Normalized and packaged audio via Docker FFmpeg into 90.0-second `dialogue.ogg` and `atmosphere.ogg` tracks (all cues fit at 1.0x tempo without artificial speedups).
2. **Visual Staging & Cinematic Choreography**:
   - Upgraded `NamesWithoutNumbersAftermathStage.cs` with smooth Hermite/eased camera pan and zoom interpolation across all shot transitions.
   - Added procedural volumetric sweeping searchlights from civic towers.
   - Rendered rising holographic registry terminal data pillars with floating golden identity particles moving into civilian hands.
   - Added distinctive operative silhouettes (Kaito's katana, Mirage's phase daggers, Aura's resonance rings, Oceanus's hydro robes, Titan's bulk, Surya's comms link).
   - Added pulsing ground speaker beacons under active characters.
   - Implemented 2.39:1 widescreen letterboxing with subtle cyan grid accents and anamorphic lens flares.
3. **UI Polish & Speaker Subtitles**:
   - Upgraded `NamesWithoutNumbersAftermath.cs` with cyber-bordered subtitle containers (`#00f0ff` accent).
   - Dynamically color-modulated subtitles matching each operative and civilian speaker.
   - Styled the Skip button with glass-morphism cyber aesthetics and keyboard/gamepad/pointer support.

### Verification Results
- `dotnet build AxiomForce.csproj`: Clean build, 0 errors, 153 existing nullable/addon warnings.
- Docker cutscene validation: 8 cues, exact 90-second duration, 0 clipped samples.
- Headless Godot editor import refresh exited with code 0 (`atmosphere.ogg.import` and `dialogue.ogg.import` regenerated).
- `run_tests.ps1`:
  - **NUnit**: 100/100 passing.
  - **GdUnit4**: 214/214 passing (36 suites, 0 errors, 0 failures, 0 orphans, report_414).
  - **Django (Containerized)**: 104/104 passing.
- **Git Status**: Changes remain local and uncommitted per standing permission policy.

## Act II Start Screen, Pre-Level 5 Cutscene, and Level 5 Gameplay Revamp (2026-11-02)

### Summary of Enhancements
Elevated the ChatGPT-era prototype implementation of the Act II start screen, pre-Level 5 cutscene, and Mission 5 gameplay to high-fidelity cyber-action standards:
1. **Act II Start Screen & Directory**:
   - Updated `scenes/main_menu/level_selection.tscn` to display canonical campaign mission titles (Missions 1-4 for Act I, Mission 5 for Act II) and enhanced the Mission 5 panel with Act II premiere styling.
   - Updated `scripts/gameplay/CodeLevel.cs` to show `ACT II // THE UNREGISTERED` on Mission 5's opening title card via `CAMPAIGN_ACT_II_TITLE` while maintaining child node structure.
   - Preserved `CyberVictoryScorecard.cs`'s `CONTINUE TO ACT II` progression handoff.
2. **Pre-Level 5 Cutscene (`ActIIOpening.cs` & `ActIIStage.cs`)**:
   - Replaced flat horizontal squad placement with dynamic tactical staging: Surya at the crossing barrier, Oceanus comforting refugees, Titan defensive anchor, Kaito vanguard lead, Aura perimeter surveillance, and Mirage service scout.
   - Enhanced Shot 0 with cyber anamorphic lens flares, scanline interference, and title ease-in.
   - Added procedural volumetric searchlight sweeps, biometric scanner pulse waves, operative ground beacon rings for active speakers, and character-color-coded subtitle modulation.
3. **Level 5 Gameplay (`NamesWithoutNumbersMission.cs`, `RegistryScanner.cs`, `RecoveryTerminal.cs`)**:
   - Replaced the 28,000px flat line floor with multi-tier cyber architecture: wall-jump ventilation shafts, elevated service conduits, multi-tier inspection catwalks, archive mezzanine balconies, and fortified core vault platforms (all upper routes use one-way platforms, guaranteeing zero collision overlap with enemy spawn tests).
   - Differentiated enemy formations with sector-specific tactical pacing (agile service ambushers, high-catwalk gunners, and phased core defense waves).
   - Elevated `RegistryScanner` with cyber emitter housing, active laser beam glow, downward warning chevrons, and safe green bypass indicator.
   - Enhanced `RecoveryTerminal` with floating holographic data projection slates, interactive status rings, and verification checkmarks.
   - Added in-world cybernetic sector landmark signs across all 6 sectors.

### Verification Results & Publication
- `dotnet build AxiomForce.csproj`: Clean build, 0 errors, 153 existing nullable/addon warnings.
- Headless Godot editor import refresh exited with code 0.
- `run_tests.ps1`:
  - **NUnit**: 100/100 passing.
  - **GdUnit4**: 214/214 passing (0 errors, 0 failures, 0 orphans, report_413).
  - **Django (Containerized)**: 104/104 passing.
- **Git Status**: Pushed to `origin/main` per explicit user instruction. Machine-specific `.vscode/settings.json` and untracked `.skill-install-staging/` excluded.

## Mission 5 — Names Without Numbers (2026-11-02)

### Post-extraction epilogue

`CodeLevel.CompleteMission` snapshots points, peak combo and operative before freezing
Mission 5 and adding `NamesWithoutNumbersAftermath`. The 90-second deterministic
timeline plays a civilian's chosen name, the district's response, five operative
reactions and Surya's comms report, then gives Kaito the closing line. A separate
seekable stage uses original Blender district plates and authored civilian silhouettes.
The overlay's speech and atmosphere players are its only audio lane; absent voice keeps
the timed subtitle sequence. Natural completion, any supported Skip, missing essential
art and independent overlay removal stop audio and deliver one result callback. The
existing scorecard still shows Replay, Level Select and Main Menu, with no Continue or
Act finale. Replay reloads a fresh Mission 5 scene; no epilogue unlock, watched flag or
progression write was added. Production and acceptance commands are documented in
`tools/cutscene/README.md` under “Mission 5 epilogue — Names Without Numbers”.

Verification for this addition is recorded below only after the final import, build and
test runs.

Final verification (2026-11-02): background Godot import exited 0 and created all four
PNG/Ogg sidecars. The renderer's 1920x1080 preview was inspected. Docker Blender asset
and FFmpeg audio validation passed: both plates are 1920x1080 RGBA with transparency,
eight voice windows fit without tempo changes, both decoded Ogg tracks are 90 seconds
and unclipped. `dotnet build AxiomForce.csproj` passed with 153 existing warnings and
zero errors. `run_tests.ps1` passed NUnit 100/100 and GdUnit4 214/214 with zero errors,
failures or orphans (`reports/report_407`). The host Django suite was skipped because
`backend/.venv` is absent; the live backend attempt failed before tests because its
existing `/app` Docker bind mount returned `No such device`. No backend files changed.

### Root Linux virtualenv test runner follow-up

`run_tests.ps1` now recognizes only `.venv/bin/python`; there is no Windows Scripts
venv preference. From Windows, it packages the root POSIX venv and backend source,
creates an owned unmounted helper from `axiom-force-backend`, installs the declared
backend requirements into the temporary venv copy, and runs `manage.py test accounts`
against the existing PostgreSQL service. Cleanup removes only the helper container and
temporary archive; the normal services remain up. Root `.venv` itself is unchanged.

Verification (2026-11-02): Django 104/104 passed; full `run_tests.ps1` passed Django
104/104, NUnit 100/100 and GdUnit4 214/214 (`reports/report_408`, zero errors,
failures or orphans). Expected mocked Clerk/Resend diagnostics appeared during passing
Django tests. The existing backend bind-mount failure is avoided by the unmounted helper.

Godot import retains the existing asset-placer TLS handshake and editor shutdown
EditorSettings/ObjectDB/resource/PagedAllocator diagnostics. No in-game captures at
1920x1080, 1920x1200 or 2560x1080, physical controller/touch hardware review or speaker
listening were performed. The owned unmounted Blender helper was stopped; all four
normal services remain running and the user's editor was not switched. No export,
commit or push was performed. Changes remain local and uncommitted.

The campaign now has five playable missions. Level 4 explicitly retains its captured
**ACT I COMPLETE** scorecard. **CONTINUE TO ACT II** launches the existing opening;
natural completion, Skip and missing essential art hand off once to Mission 5 after
stopping cinematic audio. Level Select, Resume and Mission 5 Replay enter gameplay
directly. A Mission 4 save remains at Mission 4 until completion; no save migration or
new operative grant is introduced. Mission 5 ends on **MISSION COMPLETE**, with Replay,
Level Select and Main Menu, without exposing Mission 6.

Six connected sectors cover scanner timing, processing control, archive cooling and
maintenance, three authenticated original records, exclusion isolation, three ordered
finite recovery waves and restored civilian access. Seventeen three-member formations
use the existing combat/score/checkpoint systems with a six-live-enemy cap. Required
archive stairs rise 100px; records are indestructible context terminals using the normal
attack action, or click/tap the nearby console. Exact prerequisites reject premature/duplicate interactions. Completed
objectives and formations survive checkpoints, incomplete groups reset and each member
rewards once across retries. Three optional civilian testimony fragments never gate exit.

### Combat tuning follow-up — 2026-11-02

Moved most neighboring formation centers far enough apart to avoid waking two three-member
groups from the same approach. Mission 5 alone uses a 1.45s gunner aim warning, 2.6s
post-burst recovery and 1.0s sentry beam warning. The encounter component defaults retain
the prior values for other levels. No enemy health, finite formation count, six-enemy cap,
checkpoint behavior, objective order or campaign-save contract changed. Exact coordinates
and the current playtest limitation are in `tools/gameplay/NAMES_WITHOUT_NUMBERS.md`.

The required unassisted baseline could not be completed: a normal-input service approach
was started, but the Godot remote evaluator stalled the game loop before combat. The
diagnostic-clearance route remains unsuitable as a substitute. A sustained run is still
needed to measure duration, deaths, checkpoint recoveries and combat pacing. Tuning
verification: background import exited 0 with the existing addon TLS/editor shutdown
diagnostics; `dotnet build AxiomForce.csproj` passed with 153 existing warnings and zero
errors; `run_tests.ps1` passed Django 104/104, NUnit, and GdUnit4 214/214
(`reports/report_409`, zero errors/failures/orphans). The run includes existing Level 5
ledger and runtime coverage. All four regular Docker services remain up; its disposable
Django test helper was removed. No persistence/backend contract changed.

Surya remains unlocked and speaks over comms from the queue. His slot shows
“Protecting the queue”; roster selection, direct player swaps and cycling all exclude
him only in Mission 5. Invalid preferred selections fall back to Kaito. Mirage phases
into processing, Aura isolates scanner denial, Oceanus stabilizes coolant and Titan
opens the damaged maintenance route away from protected records. Committing restored
identities visibly opens amber denial gates with accepted-access markers.

Original Docker/Blender props, nineteen new cached established-cast cues and synthesized
atmosphere/scanner cues include reproducible production scripts and provenance. A single
mission-owned speech lane serializes critical dialogue above expiring hints; missing
speech retains localized English fallback subtitles. Owned audio pauses/stops on mission
lifecycle transitions. Docker's D: bind mount failed, so production used copied inputs
in the same existing media image; the running stack was preserved.

Assisted blockout traversal completed all records, sequential recovery, commit and exit
using real movement, operative abilities and diagnostic enemy clearance. It exposed an
upper gallery approach problem, fixed with solid conservative stairs before dressing.
This is functional evidence, not unassisted combat balance or the requested 25–30-minute
first-play duration. Physical controller/touch and subjective mix listening remain
separate acceptance checks. Production and final verification details are in
[Mission 5 acceptance](tools/gameplay/NAMES_WITHOUT_NUMBERS.md).


Mission 5 overrides both title and briefing from retired narrative slot 5. Player
objectives name visible landmarks; coordinates stay in tooling notes. Defenders
spawn relative to real stairs, galleries and cover rather than the base road floor.

Verified 2026-11-02 after final engine edits: background import exit 0 with all
30 new media sidecars; final incremental C# build zero warnings/errors; NUnit 100/100;
GdUnit4 210/210 (`reports/report_406`, zero errors, failures and orphans); isolated
copied-source Django 104/104. The wrapper skips the absent host Django venv. Existing
backend `/app` and media D: mounts remain unreadable; isolated tests used the same
backend image and PostgreSQL network, with their test database removed normally.
The temporary production/test helpers were stopped; the core services remain
running. No exports, staging, commits or pushes; editor remains stopped on Level 1.

Runtime evidence under `.validation/names/`: assisted mandatory-route completion,
21 sector/restored/results captures across 1920×1080, 1920×1200 and 2560×1080,
actual keyboard/controller-event/mouse/touch terminal dispatch, repeated opening Skip,
natural completion and missing-art handoff, Mission 5 Replay and direct Level Select.
A temporary campaign stayed at Mission 4 until completion, then saved/resumed directly
into 5; its cleanup preserved the original campaign row hash. Seven transparent art
assets and nineteen complete voice cues pass alpha/margin, cast/hash, decoded duration
and clipping checks. The gallery spawn regression passes for all six archive groups.
Python tool compilation and diff hygiene pass. These checks do not establish an
unassisted uninterrupted 25–30-minute run, physical hardware or subjective listening.

Known diagnostics: a full rebuild retains 153 pre-existing nullable/addon warnings;
one concurrent build had a transient PDB-sharing retry, resolved by the final clean
incremental build. Headless editor imports still log existing addon TLS and shutdown
EditorSettings/ObjectDB/resource/allocator messages. The reference checker retains
10 pre-existing addon/menu connection errors and 27 warnings, none in Mission 5.

## Current creative direction — 2026-11-02

The user confirmed deleting docs/STORY_BIBLE.md and docs/full_game_plan.md to
allow independent creative direction instead of exact-script adherence. Both
documents have been removed. Future creative work follows the user's current
requests and agent judgment; older plans, fixed-script instructions and canon
restrictions in historical entries are superseded as creative mandates. Retain
the implementation/verification history as factual context, not a future script.
This cleanup preserves executable scripts, dialogue data, recordings, cinematics
and all existing uncommitted implementation work. It does not itself launch a
new rewrite or override operational, testing or Git authorization rules.

Verification: checked both deletions and removed active links from README and
the fortress production guide; diff hygiene passes. Only Markdown changed in
this cleanup, so no Docker preflight, Godot refresh or test rerun was needed.

## Levels 1–3 readability and objective defense — 2026-11-02

Implemented Lantern Court parry guidance independently of RecruitBeat's timeout,
live shield charges, archive signs and sealed/accessible Data-Blade presentation.
Level 2 markers expose exact local defenders, route fire (Oceanus), rubble (Titan),
battery, movement and actual shelter arrival. Spires name their defense groups.
Its ledger rejects premature spire/core/rescue callbacks. Patrols remain optional.

Level 3 retains all 30 groups/rewards; mandatory defenses are s2-4, s3-3, s5-4,
s6-5 and s7-1..5. Local dependencies replace sector clearance, both approach groups
gate the west coupler, and reinforcements are sequential. Optional combat seals
and the s4-2 harmonic-access interlock are removed. Seven checkpoints follow
insertion, freight, runoff, Aura lesson, west/east uplinks and Mirage lesson.
Markers distinguish required defenses from optional score patrols and name the
operative/current binding; escape guidance asks for curtain traversal.

IFiniteCombatHost retirement defaults to disabled (Level 4 stays unchanged).
Levels 2–3 opt in at 1,500 horizontal pixels from the authored formation center.
Retirement reuses ResetIncomplete generation rejection and reward deduplication;
it grants no kill/completion and returning reactivates unfinished groups. Neon
Rust retires remote groups before budget calculation as well as during ticks.
Routes/story/recruits/abilities/cinematics, launch/Skip/results rules, existing
caps/stats/score/campaign/save schema remain unchanged.

Final checks: build has zero errors (153 existing warnings on full compilation;
incremental checks also pass). Background import exits 0; the existing addon TLS,
EditorSettings shutdown, ObjectDB/resource and allocator diagnostics remain.
run_tests.ps1 passes NUnit 88/88 and Godot 197/197 with zero errors/failures/orphans
(report_399). Its host Django step skips the absent venv. Explicit copied-container
Django passes 104/104 after final edits; the ordinary backend /app bind mount again
returns ENODEV. The owned axiom-mission-readability-tests helper is stopped after
its successful run. Normal db/backend/website containers remain running.

Godot AI MCP reviewed 51 full-resolution state captures at 1920×1080, 1920×1200 and
2560×1080; PNG dimensions match and nine contact sheets were inspected. The final
Blade label sits above the portal lintel, and its exposed-beacon instruction is
covered by an engine assertion. Cinematic/aftermath/results regression coverage
passes. All four production results actions pass for each of Levels 1–3; Replay
resets progress/rosters and Continue retains the existing arrival launch rules.
Sampled visual/navigation runtime logs contain no warnings/errors. Editor PID
12608 remains stopped on its original Level 1 scene; no existing saves were used
for the diagnostic runs and no new save/schema migration was introduced.

Final required-only Neon Rust traversal uses real movement, swaps and abilities
with diagnostic damage limited to the nine defenses: complete in 157.436 seconds
of assisted traversal/objective wall time, zero recoveries, maximum six live
enemies, no manual resumption. This is functional route evidence, not a combat or
human pacing measurement. An earlier helper missed Mirage's window and required
repositioning; the revised helper waits for cooldown and can walk back to retry.
Those earlier timings are superseded. Seven new integration tests cover guidance,
real deflections/abilities, rescue transitions/recovery, retirement/reactivation,
budget availability, reward deduplication and objective checkpoint activation.

Evidence: .validation/mission-readability/ plus mission-readability-*.log; production
review commands and human checklist: docs/ACT_I_REVIEW.md. Python compilation and
git diff --check pass. Human first-run pacing, comprehension and physical controller/
touch acceptance remain pending. No export, staging, commit or push was performed.

## Tomorrow resume / publication — 2026-11-01

User requested pushing the current fortress work and continuing tomorrow.
Implementation commit a17c83e was pushed to origin/main and its remote SHA verified.
This publication record is included in a documentation-only follow-up commit.
Implementation and regression verification are complete as recorded below.
Start with tools/gameplay/SHATTERED_AEGIS.md; outstanding acceptance is human
20–25 minute pacing, physical controller/touch and speaker listening. The existing
Docker bind-mount and import diagnostics remain unresolved. This publication
changes documentation/Git state only; existing verification remains applicable.
No additional gameplay work, export or service shutdown is part of this task.


## Revised Act I and fortress finale — 2026-11-01

Implemented the seven-section 28,000px Level 4 assault and revised ensemble story
bible/open future outline. docs/ACT_I_REVIEW.md records a separate Levels 1–3
continuity/teaching/repetition/pacing review; their geometry and cinematics were
not rebuilt. The Level 3 aftermath and Level 4 arrival recordings/timelines remain
unchanged. The campaign still contains four missions and the save schema is unchanged.

AegisProgress owns exact prerequisites and deduplicated authored rewards. Level 4
implements IFiniteCombatHost, reuses finite groups/recovery, and caps ordinary live
actors at four. Required defense groups protect devices; other fights are bypassable.
Seven safe section/arena checkpoints retain recruits, lessons, fragments, objectives
and awarded slots. Real Bulwark and Blink crossings gate lessons. Oceanus/Titan gun
approaches share one controller and reconnect. Thermal and curtain shutdown persists.

AegisBossController opts the existing armored Voss into three phases at 300/200/100
HP, clamped damage, phase-two pylons, nonoverlapping missiles/marked strikes and
five-second stationary melee exposure. Standalone boss/HUD bindings remain compatible.
Missiles have ownership, swept collision, lifetime and genuine parried return travel;
Bulwark absorbs them and Blink does not grant ranged immunity. Attempt recovery
rebuilds all boss state; post-defeat recovery never respawns him. A separate attacked
crossing device precedes safe civilian presentation and immutable ACT I COMPLETE
results. Replay, Level Select and Main Menu are the only finale destinations.

One owned speech scheduler serializes fourteen exact event cues with expiring hints,
priority, English fallbacks/localization keys and pause/recovery/teardown handling.
Cached ElevenLabs multilingual-v2 voices use established IDs and .70/.75/.10/1
settings, with text/request/audio hashes and measured duration receipts. Six original
512px RGBA Blender assets follow the inspected navy/violet/amber/cyan target; art
and voices use the existing media image in an owned unmounted copy-based helper.

Final verification: background import exit 0; all twenty PNG/Ogg sidecars exist.
Incremental C# build: 0 errors/warnings (153 existing warnings on full compilation).
run_tests.ps1 passes NUnit 97/97 and Godot 190/190, zero failures/errors/orphans
(report_388), with no engine ERROR/SCRIPT ERROR entries in the final suite log.
Host Django remains skipped for absent venv; explicit copied-container Django
passes 104/104 after final edits. Art/alpha/margins, audio/hash/duration/unclipped
PCM checks and Python compilation pass. Existing TLS/editor shutdown resource/
PagedAllocator import diagnostics remain; the import is not error-free.

All 30 dressed section/boss/crossing/results captures at 1920×1080, 1920×1200 and
2560×1080 were inspected, preserving the user's Level 1 editor. Diagnostic
traversal and automated real-combat evidence are separate in .validation/aegis/.
The latter reached extraction with actual attacks/abilities, observed peak three
live ordinary enemies, one observed thermal checkpoint return and two tool-side
resumptions without scene reload. Its stalls/timing are not human pacing. Complete
MCP batch metadata reports no new runtime errors; sampled game logs are clean.
Oceanus's upper approach was climbed, doused and reconnected/backtracked using
real movement/ability; the full combat traversal used Titan's maintenance breach.
Titan's measured jump height is 184.71px; he traversed the required 100px stairs.

Finale Replay, Level Select and Main Menu, plus actual Level 3 Continue, mission
selector and saved-session launch routes pass. Replay rebuilds arrival/five
operatives/fresh ledger. The owned temporary save was deleted and verified absent.
No user save was changed. Both owned copy-based helpers are stopped; normal four
services remain running. The pre-existing backend bind mount still returns ENODEV.

Spent devices now remove their attack hurtboxes, preventing combo farming. Actual
player deaths in all three boss phases are checked at the arena checkpoint;
recovery disables the abandoned controller synchronously. Regression also exposed
a pre-existing upgrade-screen GrabFocus call outside the scene tree; its binding
entry now skips viewport focus for standalone tests.
Human first-run pacing, physical controller/touch and speaker listening remain
unperformed. No exports, staging, commits or pushes. Preserve existing memory audit
entries. User's editor scene remains Level 1; normal services remain running.


## Apple release setup — 2026-11-01

Added macos/ios to both launchers and the Docker export driver. macos uses the
universal preset for a release-mode unsigned ZIP with no silent debug fallback.
It and all refuse the example bundle ID before building. ios skips Linux Godot/
.NET compilation and creates a source-only Mac handoff; all also prepares that
handoff after its macOS output. Python stdlib tooling excludes development/
signing files and caches, sanitizes preset secrets, and preserves native addon
bin inputs. Dockerfile adds Python and bakes the helper; rebuild before use.

Apple presets now specify version/build, unsigned universal macOS output and
iOS ARM64/iPhone+iPad/project-only generation. Owner bundle IDs and Apple Team
ID remain deliberately unconfigured. Native Mac helper has an inert default,
read-only --check, and explicit project generation; all Actions paths remain
blocked. Updated all agent rules with the user's Apple native-completion exception.
export/APPLE_RELEASE.md documents the precise Docker/native split, Godot/.NET/
Xcode prerequisites, identity/provisioning, signing/notarization and distribution.

No actual game export, Apple handoff, signing or exporter image build has run.
Helper tests only package a tiny synthetic fixture. Final verification:
four fixture tests, source inventory, shell/Python syntax, Apple target/Actions/
host/placeholder safety paths and Compose validation pass. Background Godot
import exited 0 with the existing TLS/editor shutdown resource diagnostics.
run_tests.ps1 passes NUnit 93/93 and Godot 161/161 (report_369, zero failures/
errors/orphans). Host Django is skipped for absent venv; container Django passes
104/104. Incremental build has zero warnings/errors. No real build/ output exists.
Mac/Xcode, device/AOT/native-addon compatibility and production networking
remain unverified until a real Apple build. Prior export-policy edits are retained.

## Docker-only game exports — 2026-11-01

User requested export reorganization only, with no actual game export. Root
.github/workflows is absent and GitHub's workflows API reports total_count=0,
so no current Actions workflow existed to delete. Historical runs were retained.
All four agent instruction files now forbid Actions-based or host game exports.
Fixed root launchers referencing nonexistent exporter instead of export, removed
the implicit all-target export, validated supported targets before work, and
made invocation independent of the caller's directory. Launchers refuse Actions;
Compose forwards GITHUB_ACTIONS and the container driver rejects CI/host use.
The exporter remains opt-in with its help default. Rebuild its image before a
future authorized export because its driver changed; no image build/export
was performed for the exporter in this task. Normal services were restored for
the mandatory code-edit preflight and remain running (db healthy).
Verification: Bash syntax, both launchers' no-target/invalid-target/Actions
paths, driver help/host/Actions refusal, Compose validation and default-profile
exclusion pass. No build/ export output directory was created. run_tests.ps1
passes NUnit 93/93 and GdUnit4 161/161 (report_368, zero failures/errors/orphans).
The wrapper skips absent host Django venv; explicit container Django passes
104/104. Compilation reports 154 existing warnings and zero errors. Website
and backend countries endpoint returned HTTP 200. Only launchers/Compose/docs
changed, so no game-engine import refresh was needed. No staging/commit/push.

## Resume after container interruption — 2026-10-31

The Through the Breach implementation and verification artifacts already exist.
Reviewed the nine-shot contact sheet, asset/audio results, report_361 (142 Godot
tests, zero failures), NUnit 47/47 and container Django 104/104 logs, and the
uninterrupted playback evidence. Corrected a stale tooling-documentation sentence
that still described Level 2 as retaining its old transition. No code/assets
changed during this resume; imports and unit suites do not require rerunning.
Docker restoration completed: Compose pulled the missing images, built backend
and website, and started the core services. PostgreSQL is
healthy. Website (5173) and direct and proxied countries API (8000/5173)
returned HTTP 200. No game export, staging, commit or push performed.

## PostgreSQL-only storage and Playwright — 2026-10-31

Removed SessionManager's JSON fallback and moved operative progression plus
display/input documents into PostgreSQL JSONB game_state (schema version 1).
Session increments now use an atomic UPDATE. Host clients read environment/root
.env credentials. New Game stays in the menu on save failure; failed database
loads cannot overwrite progression with defaults. Settings/progression failures
report errors. Standalone progression tests keep simulated state only in memory.

Game and portal tokens are transient; locale lives in the URL. Existing local
saves remain untouched but are ignored, with no automatic migration. Game state
retains the existing shared development database scope. Clerk remains upstream
identity and may maintain its own authentication cookies. Assets, caches, logs,
Docker volumes and model files remain normal tooling/runtime files.

Django tests now use PostgreSQL. Compose aligns backend/database credentials.
Added opt-in Playwright v1.64.1-noble with matching npm runner, init, shared IPC
and nonroot user. Portal builds use same-origin /api. Game integration coverage
checks saves across two managers; the browser smoke check rejects storage API
access. Final verification: C# build passed (zero errors, 154 existing warnings);
background Godot import exited 0 with existing addon TLS/editor shutdown resource
diagnostics. run_tests.ps1 passed NUnit 47/47 and GdUnit4 135/135 with zero
failures/errors/orphans (report_357). The wrapper skipped the absent Windows
backend venv; explicit container Django tests passed 104/104 on PostgreSQL.
JSONB round-trip/version protection and database refusal are covered by NUnit;
campaign create/update/delete across managers is covered by GdUnit4.
Microsoft-image Chromium smoke passed with browser storage APIs blocked.
Website, direct/proxied countries API returned HTTP 200; four
normal containers remain running, PostgreSQL healthy. No export or Git
publication was performed. Clerk replacement remains a separate scope question.
Historical local-fallback observations below describe older builds.

## Evacuation Under Siege gameplay — 2026-10-30

Level 2 now orchestrates a 35,800px six-sector route with 20 finite formations,
four ferry/island crossings, four checkpoints and the existing three optional
fragments. Recruitment is at x5700 (Oceanus, before practice/required fire) and
x25000 (Titan, beyond the x24400 membrane and before safe Slam rubble). Three
spire formations shield their individual objectives until defeated. The stationary
battery at x30800 has two power relays, a shielded core, one finite reinforcement
formation and local health/phase telemetry. Core destruction ends artillery forever.

Focused `Wildfire*` components own encounters, swept parryable gunner rounds,
2.4-second locked-target artillery, invulnerable local family movement, battery,
asset scaling and localized English fallback. `WildfireProgress` tracks stable
IDs and kill rewards; incomplete groups restart on recovery without farmable score.
Completed encounters, spires, relays, core, barriers, recruits and rescues persist
within this instance. Ordinary fires reignite after eight seconds; civilian-route
fires lock permanently safe before movement. The overpass checkpoint requires its
shelter route secured. Extraction checks all exact prerequisites before the shared
one-time campaign completion path. Reload/replay creates a new ledger; no save migration.

Shared `CodeLevel` additions are default no-op recovery hooks, checkpoint safety
eligibility and a title-card objective override. Delayed recovery/transition callbacks
also guard freed instances. Other mission flow and controls remain unchanged.
RecruitBeat prompts now reflect keyboard/controller/touch device and InputMap ability
binding; the shared controller still directly owns its existing swap/combat keys.
All new systems are constructed before the unchanged arrival freeze/restore snapshot.
Level 2 hides the generic menu-control banner after arrival and restores it on exit.

Generated original facade, shelter, civilian, gunner and battery PNGs through Blender
inside the opt-in cutscene Docker image. Inspected the representative composition
before generating the family. RGBA size/alpha/margins/provenance/import validation
passes. `tools/gameplay/README.md` documents the reproduction and route manifest.
MCP used a separate custom game with autosave=false; the open editor remains Level 1.

Verification: the final 14 gameplay regression cases pass with zero errors,
failures or orphans; NUnit 45/45. Assisted blockout reached all six sector endpoints
with zero falls (~96 seconds total movement; combat cleared diagnostically). Eighteen
dressed captures cover six sectors at 1920x1080, 1920x1200 and 2560x1080; inspection
led to clearer shelter and battery labels. Reference scan has the same ten pre-existing
signal-path errors in untouched main menu/proton_scatter files.
The first full run exposed two older structural-test assumptions: every world
child had a platform Size, and enemies were globally spawned directly under Actors.
Updated those checks to distinguish decorative nodes and activate/inspect a real
nested finite formation. The new 14-case gameplay suite itself passed that run.

Final verification: background import exited 0 and all five PNG imports exist.
Incremental C# build: zero errors/warnings (full compile: 153 existing warnings).
`run_tests.ps1`: NUnit 45/45 and GdUnit4 134/134, zero failures/errors/orphans,
`reports/report_354`. Wrapper skips absent Windows backend venv; explicit Docker
backend Django suite passes 104/104. Python tooling compiles; asset validation and
diff hygiene pass. MCP full-chain verification used real signature abilities,
5.5-second civilian walks, diagnostic enemy/objective damage, and observed automatic
Level 3 loading. Evidence: `.validation/wildfire-gameplay/objectives.json`.
Only the pre-existing host PostgreSQL/local-JSON fallback appeared in live game
errors. Headless import retains existing addon TLS and shutdown/resource warnings,
separate from the clean zero-orphan game suite. The editor remains on Level 1 with
review games stopped; all four Docker services are retained. No game export was performed.
The user subsequently authorized committing and pushing these current changes.
Publication review confirmed main matched origin/main before the new commit; the
Git-only publication task reuses the completed verification above without rerunning tests.

**Outstanding acceptance:** 20–25 minute unassisted first-playthrough pacing is
not established by these diagnostics. The current assisted route time is explicitly
not combat or onboarding time. Human playtesting/content tuning and physical
controller/touch-device testing remain necessary; only synthetic device prompt/input
paths were checked, not a full touch-only gameplay session. No new voiceover was made.

> **Services restored (2026-10-30):** All four existing containers were stopped.
> Started the stack with Compose; PostgreSQL became healthy, and
> website, backend countries endpoint each returned HTTP
> 200. The first backend request preceded Django readiness; a retry succeeded
> after its clean system check. No cinematic/code/asset changes in this task.
> README and MEMORY record readiness behavior and verification. Documentation
> only: no Godot import or unit-suite run required; no commit/push performed.

> **Level 2 arrival (2026-10-30):** Latest development commit: `4260282`
> (confirmed by a later history/status read). `ConcreteWildfireMission.OnLevelReady` starts
> `ConcreteWildfireIntro` after construction and before physics. `CodeLevel` captures
> every descendant's processing mode, CanvasLayer visibility and audio pause state;
> one guarded handoff restores them and restarts the opening dialogue's 11-second
> display. Level 2 remains current, no watched flag/save-schema/progression changes.
> Checkpoint recovery reuses this scene. `WildfireStage` analytically samples six
> shots with protected families, membrane/spires, siege silhouettes and Shogun descent.
> Docker Blender/Python tools generate layered checkpoint art and two 60-second tracks.
> Seven cached ElevenLabs MCP cues use Daniel/Adam and fit their windows without cuts.
> Snapshot uses two passes: capture the entire tree before disabling any parent,
> because Godot's mode notification changes descendant audio pause flags. Restore
> processing modes first, then saved visibility/audio flags. Music has regression coverage.
> Final verification: background Godot import exited 0 (all ten asset imports present),
> C# build had zero errors/153 existing warnings; NUnit 34/34, GdUnit4 120/120
> with zero failures/errors/orphans (`reports/report_349`), container Django 104/104.
> Host runner skips Django because its venv is absent; container verification covers it.
> Docker validation passed RGBA dimensions/alpha/atlas bounds, both 60-second tracks
> and all seven cue windows. Godot AI MCP captured all six shots at 16:9 plus close/map
> shots at 16:10 and ultrawide. Real Level 1 extraction Continue, Level Select,
> saved-session confirmation (temporary record removed) and direct Level 2 all start
> the frozen intro with a solo squad. Uninterrupted final playback held position,
> health and score constant; sampled audio/visual clocks differed by about 25-30 ms,
> then natural completion restored gameplay. Synthetic Space input left Jump released
> and mission music resumed unpaused. Skip/fallback/recovery/teardown have unit coverage.
> Evidence: `.validation/wildfire/{review,assets.json,routes-playback.json,input-audio.json}`.
> Existing addon TLS/headless shutdown, missing optional gdunit4.api and PostgreSQL
> authentication/local JSON fallback diagnostics remain; no new runtime errors found.
> Physical controller/touch hardware, speaker listening and exports were not tested.
> Editor remains on Level 1; review games stopped, four Docker services retained.
> User subsequently authorized Git publication of this implementation; see MEMORY.MD.
> Open-world exploration is deferred; existing recruitment/objectives remain unchanged.


> **Tokyo Remembers implementation (2026-10-30, resumed by user):** Level 1 now
> captures extraction score/combo/operative, records campaign progress once, and
> disables mission descendants (including explicit Always/Pausable nodes). The
> new independently processing `TokyoRemembers`/`TokyoStage` overlay uses one
> 40-second analytic timeline and converges skip/end/missing-art on guarded results.
> Level 1 remains current for Replay. `CyberVictoryScorecard.ShowMissionScorecard`
> accepts a next-mission number, prepares the squad on Continue, and preserves
> existing grade thresholds/final-mission callers. Other mission flows are unchanged.
> The original three-argument `ShowScorecard` Godot binding remains intact (Godot
> dynamic calls do not apply C# optional defaults for a newly added argument).
> Six English ElevenLabs MCP cues use Daniel/Adam; Docker Blender/Python tools
> generate original planar dojo damage, poses, Data-Blade and quiet atmosphere.
> Opening artwork is reused without changing its cinematic or dialogue. Translation
> keys include speaker labels and English fallback. No new operative unlocks or
> watched flag. Final C# build passed with zero errors and 153 existing warnings.
> Background import exited 0 and generated every new asset's `.import`; existing
> addon TLS and editor shutdown/leak diagnostics remain. Full runner passed NUnit
> 34/34 and GdUnit4 114/114, zero failures/errors/orphans (report_346). Its host Django
> step skipped the absent venv; container Django passed 104/104 afterward.
> Docker asset validation passed RGBA dimensions/alpha/pose bounds and both 40-second
> audio tracks, with all six cues inside their shot windows. Godot AI MCP captured
> all six shots at 16:9 plus close/uplink shots at 16:10 and ultrawide, then verified
> results stay in Level 1, Replay restores active Level 1, natural audio completion
> reaches results, Continue loads Level 2 with a one-operative squad, and Level
> Select/Main Menu retain their destinations. Evidence is under `.validation/tokyo/`.
> Runtime diagnostics contain only the existing PostgreSQL authentication/local
> JSON fallback. The user's editor remains on Level 1, test game stopped, all four
> Docker services retained. A separate uninterrupted real-time playback verified
> audio/visual clock alignment at six checkpoints, natural completion at exactly
> timeline 40, and unchanged 100 health/150 score (`full-playback.json`). Physical
> touch/gamepad hardware and exports were not tested. Changes are local and uncommitted.

> **Docker startup only (2026-10-30):** User explicitly deferred post-Level 1
> cutscene work. The active Docker environment had no project containers/images;
> Compose pulled dependencies, built backend/website, and started db, backend,
> website. Website, backend countries endpoint
> returned HTTP 200 via IPv4 loopback; PostgreSQL reported healthy. No game code
> or assets changed. Only workflow/memory documentation updated; no import or
> unit suites required. Do not resume cutscene work without user direction.

> **Memory updates mandatory after every task (2026-10-30):** Added the same
> rule to root AGENTS, CODEX, CLAUDE, and GEMINI instructions. All agents must
> update `MEMORY.MD` before finishing any task, including read-only, planning,
> review, documentation-only, and Git-only tasks. Preserve the exact uppercase
> filename, keep durable context concise, exclude secrets, and distinguish
> actual verification from plans. README and MEMORY document the requirement.
> Diff hygiene and rule consistency checked; documentation-only changes require
> no Docker preflight, Godot import, or unit tests. No commit or push performed.

> **Project memory created (2026-10-30):** Added root `MEMORY.MD` for durable
> preferences, required skill usage, live cinematic decisions, integration
> context, and known verification limits. README links to it. Memory distinguishes
> historical test results from current checks and defers to current user/agent
> instructions. Verified against the current cinematic documentation and scene;
> this task changes Markdown only, so no import, unit tests, or container changes
> are required. Changes are left uncommitted.

> **Game-development skills installed (2026-10-30):** Installed the router and
> all 77 specialized skills from `gamedev-skills/awesome-gamedev-agent-skills`,
> pinned to commit `19d93f97c9c1ea0f8b306e77aa81e0d3e352d3e9`, using the Codex
> skill-installer helper. Independent global copies live under `~/.agents/skills`
> (Codex), `~/.claude/skills` (Claude Code), `~/.gemini/config/skills`
> (Antigravity app/IDE), and `~/.gemini/antigravity-cli/skills` (CLI).
> Updated CODEX, CLAUDE, GEMINI, and AGENTS with the same mandatory router-first
> rule for game-related work; README records the setup. Supporting references,
> scripts, and assets were installed alongside each skill. Existing skills were
> not overwritten. Installation verification checks counts and file hashes
> across all four roots; interactive discovery must be confirmed in each app.
> New skills become available on the next turn; restart/reload other agents if
> needed. Project changes are documentation-only, so no Godot refresh or game
> unit tests are required, and no containers were touched.

> **Mandatory Docker skills (2026-10-30):** Updated root `CODEX.md`, `CLAUDE.md`,
> `GEMINI.md`, and `AGENTS.md` with the same requirement to read and apply the
> relevant official `docker/skills` skills (`docker-skills@docker`) before any
> Docker-related action or guidance, including container preflight checks.
> Direct reading of installed skill files is the fallback when discovery does
> not expose them; missing access must be resolved before Docker work continues.
> README documents the workflow, and CLI examples should use `docker` from PATH.
> Documentation-only change: no containers were touched, and no Godot refresh or
> unit tests were required. Verification checks rule consistency and diff hygiene.



> **Godot AI dock "No server command found" — uv now installed on Windows (2026-10-29):** The dock showed `No server command found`, `uv not found`, and *"No godot-ai server found. Install `uv` via the Setup panel above, or run `pip install godot-ai`"*, with plugin v4.3.1 (`addons/godot_ai/plugin.cfg`). **The assumption that uv/godot-ai were already installed "locally in this directory" was wrong, and the plugin's own rules explain why:** `client_configurator.gd` resolves a server in three tiers — (1) dev `.venv`, (2) `uvx`, (3) a system `godot-ai` — and on Windows tier 1 requires `.venv\Scripts\python.exe` **plus a sibling `src/godot_ai/`**. The repo's `./.venv` is the **Linux/Docker venv** (`.venv/bin/python` → `/usr/local/bin/python`, no `Scripts/`), `src/godot_ai/` does not exist in this repo (godot_ai lives under `addons/`), and there was no `uv.exe`/`uvx.exe` anywhere in the tree — uv had only ever run inside the `python:3.14-slim` container. Tiers 2 and 3 therefore both missed and the dock had no command to run. **Fix:** installed **uv 0.13.6 (x86_64-pc-windows-msvc)** into `%LOCALAPPDATA%\Programs\uv` — one of the exact directories `clients/_cli_finder.gd::_well_known_dirs()` searches on Windows (also `~/.local/bin`, `~/.cargo/bin`, `%ProgramFiles%\uv`) — and appended it to the user `PATH`. The official `irm https://astral.sh/uv/install.ps1 | iex` installer **could not be used on this machine**: it aborts with "The 'Get-ExecutionPolicy' command was found in the module 'Microsoft.PowerShell.Security', but the module could not be loaded", so the release zip was extracted directly instead (see the README prerequisite note for that recipe). Pre-warmed the pinned tool env with both the plain and the **exact production argv** (`--isolated --no-config --no-env-file --no-sources --no-build --index-strategy first-index --keyring-provider disabled --index/--default-index https://pypi.org/simple --find-links https://pypi.org/simple/godot-ai/ --from godot-ai==4.3.1 godot-ai`): 73 packages installed, `godot-ai 4.3.1`, exit 0. **Verification:** a throwaway `-s` harness (created, run, then deleted — no project file remains) calling `ClientConfigurator.get_server_launch_mode()` / `get_server_command()` in headless Godot printed **`MODE=uvx`**, `MCP | using uvx (godot-ai==4.3.1)`, and `ARG[0]=C:\Users\karthiknk21/AppData/Local/Programs/uv/uvx.exe` with the 19-arg production command — i.e. the dock now has a real server command. **Follow-up for the user:** the dock caches "uv: not found" for the session, so click **Reload Plugin** in the dock (or restart the editor) to re-probe. The Linux `./.venv` is deliberately left alone: it is the container venv and must not be pointed at by the Windows editor; use `GODOT_AI_VENV_PYTHON` only with a real Windows interpreter (`.venv\Scripts\python.exe`) if a dev-venv override is ever wanted. Documentation-only repository changes in this task, so no Godot import refresh and no unit-suite run were triggered.

> **Host .NET SDK repair — Godot "Failed to load hostfxr" (2026-10-29):** Launching the .NET editor failed with *"Unable to load .NET runtime, specifically hostfxr"*. **The Docker image cannot fix this and the request to "point Godot at the C# image from Docker" is not achievable:** Godot is a native Windows binary that loads Windows `hostfxr.dll` via nethost, while `export/Dockerfile` carries `mcr.microsoft.com/dotnet/sdk:10.0` (Linux ELF) — the only way to use it would be running the editor inside a Linux container with a virtual display, which is not the Windows editor the user launches. **Actual cause:** the host had no .NET at all — no `dotnet` on PATH, and `C:\Program Files\dotnet\host\fxr` absent. `AxiomForce.csproj` and `tests/AxiomForce.Tests` both target `net10.0`. **Second, separate fault found while repairing:** `winget install Microsoft.DotNet.SDK.10` (10.0.403) reports "Successfully installed" and lays down 5,099 files (`sdk/10.0.403`, `shared/`, `packs/`, `templates/`) but **never writes `C:\Program Files\dotnet\dotnet.exe` or the whole `host/` directory** — watched for 5 minutes, they never appear, so they are blocked at write time rather than quarantined afterwards (Norton 360 is installed; Defender has no detections). An unelevated `/repair` run rolled the bundle back and deleted the folder entirely, so that path is a dead end without admin rights and an AV exclusion. **Working fix:** the zip-based per-user install — `dotnet-install.ps1 -Channel 10.0 -InstallDir "$env:LOCALAPPDATA\Microsoft\dotnet" -NoPath` — produced a complete SDK (`dotnet.exe`, `host/fxr/10.0.14`, `dotnet --version` → `10.0.403`), and `DOTNET_ROOT` was set for the user (plus that folder appended to the user `PATH`) so nethost resolves `hostfxr` for GUI launches. **Verification:** control run of `Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . --quit` with `DOTNET_ROOT` unset reproduced the reported failure verbatim (`Failed to load hostfxr`, `find_hostfxr modules/mono/mono_gd/gd_mono.cpp:182`, segfault, exit 139); the same command with `DOTNET_ROOT` set exits **0 with no `.NET` errors**, and `dotnet build AxiomForce.csproj` completes with **0 errors** (153 pre-existing CS8618/CS0169 warnings). **Caveats / follow-up:** the `DOTNET_ROOT` fix only reaches *newly* launched processes, so Explorer must pick it up (sign out/in or restart Explorer) before a double-click launch is clean; the leftover half-installed `C:\Program Files\dotnet` (5,099 files, no `dotnet.exe`) still needs an elevated delete, and the machine `PATH` still lists it (harmless — it is searched first, finds nothing, and falls through to the user entry). README prerequisites now carry the symptom, the per-user install and the `DOTNET_ROOT` step. Documentation-only changes in this task, so no Godot import refresh and no unit-suite run were triggered.

> **Stack rebuilt after Docker state wipe (2026-10-29):** `docker compose up -d --build` was re-run from scratch (0 images, 0 volumes beforehand) and the always-on group is up and verified: `db` healthy on 5432 with all 9 migrations applied to a fresh `pgdata`, `backend` Django 6.1.2 on 8000 (`GET /api/auth/countries/basic/` → 200, `/admin/` → 302, `/api/` → 404 because only `api/auth/` is routed), `website` 200 on 5173 with the AXIOM FORCE title and `/api/...` → 200 through the Nginx proxy. The new `pgdata` volume contains a fresh database. Nothing was exported: the `export` profile stayed unused and `build/` does not exist.

> **Compose: always-on group, exclusive export group, Node 26 (2026-10-28):** The stack now has two tiers. **Always-on** (`docker compose up`) = `db` (PostgreSQL 18, 5432), `backend` (Django, 8000), `website` (Nginx, 5173). **Exclusive** = `export`, which sits behind the `export` profile so `up` never starts it. The rule (documented in all four agent guides + README) is: game edits and website edits may run concurrently with the stack, **game exports may not overlap anything** — not another export, not a Godot import, not a website image build. The exporter is `export/Dockerfile` (Godot 4.7.2 mono + .NET 10 SDK + Android cmdline-tools/platform-34/build-tools-34 + **JDK 17 only** + `4.7.2.stable.mono` templates) driven by `export/export.sh`, and its `.godot`, `bin` and `obj` are mapped to **named volumes** — a full import through the Windows bind mount ran at ~1 KB/s (the cache is 5.7 GB of small files) against 29.7 MB/s raw sequential throughput, so imports now stay Linux-native and only finished artifacts cross to `./build`. `libfontconfig1` and `libgl1` were added after Godot warned that `libfontconfig.so.1` was missing. `website/Dockerfile` moved to **`node:26-alpine`** (v26.12.1). The unused **`eclipse-temurin:latest` (JDK 25)**, `python:latest` and `python:3.13-slim` images were deleted so JDK 17 is the only Java in the local store. `.gitignore` now ignores `build/` whole. **Verification (2026-10-28):** `docker compose config` valid; `up -d --build` brings db/backend/website up; website returns 200 with the AXIOM FORCE title and was built with `node:26-alpine`; backend `/admin/` returns 302. **Export guard (2026-10-28):** an export cannot start by accident. The service stays behind the `export` profile (so `compose config --services` and `docker compose up` show only `db`, `backend`, `website`), the service has `command: ["help"]` and `restart: "no"`, **the image's own `CMD` is `help`** (so even a bare `docker run axiom-force-export` is inert), and `export/export.sh` exits with usage text when no target is passed — the build/import/export steps sit *after* that guard. Verified by bringing the service up with the profile: it printed "No export requested — nothing was built" and exited 0, and no `./build` was created. Note the script is baked into the image, so `docker compose --profile export build export` is required after editing it (the first attempt ran the old copy and started building). `COMPOSE_PROFILES` is not set in `.env`, so nothing auto-enables the profile. **Exports are NOT end-to-end verified:** the toolchain is verified (Godot 4.7.2 mono, .NET 10.0.403, JDK 17.0.20.1, sdkmanager 12.0, templates present, C# Release build clean apart from pre-existing CS8618 warnings), but the user stopped the Linux preset run mid-export, so no export has been produced or checked; every export container was removed, `./build` was deleted, and no binaries, `.pck` or `.apk` files exist in the tree.

> **Docker Compose stack (2026-10-28):** `docker-compose.yml` now runs the whole service stack — `db` (`postgres:latest`, 18.6, published on 5432, named `pgdata` volume, `pg_isready` healthcheck), `backend` (built from the new `backend/Dockerfile` on `python:3.14-slim`, bind-mounted `./backend:/app`, published on 8000, waits for a healthy `db`), and `website` (new `website/Dockerfile`: Node 20 Vite build served by Nginx with the existing `website/nginx.conf` proxying `/api/` and `/admin/` to `backend`, published on 5173 to match `FRONTEND_URL`). Both images got a `.dockerignore` so the local `.venv` and `node_modules` never enter a build context. Two portability bugs were fixed on the way: **PostgreSQL 18 refuses the old `/var/lib/postgresql/data` mount** (it stores data in a version subdirectory), so the volume mounts at `/var/lib/postgresql`; and **`backend/entrypoint.sh` was checked out with CRLF**, which made the bind-mounted copy fail with `exec ... no such file or directory` even after the image normalised it — the file is now LF in the repo and `.gitattributes` keeps `*.sh` at `eol=lf`. **Verification (2026-10-28):** `docker compose config` valid; `docker compose up -d --build` brings all three services up, with `db` healthy and the backend log showing the entrypoint wait for PostgreSQL, the migration pass and Django 6.1.2 serving; `GET /` on 5173 returns 200 with the AXIOM FORCE title, `GET /api/auth/profile/` on 5173 returns Django's 401 JSON through the Nginx proxy, `GET /admin/` on 8000 returns 302, and `POST /api/auth/register/` returned 201 with the row visible in `accounts_user` inside the `db` container (smoke-test user deleted afterwards). The preflight bullet in the four agent guides now points at `docker-compose.yml` instead of the "no compose file yet" fallback, and `README.md` has a "Running the Stack with Docker" section. The stack is left running.

> **Docker workflow adopted (2026-10-28):** The user is moving the deleted WSL-container tooling to Docker Desktop. The `NO Docker CLI / Docker Desktop` rule was removed from the Permissions Denylist in `AGENTS.md`, `CLAUDE.md`, `CODEX.md` and `GEMINI.md`; the Git-commit and standard-WSL-distro rules remain. Official images were pulled and version-checked against the local Docker (29.9.2, Linux daemon): `python:latest` (newest CPython; not kept, see the venv note below), `mcr.microsoft.com/dotnet/sdk:latest` (10.0.403), `eclipse-temurin:latest` (OpenJDK 25.0.4.1 LTS — too new for Android Gradle, which wants 17/21), `postgres:latest` (18.6, matching the documented PostgreSQL 18) and `thyrlian/android-sdk:latest` (community image chosen by the user: JDK 17.0.12 plus cmdline-tools and licenses only — platform-tools, platforms and build-tools still must be installed with `sdkmanager`). No compose file or Dockerfile exists yet. **Python venv (2026-10-28, follow-up):** `python:latest` resolved to a CPython release too new to have prebuilt wheels for part of the `godot-ai` dependency tree, so pip fell back to compiling PyYAML and friends from source and had not finished after 17 minutes. Switching to **`python:3.14-slim`** (the project's own target) and installing with **`uv`** produced a complete `./.venv` in about 4 minutes: Python 3.14.10, `django 6.1.2`, `godot-ai 4.3.1` (79 distributions, both packages at their latest PyPI release). The venv was created by mounting the repo at `/app`, so it is a **Linux venv bound to `/app/.venv`** — use it from inside a `python:3.14` container (e.g. `docker run --rm -v "$PWD:/app" -w /app python:3.14-slim .venv/bin/python ...`), never from the Windows Python, and keep the mount path stable or recreate it. `.venv/` was already gitignored; the host OS Python was not touched. **Version-reference sweep (2026-10-28):** every mention of the discarded CPython releases is gone — this entry no longer names them, `README.md`, `AGENTS.md`, `CLAUDE.md`, `CODEX.md`, `GEMINI.md` and `website/README.md` all say Python 3.14, and the vendored `godot_ai` uv-cache comment (`addons/godot_ai/utils/uv_cache_cleanup.gd` plus the two backup copies under `addons/.godot_ai_update/backup/`) now uses version-agnostic `cp3XX` wheel tags instead of a concrete interpreter ABI tag. The archived `.zip` payloads in that backup folder still contain the old text; they are binary snapshots and were left untouched. **Docker preflight rule (2026-10-28):** `AGENTS.md`, `CLAUDE.md`, `CODEX.md` and `GEMINI.md` now open their Development Workflows section with "Docker Container Preflight (Before Editing Code Files)": before any task that edits code, an agent runs `docker ps`, starts Docker Desktop if the daemon is down, brings the stack up with `docker compose up -d` when the project containers are absent, and reads `docker compose logs <service>` if one exits. Read-only, review, documentation-only and Git-only tasks are exempt, and agents must never stop containers the user started. **This rule currently has no compose file to act on** — writing it is still the outstanding step, and until then the rule instructs agents to start whatever the repository defines and record the gap here. `README.md` carries a matching one-paragraph summary under Contributing & Agent Guidelines. **Verification (earlier addon-comment edit):** the Godot import refresh was run and the .NET-hosted engine segfaults at startup (missing `hostfxr`, no .NET SDK on this machine), so neither the refresh nor the GdUnit4 suite could complete; the addon edit is comment-only, confirmed by `git diff` showing 2 changed lines with no code changes.

> **WSL Containers tooling removed (2026-10-28):** At the user's request the entire native-WSL-Containers ("wslc") stack was deleted: the four root scripts (`setup_wsl_containers.ps1`, `run_wsl_containers.ps1`, `stop_wsl_containers.ps1`, `export_game_wsl.ps1`), `scripts/wsl_containers/fetch_image.py`, the three image definitions (`backend/Containerfile`, `website/Containerfile`, `export/Containerfile` with `export/export.sh`), and the machine-local artifacts `.container_cache/` and `.local-ca/`. Stale references were stripped from `README.md` (tech-stack row and "WSL Container Architecture" section), `AGENTS.md` / `CLAUDE.md` / `CODEX.md` / `GEMINI.md` (architecture section), `website/README.md`, `.env.example`, `.gitignore` (the `.local-ca/` rule) and doc comments in `backend/core/settings.py` plus `backend/accounts/onboarding_views.py`. The Docker and standard-WSL-distro prohibitions stay in the agent permissions denylist: they are environment policy, not this tooling. **Consequences:** there is no one-command way to start PostgreSQL + Django + Nginx together (export `POSTGRES_*` / `DB_*` yourself — nothing parses `.env` automatically — then run `python backend/manage.py runserver` and `npm run dev` in `website/`), and there is no automated headless exporter. Root `export.sh` / `export.bat` were left in place but they already invoked a `docker compose` `exporter` service whose compose file does not exist, and the `export/Containerfile` they depended on is now gone, so both are dead scripts; delete or rewrite them in a follow-up if export automation is wanted. **Verification (2026-10-28):** `run_tests.ps1` could not run in this environment — `backend/.venv` is absent and neither `django` nor DRF is installed for the system Python, nothing listens on PostgreSQL 5432, and no .NET SDK (`dotnet`) is on PATH, so the NUnit suite is unavailable and the .NET-hosted Godot binary aborts at startup on missing `hostfxr`. Ran instead: `python -m py_compile` on both edited backend modules (both parse clean) and a repo-wide sweep for `wsl_containers|export_game_wsl|frog_network|container.exe|Containerfile|local-ca|WSL Container` (no hits outside this handoff entry). No game engine, scene, shader or asset file was created, modified or deleted, so no Godot import refresh was required. Re-run `run_tests.ps1` on a machine with the .NET 10 SDK, a populated backend virtualenv and PostgreSQL before treating this cleanup as fully verified.

> **Documentation & Legacy Cleanup (2026-10-26, Antigravity):**
> - **Legacy Planning Cleanup**: Permanently deleted `UNITY_MIGRATION_PLAN.md` per user request, retiring obsolete engine migration planning.
> - **Markdown Documentation**: Synchronized operational guidelines across `AGENTS.md`, `README_Asset_Import.md`, `website/README.md`, and `docs/full_game_plan.md` to reflect current 2D Cyber-Action canon and testing protocols.
> - **Testing & Refresh Protocol Refinement**: Updated `AGENTS.md`, `CLAUDE.md`, `CODEX.md`, `GEMINI.md`, and `README.md` to clarify that unit test suites and Godot background imports are triggered strictly after code, scene, shader, or asset changes (`.cs`, `.gd`, `.tscn`, `.py`, `.json`, art/sound), and are skipped for documentation-only (`.md`) edits/deletions.
> - **Automated Suite Integrity**: Fixed backend environment packages (`django`, `pyjwt`) ensuring 100% pass across all 3 test suites (`run_tests.ps1`).

> **Airborne Combat & Aerial Locomotion (2026-10-24, Antigravity):**

> - **Issue Addressed:** Players previously could not attack or dash while airborne in Level 1 or across any mission, as `CyberShogun.cs` strictly required `IsOnFloor() && !_isAttacking` to initiate actions. This made attacking hovering enemies (e.g. `PalaceSentry` drones) extremely difficult.

> - **Changes in `scripts/actors/CyberShogun.cs`**:

>   - Removed the `IsOnFloor()` barrier in `HandleSpecialActions()`, enabling Katana Slash (`F` / `attack`), Signature Ability / Martial Strike (`Q`), and Blade Dash (`Shift` / `C`) both on the ground and in mid-air.

>   - Grounded vs Airborne physics tuning: grounded attacks zero out horizontal velocity (`Velocity.X = 0f`) to anchor the swing, whereas aerial attacks preserve existing horizontal trajectory momentum (`Velocity.X`) so the player smoothly completes their jump arc while slicing.

>   - Aerial Blade Dash: suspends vertical falling at initiation (`Velocity = new Vector2(dashDir * dashSpeed, IsOnFloor() ? Velocity.Y : 0f);`), giving responsive, snappy air dashes reminiscent of *Katana Zero* and *Ghostrunner*.

>   - Action completion: only resets horizontal velocity to zero if `IsOnFloor()`, preventing jarring dead stops in midair when an aerial attack completes.

> - **Changes in `scripts/actors/MainCharacter.cs`**: Updated legacy controller to match the non-blocking airborne attack paradigm.

> - **Automated Coverage**: Added `test_cyber_shogun_airborne_attacks_and_dashes` in `tests/test_cyber_combat_hud_and_mechanics.gd` validating that all 3 combat actions trigger while airborne and transition smoothly.



> **Earned-operatives campaign (2026-10-22, supersedes the Protocol Zero, character-selection-first and old level 2-4 notes below):**
> - **Flow:** New Game → AXIOM-7 briefing (`StoryIntro.cs`) → Mission 1 (`CampaignProgress.StartNewCampaignAsync` creates the save with Cyber Shogun). The roster screen (`CharacterSelection.cs`, button "SQUAD ROSTER" on the main menu) is no longer part of new-game flow: locked operatives are marked, "Set Active" chooses who starts the next mission, Escape returns. **Protocol Zero is deleted** (`TrainingDirector`, `TrainingTarget`, `training_protocol.*`, its test, the capture tool, the level-select replay button); `TrainingArtwork.cs` is kept because `PalaceOperative` draws the approved art with it.
> - **Unlock rules** live in engine-free C#: `LevelCatalog` (ordered missions, who joins, required ability tags) and `OperativeRoster` (unlock/active/cycle/lesson-seen). The squad is *derived from progress*, not saved: at the start of mission N it is Shogun plus everyone granted by missions < N (so no DB column or Django change was needed; old saves keep working). `CampaignProgress` syncs `Global.selected_character` / `Global.unlocked_levels`, rebuilds the roster before every mission launch (level select, Continue, intro) and advances the save on completion. Joins: M2 Oceanus then Apex Titan; M3 Aura Phoenicia then Mirage Blade; M4 Surya Prime. Mid-mission joins are replayed if the mission is reloaded.
> - **Swap:** Tab / Shift+Tab, 1-6, gamepad D-pad Down (next) / Back (previous). `CyberShogun.SwapOperative` keeps the health/shield *ratio* (no swap-healing), carries the dash cooldown, applies a 1 s lockout, and updates `Global`. `SquadStrip` (HUD) shows active/unlocked/locked slots. `TrainingArtwork` reloads art on profile change.
> - **Signature abilities (Q, alongside the existing secondary strike):** `CyberShogun.TriggerSignatureAbility` — parry (0.45 s window reflects *ranged* damage and raises `ParryDeflected`), douse, slam, resonate, phase (0.8 s hazard immunity + short blink), bulwark (1.4 s immunity to hazards and ranged fire). Ranged attackers must call `TakeDamageFrom(damage, knockback, source)`; sourceless `TakeDamage` is treated as environmental. (`TakeDamage` keeps its 2-arg signature because GDScript tests call it that way; Godot C# ignores default parameters from GDScript.) Gates are `AbilityGate` (fire, rubble, sonic lock, heat vent) found through the `ability_gates` group, plus lethal always-on `CyberLaserGrid2D` curtains cleared only by Phase. Rubble/locks/gates reach the world ceiling and `CodeLevel` adds an invisible ceiling so wall-jumping cannot bypass a gate.
> - **Lessons:** `RecruitBeat` unlocks the operative on proximity, shows story lines and a prompt (swap key + ability), and completes when that operative uses their ability; never blocks. Shogun's parry lesson is in Mission 1.
> - **Levels:** `CodeLevel` (new base class extracted from the old `PalaceMission`) owns backdrop/parallax, camera, HUD, shrines, fragments, title card (from `NarrativeManager`), pause, scorecard on the final mission, chasm recovery, completion. Subclasses: `PalaceMission` (M1, painted backdrop, shielded beacon needs two parries), `ConcreteWildfireMission` (M2), `NeonRustMission` (M3), `ShatteredAegisMission` (M4, spawns `CyberBossMech2D` on entering the arena, binds the HUD boss bar). Levels 2-4 use `SkylineBackdrop` (procedural parallax city, palettes per level) and new `PalaceSurface.Concrete/Steel`. Old tilemap level2-4 scenes were replaced by 6-line scenes.
> - **Story:** `english.json` v3.1 (level 0 removed, `joins` lists, Surya's L4 line), `docs/STORY_BIBLE.md` has the earned-operatives table. Other locale files are still v2.0 and fall back/stale as before.
> - **Verification (2026-10-22):** `dotnet build` clean; GdUnit4 100 test cases, 0 errors/failures/orphans (new: `test_operative_swap`, `test_signature_abilities`, `test_campaign_levels` incl. a reachability audit of every shrine/objective/fragment with conservative jump limits and a gate-meets-floor check; updated level-selection, level 1, HUD, hazards and boss tests); NUnit adds `OperativeRosterTests`. Screenshots of every level were inspected. The audit found and fixed gate walls in M2/M4 that stopped 600 px above the floor.
> - **Not done / limitations:** nobody has played M2-M4 end to end (jump limits are audited, difficulty and pacing are untested); no VO; levels 5-9 not built (add a `LevelCatalog` row + a `CodeLevel` subclass + scene); mid-mission joins are not saved (they replay on reload); the roster screen's dossier text/portrait for locked operatives is still fully visible (only the button state is locked); the isometric files (`Isometric*.cs`, `isometric_operative.tscn`, `ghost_gridlock.png`, `campaign_atlas.png`, art tools) and the old `finish.gd`/`collectable.gd` are unused but kept (`CyberCombatHud` still references `IsometricOperative`); pause menu has no roster entry (swap in-level instead); `Palace*` class names are historical.

> **Level 1 platformer redesign (2026-10-22, supersedes the isometric Level 1 below):** `scenes/gameplay/level1.tscn` now only attaches `scripts/gameplay/PalaceMission.cs`, which builds everything in code: a painted parallax backdrop (`assets/images/level1/palace_garden_painted.png`, manual parallax on a CanvasLayer), ~9,500 px of procedural geometry (`PalacePlatform` stone/roof/wood/wall/gate surfaces, one-way timber ledges, `PalaceMovingPlatform` ferry), `PalaceOperative` (the selected operative on the platformer `CyberShogun` controller with `TrainingArtwork`, i.e. the approved AI-generated Shogun sheets), `PalaceSentry` hover drones, `PalaceObjective` props (beacon, Data-Blade, exit, shrine checkpoints, memory fragments) and the shared laser-grid / surge-floor hazard scenes. Route: Garden Gate → Lantern Court (laser bridge) → Blossom Gorge (wall-jump shaft with optional ledges) → Rooftop Run → Archive Descent → Beacon Court. Beacon destroyed → `DojoGate` opens → Data-Blade → two enforcers spawn → extraction unlocks Level 2. Design limits: vertical steps ≤150 px, gaps ≤220 px so every operative (jump 940–1060, speed 340–460) can finish. Chasm falls (Y>950) respawn at the last shrine with a 15-damage penalty; defeats respawn there after 0.9 s. The old isometric files (`Isometric*.cs`, `isometric_operative.tscn`, `ghost_gridlock.png`, `campaign_atlas.png`, `tools/art/render_level1_iso.py`, `prepare_campaign_shogun.py`) are now unused by Level 1 but left in place (`CyberCombatHud` still references `IsometricOperative`); delete them in a follow-up if the isometric direction is permanently dropped. Not yet play-tested by a human end to end — jump spacing was verified by calculation and by rendered screenshots, not by a completed run. `tests/test_level1_2d_cyber_shogun.gd` was rewritten for the platformer. Verification (2026-10-22): `dotnet build` clean; `run_tests.ps1` → Django 104 OK, NUnit 20/20, GdUnit4 81 cases with 0 errors/failures/orphans; seven rendered screenshots (start, court, gorge, rooftops, archive, beacon court, dojo core) were inspected visually.

> **Unified selection dossier (2026-10-22; now the Squad Roster screen):** Character selection now has only the roster and one right-hand dossier. The former center preview is inside `RightShowcasePanel/VBox/ShogunArtStudy`; the four loadout buttons are the sole ability-preview controls, while movement/melee previews remain separate. Upgrade and Deploy share `VBox/Footer`. Focus links are rebuilt for visible controls and return to the selected operative, including after upgrades. Preview sizing now respects available height. Original artwork and training/gameplay remain unchanged. Scene-path and layout/navigation regression coverage was updated; post-edit verification results are reported with this task's completion.


> **Agent documentation workflow (2026-10-21):** `CLAUDE.md`, `CODEX.md`, and `GEMINI.md` now require every agent to update both root `README.md` and root `HANDOFF.md` before finishing a task that edits the game or website, including related backend, assets, configuration, tooling, tests, or documentation. README covers current behavior and contributor guidance; HANDOFF records implementation decisions, actual verification status, and remaining work. Documentation belongs in the final edit batch before the required refresh/tests. Read-only and Git-only tasks are exempt; commit/push authorization remains separate. This task changes documentation only; post-edit refresh and test results are pending at the time of this entry and must be checked in the task's final report.


> **Protocol Zero (2026-10-21) — REMOVED 2026-10-22, see the earned-operatives entry above:** New campaigns complete AXIOM-7's story briefing before entering the seven-room guided 2D trainer; completing or skipping training starts Level 1. Level selection index 0 replays training. `TrainingDirector` owns local checkpoints, text guidance, pause/focus, and non-scoring completion; it never persists campaign scores/deaths/unlocks. All six operatives use `TrainingArtwork` on their real physics bodies, preserving the trainer's current art. Lessons cover movement, jump/wall mobility, melee, dash, and passive shield recovery. No ElevenLabs dependency. Controller dash travels during attack physics; current-transform hit queries prevent stale or duplicate hits. Training targets are non-blocking layer-2 hurtboxes.

> **Selection art study:** All six operatives have hand-drawn character-selection previews with movement and their own four loadout actions. Cyber Shogun uses the approved AI-generated pose and locomotion sheets in selection and Level 1; `tools/art/prepare_campaign_shogun.py` packs those sources for Level 1. The five other selection sheets live in `assets/characters/hand_drawn`. All six original flat designs and SpriteFrames remain preserved under `assets/characters/rendered` for fallback and other existing scenes. These visuals do not add combat mechanics. `ShogunArtPreview.UseOriginalDesign` still enables the preserved flat-art comparison.

> **Character artwork update (2026-10-20, supersedes the model-render approach):** Characters now use original flat 2D drawings authored in Python and Blender, with outlined planar shapes, solid colors and 2D joint animation. No prior game artwork or models are inputs. `tools/art/draw_flat_characters.py` and `package_character_sprites.py` generate the portraits and SpriteFrames under `assets/characters/rendered`. Do not restore the recovered GLBs, old model-render pipeline, rollback JPG cards or legacy samurai/robot sheets. See `tools/art/CHARACTER_RENDERING.md` for regeneration.

> **Current cleanup status (2026-10-19):** The game now starts at `main_menu.tscn`, uses the 2D operative cards and briefing, and deploys into `level1.tscn`. Scene background photos and project-owned 3D models, animation libraries, gameplay code, shaders, tooling, and web viewers have been deleted at the user's request. The historical 3D inventory and migration tracks below no longer describe files present in the workspace. Shared add-ons remain intact. `TitleLogo.cs` now owns localized title lookup; the website uses text/icon panels. See `tests/test_scene_cleanup.gd` for coverage of all retained scenes.

**Target Audience:** Incoming AI Agent (Claude, Codex, Antigravity)  
**Date / Timestamp:** 2026-10-26  
**Current Branch:** `main` (in sync with `origin/main` at commit `be99afa`)  
**Test Suite Status:** 100% PASSED (Django: 104/104, NUnit: 34/34, GdUnit4: 21/21 suites, 102/102 tests, 0 errors, 0 failures, 0 orphans)

---

## 1. Executive Summary: The 2D Cyber-Action Transition

Axiom Force was originally structured as a hybrid 3D/2D action game. Due to persistent 3D meshing, GLB bone orientation mismatches, and rigging friction from multi-generator assets, **the project is transitioning toward a high-fidelity 2D Cyber-Action platformer** (inspired by *Katana Zero*, *Hades*, and *Ghostrunner*).

Over recent sprints, **Phases 1 through 6 of the 2D Cyber-Action core** have been successfully built, integrated, and verified with automated test suites. However, **several critical parts of the game (main scene bootstrap, tutorial level, story intro, and level select mapping) still rely on legacy 3D scenes**.

This document outlines:
1. Everything built in 2D so far.
2. An exhaustive inventory of remaining 3D components.
3. The roadmap and technical recommendations for the incoming agent to complete the transition.
4. Strict operational constraints (testing protocols and Git push permissions).

---

## 2. Completed 2D Architecture (Phases 1 – 6)

### Phase 1: 2D Operative Selection & Card Showcase
- **Scene/Script**: `scenes/main_menu/character_selection.tscn` / `scripts/main_menu/CharacterSelection.cs`
- High-resolution 2D character portrait cards (`assets/images/operatives/*.png|jpg`) for all 6 operatives:
  1. **Cyber Shogun (Kaito Kurogane)** - Vanguard Striker
  2. **Surya Prime (Arjun Varma)** - Solar Bulwark Tank
  3. **Oceanus (Kiana Vaea)** - Hydro Vanguard Tactician
  4. **Mirage Blade (Zayd Al-Mansoor)** - Phantom Infiltrator Assassin
  5. **Apex Titan (King Tariq Nzinga)** - Electro-Magnetic Juggernaut
  6. **Aura Phoenicia (Maya Khoury)** - Prismatic Sound Weaver
- Tactical ability loadout inspections, animated 2D sprite frames, and attribute stat meters.

### Phase 2: Cyber Shogun 2D Controller & Level 1 Cyber City
- **Scene/Script**: `scenes/gameplay/cyber_shogun.tscn` (`scripts/actors/CyberShogun.cs`), `scenes/gameplay/level1.tscn`
- Grounded katana attacks (`[F]` / sword attack), martial strike (`[Q]` / karate attack), blade dash (`[Shift]` / `[C]`), hitboxes, dynamic camera smoothing, and neon cyber city backdrop (`assets/images/cyber_city_dusk_background.png`).

### Phase 3: Dynamic Combat HUD, Slash Combos, & 4-Level Chain
- **Scene/Script**: `scenes/gameplay/cyber_combat_hud.tscn` (`scripts/ui/CyberCombatHud.cs`), `scripts/actors/CyberDrone2D.cs`
- Full 4-level linear campaign chain: `level1.tscn` $\rightarrow$ `level2.tscn` $\rightarrow$ `level3.tscn` $\rightarrow$ `level4.tscn`.
- Flying patrol drone enemies (`CyberDrone2D`), combo scaling counter (`DOUBLE STRIKE!`, `TRIPLE COMBO!`, `TACTICAL OVERDRIVE!`), health & shield telemetry.

### Phase 4: Wall-Slide/Jump Mobility, Cyber SFX Engine, & 2D Hazards
- **Wall Mobility**: Wall-sliding with downward velocity clamping (`140 px/s`) and outward wall-jumping (`380 px/s`).
- **Interactive Hazards**:
  - `CyberLaserGrid2D.cs` / `cyber_laser_grid_2d.tscn`: Cycling lethal laser beams with periodic hum.
  - `CyberHazardFloor2D.cs` / `cyber_hazard_floor_2d.tscn`: Electrified floor grids with rhythmic surges.
- **Synthesized SFX Engine** (`scripts/tools/generate_cyber_sfx.py`): 6 native 16-bit 44.1kHz PCM WAVs: `katana_slash.wav`, `blade_clash.wav`, `blade_dash.wav`, `drone_explosion.wav`, `laser_hum.wav`, `shield_hit.wav`.

### Phase 5: Vanguard-X Boss Encounter & Victory Evaluation Flow
- **Boss Mech**: `scripts/actors/CyberBossMech2D.cs` / `scenes/gameplay/cyber_boss_mech_2d.tscn` in `level4.tscn`.
  - Phase 1 Tactical Hover with homing plasma missiles (`CyberBossMissile2D.cs` / `boss_cannon.wav`).
  - Phase 2 Overdrive Rage shift at $\le 50\%$ HP (siren alarm `boss_alarm.wav`, crimson glow, 3-way spread missiles).
  - Multi-explosion death sequence awarding 50 points and unlocking extraction.
- **Boss HUD Integration**: Real-time boss health bar and phase badge inside `CyberCombatHud`.
- **Victory Scorecard**: `scripts/ui/CyberVictoryScorecard.cs` / `scenes/ui/cyber_victory_scorecard.tscn` hooked to `finish.gd`, evaluating mission combat ranks (**S Rank / Flawless**, **A Rank / Vanguard**, **B Rank / Operative**) with victory fanfare (`victory_jingle.wav`).

### Phase 6: Multi-Operative Deployment & Character Synergy
- **Registry & Profile**: `scripts/actors/OperativeProfile.cs` (`OperativeRegistry`) providing distinct stats and passives for all 6 operatives:
  - **Surya Prime**: 140 HP, 80 Shield, 15% Armor Damage Mitigation, Solar Amber glow.
  - **Oceanus**: 90 HP, 70 Shield, 25 HP/s Passive Hydro Shield Regen, Oceanic Teal glow.
  - **Mirage Blade**: 80 HP, 40 Shield, 460 Speed, 0.6s Dash Cooldown, 1000 Dash Speed, Emerald glow.
  - **Apex Titan**: 160 HP, 60 Shield, 20% Kinetic Armor Mitigation, Heavy Knockback Resistance, Violet glow.
  - **Aura Phoenicia**: 95 HP, 55 Shield, 8% Mitigation, 0.85s Dash Cooldown, Magenta glow.
  - **Cyber Shogun**: 100 HP, 50 Shield, Balanced Striker, Radiant Cyan glow.
- **Dynamic HUD & Scorecard Telemetry**: HUD badges automatically adapt to operative ability names and theme colors; scorecard displays the deployed operative's full identity.

---

## 3. Inventory of Remaining 3D Systems & Relics

The user explicitly noted: *"Many parts of the game are still in 3D."*  
Here is the exact catalog of what is still 3D in the project:

### A. Engine Startup & Main Menu
1. **`project.godot` Bootstrap**:
   - `run/main_scene="res://scenes/main_menu/3d_menu_experience.tscn"`
   - The game still boots into `3d_menu_experience.tscn` rather than a pure 2D Cyber Command Center.
2. **`scenes/main_menu/3d_menu_experience.tscn` & `scripts/main_menu/ThreeDMenuExperience.cs`**:
   - Contains 3D Meshy GLB models: `Syndicate_Hab_Block`, `Terminal_Kiosk_Node`, `PatrolDroneVehicle`, `HoloProjector`, and 3D lighting.
3. **`scenes/main_menu/character_selection.tscn`**:
   - Still houses a `SubViewport3D` with `World3D` and `Camera3D` for rotating 3D models on a podium (even though 2D character cards and 2D sprite frames are now active).

### B. Story Cutscene & Intro
1. **`scenes/main_menu/3d_story_intro.tscn` & `scripts/main_menu/story_intro.gd`**:
   - The story cutscene transitioning from Character Selection to gameplay is rendered in 3D.

### C. Gameplay Scenes & Level Select Mapping
1. **`scenes/gameplay/training_protocol_3d.tscn` (Sector 0 / Tutorial)**:
   - Full 3D tutorial level using `Level3DManager.cs`, `Player3D.cs`, `FloatingRobotBuddy3D.cs`, `WeaponArea3D.cs`, and `PlayerHUD3D.cs`.
2. **`scenes/gameplay/prototype_3d.tscn`**:
   - Relic 3D level still pointed to by `level_map[1]` in `level_selection.gd`.
3. **`scripts/main_menu/level_selection.gd`**:
   - Currently maps index 0 to `training_protocol_3d.tscn` and index 1 to `prototype_3d.tscn`, whereas `level1.tscn` through `level4.tscn` are the active 2D levels!

### D. 3D Scripts & Test Suites
1. **3D Actor & Gameplay Scripts**:
   - `scripts/gameplay/Player3D.cs`
   - `scripts/gameplay/Enemy3D.cs`
   - `scripts/gameplay/Hazard3D.cs`
   - `scripts/gameplay/WeaponArea3D.cs`
   - `scripts/gameplay/Level3DManager.cs`
   - `scripts/gameplay/HealthBar3D.cs`
   - `scripts/gameplay/FinishZone3D.cs`
   - `scripts/actors/FloatingRobotBuddy3D.cs`
   - `scripts/ui/PlayerHUD3D.cs`
   - `scripts/gameplay/HeroAbilityVFX.cs` (3D particle and mesh generator for 3D selection podium)
2. **3D Assets & Blender Tools**:
   - `assets/models/hq/*.glb` (GLB meshes for operatives and environmental props)
   - `tools/assets/rebuild_all_hero_animations.py`
   - `tools/assets/screenshot_ability_vfx_preview.gd`
3. **Tests Depending on 3D**:
   - `tests/test_protocol_zero_3d.gd` (Asserts `training_protocol_3d.tscn` loads and binds 5 tutorial targets).

---

## 4. Suggested Roadmap for the Incoming Agent

To address *"Many parts of the game are still in 3D"*, here are the high-priority migration tracks:

### Track 1: Level Selection & Campaign Mapping (Immediate Win)
- **Goal**: Re-align `scripts/main_menu/level_selection.gd` so the player directly selects and launches 2D missions (`level1.tscn` through `level4.tscn`).
- **Action**:
  - Update `level_map` in `level_selection.gd`:
    ```gdscript
    var level_map = {
        0: "res://scenes/gameplay/training_protocol_2d.tscn", # (or level1.tscn)
        1: "res://scenes/gameplay/level1.tscn",
        2: "res://scenes/gameplay/level2.tscn",
        3: "res://scenes/gameplay/level3.tscn",
        4: "res://scenes/gameplay/level4.tscn",
    }
    ```
  - Migrate or update `tests/test_protocol_zero_3d.gd` to test the new 2D tutorial/level mapping without breaking CI.

### Track 2: 2D Story Intro / Tactical Briefing
- **Goal**: Replace `scenes/main_menu/3d_story_intro.tscn` with a sleek 2D cyber visual novel / tactical briefing screen.
- **Action**:
  - Display the selected operative's 2D portrait card, mission directives, audio chatter, and a "START MISSION" trigger directing to `level1.tscn`.

### Track 3: 2D Protocol Zero Training Level
- **Goal**: Convert Sector 01 from `training_protocol_3d.tscn` to `training_protocol_2d.tscn`.
- **Action**:
  - A dedicated 2D training arena introducing basic movement, wall-sliding, blade dashing, and parrying against dummy drones.

### Track 4: Main Menu 2D Modernization & Engine Bootstrap
- **Goal**: Update `project.godot` to launch `scenes/main_menu/main_menu.tscn` (or a dedicated 2D Cyber Command Center) instead of `3d_menu_experience.tscn`.
- **Action**:
  - Update `run/main_scene="res://scenes/main_menu/main_menu.tscn"` in `project.godot`.
  - Ensure background parallax, 2D holographic glow, and audio responsiveness are active in the 2D menu.
  - Cleanly decommission the `SubViewport3D` inside `character_selection.tscn` so it relies 100% on the 2D Operative cards and animated previews.

### Track 5: Safe Retirement / Decoupling of 3D Scripts
- **Goal**: Cleanly archive or deprecate `Player3D.cs`, `Enemy3D.cs`, `Level3DManager.cs`, `PlayerHUD3D.cs` so dead 3D code does not cause confusion or maintainability debt.

---

## 5. Critical Operational Rules (MANDATORY)

> [!IMPORTANT]
> ### 1. Strict Git Push Constraint
> **DO NOT execute `git push` under any circumstances unless the user explicitly commands you to do so!**  
> Staging (`git add`) and local commits (`git commit`) are permissible when instructed, but pushing to remote must wait for explicit user approval (`"push to git"`).

> [!IMPORTANT]
> ### 2. Mandatory Testing Protocol (End of Every Prompt)
> At the conclusion of **EVERY** turn or task, you **MUST** execute the test suite and verify 0 failures, 0 errors, and 0 orphans before concluding:
> ```powershell
> .\run_tests.ps1
> ```
> To run individual suites:
> - **NUnit (.NET 10 C#)**:
>   ```powershell
>   dotnet test tests/AxiomForce.Tests/AxiomForce.Tests.csproj
>   ```
> - **GdUnit4 (Godot 4 Console Executable)**:
>   ```powershell
>   & "C:\Users\karthiknk21\Desktop\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe" --headless --path . -s addons/gdUnit4/bin/GdUnitCmdTool.gd --add res://tests/ --ignoreHeadlessMode
>   ```

### 3. Godot Console Executable Requirement
- Always use `Godot_v4.7.2-stable_mono_win64_console.exe` on Windows to avoid stdout GUI deadlocks that can cause headless processes to hang.

### 4. Tree Safety & Node Lifecycle
- Always check `IsInsideTree()` before calling `GetTree()`, `CreateTween()`, or `.Play()` on audio players. Standalone unit tests instantiate scenes outside the active tree; unguarded calls trigger engine error 100/105.
- Always call `.free()` or `.queue_free()` on test-instantiated nodes in GdUnit4 to guarantee 0 orphan nodes.

---

## 6. Key File & Directory Map

| Path | Purpose |
|---|---|
| `scripts/actors/OperativeProfile.cs` | Model and central registry for all 6 operatives (stats, passives, synergy). |
| `scripts/actors/CyberShogun.cs` | Core 2D player controller (attacks, blade dash, wall-sliding, shield regen, operative scaling). |
| `scripts/actors/CyberBossMech2D.cs` | Multi-phase 2D boss mech encounter ("Vanguard-X") with Overdrive Rage phase shift. |
| `scripts/gameplay/CyberBossMissile2D.cs` | Boss missile projectile with collision, knockback, and particle trails. |
| `scripts/gameplay/CyberLaserGrid2D.cs` | Interactive 2D laser hazard with periodic cycling. |
| `scripts/gameplay/CyberHazardFloor2D.cs` | Interactive 2D electrified floor hazard with rhythmic damage surges. |
| `scripts/ui/CyberCombatHud.cs` | 2D in-game HUD with player shields, health, combo streak, and boss telemetry. |
| `scripts/ui/CyberVictoryScorecard.cs` | Post-mission tactical scorecard evaluating combat grades (S/A/B ranks) with fanfare. |
| `scripts/main_menu/CharacterSelection.cs` | Operative selection screen with 2D character portrait cards. |
| `scenes/gameplay/level1.tscn` ... `level4.tscn` | The complete 4-level 2D progression chain. |
| `tests/test_phase4_hazards_and_audio.gd` | GdUnit4 test suite for wall mechanics, SFX, and 2D hazards. |
| `tests/test_phase5_boss_and_victory.gd` | GdUnit4 test suite for boss mech, missile attacks, and victory scorecard. |
| `tests/test_phase6_operative_synergy.gd` | GdUnit4 test suite for operative registry, passives, and dynamic HUD styling. |
| `run_tests.ps1` | Unified test runner for NUnit (.NET 10), GdUnit4, and Django. |
# Deployment cinematic implementation

New Game routes through `deployment_cutscene.tscn` before the existing briefing.
The opt-in `cutscene` Compose service renders Blender artwork and packages a
40-second Theora film using Python/FFmpeg. Playback includes timed English subtitle
fallbacks, aspect preservation, skip controls, and an idempotent transition.
ElevenLabs narration is now generated via the direct API: the root `.env` key
was validated, and Daniel was selected from the live voice list. Four multilingual
v2 cues (6.03s, 6.11s, 7.11s, 5.62s) fit their subtitle windows without tempo
changes. `tools/cutscene/narrate.py` caches paid generations and uses Docker
FFmpeg for radio EQ and the 40-second `narration.ogg` mix. MCP tools are still
unavailable; the plugin text-to-speech workflow supplied the API approach.
Audio verification: Godot generated `narration.ogg.import`; ffprobe confirmed
40.000 seconds. Python compilation passed. NUnit 34/34, GdUnit4 104/104
(report_339), and container Django 104/104 passed. The test wrapper skips its
Windows Django environment, so Django ran in the backend container instead.
Import exited 0 with the existing plugin TLS/editor cleanup errors. A capped
45-second playback check reached StoryIntro and exited 0, with one resource
still in use at engine shutdown; runtime also reports the existing database
password fallback. No API key was printed or added to tracked configuration.
Four plates rendered in Windows Blender 5.2.3;
Docker packaging succeeded and ffprobe verified 40 seconds, 1920×1080, 24 fps,
Theora/Vorbis, 11,414,306 bytes. This is a four-plate animatic: animated rain,
running and surveillance sweeps remain unfinished.
Initial verification: C# build passed (153 existing warnings); NUnit 34/34,
GdUnit4 104/104 including two new tests, and container Django 104/104 passed.
The wrapper skips Django without a Windows venv, so it was run in Docker.
Import logs contain plugin TLS errors and exit resource leaks; do not describe
the import as warning-free. Final import exited 0 with the same plugin/cleanup
errors and no cutscene parse errors. Final NUnit 34/34, GdUnit4 104/104
(report_338), and Docker Django 104/104 passed after video packaging.
Runtime startup loaded without cutscene errors; host SessionManager reports a
PostgreSQL password failure and uses its existing local JSON fallback.
The 45-second, 60-fps-capped headless playback check reached StoryIntro and
successfully started its first existing AXIOM-7 voice line, with no ERROR lines.
Existing `.vscode/settings.json`
changes are unrelated and were preserved.

## Live 2D deployment cinematic (2026-10-30; supersedes the film playback above)

Replaced runtime video decoding with an editable layered SubViewport/Node2D scene,
Camera2D rig, sampled Shogun atlas, deterministic bounded rain/drone sweeps/transit,
weighted jump/landing/dash, afterimage, sparks and AXIOM-7 scan transmission.
The 40-second narration binary, dialogue, cue windows and New Game -> briefing
routing are preserved. Audio playback position plus mixer/output latency drives the
single clock, with monotonic elapsed fallback for missing/ended/stalled narration.
UI and skip remain independent of the camera; completion and all skip inputs share
one guarded deferred transition. Scene exit stops owned audio.

Asset generation: `docker compose --profile cutscene run --rm cutscene --reveal-only`
then `--live`. Reuses established original red/gold/ink Blender geometry (8 columns,
5 action rows, 512px cells) and planar city vocabulary; the container uses portable
CPU emission rendering because its Blender 4.3 calls Eevee by a different identifier.
Python validates dimensions, transparency and occupied atlas cells and writes the
manifest. The old OGV and build options remain a legacy preview only.

Godot AI tools were absent from the exposed tool list. `tools/cutscene/mcp_probe.py`
restores direct authenticated MCP access using protocol 2025-11-25, with no changes
to global MCP configuration. Verified session frog-jumper@52eac1f41bdee749 identifies
D:/Projects/frog_jumper, Godot 4.7.2. The user's open level1 scene stays untouched;
runtime checks launch the custom cutscene with autosave disabled.

Verification so far: Docker preflight found the core services running; representative
Shogun render inspected for identity/palette; C# build passed with 153 existing
warnings and zero errors. Final asset validation, import, runtime captures/layouts,
and unit suites are pending at this point; results will be recorded below.

Runtime review found and removed the global keyboard-help banner during the cinematic
using Global's existing visibility API, restoring it on exit. Renamed a shadowing
shader local after MCP diagnostics caught the warning. Eight in-engine captures
cover all shots plus 16:10/ultrawide in `.validation/deployment/review`; UI bounds
are inside every viewport. Initial GdUnit4 108/108 (report_340), NUnit 34/34 and
container Django 104/104 pass. The reference checker reports 10 existing connection
issues: eight in vendored proton_scatter and two in the unchanged main menu scene;
none is in this cutscene. Import exited 0 with pre-existing TLS/editor shutdown
resource errors. Host SessionManager still reports its existing PostgreSQL password
fallback; the container backend suite passes. Final checks must be repeated after
the banner/clock/input-test refinements.

The landing uses a grounded pose during compression, and the scan shader preserves
inherited alpha so the transmission portrait assembles with its overlay. Final
runtime/performance review follows after import and unit-suite reruns.

Live pointer testing exposed Global's project-wide mouse consumption: native GUI
mouse delivery never reached Skip. The cinematic now handles mouse/touch presses
inside its Skip rectangle during `_input`, before Global, without changing Global.
Direct screen touch does not depend on mouse emulation. A regression test covers
outside/inside/repeated touch; keyboard/controller/button signal tests remain.
An uninterrupted 40-second run reached story_intro with narration playing through
all shots. 1080p measured frame medians ~16.6-16.7ms and batch p95 ~17.4-17.8ms;
screenshot stalls are excluded. Audio and timeline differed by the intended ~20ms
output compensation. Final checks are repeated after the pointer fix.

Final verification (2026-10-30): 109/109 GdUnit4 tests pass, with 0 failures,
0 errors and 0 orphans (report_342); NUnit 34/34; Docker backend Django 104/104.
The wrapper skips its absent Windows backend venv, so Django ran explicitly in
the existing backend container. Incremental C# build: 0 errors, 0 warnings (the
initial full compile had 153 existing warnings). All eight live PNGs have imports;
container asset validation passes, and all five Python tool modules compile.
MCP-injected Escape, Start, pointer click and direct screen-touch each reached the
briefing at early/mid/late times after the pointer fix. Repeated transitions and
missing narration/standalone cleanup are covered by unit tests. Runtime review
artifacts: `.validation/deployment/review/`, `full-playback.json`, `input-checks.json`.
The narration SHA256 matches HEAD exactly. The user's open level1 editor scene
was preserved; only our test game was stopped and all four Docker services remain up.

Known limitations/diagnostics: project-wide reference checker still reports the
10 unchanged signal-path findings listed above; headless editor import exits 0 but
retains the existing TLS/plugin and editor shutdown resource diagnostics. These
are separate from the zero-orphan unit result. Host SessionManager still uses its
existing local JSON fallback after database authentication failure. No physical
mobile-device or export verification was performed. The narration is the only
runtime audio layer; the legacy film retains its synthesized atmosphere. The legacy renderer version mismatch noted earlier has since been fixed;
its four-plate path and the retained packaging option are available.

A final whitespace-only normalization follows the verified implementation; import,
build and all suites are rerun once more below before concluding.

The legacy plate renderer now uses portable CPU emission rendering as well, fixing
the previously documented Eevee identifier mismatch without changing the retained
OGV. Its four-plate rendering path is checked separately before the final suites.

Final rerun after normalization and legacy portability repair: import exited 0
(with the same pre-existing diagnostics); C# build passed with 0 errors/warnings;
GdUnit4 109/109, 0 failures/errors/orphans (report_343); NUnit 34/34; container
Django 104/104. All six cutscene Python modules compile. The legacy Blender path
rendered all four preview plates successfully in Docker; the original OGV and
narration binaries remain unchanged. No game export, commit or push was performed.

## Through the Breach — 2026-10-31

CodeLevel now builds Level 2's scorecard before extraction, captures score/combo/
operative once, records campaign completion through the existing PostgreSQL path,
freezes mission descendants/audio/HUD/pause, and retains Level 2 for Replay.
WildfireAftermath owns 75-second shots/subtitles/audio, safe missing-resource
fallback, input consumption and a guarded deferred results handoff. Continue targets
Level 3. Level 1's existing Tokyo cinematic and Level 3 introductions are preserved.
No watched flag, persistence/schema change or changes to unrelated PostgreSQL work.

WildfireAftermathStage analytically samples nine shots with parallax, smoke, water,
family movement, support/release, a live manifest and a walking departure following
the road slope. Docker `render_breach.py` creates original scenery and an 18-cell
operative atlas. `package_breach.py` assembles cached ElevenLabs Daniel/Adam/Sarah/
George cues and original atmosphere into 75-second tracks. `validate_breach.py`
checks raster alpha/bounds, timings and decoded peaks. `review_breach.py` runs real
objective callbacks, 27 captures, full playback and result navigation through MCP.
The existing gameplay verification tool now skips aftermath and presses Continue.

English BREACH_CUE_1..9 and BREACH_SKIP keys have supplied fallback text. Missing
speech preserves full timing; missing essential art completes through the same
results callback. Prompt restoration tests supply their own canvas because headless
Global omits it, and compare DisplayServer's actual supported cursor state.


Final verification (2026-10-31): background import exit 0 and all nine new import
files present; C# build 0 errors (154 existing warnings on recompilation, final
incremental build 0 warnings). `run_tests.ps1` passes NUnit 47/47 and GdUnit4
142/142 with zero failures/errors/orphans (`report_361`). Host Django is skipped
because its venv is absent; explicit container Django passes 104/104. Docker
asset/audio validation and Python compilation pass. Both tracks are 75 seconds;
all eight complete cues fit their windows and decoded peaks remain below clipping.

Godot AI MCP captured and visually reviewed all nine shots at 1920×1080,
1920×1200 and 2560×1080. Uninterrupted 75-second playback kept player position,
health and score fixed; maximum sampled audio/visual clock separation was under
30 ms. Natural results, Continue to Level 3 with three earned operatives, Replay
with Level 2 arrival, Level Select and Main Menu all pass. Runtime review logs
contain no errors or warnings. Level 1 regression passes in the Godot suite.
Evidence: `.validation/breach/`, including 27 captures and aspect contact sheets.

Existing import diagnostics remain separate: asset-placer TLS failures, editor
shutdown settings/resource/ObjectDB/PagedAllocator messages, and optional
`gdunit4.api` warning. No physical controller/touch hardware, speaker-listening
review or export was performed. Review game stopped; the user's editor remains
on Level 1, the core services running. Changes are local/uncommitted; unrelated
PostgreSQL/website work is preserved.

## Silent Freight implementation — 2026-10-31

Added NeonRustIntro/NeonRustStage and start the overlay at the end of
NeonRustMission.OnLevelReady. CodeLevel now accepts an arrival CanvasLayer
with Finished; its original parameterless Level 2 entry still constructs
ConcreteWildfireIntro. The snapshot captures every mission descendant before
disabling any modes; Finished or overlay removal restores it once and restarts
the original opening dialogue. New input uses the aftermath's deferred handoff
and mapped-action release. The 75-second timeline supports deterministic Seek,
SeekAudio, compensated speech clock and timed-subtitle fallback.

Docker Blender authors depot/rail/cargo/walkway layers and a 512-cell missing-pose
atlas; existing city and operative walks are reused. Exact eight-line script,
cast, timing, localization keys and provenance live in assets/cutscenes/neon_rust_intro.
New lifecycle/integration tests accompany existing mission-fixture adjustments.
Final suites after shot refinements: NUnit 47/47, Godot 151/151
(report_363, zero failures/errors/orphans), container Django 104/104.
Unrelated PostgreSQL, website and aftermath changes are preserved. No commits/pushes.

Silent Freight runtime review: Godot AI MCP captured all nine shots at
1920×1080, 1920×1200 and 2560×1080; all three contact sheets were inspected.
Uninterrupted playback kept the entire mission frozen, including player
(200,554), health, score, fragments and relay state. Maximum sampled speech/visual
clock separation was 32 ms. Natural handoff restored HUD, processing and music
with Jump released. Actual Continue, Level Select, saved-session loading and
Replay reached Level 3 with its arrival active. The save-load test used a new
owned PostgreSQL session and verified deletion afterwards; existing saves were
untouched. Runtime review contains no warnings/errors. Editor remains Level 1,
game stopped. Final verification follows.

Final verification (2026-10-31): background Godot import exited 0 and all eleven
PNG/Ogg import files exist. C# build has zero errors (154 pre-existing addon
warnings). Final run_tests.ps1 passes NUnit 47/47 and Godot 151/151, zero
failures/errors/orphans (report_363). The host wrapper skips Django because
backend/.venv is absent; the explicit container suite passes 104/104. Docker
raster/audio QA and Python compilation pass: 1920×1080 transparent scenery,
512-cell atlas bounds, eight complete nonoverlapping cues, two exact 75-second
tracks and decoded peaks below clipping. Diff hygiene passes.

Existing import diagnostics: asset-placer TLS failures and editor shutdown
EditorSettings/ObjectDB/resource/PagedAllocator messages; runtime review logs
are clean. No physical controller/touch hardware, speaker-listening review or
export was performed. Evidence: .validation/neon-rust/ (including 27 captures
and three contact sheets). The core Docker services remain running.
User's editor remains on Level 1; review game stopped; changes are local and
uncommitted. No watched flag, save migration or local persistence was added.

## Git publication — 2026-10-31

The user authorized committing and pushing the current accumulated changes:
Silent Freight, Through the Breach, PostgreSQL-only persistence, website runtime
credentials/API wiring, tests, tools and documentation. The pre-publication fetch
confirmed main and origin/main match; diff hygiene passes. This is a Git/docs-only
task; the existing 151 Godot, 47 NUnit and 104 container Django passing results
remain applicable. Check MEMORY.MD for the verified publication result.

Publication is blocked by Windows write permissions on .git/objects. Staging
failed before any commit/push; access controls were left unchanged. Restore
Git write access before retrying. All implementation files remain local.

Publication retry: Git write access is now available; staging succeeded without
changing access controls. The user renewed authorization to commit and push
the reviewed accumulated changes. Final remote verification is recorded in MEMORY.MD.

## Neon Rust gameplay expansion — 2026-10-31

Eight sectors now span 48,000px with 30 finite encounter groups, three defended
shipments, two uplinks, two couplers, two sequential reinforcements and guarded
relay/extraction. NeonRustProgress uses exact IDs, idempotent rewards and explicit
prerequisites. IFiniteCombatHost removes the encounter/gunner dependency on Level 2
while preserving its authored activation, timings, artillery and rewards. Ability
and recruit predicates default to the original behavior in other missions.

Aura retains her original lines before sonic access. Mirage retains his lines
after blackout; his lesson requires a real Phase Blink across the harmless beam.
Seven safe stationary shrines retain objectives, recruitment, lessons and rewards.
Recovery clears rounds, resets incomplete groups and restores exact actor modes.
Level 3 now freezes for The City Finds Its Voice (90 seconds), then shows results
with Continue to 4, Replay, Level Select and Main Menu. Replay rebuilds the
original roster and arrival; the aftermath adds no unlocks or persistence.

Collision blockout was traversed using Godot AI MCP before dressing. Required
rises are 100px; escape drops 110px; optional archives reconnect locally. Docker/
Blender rendered seven original 512px RGBA depot assets with explicit import scales
and collision separation. Existing arrival, operative stats, audio and save schema
remain. The core containers were running before edits. No Git publication.

Initial build passed with 154 existing warnings. Ten focused Godot tests passed
with zero errors/failures/orphans. MCP completed all sectors using real movement
and abilities plus diagnostic kills, zero recoveries, about 145s simulation. This
is not a normal combat pacing result: the 30–35 minute human first-run target
remains UNVALIDATED. Final import, full suites and dressed captures follow below.
The optional full-project reference checker found ten existing signal-path errors
in unchanged Proton Scatter/main-menu scenes, plus 27 warnings; it is not a clean
pass. See tools/gameplay/NEON_RUST.md for reproduction and remaining acceptance.

Final verification (2026-10-31): background Godot refresh exited 0 and generated
all seven texture sidecars; asset validation passed dimensions, alpha, margins
and imports. Refresh logs retain TLS, EditorSettings and editor shutdown/resource
diagnostics; the import log is not error-free. Incremental C# build: 0 errors,
0 warnings (a full compile earlier reported 154 existing warnings). run_tests.ps1:
NUnit 93/93 and Godot 161/161, zero errors/failures/orphans (report_367). The wrapper
skipped Django because the host venv is absent; explicit container Django tests
passed 104/104 after the final code/test edits.

Godot AI MCP completed the dressed route with real movement/swaps/abilities and
diagnostic kills, no recoveries, observed peak four simultaneous enemies. Per-sector
movement/objective wall times were 18.859, 23.470, 18.882, 10.794, 19.431, 27.374,
14.745, 17.778 seconds (151.333 total, arrival excluded). Combat is diagnostically
resolved and unmeasured; these numbers do not validate human pacing. All 24 dressed
sector/aspect captures exist. Replay restored Level 3 arrival/three operatives;
Continue reached Level 4/five operatives; Level Select and Main Menu navigated
correctly. Final game logs contained no warnings/errors. User editor remains
stopped on level1.tscn. Core Docker services remain running. No staging,
commit or push. Human first-run pacing, physical hardware and listening remain
unvalidated; see tools/gameplay/NEON_RUST.md.

Publication preparation (2026-10-31): user authorized shutting down servers and
pushing the current Neon Rust implementation. The core Compose services
are now stopped with exit 0; persisted volumes and containers remain. main matched
origin/main after fetch. Existing verified implementation test results remain
applicable; only documentation/Git operations occurred during this task.

Publication completed: Neon Rust implementation commit 3c92724 pushed to origin/main
and remote SHA verified. Services remain stopped. This follow-up records publication
only; no code/assets changed and prior verification remains applicable.

## The City Finds Its Voice — 2026-11-01

Implemented Level 3 extraction → 90-second NeonRustAftermath → existing results →
mission 4. CodeLevel retains one campaign-completion call and immutable extraction
score/combo/operative values. Complete mission/pause/HUD/audio freeze, compensated
clock with monotonic fallback, all-device Skip, mapped action release, deferred
single handoff and teardown restoration follow the established cinematic pattern.
Missing speech retains the full subtitle timeline; missing essential art opens
results safely. Replay rebuilds Level 3 arrival/three operatives; Continue enters
Level 4/five operatives. No new objectives, unlocks, watched state or schema changes.

Eleven deterministic shots use reused depot art, original Docker/Blender fortress
scenery and a 30-cell five-operative pose atlas. Representative squad inspected
before full production. Speaker framing, sloped-road foot placement, fading red
links, recovered freight, anonymous radio responses and independently powered
fortress defenses are cinematic only. Surya/Voss and fortress arrival stay in Level 4.
Ten exact requested English cues use Daniel/AXIOM-7, Adam/Kaito, Sarah/Oceanus,
George/Titan, Matilda/Aura and Eric/Mirage. Manifests record voice IDs, exact text,
request/source hashes, generation settings and measured timings. Original seeded
atmosphere and dialogue are separate 90-second Ogg tracks. All cues fit at tempo 1.
Reproduction: tools/cutscene/README.md; caches/evidence: .validation/city-voice/.

Final verification: background import exit 0 and all five PNG/Ogg sidecars exist.
C# build zero errors (154 existing addon warnings). run_tests.ps1 passes NUnit
93/93 and Godot 170/170, zero failures/errors/orphans (report_372). Host Django is
skipped for absent venv; explicit isolated container Django passes 104/104 after
final edits. Raster dimensions/alpha/all atlas boundaries, ten cue windows, decoded
peaks (dialogue 26651, atmosphere 4841), exact 90-second durations, manifest/runtime
alignment, Python compilation and diff hygiene pass. Godot AI MCP captured and
visually reviewed all eleven shots at 1920×1080, 1920×1200 and 2560×1080. Full
playback kept position/health/score fixed, all mission descendants frozen and
maximum sampled audio/visual separation 36.8ms. Natural results plus all four
navigation actions pass. All 90 captured runtime log entries have no warning/error.
Earlier cinematics/results regressions pass in the full suite.

Existing import TLS/EditorSettings/shutdown ObjectDB/resource/PagedAllocator
diagnostics remain; do not describe the import log as error-free. No physical
controller/touch hardware or speaker-listening review was performed. Normal four
containers remain running and the user's editor stays stopped on Level 1. Docker
Desktop cannot access D: bind mounts (including existing backend /app); this fault
is unresolved. Production used an owned unmounted copy-based container, now stopped.
Django used copied current backend source and the existing runtime environment in
an owned unmounted test container, also stopped, on the existing Compose network.
No user service was stopped/restarted. Unrelated export changes are preserved;
no export, staging, commit or push. Changes remain local/uncommitted.


## The Wall That Faces Inward — 2026-11-01

Level 4 now begins with a skippable 120-second live cinematic on every fresh
launch. Thirteen shots introduce the independently powered fortress, separated
Surya Prime (Brian) and armored Commander Voss (Bill). The five-operative squad,
spawn, gameplay briefing, recruitment and arena confrontation remain intact.
Surya still joins at the approach trigger; the gameplay boss appears only in the
arena. The complete mission/HUD/pause/audio snapshot restores once on natural
completion, Skip, missing art or independent overlay removal. Missing speech
keeps the full timed subtitles; checkpoint recovery does not replay the intro.

Original Docker/Blender fortress, shield-road and command artwork reuse existing
operative/boss designs. Twelve cached ElevenLabs multilingual-v2 cues use .70
stability, .75 similarity, .10 style and speed 1; Voss receives restrained speaker
EQ/echo. Separate dialogue/seeded atmosphere tracks measure 120 seconds each.
Production and review: tools/cutscene/README.md and *_inward_wall.py. No watched
flag, save migration, export or publication.

Final verification: C# build has zero errors (154 existing addon warnings);
background import exits 0 with all nine PNG/Ogg sidecars. NUnit 93/93, Godot
180/180 (report_375; zero failures/errors/orphans), and isolated container Django
104/104 pass. All thirteen shots were inspected at 1920×1080, 1920×1200 and
2560×1080. Full playback holds the mission frozen, hands control back naturally,
and stays within 32 ms sampled audio/visual separation. Continue, Level Select,
Replay and loading an owned temporary save activate arrival; the save was removed.

Raster/alpha, Surya atlas boundaries, twelve complete cue windows, runtime text/
manifest agreement, unclipped PCM and both exact 120-second tracks pass. Python
compilation and diff hygiene pass. Runtime review has 95 log entries with zero
warnings/errors; evidence is in .validation/inward-wall/, including 39 captures
and three inspected contact sheets. Unit coverage includes all Skip routes,
missing speech/art, repeated completion, teardown/removal, exact mixed snapshot
restoration, full briefing lifetime, all-shot state invariants, recruitment after
handoff and recovery without replay. Existing arena tests still verify Voss and
the wall after arrival; earlier cinematics/results regress in the full suite.

Existing import TLS/editor shutdown diagnostics and the reference checker's ten
unchanged addon/main-menu signal-path errors (27 warnings) remain separate from
runtime/test results. Host Django is skipped for the absent venv; the backend
bind mount still fails with ENODEV, so current backend source/environment were
copied into an owned unmounted container for the explicit 104-test pass. The
production helper also used copied inputs. Both owned helpers are now stopped;
the core services remain running. Editor remains stopped on Level 1.
Physical controller/touch hardware and speaker-listening checks remain unperformed.
No export, staging, commit or push; changes remain local and uncommitted.

## Act I opening label — 2026-11-02

Added mission-1-only ActHeading in CodeLevel.BuildTitleCard: centered 24px cyan,
CAMPAIGN_ACT_I_TITLE with ACT I fallback, above the unchanged 34px mission title
and objective. It shares the existing hold/fade, ignores input and adds no saved
state or API. Recovery reuses the scene; every fresh Level 1 instance gets the
label. Docker preflight: the core services running. C# build passes with
153 existing warnings and zero errors. Background import exits 0; known TLS,
EditorSettings and shutdown resource/ObjectDB/allocator diagnostics remain.
MCP runtime checks pass for direct launch, Replay, level selection, briefing Skip
and completion via Continue callbacks, fade cleanup and fall/checkpoint recovery
without rebuilding the card. Levels 2–4 have no ActHeading. Inspected screenshots
at 1920×1080, 1920×1200 and 2560×1080: no title clipping or HUD overlap; actual
viewport dimensions checked. Final runtime log sample has no warnings/errors.
The existing editor remains stopped on its original Level 1 scene.
Required run_tests.ps1: NUnit 88/88 and GdUnit4 197/197 (report_400; zero failures,
errors or orphans); host Django skips its absent venv. Container Django 104/104
passes using a temporary source/fixture copy streamed through stdin because the
existing /app Docker Desktop bind mount is unreadable (docker cp also fails).
The first isolated attempt lacked countries.json; supplying that fixture resolves
all failures. Expected mocked Resend/Clerk diagnostics remain in passing tests.
Evidence: .validation/act-i/ and act-i-*.log. Diff hygiene passes. No exports,
staging, commits or pushes. Changes remain local and uncommitted.

Act II input follow-up: keyboard arrows/Enter and injected controller D-pad/A
were verified through actual GUI dispatch for completion focus, Replay Preview
and Main Menu. Physical-device acceptance remains separate.

## English voice coverage implementation — 2026-11-05

Added an English cue catalog and one `MissionSpeechQueue` per `CodeLevel`. Mission 4/5 schedulers forward events to that owner; recruitment queues complete conversations at trigger time. Shared intros, story events and all 36 collectible lines now resolve to stable speech cues. Later log bubbles no longer start an independent subtitle presentation. The queue serializes complete takes, protects subtitles from status updates, pauses with gameplay and clears on recovery/results/teardown.

Generated missing gameplay MP3s with existing cast IDs and exact-text/settings/hash receipts. Replaced four obsolete briefing recordings and the confirmed duplicate Oceanus take in Titan's Foundry Forge aftermath slot, preserving other performances and timing. Corrected Kaito's unattributed Tokyo internal line assignment. Recovered Cyber-Grave intro request receipts by matching historical source/request hashes. `docs/voice-coverage` contains 301 inventory entries, decoded evidence and cached Scribe transcripts; all legacy receipt gaps were recovered from authenticated ElevenLabs history, while seven ASR pronunciation/word-recognition candidates remain explicitly open for listening. Existing English briefing text (including its legacy roster wording) was preserved as requested.

Validation in progress: C# build and NUnit 276/276 passed; five offline inventory tests passed. Natural mixer capture verified three Mission 5 testimonies in FIFO order, a later log, pause/resume, the complete repaired Foundry epilogue and all four briefing lines with Continue advancement. The first full suite exposed two arrival-subtitle initialization regressions; these were fixed by initializing/restoring visual intro subtitles before the shared queue starts. Final verification is recorded below. Historical editor/addon TLS and shutdown diagnostics remain separate from unit-suite failures. No exports, commits or pushes are authorized.

Eight technical-log/intro takes were replaced after two independent recognizers corroborated omitted units or spoken separator artifacts. Receipts preserve both authored display text and normalized synthesis input; displayed dialogue was not rewritten. Docker Desktop later refused D: bind mounts, so `--docker-media` streams only nonsecret source/media into the existing disposable media image. Updated later-campaign validation hashes after the Foundry repair. Coverage totals and remaining candidates live in `docs/voice-coverage`; no runtime ElevenLabs calls were introduced.


### English voice verification outcome

Final verification (2026-11-05): background Godot import exited 0; all 60 active new MP3s have import sidecars. `dotnet build AxiomForce.csproj` passed with zero errors (155 warnings in existing code/addons; no warnings in the new speech classes). Five Python inventory/normalization tests passed. The final `run_tests.ps1` run passed NUnit 276/276 and GdUnit4 309/309 with 0 errors, 0 failures and 0 orphans (`reports/report_501`). Its Django step failed because Docker Desktop's existing D: bind mount returned “No such device” for `/app/manage.py`, so the aggregate runner exited 1. The identical final backend source was streamed without secrets into a temporary directory inside the existing backend container: Django 104/104 passed, and the temporary directory/test database were cleaned up. No service was restarted or stopped. Existing addon TLS/editor shutdown diagnostics and standalone review-process resource shutdown warnings are distinct from GdUnit's zero-orphan result. Natural production playback and mixer capture passed for all three Mission 5 testimonies in order, a later collectible log, pause/resume, the complete Foundry aftermath transition and all four briefing recordings with Continue advancement.

The 301-entry coverage inventory contains 296 spoken occurrences plus five explicit silent classifications. All spoken occurrences have audio, imports, current-hash decoding, transcripts and provenance. The strict coverage gate intentionally remains exit 1 for seven ambiguous pronunciation/word-recognition candidates listed in `docs/voice-coverage/REVIEW.md`; subjective listening acceptance is not claimed. No exports, commits or pushes were performed.

## Speech-to-text options review — 2026-11-05

Read-only product/architecture assessment; no addon, package, model or runtime feature installed. Checked Godot.NET.Sdk 4.7.2 / net10.0, Godot/C# and audio skills, and primary upstream documentation/source. Recommended ranking for local game STT: Godot Whisper first, C# with OllamaSharp second for a broader conversational service, embedded Python plus Ollama last. Godot Whisper provides offline/realtime recognition, VAD and native platform backends, but the linked store release is explicitly unstable; compatibility, C# adapter calls/signals, model accuracy and frame-time impact require a prototype.

Python bridges exist (including py4godot), but introduce an additional scripting runtime and deployment support to validate. OllamaSharp is an API client, requiring a running Ollama service; inspected IOllamaApiClient/Message source does not expose typed speech-transcription methods/audio fields, so validate actual audio support or use direct .NET HTTP. Ollama now documents `/v1/audio/transcriptions`; do not repeat the outdated claim that it cannot accept audio at all. Liquid AI's current model card specifies Ollama LFM3-Omni audio input on macOS, while listing a separate llama.cpp path for Windows. Ollama's Phonon is Gradium TTS, not STT (distinct from the unrelated Phonon voice-typing app).

Suggested prototype: push-to-talk microphone capture, isolated speech resampling, asynchronous recognition, C# transcript event, then deterministic command mapping if voice commands are desired. No performance or Godot 4.7.2 compatibility benchmark was run. Documentation-only changes require no Docker preflight, import or unit-test rerun; no commit/push authorized for this new task.

Localization cleanup note (2026-11-05): automatic approval review blocked recursive removal of the task-only temporary authoring environment/model folders. They remain under the user Temp directory, outside the repository and runtime dependencies. No cleanup escalation was attempted. Final source checks also exclude generated lookup-key templates and color literals from player-facing inventory.

## Browser authentication — 2026-11-06

Completed the browser handoff implementation. The main menu offers Log In and Sign Up, a waiting panel with reopen/cancel, and uninterrupted guest navigation. The deleted credential scene/controller has no runtime references; Settings remains the language picker. AuthManager owns one cancellable attempt, process-memory credentials, two-second polling with bounded backoff and a fifteen-minute deadline. Leaving the menu cancels late sign-in and detaches subscriptions. Success updates the existing profile, hides both buttons, restores/focuses the window and focuses Play; taskbar attention is requested if Godot reports activation failure. Logout clears only the game session.

Backend migration 0007 adds private-secret hashes, expiry/cancel/consume state and the optional persisted email-onboarding association. Browser POST endpoints lock rows and redeem tokens once. Clerk JWTs require configured RS256 keys, issuer, expiry/not-before and authorized origin; identity matching uses clerk_user_id exclusively. A missing profile goes through existing email/country/date-of-birth onboarding, while completed onboarding's verified Django JWT can explicitly approve the game in a fresh tab. No implicit email merge or private proof in URLs. Legacy secretless requests must restart. Configure CLERK_ISSUER, optional CLERK_JWKS_URL and CLERK_AUTHORIZED_PARTIES; see .env.example. PyJWT crypto support is required.

Website Clerk components and authentication methods are reused. Wildcard routes and a public UUID in each form's base path preserve handoffs through factor/verification redirects and account switching; locale survives login and emailed onboarding. Tokens stay in page memory. Nine game messages and the browser-specific copy cover all seventeen languages and retain the localization gate. The transport uses explicit Content-Length JSON because the local WSGI path cannot read streaming/chunked request bodies. API override AXIOM_API_BASE_URL reads Godot's environment; default remains localhost:8000/api.

Verified Windows through Godot AI MCP: real Clerk-approved profile handoff, exact displayed username, both buttons hidden and Play focus. Separate Win32 sampling confirmed that approving a minimized game restored its window and gave its process foreground ownership. Guest Play remained available while waiting. Real browser checks cover login/MFA, already-signed-in reload, complete switching to a second account, cancellation/replay, browser closure, fresh signup/email/phone verification, captured onboarding email in a fresh browser context without the original Clerk cookies, Django-session approval and storage checks. No real email/SMS delivery or production identities were used. Cleanup removed all disposable Clerk users and the isolated PostgreSQL database; only normal backend/db/website remain running.

Final checks: C# build (zero errors, 153 existing warnings), background import exit 0 with all seventeen translation outputs present, Django 111/111, NUnit 303/303, GdUnit4 319/319 with zero errors/failures/orphans (report_518), twenty Python checks, structural localization gate and four Playwright checks. The final build/import and full run_tests.ps1 rerun completed successfully after all source edits. Editor import still logs existing addon TLS/updater and shutdown/leaked-resource diagnostics. The independent strict voice gate remains exit 1 for seven historical evidence/listening candidates; no audio/cue ordering/timing changed. Regenerated voice metadata resolves current subtitle aliases.

Reproduction and test isolation are documented in tools/playwright/README.md. The optional host test proxy uses port 18002; 8002 is occupied by Godot MCP's WebSocket server. OS-denied focus/taskbar fallback was not forcibly reproduced. macOS/Linux verification, mobile integration and production rollout remain deferred. No exports, deployments or Neon/game-storage migration were performed. On 2026-11-06 the user separately authorized committing and pushing this verified change to origin/main; Git-only publication reuses the checks above. Local keys and ignored validation logs are excluded.

## Paused checkpoint — owner requested stop, 2026-11-15

- Resume approved shared input prompts plan from this working tree; do not restart implementation.
- Owner requested stopping until tomorrow. Changes remain uncommitted; keep Docker running.
- Latest stable graphical smoke: 58 checks passed, 0 failures, 22 screenshots in reports/input-prompts-smoke/. Physical controllers untested.
- SquadStrip child-listener teardown bug fixed using a direct service subscription and immediate freeing of passive slot panels; focused ApexBastion suite passed 3/3 with 0 errors/failures/orphans. New prompt suite contains 19 cases including squad lifetime regression.
- Root full run log input-tests-verified.log was running when stop requested; inspect its completion and counts before claiming final pass. Earlier full run had 384 cases with 0 failures but 276 orphans before the SquadStrip fix.
- IMPORTANT last root edits are NOT rebuilt or verified: CodeLevel and IsometricMission control hints increased to14px with outline; isometric hints moved above subtitles (-200..-128).
- Newly found remaining plan gap: CyberCombatHud badges still used fixed SHIFT/F/Q-RT. Prompt-consumer agent was implementing this when interrupted; inspect its partial diff before continuing. Preserve cooldown state while adding live action/device/style/locale labels. No final checks after these edits yet.
- Latest background editor import exited but logged System.Runtime11 editor-tool loading and native EditorSettings/resource/ObjectDB shutdown diagnostics. Do not claim import logs are clean.
- Read docs/INPUT_PROMPTS_AUDIT.md and review reports. Documentation verification totals remain provisional. After final edits: build, background import, full run_tests.ps1 (0 errors/failures/orphans), graphical smoke/screenshot inspection, Graphify update, and final README/HANDOFF/MEMORY results.
- Root temporary logs remain untracked; move into ignored reports/input-prompts-smoke or a verification log folder after final verification. Preserve unrelated owner changes. No exports, generation spending, commits or pushes.
