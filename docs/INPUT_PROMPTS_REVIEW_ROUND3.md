# Input prompts: fresh-context review, round 3

Date: 2026-11-15. Reviewer: codebase-details Goldfish. Status: completed source review.

## Findings

No new Blocker or Major findings were established in this snapshot. This is one clean source-review round, not a claim that the review loop has converged or that runtime validation is complete.

## Prior findings checked

- Main-menu and roster blanket mouse handlers are removed. The roster enables button mouse filters. `CyberButton` handles press/release, disabled controls retain their click region, and `CyberDropdown` opens, selects, and closes through GUI input.
- Scene input owners observe input before consuming shortcuts or reading device-dependent aim. The shared service does not consume input.
- `InputActionDefaults.Matches` keeps Shift+Tab separate from ordinary Tab while allowing the existing combat aliases with held modifiers. `CyberShogun` retains its original `_Input` stage and explicitly excludes mouse events, preserving the existing B/Back ordering relative to pause.
- Remap rows and instructions subscribe to presentation changes. Keyboard replacements retain controller events, and reset rebuilds runtime defaults. Home/story actions have explicit labels.
- `StoryIntro.cs:150` wraps Skip text in a wider bounded region. The Continue button already uses beginning-directed horizontal growth in `scenes/main_menu/story_intro.tscn:133`; its longer text does not imply the same right-edge overflow as the original Skip report.
- Campaign controls now occupy y=-116..-44 (`scripts/gameplay/CodeLevel.cs:782`), above the existing squad row. Pad presentation removes keyboard direct-slot hints from the squad strip.
- Cinematic call sites select the helper variant that matches their existing accepted controller shortcuts; the shared helper changes presentation and context rather than completion timing.

## Reads and evidence

Read the full approved plan at `C:/Users/karthiknk21/.codex/attachments/9bda3af4-b96f-4ed6-9be6-a41a7c226e74/approved-plan.md`, repository AGENTS.md, EGM review/review-loop skills, bundled Codex integration, codebase-details persona and implementation-readiness standard. Read the game router, Godot C#, C# essentials and interactions lesson, input-systems, and Godot UI Control skills. No conversation history or memory service was queried.

Read the three `scripts/input/*.cs` sources, prior `INPUT_PROMPTS_GOLDFISH_REVIEW.md` and `INPUT_PROMPTS_REVIEW_ROUND2.md`, `assets/translations/InputPrompts.csv`, `tests/test_input_prompts.gd`, and `tools/input_prompt_smoke.gd`. Inspected script and project diffs and relevant menu, briefing scene, pause, gameplay, world, cinematic and custom-control source. The CSV includes the new settings and input terms in the project's 17 language columns; this reviewer does not claim native-language translation quality approval.

Graphify query `InputPromptService InputPromptFormatter InputActionCatalog` returned **No matching nodes found**. Direct source was authoritative for the new implementation. Graph refresh is the Elephant's delivery step.

## Validation limits

This reviewer made no implementation edits and ran no build, import or unit suite. The report is the sole workspace change. The Elephant's fresh validation was in progress and is not counted as a failure or a completed pass here.

The smoke harness checks actual menu/settings/remap-close mouse interaction, dropdown selection, Mission 1 mouse combat and a GUI blocker, live campaign/recruitment prompts, world-map wheel/drag/waypoint input, and cinematic Skip completion. It captures English, Arabic and German layouts at standard and wide viewports. Its controller selection helper calls the service directly, and the campaign modifier tests call actor methods directly; those checks do not prove physical-device behavior or full viewport dispatch for every legacy alias. Physical controller coverage, screenshot inspection, briefing layout appearance, final full-suite results and import completion remain separate delivery evidence.
