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
- Latest startup check (2026-10-30): found all four containers stopped, started
  them with Compose, and verified website, backend countries endpoint, and
  Ollama tags API returned HTTP 200. PostgreSQL and Ollama were healthy.
  Django needed a brief startup interval before responding. This task only
  restored services; no cinematic work was performed. Treat this as a dated
  observation, not live status.
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

## Concrete Wildfire arrival

- 2026-10-30: Added the 60-second fresh-Level-2 arrival hook, reversible mission
  lock, independent six-shot stage, labelled subtitle fallback and guarded skip/end.
  Checkpoint recovery stays in the same scene and does not replay. No watched flag
  or new objectives; exploration-map direction remains deferred.
- Docker was manually paused; resumed through its local backend and confirmed all
  four services running before code edits. Representative checkpoint art inspected.
  Seven authorized Daniel/Adam cues generated through the configured ElevenLabs MCP;
  both packaged tracks are 60 seconds and asset/timing validation passed.
- Engine finding: capture all descendant audio pause flags BEFORE disabling any
  parent, since Godot changes them on process-mode notifications. Restore modes
  first, then original audio pause flags; music resume has regression coverage.
- Final verification: Godot import exit 0/all ten imports generated; C# build zero
  errors/153 existing warnings; NUnit 34/34, GdUnit4 120/120 with zero errors,
  failures/orphans (report_349), container Django 104/104 (host venv absent).
  Docker asset validation passed; all seven cues fit and both tracks last 60 seconds.
- MCP verified six shots/three aspect ratios, Level 1 Continue, Level Select,
  saved-session confirmation and direct launch; temporary save removed. Final full
  playback kept position/health/score fixed with roughly 25-30 ms clock separation,
  then restored gameplay. Synthetic Space released Jump and resumed mission music.
  Evidence `.validation/wildfire/`; no new runtime errors beyond existing database
  fallback. Addon TLS/headless shutdown/optional gdunit4.api diagnostics remain.
  Physical controller/touch hardware, speaker listening and exports untested.
  Review games stopped; editor remains on Level 1, four containers retained.
  User subsequently authorized committing and pushing this implementation to origin/main.

## Concrete Wildfire Git publication

- Latest development commit confirmed from Git history: `4260282`,
  "Add skippable Concrete Wildfire arrival cinematic" (2026-10-30).
  This status check read history/handoff only; it did not rerun tests or change
  game code/assets. Subsequent server operations are separate from development.
- 2026-10-30: User explicitly requested pushing the current Level 2 arrival work.
  Reviewed the working tree, asset inventory and diff hygiene; fetched origin and
  confirmed main matches origin/main before publication. The authorized commit
  includes implementation, generated assets/import metadata, tests, production/review
  tools and documentation. This Git-only task changes no code/assets; the final
  verification recorded above remains applicable without rerunning the suites.
- Initial publication attempt was blocked during `git add`: Windows denied creating object
  `.git/objects/03/aac1b37ac90cd2915c1c51ef6053fb4650c133` for `siege_lights.png`.
  Inspection found explicit write-deny ACL entries on `.git/objects/03` for sandbox
  identities. No ACL changes or bypass attempted. No commit or push occurred, and
  the index remained unstaged; working files stayed intact. `git fsck --connectivity-only` found no connectivity errors
  (dangling objects from staging/history were reported).
- 2026-10-30 retry: User renewed the push request. Standard `git add` now succeeds
  without any permission changes by this agent. Rechecked staged diff hygiene and
  remote branch synchronization; proceeding with the authorized commit and normal
  push to origin/main. No code/assets changed during this Git-only retry.

## Project server shutdown — 2026-10-30

- User requested shutdown. Resumed manually paused Docker Desktop, then ran docker compose stop successfully for website, backend, db, and ollama. Verified all four services exited; containers and persistent data retained. No code/assets changed; imports and tests were not required. This memory update remains local and uncommitted.

## Evacuation Under Siege gameplay — 2026-10-30

- Implemented six-sector Level 2 route, three local invulnerable rescues, 20 finite
  mixed encounters, parryable burst gunners, safe locked-target shelling, defended
  spires and relay/core battery. Permanent rescue fire safety is separate from
  eight-second traversal dousing. Exact objective IDs guard extraction; scene-local
  recovery retains completed progress and prevents repeated kill rewards.
- Preserved arrival lines, squad progression, player controls and Level 3 transition.
  Added default shared recovery/checkpoint/title-card hooks; device-aware recruitment
  prompts; Docker/Blender original gameplay art and reproducible gameplay review tools.
- Docker preflight confirmed all four services running. Final build zero errors/warnings;
  background import exit 0/five new PNG imports (existing TLS/shutdown diagnostics).
  GdUnit4 134/134 with zero failures/errors/orphans (`report_354`), NUnit 45/45,
  container Django 104/104 (host wrapper skips absent backend venv). Five-asset QA,
  Python compilation and diff hygiene pass. MCP assisted six-sector traversal had
  zero falls; 18 dressed aspect-ratio captures were inspected. Full objective chain
  used real abilities/civilian walks with diagnostic damage and reached Level 3.
  Evidence: `.validation/wildfire-gameplay/`. Editor remains Level 1, game stopped.
- Older structural tests now ignore noncollision world dressing and activate a real
  nested finite encounter; they retain reachability/HUD/enemy requirements. Swept
  rounds handle starting inside the target, and delayed callbacks guard freed scenes.
- No 20–25 minute unassisted first-playthrough measurement exists; diagnostic traversal
  took ~96 seconds with combat bypassed. Pacing/content tuning and physical input
  hardware checks remain outstanding. Implementation was left local/uncommitted;
  the user subsequently authorized publication below. Editor stays on Level 1.

## Evacuation Under Siege Git publication — 2026-10-30

- User explicitly requested pushing the current changes, authorizing the needed
  commit and push. Reviewed all modified/untracked files, generated asset/import
  inventory and diff hygiene; fetched origin and confirmed main equals origin/main.
  Publishing implementation, tests, tools and accumulated project documentation.
- This Git/documentation-only task changes no code/assets. Prior verified build,
  Godot 134/134, NUnit 45/45 and container Django 104/104 remain applicable;
  imports and tests were not rerun. Pacing and physical hardware limits remain recorded.
