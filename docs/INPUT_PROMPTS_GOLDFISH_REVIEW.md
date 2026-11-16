# Input prompts implementation review

Date: 2026-11-15. Reviewer: fresh-context Goldfish, using the codebase-details and implementation-readiness personas. Status: completed initial implementation audit; findings below describe the initial reviewed snapshot. The Elephant is applying fixes concurrently. This report does not claim final validation or review-loop convergence.

## Reads and scope

- Read repository `AGENTS.md`, the full approved plan at `C:/Users/karthiknk21/.codex/attachments/9bda3af4-b96f-4ed6-9be6-a41a7c226e74/approved-plan.md`, EGM review/review-loop skills, the installed EGM Codex integration, both assigned persona files, and `implementation-readiness-standard.md`.
- Read the game router, Godot C#, Godot C# essentials, its interactions lesson, input-systems, and Godot UI Control skills.
- Read all three new `scripts/input/*.cs` files, the `scripts/` and `project.godot` diff, and `tests/test_input_prompts.gd`. Confirmed relevant behavior in the menu, settings, cinematic, isometric, platformer, pod/recruitment, and world/map source.
- Queried Graphify for `InputPromptService PromptFormatter InputBindingResolver InputContext`. Result: no matching nodes. The new implementation was not represented by that query; source files were authoritative.
- No implementation edits, builds, tests, art generation, database operations, or exports were performed by this reviewer. Writing this report is the sole workspace mutation. Localization resource integration and delivery documents were in progress and were excluded as requested.

## Findings

### input-review-1 — Major: menu mouse suppression remains

Scope: `scripts/main_menu/MainMenu.cs:348` and `scripts/main_menu/CharacterSelection.cs:57`, `:121` (initial snapshot).

Both screens still mark every mouse event handled in `_Input`, before GUI controls can receive it. The roster also explicitly hides the pointer. Removing Global's blanket handler therefore does not enable main-menu/settings/roster clicking. This directly misses the approved menu mouse requirement. Remove these screen-wide blocks, let GUI controls own clicks, and apply the shared menu context/pointer policy. Verify actual menu, roster, and remap confirmation clicks.

### input-review-2 — Major: settings dropdowns explicitly reject clicks

Scope: `scripts/ui/CyberDropdown.cs:111`.

`_GuiInput` calls `AcceptEvent()` and returns for every mouse event without calling `TogglePopup()`. Language and display-mode controls using this widget remain inaccessible by mouse even after finding 1 is fixed. Accept a deliberate left-button press to open the popup and retain appropriate outside-click close behavior. Verify both opening and choosing a value.

### input-review-3 — Major: device observation follows scene consumption and aim reads

Scope: `scripts/input/InputPromptService.cs:59`; `scripts/actors/IsometricOperative.cs:57`; `scripts/main_menu/GameHome.cs:107`; `scripts/main_menu/StoryIntro.cs:315` (initial snapshot).

Godot dispatches `_Input` in reverse scene-tree order. The service autoload is earlier than scene owners. Home consumes every deliberate key/pad press; cinematic handlers also consume accepted shortcuts. Such events can stop before reaching the service. Separately, the isometric actor reads `UsingController` in its own `_Input` before the service updates it, so the first event after a device switch can retain the previous aim source until another input event. Ensure observation occurs before those reads and before scene-level consumption. Add integration assertions using actual event propagation, not only direct `ObserveInput` calls.

### input-review-4 — Major: exact modifier matching rejects existing combat combinations

Scope: `scripts/input/InputActionDefaults.cs:25`; callers in `CyberShogun._UnhandledInput` and `IsometricOperative.IsGameplayPress` (initial snapshot).

The initial matcher requires modifier equality on every key binding. Previously F, Q, and C worked through raw key checks while Shift was held. After dashing with Shift, pressing F or Q while still holding Shift no longer queues attack/ability. Only Tab versus Shift+Tab needs this distinction under the approved plan. Preserve unmodified combat aliases under incidental modifiers while retaining deliberate chord specificity. Verify Shift+F/Q/C and forward/reverse Tab separately. The Elephant changed the matcher during review; this finding requires final regression validation.

### input-review-5 — Major: visible instructions do not follow device or locale

Scope: `scripts/main_menu/MenuInputManager.cs:257`; `scripts/main_menu/MainMenu.cs:311`.

Instructions use `GetBoundKeysString`, which forces `Binding(action, false)`, so controller users continue seeing keyboard hints. The shared prompt subscription calls `UpdateLocalization`, but that method does not refresh `UpdateInstructionsUI` or rebuild/refresh visible dynamic controls rows. New prompt-style labels/options and existing formatted binding strings can therefore retain the old locale until reopening the popup. Refresh visible instructions from the active presentation device, and refresh dynamic text without discarding an in-progress remap or focused control.

### input-review-6 — Major: global banner exposes internal action names

Scope: `scripts/input/InputPromptService.cs:124`, `:337`; `scripts/main_menu/GameHome.cs:57`; `scripts/main_menu/StoryIntro.cs:104`.

`GetContextText` uses `ActionLabel`, whose fallback is the raw action identifier. The new home and story contexts supply `home_start`, `story_advance`, and `story_skip`, which have no label cases. The visible global banner consequently displays these identifiers instead of localized action descriptions. World/isometric context identifiers have the same gap if their banner is shown. Supply explicit localized labels for every shipped context action, or carry localized display labels in the context contract.

## Verification limits

This was a source audit, not a physical-controller or visual-layout test. The new direct-service tests cover formatting, styles, disconnection, drift, reset, persistence representation, and locale signal emission, but do not exercise consumed scene events, first-event aim switching, actual menu clicks, dropdown interaction, or live text in an open popup. Final review should assess the repaired files and the Elephant's runtime/build/test evidence. No blocker was established; the six Major findings made the initial round not clean.
