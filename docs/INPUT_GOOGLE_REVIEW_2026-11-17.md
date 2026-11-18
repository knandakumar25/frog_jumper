# Goldfish Annotated Review Report: touch controls and controller button images

Status: resolved 2026-11-18. The owner asked to fix all findings.
- All PROPOSE FIX items were applied: code fixes, tests, and the HANDOFF, README and assets-README rewrites. The six DECLINE items stay recorded only.
- No second review round has been run, so convergence is not claimed.
- The applied changes are summarized in HANDOFF.md, "Google EGM review fixes … 2026-11-18".

**Target:** `HANDOFF.md` (lines 3–114: "On-screen touch controls for iOS and Android — 2026-11-17" and "Controller button images — 2026-11-17"). The style reviewer also read the matching README.md sections and `assets/ui/input_prompts/README.md`.

**Skills:** `egm-review-loop` (one round, direct dispatch) and `egm-resolve`.

| Reviewer | Standard | Status | Result |
|---|---|---|---|
| `codebase_details` (cb) | re-implementation | completed | 3 Blockers, 6 Majors, 10 Nits |
| `implementation_readiness` (ir) | implementation readiness | completed | 0 Blockers, 7 Majors, 5 Nits |
| `style` (st) | style persona | completed | 0 Blockers, 12 Majors, 16 Nits |

- Reviewers ran as fresh-context subagents, read files only and ran nothing.
- I checked the code claims marked "confirmed" below against the source myself.
- This is one round, not convergence. The round is **not clean**: it has Blockers and Majors.

## Summary of suggestions

- **Code fixes proposed (10):**
  - touch-only resume in the top-down mission;
  - taps attacking in the top-down mission;
  - taps flipping prompts to keyboard;
  - the joystick staying pressed while hidden;
  - mobile hint text before the first touch;
  - controller ID parsing;
  - text and images using different detection;
  - minimum physical button size;
  - world help lines showing over the map;
  - four small code nits.
- **Document fixes proposed:** the handoff overstates several things — desktop mouse testing, multi-touch, safe area and "never overlap". It also misstates RecruitBeat, lacks a device-test checklist and mobile-export facts, mixes names, and buries the secret-rotation decision.
- **Contested:** 0. **Declined (Nit, recorded only):** 6.

---

## Blockers

### [cb-1] `codebase_details` – Blocker – A real mouse cannot use the touch buttons on desktop
* **Affected Scope:** [HANDOFF.md L15](../HANDOFF.md#L15) ("accepts a real left click for desktop testing")
* **Discrepancy:** `TouchControls._Input` runs before GUI input and treats a real mouse press as "hide" (`TouchLayout.cs` `TouchVisibility.Classify`). The button disappears before the click reaches it. `test_touch_controls.gd` itself asserts that a real click hides the controls.
* **AI Suggestion:** PROPOSE FIX (document)
* **Justification:** Confirmed. Hiding on real mouse use is the owner's rule, so the code is right and the claim is wrong. The button's mouse path is only reachable from direct test calls.
* **Proposed Edit:** L15: replace "and accepts a real left click for desktop testing" with "(its left-click path is only reachable from tests; a real mouse press hides the controls by design)". Same in the code comment in `TouchActionButton.cs`.

### [cb-2] `codebase_details` – Blocker – Touch-only players cannot resume the top-down mission after pausing
* **Affected Scope:** [HANDOFF.md L46-L50](../HANDOFF.md#L46-L50) (limits); code `IsometricMission.cs:142-149, 321-327, 478-486`
* **Discrepancy:**
  1. The touch Pause button pauses the tree.
  2. The touch controls hide while paused.
  3. The pause panel is only the text "Press ESC / START to resume", with nothing to tap.
* **AI Suggestion:** PROPOSE FIX (code)
* **Justification:** Confirmed: `_pauseLabel` is a plain label and there is no button. On a phone without a pad this soft-locks the mission. The platformer pause menu has a Resume button, so only the top-down mission is affected.
* **Proposed Edit:** add a tappable "Resume" button (`CyberButton`/`Button`, ProcessMode Always) to `_pausePanel` that calls `SetPaused(false)`. Change the label to the existing translated pause text. Add a GdUnit test: pause via the `iso_pause` action, press Resume, and the tree is unpaused.

### [cb-3] `codebase_details` – Blocker – HANDOFF says RecruitBeat uses `ContextGlyph`; it does not
* **Affected Scope:** [HANDOFF.md L86](../HANDOFF.md#L86)
* **Discrepancy:** `RecruitBeat.cs:99-112` builds marked bindings inline with `WF_TOUCH_ABILITY`. Only Names, NeonRust, ConcreteWildfire and Palace call `ContextGlyph`.
* **AI Suggestion:** PROPOSE FIX (document)
* **Justification:** Confirmed. The behaviour is intended (RecruitBeat already had its own touch key), but the record is wrong.
* **Proposed Edit:** L86: "`RecruitBeat` builds marked bindings itself (`InputPromptService.MarkedBinding`, touch text `WF_TOUCH_ABILITY`); `NamesWithoutNumbersMission`, `NeonRustMission`, `ConcreteWildfireMission` and `PalaceMission` use `ContextGlyph`."

## Majors: code

### [cb-M5 + ir-M2] `codebase_details`, `implementation_readiness` – Major – In the top-down mission, any tap outside the buttons attacks
* **Affected Scope:** HANDOFF L31; code `InputActionDefaults.cs:91`, `IsometricOperative.cs:64-73`
* **Discrepancy:** `iso_primary_attack` is bound to left mouse on any device (`Device = -1`). On a phone, Godot turns a tap into a copied ("emulated") left click, which reaches `_UnhandledInput` and attacks. The smoke harness cannot see this, because `push_input` does not create emulated clicks.
* **AI Suggestion:** PROPOSE FIX (code + document)
* **Justification:** Confirmed in source.
* **Proposed Edit:** in `IsometricOperative._UnhandledInput`, ignore mouse events whose `Device == InputEvent.DeviceIdEmulation`; attacks then come from the Attack button. Add a GdUnit case: an emulated left click does not request an attack, and a real click still does. Record it in HANDOFF L31.

### [cb-M6 + ir-M2b] `codebase_details`, `implementation_readiness` – Major – Every tap flips prompts to keyboard text
* **Affected Scope:** HANDOFF L30; code `InputPromptService.cs:76-78`
* **Discrepancy:** `ObserveInput` still treats emulated mouse presses as mouse use. Every tap sets `UsingController = false` and changes the mouse mode. With a Bluetooth pad on a tablet, prompts flip to keyboard text on each tap.
* **AI Suggestion:** PROPOSE FIX (code + document)
* **Justification:** Confirmed. Only `DeliberateNonTouch` was fixed.
* **Proposed Edit:** in `ObserveInput`, return early for `InputEventMouse` with `Device == DeviceIdEmulation`. Add a test. Update L30 to say both paths ignore emulated clicks.

### [cb-M1 + ir-M3] `codebase_details`, `implementation_readiness` – Major – The joystick can keep pressing directions while hidden, and may then ignore new touches
* **Affected Scope:** HANDOFF L17, L28; code `TouchControls.cs` (`OnStick`, `ReleaseAll`), `go_joystick.gd` (`_touch_index`, `_end`)
* **Discrepancy:** `ReleaseAll` sends zero strengths once, but leaves GoJoystick's own `_touch_index`/`_active` set. If a thumb stays on the stick when the controls hide (Map, Pause or a key), later drag events can still emit `moved` and re-press directions while the map is open or the game is paused. After the controls reappear, a new finger is ignored until the old touch releases.
* **AI Suggestion:** PROPOSE FIX (code + document)
* **Justification:** Confirmed by reading: `OnStick` does not check `Shown`, and nothing calls the joystick's `_end()`. Godot's exact routing of a held touch to a hidden control was not run.
* **Proposed Edit:**
  - In `OnStick`, ignore non-zero vectors while `!Shown`.
  - In `UpdateShown` on hide, call the joystick's `_end()` when `is_active()`.
  - Release everything on `NotificationApplicationFocusOut` and `NotificationWMWindowFocusOut`. On focus loss, also reset `TouchActionButton._pointer`.
  - Add tests.
  - L17: "no button or joystick direction stays pressed after hide, pause, focus loss or exit".

### [cb-M2] `codebase_details` – Major – On iOS/Android the platformer shows keyboard hints until the first touch
* **Affected Scope:** HANDOFF L5; code `CodeLevel.cs:156, 259-267`
* **Discrepancy:** `_usingTouch` starts `false`, while the touch controls start visible on mobile. The controls bar shows key/pad text beside the touch buttons until the first touch.
* **AI Suggestion:** PROPOSE FIX (code)
* **Justification:** Confirmed.
* **Proposed Edit:** set `_usingTouch = TouchVisibility.IsMobileBuild()` at start-up, then refresh the hint; also refresh the hint from `TouchControls.ShownChanged`. Test with an injected "mobile" flag.

### [ir-M6 gap 1] `implementation_readiness` – Major – Controller USB IDs may never be read
* **Affected Scope:** HANDOFF L106; code `InputPromptService.cs:295-296`
* **Discrepancy:** IDs are only accepted when Godot reports them as Int or Float. The reviewer believes Godot's joypad drivers store them as decimal strings. If so, detection is always name-only.
* **AI Suggestion:** PROPOSE FIX (code)
* **Justification:** I can't confirm the type without a controller, but accepting strings costs nothing and covers both cases.
* **Proposed Edit:** accept `Variant.Type.String` via `int.TryParse`. Note in HANDOFF that the type is unverified.

### [cb-M4] `codebase_details` – Major – Images and text labels use different detection
* **Affected Scope:** HANDOFF L67-L70; code `InputPromptService.cs` (`Family()` vs `DetectActiveSet()`)
* **Discrepancy:** Images use USB IDs then name; text and tooltips use the name only. A Sony pad with a generic name gets PS5 images with "South face" text.
* **AI Suggestion:** PROPOSE FIX (code)
* **Justification:** Confirmed.
* **Proposed Edit:** in `Family()`, when the active set is not `None`, return `ControllerGlyphCatalog.FamilyOf(set)`; otherwise use the name. Add a test.

### [ir-M4] `implementation_readiness` – Major – Small and odd screens are unverified, and phone buttons may be too small
* **Affected Scope:** HANDOFF L21-L22, L38, L43; code `TouchControls.ApplyLayout`
* **Discrepancy:**
  - Only 1920×1080, 2560×1080 and the 1920×1920 headless view were checked.
  - Size comes from viewport height, so on a landscape phone the main buttons are about 7 mm and the secondary ones about 5 mm, below the usual 48 dp target.
  - The safe-area path never runs in tests.
  - The idle joystick ring is drawn over the touch hint text in `touch_platformer_english_2560.png`.
* **AI Suggestion:** PROPOSE FIX (code + document)
* **Justification:** Valid. The screenshot overlap is visible in the capture.
* **Proposed Edit:**
  - Use a minimum physical size from `DisplayServer.ScreenGetDpi()`: main buttons at least 10 mm, secondary at least 8 mm, capped so everything still fits.
  - Move the touch hint above the joystick zone while touch controls show, or hide it after the first touch.
  - Add GdUnit layout checks at 1280×800 (16:10), 1024×768 (4:3) and 2340×1080 (19.5:9).
  - Record the untested safe-area path.

### [world map, cb-N6] `codebase_details` – Nit (with code fix) – World help lines reappear over the open map
* **Affected Scope:** HANDOFF L28; code `NeoKyotoWorld.cs:881-883, 1044`
* **AI Suggestion:** PROPOSE FIX (code)
* **Justification:** Suppression fires `ShownChanged(false)`, which shows the help lines again over the map.
* **Proposed Edit:** hide the help lines while either the touch controls show or the map is open.

## Majors: document

### [ir-M1 + st-M1] – Major – Touch verification is mostly synthetic, and the record reads as end-to-end
* **Affected Scope:** HANDOFF L13, L38-L42, L47; README "Several fingers work at once"
* **Discrepancy:** The tests call `_GuiInput` directly and emit the joystick's `moved` signal. The joystick's own drag handling and stick-plus-button together are untested. The smoke harness's `push_input` skips touch-to-mouse emulation.
* **AI Suggestion:** PROPOSE FIX (document; one test)
* **Proposed Edit:**
  - Rewrite the verification bullets to say exactly what each test drives.
  - Change "multi-touch works" to "two touches are tracked separately (synthetic events only)".
  - Add one smoke case that holds the stick (index 0) and taps Jump (index 1) through `push_input`.

### [st-M2, cb-N9] – Major – Safe-area claim stronger than the evidence
* **Proposed Edit:** L22: "places controls inside `DisplayServer.GetDisplaySafeArea()` with a 16 px margin; ignored if implausibly small; not run on a device with a notch, and the follow-mode joystick circle can draw past the edge."

### [st-M3] – Major – "Neighbours never touch" is universal
* **Proposed Edit:** L20: "a ring radius chosen so neighbouring buttons do not overlap (checked by `test_touch_controls.gd` at the 1920×1920 headless view and in the 1920/2560-wide captures)."

### [st-M4, st-M5] – Major – README overclaims phone play and Bluetooth button images on phones
* **Proposed Edit:**
  - README touch intro: "iOS and Android builds show on-screen controls (not yet tested on a device; see Limits)."
  - Bluetooth line: "Controllers connected to a phone use the same input and image code as desktop; untested on a phone."

### [st-M6, st-M7] – Major – Show/hide rule ambiguous; "real/emulated mouse" undefined
* **Proposed Edit:** L5 and README: "On iOS/Android the controls start visible; elsewhere they appear after the first touch. On every platform a keyboard, controller or real-mouse press hides them, the next touch shows them, and they hide while paused. (Godot copies each touch into a fake 'emulated' mouse event, `InputEvent.DeviceIdEmulation`; a 'real mouse' is a physical mouse.)"

### [st-M8] – Major – One mode has three names; Pause placement differs between files
* **Proposed Edit:**
  - Use "top-down (isometric) mission" once, then "top-down".
  - List each layout as "arc: … ; row: …" in both files. For example, "Top-down: arc Attack, Ability, Dash, Use; row Pause." "World: arc Attack, Dodge, Guard, Interact, Run, Bike; row Map, Squad."

### [st-M9, ir-N1] – Major – Smoke counts and paths unclear
* **Proposed Edit:** "Graphical smoke harness (`tools/input_prompt_smoke.gd`, results in `reports/input-prompts-smoke/results.json`): 101 checks, 0 failures. That is 72 after the button-image work, plus 29 touch checks (named list)." Name the screenshot folder.

### [st-M10, ir-M7 part] – Major – Plan file not findable
* **Proposed Edit:** drop the pointer to the Claude plans folder; the owner decisions are already listed in the section. Optionally copy the plan to `docs/plans/2026-11-17-controller-images-and-touch.md`.

### [st-M11] – Major – Secret-rotation decision is buried in the controller-images section
* **Proposed Edit:** move it to the 2026-11-16 secrets section: "Owner decision 2026-11-17: the Google OAuth client ID and secret (alerts #1, #2) are abandoned and will not be rotated."

### [st-M12, ir-M6 gaps 2–3] – Major – Controller-images README has no limits; test coverage of detection overstated
* **Proposed Edit:** add a README Limits bullet:
  - no physical controller tested;
  - if USB IDs are not read, an Xbox 360 pad with a generic name shows Series art (View/Menu instead of Back/Start);
  - all image checks used forced styles, and only Xbox Series art was seen on screen; PS5, Switch 2, 360 and One art was never captured in place;
  - the dropdown id mapping is untested;
  - some screens remain text.

  Add a smoke capture per forced set.

### [ir-M5] `implementation_readiness` – Major – The joystick zone may block HUD taps; no suppression during dialogue/recruit/death panels
* **AI Suggestion:** PROPOSE FIX (document + audit)
* **Justification:** The zone (lower-left 32%×45%, layer 20) stops all pointer input. I found no tappable HUD controls there in the platformer, but no audit covered all screens.
* **Proposed Edit:**
  - Record as a limit.
  - Add a smoke check that no visible `BaseButton` on lower layers intersects the joystick zone in the three modes.

### [ir-M7] `implementation_readiness` – Major – Follow-up steps not concrete; mobile-export facts missing
* **Proposed Edit:** add a "Device test checklist" covering:
  - stick plus button together;
  - tapping the top-down field (cb-M5);
  - pause and resume (cb-2);
  - backgrounding the app;
  - notch phone, 4:3 tablet and small phone;
  - Bluetooth pad plus touch.

  Also record:
  - the export command: `docker compose run --rm export android|ios`;
  - verified preset facts: Android signs release with the debug keystore (`export_presets.cfg:389-391`) and has the placeholder package name; iOS is `export_project_only=true` (line 513);
  - an owner and step for native review of Run/Bike/Squad.

## Contested

None. I first planned to contest ir-N3's "Squad always visible in world". On checking, `NeoKyotoWorld.cs:471-476` only cycles `OperativeRoster.Current`, and the world can be reached after Mission 1 with one operative. So that part is PROPOSE FIX: show the world Squad button only when `Unlocked.Count > 1`, as in the platformer.

## Nits

| ID | Finding | Suggestion |
|---|---|---|
| ir-N3 (rest) | World map panning is right-drag only, so unusable by touch; sliding a finger onto a button does not press it | PROPOSE FIX (document limit); map one-finger pan is a separate task |
| ir-N2 | Labels captured only in English/Arabic; `ToUpper()` is culture-dependent (Turkish i) | PROPOSE FIX: use `ToUpperInvariant()`; record the font-coverage limit |
| ir-N5 | Xbox One name check matches "one" inside other words | PROPOSE FIX: match the word "one" |
| ir-N4, cb-N1 | `SetButton` clears the accessible name; tooltip/min width go stale when switching back to text | PROPOSE FIX: clear tooltip/meta on the text path; record the accessible-name limit |
| cb-N2 | `Upgrade` comment says it returns the label itself if rich; false | PROPOSE FIX: correct the comment |
| cb-N7 | Every tap re-lays out the touch controls | PROPOSE FIX: `SetButtonVisible` returns early when unchanged |
| cb-N10 | Release on pause is done by `TouchControls`, not the button | PROPOSE FIX (document) |
| cb-N5 | `xbox` feature tag forces Series art for all pads if that preset is used for PC builds | PROPOSE FIX (document note) |
| cb-N3 | GenePod draws only the first binding's image | DECLINE |
| cb-N4 | Missing Microsoft product IDs (Elite 2 BT, Adaptive) | DECLINE: name fallback gives identical art |
| cb-N8 | Platformer stick drifts sideways when pushed mostly up | DECLINE: the 0.15 dead zone on length is intended for a follow-thumb stick |
| cb-M3 | Missing constants (sizes, radii, margins, dead zones) | DECLINE in HANDOFF: they are in the code's comments; the handoff will cite `TouchControls.ApplyLayout` and `TouchLayout.StickStrengths` instead |
| st-N1 – st-N16 | Undefined terms (engine-light, XInput, Double folder, orphans, FOLLOW mode, glyph), inconsistent set/button names, cryptic test names, missing full style-value map (0 Automatic … 6 Xbox One), inference stated as fact (UID message), unbackticked class names, API list without descriptions | PROPOSE FIX: one editing pass over both HANDOFF sections, both README sections and the assets README, using the reviewer's rewrites |
| ir-N1 | Screenshot folder and smoke log not named | PROPOSE FIX (with st-M9) |
| (none) | — | DECLINE count: cb-N3, cb-N4, cb-N8, cb-M3 (doc), plus 2 style nits on wording preference (st-N13 tense, st-N16 "map help" vs "world map") |

## Counts

- Proposed fixes: 3 Blocker items (1 code, 2 document), 16 Major items (8 with code changes) and most Nits.
- Contested: 0. Escalated: 0. Declined: 6.
