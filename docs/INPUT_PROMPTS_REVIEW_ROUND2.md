# Input prompts: fresh-context review, round 2

Reviewed 2026-11-15 against the full approved plan at `C:/Users/karthiknk21/.codex/attachments/9bda3af4-b96f-4ed6-9be6-a41a7c226e74/approved-plan.md`.

## Current findings

### round2-1 — Major: briefing Skip text grows beyond the right edge

Scope: `scripts/main_menu/StoryIntro.cs:146` and `scenes/main_menu/story_intro.tscn:138`.

The briefing Skip button now displays every accepted alias. On an Xbox-style controller that includes Start/Menu, Back/View, Y and B. The scene still places the button at horizontal offsets -160 to -40 from the right edge, with no wrapping and the default horizontal growth toward the end. Its text minimum width can therefore grow beyond the viewport. The common `InputPromptBinding.Cinematic` sizing changes do not apply to this separate briefing screen. Give both briefing buttons bounded wrapping layouts and check the Skip and Continue bounds with controller labels and long/RTL text. This conclusion is based on source; runtime screenshots were pending during review.

### round2-2 — Major: campaign hints share the squad row

Scope: `scripts/gameplay/CodeLevel.cs:778` and `:790`.

The controls hint now spans almost the entire viewport and wraps inside y=-76..-10. The squad strip occupies the bottom-right y=-34..-8. Wrapped alternatives can use the same space as the squad. The previous restricted hint width avoided that intersection. Reserve separate vertical space and verify long controller labels. The Elephant was notified and is making this repair; this report does not claim the repair has already been checked.

## Finding repaired during this review

The initial round-2 snapshot moved `CyberShogun` combat/squad handling to `_UnhandledInput`. The later pause-menu handler receives B (`ui_cancel`) and Back (`escape`) first and consumes them, preventing the shipped B attack and Back squad aliases from reaching the actor. The Elephant restored the actor's original `_Input` stage with named-action matching and explicit mouse exclusion. The later source read confirms that repair at `scripts/actors/CyberShogun.cs:218`. Runtime checks must distinguish the pre-existing queue-before-pause behavior from an attempt to redesign those conflicting legacy aliases.

## Prior review status

The six prior findings have source repairs: mouse suppression was removed, the dropdown handles clicks, scene owners observe deliberate input before consuming/reading it, incidental Shift no longer blocks combat aliases, active-device instructions and dynamic rows refresh, and home/story labels have explicit user-facing names. The Elephant reported that `CyberButton` mouse handling also needed work; that patch and its click test were still pending at this snapshot.

## Evidence and limits

Read AGENTS.md; EGM review instructions, Codex integration, codebase-details persona, and implementation-readiness standard; router, Godot C#, C# essentials interaction lesson, input systems and Godot UI skills. The standard file is named `implementation-readiness-standard.md` in this installation (hyphens, not underscores).

Read all three `scripts/input/*.cs` files, approved plan, prior review, input prompt CSV, prompt unit tests, smoke harness, affected actor/mission/menu/world code, briefing scene, shared pause handler and squad layout. Inspected the script/project diff. Queried Graphify for input service/default/alias relationships: it returned the older InputBootstrap-centered graph, so direct source was used for the new implementation. Graph refresh remains the Elephant's responsibility.

No implementation changes, builds, imports or unit runs were performed by this reviewer. The Elephant is running these checks. The existing smoke script exercises actual menu Settings/Controls clicks, Mission 1 mouse aim/combat and a UI blocker, recruitment, world map pointer input and one cinematic. At the reviewed snapshot it did not test the briefing buttons or actual viewport dispatch of platformer aliases. Physical controller behavior and final visual layout are unverified here. Pending evidence is not recorded as a test failure.
