# Input prompts code review — 2026-11-16

Reviewer: fresh-context `codebase_details`, mode REVIEW. Target:
`docs/INPUT_PROMPTS_AUDIT.md`, recomputed lines 1–84.

Result: 0 Blockers, 0 Majors, 1 Nit.

## code-1-1 — Nit — Remap modal regresses localized action names

`scripts/main_menu/MenuInputManager.cs:87–91` stores English source strings in
the action catalog. Lines 134–135 pass the source string to OpenKeyDetectModal;
line 180 interpolates actionTitle without translation. Rows translate it at
lines 102 and 138. Git diff confirms the catalog previously stored translated titles.

Reproduction from source: select German or Arabic, open Controls, choose Rebind
for Move Left or Pause Game. The row is localized but the modal names the action
in English, such as `[Move Left]`. This is a small regression in the workflow
described at audit line 20; no runtime reproduction was performed.

Suggested correction: translate the source action title when constructing modal
text. If live locale changes include this modal, retain its source title and
refresh its text on existing prompt invalidation.

## Reads and method

Completed persona and re-implementation-standard reads, router, Godot C#,
Godot C# essentials with scripting lesson, and input-systems. Applied AGENTS
operational instructions. Read the full audit and supplied source list:
scripts/input, global.gd, MenuInputManager, CyberShogun, IsometricOperative,
CodeLevel, IsometricMission, NeoKyotoWorld, SquadStrip, project.godot and both
input test files. Inspected relevant diffs and neighboring changed input owners.

Graphify was queried before source conclusions; it returned a 7,825-node graph
and service/menu methods. Broad results were truncated; findings were confirmed
against source and Git diff. No memory or previous review conclusions were read.

Read-only review: no runtime edits, tests, Docker, imports, generation or physical
device checks. Existing validation was not independently reproduced. Root saved
this reviewer-authored result from its returned report.
