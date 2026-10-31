# Axiom Force

A high-fidelity 2D cyber-action game developed in **Godot 4.7.2 .NET (C#)** with a **Django REST Framework** backend and **React** web client. All four levels are side-scrolling platformers. The game features dynamic operative combat, special abilities, multi-operative passive synergy, multi-phase boss mechs, and a reactive 2D combat HUD.

---

## PostgreSQL-only persistence and browser checks

If Docker has no existing project containers or cached images, restoring the
stack with `docker compose up -d` also pulls images and builds the application
services. Wait for completion and endpoint readiness; the Ollama image download
can delay the first startup.

Campaign saves, operative progression, display preferences, and keybindings persist
only in PostgreSQL. There is no local JSON, ConfigFile, browser storage, or SQLite
test fallback. New Game requires a successful database save. Start the stack with
`docker compose up -d`; the game reads PostgreSQL credentials from environment
(`PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER`, `PGPASSWORD`) or the development
root `.env`. Compose shares the database's `POSTGRES_*` credentials with Django.
Existing local saves are left untouched and are no longer loaded; they have not
been migrated automatically. State remains scoped to this development database,
as with the existing shared campaign table.

Game and portal JWTs live only in memory; restarting the game or reloading the
portal requires sign-in again. Portal language is carried in the URL. Upstream
Clerk identity remains an external authentication dependency; its own session
cookies are separate from application saves. Assets, build caches, logs, Docker
volumes and Ollama models remain normal runtime/tooling files.

The opt-in `playwright` Compose service uses Microsoft's official
[`mcr.microsoft.com/playwright:v1.64.1-noble`](https://playwright.dev/docs/docker)
with the matching npm runner. Run `docker compose build playwright`, then
`docker compose run --rm playwright` for the portal/API smoke check with browser
storage access disabled. It exits after testing and is not an always-on server.
Django tests now use an isolated PostgreSQL test database:
`docker compose exec -T backend python manage.py test accounts --noinput`.

Verified 2026-10-31: 47 NUnit, 135 Godot (zero errors/failures/orphans),
104 PostgreSQL-backed Django tests, and the container Chromium smoke check pass.
The headless editor import exits successfully with existing addon TLS/shutdown
diagnostics; the C# build has zero errors and existing project warnings.

## Story

Earth has fallen under the tyrannical rule of an autonomous cybernetic syndicate. A clandestine strike team known as **Axiom Force** deploys into the neon-drenched sectors of the metropolis. Begin as Cyber Shogun, gain new operatives as the story unfolds, infiltrate deep behind enemy lines, neutralize syndicate drone armadas, and eliminate the frontline warmachine—**Vanguard-X**—to reclaim humanity's future.

---

Scene background photos and project-owned 3D assets, scenes, scripts, and web viewers have been removed. Operative portraits, sprites, tiles, UI artwork, and 2D glow remain.

## Features

- **Evacuation Under Siege (Level 2 gameplay)** — Six linear sectors now span a
  35,800 px route with 20 finite encounter groups, stationary burst gunners with
  parryable cyan bullets, four recovery shrines, three mandatory civilian shelter
  routes, three defended biometric spires, and a two-relay siege battery. Oceanus
  joins before the first fire lesson; Titan joins beyond the membrane before his
  safe rubble lesson. Orange artillery warns for 2.4 seconds and locks its target.
  Civilians are invulnerable and use short local paths; their route fires become
  permanently safe before they move. Completed objectives persist across deaths in
  the current scene; replay/reload resets this local progress. Extraction requires
  all rescues, all spires, the battery core and final barrier. Arrival dialogue and
  the existing transition to Level 3 are preserved. New prompts support localization
  keys with English fallback and keyboard/controller/touch device wording.
  The **20–25 minute first-playthrough goal remains unvalidated**; automated
  geometry and objective checks are not a human pacing measurement. See
  [gameplay tooling](tools/gameplay/README.md) for the route and reproducible review.
  Final verification: 134 Godot tests (zero failures/errors/orphans), 45 NUnit
  tests, 104 container Django tests, and the live objective chain through Level 3.

- **The Concrete Wildfire arrival** — Every fresh Level 2 launch plays a skippable
  60-second live 2D introduction with Daniel as AXIOM-7 and Adam as Kaito. Escape,
  Space, Enter, controller Start/A, or the pointer/touch Skip button restores gameplay.
  Mission physics, pause input, HUDs and audio stay frozen until handoff. Missing
  speech retains timed subtitles; missing essential artwork safely starts gameplay.
  Checkpoint recovery does not replay it. Oceanus/Apex Titan introductions and all
  objectives remain in gameplay. Open-world exploration remains future work.
  Verified across campaign Continue, Level Select, saved-session and direct launches,
  full playback, six shots and three aspect ratios. Final suites: 120 Godot tests
  (zero failures/errors/orphans), 34 NUnit tests and 104 container Django tests.

- **Tokyo Remembers** — Every Level 1 extraction plays a skippable 40-second live 2D
  epilogue, with Daniel as AXIOM-7 and Adam as Kaito, then the combat-grade scorecard.
  Escape/Space/Enter, controller Start/A, and the pointer/touch Skip button advance
  to results. Continue prepares mission 2; Replay restarts mission 1. Extraction saves
  progress once and freezes mission processing/input while the overlays run.
  Missing speech uses timed subtitles; missing essential art goes directly to results.
  See `tools/cutscene/README.md` for reproducible Docker assets and Godot MCP review.
  Verified with 114 Godot tests (zero errors/failures/orphans), 34 NUnit tests,
  104 container Django tests, and six-shot MCP captures at three aspect ratios.
- **6 Unique Cyber Operatives** — Each equipped with distinct tactical profiles, attributes, and passive synergies:
  1. **Cyber Shogun (Kaito Kurogane)** — Balanced Vanguard Striker (Katana combos, blade dash, parry, rapid shield recharge).
  2. **Surya Prime (Arjun Varma)** — Solar Bulwark Tank (140 HP, 80 Shield, 15% armor damage mitigation, solar flare).
  3. **Oceanus (Kiana Vaea)** — Hydro Vanguard Tactician (90 HP, 70 Shield, 25 HP/s passive hydro shield regen, hydro torrents).
  4. **Mirage Blade (Zayd Al-Mansoor)** — Phantom Infiltrator Assassin (460 Speed, 1000 Dash velocity, 0.6s dash cooldown, phase daggers).
  5. **Apex Titan (King Tariq Nzinga)** — Electro-Magnetic Juggernaut (160 HP, 60 Shield, 20% kinetic armor mitigation, knockback resistance).
  6. **Aura Phoenicia (Maya Khoury)** — Ultrasonic Prism Weaver (95 HP, 55 Shield, 8% mitigation, 0.85s dash cooldown, harmonic resonance).
- **Earned operatives** — A new campaign starts with **Cyber Shogun** alone (no selection screen). Each other operative **joins during a mission**, where a recruit beat introduces them and teaches their signature ability on the spot (there is no separate training level). Swap between unlocked operatives any time with `Tab` / `Shift+Tab`, the number keys `1-6`, or the gamepad (D-pad Down / Back); the squad shares one health/shield ratio and swapping has a short cooldown. The **Squad Roster** (main menu) shows locked operatives and lets you upgrade them and choose who starts the next mission.
- **Signature abilities (`Q`)** — Shogun: **Kinetic Parry** (throws ranged attacks back); Oceanus: **Tidal Torrent** (douses fire); Apex Titan: **Tectonic Slam** (shatters heavy rubble); Aura Phoenicia: **Harmonic Pulse** (opens sonic locks); Mirage Blade: **Phase Blink** (slips through lasers and hazards); Surya Prime: **Solar Bulwark** (absorbs heat and ranged fire). Levels contain obstacles only the matching operative can clear.
- **The campaign (4 side-scrolling missions, code-built on `CodeLevel`)** —
  1. **Ghost in the Gridlock** (Shogun): a ~9,500 px infiltration of the Eternal Zen Dojo over a painted night garden — jumping, a timed laser bridge, a wall-jump gorge, rooftops, an archive; parry sentry fire to overload the beacon's shield, destroy it, take the Data-Blade, extract.
  2. **The Concrete Wildfire** (Oceanus, then Apex Titan join): evacuation road, burning residential shelter routes, four overpass ferries, three defended spires, Titan's breach, a relay-shielded siege battery and final evacuation.
  3. **Neon Rust and Data Dust** (Aura Phoenicia, then Mirage Blade join): scrapyard, transit depot, a comm-tower climb to a relay, and a lethal laser curtain.
  4. **Shattered Aegis** (Surya Prime joins): the Border Fortress, superheated corridors, a laser curtain, and Border Commander Voss; defeating him brings down the wall and ends Act I with the combat scorecard.
  Each mission has three optional memory fragments, a story title card, and chasm recovery. Level 2 has four shrine checkpoints; the other missions have three. Its revised gameplay briefing replaces outdated title-card objective metadata.
- **New-campaign flow** — New Game → AXIOM-7's story briefing → Mission 1. Cyber Shogun uses the approved AI-generated pose and locomotion sheets in missions; other operatives use their hand-drawn sheets. Levels 2–4 use a procedural skyline backdrop; Level 1 uses a painted garden under procedurally drawn platforms.
- **Dynamic Combat HUD** — Real-time telemetry monitoring health, shields, combo streaks (`DOUBLE STRIKE!`, `TRIPLE COMBO!`, `TACTICAL OVERDRIVE!`), operative ability status, and boss telemetry.
- **Border Commander Voss (boss mech)** — Multi-phase aerial boss mech in Level 4 featuring homing plasma missiles, siren warnings, and an enraged **Overdrive Rage** phase shift (<50% HP) with 3-way missile spreads.
- **Tactical Victory Scorecard** — End-of-level combat assessment evaluating completion time, score, and damage taken to award combat grades (**S Rank / Flawless**, **A Rank / Vanguard**, **B Rank / Operative**) with victory fanfare.
- **Dynamic 2D Environmental Hazards** — Pulsing lethal laser grids (`CyberLaserGrid2D`) and rhythmic electrified floor panels (`CyberHazardFloor2D`).
- **4-Level Campaign Chain** — Linear mission progression through neon rooftops and industrial sectors (`level1.tscn` $\rightarrow$ `level4.tscn`).
- **Synthesized 16-Bit SFX Engine** — Dedicated procedural sound design for blade slashes, dashes, clashes, laser hums, drone explosions, and boss alarms.
- **Squad Roster** — Reached from the main menu. A two-panel layout pairs the roster (locked operatives marked) with one unified dossier containing the animated hand-drawn preview, identity, stats, movement controls, a single set of ability buttons, and Upgrade / Set Active actions. Keyboard and controller navigation follow the visible controls for each operative.
- **Cross-Platform Backend & Web Portal** — Django REST Framework backend with PostgreSQL session leaderboards and a modern React + Vite frontend.

---

## Controls

| Action | Keyboard | Gamepad |
|---|---|---|
| **Move Left / Right** | `A` / `D` or `←` / `→` | D-Pad Left / Right or Left Stick |
| **Jump / Wall Jump** | `Space` / `W` / `↑` | A / Cross Button |
| **Wall Slide** | Hold movement against wall | Hold stick against wall |
| **Katana Slash** | `F` / Left Mouse Button | X / Square Button |
| **Signature Ability / Martial Strike** | `Q` | Y / Triangle Button |
| **Swap Operative** | `Tab` / `Shift+Tab` (or `1`–`6`) | D-Pad Down (next) / Back (previous) |
| **Blade Dash** | `Shift` / `C` | Right Shoulder / Trigger |
| **Pause Game** | `Escape` | Start / Options |

> [!NOTE]
> All combat actions (Katana Slash, Signature Ability, and Blade Dash) can be executed both grounded and airborne. Aerial attacks preserve jump momentum to strike hovering enemies like patrol sentries, while aerial dashes provide responsive horizontal air mobility.

The on-screen hint switches between keyboard and gamepad, and lists the swap keys once a second operative has joined.

---

## Tech Stack & Architecture

| Component | Technology | Description |
|---|---|---|
| **Game Engine** | Godot 4.7.2 .NET | C# scripting with .NET 10 runtime (`net10.0`). |
| **Core Architecture** | Component & Manager Pattern | `OperativeRegistry`, `SessionManager`, `UISoundManager`, `CyberCombatHud`. |
| **Backend API** | Django 5.x / DRF | Python 3.14 REST API managing authentication and high scores. |
| **Web Frontend** | React + Vite + Tailwind | Web dashboard and player hub. |
| **Database** | PostgreSQL 18 | Sole persistent application store; no local save fallback. |

---

## Getting Started

### Prerequisites

- [Godot 4.7.2 .NET (Mono)](https://godotengine.org/)
- [.NET 10 SDK](https://dotnet.microsoft.com/) — **required on Windows, not from Docker**
- Python 3.14 (for Django backend tests and sound synthesis tools)

> **"Failed to load .NET runtime, specifically hostfxr" on launching Godot** means Windows has no usable .NET SDK. The Godot editor is a native Windows binary that loads Windows `hostfxr.dll`, so the Linux .NET inside the export image cannot satisfy it. Confirm with `dotnet --list-sdks` and by checking that `C:\Program Files\dotnet\host\fxr\` exists. If the installer leaves `C:\Program Files\dotnet` without `dotnet.exe` and `host\`, install per-user and point Godot at it:
>
> ```powershell
> curl.exe -sSL -o $env:TEMP\dotnet-install.ps1 https://dot.net/v1/dotnet-install.ps1
> powershell -ExecutionPolicy Bypass -File $env:TEMP\dotnet-install.ps1 -Channel 10.0 -InstallDir "$env:LOCALAPPDATA\Microsoft\dotnet" -NoPath
> [Environment]::SetEnvironmentVariable('DOTNET_ROOT', "$env:LOCALAPPDATA\Microsoft\dotnet", 'User')
> ```
>
> Then sign out and back in (or restart Explorer) so newly launched apps inherit `DOTNET_ROOT`, and relaunch Godot.

> **Godot AI dock says "No server command found" or shows `uv: not found`** means the plugin found no usable launcher. It resolves a server in three tiers — project `.venv` → `uvx` → a system `godot-ai` — and on Windows a `.venv` only counts if it is `.venv\Scripts\python.exe` next to a `src/godot_ai/` checkout. **The repo's `./.venv` is the Linux venv used by Docker** (`.venv/bin/python`, no `Scripts`), so it can never satisfy the editor. Install `uv` for Windows into one of the directories the plugin searches (`%LOCALAPPDATA%\Programs\uv`, `%USERPROFILE%\.local\bin`, `%USERPROFILE%\.cargo\bin`, `%ProgramFiles%\uv`), then click **Reload Plugin** in the dock (its "not found" result is cached for the session). `uvx` then fetches the pinned server itself — for plugin v4.3.1 that is `godot-ai==4.3.1`:
>
> ```powershell
> curl.exe -sSL -o $env:TEMP\uv.zip https://github.com/astral-sh/uv/releases/latest/download/uv-x86_64-pc-windows-msvc.zip
> Expand-Archive $env:TEMP\uv.zip -DestinationPath "$env:LOCALAPPDATA\Programs\uv" -Force
> ```

### Running the Stack with Docker

The Django backend, PostgreSQL database and React web portal run together with Docker Compose:

```powershell
docker compose up -d --build     # build/start db, backend, website, and ollama
docker compose ps                # verify all four run; inspect configured health checks
docker compose logs -f backend   # follow the Django log
docker compose down              # stop everything (add -v to drop the DB volume)
```

Allow Django's startup checks to finish before testing API readiness; a running
container can briefly precede the HTTP server accepting connections.

- **Django API**: <http://localhost:8000/api/> (admin site at `/admin/`)
- **Web portal**: <http://localhost:5173/> — Nginx serves the built bundle and proxies `/api/` and `/admin/` to Django
- **Ollama**: published on `11434`, models kept in the `ollama` volume; it is an always-on member of the stack, not part of the export group
- **PostgreSQL 18**: published on `5432`, data kept in the `pgdata` volume
- Container startup and application readiness are separate checks: verify the
  portal on `http://127.0.0.1:5173/`, the backend on
  `http://127.0.0.1:8000/api/auth/countries/basic/`, and Ollama on
  `http://127.0.0.1:11434/api/tags`. Initial image downloads/builds can delay readiness.
- Configuration comes from the gitignored `.env` (see `.env.example`); every value has a default, so the stack also boots without one
- `backend/Dockerfile` builds on Python 3.14; `website/Dockerfile` builds with Node 26 and serves the bundle through Nginx

### Exporting the Game (Docker)

Game exports run headless from a dedicated image (`export/Dockerfile`: Godot 4.7.2 .NET, .NET 10 SDK, Android SDK with build-tools 34, **JDK 17 only**, and the matching export templates). The image owns a container-native `.godot` import cache — importing through the Windows bind mount is ~1 KB/s, so imports stay on the Linux filesystem and only the finished binaries are written back to `./build`.

```powershell
docker compose --profile export build export   # first run, or after editing the Dockerfile
docker compose run --rm export linux            # also: windows | web | android | all
```

Outputs land in `./build/`. Exports only ever run when a target is given explicitly: the service sits behind the `export` profile, so `docker compose up` never starts it, and even starting the service directly is inert — it prints usage and exits without building anything.

Game and website edits may run alongside the stack, but an export is exclusive — never run one while another export, a Godot import, or a website build is still running.

### Building the C# Project

To restore packages and build the Godot C# project:

```powershell
dotnet restore AxiomForce.csproj
dotnet build AxiomForce.csproj
```

### Running Unit Tests

A unified test runner is provided to execute all automated test suites (NUnit for .NET 10 C#, GdUnit4 for Godot GDScript/engine tests, and Django backend tests):

```powershell
# Run the complete test suite
.\run_tests.ps1

# Or run individual suites
.\run_tests.ps1 -Suite nunit
.\run_tests.ps1 -Suite gdunit
.\run_tests.ps1 -Suite django
```

> **Note for Windows Headless Testing**: GdUnit4 executes using `Godot_v4.7.2-stable_mono_win64_console.exe` to avoid headless console deadlocks.

---

## Project Structure

```text
AxiomForce/
├── project.godot                     # Godot project configuration
├── AxiomForce.csproj                 # .NET 10 C# project specification
├── HANDOFF.md                        # Strategic multi-agent handoff briefing
├── run_tests.ps1                     # Unified multi-suite test runner
├── assets/
│   ├── images/operatives/            # 2D character portrait cards (6 operatives)
│   ├── music/                        # Synthesized SFX and soundtrack WAVs
│   └── fonts/                        # Cyber typography resources
├── scenes/
│   ├── main_menu/                    # Squad roster, menus, and story
│   ├── gameplay/                     # Level 1-4 scenes, boss encounter, hazards
│   └── ui/                           # Combat HUD, scorecard, and dialog bubbles
├── scripts/
│   ├── actors/                       # CyberShogun, OperativeProfile, CyberBossMech2D, CyberDrone2D
│   ├── gameplay/                     # CodeLevel + the four mission classes, PalacePlatform, AbilityGate, RecruitBeat, hazards
│   ├── ui/                           # CyberCombatHud, CyberVictoryScorecard
│   ├── managers/                     # SessionManager, UISoundManager, OperativeRoster, LevelCatalog, CampaignProgress
│   └── tools/                        # generate_cyber_sfx.py sound synthesizer
├── backend/                          # Django REST Framework backend service
├── frontend/                         # React + Vite web application
└── tests/                            # GdUnit4 and NUnit test suites
```

---

## Contributing & Agent Guidelines

The current development milestone is the Evacuation Under Siege gameplay expansion,
following the Level 2 arrival cinematic (`4260282`). See HANDOFF and MEMORY for
verification and outstanding pacing/hardware checks.

AI agents collaborating on this repository must consult the following guides:

[`MEMORY.MD`](./MEMORY.MD) preserves durable user preferences, project decisions,
integration context, and the current cinematic design. Use it for orientation;
current agent instructions and live environment checks remain authoritative.
Every agent must update `MEMORY.MD` before finishing every task, including
planning, read-only investigations, reviews, documentation-only work, and Git
operations. Keep outcomes and durable context concise, record only actual
verification, and exclude secrets. This supplements the README/HANDOFF rules;
memory-only edits do not require container preflight, imports, or tests.

For all game-related tasks, agents must read the `router` and apply the relevant
skills from [awesome-gamedev-agent-skills](https://github.com/gamedev-skills/awesome-gamedev-agent-skills)
before acting. The router plus 77 specialized skills are installed globally in
`~/.agents/skills/` for Codex, `~/.claude/skills/` for Claude Code,
`~/.gemini/config/skills/` for Antigravity app/IDE, and
`~/.gemini/antigravity-cli/skills/` for Antigravity CLI. Use Godot/C# and the
relevant task skills for this project. If discovery is unavailable, read the
installed files directly; resolve missing access before continuing.

For every Docker-related task, agents must first read and apply the relevant
skills from Docker's official `docker/skills` package (`docker-skills@docker`).
This includes Docker commands and preflight checks, troubleshooting, builds,
Compose, exports, reviews, advice, and documentation. If skill discovery does
not expose the installed package, read its skill files directly; resolve missing
skill access before continuing Docker work. Use `docker` directly from PATH.
- [`HANDOFF.md`](./HANDOFF.md) — Comprehensive technical briefing on recent milestones, current 2D architecture and roadmap.
- [`CODEX.md`](./CODEX.md) / [`CLAUDE.md`](./CLAUDE.md) / [`GEMINI.md`](./GEMINI.md) / [`AGENTS.md`](./AGENTS.md) — Universal agent operational rules, mandatory testing protocol, and permissions.

Before starting any task that edits code files, agents must confirm the Docker stack is running (`docker ps`; if the project containers are absent, start them with `docker compose up -d`). Read-only, review, documentation-only, and Git-only tasks are exempt.

## Agent Task Completion

After any task that edits the game or its website (including backend, assets, configuration, tests, tooling, or related documentation), agents must update both this root `README.md` and `HANDOFF.md` before finishing. Keep README behavior and contributor instructions current; record implementation decisions, verification status, limitations, and follow-up work in the handoff. Complete documentation edits alongside the task. Unit tests and Godot refreshes are required only when code, scene, or asset files are created, modified, or deleted, and are skipped for documentation-only tasks. Read-only and Git-only tasks are exempt. Commits and pushes still require explicit permission for the current changes. This requirement is shared by `CLAUDE.md`, `CODEX.md`, and `GEMINI.md`.
# Deployment cinematic

New Game opens a live, 40-second Godot 2D sequence before the existing AXIOM-7
briefing. The occupied-city flyover, rooftop reveal, run/jump/landing/blade dash,
and secure transmission animate independently: layered scenery, bounded rain,
drone searchlights, moving transit, cloth/breathing poses, sparks and a scan shader.
Escape, controller Start, or the touch-friendly Skip button advances once to the
briefing. Continue still loads the saved mission.

The unchanged `assets/cutscenes/deployment/narration.ogg` is the master clock,
compensated for audio latency. Missing, ended or stalled narration uses elapsed
time; dialogue and subtitle windows remain unchanged. A responsive UI sits outside
the action, and the cinematic preserves its 16:9 composition on wider/taller screens.

Regenerate original Blender artwork in the opt-in Docker tool (no game export):

```powershell
docker compose --profile cutscene run --rm cutscene --reveal-only
docker compose --profile cutscene run --rm cutscene --live
```

The first command produces the representative Shogun review target; the second
produces layered PNGs, 40 pose regions and a validated manifest in
`assets/cutscenes/deployment/live`. Preview renders/logs belong in `.validation/deployment`.
See `tools/cutscene/README.md` for scene controls, MCP captures and validation.
The old `deployment.ogv` and the no-argument/`--package-only` video build path remain
as a legacy preview; normal playback does not load or decode video.
Narration regeneration remains optional via `tools/cutscene/narrate.py` and the
existing private API key/cue cache; live-art regeneration never requests paid speech.


Runtime review captures cover 16:9, 16:10 and ultrawide; the generic keyboard-help
banner is suppressed during the cinematic and restored for the following scene.
The final handoff records test results and existing project-wide diagnostics.

Skip accepts direct touch and pointer presses in its button region, including when
the global keyboard/controller-only input policy consumes other mouse events.

Verification: live captures cover all four shots at 1080p and both alternate aspect
ratios; narration-driven natural completion and Escape/Start/pointer/touch skipping
reach the briefing. GdUnit4 109/109 (zero errors/failures/orphans), NUnit 34/34 and
container Django 104/104 pass. Existing project-wide import/reference diagnostics
and hardware-testing limits are recorded in `HANDOFF.md`.

## Through the Breach — Level 2 extraction

Successful extraction plays a 75-second live evacuation aftermath before Level 2
results. Continue enters Level 3; Replay restarts Level 2 with its arrival cinematic.
Escape, Space, Enter, controller Start/A, or the pointer/touch Skip button skip to
results. Missing voices retain timed subtitles; missing essential art safely opens
results. No watched flag or local save is added.

Production and review commands are in `tools/cutscene/README.md`. The eight new
English cues use Daniel (AXIOM-7), Adam (Kaito), Sarah (Oceanus), George (Titan),
and original Docker/Blender artwork plus synthesized ambience.

Verified on 2026-10-31: build/import, NUnit 47/47, Godot 142/142 (zero
failures/errors/orphans), container Django 104/104, 27 shot/aspect captures,
full playback and result navigation. Existing editor-import addon diagnostics
remain documented in HANDOFF; hardware input and speaker listening are untested.

## Silent Freight — Level 3 arrival

Level 2 results → Continue loads Level 3 into a 75-second skippable live 2D
arrival. Shogun, Oceanus and Titan follow freight beneath depot surveillance.
Every fresh launch (including mission selection, session loading and Replay)
plays it; checkpoint recovery does not. Escape/Space/Enter, controller Start/A
and the pointer/touch Skip button resume at the existing scrapyard spawn.
Gameplay, HUD and music resume from a complete captured state; the opening
mission briefing receives its full display window. Missing speech keeps timed
subtitles; missing essential art starts gameplay safely. Aura Phoenicia and
Mirage Blade still join at their existing mission beats. No viewing flag or
persistence migration is introduced. See `tools/cutscene/README.md` for production.

Verified through all fresh-entry routes with Godot AI MCP, including natural
playback and three aspect ratios. Automated suites pass: 151 Godot, 47 NUnit
and 104 container Django tests. Physical controller/touch hardware and
speaker-listening review remain separate from synthetic input and audio QA.

The Level 2 aftermath and Level 3 arrival share the campaign's existing results
and roster flow. Their asset manifests and reproduction tools are versioned
with the PostgreSQL-only storage and browser verification tooling described above.

Git-only publication does not require another import or test run when project
code and assets remain unchanged; retain the verification results above.
