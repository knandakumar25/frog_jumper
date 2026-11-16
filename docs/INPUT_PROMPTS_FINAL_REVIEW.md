# Shared input prompts: final source review

Date: 2026-11-15. Reviewer: codebase-details Goldfish. Status: completed source audit.

## Open findings

Clean source audit: no remaining concrete Blocker or Major was established in the inspected implementation. This is a source-review result, not a claim of EGM protocol convergence, successful pending validation, or hardware validation.

The previous round's Home mouse finding remains repaired: `scripts/main_menu/GameHome.cs:105` accepts a pressed left mouse button and `scripts/input/InputActionDefaults.cs:125` advertises the matching binding. No implementation repair was made during this review.

## Source evidence

- `project.godot:23` registers InputPromptService before Global. `scripts/input/InputActionDefaults.cs:9` initializes shared project/runtime defaults once; `:17` reconstructs both on reset. Keyboard remapping does not call scene registration to restore replaced keys.
- `scripts/input/InputPromptService.cs:68` observes deliberate device events without consuming them. Its `MeaningfulMotion` at `:89` checks current-context actions and the 0.4 floor; triggers use positive input above 0.5. The disconnect path at `:112` changes presentation only for the active controller.
- `scripts/input/InputPromptService.cs:144` separates presentation style from active-device state and stores glyph_style in input_prompt_settings. Capture/load at `:187` and `:204` preserve pad and mouse events, chord flags and legacy numeric saves. Tokens at `:247` expose physical identities and current labels, with Unbound fallback. Family names at `:340` follow physical button positions.
- `scripts/input/InputPromptBinding.cs:13` attaches refresh ownership beneath the consuming node. Its `:38` exit handler detaches the service callback and restores a cinematic context only when that context is still current. Menu consumers now use these owned subscriptions instead of anonymous callbacks from Global.
- `scripts/input/InputActionDefaults.cs:25` preserves Shift+Tab specificity while allowing combat aliases during held modifiers. `scripts/actors/CyberShogun.cs:217` uses named actions and excludes mouse events from platformer combat. Isometric combat enters through unhandled input in `scripts/actors/IsometricOperative.cs:64`, after GUI controls can claim clicks.
- The custom button and dropdown diffs restore mouse ownership, including disabled button click regions, release-inside activation and popup closing. The platformer pause menu saves/restores prompt context so mouse menu use has a visible pointer.
- CodeLevel, recruitment, pod, world/map and cinematic consumers use current bindings. The cinematic changes retain the existing Skip handlers and timing. The physical level1 scene still attaches PalaceMission; the audit and smoke harness explain the separate IsometricMission implementation without changing campaign routing.

## Read coverage and graph evidence

Read repository AGENTS.md and the complete approved plan at `C:/Users/karthiknk21/.codex/attachments/9bda3af4-b96f-4ed6-9be6-a41a7c226e74/approved-plan.md`. Read the EGM review/review-loop guidance, Codex integration, codebase-details persona and implementation-readiness standard. Read the game router, Godot C#, C# essentials and bundled interactions lesson, input-systems and Godot UI Control guidance.

Read all three `scripts/input/*.cs` files, the scripts/project.godot diff, `tests/test_input_prompts.gd`, `tests/AxiomForce.Tests/InputPromptFamilyTests.cs`, `tools/input_prompt_smoke.gd`, `docs/INPUT_PROMPTS_AUDIT.md` and `docs/INPUT_PROMPTS_REVIEW_ROUND4.md`. Inspected relevant source bodies for the changed input routes, prompt consumers and custom controls. No memory contents or conversation history were retrieved, and no nested agents were started.

The single-keyword Graphify query InputPromptService returned 49 matching nodes from a 7,799-node graph. The 1,000-token response showed 25 and explicitly truncated the remaining 24. The visible results identified the shared service, its device/context/binding methods and input binding helper. Source reads confirmed the reviewed behavior. The graph predates the final small edits and its line positions are not authoritative.

## Verification limits

This reviewer ran no build, import, smoke session or unit suite and inspected no screenshots. Root validation was in progress when this review was delegated; its final outcome must be recorded separately. This report is the only reviewer-authored workspace change.

The harness sends mouse events through the viewport but directly calls ObserveInput to select controller presentation; that is not proof of controller gameplay event routing or physical pad behavior. The unit tests cover filtering, family labels, binding formatting, modifiers, keyboard round trips, reset, locale changes and repeated bootstrap initialization. Their source alone does not establish a passing runtime result.

No claim is made here about physical Xbox, PlayStation or Nintendo devices, real disconnection, alternative hardware keyboard layouts, touch hardware, native-language translation quality, or appearance at every viewport size. The supplied audit records these limits separately from automated coverage.

## Runtime follow-up within the same audit

The Elephant subsequently reported two runtime findings and requested source verification of their repairs. This follow-up is part of the same audit; it is not another clean review round and does not establish protocol convergence.

- **Campaign controls obscured by subtitles — repaired in source.** `scripts/gameplay/CodeLevel.cs:783` now places the wrapping controls in the bottom-relative vertical band -200 through -128. The subtitle panel at `:802` starts at -116, leaving a 12-pixel gap between the allocated rectangles; the squad strip occupies -34 through -8. This removes the prior geometric overlap. Final screenshots must still establish that localized text fits within its allocated band.
- **SquadStrip listener removed by its own rebuild, with orphan nodes — repaired in source.** `scripts/ui/SquadStrip.cs` now subscribes Rebuild directly to InputPromptService in Bind and detaches it in _ExitTree. Rebuild therefore no longer destroys a child InputPromptBinding that owns its own active callback. It immediately frees its passive slot panels rather than leaving detached queued panels during teardown. The inspected call site invokes Bind once after adding the strip to CodeLevel; no repeated binding path was found.

The Elephant reports that the earlier full run had 384 cases with no failures but 276 orphans, and that the repaired targeted ApexBastion run passed three tests with zero errors, failures and orphans. These are caller-reported results, not tests executed by this reviewer. The final full suites and graphical smoke were rerunning at the time of this follow-up. Their results and final screenshot inspection remain delivery evidence to record separately.

No additional concrete open Blocker or Major was established in the follow-up source inspection. This reviewer changed only this Markdown report.
