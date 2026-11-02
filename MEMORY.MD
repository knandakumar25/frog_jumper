# Axiom Force — Project Memory

Last reviewed: 2026-11-02. This file preserves durable decisions and useful
context across sessions. Follow current user instructions and `AGENTS.md`;
this summary does not replace them. Verify live environment state before acting.

## Memory maintenance

- 2026-11-01 export policy: game exports are local Docker-only, with explicit
  targets. Do not add or run GitHub Actions game exports, including Docker jobs.
  GitHub reports zero current workflows; historical runs remain. Fixed launcher
  service naming, inert no-target behavior, target validation, and CI/container
  guards. Exporter image must be rebuilt before its next authorized use; this
  task ran no actual game export or exporter image build. Safety/help/error-path,
  shell syntax and Compose isolation checks pass; no build/ output was created.
  NUnit 93/93, Godot 161/161 (report_368, zero failures/errors/orphans) and container
  Django 104/104 pass; host Django wrapper skips its absent venv. Existing compile
  warnings remain. Normal services were restored for mandatory preflight and are
  left running, db/Ollama healthy; website/API returned HTTP 200. No engine files
  changed, so no import refresh; no staging/commit/push.
- Current game status: Neon Rust's sabotage/escape expansion is followed by
  The City Finds Its Voice, Level 3 results, then The Wall That Faces Inward
  on fresh Level 4 launches. See the dated production/verification entries below.

- Every agent must update this file before finishing every task, including
  read-only, planning, documentation-only, and Git-only work. Record concise
  outcomes, durable decisions, actual verification, and remaining limitations.
- Preserve unrelated context, revise stale facts, avoid duplicate summaries,
  and never record secrets. Keep the exact filename `MEMORY.MD`.
- 2026-10-30: Added this requirement consistently to AGENTS, CODEX, CLAUDE, and
  GEMINI instructions, with README/HANDOFF guidance. Verified rule consistency
  and diff hygiene; no code/assets changed, so no imports or tests were needed.

## User preferences and permissions

- 2026-11-01 shutdown: user explicitly requested stopping the server. Ran
  docker compose stop for website, backend, PostgreSQL and Ollama; all four
  exited successfully and no project services remain running. Containers and
  persistent volumes retained. Shutdown/memory only; no imports, unit tests,
  exports, staging, commits or pushes performed.

- 2026-11-01: User plans macOS and iOS releases and accepts manual Apple steps.
  Added macos unsigned-universal packaging and ios source-handoff targets, Apple
  preset settings, a native Mac iOS completion helper, and export/APPLE_RELEASE.md.
  Apple signing/notarization and native iOS Godot/Xcode generation/distribution
  are the explicit exception to Docker-only execution. GitHub Actions stays
  prohibited; setup does not authorize an actual export. Real owner bundle IDs
  and Team ID must be supplied later; no keys/passwords are stored.
  Four synthetic helper tests, source inventory, syntax/safety/Compose checks
  pass. Background import exits 0 with existing TLS/editor shutdown diagnostics.
  NUnit 93/93, Godot 161/161 (report_369, zero failures/errors/orphans) and
  container Django 104/104 pass; wrapper skips absent host Django venv.
  No actual Apple export, source handoff, signing, IPA or exporter image build
  ran. Real Mac/device/AOT/native-library validation remains outstanding.
  Normal services remain running; all changes are local and uncommitted.

- 2026-10-30: User explicitly resumed the Tokyo Remembers plan after the earlier
  Docker-only task. Level 1 now uses cinematic then results, with Continue to 2
  and Replay retaining Level 1; other mission transitions remain unchanged.
- Latest startup check (2026-11-02): user requested restart. No existing project
  containers were present in the active Docker session. Compose pulled images,
  built backend/website and started all four services. PostgreSQL/Ollama healthy;
  website, direct/proxied countries API and Ollama tags returned HTTP 200.
  Startup/memory only: no game changes, imports, tests, exports or Git publication.
  Treat this as a dated observation, not live status.
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

## Storage policy — 2026-10-31

- User requested PostgreSQL-only application persistence, Microsoft's official
  Playwright Docker image, and normal server startup.
- Removed local save/settings/progression/token files and portal localStorage.
  PostgreSQL JSONB stores settings/progression; campaigns stay in PostgreSQL.
  Django tests no longer use SQLite.
- Tokens stay in memory; restart/reload requires sign-in. Portal locale is in
  the URL. Clerk identity/cookies remain upstream; tooling caches/assets/logs,
  Docker volumes and Ollama models are outside application-save storage.
- Existing local saves remain untouched and ignored, with no automatic migration.
  Game state retains the existing shared development database scope.
- Added opt-in Playwright v1.64.1-noble and matching npm runner. The four normal
  services remain always-on; browser verification is one-shot.
- Status rechecked 2026-10-31: docker ps confirms website (5173), backend
  (8000), PostgreSQL (5432, healthy), and Ollama (11434, healthy) running.
  Playwright has exited after its successful test; no persistent Playwright
  server is configured. Read-only status task; no services changed or tests rerun.
- Final verification: C# build zero errors (154 existing warnings); background
  import exit 0 with existing addon TLS/editor shutdown diagnostics. NUnit 47/47,
  GdUnit4 135/135 with zero errors/failures/orphans (report_357), container Django
  104/104 on PostgreSQL, and Microsoft-image Chromium smoke passed. Host test
  wrapper skips absent backend venv; container execution covers Django.
- Website, direct/proxied countries API and Ollama tags returned HTTP 200.
  Four normal containers remain running; PostgreSQL/Ollama healthy. No commit,
  push or export. Clerk replacement was asked as a scope clarification and has
  not been authorized separately. This policy supersedes older fallback checks.

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
- Latest shutdown, after Level 2 publication (`9f1700e`): user again requested
  stopping the servers. All four were running; `docker compose stop` succeeded,
  and `docker compose ps -a` confirmed backend, db, website and ollama exited.
  Containers/data retained. No code/assets changed or tests/imports required;
  this shutdown record is local and uncommitted.

## Evacuation Under Siege gameplay — 2026-10-30

- 2026-10-31 recall: the user's last gameplay request was to start Level 2
  gameplay. The subsequent PostgreSQL/Playwright/startup request took priority;
  this recall task made no game edits and required no imports or tests.
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
- Publication succeeded: gameplay commit `b60d34c` (47 files) was pushed normally
  to `origin/main`; remote branch SHA and local tracking state were verified equal.
  This follow-up publication record is included in a documentation-only commit.

## Through the Breach — 2026-10-31

- Status clarification: Level 2 results and Continue to Level 3 are implemented.
  Through the Breach precedes those results and teases Level 3. At that earlier
  investigation no Level 3 arrival existed; Silent Freight below now supplies it.
  The original AXIOM-7 gameplay dialogue resumes after arrival. That earlier
  read-only investigation did not rerun tests.

- Resumed after user requested Docker restoration first. Existing implementation,
  nine-shot visual review, report_361 and final test/playback evidence were present
  and reviewed; no reimplementation was needed. Corrected stale tooling transition
  documentation. This resume changes Markdown only; no import/test rerun required.
  Docker restoration completed after pulling missing images/building the app.
  All four services are running; PostgreSQL/Ollama healthy. Website, direct and
  proxied countries API, and Ollama tags returned HTTP 200. No staging/commit/push.

- Added Level 2 extraction → 75-second live WildfireAftermath → captured results →
  Level 3. Retains Level 2 for Replay/arrival; freezes mission/pause/HUD/audio and
  guards progression/completion. No watched flag, persistence or schema changes.
- Original Docker/Blender reunion/terminal scenery and 18-cell pose atlas; eight
  ElevenLabs multilingual-v2 cues: Daniel/AXIOM-7, Adam/Kaito, Sarah/Oceanus,
  George/Titan. Docker Python/FFmpeg packages two 75-second unclipped tracks.
- Missing speech preserves timed subtitles; missing essential art opens results.
  Skip supports keyboard/controller/pointer/touch, clears mapped action state and
  defers results beyond input dispatch. Teardown restores actual cursor/prompt state.
- Existing PostgreSQL/website changes preserved; four services remain running.
  Prompt tests supply a canvas omitted by headless Global and compare the cursor
  state actually supported by headless DisplayServer. Verification is recorded below.

Final verification (2026-10-31): background import exit 0 and all nine new import
files present; C# build 0 errors (154 existing warnings on recompilation, final
incremental build 0 warnings). `run_tests.ps1` passes NUnit 47/47 and GdUnit4
142/142 with zero failures/errors/orphans (`report_361`). Host Django is skipped
because its venv is absent; explicit container Django passes 104/104. Docker
asset/audio validation and Python compilation pass. Both tracks are 75 seconds;
all eight complete cues fit their windows and decoded peaks remain below clipping.

Godot AI MCP captured and visually reviewed all nine shots at 1920×1080,
1920×1200 and 2560×1080. Uninterrupted 75-second playback kept player position,
health and score fixed; maximum sampled audio/visual clock separation was under
30 ms. Natural results, Continue to Level 3 with three earned operatives, Replay
with Level 2 arrival, Level Select and Main Menu all pass. Runtime review logs
contain no errors or warnings. Level 1 regression passes in the Godot suite.
Evidence: `.validation/breach/`, including 27 captures and aspect contact sheets.

Existing import diagnostics remain separate: asset-placer TLS failures, editor
shutdown settings/resource/ObjectDB/PagedAllocator messages, and optional
`gdunit4.api` warning. No physical controller/touch hardware, speaker-listening
review or export was performed. Review game stopped; the user's editor remains
on Level 1, all four services running. Changes are local/uncommitted; unrelated
PostgreSQL/website work is preserved.

## Silent Freight — 2026-10-31

- Implemented requested 75-second live Level 3 arrival, every fresh launch,
  no watched flag/migration. Existing scrapyard obstacles and Aura/Mirage
  recruitment remain unchanged; only Shogun/Oceanus/Titan appear.
- Reused aftermath input lifecycle; shared arrival helper preserves Level 2
  entry and captures/restores the complete mission once, including overlay removal.
- Docker preflight: all four project services running. C# implementation builds
  with zero errors (existing addon warnings). Final suites: NUnit 47/47,
  Godot 151/151 with zero failures/errors/orphans; container Django 104/104.
- Preserve all pre-existing uncommitted PostgreSQL/website/aftermath work and
  user's open Level 1 editor; changes remain local and uncommitted.

Silent Freight runtime review: Godot AI MCP captured all nine shots at
1920×1080, 1920×1200 and 2560×1080; all three contact sheets were inspected.
Uninterrupted playback kept the entire mission frozen, including player
(200,554), health, score, fragments and relay state. Maximum sampled speech/visual
clock separation was 32 ms. Natural handoff restored HUD, processing and music
with Jump released. Actual Continue, Level Select, saved-session loading and
Replay reached Level 3 with its arrival active. The save-load test used a new
owned PostgreSQL session and verified deletion afterwards; existing saves were
untouched. Runtime review contains no warnings/errors. Editor remains Level 1,
game stopped. Final verification follows.

Final verification (2026-10-31): background Godot import exited 0 and all eleven
PNG/Ogg import files exist. C# build has zero errors (154 pre-existing addon
warnings). Final run_tests.ps1 passes NUnit 47/47 and Godot 151/151, zero
failures/errors/orphans (report_363). The host wrapper skips Django because
backend/.venv is absent; the explicit container suite passes 104/104. Docker
raster/audio QA and Python compilation pass: 1920×1080 transparent scenery,
512-cell atlas bounds, eight complete nonoverlapping cues, two exact 75-second
tracks and decoded peaks below clipping. Diff hygiene passes.

Existing import diagnostics: asset-placer TLS failures and editor shutdown
EditorSettings/ObjectDB/resource/PagedAllocator messages; runtime review logs
are clean. No physical controller/touch hardware, speaker-listening review or
export was performed. Evidence: .validation/neon-rust/ (including 27 captures
and three contact sheets). All four normal Docker services remain running.
User's editor remains on Level 1; review game stopped; changes are local and
uncommitted. No watched flag, save migration or local persistence was added.

## Current changes Git publication — 2026-10-31

- User explicitly requested pushing the current changes, authorizing the necessary
  commit and push. Scope includes Silent Freight, Through the Breach, existing
  PostgreSQL-only persistence/website changes, tests, production tools and docs.
- Reviewed tracked/untracked inventory, import metadata and diff hygiene; fetched
  origin and confirmed main equals origin/main before publication. Exclude ignored
  local configuration, generated validation evidence, caches and credentials.
- This Git/documentation-only task changes no code/assets. Prior verified build,
  NUnit 47/47, Godot 151/151 (zero failures/errors/orphans) and container Django
  104/104 remain applicable; no Docker preflight/import/test rerun is required.
  Publication blocked: git add --all failed writing a new object beneath
  .git/objects/39 (Windows Permission denied). No files were staged, no commit
  was created and no push occurred. Read-only ACL inspection found explicit
  deny entries on .git and its descendants. Access rules were not modified;
  Git write access must be restored before retrying publication.

- Retry authorized by the user: Git write access is now available and staging
  all reviewed current changes succeeded. Fetched origin and checked the staged
  diff before committing. No access-control changes were made by this agent.
  This retry changes documentation only; prior test/build results still apply.

- Publication succeeded: implementation commit dc24b7f (102 files) pushed
  normally to origin/main. git ls-remote confirmed the remote SHA matches
  the local implementation commit. This verification record is included in
  a follow-up documentation-only commit and normal push. No code/assets
  changed during publication; existing test results remain applicable.

## Neon Rust gameplay expansion — 2026-10-31

- Implemented the 48,000px eight-sector sabotage route: 30 finite groups, exact
  local objective/reward ledger, shipments/uplinks/couplers, guarded relay and
  mandatory Mirage escape lesson. Shared combat uses IFiniteCombatHost; Level 2
  retains authored activation, timings, artillery and rewards.
- Seven stationary shrines preserve completed events, recruits, lessons and reward
  slots; death/fall clears bullets and resets incomplete groups. Aura opens a real
  practice lock; Mirage must Blink across a harmless beam. Original arrival,
  operative stats, dialogue/audio and save schema remain unchanged.
- Level 3 now freezes at direct results with Continue to 4 (five operatives),
  Replay, Level Select and Main Menu. Replay restores the three-operative roster
  and arrival. All work remains local and uncommitted.
- Docker preflight confirmed all four services up. Initial C# build passed with
  154 existing warnings. Ten focused Godot tests passed with zero errors/failures/
  orphans. MCP traversed the blockout with real movement/abilities and diagnostic
  kills, no recoveries (~145s simulation), before Docker/Blender art production.
- Seven original 512px RGBA depot assets and reproducible tooling were added.
  Full-project reference checker reports ten existing signal-path errors in
  unchanged Proton Scatter/main-menu scenes and 27 warnings, not a clean pass.
- Human first-run 30–35 minute pacing remains unvalidated. Assisted traversal
  cannot measure combat or human pacing. Final verification is recorded below;
  reproduction and acceptance details live in tools/gameplay/NEON_RUST.md.

Neon Rust final verification (2026-10-31): refresh exit 0, seven generated import
sidecars and seven-asset RGBA/margin validation passed. Import still logs TLS and
editor shutdown/resource diagnostics; do not call it error-free. Final incremental
build had 0 errors/warnings; earlier full compile had 154 existing warnings.
run_tests.ps1 passed NUnit 93/93 and Godot 161/161 with zero errors/failures/orphans
(report_367); Django was skipped for absent host venv, then explicit container
Django passed 104/104 after final edits. MCP dressed traversal completed every
objective with actual movement/abilities plus diagnostic kills, no recoveries,
peak four live enemies, 151.333s movement/objective wall time. Combat/human pacing
remain unmeasured. All 24 sector/aspect captures and all four scorecard navigation
actions verified; Replay gives fresh arrival/three operatives, Continue gives
Level 4/five operatives. Final runtime logs had no warning/error entries. User
editor remains stopped on Level 1, all four normal containers running. Changes
remain local, unstaged and uncommitted. Human 30–35 minute target stays unvalidated.

## Server shutdown and publication — 2026-10-31

User explicitly requested server shutdown followed by publishing the current Neon
Rust changes. Stopped website, backend, db and ollama with docker compose stop;
all four exited successfully (0). Containers and persisted volumes remain intact.
Fetched origin and confirmed main matched origin/main before preparing the commit.
This task changes documentation/Git state only, so the previous 93 NUnit, 161
Godot (zero errors/failures/orphans) and 104 container Django passes still apply;
no import or test rerun is required. Publication verification follows after push.

Publication verified: implementation commit 3c92724 pushed normally to origin/main;
git ls-remote confirmed the remote SHA matched the local implementation commit.
The shutdown/publication record is included in this follow-up documentation commit.
No code/assets changed during publication; prior test results remain applicable.

## The City Finds Its Voice — 2026-11-01

- Added requested 90-second live Level 3 aftermath before immutable results; one
  completion submission, full mission freeze, guarded deferred handoff, all-device
  Skip/action release and missing speech/art fallbacks. Continue: Level 4/five
  operatives; Replay: Level 3 arrival/three operatives. No extra unlocks, watched
  state, migration, Surya/Voss introduction or pre-Level 4 arrival.
- Original Docker/Blender five-operative atlas/fortress; ten exact requested
  ElevenLabs cues and two 90-second tracks. Cue text/IDs/settings/hashes/timing and
  reproduction are recorded beside assets and in tools/cutscene/README.md.
- Final import exit 0/five sidecars; C# build zero errors, 154 existing warnings.
  Required suites: NUnit 93/93, Godot 170/170 (report_372; zero failures/errors/
  orphans), isolated container Django 104/104 (host wrapper skips absent venv).
  Raster/alpha/atlas, audio/timing/manifest QA, Python compilation and diff pass.
- MCP inspected all 33 shot/aspect captures; full playback kept position/health/
  score and mission state frozen, max clock separation 36.8ms. All four results
  destinations/rosters pass; 90 runtime log entries contain no warnings/errors.
  Editor remains stopped on Level 1; evidence: .validation/city-voice/.
- Existing import TLS/shutdown diagnostics remain. Physical controller/touch and
  speaker-listening review unperformed. Docker Desktop D: bind-mount fault also
  blocks existing backend /app and remains unresolved; production and Django QA
  used owned unmounted copy-based containers, now stopped. Four normal services
  remain running, never restarted/stopped. Unrelated export changes preserved;
  no exports, staging, commits or pushes; work remains local/uncommitted.

## Current changes publication — 2026-11-01

User explicitly requested pushing current changes, authorizing the required local
commit and normal push. Scope includes The City Finds Its Voice, generated assets,
production/review tools and tests, plus pre-existing Apple export setup and policy
documentation. Reviewed tracked/untracked inventory, imports, diff hygiene and
remote synchronization; main equals origin/main before publication. Ignored local
configuration, speech caches and validation evidence remain excluded. This Git/
documentation-only task changes no code/assets; prior build, NUnit 93/93, Godot
170/170 (zero failures/errors/orphans), container Django 104/104 and runtime QA
remain applicable without rerunning tests/imports. Existing Docker bind-mount and
physical-device/listening limitations remain documented.

Publication verified: implementation commit 545fc9a (47 files) pushed normally to
origin/main; git ls-remote confirmed the remote SHA matched. No code/assets changed
during publication. This result is recorded in a follow-up documentation commit.


## The Wall That Faces Inward — 2026-11-01

Level 4 now begins with a skippable 120-second live cinematic on every fresh
launch. Thirteen shots introduce the independently powered fortress, separated
Surya Prime (Brian) and armored Commander Voss (Bill). The five-operative squad,
spawn, gameplay briefing, recruitment and arena confrontation remain intact.
Surya still joins at the approach trigger; the gameplay boss appears only in the
arena. The complete mission/HUD/pause/audio snapshot restores once on natural
completion, Skip, missing art or independent overlay removal. Missing speech
keeps the full timed subtitles; checkpoint recovery does not replay the intro.

Original Docker/Blender fortress, shield-road and command artwork reuse existing
operative/boss designs. Twelve cached ElevenLabs multilingual-v2 cues use .70
stability, .75 similarity, .10 style and speed 1; Voss receives restrained speaker
EQ/echo. Separate dialogue/seeded atmosphere tracks measure 120 seconds each.
Production and review: tools/cutscene/README.md and *_inward_wall.py. No watched
flag, save migration, export or publication.

Final verification: C# build has zero errors (154 existing addon warnings);
background import exits 0 with all nine PNG/Ogg sidecars. NUnit 93/93, Godot
180/180 (report_375; zero failures/errors/orphans), and isolated container Django
104/104 pass. All thirteen shots were inspected at 1920×1080, 1920×1200 and
2560×1080. Full playback holds the mission frozen, hands control back naturally,
and stays within 32 ms sampled audio/visual separation. Continue, Level Select,
Replay and loading an owned temporary save activate arrival; the save was removed.

Raster/alpha, Surya atlas boundaries, twelve complete cue windows, runtime text/
manifest agreement, unclipped PCM and both exact 120-second tracks pass. Python
compilation and diff hygiene pass. Runtime review has 95 log entries with zero
warnings/errors; evidence is in .validation/inward-wall/, including 39 captures
and three inspected contact sheets. Unit coverage includes all Skip routes,
missing speech/art, repeated completion, teardown/removal, exact mixed snapshot
restoration, full briefing lifetime, all-shot state invariants, recruitment after
handoff and recovery without replay. Existing arena tests still verify Voss and
the wall after arrival; earlier cinematics/results regress in the full suite.

Existing import TLS/editor shutdown diagnostics and the reference checker's ten
unchanged addon/main-menu signal-path errors (27 warnings) remain separate from
runtime/test results. Host Django is skipped for the absent venv; the backend
bind mount still fails with ENODEV, so current backend source/environment were
copied into an owned unmounted container for the explicit 104-test pass. The
production helper also used copied inputs. Both owned helpers are now stopped;
the four normal services remain running. Editor remains stopped on Level 1.
Physical controller/touch hardware and speaker-listening checks remain unperformed.
No export, staging, commit or push; changes remain local and uncommitted.


## Level 4 arrival publication — 2026-11-01

User explicitly requested pushing current changes, authorizing the required commit
and normal push. Reviewed the implementation, generated assets/imports, production
manifests/tools, tests and documentation; fetched origin and confirmed main equals
origin/main. Repaired accidental encoding damage in documentation before staging.
Ignored validation evidence, speech caches and local secrets remain excluded.
This publication task changes documentation/Git state only: prior build, NUnit
93/93, Godot 180/180 (zero failures/errors/orphans), Django 104/104 and runtime QA
remain applicable without rerunning imports/tests. Physical input/listening and
existing Docker/import diagnostic limitations remain documented.

Publication verified: implementation commit 8fb0c1b (45 files) pushed normally to
origin/main; git ls-remote confirmed the remote SHA matched the local commit.
This verification record is included in a documentation-only follow-up commit.
No code/assets changed during publication; prior verification remains applicable.

## Act I player-facing labels — 2026-11-02

Checked campaign scripts/scenes: Level 4 Shattered Aegis ends Act I;
its completion line says ACT I COMPLETE / CROSSING OPEN. BRING THEM HOME,
and the final victory scorecard title is ACT I COMPLETE. No explicit Act I
opening title was found in scripts/scenes. Recent redesign covered Levels 1–3,
not all four Act I missions. Read-only investigation and memory update only;
no runtime changes, import, tests or publication performed.

## Act boundary clarification — 2026-11-02

Confirmed README and ShatteredAegisMission completion text identify Level 4,
Shattered Aegis, as the Act I finale. Act I includes Levels 1–4; the completed
recent redesign covered Levels 1–3 only. Earlier chat implying those three alone
constitute all of Act I was inaccurate. Read-only verification plus this memory
entry; no runtime changes or new test run.

Git status check (2026-11-01): main matches the local origin/main tracking ref;
the working tree was clean. This required memory-only status record remains
uncommitted. No fetch, code changes, imports or tests were needed.

Scope clarification (2026-11-01): verified commit 8fb0c1b only adds the pre-Level 4
cinematic and its arrival integration/guards. No Level 4 gameplay expansion was
started; existing geometry, enemies, hazards, recruitment and boss encounter remain
as before. Read-only diff verification; this memory update is uncommitted.

Script fidelity check (2026-11-01): compared the shipped cue manifest/runtime
subtitles and mission diff against the supplied pre-Level 4 plan. All twelve
dialogue lines, speaker order and cue windows match, with the silent title at
117–120 seconds. Creative implementation covers artwork, framing and atmosphere;
no new plot or Level 4 gameplay expansion was added. No code/assets changed or
tests rerun; this required memory-only audit record remains uncommitted.

Creative-direction discussion (2026-11-01): user is considering replacing the
fixed script with agent-led direction; this is discussion, not authorization to
rewrite the campaign. Reviewed story bible, mission catalog/current mission code
and prior pacing evidence. Recommend preserving established characters, earned
recruits and liberation consequences while revising the future outline. Story
bible still describes nine levels and restricted supporting-cast lines; the live
catalog has four missions and newer ensemble cinematics, so reconcile canon
before expansion. Proposed priority: develop Level 4's fortress/Surya/Voss payoff,
then targeted consistency/readability/pacing passes on 1–3. Human pacing remains
unmeasured; do not call encounter counts proof of poor pacing. Review the combined
90-second Level 3 aftermath plus 120-second Level 4 arrival for time without player
control. No gameplay, assets or story files changed; no imports/tests required.


## Revised Act I and finished fortress — 2026-11-01

Implemented a scene-local fortress objective ledger, seven-section 28,000px route,
finite combat (four live ordinary enemies maximum), earned Surya/actual protection
lessons, permanent thermal/gun/curtain shutdown and two reconnecting gun approaches.
Three-phase opt-in Voss retains his armored missile identity; phase damage clamps,
pylons, stationary melee recovery and real owned missile reflection are covered by
engine tests. Checkpoints retain durable progress and rewards; boss deaths reset
attempt state, post-defeat recovery does not respawn Voss. Crossing release and safe
civilian presentation precede frozen ACT I COMPLETE results without Continue.

Reconciled ensemble canon and open future acts; documented Levels 1–3 review and
separate follow-ups without rebuilding them. Earlier cinematic recordings/durations,
four-mission catalog, controls/stats and save schema remain intact. Produced fourteen
cached cast cues with exact settings/text/hash/duration provenance and six original
Docker/Blender RGBA assets after target inspection. Missing speech/art keeps progress
available. Final checks pass: import exit 0/all twenty sidecars, incremental build 0 errors/
warnings (153 existing full-compile warnings), NUnit 97/97, Godot 190/190 with zero
failures/errors/orphans (report_388), and explicit copied-container Django 104/104.
Final suite log has no engine errors. Existing TLS/editor shutdown import diagnostics
remain. Media alpha/margins/full-cue/hash/unclipped checks and Python compilation pass.

All 30 section/phase/crossing/results captures at three requested ratios were
inspected. Automated traversal reached extraction using real movement, attacks,
abilities and parries; diagnostic runs remain separate. One observed thermal
checkpoint return and two tool-side resumptions kept the same scene/ledger. No new
runtime errors were reported by its MCP batches; sampled logs were clean. Both gun
routes reconnect/backtrack; Oceanus's upper route was exercised separately with real
movement/douse. Titan's measured jump is 184.71px and required stairs are 100px.
Real boss deaths in each phase restart at the arena checkpoint. Spent devices disable
their hurtboxes to prevent combo farming. A pre-existing upgrade-screen focus call
outside the tree was guarded to remove six regression-log errors.

Actual finale navigation and all launch routes pass, including fresh Replay with
five operatives/arrival and a clean ledger. The owned temporary save was deleted;
existing saves were untouched. Both owned helper containers are stopped; four normal
services remain running and the user's editor stays stopped on Level 1.
The Docker bind fault persists; owned copied inputs enable media/Django checks.
No exports or Git publication authorized/performed; existing memory audits preserved.
Human 20–25 minute target, physical input hardware and listening remain unvalidated.


## Fortress publication and tomorrow handoff — 2026-11-01

User explicitly requested pushing the current work and continuing tomorrow,
authorizing the required commit and normal push. Reviewed the tracked/untracked
inventory, import sidecars and diff hygiene; fetched origin and confirmed main
matches origin/main before publication. Scope is revised Act I, the fortress
implementation/assets/voices/tests/tooling, documentation and existing memory
audits. Ignored speech caches, validation evidence, credentials and test reports
remain excluded. This Git/documentation task changes no code/assets; the verified
build, NUnit 97/97, Godot 190/190 (zero failures/errors/orphans) and copied-container
Django 104/104 remain applicable without another import/test run.

Resume tomorrow from tools/gameplay/SHATTERED_AEGIS.md and the latest HANDOFF:
human first-run pacing, physical controller/touch and speaker listening remain
outstanding. Docker bind-mount and import diagnostics remain documented. No
additional gameplay work or service shutdown was requested for this publication.

Publication verified: implementation commit a17c83e (87 files) pushed normally to
origin/main; git ls-remote confirmed the remote SHA matched the local commit.
This result is recorded in a documentation-only follow-up commit. No code/assets
changed during publication; prior verification remains applicable. Resume tomorrow
with the documented human acceptance checks; no further implementation is running.

Latest-game-change recap (2026-11-02): confirmed from the recorded implementation
and publication entries that the latest gameplay addition was the completed Level 4
fortress and Act I finale, published as a17c83e. Server restart was operational only.
No code/assets changed or verification suites rerun for this memory-only recap.

Creative-freedom recap (2026-11-02): checked docs/ACT_I_REVIEW.md and fortress
production notes. Earlier proposals cover Level 1 parry/archive guidance, Level 2
rescue clarity/repeated defenses and Level 3 attrition/cinematic downtime, pending
human observations. These are proposed follow-ups; Levels 1–3 were not rebuilt as
part of the completed Level 4 creative pass. Read-only review plus memory update;
no code/assets changed, imports or tests required.

## Levels 1–3 teaching and objective defense — 2026-11-02

Implemented contextual parry/archive/beacon/Blade guidance, state-specific rescue
and spire markers, nine required Level 3 defenses of the original 30, and objective
checkpoints. Levels 2–3 retire remote unfinished groups beyond 1,500 horizontal
pixels using existing recovery/reward safeguards; default retirement stays disabled
for Level 4. Routes/story/recruits/abilities/cinematics, stats/caps/score/save schema
remain intact. Replay starts fresh; recovery retains objectives and reward slots.
Human pacing/device acceptance remains pending; checklist in docs/ACT_I_REVIEW.md.
Final build: zero errors; 153 existing full-compile warnings, clean incremental
checks. Background import exits 0 with existing TLS/editor shutdown/resource
diagnostics. run_tests.ps1 passes NUnit 88/88 and Godot 197/197 with zero failures/
errors/orphans (report_399); host Django skips the absent venv. Final explicit
copied-container Django passes 104/104 because the standard backend bind mount
again returns ENODEV. Owned axiom-mission-readability-tests helper is stopped;
all four normal services remain running.

MCP inspected 51 state captures at all three requested sizes (dimensions checked,
nine contact sheets); runtime samples contain no warnings/errors. Final Blade
sign placement clears the portal lintel. All four results actions for each of
Levels 1–3 pass, including fresh Replay and unchanged Continue/arrival rosters.
Final required-only Level 3 traversal uses actual movement/abilities with diagnostic
defense kills: 157.436 seconds assisted route/objective wall time, max six live
enemies, zero recoveries or manual resumption. It supersedes an earlier helper run
that missed Mirage's window; neither run establishes combat/human pacing.

Seven new Godot checks cover controls/deflection, beacon/Blade states, rescue
arrival and real abilities, recovery, retirement/reactivation, budget/rewards and
objective checkpoints. Ledger cases cover every required defense, optional bypass
and premature/duplicate events. Python compilation and diff hygiene pass. Evidence
is under .validation/mission-readability/ and mission-readability-*.log. Editor PID
12608 remains stopped on its original Level 1 scene. No existing saves used by MCP,
no exports, staging, commits or pushes; earlier memory entries preserved.

Creative direction supersedes prior fixed-script mandates (2026-11-02): user
confirmed deleting the written creative documents docs/STORY_BIBLE.md and
docs/full_game_plan.md while preserving executable game code and cinematics.
Deleted both documents, removed active links and updated README/HANDOFF and the
fortress production guide. Future creative work uses agent judgment under current
user requests; historical story restrictions and exact-script adherence are no
longer creative mandates. Existing implementation/QA records remain factual
context. No new rewrite, code/data/recording changes, commits or pushes were
requested or performed. Verified deletions, active references and diff hygiene;
this Markdown-only cleanup needs no Docker preflight, import or test rerun.

## Levels 1–3 and creative-direction publication — 2026-11-02

User explicitly requested pushing the current changes, authorizing the required
local commit and normal push. Reviewed the full tracked/untracked inventory and
diff hygiene; fetched origin and confirmed main matches origin/main. Scope is
the Levels 1–3 teaching/objective-defense implementation, tests/review tooling,
documentation and the confirmed removal of the story bible/full development plan.
Ignored credentials, validation evidence, caches and test reports remain excluded.
This publication task changes Git/documentation only; prior build, NUnit 88/88,
Godot 197/197 (zero failures/errors/orphans) and copied-container Django 104/104
remain applicable without rerunning import/tests. Human pacing/device acceptance
and existing Docker/import diagnostics remain as documented.

Publication verified: implementation commit fbdc044 pushed normally to origin/main;
git ls-remote confirmed the remote SHA matched the local implementation commit.
This verification record is included in a documentation-only follow-up commit.
No code/assets changed during publication; prior verification remains applicable.

## Act I opening label — 2026-11-02

Added mission-1-only ActHeading in CodeLevel.BuildTitleCard: centered 24px cyan,
CAMPAIGN_ACT_I_TITLE with ACT I fallback, above the unchanged 34px mission title
and objective. It shares the existing hold/fade, ignores input and adds no saved
state or API. Recovery reuses the scene; every fresh Level 1 instance gets the
label. Docker preflight: all four normal services running. C# build passes with
153 existing warnings and zero errors. Background import exits 0; known TLS,
EditorSettings and shutdown resource/ObjectDB/allocator diagnostics remain.
MCP runtime checks pass for direct launch, Replay, level selection, briefing Skip
and completion via Continue callbacks, fade cleanup and fall/checkpoint recovery
without rebuilding the card. Levels 2–4 have no ActHeading. Inspected screenshots
at 1920×1080, 1920×1200 and 2560×1080: no title clipping or HUD overlap; actual
viewport dimensions checked. Final runtime log sample has no warnings/errors.
The existing editor remains stopped on its original Level 1 scene.
Required run_tests.ps1: NUnit 88/88 and GdUnit4 197/197 (report_400; zero failures,
errors or orphans); host Django skips its absent venv. Container Django 104/104
passes using a temporary source/fixture copy streamed through stdin because the
existing /app Docker Desktop bind mount is unreadable (docker cp also fails).
The first isolated attempt lacked countries.json; supplying that fixture resolves
all failures. Expected mocked Resend/Clerk diagnostics remain in passing tests.
Evidence: .validation/act-i/ and act-i-*.log. Diff hygiene passes. No exports,
staging, commits or pushes. Changes remain local and uncommitted.

## Act II preview implementation — 2026-11-02

Act I final results gain a separate ACT II PREVIEW for The Unregistered, a standalone
120-second opening introducing Mission 5: Names Without Numbers. Natural completion
and Skip show a local preview-complete screen with Replay Preview/Main Menu. Exactly
four missions remain playable; preview code calls no campaign, roster, score, save or
session mutation. Existing character atlases/cast are reused with ten newly generated
cached voice cues; layered art uses the established Docker/Blender workflow. Missing
speech keeps subtitles; absent essential visuals complete safely. Localization uses
keys with English fallbacks. Code/tests/production tooling and README/HANDOFF updated;
production and initial verification complete. Build: zero errors, 153 existing
warnings. NUnit 88/88, Godot 203/203 (report_401; zero errors/failures/orphans), copied
source/container Django 104/104 pass. The standard Django wrapper skips its absent
venv; backend /app still has ENODEV. Fourteen PNG/two Ogg asset, cast/hash, timing,
silent-ending and decoded mix checks pass. Inspected 36 shot + three completion
captures at all requested sizes and the results panel. Full 120-second playback
completes with audio stopped, within 28.5 ms sampled clock separation. Production
results entry, Replay, Main Menu and repeated Skip paths pass; progression snapshots
and game_sessions row hashes stay identical. Review harness corrections handle
polling overhead and the existing results node; separate playback/navigation
records remain under .validation/unregistered. Speaker-listening and physical-device
acceptance remain unperformed. Final repeat after the last source/tool edits passes: incremental C# build zero
warnings/errors, background import exit 0 and all sixteen sidecars, NUnit 88/88,
Godot 203/203 (report_402; zero errors/failures/orphans), explicit copied-source
container Django 104/104. Known addon TLS/EditorSettings/shutdown diagnostics remain;
reference checker retains its existing ten addon/menu errors and 27 warnings.
Python compilation/diff hygiene pass. Artwork helper stopped; four normal services
remain up. Editor stopped on its original Level 1 scene. Existing uncommitted Act I
title work preserved.
No export, staging, commit or push authorized or performed.

Act II input follow-up: keyboard arrows/Enter and injected controller D-pad/A
were verified through actual GUI dispatch for completion focus, Replay Preview
and Main Menu. Physical-device acceptance remains separate.

## Act I heading and Act II preview publication — 2026-11-02

User explicitly requested pushing the current changes, authorizing the local commit
and normal push. Publication scope includes the existing Act I title label, completed
Act II preview, new art/audio/provenance, tests, production/review tools and related
documentation. Reviewed tracked/untracked inventory and diff hygiene; fetched origin
and confirmed main matches origin/main before publication. Ignored credentials,
cached speech requests, validation evidence and test reports remain excluded.
This Git/documentation-only task requires no new import or tests: prior final build,
import exit 0, NUnit 88/88, Godot 203/203 (zero errors/failures/orphans) and isolated
Django 104/104 remain applicable. Known diagnostics and human listening/device
acceptance limitations remain documented. No exports or additional code/asset edits.
