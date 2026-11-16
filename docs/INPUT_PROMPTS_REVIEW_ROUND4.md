# Input prompts: fresh-context review, round 4

Date: 2026-11-15. Reviewer: codebase-details Goldfish. Status: completed source review.

## Findings and disposition

One Major was found and repaired during this review. No remaining Blocker or Major was established in the final source snapshot inspected here.

- **codebase-details-4-1 — Major, repaired:** `scripts/main_menu/GameHome.cs:95` accepted keyboard and controller input but no mouse press. The standalone Home screen therefore could not be left with a click, contrary to the plan's menu mouse interaction requirement. The scene contains decorative controls and no separate click handler. The Elephant added pressed left mouse input to the existing start condition at `GameHome.cs:105`, retaining its existing key and pad behavior, and added the matching mouse event to `scripts/input/InputActionDefaults.cs:125`. Both changes were reread and confirmed. This is a standalone Home-screen gap; `project.godot:14` starts at MainMenu, so this was not a startup blocker. A Home click runtime check remains delivery evidence for the Elephant to collect.

The EGM review-loop skill defines a clean round from the findings before that round's fixes. Round 4 therefore has zero open major findings but does not itself count as a clean round toward convergence. A fresh review of the stabilized source can establish the next clean round.

## Source checks

- MainMenu, GameHome and StoryIntro use `InputPromptBinding.Track` for prompt updates. Their previous Global device-change subscriptions have been removed. `InputPromptBinding._ExitTree` detaches its service subscription before clearing the callback, addressing the reported disposed menu Label callback path.
- `InputPromptService` initializes defaults before Global, keeps style separate from actual active device, formats keyboard/mouse/pad tokens, filters deliberate device input, and falls back after the active pad disconnects. The final snapshot's stick filtering considers current context actions, preserving their deadzone and the 0.4 floor rather than imposing unrelated platformer action deadzones on world movement.
- Keyboard replacement and saved override loading preserve pad events. Reset restores runtime defaults, and scene initialization is inert after the initial registration. Shift+Tab specificity is retained without removing combat aliases used while Shift is held.
- `CyberButton` owns mouse clicks, preserves a disabled control's click region, and activates on a matching release inside its bounds. `CyberDropdown` supports mouse opening, selection and outside-click closing. Platformer combat excludes mouse events. Isometric combat is queued through unhandled input after GUI handling.
- `scenes/gameplay/level1.tscn:3` attaches PalaceMission. The smoke harness now distinguishes the actual campaign Mission 1 platformer scene from the separately instantiated IsometricMission feature. It does not change campaign routing.
- CodeLevel controls wrap above the squad strip; IsometricMission controls wrap in a bounded bottom region. The source inspection does not establish appearance across every viewport or locale.

## Reads and graph evidence

Read repository AGENTS.md; the full approved plan at `C:/Users/karthiknk21/.codex/attachments/9bda3af4-b96f-4ed6-9be6-a41a7c226e74/approved-plan.md`; EGM review/review-loop skills, bundled Codex integration, codebase-details persona and implementation-readiness standard. Read the game router, Godot C#, C# essentials and interactions lesson, input-systems and Godot UI Control skills. No conversation history or memory service was queried, and no nested reviewers were started.

Read `scripts/input/*.cs`, round 3's report, input tests in GDScript and NUnit, and `tools/input_prompt_smoke.gd`. Inspected menu, custom control, actor, CodeLevel, IsometricMission and Global diffs and the relevant source bodies and Home/Mission 1 scene definitions.

Graphify's single-keyword `InputPromptService` query returned 49 nodes and 84 edges from a 7,799-node graph, including `scripts/input/InputPromptService.cs`, its device, context, formatting and binding methods, and InputPromptBinding. These were checked against source. Source changed during this review; those final source reads are authoritative over graph line positions.

## Validation limits

This reviewer made no implementation edits and ran no build, import, smoke run or unit suite. This Markdown report is the only reviewer-authored workspace change. Root validation in progress is neither a failure nor a completed pass here.

The inspected harness injects viewport mouse input for menu, isometric combat, map and cinematic checks. Controller presentation selection calls ObserveInput directly, so it is not proof of physical controller behavior. The tests exercise device filtering, family labels, keyboard save/reload, reset and modifier behavior, including the added current-context deadzone case. Runtime results, screenshot inspection, Home click coverage, physical controller testing and final full-suite/import evidence remain separate from this source review.
