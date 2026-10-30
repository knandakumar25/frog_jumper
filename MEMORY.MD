# Axiom Force — Project Memory

Last reviewed: 2026-10-30. This file preserves durable decisions and useful
context across sessions. Follow current user instructions and `AGENTS.md`;
this summary does not replace them. Verify live environment state before acting.

## Memory maintenance

- Every agent must update this file before finishing every task, including
  read-only, planning, documentation-only, and Git-only work. Record concise
  outcomes, durable decisions, actual verification, and remaining limitations.
- Preserve unrelated context, revise stale facts, avoid duplicate summaries,
  and never record secrets. Keep the exact filename `MEMORY.MD`.
- 2026-10-30: Added this requirement consistently to AGENTS, CODEX, CLAUDE, and
  GEMINI instructions, with README/HANDOFF guidance. Verified rule consistency
  and diff hygiene; no code/assets changed, so no imports or tests were needed.

## User preferences and permissions

- 2026-10-30: User explicitly resumed the Tokyo Remembers plan after the earlier
  Docker-only task. Level 1 now uses cinematic then results, with Continue to 2
  and Replay retaining Level 1; other mission transitions remain unchanged.
- Latest startup check (2026-10-30): all four services started; website,
  backend countries endpoint, and Ollama tags API returned HTTP 200.
  PostgreSQL was healthy. Treat this as a dated observation, not live status.
- Use `docker` directly from PATH rather than a hardcoded executable path.
- For cinematic asset production, use Blender and Python inside Docker.
- Preserve existing narration lines unless the user requests changes.
- Preserve the user's open Godot editor scene; use separate test runs and
  background imports rather than closing or replacing their editor session.
- Complete authorized work without repeatedly asking for confirmation.
- Never commit, stage, or push without explicit authorization for the current
  operation and changes. Earlier Git permission does not carry to later work.
- Never install a standard WSL distribution.

## Project and required skills

- Axiom Force is a 2D cyber-action platformer: Godot 4.7.2 .NET, C#/.NET 10,
  Django REST Framework/Python 3.14, and a React/Vite website.
- Keep the four-mission campaign and story-earned operative roster.
- Read the game-development `router` and applicable specialized skills before
  game-related work. The installed source is
  `gamedev-skills/awesome-gamedev-agent-skills` (router plus 77 skills).
- Read applicable official `docker-skills@docker` skills before Docker-related
  actions or guidance. Installation alone does not satisfy the requirement.
- Installation roots and the complete workflow rules are in `AGENTS.md`,
  `CODEX.md`, `CLAUDE.md`, and `GEMINI.md`.
- Document modified code in its source. Update root README and HANDOFF whenever
  project files change. Code/assets require the prescribed background import
  and final test suites; documentation-only changes do not.

## Deployment cinematic

- New Game opens the live 40-second 2D cinematic, then the existing story
  briefing. Continue loads the saved mission directly.
- Four beats: occupied city, rooftop Shogun reveal, rooftop deployment, and
  secure AXIOM-7 transmission. Preserve red/gold Shogun identity and cyan/navy
  city artwork.
- The scene uses live Godot cameras, layered scenery, rain, drones,
  character poses, afterimages, and a transmission shader. Normal playback
  does not decode the retained legacy video.
- `deployment_cutscene.gd` owns narration timing, subtitle cues, skip input,
  and the guarded handoff; `deployment_stage.gd` samples visual animation.
- Existing `narration.ogg` and dialogue are unchanged. The latest handoff
  records narration as the only live audio layer; additional ambience is
  still a possible follow-up, not completed work.
- See `tools/cutscene/README.md` for Docker asset generation, Godot AI MCP
  inspection/captures, deterministic seeking, and legacy video tooling.

## Integrations and verification

- ElevenLabs MCP was repaired using the official local server and Windows
  certificate trust. A subsequent session exposed 27 tools and successfully
  searched for Daniel, the narration voice. Check discovery before declaring
  tools unavailable; API access and MCP registration are separate checks.
- Secrets belong in private environment configuration or the gitignored `.env`.
  Never print keys or store capability tokens in project documentation.
- Godot AI MCP is configured locally. The cutscene tools document authenticated
  access and project identity checks; verify the active project before use.
- Latest recorded cinematic verification in HANDOFF: GdUnit4 109/109 with zero
  failures/errors/orphans (report_343), NUnit 34/34, container Django 104/104,
  and C# build with zero errors/warnings. These are historical results, not
  checks performed by creating this file.
- Known diagnostics: existing plugin/TLS and editor shutdown import errors,
  ten unrelated reference-check findings, and host database authentication
  falling back to local JSON. Physical mobile and export testing were not done.
- Container status, downloaded models, tool exposure, and Git state can change;
  inspect them when needed instead of treating past chat status as current.

## Tokyo Remembers epilogue

- 2026-10-30: Implemented extraction snapshots, one-time completion guard, full
  mission descendant freeze, independent 40-second live overlay, analytic seeking,
  speaker-labelled subtitle keys/English fallback, resource fallback and guarded
  results. Scorecard accepts an optional next mission without changing grades.
- Generated all six authorized English cues through ElevenLabs MCP (Daniel/Adam),
  cached under `.validation/tokyo/speech`. Docker cutscene image built successfully;
  Debian Blender needs denoising disabled. Dojo review target rendered and inspected.
- Final verification: C# build zero errors/153 existing warnings; background import
  exited 0 with all new imports generated (existing addon TLS/shutdown diagnostics).
  NUnit 34/34, GdUnit4 114/114 with zero failures/errors/orphans (report_346),
  container Django 104/104 (host runner skipped Django because its venv is absent).
- Docker asset validation passed dimensions, alpha, cell bounds and two 40-second
  tracks; all six generated speech cues fit their windows. Godot AI MCP captured
  six shots and 16:9/16:10/ultrawide layouts and verified Replay=Level 1,
  Continue=Level 2/squad count 1, Level Select and Main Menu destinations, and
  natural completion using the track's ending. Runtime only reported the existing
  PostgreSQL/local-JSON fallback. Editor still on Level 1, test game stopped.
- Uninterrupted real-time playback subsequently verified aligned speech/visual
  clocks at six checkpoints, natural callback at timeline 40, and unchanged
  health/score; evidence `.validation/tokyo/full-playback.json`.
- Keep the three-argument `ShowScorecard` Godot binding: GDScript dynamic calls
  cannot omit a newly appended C# optional parameter. Use `ShowMissionScorecard`
  for the explicit next-mission target. Physical hardware and exports were not
  tested. The subsequent Git publication task is recorded below.

## Git publication

- 2026-10-30: User explicitly authorized committing and pushing the current
  Tokyo Remembers implementation plus the accumulated workflow/memory documentation
  to `origin/main`. Reviewed the working tree, asset inventory and diff hygiene.
  This Git-only task changes no code/assets; prior verification above remains
  applicable, so no Docker preflight, import or unit-test rerun is required.
