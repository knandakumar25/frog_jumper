# Shared input prompts — implementation and verification

Approved plan implemented on 2026-11-15. This supersedes the initial read-only audit,
which found fixed Xbox labels, inconsistent remap prompts, missing axis formatting,
and mouse suppression. No new artwork, generation credits, website/backend/schema
changes, exports, commits or pushes are included.

## Current behavior

| Area | Implementation |
| --- | --- |
| Shared service | `scripts/input/InputPromptService.cs`, autoload before Global; physical binding tokens, live labels and `PromptsChanged` |
| Contexts | Each menu, platformer, isometric feature, world/map and cinematic supplies its actions; Global helpers/signals remain compatibility wrappers |
| Device selection | Fresh keys/clicks, mouse movement of at least four pixels, pad presses, active-context stick deadzones with a 0.4 floor, positive triggers above 0.5; echoes/releases/drift do not switch |
| Controller labels | Reported identity chooses Xbox/PlayStation/Nintendo; unknown pads receive position labels. Family choice changes names, never gameplay button positions |
| Styles | The Controls menu contains Automatic, Xbox, PlayStation, Nintendo and Keyboard. Keyboard forces labels while controller input remains enabled |
| Persistence | SessionManager stores only `glyph_style` in separate `input_prompt_settings`; missing/invalid values become Automatic. Active device stays in memory |
| Defaults/remaps | `InputActionDefaults` registers project/runtime actions before saved mappings; four keyboard remaps preserve pad events, modifiers and legacy numeric-array loading. Reset reconstructs runtime actions |
| Aliases | Xbox-style pad names: attack X/B, ability Y/LB, dash RB. Keyboard dash uses Shift/C. Squad cycling/direct slots and jump/pause alternatives retained. Shift+Tab cannot also cycle forward |
| Live text | Menu instructions/remap rows, campaign HUD, squad slot keys, recruitment/pods, world/map and cinematic buttons refresh for bindings/device/style/locale. New terminology has 17 locale columns |
| Mouse ownership | Menus/custom buttons/dropdowns/roster/Home accept clicks. Pause context shows the pointer. Isometric combat queues only unhandled input; platformer combat ignores mouse events |
| Cinematics | Skip labels use each scene's accepted input set. Original handlers, timing and completion ownership remain intact |

Scene `_Input` owners observe the event before consuming it: Godot dispatches scene
handlers before the earlier autoload. Prompt subscriptions live under their owners;
legacy anonymous Global callbacks were removed to avoid stale controls after transitions.

The physical `level1.tscn` currently runs **PalaceMission**, a platformer. The separate
`IsometricMission` implementation retains mouse aiming/attacks and is tested separately;
this task does not rewire campaign scenes. Using Xbox-style pad names, B is shared
by attack and the UI cancel pause shortcut; Back is shared by previous-operative
selection and the escape pause action. Their original queue-before-pause ordering remains.

## Review fixes — 2026-11-18

- **Copied clicks:** mouse clicks Godot copies from touches no longer switch prompts (`ObserveInput`) or attack in the top-down mission.
- **Top-down pause:** the pause panel has a tappable Resume button.
- **Release:** the joystick releases when hidden or when the app loses focus.
- **Detection:** text labels follow the same controller detection as the images, and USB IDs are read whether Godot reports them as numbers or text.
- **Smoke harness:** now 131 checks, 0 failures. New checks cover stick plus Jump through the viewport, phone and tablet layouts, and captures of every console set.
- Details: `HANDOFF.md` (2026-11-18) and `docs/INPUT_GOOGLE_REVIEW_2026-11-17.md`.

## On-screen touch controls — 2026-11-17

- iOS/Android builds show a virtual joystick and action buttons (`scripts/ui/touch/`). Other platforms show them after a touch. A deliberate key, controller or real mouse press hides them.
- Mouse events copied from touches no longer count as deliberate mouse use (`InputPromptBinding.DeliberateNonTouch`).
- Verification:
  - `test_touch_controls.gd`: 10/10;
  - `TouchLayoutTests`: 6/6;
  - smoke harness: 101 checks with 0 failures, including touch checks in the platformer, isometric and world scenes.
- No physical touchscreen device was used.

## Controller button images — 2026-11-17

- Controller prompts now show Kenney "Input Prompts" 1.5A (CC0) button images for Xbox 360, Xbox One, Xbox Series X|S, PS5 and Switch 2. See `assets/ui/input_prompts/README.md` and the HANDOFF entry for the same date.
- The "Controller labels" and "Styles" rows above still describe the text labels. These remain the fallback, the tooltip and the plain text that tests read.
- The Styles menu is now Automatic, Xbox 360, Xbox One, Xbox Series X|S, PlayStation 5, Nintendo Switch 2, Keyboard. Saved values 0–4 are unchanged, and 5 and 6 were appended.
- Verification:
  - smoke harness: 72 checks, 0 failures, including new checks that the menu label, the platformer controls and the cinematic Skip draw images;
  - full suites: backend 158, NUnit 492, GdUnit4 390, with 0 failures and 0 orphans.
  - There is still no physical-device test.

## Validation record

Build, full suites, graphical smoke, import diagnostics and graph refresh results
are recorded below. Test tooling is `tests/test_input_prompts.gd`,
`tests/AxiomForce.Tests/InputPromptFamilyTests.cs` and `tools/input_prompt_smoke.gd`.
The graphical harness uses an isolated preview process and writes evidence to ignored
`reports/input-prompts-smoke/`; it does not operate the owner's editor or real save.

- Build: candidate `dotnet build AxiomForce.csproj` passed with 0 errors and 151 existing nullable/obsolete/unused warnings.
- Initial targeted engine tests: 15 passed before subsequent regression additions. Pure family mapping tests: 15 passed.
- Initial full run: backend and NUnit passed. Godot found a stale menu callback and three failures (one new locale assertion, one test assuming inherited keyboard state, one old D-pad casing assertion); repairs were verified in the completed resume run below.
- Graphify: incremental local update completed, 7,799 nodes and 16,531 edges. A follow-up `InputPromptService` query returned service methods and dependencies. SQL extraction warns about the absent optional `tree_sitter_sql`; it does not affect the changed C# extraction. No model-backed graph command was used.
- Import: resources imported and process exited; the editor SDK load needed `DOTNET_ROLL_FORWARD=LatestMajor` with installed SDK 11. Native editor shutdown still reports `EditorSettings`/resource/ObjectDB allocator diagnostics. The import log is not error-free due to known engine allocator diagnostics on shutdown.

### Resume verification — 2026-11-16

- Restarted the normal Docker stack; website HTTP 200 and API health `status: ok`.
- The previous `input-tests-verified.log` ended mid-suite. A fresh full run recorded
  `reports/input-prompts-resume-tests.log`; backend passed 158/158 and NUnit 454/454.
- Final GdUnit4 result: 385/385 cases across 78 suites, zero errors, failures,
  skipped cases or orphan nodes. Full runner exited 0; XML evidence is
  `reports/report_698/results.xml`. This supersedes the incomplete and orphan-bearing
  earlier logs; no additional source fixes were needed during this resume.
- Reran the graphical harness against the rebuilt assembly: 58 checks, zero failures,
  process exited. Evidence: `reports/input-prompts-smoke/results.json`, `engine.log`
  and `resume-stdout.log`; the fresh stderr file is empty.
- Inspected menu, platformer, cinematic, map and isometric captures. The older isometric
  capture predated the final control-row fix; the fresh wide controller capture shows
  controls above the dialogue panel. Sampled English/Arabic/German text stays within
  its allocated areas. Recruitment text still includes existing English lesson prose
  in other locales; native-language review remains outside this automated validation.
- The last local graph refresh recorded 7,825 nodes and 16,562 edges; today's MCP
  lookup found the shared service and its binding/context methods. No code was changed
  during this resume, so a second import/graph extraction is unnecessary.

## Fresh-context review

### Approved Google review repairs — 2026-11-16

Owner approved all five findings in [`docs/INPUT_GOOGLE_REVIEW_2026-11-16.md`](INPUT_GOOGLE_REVIEW_2026-11-16.md).
The modal retains its source action title and translates it on opening and live
locale changes. The historical test wording and device-specific alias notation
above are corrected.

The graphical harness now sends actual synthetic controller button presses and
releases through `Viewport.push_input`, rather than calling ObserveInput directly.
It checks popup cancellation and device selection in the consuming menu handler,
a platformer primary attack while Keyboard labels are forced, and exactly-once
cinematic Skip. The six menu-refresh checks identify the instruction label and
compare its keyboard/controller text, expected action text and Xbox A binding.
German opening and Arabic live-update remap-modal checks were added.

Fresh graphical run: **64 checks, zero failures**, with empty `fixes-stderr.log`.
Evidence: `reports/input-prompts-smoke/results.json` and `fixes-stdout.log`.
The full suites were rerun in `reports/input-fixes-tests.log`; backend passed
158/158, NUnit passed 454/454, and GdUnit4 passed 385/385 test cases across 78
suites with zero errors, failures, flaky cases, or orphan nodes (`reports/report_699/results.xml`).
Current build: 151 warnings, zero errors (`reports/input-fixes-build.log`).
Background import exited but still reports the documented SDK 11 System.Runtime
loading and editor shutdown diagnostics (`reports/input-fixes-import-console.log`).
The former temporary SDK 10 workaround is absent in this environment; no SDK,
project target or global configuration was changed.

### Google EGM review round 2 — 2026-11-16

A second fresh-context Google EGM review round was executed across `codebase_details`,
`implementation_readiness`, and `style` reviewer personas.
- `codebase_details`: **Clean** (0 Blockers, 0 Majors). Finding `code-1-1` verified resolved in `MenuInputManager.cs`.
- `implementation_readiness`: **Clean** (0 Blockers, 0 Majors). Findings `evidence-1` (viewport-routed synthetic controller events) and `evidence-2` (specific menu prompt assertions) verified resolved in `tools/input_prompt_smoke.gd`.
- `style`: **Clean after calibration** (0 Blockers, 0 Majors). Findings `style-1` and `style-2` verified resolved; test suite currency and formatting calibrations applied.

Full Round 2 report: [`docs/INPUT_GOOGLE_REVIEW_ROUND2_2026-11-16.md`](INPUT_GOOGLE_REVIEW_ROUND2_2026-11-16.md). Earlier reports:
[`docs/INPUT_PROMPTS_GOLDFISH_REVIEW.md`](INPUT_PROMPTS_GOLDFISH_REVIEW.md), [`docs/INPUT_PROMPTS_REVIEW_ROUND2.md`](INPUT_PROMPTS_REVIEW_ROUND2.md),
[`docs/INPUT_PROMPTS_REVIEW_ROUND3.md`](INPUT_PROMPTS_REVIEW_ROUND3.md), and [`docs/INPUT_PROMPTS_REVIEW_ROUND4.md`](INPUT_PROMPTS_REVIEW_ROUND4.md).
Initial findings covered blocked mouse controls, input ordering, modifier behavior,
live text and internal action names. Later findings covered briefing/HUD bounds and
Home clicking. All reported implementation findings were repaired. Round 3 was clean;
round 4 found and verified the Home fix and reports no remaining Blocker/Major.
This record does not claim two consecutive clean rounds or completed physical-device validation.

## Coverage limits

Controller button events in the current graphical harness are viewport-routed
synthetic presses/releases, including actual menu, platformer and cinematic outcomes.
Family identities use deterministic mapping tests; some engine unit tests still
call the service directly to isolate formatting and filtering. Axis routing,
all gameplay actions and every scene's controller handling are not exhaustively tested.
No physical Xbox, PlayStation or Nintendo device, real cable disconnection, alternate
hardware keyboard layout, native-language translation review or touch hardware was
available. Standard/wide English, Arabic and German screenshots and pointer routing
are checked by the isolated graphical harness. Test results do not establish hardware coverage.
