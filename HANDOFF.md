# Axiom Force - Strategic Inter-Agent Handoff Briefing

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
