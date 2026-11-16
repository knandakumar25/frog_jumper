# Input prompt evidence review — 2026-11-16

Reviewer: fresh-context implementation_readiness, mode REVIEW. Target:
`docs/INPUT_PROMPTS_AUDIT.md`, recomputed lines 1–84.

Result: 0 Blockers, 1 Major, 1 Nit.

## evidence-1 — Major — Controller event routing is not exercised

Scope: audit lines 16, 19, 24–25, 56–58, 80–84;
`tools/input_prompt_smoke.gd:66–81`; `tests/test_input_prompts.gd:106–117`.

The graphical harness calls InputPromptService.ObserveInput directly. Its pad
helper explicitly avoids gameplay/Skip handlers; its keyboard helper also calls
the service directly. Engine tests do likewise. The Keyboard-style test checks
UsingController, IsGamepad and text, without checking a controller gameplay action.

These establish label selection and refresh, not controller routing through
Godot's Input/GUI/UnhandledInput pipeline. A consuming handler, missed observer,
or gameplay disabled under forced Keyboard style could leave these tests passing.
The audit specifically describes scene-before-autoload ordering and continued
controller functionality. The physical-device disclaimer does not identify this
additional software verification boundary.

This is an uncovered regression risk, not a demonstrated runtime failure.
Add viewport-routed synthetic presses and releases with observable outcomes:
forced Keyboard style, a consuming menu/cinematic, and a gameplay action. Until
then, describe controller smoke checks as direct observation and label refresh.

## evidence-2 — Nit — Menu refresh assertion can pass without refresh

Scope: `tools/input_prompt_smoke.gd:90–95,136–143` and results.json lines 10–20.
Searching every visible label for the character A can match an unrelated title.
It neither identifies the instruction label nor compares before/after text.
Six menu-pad-refresh checks therefore provide weaker evidence than their names.
Assert the specific instruction label's expected bindings and actual device change.

## Supported evidence and remaining boundaries

- Resume log lines 240–246 support backend 158 passes, zero failures; lines
  952–954 support NUnit 454 passes. Lines 6423–6429 support Godot 385 cases,
  78 suites, zero errors/failures/skips/orphans and exit 0. XML independently
  parses to 78 suites, 385 cases, zero errors and failures.
- Graphical JSON contains 58 checks and no failures. Both stdout logs report
  58/0; fresh stderr is empty. Mouse tests route viewport events and assert
  outcomes for Home/menu, isometric aim/attacks/UI interception, map and Skip.
- Remap smoke checks HUD text, not jumping through a routed rebound key.
  Map opening calls UnhandledInput directly before actual pointer injection.
- Screenshot assertions check PNG writing. Platformer bounds assertions compare
  controls/subtitle/squad rectangles. Root visual-inspection claims were not
  independently inspected by this reviewer; no artwork review was performed.
- Final graph log supports 7,825 nodes/16,562 edges and discloses absent SQL
  extraction. Import shutdown diagnostics are disclosed accurately.
- input-build.log ends at 152 warnings/zero errors, including a PDB copy retry.
  Resume log lines 1116–1117 support 151 warnings/zero errors. The candidate
  build claim is supported, although exact log associations could be clearer.

No Blocker or independently demonstrated runtime defect was found. Passing
suites remain distinct from the coverage gaps above.

## Reads and limits

Read the complete persona, implementation-readiness standard, EGM review and
Codex integration, router, Godot C#, input-systems, C# essentials/debugging lesson
and headless-testing guidance. Read full target, all input C# files, graphical
harness, both input tests, supplied suite/XML/smoke/import/graph/build evidence,
relevant mission/player/cinematic excerpts and project version metadata.

All supplied inputs were accessible. A missing user-skill-relative integration
path was resolved from the installed plugin. Graphify was queried and source
confirmed. No memory, history or earlier findings were consulted; no files,
suites, Docker or art were changed. Root saved this returned reviewer report.
