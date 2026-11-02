# Axiom Force - Strategic Inter-Agent Handoff Briefing

## Tomorrow resume / publication — 2026-11-01

User requested pushing the current fortress work and continuing tomorrow.
Implementation commit a17c83e was pushed to origin/main and its remote SHA verified.
This publication record is included in a documentation-only follow-up commit.
Implementation and regression verification are complete as recorded below.
Start with tools/gameplay/SHATTERED_AEGIS.md; outstanding acceptance is human
20–25 minute pacing, physical controller/touch and speaker listening. The existing
Docker bind-mount and import diagnostics remain unresolved. This publication
changes documentation/Git state only; existing verification remains applicable.
No additional gameplay work, export or service shutdown is part of this task.


## Revised Act I and fortress finale — 2026-11-01

Implemented the seven-section 28,000px Level 4 assault and revised ensemble story
bible/open future outline. docs/ACT_I_REVIEW.md records a separate Levels 1–3
continuity/teaching/repetition/pacing review; their geometry and cinematics were
not rebuilt. The Level 3 aftermath and Level 4 arrival recordings/timelines remain
unchanged. The campaign still contains four missions and the save schema is unchanged.

AegisProgress owns exact prerequisites and deduplicated authored rewards. Level 4
implements IFiniteCombatHost, reuses finite groups/recovery, and caps ordinary live
actors at four. Required defense groups protect devices; other fights are bypassable.
Seven safe section/arena checkpoints retain recruits, lessons, fragments, objectives
and awarded slots. Real Bulwark and Blink crossings gate lessons. Oceanus/Titan gun
approaches share one controller and reconnect. Thermal and curtain shutdown persists.

AegisBossController opts the existing armored Voss into three phases at 300/200/100
HP, clamped damage, phase-two pylons, nonoverlapping missiles/marked strikes and
five-second stationary melee exposure. Standalone boss/HUD bindings remain compatible.
Missiles have ownership, swept collision, lifetime and genuine parried return travel;
Bulwark absorbs them and Blink does not grant ranged immunity. Attempt recovery
rebuilds all boss state; post-defeat recovery never respawns him. A separate attacked
crossing device precedes safe civilian presentation and immutable ACT I COMPLETE
results. Replay, Level Select and Main Menu are the only finale destinations.

One owned speech scheduler serializes fourteen exact event cues with expiring hints,
priority, English fallbacks/localization keys and pause/recovery/teardown handling.
Cached ElevenLabs multilingual-v2 voices use established IDs and .70/.75/.10/1
settings, with text/request/audio hashes and measured duration receipts. Six original
512px RGBA Blender assets follow the inspected navy/violet/amber/cyan target; art
and voices use the existing media image in an owned unmounted copy-based helper.

Final verification: background import exit 0; all twenty PNG/Ogg sidecars exist.
Incremental C# build: 0 errors/warnings (153 existing warnings on full compilation).
run_tests.ps1 passes NUnit 97/97 and Godot 190/190, zero failures/errors/orphans
(report_388), with no engine ERROR/SCRIPT ERROR entries in the final suite log.
Host Django remains skipped for absent venv; explicit copied-container Django
passes 104/104 after final edits. Art/alpha/margins, audio/hash/duration/unclipped
PCM checks and Python compilation pass. Existing TLS/editor shutdown resource/
PagedAllocator import diagnostics remain; the import is not error-free.

All 30 dressed section/boss/crossing/results captures at 1920×1080, 1920×1200 and
2560×1080 were inspected, preserving the user's Level 1 editor. Diagnostic
traversal and automated real-combat evidence are separate in .validation/aegis/.
The latter reached extraction with actual attacks/abilities, observed peak three
live ordinary enemies, one observed thermal checkpoint return and two tool-side
resumptions without scene reload. Its stalls/timing are not human pacing. Complete
MCP batch metadata reports no new runtime errors; sampled game logs are clean.
Oceanus's upper approach was climbed, doused and reconnected/backtracked using
real movement/ability; the full combat traversal used Titan's maintenance breach.
Titan's measured jump height is 184.71px; he traversed the required 100px stairs.

Finale Replay, Level Select and Main Menu, plus actual Level 3 Continue, mission
selector and saved-session launch routes pass. Replay rebuilds arrival/five
operatives/fresh ledger. The owned temporary save was deleted and verified absent.
No user save was changed. Both owned copy-based helpers are stopped; normal four
services remain running. The pre-existing backend bind mount still returns ENODEV.

Spent devices now remove their attack hurtboxes, preventing combo farming. Actual
player deaths in all three boss phases are checked at the arena checkpoint;
recovery disables the abandoned controller synchronously. Regression also exposed
a pre-existing upgrade-screen GrabFocus call outside the scene tree; its binding
entry now skips viewport focus for standalone tests.
Human first-run pacing, physical controller/touch and speaker listening remain
unperformed. No exports, staging, commits or pushes. Preserve existing memory audit
entries. User's editor scene remains Level 1; normal services remain running.


## Apple release setup — 2026-11-01

Added macos/ios to both launchers and the Docker export driver. macos uses the
universal preset for a release-mode unsigned ZIP with no silent debug fallback.
It and all refuse the example bundle ID before building. ios skips Linux Godot/
.NET compilation and creates a source-only Mac handoff; all also prepares that
handoff after its macOS output. Python stdlib tooling excludes development/
signing files and caches, sanitizes preset secrets, and preserves native addon
bin inputs. Dockerfile adds Python and bakes the helper; rebuild before use.

Apple presets now specify version/build, unsigned universal macOS output and
iOS ARM64/iPhone+iPad/project-only generation. Owner bundle IDs and Apple Team
ID remain deliberately unconfigured. Native Mac helper has an inert default,
read-only --check, and explicit project generation; all Actions paths remain
blocked. Updated all agent rules with the user's Apple native-completion exception.
export/APPLE_RELEASE.md documents the precise Docker/native split, Godot/.NET/
Xcode prerequisites, identity/provisioning, signing/notarization and distribution.

No actual game export, Apple handoff, signing or exporter image build has run.
Helper tests only package a tiny synthetic fixture. Final verification:
four fixture tests, source inventory, shell/Python syntax, Apple target/Actions/
host/placeholder safety paths and Compose validation pass. Background Godot
import exited 0 with the existing TLS/editor shutdown resource diagnostics.
run_tests.ps1 passes NUnit 93/93 and Godot 161/161 (report_369, zero failures/
errors/orphans). Host Django is skipped for absent venv; container Django passes
104/104. Incremental build has zero warnings/errors. No real build/ output exists.
Mac/Xcode, device/AOT/native-addon compatibility and production networking
remain unverified until a real Apple build. Prior export-policy edits are retained.

## Docker-only game exports — 2026-11-01

User requested export reorganization only, with no actual game export. Root
.github/workflows is absent and GitHub's workflows API reports total_count=0,
so no current Actions workflow existed to delete. Historical runs were retained.
All four agent instruction files now forbid Actions-based or host game exports.
Fixed root launchers referencing nonexistent exporter instead of export, removed
the implicit all-target export, validated supported targets before work, and
made invocation independent of the caller's directory. Launchers refuse Actions;
Compose forwards GITHUB_ACTIONS and the container driver rejects CI/host use.
The exporter remains opt-in with its help default. Rebuild its image before a
future authorized export because its driver changed; no image build/export
was performed for the exporter in this task. Normal services were restored for
the mandatory code-edit preflight and remain running (db/Ollama healthy).
Verification: Bash syntax, both launchers' no-target/invalid-target/Actions
paths, driver help/host/Actions refusal, Compose validation and default-profile
exclusion pass. No build/ export output directory was created. run_tests.ps1
passes NUnit 93/93 and GdUnit4 161/161 (report_368, zero failures/errors/orphans).
The wrapper skips absent host Django venv; explicit container Django passes
104/104. Compilation reports 154 existing warnings and zero errors. Website
and backend countries endpoint returned HTTP 200. Only launchers/Compose/docs
changed, so no game-engine import refresh was needed. No staging/commit/push.

## Resume after container interruption — 2026-10-31

The Through the Breach implementation and verification artifacts already exist.
Reviewed the nine-shot contact sheet, asset/audio results, report_361 (142 Godot
tests, zero failures), NUnit 47/47 and container Django 104/104 logs, and the
uninterrupted playback evidence. Corrected a stale tooling-documentation sentence
that still described Level 2 as retaining its old transition. No code/assets
changed during this resume; imports and unit suites do not require rerunning.
Docker restoration completed: Compose pulled the missing images, built backend
and website, and started all four normal services. PostgreSQL and Ollama are
healthy. Website (5173), direct and proxied countries API (8000/5173), and Ollama
tags (11434) returned HTTP 200. No game export, staging, commit or push performed.

## PostgreSQL-only storage and Playwright — 2026-10-31

Removed SessionManager's JSON fallback and moved operative progression plus
display/input documents into PostgreSQL JSONB game_state (schema version 1).
Session increments now use an atomic UPDATE. Host clients read environment/root
.env credentials. New Game stays in the menu on save failure; failed database
loads cannot overwrite progression with defaults. Settings/progression failures
report errors. Standalone progression tests keep simulated state only in memory.

Game and portal tokens are transient; locale lives in the URL. Existing local
saves remain untouched but are ignored, with no automatic migration. Game state
retains the existing shared development database scope. Clerk remains upstream
identity and may maintain its own authentication cookies. Assets, caches, logs,
Docker volumes and model files remain normal tooling/runtime files.

Django tests now use PostgreSQL. Compose aligns backend/database credentials.
Added opt-in Playwright v1.64.1-noble with matching npm runner, init, shared IPC
and nonroot user. Portal builds use same-origin /api. Game integration coverage
checks saves across two managers; the browser smoke check rejects storage API
access. Final verification: C# build passed (zero errors, 154 existing warnings);
background Godot import exited 0 with existing addon TLS/editor shutdown resource
diagnostics. run_tests.ps1 passed NUnit 47/47 and GdUnit4 135/135 with zero
failures/errors/orphans (report_357). The wrapper skipped the absent Windows
backend venv; explicit container Django tests passed 104/104 on PostgreSQL.
JSONB round-trip/version protection and database refusal are covered by NUnit;
campaign create/update/delete across managers is covered by GdUnit4.
Microsoft-image Chromium smoke passed with browser storage APIs blocked.
Website, direct/proxied countries API, and Ollama tags returned HTTP 200; four
normal containers remain running, PostgreSQL/Ollama healthy. No export or Git
publication was performed. Clerk replacement remains a separate scope question.
Historical local-fallback observations below describe older builds.

## Evacuation Under Siege gameplay — 2026-10-30

Level 2 now orchestrates a 35,800px six-sector route with 20 finite formations,
four ferry/island crossings, four checkpoints and the existing three optional
fragments. Recruitment is at x5700 (Oceanus, before practice/required fire) and
x25000 (Titan, beyond the x24400 membrane and before safe Slam rubble). Three
spire formations shield their individual objectives until defeated. The stationary
battery at x30800 has two power relays, a shielded core, one finite reinforcement
formation and local health/phase telemetry. Core destruction ends artillery forever.

Focused `Wildfire*` components own encounters, swept parryable gunner rounds,
2.4-second locked-target artillery, invulnerable local family movement, battery,
asset scaling and localized English fallback. `WildfireProgress` tracks stable
IDs and kill rewards; incomplete groups restart on recovery without farmable score.
Completed encounters, spires, relays, core, barriers, recruits and rescues persist
within this instance. Ordinary fires reignite after eight seconds; civilian-route
fires lock permanently safe before movement. The overpass checkpoint requires its
shelter route secured. Extraction checks all exact prerequisites before the shared
one-time campaign completion path. Reload/replay creates a new ledger; no save migration.

Shared `CodeLevel` additions are default no-op recovery hooks, checkpoint safety
eligibility and a title-card objective override. Delayed recovery/transition callbacks
also guard freed instances. Other mission flow and controls remain unchanged.
RecruitBeat prompts now reflect keyboard/controller/touch device and InputMap ability
binding; the shared controller still directly owns its existing swap/combat keys.
All new systems are constructed before the unchanged arrival freeze/restore snapshot.
Level 2 hides the generic menu-control banner after arrival and restores it on exit.

Generated original facade, shelter, civilian, gunner and battery PNGs through Blender
inside the opt-in cutscene Docker image. Inspected the representative composition
before generating the family. RGBA size/alpha/margins/provenance/import validation
passes. `tools/gameplay/README.md` documents the reproduction and route manifest.
MCP used a separate custom game with autosave=false; the open editor remains Level 1.

Verification: the final 14 gameplay regression cases pass with zero errors,
failures or orphans; NUnit 45/45. Assisted blockout reached all six sector endpoints
with zero falls (~96 seconds total movement; combat cleared diagnostically). Eighteen
dressed captures cover six sectors at 1920x1080, 1920x1200 and 2560x1080; inspection
led to clearer shelter and battery labels. Reference scan has the same ten pre-existing
signal-path errors in untouched main menu/proton_scatter files.
The first full run exposed two older structural-test assumptions: every world
child had a platform Size, and enemies were globally spawned directly under Actors.
Updated those checks to distinguish decorative nodes and activate/inspect a real
nested finite formation. The new 14-case gameplay suite itself passed that run.

Final verification: background import exited 0 and all five PNG imports exist.
Incremental C# build: zero errors/warnings (full compile: 153 existing warnings).
`run_tests.ps1`: NUnit 45/45 and GdUnit4 134/134, zero failures/errors/orphans,
`reports/report_354`. Wrapper skips absent Windows backend venv; explicit Docker
backend Django suite passes 104/104. Python tooling compiles; asset validation and
diff hygiene pass. MCP full-chain verification used real signature abilities,
5.5-second civilian walks, diagnostic enemy/objective damage, and observed automatic
Level 3 loading. Evidence: `.validation/wildfire-gameplay/objectives.json`.
Only the pre-existing host PostgreSQL/local-JSON fallback appeared in live game
errors. Headless import retains existing addon TLS and shutdown/resource warnings,
separate from the clean zero-orphan game suite. The editor remains on Level 1 with
review games stopped; all four Docker services are retained. No game export was performed.
The user subsequently authorized committing and pushing these current changes.
Publication review confirmed main matched origin/main before the new commit; the
Git-only publication task reuses the completed verification above without rerunning tests.

**Outstanding acceptance:** 20–25 minute unassisted first-playthrough pacing is
not established by these diagnostics. The current assisted route time is explicitly
not combat or onboarding time. Human playtesting/content tuning and physical
controller/touch-device testing remain necessary; only synthetic device prompt/input
paths were checked, not a full touch-only gameplay session. No new voiceover was made.

> **Services restored (2026-10-30):** All four existing containers were stopped.
> Started the stack with Compose; PostgreSQL and Ollama became healthy, and
> website, backend countries endpoint, and Ollama tags API each returned HTTP
> 200. The first backend request preceded Django readiness; a retry succeeded
> after its clean system check. No cinematic/code/asset changes in this task.
> README and MEMORY record readiness behavior and verification. Documentation
> only: no Godot import or unit-suite run required; no commit/push performed.

> **Level 2 arrival (2026-10-30):** Latest development commit: `4260282`
> (confirmed by a later history/status read). `ConcreteWildfireMission.OnLevelReady` starts
> `ConcreteWildfireIntro` after construction and before physics. `CodeLevel` captures
> every descendant's processing mode, CanvasLayer visibility and audio pause state;
> one guarded handoff restores them and restarts the opening dialogue's 11-second
> display. Level 2 remains current, no watched flag/save-schema/progression changes.
> Checkpoint recovery reuses this scene. `WildfireStage` analytically samples six
> shots with protected families, membrane/spires, siege silhouettes and Shogun descent.
> Docker Blender/Python tools generate layered checkpoint art and two 60-second tracks.
> Seven cached ElevenLabs MCP cues use Daniel/Adam and fit their windows without cuts.
> Snapshot uses two passes: capture the entire tree before disabling any parent,
> because Godot's mode notification changes descendant audio pause flags. Restore
> processing modes first, then saved visibility/audio flags. Music has regression coverage.
> Final verification: background Godot import exited 0 (all ten asset imports present),
> C# build had zero errors/153 existing warnings; NUnit 34/34, GdUnit4 120/120
> with zero failures/errors/orphans (`reports/report_349`), container Django 104/104.
> Host runner skips Django because its venv is absent; container verification covers it.
> Docker validation passed RGBA dimensions/alpha/atlas bounds, both 60-second tracks
> and all seven cue windows. Godot AI MCP captured all six shots at 16:9 plus close/map
> shots at 16:10 and ultrawide. Real Level 1 extraction Continue, Level Select,
> saved-session confirmation (temporary record removed) and direct Level 2 all start
> the frozen intro with a solo squad. Uninterrupted final playback held position,
> health and score constant; sampled audio/visual clocks differed by about 25-30 ms,
> then natural completion restored gameplay. Synthetic Space input left Jump released
> and mission music resumed unpaused. Skip/fallback/recovery/teardown have unit coverage.
> Evidence: `.validation/wildfire/{review,assets.json,routes-playback.json,input-audio.json}`.
> Existing addon TLS/headless shutdown, missing optional gdunit4.api and PostgreSQL
> authentication/local JSON fallback diagnostics remain; no new runtime errors found.
> Physical controller/touch hardware, speaker listening and exports were not tested.
> Editor remains on Level 1; review games stopped, four Docker services retained.
> User subsequently authorized Git publication of this implementation; see MEMORY.MD.
> Open-world exploration is deferred; existing recruitment/objectives remain unchanged.


> **Tokyo Remembers implementation (2026-10-30, resumed by user):** Level 1 now
> captures extraction score/combo/operative, records campaign progress once, and
> disables mission descendants (including explicit Always/Pausable nodes). The
> new independently processing `TokyoRemembers`/`TokyoStage` overlay uses one
> 40-second analytic timeline and converges skip/end/missing-art on guarded results.
> Level 1 remains current for Replay. `CyberVictoryScorecard.ShowMissionScorecard`
> accepts a next-mission number, prepares the squad on Continue, and preserves
> existing grade thresholds/final-mission callers. Other mission flows are unchanged.
> The original three-argument `ShowScorecard` Godot binding remains intact (Godot
> dynamic calls do not apply C# optional defaults for a newly added argument).
> Six English ElevenLabs MCP cues use Daniel/Adam; Docker Blender/Python tools
> generate original planar dojo damage, poses, Data-Blade and quiet atmosphere.
> Opening artwork is reused without changing its cinematic or dialogue. Translation
> keys include speaker labels and English fallback. No new operative unlocks or
> watched flag. Final C# build passed with zero errors and 153 existing warnings.
> Background import exited 0 and generated every new asset's `.import`; existing
> addon TLS and editor shutdown/leak diagnostics remain. Full runner passed NUnit
> 34/34 and GdUnit4 114/114, zero failures/errors/orphans (report_346). Its host Django
> step skipped the absent venv; container Django passed 104/104 afterward.
> Docker asset validation passed RGBA dimensions/alpha/pose bounds and both 40-second
> audio tracks, with all six cues inside their shot windows. Godot AI MCP captured
> all six shots at 16:9 plus close/uplink shots at 16:10 and ultrawide, then verified
> results stay in Level 1, Replay restores active Level 1, natural audio completion
> reaches results, Continue loads Level 2 with a one-operative squad, and Level
> Select/Main Menu retain their destinations. Evidence is under `.validation/tokyo/`.
> Runtime diagnostics contain only the existing PostgreSQL authentication/local
> JSON fallback. The user's editor remains on Level 1, test game stopped, all four
> Docker services retained. A separate uninterrupted real-time playback verified
> audio/visual clock alignment at six checkpoints, natural completion at exactly
> timeline 40, and unchanged 100 health/150 score (`full-playback.json`). Physical
> touch/gamepad hardware and exports were not tested. Changes are local and uncommitted.

> **Docker startup only (2026-10-30):** User explicitly deferred post-Level 1
> cutscene work. The active Docker environment had no project containers/images;
> Compose pulled dependencies, built backend/website, and started db, backend,
> website, and Ollama. Website, backend countries endpoint, and Ollama tags API
> returned HTTP 200 via IPv4 loopback; PostgreSQL reported healthy. No game code
> or assets changed. Only workflow/memory documentation updated; no import or
> unit suites required. Do not resume cutscene work without user direction.

> **Memory updates mandatory after every task (2026-10-30):** Added the same
> rule to root AGENTS, CODEX, CLAUDE, and GEMINI instructions. All agents must
> update `MEMORY.MD` before finishing any task, including read-only, planning,
> review, documentation-only, and Git-only tasks. Preserve the exact uppercase
> filename, keep durable context concise, exclude secrets, and distinguish
> actual verification from plans. README and MEMORY document the requirement.
> Diff hygiene and rule consistency checked; documentation-only changes require
> no Docker preflight, Godot import, or unit tests. No commit or push performed.

> **Project memory created (2026-10-30):** Added root `MEMORY.MD` for durable
> preferences, required skill usage, live cinematic decisions, integration
> context, and known verification limits. README links to it. Memory distinguishes
> historical test results from current checks and defers to current user/agent
> instructions. Verified against the current cinematic documentation and scene;
> this task changes Markdown only, so no import, unit tests, or container changes
> are required. Changes are left uncommitted.

> **Game-development skills installed (2026-10-30):** Installed the router and
> all 77 specialized skills from `gamedev-skills/awesome-gamedev-agent-skills`,
> pinned to commit `19d93f97c9c1ea0f8b306e77aa81e0d3e352d3e9`, using the Codex
> skill-installer helper. Independent global copies live under `~/.agents/skills`
> (Codex), `~/.claude/skills` (Claude Code), `~/.gemini/config/skills`
> (Antigravity app/IDE), and `~/.gemini/antigravity-cli/skills` (CLI).
> Updated CODEX, CLAUDE, GEMINI, and AGENTS with the same mandatory router-first
> rule for game-related work; README records the setup. Supporting references,
> scripts, and assets were installed alongside each skill. Existing skills were
> not overwritten. Installation verification checks counts and file hashes
> across all four roots; interactive discovery must be confirmed in each app.
> New skills become available on the next turn; restart/reload other agents if
> needed. Project changes are documentation-only, so no Godot refresh or game
> unit tests are required, and no containers were touched.

> **Mandatory Docker skills (2026-10-30):** Updated root `CODEX.md`, `CLAUDE.md`,
> `GEMINI.md`, and `AGENTS.md` with the same requirement to read and apply the
> relevant official `docker/skills` skills (`docker-skills@docker`) before any
> Docker-related action or guidance, including container preflight checks.
> Direct reading of installed skill files is the fallback when discovery does
> not expose them; missing access must be resolved before Docker work continues.
> README documents the workflow, and CLI examples should use `docker` from PATH.
> Documentation-only change: no containers were touched, and no Godot refresh or
> unit tests were required. Verification checks rule consistency and diff hygiene.



> **Godot AI dock "No server command found" — uv now installed on Windows (2026-10-29):** The dock showed `No server command found`, `uv not found`, and *"No godot-ai server found. Install `uv` via the Setup panel above, or run `pip install godot-ai`"*, with plugin v4.3.1 (`addons/godot_ai/plugin.cfg`). **The assumption that uv/godot-ai were already installed "locally in this directory" was wrong, and the plugin's own rules explain why:** `client_configurator.gd` resolves a server in three tiers — (1) dev `.venv`, (2) `uvx`, (3) a system `godot-ai` — and on Windows tier 1 requires `.venv\Scripts\python.exe` **plus a sibling `src/godot_ai/`**. The repo's `./.venv` is the **Linux/Docker venv** (`.venv/bin/python` → `/usr/local/bin/python`, no `Scripts/`), `src/godot_ai/` does not exist in this repo (godot_ai lives under `addons/`), and there was no `uv.exe`/`uvx.exe` anywhere in the tree — uv had only ever run inside the `python:3.14-slim` container. Tiers 2 and 3 therefore both missed and the dock had no command to run. **Fix:** installed **uv 0.13.6 (x86_64-pc-windows-msvc)** into `%LOCALAPPDATA%\Programs\uv` — one of the exact directories `clients/_cli_finder.gd::_well_known_dirs()` searches on Windows (also `~/.local/bin`, `~/.cargo/bin`, `%ProgramFiles%\uv`) — and appended it to the user `PATH`. The official `irm https://astral.sh/uv/install.ps1 | iex` installer **could not be used on this machine**: it aborts with "The 'Get-ExecutionPolicy' command was found in the module 'Microsoft.PowerShell.Security', but the module could not be loaded", so the release zip was extracted directly instead (see the README prerequisite note for that recipe). Pre-warmed the pinned tool env with both the plain and the **exact production argv** (`--isolated --no-config --no-env-file --no-sources --no-build --index-strategy first-index --keyring-provider disabled --index/--default-index https://pypi.org/simple --find-links https://pypi.org/simple/godot-ai/ --from godot-ai==4.3.1 godot-ai`): 73 packages installed, `godot-ai 4.3.1`, exit 0. **Verification:** a throwaway `-s` harness (created, run, then deleted — no project file remains) calling `ClientConfigurator.get_server_launch_mode()` / `get_server_command()` in headless Godot printed **`MODE=uvx`**, `MCP | using uvx (godot-ai==4.3.1)`, and `ARG[0]=C:\Users\karthiknk21/AppData/Local/Programs/uv/uvx.exe` with the 19-arg production command — i.e. the dock now has a real server command. **Follow-up for the user:** the dock caches "uv: not found" for the session, so click **Reload Plugin** in the dock (or restart the editor) to re-probe. The Linux `./.venv` is deliberately left alone: it is the container venv and must not be pointed at by the Windows editor; use `GODOT_AI_VENV_PYTHON` only with a real Windows interpreter (`.venv\Scripts\python.exe`) if a dev-venv override is ever wanted. Documentation-only repository changes in this task, so no Godot import refresh and no unit-suite run were triggered.

> **Host .NET SDK repair — Godot "Failed to load hostfxr" (2026-10-29):** Launching the .NET editor failed with *"Unable to load .NET runtime, specifically hostfxr"*. **The Docker image cannot fix this and the request to "point Godot at the C# image from Docker" is not achievable:** Godot is a native Windows binary that loads Windows `hostfxr.dll` via nethost, while `export/Dockerfile` carries `mcr.microsoft.com/dotnet/sdk:10.0` (Linux ELF) — the only way to use it would be running the editor inside a Linux container with a virtual display, which is not the Windows editor the user launches. **Actual cause:** the host had no .NET at all — no `dotnet` on PATH, and `C:\Program Files\dotnet\host\fxr` absent. `AxiomForce.csproj` and `tests/AxiomForce.Tests` both target `net10.0`. **Second, separate fault found while repairing:** `winget install Microsoft.DotNet.SDK.10` (10.0.403) reports "Successfully installed" and lays down 5,099 files (`sdk/10.0.403`, `shared/`, `packs/`, `templates/`) but **never writes `C:\Program Files\dotnet\dotnet.exe` or the whole `host/` directory** — watched for 5 minutes, they never appear, so they are blocked at write time rather than quarantined afterwards (Norton 360 is installed; Defender has no detections). An unelevated `/repair` run rolled the bundle back and deleted the folder entirely, so that path is a dead end without admin rights and an AV exclusion. **Working fix:** the zip-based per-user install — `dotnet-install.ps1 -Channel 10.0 -InstallDir "$env:LOCALAPPDATA\Microsoft\dotnet" -NoPath` — produced a complete SDK (`dotnet.exe`, `host/fxr/10.0.14`, `dotnet --version` → `10.0.403`), and `DOTNET_ROOT` was set for the user (plus that folder appended to the user `PATH`) so nethost resolves `hostfxr` for GUI launches. **Verification:** control run of `Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . --quit` with `DOTNET_ROOT` unset reproduced the reported failure verbatim (`Failed to load hostfxr`, `find_hostfxr modules/mono/mono_gd/gd_mono.cpp:182`, segfault, exit 139); the same command with `DOTNET_ROOT` set exits **0 with no `.NET` errors**, and `dotnet build AxiomForce.csproj` completes with **0 errors** (153 pre-existing CS8618/CS0169 warnings). **Caveats / follow-up:** the `DOTNET_ROOT` fix only reaches *newly* launched processes, so Explorer must pick it up (sign out/in or restart Explorer) before a double-click launch is clean; the leftover half-installed `C:\Program Files\dotnet` (5,099 files, no `dotnet.exe`) still needs an elevated delete, and the machine `PATH` still lists it (harmless — it is searched first, finds nothing, and falls through to the user entry). README prerequisites now carry the symptom, the per-user install and the `DOTNET_ROOT` step. Documentation-only changes in this task, so no Godot import refresh and no unit-suite run were triggered.

> **Stack rebuilt after Docker state wipe (2026-10-29):** `docker compose up -d --build` was re-run from scratch (0 images, 0 volumes beforehand) and the always-on group is up and verified: `db` healthy on 5432 with all 9 migrations applied to a fresh `pgdata`, `backend` Django 6.1.2 on 8000 (`GET /api/auth/countries/basic/` → 200, `/admin/` → 302, `/api/` → 404 because only `api/auth/` is routed), `website` 200 on 5173 with the AXIOM FORCE title and `/api/...` → 200 through the Nginx proxy, `ollama` healthy on 11434 answering `{"version":"0.40.13"}`. The `pgdata` and `ollama` volumes are new, so the database and model store are empty. Nothing was exported: the `export` profile stayed unused and `build/` does not exist.

> **Compose: always-on group, exclusive export group, Ollama, Node 26 (2026-10-28):** The stack now has two tiers. **Always-on** (`docker compose up`) = `db` (PostgreSQL 18, 5432), `backend` (Django, 8000), `website` (Nginx, 5173) and **`ollama`** (official `ollama/ollama:latest`, 11434, models in the `ollama` volume; verified `{"version":"0.40.12"}` on `/api/version`). **Exclusive** = `export`, which sits behind the `export` profile so `up` never starts it. The rule (documented in all four agent guides + README) is: game edits and website edits may run concurrently with the stack, **game exports may not overlap anything** — not another export, not a Godot import, not a website image build. The exporter is `export/Dockerfile` (Godot 4.7.2 mono + .NET 10 SDK + Android cmdline-tools/platform-34/build-tools-34 + **JDK 17 only** + `4.7.2.stable.mono` templates) driven by `export/export.sh`, and its `.godot`, `bin` and `obj` are mapped to **named volumes** — a full import through the Windows bind mount ran at ~1 KB/s (the cache is 5.7 GB of small files) against 29.7 MB/s raw sequential throughput, so imports now stay Linux-native and only finished artifacts cross to `./build`. `libfontconfig1` and `libgl1` were added after Godot warned that `libfontconfig.so.1` was missing. `website/Dockerfile` moved to **`node:26-alpine`** (v26.12.1). The unused **`eclipse-temurin:latest` (JDK 25)**, `python:latest` and `python:3.13-slim` images were deleted so JDK 17 is the only Java in the local store. `.gitignore` now ignores `build/` whole. **Verification (2026-10-28):** `docker compose config` valid; `up -d --build` brings db/backend/website/ollama up; website returns 200 with the AXIOM FORCE title and was built with `node:26-alpine`; Ollama answers on 11434; backend `/admin/` returns 302. **Export guard (2026-10-28):** an export cannot start by accident. The service stays behind the `export` profile (so `compose config --services` and `docker compose up` show only `db`, `backend`, `ollama`, `website`), the service has `command: ["help"]` and `restart: "no"`, **the image's own `CMD` is `help`** (so even a bare `docker run axiom-force-export` is inert), and `export/export.sh` exits with usage text when no target is passed — the build/import/export steps sit *after* that guard. Verified by bringing the service up with the profile: it printed "No export requested — nothing was built" and exited 0, and no `./build` was created. Note the script is baked into the image, so `docker compose --profile export build export` is required after editing it (the first attempt ran the old copy and started building). `COMPOSE_PROFILES` is not set in `.env`, so nothing auto-enables the profile. **Exports are NOT end-to-end verified:** the toolchain is verified (Godot 4.7.2 mono, .NET 10.0.403, JDK 17.0.20.1, sdkmanager 12.0, templates present, C# Release build clean apart from pre-existing CS8618 warnings), but the user stopped the Linux preset run mid-export, so no export has been produced or checked; every export container was removed, `./build` was deleted, and no binaries, `.pck` or `.apk` files exist in the tree.

> **Docker Compose stack (2026-10-28):** `docker-compose.yml` now runs the whole service stack — `db` (`postgres:latest`, 18.6, published on 5432, named `pgdata` volume, `pg_isready` healthcheck), `backend` (built from the new `backend/Dockerfile` on `python:3.14-slim`, bind-mounted `./backend:/app`, published on 8000, waits for a healthy `db`), and `website` (new `website/Dockerfile`: Node 20 Vite build served by Nginx with the existing `website/nginx.conf` proxying `/api/` and `/admin/` to `backend`, published on 5173 to match `FRONTEND_URL`). Both images got a `.dockerignore` so the local `.venv` and `node_modules` never enter a build context. Two portability bugs were fixed on the way: **PostgreSQL 18 refuses the old `/var/lib/postgresql/data` mount** (it stores data in a version subdirectory), so the volume mounts at `/var/lib/postgresql`; and **`backend/entrypoint.sh` was checked out with CRLF**, which made the bind-mounted copy fail with `exec ... no such file or directory` even after the image normalised it — the file is now LF in the repo and `.gitattributes` keeps `*.sh` at `eol=lf`. **Verification (2026-10-28):** `docker compose config` valid; `docker compose up -d --build` brings all three services up, with `db` healthy and the backend log showing the entrypoint wait for PostgreSQL, the migration pass and Django 6.1.2 serving; `GET /` on 5173 returns 200 with the AXIOM FORCE title, `GET /api/auth/profile/` on 5173 returns Django's 401 JSON through the Nginx proxy, `GET /admin/` on 8000 returns 302, and `POST /api/auth/register/` returned 201 with the row visible in `accounts_user` inside the `db` container (smoke-test user deleted afterwards). The preflight bullet in the four agent guides now points at `docker-compose.yml` instead of the "no compose file yet" fallback, and `README.md` has a "Running the Stack with Docker" section. The stack is left running.

> **Docker workflow adopted (2026-10-28):** The user is moving the deleted WSL-container tooling to Docker Desktop. The `NO Docker CLI / Docker Desktop` rule was removed from the Permissions Denylist in `AGENTS.md`, `CLAUDE.md`, `CODEX.md` and `GEMINI.md`; the Git-commit and standard-WSL-distro rules remain. Official images were pulled and version-checked against the local Docker (29.9.2, Linux daemon): `python:latest` (newest CPython; not kept, see the venv note below), `mcr.microsoft.com/dotnet/sdk:latest` (10.0.403), `eclipse-temurin:latest` (OpenJDK 25.0.4.1 LTS — too new for Android Gradle, which wants 17/21), `postgres:latest` (18.6, matching the documented PostgreSQL 18) and `thyrlian/android-sdk:latest` (community image chosen by the user: JDK 17.0.12 plus cmdline-tools and licenses only — platform-tools, platforms and build-tools still must be installed with `sdkmanager`). No compose file or Dockerfile exists yet. **Python venv (2026-10-28, follow-up):** `python:latest` resolved to a CPython release too new to have prebuilt wheels for part of the `godot-ai` dependency tree, so pip fell back to compiling PyYAML and friends from source and had not finished after 17 minutes. Switching to **`python:3.14-slim`** (the project's own target) and installing with **`uv`** produced a complete `./.venv` in about 4 minutes: Python 3.14.10, `django 6.1.2`, `godot-ai 4.3.1` (79 distributions, both packages at their latest PyPI release). The venv was created by mounting the repo at `/app`, so it is a **Linux venv bound to `/app/.venv`** — use it from inside a `python:3.14` container (e.g. `docker run --rm -v "$PWD:/app" -w /app python:3.14-slim .venv/bin/python ...`), never from the Windows Python, and keep the mount path stable or recreate it. `.venv/` was already gitignored; the host OS Python was not touched. **Version-reference sweep (2026-10-28):** every mention of the discarded CPython releases is gone — this entry no longer names them, `README.md`, `AGENTS.md`, `CLAUDE.md`, `CODEX.md`, `GEMINI.md` and `website/README.md` all say Python 3.14, and the vendored `godot_ai` uv-cache comment (`addons/godot_ai/utils/uv_cache_cleanup.gd` plus the two backup copies under `addons/.godot_ai_update/backup/`) now uses version-agnostic `cp3XX` wheel tags instead of a concrete interpreter ABI tag. The archived `.zip` payloads in that backup folder still contain the old text; they are binary snapshots and were left untouched. **Docker preflight rule (2026-10-28):** `AGENTS.md`, `CLAUDE.md`, `CODEX.md` and `GEMINI.md` now open their Development Workflows section with "Docker Container Preflight (Before Editing Code Files)": before any task that edits code, an agent runs `docker ps`, starts Docker Desktop if the daemon is down, brings the stack up with `docker compose up -d` when the project containers are absent, and reads `docker compose logs <service>` if one exits. Read-only, review, documentation-only and Git-only tasks are exempt, and agents must never stop containers the user started. **This rule currently has no compose file to act on** — writing it is still the outstanding step, and until then the rule instructs agents to start whatever the repository defines and record the gap here. `README.md` carries a matching one-paragraph summary under Contributing & Agent Guidelines. **Verification (earlier addon-comment edit):** the Godot import refresh was run and the .NET-hosted engine segfaults at startup (missing `hostfxr`, no .NET SDK on this machine), so neither the refresh nor the GdUnit4 suite could complete; the addon edit is comment-only, confirmed by `git diff` showing 2 changed lines with no code changes.

> **WSL Containers tooling removed (2026-10-28):** At the user's request the entire native-WSL-Containers ("wslc") stack was deleted: the four root scripts (`setup_wsl_containers.ps1`, `run_wsl_containers.ps1`, `stop_wsl_containers.ps1`, `export_game_wsl.ps1`), `scripts/wsl_containers/fetch_image.py`, the three image definitions (`backend/Containerfile`, `website/Containerfile`, `export/Containerfile` with `export/export.sh`), and the machine-local artifacts `.container_cache/` and `.local-ca/`. Stale references were stripped from `README.md` (tech-stack row and "WSL Container Architecture" section), `AGENTS.md` / `CLAUDE.md` / `CODEX.md` / `GEMINI.md` (architecture section), `website/README.md`, `.env.example`, `.gitignore` (the `.local-ca/` rule) and doc comments in `backend/core/settings.py` plus `backend/accounts/onboarding_views.py`. The Docker and standard-WSL-distro prohibitions stay in the agent permissions denylist: they are environment policy, not this tooling. **Consequences:** there is no one-command way to start PostgreSQL + Django + Nginx together (export `POSTGRES_*` / `DB_*` yourself — nothing parses `.env` automatically — then run `python backend/manage.py runserver` and `npm run dev` in `website/`), and there is no automated headless exporter. Root `export.sh` / `export.bat` were left in place but they already invoked a `docker compose` `exporter` service whose compose file does not exist, and the `export/Containerfile` they depended on is now gone, so both are dead scripts; delete or rewrite them in a follow-up if export automation is wanted. **Verification (2026-10-28):** `run_tests.ps1` could not run in this environment — `backend/.venv` is absent and neither `django` nor DRF is installed for the system Python, nothing listens on PostgreSQL 5432, and no .NET SDK (`dotnet`) is on PATH, so the NUnit suite is unavailable and the .NET-hosted Godot binary aborts at startup on missing `hostfxr`. Ran instead: `python -m py_compile` on both edited backend modules (both parse clean) and a repo-wide sweep for `wsl_containers|export_game_wsl|frog_network|container.exe|Containerfile|local-ca|WSL Container` (no hits outside this handoff entry). No game engine, scene, shader or asset file was created, modified or deleted, so no Godot import refresh was required. Re-run `run_tests.ps1` on a machine with the .NET 10 SDK, a populated backend virtualenv and PostgreSQL before treating this cleanup as fully verified.

> **Documentation & Legacy Cleanup (2026-10-26, Antigravity):**
> - **Legacy Planning Cleanup**: Permanently deleted `UNITY_MIGRATION_PLAN.md` per user request, retiring obsolete engine migration planning.
> - **Markdown Documentation**: Synchronized operational guidelines across `AGENTS.md`, `README_Asset_Import.md`, `website/README.md`, and `docs/full_game_plan.md` to reflect current 2D Cyber-Action canon and testing protocols.
> - **Testing & Refresh Protocol Refinement**: Updated `AGENTS.md`, `CLAUDE.md`, `CODEX.md`, `GEMINI.md`, and `README.md` to clarify that unit test suites and Godot background imports are triggered strictly after code, scene, shader, or asset changes (`.cs`, `.gd`, `.tscn`, `.py`, `.json`, art/sound), and are skipped for documentation-only (`.md`) edits/deletions.
> - **Automated Suite Integrity**: Fixed backend environment packages (`django`, `pyjwt`) ensuring 100% pass across all 3 test suites (`run_tests.ps1`).

> **Airborne Combat & Aerial Locomotion (2026-10-24, Antigravity):**

> - **Issue Addressed:** Players previously could not attack or dash while airborne in Level 1 or across any mission, as `CyberShogun.cs` strictly required `IsOnFloor() && !_isAttacking` to initiate actions. This made attacking hovering enemies (e.g. `PalaceSentry` drones) extremely difficult.

> - **Changes in `scripts/actors/CyberShogun.cs`**:

>   - Removed the `IsOnFloor()` barrier in `HandleSpecialActions()`, enabling Katana Slash (`F` / `attack`), Signature Ability / Martial Strike (`Q`), and Blade Dash (`Shift` / `C`) both on the ground and in mid-air.

>   - Grounded vs Airborne physics tuning: grounded attacks zero out horizontal velocity (`Velocity.X = 0f`) to anchor the swing, whereas aerial attacks preserve existing horizontal trajectory momentum (`Velocity.X`) so the player smoothly completes their jump arc while slicing.

>   - Aerial Blade Dash: suspends vertical falling at initiation (`Velocity = new Vector2(dashDir * dashSpeed, IsOnFloor() ? Velocity.Y : 0f);`), giving responsive, snappy air dashes reminiscent of *Katana Zero* and *Ghostrunner*.

>   - Action completion: only resets horizontal velocity to zero if `IsOnFloor()`, preventing jarring dead stops in midair when an aerial attack completes.

> - **Changes in `scripts/actors/MainCharacter.cs`**: Updated legacy controller to match the non-blocking airborne attack paradigm.

> - **Automated Coverage**: Added `test_cyber_shogun_airborne_attacks_and_dashes` in `tests/test_cyber_combat_hud_and_mechanics.gd` validating that all 3 combat actions trigger while airborne and transition smoothly.



> **Earned-operatives campaign (2026-10-22, supersedes the Protocol Zero, character-selection-first and old level 2-4 notes below):**
> - **Flow:** New Game → AXIOM-7 briefing (`StoryIntro.cs`) → Mission 1 (`CampaignProgress.StartNewCampaignAsync` creates the save with Cyber Shogun). The roster screen (`CharacterSelection.cs`, button "SQUAD ROSTER" on the main menu) is no longer part of new-game flow: locked operatives are marked, "Set Active" chooses who starts the next mission, Escape returns. **Protocol Zero is deleted** (`TrainingDirector`, `TrainingTarget`, `training_protocol.*`, its test, the capture tool, the level-select replay button); `TrainingArtwork.cs` is kept because `PalaceOperative` draws the approved art with it.
> - **Unlock rules** live in engine-free C#: `LevelCatalog` (ordered missions, who joins, required ability tags) and `OperativeRoster` (unlock/active/cycle/lesson-seen). The squad is *derived from progress*, not saved: at the start of mission N it is Shogun plus everyone granted by missions < N (so no DB column or Django change was needed; old saves keep working). `CampaignProgress` syncs `Global.selected_character` / `Global.unlocked_levels`, rebuilds the roster before every mission launch (level select, Continue, intro) and advances the save on completion. Joins: M2 Oceanus then Apex Titan; M3 Aura Phoenicia then Mirage Blade; M4 Surya Prime. Mid-mission joins are replayed if the mission is reloaded.
> - **Swap:** Tab / Shift+Tab, 1-6, gamepad D-pad Down (next) / Back (previous). `CyberShogun.SwapOperative` keeps the health/shield *ratio* (no swap-healing), carries the dash cooldown, applies a 1 s lockout, and updates `Global`. `SquadStrip` (HUD) shows active/unlocked/locked slots. `TrainingArtwork` reloads art on profile change.
> - **Signature abilities (Q, alongside the existing secondary strike):** `CyberShogun.TriggerSignatureAbility` — parry (0.45 s window reflects *ranged* damage and raises `ParryDeflected`), douse, slam, resonate, phase (0.8 s hazard immunity + short blink), bulwark (1.4 s immunity to hazards and ranged fire). Ranged attackers must call `TakeDamageFrom(damage, knockback, source)`; sourceless `TakeDamage` is treated as environmental. (`TakeDamage` keeps its 2-arg signature because GDScript tests call it that way; Godot C# ignores default parameters from GDScript.) Gates are `AbilityGate` (fire, rubble, sonic lock, heat vent) found through the `ability_gates` group, plus lethal always-on `CyberLaserGrid2D` curtains cleared only by Phase. Rubble/locks/gates reach the world ceiling and `CodeLevel` adds an invisible ceiling so wall-jumping cannot bypass a gate.
> - **Lessons:** `RecruitBeat` unlocks the operative on proximity, shows story lines and a prompt (swap key + ability), and completes when that operative uses their ability; never blocks. Shogun's parry lesson is in Mission 1.
> - **Levels:** `CodeLevel` (new base class extracted from the old `PalaceMission`) owns backdrop/parallax, camera, HUD, shrines, fragments, title card (from `NarrativeManager`), pause, scorecard on the final mission, chasm recovery, completion. Subclasses: `PalaceMission` (M1, painted backdrop, shielded beacon needs two parries), `ConcreteWildfireMission` (M2), `NeonRustMission` (M3), `ShatteredAegisMission` (M4, spawns `CyberBossMech2D` on entering the arena, binds the HUD boss bar). Levels 2-4 use `SkylineBackdrop` (procedural parallax city, palettes per level) and new `PalaceSurface.Concrete/Steel`. Old tilemap level2-4 scenes were replaced by 6-line scenes.
> - **Story:** `english.json` v3.1 (level 0 removed, `joins` lists, Surya's L4 line), `docs/STORY_BIBLE.md` has the earned-operatives table. Other locale files are still v2.0 and fall back/stale as before.
> - **Verification (2026-10-22):** `dotnet build` clean; GdUnit4 100 test cases, 0 errors/failures/orphans (new: `test_operative_swap`, `test_signature_abilities`, `test_campaign_levels` incl. a reachability audit of every shrine/objective/fragment with conservative jump limits and a gate-meets-floor check; updated level-selection, level 1, HUD, hazards and boss tests); NUnit adds `OperativeRosterTests`. Screenshots of every level were inspected. The audit found and fixed gate walls in M2/M4 that stopped 600 px above the floor.
> - **Not done / limitations:** nobody has played M2-M4 end to end (jump limits are audited, difficulty and pacing are untested); no VO; levels 5-9 not built (add a `LevelCatalog` row + a `CodeLevel` subclass + scene); mid-mission joins are not saved (they replay on reload); the roster screen's dossier text/portrait for locked operatives is still fully visible (only the button state is locked); the isometric files (`Isometric*.cs`, `isometric_operative.tscn`, `ghost_gridlock.png`, `campaign_atlas.png`, art tools) and the old `finish.gd`/`collectable.gd` are unused but kept (`CyberCombatHud` still references `IsometricOperative`); pause menu has no roster entry (swap in-level instead); `Palace*` class names are historical.

> **Level 1 platformer redesign (2026-10-22, supersedes the isometric Level 1 below):** `scenes/gameplay/level1.tscn` now only attaches `scripts/gameplay/PalaceMission.cs`, which builds everything in code: a painted parallax backdrop (`assets/images/level1/palace_garden_painted.png`, manual parallax on a CanvasLayer), ~9,500 px of procedural geometry (`PalacePlatform` stone/roof/wood/wall/gate surfaces, one-way timber ledges, `PalaceMovingPlatform` ferry), `PalaceOperative` (the selected operative on the platformer `CyberShogun` controller with `TrainingArtwork`, i.e. the approved AI-generated Shogun sheets), `PalaceSentry` hover drones, `PalaceObjective` props (beacon, Data-Blade, exit, shrine checkpoints, memory fragments) and the shared laser-grid / surge-floor hazard scenes. Route: Garden Gate → Lantern Court (laser bridge) → Blossom Gorge (wall-jump shaft with optional ledges) → Rooftop Run → Archive Descent → Beacon Court. Beacon destroyed → `DojoGate` opens → Data-Blade → two enforcers spawn → extraction unlocks Level 2. Design limits: vertical steps ≤150 px, gaps ≤220 px so every operative (jump 940–1060, speed 340–460) can finish. Chasm falls (Y>950) respawn at the last shrine with a 15-damage penalty; defeats respawn there after 0.9 s. The old isometric files (`Isometric*.cs`, `isometric_operative.tscn`, `ghost_gridlock.png`, `campaign_atlas.png`, `tools/art/render_level1_iso.py`, `prepare_campaign_shogun.py`) are now unused by Level 1 but left in place (`CyberCombatHud` still references `IsometricOperative`); delete them in a follow-up if the isometric direction is permanently dropped. Not yet play-tested by a human end to end — jump spacing was verified by calculation and by rendered screenshots, not by a completed run. `tests/test_level1_2d_cyber_shogun.gd` was rewritten for the platformer. Verification (2026-10-22): `dotnet build` clean; `run_tests.ps1` → Django 104 OK, NUnit 20/20, GdUnit4 81 cases with 0 errors/failures/orphans; seven rendered screenshots (start, court, gorge, rooftops, archive, beacon court, dojo core) were inspected visually.

> **Unified selection dossier (2026-10-22; now the Squad Roster screen):** Character selection now has only the roster and one right-hand dossier. The former center preview is inside `RightShowcasePanel/VBox/ShogunArtStudy`; the four loadout buttons are the sole ability-preview controls, while movement/melee previews remain separate. Upgrade and Deploy share `VBox/Footer`. Focus links are rebuilt for visible controls and return to the selected operative, including after upgrades. Preview sizing now respects available height. Original artwork and training/gameplay remain unchanged. Scene-path and layout/navigation regression coverage was updated; post-edit verification results are reported with this task's completion.


> **Agent documentation workflow (2026-10-21):** `CLAUDE.md`, `CODEX.md`, and `GEMINI.md` now require every agent to update both root `README.md` and root `HANDOFF.md` before finishing a task that edits the game or website, including related backend, assets, configuration, tooling, tests, or documentation. README covers current behavior and contributor guidance; HANDOFF records implementation decisions, actual verification status, and remaining work. Documentation belongs in the final edit batch before the required refresh/tests. Read-only and Git-only tasks are exempt; commit/push authorization remains separate. This task changes documentation only; post-edit refresh and test results are pending at the time of this entry and must be checked in the task's final report.


> **Protocol Zero (2026-10-21) — REMOVED 2026-10-22, see the earned-operatives entry above:** New campaigns complete AXIOM-7's story briefing before entering the seven-room guided 2D trainer; completing or skipping training starts Level 1. Level selection index 0 replays training. `TrainingDirector` owns local checkpoints, text guidance, pause/focus, and non-scoring completion; it never persists campaign scores/deaths/unlocks. All six operatives use `TrainingArtwork` on their real physics bodies, preserving the trainer's current art. Lessons cover movement, jump/wall mobility, melee, dash, and passive shield recovery. No ElevenLabs dependency. Controller dash travels during attack physics; current-transform hit queries prevent stale or duplicate hits. Training targets are non-blocking layer-2 hurtboxes.

> **Selection art study:** All six operatives have hand-drawn character-selection previews with movement and their own four loadout actions. Cyber Shogun uses the approved AI-generated pose and locomotion sheets in selection and Level 1; `tools/art/prepare_campaign_shogun.py` packs those sources for Level 1. The five other selection sheets live in `assets/characters/hand_drawn`. All six original flat designs and SpriteFrames remain preserved under `assets/characters/rendered` for fallback and other existing scenes. These visuals do not add combat mechanics. `ShogunArtPreview.UseOriginalDesign` still enables the preserved flat-art comparison.

> **Character artwork update (2026-10-20, supersedes the model-render approach):** Characters now use original flat 2D drawings authored in Python and Blender, with outlined planar shapes, solid colors and 2D joint animation. No prior game artwork or models are inputs. `tools/art/draw_flat_characters.py` and `package_character_sprites.py` generate the portraits and SpriteFrames under `assets/characters/rendered`. Do not restore the recovered GLBs, old model-render pipeline, rollback JPG cards or legacy samurai/robot sheets. See `tools/art/CHARACTER_RENDERING.md` for regeneration.

> **Current cleanup status (2026-10-19):** The game now starts at `main_menu.tscn`, uses the 2D operative cards and briefing, and deploys into `level1.tscn`. Scene background photos and project-owned 3D models, animation libraries, gameplay code, shaders, tooling, and web viewers have been deleted at the user's request. The historical 3D inventory and migration tracks below no longer describe files present in the workspace. Shared add-ons remain intact. `TitleLogo.cs` now owns localized title lookup; the website uses text/icon panels. See `tests/test_scene_cleanup.gd` for coverage of all retained scenes.

**Target Audience:** Incoming AI Agent (Claude, Codex, Antigravity)  
**Date / Timestamp:** 2026-10-26  
**Current Branch:** `main` (in sync with `origin/main` at commit `be99afa`)  
**Test Suite Status:** 100% PASSED (Django: 104/104, NUnit: 34/34, GdUnit4: 21/21 suites, 102/102 tests, 0 errors, 0 failures, 0 orphans)

---

## 1. Executive Summary: The 2D Cyber-Action Transition

Axiom Force was originally structured as a hybrid 3D/2D action game. Due to persistent 3D meshing, GLB bone orientation mismatches, and rigging friction from multi-generator assets, **the project is transitioning toward a high-fidelity 2D Cyber-Action platformer** (inspired by *Katana Zero*, *Hades*, and *Ghostrunner*).

Over recent sprints, **Phases 1 through 6 of the 2D Cyber-Action core** have been successfully built, integrated, and verified with automated test suites. However, **several critical parts of the game (main scene bootstrap, tutorial level, story intro, and level select mapping) still rely on legacy 3D scenes**.

This document outlines:
1. Everything built in 2D so far.
2. An exhaustive inventory of remaining 3D components.
3. The roadmap and technical recommendations for the incoming agent to complete the transition.
4. Strict operational constraints (testing protocols and Git push permissions).

---

## 2. Completed 2D Architecture (Phases 1 – 6)

### Phase 1: 2D Operative Selection & Card Showcase
- **Scene/Script**: `scenes/main_menu/character_selection.tscn` / `scripts/main_menu/CharacterSelection.cs`
- High-resolution 2D character portrait cards (`assets/images/operatives/*.png|jpg`) for all 6 operatives:
  1. **Cyber Shogun (Kaito Kurogane)** - Vanguard Striker
  2. **Surya Prime (Arjun Varma)** - Solar Bulwark Tank
  3. **Oceanus (Kiana Vaea)** - Hydro Vanguard Tactician
  4. **Mirage Blade (Zayd Al-Mansoor)** - Phantom Infiltrator Assassin
  5. **Apex Titan (King Tariq Nzinga)** - Electro-Magnetic Juggernaut
  6. **Aura Phoenicia (Maya Khoury)** - Prismatic Sound Weaver
- Tactical ability loadout inspections, animated 2D sprite frames, and attribute stat meters.

### Phase 2: Cyber Shogun 2D Controller & Level 1 Cyber City
- **Scene/Script**: `scenes/gameplay/cyber_shogun.tscn` (`scripts/actors/CyberShogun.cs`), `scenes/gameplay/level1.tscn`
- Grounded katana attacks (`[F]` / sword attack), martial strike (`[Q]` / karate attack), blade dash (`[Shift]` / `[C]`), hitboxes, dynamic camera smoothing, and neon cyber city backdrop (`assets/images/cyber_city_dusk_background.png`).

### Phase 3: Dynamic Combat HUD, Slash Combos, & 4-Level Chain
- **Scene/Script**: `scenes/gameplay/cyber_combat_hud.tscn` (`scripts/ui/CyberCombatHud.cs`), `scripts/actors/CyberDrone2D.cs`
- Full 4-level linear campaign chain: `level1.tscn` $\rightarrow$ `level2.tscn` $\rightarrow$ `level3.tscn` $\rightarrow$ `level4.tscn`.
- Flying patrol drone enemies (`CyberDrone2D`), combo scaling counter (`DOUBLE STRIKE!`, `TRIPLE COMBO!`, `TACTICAL OVERDRIVE!`), health & shield telemetry.

### Phase 4: Wall-Slide/Jump Mobility, Cyber SFX Engine, & 2D Hazards
- **Wall Mobility**: Wall-sliding with downward velocity clamping (`140 px/s`) and outward wall-jumping (`380 px/s`).
- **Interactive Hazards**:
  - `CyberLaserGrid2D.cs` / `cyber_laser_grid_2d.tscn`: Cycling lethal laser beams with periodic hum.
  - `CyberHazardFloor2D.cs` / `cyber_hazard_floor_2d.tscn`: Electrified floor grids with rhythmic surges.
- **Synthesized SFX Engine** (`scripts/tools/generate_cyber_sfx.py`): 6 native 16-bit 44.1kHz PCM WAVs: `katana_slash.wav`, `blade_clash.wav`, `blade_dash.wav`, `drone_explosion.wav`, `laser_hum.wav`, `shield_hit.wav`.

### Phase 5: Vanguard-X Boss Encounter & Victory Evaluation Flow
- **Boss Mech**: `scripts/actors/CyberBossMech2D.cs` / `scenes/gameplay/cyber_boss_mech_2d.tscn` in `level4.tscn`.
  - Phase 1 Tactical Hover with homing plasma missiles (`CyberBossMissile2D.cs` / `boss_cannon.wav`).
  - Phase 2 Overdrive Rage shift at $\le 50\%$ HP (siren alarm `boss_alarm.wav`, crimson glow, 3-way spread missiles).
  - Multi-explosion death sequence awarding 50 points and unlocking extraction.
- **Boss HUD Integration**: Real-time boss health bar and phase badge inside `CyberCombatHud`.
- **Victory Scorecard**: `scripts/ui/CyberVictoryScorecard.cs` / `scenes/ui/cyber_victory_scorecard.tscn` hooked to `finish.gd`, evaluating mission combat ranks (**S Rank / Flawless**, **A Rank / Vanguard**, **B Rank / Operative**) with victory fanfare (`victory_jingle.wav`).

### Phase 6: Multi-Operative Deployment & Character Synergy
- **Registry & Profile**: `scripts/actors/OperativeProfile.cs` (`OperativeRegistry`) providing distinct stats and passives for all 6 operatives:
  - **Surya Prime**: 140 HP, 80 Shield, 15% Armor Damage Mitigation, Solar Amber glow.
  - **Oceanus**: 90 HP, 70 Shield, 25 HP/s Passive Hydro Shield Regen, Oceanic Teal glow.
  - **Mirage Blade**: 80 HP, 40 Shield, 460 Speed, 0.6s Dash Cooldown, 1000 Dash Speed, Emerald glow.
  - **Apex Titan**: 160 HP, 60 Shield, 20% Kinetic Armor Mitigation, Heavy Knockback Resistance, Violet glow.
  - **Aura Phoenicia**: 95 HP, 55 Shield, 8% Mitigation, 0.85s Dash Cooldown, Magenta glow.
  - **Cyber Shogun**: 100 HP, 50 Shield, Balanced Striker, Radiant Cyan glow.
- **Dynamic HUD & Scorecard Telemetry**: HUD badges automatically adapt to operative ability names and theme colors; scorecard displays the deployed operative's full identity.

---

## 3. Inventory of Remaining 3D Systems & Relics

The user explicitly noted: *"Many parts of the game are still in 3D."*  
Here is the exact catalog of what is still 3D in the project:

### A. Engine Startup & Main Menu
1. **`project.godot` Bootstrap**:
   - `run/main_scene="res://scenes/main_menu/3d_menu_experience.tscn"`
   - The game still boots into `3d_menu_experience.tscn` rather than a pure 2D Cyber Command Center.
2. **`scenes/main_menu/3d_menu_experience.tscn` & `scripts/main_menu/ThreeDMenuExperience.cs`**:
   - Contains 3D Meshy GLB models: `Syndicate_Hab_Block`, `Terminal_Kiosk_Node`, `PatrolDroneVehicle`, `HoloProjector`, and 3D lighting.
3. **`scenes/main_menu/character_selection.tscn`**:
   - Still houses a `SubViewport3D` with `World3D` and `Camera3D` for rotating 3D models on a podium (even though 2D character cards and 2D sprite frames are now active).

### B. Story Cutscene & Intro
1. **`scenes/main_menu/3d_story_intro.tscn` & `scripts/main_menu/story_intro.gd`**:
   - The story cutscene transitioning from Character Selection to gameplay is rendered in 3D.

### C. Gameplay Scenes & Level Select Mapping
1. **`scenes/gameplay/training_protocol_3d.tscn` (Sector 0 / Tutorial)**:
   - Full 3D tutorial level using `Level3DManager.cs`, `Player3D.cs`, `FloatingRobotBuddy3D.cs`, `WeaponArea3D.cs`, and `PlayerHUD3D.cs`.
2. **`scenes/gameplay/prototype_3d.tscn`**:
   - Relic 3D level still pointed to by `level_map[1]` in `level_selection.gd`.
3. **`scripts/main_menu/level_selection.gd`**:
   - Currently maps index 0 to `training_protocol_3d.tscn` and index 1 to `prototype_3d.tscn`, whereas `level1.tscn` through `level4.tscn` are the active 2D levels!

### D. 3D Scripts & Test Suites
1. **3D Actor & Gameplay Scripts**:
   - `scripts/gameplay/Player3D.cs`
   - `scripts/gameplay/Enemy3D.cs`
   - `scripts/gameplay/Hazard3D.cs`
   - `scripts/gameplay/WeaponArea3D.cs`
   - `scripts/gameplay/Level3DManager.cs`
   - `scripts/gameplay/HealthBar3D.cs`
   - `scripts/gameplay/FinishZone3D.cs`
   - `scripts/actors/FloatingRobotBuddy3D.cs`
   - `scripts/ui/PlayerHUD3D.cs`
   - `scripts/gameplay/HeroAbilityVFX.cs` (3D particle and mesh generator for 3D selection podium)
2. **3D Assets & Blender Tools**:
   - `assets/models/hq/*.glb` (GLB meshes for operatives and environmental props)
   - `tools/assets/rebuild_all_hero_animations.py`
   - `tools/assets/screenshot_ability_vfx_preview.gd`
3. **Tests Depending on 3D**:
   - `tests/test_protocol_zero_3d.gd` (Asserts `training_protocol_3d.tscn` loads and binds 5 tutorial targets).

---

## 4. Suggested Roadmap for the Incoming Agent

To address *"Many parts of the game are still in 3D"*, here are the high-priority migration tracks:

### Track 1: Level Selection & Campaign Mapping (Immediate Win)
- **Goal**: Re-align `scripts/main_menu/level_selection.gd` so the player directly selects and launches 2D missions (`level1.tscn` through `level4.tscn`).
- **Action**:
  - Update `level_map` in `level_selection.gd`:
    ```gdscript
    var level_map = {
        0: "res://scenes/gameplay/training_protocol_2d.tscn", # (or level1.tscn)
        1: "res://scenes/gameplay/level1.tscn",
        2: "res://scenes/gameplay/level2.tscn",
        3: "res://scenes/gameplay/level3.tscn",
        4: "res://scenes/gameplay/level4.tscn",
    }
    ```
  - Migrate or update `tests/test_protocol_zero_3d.gd` to test the new 2D tutorial/level mapping without breaking CI.

### Track 2: 2D Story Intro / Tactical Briefing
- **Goal**: Replace `scenes/main_menu/3d_story_intro.tscn` with a sleek 2D cyber visual novel / tactical briefing screen.
- **Action**:
  - Display the selected operative's 2D portrait card, mission directives, audio chatter, and a "START MISSION" trigger directing to `level1.tscn`.

### Track 3: 2D Protocol Zero Training Level
- **Goal**: Convert Sector 01 from `training_protocol_3d.tscn` to `training_protocol_2d.tscn`.
- **Action**:
  - A dedicated 2D training arena introducing basic movement, wall-sliding, blade dashing, and parrying against dummy drones.

### Track 4: Main Menu 2D Modernization & Engine Bootstrap
- **Goal**: Update `project.godot` to launch `scenes/main_menu/main_menu.tscn` (or a dedicated 2D Cyber Command Center) instead of `3d_menu_experience.tscn`.
- **Action**:
  - Update `run/main_scene="res://scenes/main_menu/main_menu.tscn"` in `project.godot`.
  - Ensure background parallax, 2D holographic glow, and audio responsiveness are active in the 2D menu.
  - Cleanly decommission the `SubViewport3D` inside `character_selection.tscn` so it relies 100% on the 2D Operative cards and animated previews.

### Track 5: Safe Retirement / Decoupling of 3D Scripts
- **Goal**: Cleanly archive or deprecate `Player3D.cs`, `Enemy3D.cs`, `Level3DManager.cs`, `PlayerHUD3D.cs` so dead 3D code does not cause confusion or maintainability debt.

---

## 5. Critical Operational Rules (MANDATORY)

> [!IMPORTANT]
> ### 1. Strict Git Push Constraint
> **DO NOT execute `git push` under any circumstances unless the user explicitly commands you to do so!**  
> Staging (`git add`) and local commits (`git commit`) are permissible when instructed, but pushing to remote must wait for explicit user approval (`"push to git"`).

> [!IMPORTANT]
> ### 2. Mandatory Testing Protocol (End of Every Prompt)
> At the conclusion of **EVERY** turn or task, you **MUST** execute the test suite and verify 0 failures, 0 errors, and 0 orphans before concluding:
> ```powershell
> .\run_tests.ps1
> ```
> To run individual suites:
> - **NUnit (.NET 10 C#)**:
>   ```powershell
>   dotnet test tests/AxiomForce.Tests/AxiomForce.Tests.csproj
>   ```
> - **GdUnit4 (Godot 4 Console Executable)**:
>   ```powershell
>   & "C:\Users\karthiknk21\Desktop\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe" --headless --path . -s addons/gdUnit4/bin/GdUnitCmdTool.gd --add res://tests/ --ignoreHeadlessMode
>   ```

### 3. Godot Console Executable Requirement
- Always use `Godot_v4.7.2-stable_mono_win64_console.exe` on Windows to avoid stdout GUI deadlocks that can cause headless processes to hang.

### 4. Tree Safety & Node Lifecycle
- Always check `IsInsideTree()` before calling `GetTree()`, `CreateTween()`, or `.Play()` on audio players. Standalone unit tests instantiate scenes outside the active tree; unguarded calls trigger engine error 100/105.
- Always call `.free()` or `.queue_free()` on test-instantiated nodes in GdUnit4 to guarantee 0 orphan nodes.

---

## 6. Key File & Directory Map

| Path | Purpose |
|---|---|
| `scripts/actors/OperativeProfile.cs` | Model and central registry for all 6 operatives (stats, passives, synergy). |
| `scripts/actors/CyberShogun.cs` | Core 2D player controller (attacks, blade dash, wall-sliding, shield regen, operative scaling). |
| `scripts/actors/CyberBossMech2D.cs` | Multi-phase 2D boss mech encounter ("Vanguard-X") with Overdrive Rage phase shift. |
| `scripts/gameplay/CyberBossMissile2D.cs` | Boss missile projectile with collision, knockback, and particle trails. |
| `scripts/gameplay/CyberLaserGrid2D.cs` | Interactive 2D laser hazard with periodic cycling. |
| `scripts/gameplay/CyberHazardFloor2D.cs` | Interactive 2D electrified floor hazard with rhythmic damage surges. |
| `scripts/ui/CyberCombatHud.cs` | 2D in-game HUD with player shields, health, combo streak, and boss telemetry. |
| `scripts/ui/CyberVictoryScorecard.cs` | Post-mission tactical scorecard evaluating combat grades (S/A/B ranks) with fanfare. |
| `scripts/main_menu/CharacterSelection.cs` | Operative selection screen with 2D character portrait cards. |
| `scenes/gameplay/level1.tscn` ... `level4.tscn` | The complete 4-level 2D progression chain. |
| `tests/test_phase4_hazards_and_audio.gd` | GdUnit4 test suite for wall mechanics, SFX, and 2D hazards. |
| `tests/test_phase5_boss_and_victory.gd` | GdUnit4 test suite for boss mech, missile attacks, and victory scorecard. |
| `tests/test_phase6_operative_synergy.gd` | GdUnit4 test suite for operative registry, passives, and dynamic HUD styling. |
| `run_tests.ps1` | Unified test runner for NUnit (.NET 10), GdUnit4, and Django. |
# Deployment cinematic implementation

New Game routes through `deployment_cutscene.tscn` before the existing briefing.
The opt-in `cutscene` Compose service renders Blender artwork and packages a
40-second Theora film using Python/FFmpeg. Playback includes timed English subtitle
fallbacks, aspect preservation, skip controls, and an idempotent transition.
ElevenLabs narration is now generated via the direct API: the root `.env` key
was validated, and Daniel was selected from the live voice list. Four multilingual
v2 cues (6.03s, 6.11s, 7.11s, 5.62s) fit their subtitle windows without tempo
changes. `tools/cutscene/narrate.py` caches paid generations and uses Docker
FFmpeg for radio EQ and the 40-second `narration.ogg` mix. MCP tools are still
unavailable; the plugin text-to-speech workflow supplied the API approach.
Audio verification: Godot generated `narration.ogg.import`; ffprobe confirmed
40.000 seconds. Python compilation passed. NUnit 34/34, GdUnit4 104/104
(report_339), and container Django 104/104 passed. The test wrapper skips its
Windows Django environment, so Django ran in the backend container instead.
Import exited 0 with the existing plugin TLS/editor cleanup errors. A capped
45-second playback check reached StoryIntro and exited 0, with one resource
still in use at engine shutdown; runtime also reports the existing database
password fallback. No API key was printed or added to tracked configuration.
Four plates rendered in Windows Blender 5.2.3;
Docker packaging succeeded and ffprobe verified 40 seconds, 1920×1080, 24 fps,
Theora/Vorbis, 11,414,306 bytes. This is a four-plate animatic: animated rain,
running and surveillance sweeps remain unfinished.
Initial verification: C# build passed (153 existing warnings); NUnit 34/34,
GdUnit4 104/104 including two new tests, and container Django 104/104 passed.
The wrapper skips Django without a Windows venv, so it was run in Docker.
Import logs contain plugin TLS errors and exit resource leaks; do not describe
the import as warning-free. Final import exited 0 with the same plugin/cleanup
errors and no cutscene parse errors. Final NUnit 34/34, GdUnit4 104/104
(report_338), and Docker Django 104/104 passed after video packaging.
Runtime startup loaded without cutscene errors; host SessionManager reports a
PostgreSQL password failure and uses its existing local JSON fallback.
The 45-second, 60-fps-capped headless playback check reached StoryIntro and
successfully started its first existing AXIOM-7 voice line, with no ERROR lines.
Existing `.vscode/settings.json`
changes are unrelated and were preserved.

## Live 2D deployment cinematic (2026-10-30; supersedes the film playback above)

Replaced runtime video decoding with an editable layered SubViewport/Node2D scene,
Camera2D rig, sampled Shogun atlas, deterministic bounded rain/drone sweeps/transit,
weighted jump/landing/dash, afterimage, sparks and AXIOM-7 scan transmission.
The 40-second narration binary, dialogue, cue windows and New Game -> briefing
routing are preserved. Audio playback position plus mixer/output latency drives the
single clock, with monotonic elapsed fallback for missing/ended/stalled narration.
UI and skip remain independent of the camera; completion and all skip inputs share
one guarded deferred transition. Scene exit stops owned audio.

Asset generation: `docker compose --profile cutscene run --rm cutscene --reveal-only`
then `--live`. Reuses established original red/gold/ink Blender geometry (8 columns,
5 action rows, 512px cells) and planar city vocabulary; the container uses portable
CPU emission rendering because its Blender 4.3 calls Eevee by a different identifier.
Python validates dimensions, transparency and occupied atlas cells and writes the
manifest. The old OGV and build options remain a legacy preview only.

Godot AI tools were absent from the exposed tool list. `tools/cutscene/mcp_probe.py`
restores direct authenticated MCP access using protocol 2025-11-25, with no changes
to global MCP configuration. Verified session frog-jumper@52eac1f41bdee749 identifies
D:/Projects/frog_jumper, Godot 4.7.2. The user's open level1 scene stays untouched;
runtime checks launch the custom cutscene with autosave disabled.

Verification so far: Docker preflight found all four services running; representative
Shogun render inspected for identity/palette; C# build passed with 153 existing
warnings and zero errors. Final asset validation, import, runtime captures/layouts,
and unit suites are pending at this point; results will be recorded below.

Runtime review found and removed the global keyboard-help banner during the cinematic
using Global's existing visibility API, restoring it on exit. Renamed a shadowing
shader local after MCP diagnostics caught the warning. Eight in-engine captures
cover all shots plus 16:10/ultrawide in `.validation/deployment/review`; UI bounds
are inside every viewport. Initial GdUnit4 108/108 (report_340), NUnit 34/34 and
container Django 104/104 pass. The reference checker reports 10 existing connection
issues: eight in vendored proton_scatter and two in the unchanged main menu scene;
none is in this cutscene. Import exited 0 with pre-existing TLS/editor shutdown
resource errors. Host SessionManager still reports its existing PostgreSQL password
fallback; the container backend suite passes. Final checks must be repeated after
the banner/clock/input-test refinements.

The landing uses a grounded pose during compression, and the scan shader preserves
inherited alpha so the transmission portrait assembles with its overlay. Final
runtime/performance review follows after import and unit-suite reruns.

Live pointer testing exposed Global's project-wide mouse consumption: native GUI
mouse delivery never reached Skip. The cinematic now handles mouse/touch presses
inside its Skip rectangle during `_input`, before Global, without changing Global.
Direct screen touch does not depend on mouse emulation. A regression test covers
outside/inside/repeated touch; keyboard/controller/button signal tests remain.
An uninterrupted 40-second run reached story_intro with narration playing through
all shots. 1080p measured frame medians ~16.6-16.7ms and batch p95 ~17.4-17.8ms;
screenshot stalls are excluded. Audio and timeline differed by the intended ~20ms
output compensation. Final checks are repeated after the pointer fix.

Final verification (2026-10-30): 109/109 GdUnit4 tests pass, with 0 failures,
0 errors and 0 orphans (report_342); NUnit 34/34; Docker backend Django 104/104.
The wrapper skips its absent Windows backend venv, so Django ran explicitly in
the existing backend container. Incremental C# build: 0 errors, 0 warnings (the
initial full compile had 153 existing warnings). All eight live PNGs have imports;
container asset validation passes, and all five Python tool modules compile.
MCP-injected Escape, Start, pointer click and direct screen-touch each reached the
briefing at early/mid/late times after the pointer fix. Repeated transitions and
missing narration/standalone cleanup are covered by unit tests. Runtime review
artifacts: `.validation/deployment/review/`, `full-playback.json`, `input-checks.json`.
The narration SHA256 matches HEAD exactly. The user's open level1 editor scene
was preserved; only our test game was stopped and all four Docker services remain up.

Known limitations/diagnostics: project-wide reference checker still reports the
10 unchanged signal-path findings listed above; headless editor import exits 0 but
retains the existing TLS/plugin and editor shutdown resource diagnostics. These
are separate from the zero-orphan unit result. Host SessionManager still uses its
existing local JSON fallback after database authentication failure. No physical
mobile-device or export verification was performed. The narration is the only
runtime audio layer; the legacy film retains its synthesized atmosphere. The legacy renderer version mismatch noted earlier has since been fixed;
its four-plate path and the retained packaging option are available.

A final whitespace-only normalization follows the verified implementation; import,
build and all suites are rerun once more below before concluding.

The legacy plate renderer now uses portable CPU emission rendering as well, fixing
the previously documented Eevee identifier mismatch without changing the retained
OGV. Its four-plate rendering path is checked separately before the final suites.

Final rerun after normalization and legacy portability repair: import exited 0
(with the same pre-existing diagnostics); C# build passed with 0 errors/warnings;
GdUnit4 109/109, 0 failures/errors/orphans (report_343); NUnit 34/34; container
Django 104/104. All six cutscene Python modules compile. The legacy Blender path
rendered all four preview plates successfully in Docker; the original OGV and
narration binaries remain unchanged. No game export, commit or push was performed.

## Through the Breach — 2026-10-31

CodeLevel now builds Level 2's scorecard before extraction, captures score/combo/
operative once, records campaign completion through the existing PostgreSQL path,
freezes mission descendants/audio/HUD/pause, and retains Level 2 for Replay.
WildfireAftermath owns 75-second shots/subtitles/audio, safe missing-resource
fallback, input consumption and a guarded deferred results handoff. Continue targets
Level 3. Level 1's existing Tokyo cinematic and Level 3 introductions are preserved.
No watched flag, persistence/schema change or changes to unrelated PostgreSQL work.

WildfireAftermathStage analytically samples nine shots with parallax, smoke, water,
family movement, support/release, a live manifest and a walking departure following
the road slope. Docker `render_breach.py` creates original scenery and an 18-cell
operative atlas. `package_breach.py` assembles cached ElevenLabs Daniel/Adam/Sarah/
George cues and original atmosphere into 75-second tracks. `validate_breach.py`
checks raster alpha/bounds, timings and decoded peaks. `review_breach.py` runs real
objective callbacks, 27 captures, full playback and result navigation through MCP.
The existing gameplay verification tool now skips aftermath and presses Continue.

English BREACH_CUE_1..9 and BREACH_SKIP keys have supplied fallback text. Missing
speech preserves full timing; missing essential art completes through the same
results callback. Prompt restoration tests supply their own canvas because headless
Global omits it, and compare DisplayServer's actual supported cursor state.


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

## Silent Freight implementation — 2026-10-31

Added NeonRustIntro/NeonRustStage and start the overlay at the end of
NeonRustMission.OnLevelReady. CodeLevel now accepts an arrival CanvasLayer
with Finished; its original parameterless Level 2 entry still constructs
ConcreteWildfireIntro. The snapshot captures every mission descendant before
disabling any modes; Finished or overlay removal restores it once and restarts
the original opening dialogue. New input uses the aftermath's deferred handoff
and mapped-action release. The 75-second timeline supports deterministic Seek,
SeekAudio, compensated speech clock and timed-subtitle fallback.

Docker Blender authors depot/rail/cargo/walkway layers and a 512-cell missing-pose
atlas; existing city and operative walks are reused. Exact eight-line script,
cast, timing, localization keys and provenance live in assets/cutscenes/neon_rust_intro.
New lifecycle/integration tests accompany existing mission-fixture adjustments.
Final suites after shot refinements: NUnit 47/47, Godot 151/151
(report_363, zero failures/errors/orphans), container Django 104/104.
Unrelated PostgreSQL, website and aftermath changes are preserved. No commits/pushes.

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

## Git publication — 2026-10-31

The user authorized committing and pushing the current accumulated changes:
Silent Freight, Through the Breach, PostgreSQL-only persistence, website runtime
credentials/API wiring, tests, tools and documentation. The pre-publication fetch
confirmed main and origin/main match; diff hygiene passes. This is a Git/docs-only
task; the existing 151 Godot, 47 NUnit and 104 container Django passing results
remain applicable. Check MEMORY.MD for the verified publication result.

Publication is blocked by Windows write permissions on .git/objects. Staging
failed before any commit/push; access controls were left unchanged. Restore
Git write access before retrying. All implementation files remain local.

Publication retry: Git write access is now available; staging succeeded without
changing access controls. The user renewed authorization to commit and push
the reviewed accumulated changes. Final remote verification is recorded in MEMORY.MD.

## Neon Rust gameplay expansion — 2026-10-31

Eight sectors now span 48,000px with 30 finite encounter groups, three defended
shipments, two uplinks, two couplers, two sequential reinforcements and guarded
relay/extraction. NeonRustProgress uses exact IDs, idempotent rewards and explicit
prerequisites. IFiniteCombatHost removes the encounter/gunner dependency on Level 2
while preserving its authored activation, timings, artillery and rewards. Ability
and recruit predicates default to the original behavior in other missions.

Aura retains her original lines before sonic access. Mirage retains his lines
after blackout; his lesson requires a real Phase Blink across the harmless beam.
Seven safe stationary shrines retain objectives, recruitment, lessons and rewards.
Recovery clears rounds, resets incomplete groups and restores exact actor modes.
Level 3 now freezes for The City Finds Its Voice (90 seconds), then shows results
with Continue to 4, Replay, Level Select and Main Menu. Replay rebuilds the
original roster and arrival; the aftermath adds no unlocks or persistence.

Collision blockout was traversed using Godot AI MCP before dressing. Required
rises are 100px; escape drops 110px; optional archives reconnect locally. Docker/
Blender rendered seven original 512px RGBA depot assets with explicit import scales
and collision separation. Existing arrival, operative stats, audio and save schema
remain. All four normal containers were running before edits. No Git publication.

Initial build passed with 154 existing warnings. Ten focused Godot tests passed
with zero errors/failures/orphans. MCP completed all sectors using real movement
and abilities plus diagnostic kills, zero recoveries, about 145s simulation. This
is not a normal combat pacing result: the 30–35 minute human first-run target
remains UNVALIDATED. Final import, full suites and dressed captures follow below.
The optional full-project reference checker found ten existing signal-path errors
in unchanged Proton Scatter/main-menu scenes, plus 27 warnings; it is not a clean
pass. See tools/gameplay/NEON_RUST.md for reproduction and remaining acceptance.

Final verification (2026-10-31): background Godot refresh exited 0 and generated
all seven texture sidecars; asset validation passed dimensions, alpha, margins
and imports. Refresh logs retain TLS, EditorSettings and editor shutdown/resource
diagnostics; the import log is not error-free. Incremental C# build: 0 errors,
0 warnings (a full compile earlier reported 154 existing warnings). run_tests.ps1:
NUnit 93/93 and Godot 161/161, zero errors/failures/orphans (report_367). The wrapper
skipped Django because the host venv is absent; explicit container Django tests
passed 104/104 after the final code/test edits.

Godot AI MCP completed the dressed route with real movement/swaps/abilities and
diagnostic kills, no recoveries, observed peak four simultaneous enemies. Per-sector
movement/objective wall times were 18.859, 23.470, 18.882, 10.794, 19.431, 27.374,
14.745, 17.778 seconds (151.333 total, arrival excluded). Combat is diagnostically
resolved and unmeasured; these numbers do not validate human pacing. All 24 dressed
sector/aspect captures exist. Replay restored Level 3 arrival/three operatives;
Continue reached Level 4/five operatives; Level Select and Main Menu navigated
correctly. Final game logs contained no warnings/errors. User editor remains
stopped on level1.tscn. Four normal Docker services remain running. No staging,
commit or push. Human first-run pacing, physical hardware and listening remain
unvalidated; see tools/gameplay/NEON_RUST.md.

Publication preparation (2026-10-31): user authorized shutting down servers and
pushing the current Neon Rust implementation. All four normal Compose services
are now stopped with exit 0; persisted volumes and containers remain. main matched
origin/main after fetch. Existing verified implementation test results remain
applicable; only documentation/Git operations occurred during this task.

Publication completed: Neon Rust implementation commit 3c92724 pushed to origin/main
and remote SHA verified. Services remain stopped. This follow-up records publication
only; no code/assets changed and prior verification remains applicable.

## The City Finds Its Voice — 2026-11-01

Implemented Level 3 extraction → 90-second NeonRustAftermath → existing results →
mission 4. CodeLevel retains one campaign-completion call and immutable extraction
score/combo/operative values. Complete mission/pause/HUD/audio freeze, compensated
clock with monotonic fallback, all-device Skip, mapped action release, deferred
single handoff and teardown restoration follow the established cinematic pattern.
Missing speech retains the full subtitle timeline; missing essential art opens
results safely. Replay rebuilds Level 3 arrival/three operatives; Continue enters
Level 4/five operatives. No new objectives, unlocks, watched state or schema changes.

Eleven deterministic shots use reused depot art, original Docker/Blender fortress
scenery and a 30-cell five-operative pose atlas. Representative squad inspected
before full production. Speaker framing, sloped-road foot placement, fading red
links, recovered freight, anonymous radio responses and independently powered
fortress defenses are cinematic only. Surya/Voss and fortress arrival stay in Level 4.
Ten exact requested English cues use Daniel/AXIOM-7, Adam/Kaito, Sarah/Oceanus,
George/Titan, Matilda/Aura and Eric/Mirage. Manifests record voice IDs, exact text,
request/source hashes, generation settings and measured timings. Original seeded
atmosphere and dialogue are separate 90-second Ogg tracks. All cues fit at tempo 1.
Reproduction: tools/cutscene/README.md; caches/evidence: .validation/city-voice/.

Final verification: background import exit 0 and all five PNG/Ogg sidecars exist.
C# build zero errors (154 existing addon warnings). run_tests.ps1 passes NUnit
93/93 and Godot 170/170, zero failures/errors/orphans (report_372). Host Django is
skipped for absent venv; explicit isolated container Django passes 104/104 after
final edits. Raster dimensions/alpha/all atlas boundaries, ten cue windows, decoded
peaks (dialogue 26651, atmosphere 4841), exact 90-second durations, manifest/runtime
alignment, Python compilation and diff hygiene pass. Godot AI MCP captured and
visually reviewed all eleven shots at 1920×1080, 1920×1200 and 2560×1080. Full
playback kept position/health/score fixed, all mission descendants frozen and
maximum sampled audio/visual separation 36.8ms. Natural results plus all four
navigation actions pass. All 90 captured runtime log entries have no warning/error.
Earlier cinematics/results regressions pass in the full suite.

Existing import TLS/EditorSettings/shutdown ObjectDB/resource/PagedAllocator
diagnostics remain; do not describe the import log as error-free. No physical
controller/touch hardware or speaker-listening review was performed. Normal four
containers remain running and the user's editor stays stopped on Level 1. Docker
Desktop cannot access D: bind mounts (including existing backend /app); this fault
is unresolved. Production used an owned unmounted copy-based container, now stopped.
Django used copied current backend source and the existing runtime environment in
an owned unmounted test container, also stopped, on the existing Compose network.
No user service was stopped/restarted. Unrelated export changes are preserved;
no export, staging, commit or push. Changes remain local/uncommitted.


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
