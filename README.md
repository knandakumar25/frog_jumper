# Axiom Force

A high-fidelity 2D cyber-action game developed in **Godot 4.7.2 .NET (C#)** with a **Django REST Framework** backend and **React** web client. All five levels are side-scrolling platformers. The game features dynamic operative combat, special abilities, multi-operative passive synergy, multi-phase boss mechs, and a reactive 2D combat HUD.

---

## Mission 5 — Names Without Numbers (2026-11-02)

The campaign now has five playable missions. Level 4 explicitly retains its captured
**ACT I COMPLETE** scorecard. **CONTINUE TO ACT II** launches the existing opening;
natural completion, Skip and missing essential art hand off once to Mission 5 after
stopping cinematic audio. Level Select, Resume and Mission 5 Replay enter gameplay
directly. A Mission 4 save remains at Mission 4 until completion; no save migration or
new operative grant is introduced. Mission 5 ends on **MISSION COMPLETE**, with Replay,
Level Select and Main Menu, without exposing Mission 6.

Six connected sectors cover scanner timing, processing control, archive cooling and
maintenance, three authenticated original records, exclusion isolation, three ordered
finite recovery waves and restored civilian access. Seventeen three-member formations
use the existing combat/score/checkpoint systems with a six-live-enemy cap. Required
archive stairs rise 100px; records are indestructible context terminals using the normal
attack action, or click/tap the nearby console. Exact prerequisites reject premature/duplicate interactions. Completed
objectives and formations survive checkpoints, incomplete groups reset and each member
rewards once across retries. Three optional civilian testimony fragments never gate exit.

Surya remains unlocked and speaks over comms from the queue. His slot shows
“Protecting the queue”; roster selection, direct player swaps and cycling all exclude
him only in Mission 5. Invalid preferred selections fall back to Kaito. Mirage phases
into processing, Aura isolates scanner denial, Oceanus stabilizes coolant and Titan
opens the damaged maintenance route away from protected records. Committing restored
identities visibly opens amber denial gates with accepted-access markers.

Original Docker/Blender props, nineteen new cached established-cast cues and synthesized
atmosphere/scanner cues include reproducible production scripts and provenance. A single
mission-owned speech lane serializes critical dialogue above expiring hints; missing
speech retains localized English fallback subtitles. Owned audio pauses/stops on mission
lifecycle transitions. Docker's D: bind mount failed, so production used copied inputs
in the same existing media image; the running stack was preserved.

Assisted blockout traversal completed all records, sequential recovery, commit and exit
using real movement, operative abilities and diagnostic enemy clearance. It exposed an
upper gallery approach problem, fixed with solid conservative stairs before dressing.
This is functional evidence, not unassisted combat balance or the requested 25–30-minute
first-play duration. Physical controller/touch and subjective mix listening remain
separate acceptance checks. Production and final verification details are in
[Mission 5 acceptance](tools/gameplay/NAMES_WITHOUT_NUMBERS.md).

### Mission 5 epilogue — Names Without Numbers

After extraction, a 90-second skippable live 2D scene returns to the restored district.
A resident chooses and speaks her name; other residents reclaim their records and
begin naming themselves. Kaito, Mirage, Aura, Oceanus and Titan react briefly while
Surya stays on comms. Kaito leaves the final choice with the district. The overlay
freezes the extracted mission and keeps its score snapshot; natural completion, Skip,
missing scene art and independent overlay removal each stop its owned audio and reveal
the existing **MISSION COMPLETE** card once. Missing speech keeps the timed English
subtitles. Replay restarts Mission 5 with a fresh run; the epilogue adds no unlock or
progression write and does not introduce Mission 6. Art/audio production and review
steps are in [cutscene production notes](tools/cutscene/README.md#mission-5-epilogue-names-without-numbers).


Verified 2026-11-02 after final engine edits: background import exit 0 with all
30 new media sidecars; final incremental C# build zero warnings/errors; NUnit 100/100;
GdUnit4 210/210 (`reports/report_406`, zero errors, failures and orphans); isolated
copied-source Django 104/104. The test runner was subsequently updated to use only the
root Linux `.venv`; its full run passes NUnit 100/100, GdUnit4 214/214 and Django
104/104 (`reports/report_408`; zero GdUnit errors/failures/orphans). Django runs from
a disposable copy of the root venv with `backend/requirements.txt` installed, using an
unmounted helper container on the existing PostgreSQL network. The backend `/app` and
media D: mounts remain unreadable; normal services stay running.
The temporary production/test helpers were stopped; all four normal services remain
running. No exports, staging, commits or pushes; editor remains stopped on Level 1.

Runtime evidence under `.validation/names/`: assisted mandatory-route completion,
21 sector/restored/results captures across 1920×1080, 1920×1200 and 2560×1080,
actual keyboard/controller-event/mouse/touch terminal dispatch, repeated opening Skip,
natural completion and missing-art handoff, Mission 5 Replay and direct Level Select.
A temporary campaign stayed at Mission 4 until completion, then saved/resumed directly
into 5; its cleanup preserved the original campaign row hash. Seven transparent art
assets and nineteen complete voice cues pass alpha/margin, cast/hash, decoded duration
and clipping checks. The gallery spawn regression passes for all six archive groups.
Python tool compilation and diff hygiene pass. These checks do not establish an
unassisted uninterrupted 25–30-minute run, physical hardware or subjective listening.

Known diagnostics: a full rebuild retains 153 pre-existing nullable/addon warnings;
one concurrent build had a transient PDB-sharing retry, resolved by the final clean
incremental build. Headless editor imports still log existing addon TLS and shutdown
EditorSettings/ObjectDB/resource/allocator messages. The reference checker retains
10 pre-existing addon/menu connection errors and 27 warnings, none in Mission 5.

## Act I opening label

Level 1 shows a centered cyan **ACT I** above its mission title as gameplay begins.
It uses `CAMPAIGN_ACT_I_TITLE` with English fallback, shares the existing 3.2-second
hold and 1.2-second fade, and adds no pause or input. Fresh launches (briefing
completion/Skip, level selection and Replay) show it; checkpoint recovery does
not replay it. Levels 2–4 retain their existing title cards.

Verified 2026-11-02: all launch paths and recovery checked through runtime
callbacks; layouts inspected at 1920×1080, 1920×1200 and 2560×1080. Build/import
complete; NUnit 88/88, Godot 197/197 (zero failures/errors/orphans) and isolated
container Django 104/104 pass. Existing compile/import diagnostics remain; Django
used a temporary source copy because the running container's host bind mount is
unreadable. The wrapper skips Django when the host virtual environment is absent.

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

Creative direction is agent-led under the user's current requests. The former
story bible and full development plan were removed at the user's request;
historical plans and production notes describe prior work rather than prescribe
future story, dialogue or design. Existing gameplay and cinematics remain playable.

---

Scene background photos and project-owned 3D assets, scenes, scripts, and web viewers have been removed. Operative portraits, sprites, tiles, UI artwork, and 2D glow remain.

## Features

- **Levels 1–3 objective guidance** — Lantern Court keeps the current parry control
  visible until a successful deflection; archive signs, beacon shield charges and
  sealed Data-Blade access are explicit. Civilian markers show local defenders,
  route obstacles, moving evacuees and actual shelter arrival. Neon Rust retains
  all 30 formations/rewards but requires nine objective defenses; 21 patrols are
  optional score opportunities. Checkpoints follow objective milestones. Levels
  2–3 retire unfinished groups beyond 1,500 horizontal pixels without completion
  or repeat rewards. Recovery preserves progress; Replay starts fresh. See the
  [mission review and human checklist](docs/ACT_I_REVIEW.md).

  Verified 2026-11-02: C# build, 88 NUnit, 197 Godot (zero errors/failures/orphans)
  and 104 isolated-container Django tests pass. MCP reviewed three viewport sizes
  and all results destinations; assisted traversal confirms optional-patrol bypass.
  Human pacing and physical controller/touch acceptance remain pending. Existing
  import shutdown/TLS diagnostics and Docker bind-mount fault are recorded in
  [HANDOFF.md](HANDOFF.md).
- **Shattered Aegis / Act I finale** — A 28,000px seven-section fortress assault,
  fourteen finite formations (four live ordinary enemies maximum), earned Surya
  recruitment, real Bulwark/Blink lessons, permanent coolant/gun/curtain shutdown,
  and two locally reconnecting gun-deck approaches. Ordinary combat is bypassable
  outside designated objective defenders. Seven checkpoints retain local progress.
  Voss keeps his armor/missiles in a three-phase encounter with 300 total HP, destructible
  phase-two pylons, reflected missiles and five-second melee openings. Defeat
  unlocks a separate crossing release; civilians cross a safe nonblocking road
  before extraction freezes ACT I COMPLETE results (Replay, Level Select, Main Menu).
  The Act I mission behavior, saves and earlier recorded cinematics are preserved.
  Fourteen cached event voices and six original Docker/Blender fortress assets
  have reproducible provenance. The 20–25 minute first-run target is **unvalidated**.
  See [production/acceptance](tools/gameplay/SHATTERED_AEGIS.md),
  and [Levels 1–3 review](docs/ACT_I_REVIEW.md).
  Verified 2026-11-01: build, 97 NUnit, 190 Godot (zero failures/errors/orphans),
  104 copied-container Django tests, all three display ratios, launch/results
  routes and automated traversal using actual combat. Physical input/listening
  and human pacing remain outstanding; existing import/bind-mount issues persist.
  For the next session, use the latest [handoff](HANDOFF.md) and acceptance guide.

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
  3. **Neon Rust and Data Dust** (Aura Phoenicia, then Mirage Blade join): eight sectors of freight sabotage, three shipments, two uplinks, a relay blackout and a blind-grid escape across 48,000px.
  4. **Shattered Aegis** (Surya Prime joins): the Border Fortress, superheated corridors, a laser curtain, and Border Commander Voss; defeating him unlocks the crossing control, then opening the border and reaching extraction ends Act I with its scorecard.
  Each mission has three optional memory fragments, a story title card, and chasm recovery. Level 2 has four shrine checkpoints; the other missions have three. Its revised gameplay briefing replaces outdated title-card objective metadata.
- **New-campaign flow** — New Game → AXIOM-7's story briefing → Mission 1. Cyber Shogun uses the approved AI-generated pose and locomotion sheets in missions; other operatives use their hand-drawn sheets. Levels 2–4 use a procedural skyline backdrop; Level 1 uses a painted garden under procedurally drawn platforms.
- **Dynamic Combat HUD** — Real-time telemetry monitoring health, shields, combo streaks (`DOUBLE STRIKE!`, `TRIPLE COMBO!`, `TACTICAL OVERDRIVE!`), operative ability status, and boss telemetry.
- **Border Commander Voss (boss mech)** — Level 4 uses three phases at 300/200/100 HP, phase-two power pylons, aimed/spread missiles, marked defense lanes and stationary melee recovery. Parry returns the actual missile to its owner. Standalone mech scenes retain the legacy overdrive behavior.
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

Game export preparation is requested locally through Docker with an explicit target.
Apple signing/distribution and iOS native project generation are the documented
manual Mac/Xcode exception; see [Apple release steps](export/APPLE_RELEASE.md).
GitHub Actions must not export the game, including from manual or container jobs.
The repository and GitHub workflow list were checked on 2026-11-01: neither has
any Actions workflows to remove. Existing historical runs/artifacts were not deleted.

Game exports run headless from a dedicated image (`export/Dockerfile`: Godot 4.7.2 .NET, .NET 10 SDK, Android SDK with build-tools 34, **JDK 17 only**, and the matching export templates). The image owns a container-native `.godot` import cache — importing through the Windows bind mount is ~1 KB/s, so imports stay on the Linux filesystem and only the finished binaries are written back to `./build`.

```powershell
docker compose --profile export build export   # first run, or after editing the Dockerfile
docker compose run --rm export linux            # also: windows | web | android | macos | ios | all
```

Outputs land in `./build/`. Exports only ever run when a target is given explicitly: the service sits behind the `export` profile, so `docker compose up` never starts it, and even starting the service directly is inert — it prints usage and exits without building anything.

The root launchers `export.bat linux` (Windows) and `./export.sh linux` (Bash)
build and invoke that same `export` service from the repository directory.
Supported targets are `linux`, `windows`, `web`, `android`, `macos`, `ios`, and `all`. A bare
invocation or `--help` only prints usage; invalid targets fail before any build.
Both launchers refuse `GITHUB_ACTIONS=true`, and Compose forwards that marker
to the container driver, which also refuses Actions and host execution.
The driver is baked into the image: rebuild it before your next authorized
export after changing `export/export.sh`. No exporter image was built or game
export performed during this configuration-only change.

Configuration verification (2026-11-01): launcher safety paths, Bash syntax,
Compose isolation, 93 NUnit tests, 161 Godot tests (zero errors/failures/orphans),
and 104 container Django tests pass. No packaged game build was produced.

Apple setup (2026-11-01): `export.bat macos` prepares an unsigned universal ZIP;
`export.bat ios` prepares a credential-filtered source ZIP for a Mac with Xcode.
The latter is not an Xcode project or IPA. Set a real macOS bundle ID before
requesting `macos` or `all`; the checked-in ID is an intentional example.
The iOS preset has a blank Team ID and project-only export for later completion.
The native Mac helper `bash export/finish_ios.sh --check` checks prerequisites;
its explicit `project` command generates the Xcode project on the Mac.
Apple signing, provisioning, icons/privacy review, physical device validation,
and distribution remain manual. Setup alone does not certify an Apple release.

Setup verification: four synthetic helper tests plus 93 NUnit, 161 Godot
(zero failures/errors/orphans) and 104 container Django tests pass. Syntax,
configuration and safety checks pass; background import exits 0 with existing
addon/shutdown diagnostics. No real Apple export or exporter image build ran.
Mac/device/signing validation is still required.

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

The Django suite uses the root Linux `.venv` only. On Windows PowerShell, the runner
packages that POSIX environment and `backend/` into a temporary Linux container; it
installs `backend/requirements.txt` in the temporary copy and runs `manage.py test
accounts` against PostgreSQL. It does not prefer or require a Windows venv.

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
Mirage Blade join in Harmonic Access and after blackout respectively. No viewing flag or
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

## Neon Rust sabotage expansion — 2026-10-31

Level 3 now spans eight sectors and 48,000px with 30 finite formations (three or
four normal-health members, at most six alive), three defended shipments, two
uplinks, two power couplers, finite relay reinforcements and the blind-grid escape.
Aura joins before the sonic-lock lesson; Mirage joins after blackout and must
actually blink across a harmless practice beam. Seven stationary recovery shrines
retain completed objectives, recruits, lessons and kill-reward slots in this scene.
Replay/reload starts a fresh three-operative run with the existing arrival.

Extraction now plays **The City Finds Its Voice**, a 90-second skippable live 2D
aftermath voiced by the five earned operatives and AXIOM-7, before the existing
results. Continue enters Level 4 with five operatives; Replay restores Level 3’s
arrival and three-operative starting roster. Level Select and Main Menu retain
their destinations. The mission stays frozen and results retain extraction-time
score, peak combo and operative. Missing speech preserves subtitles; missing
essential art safely opens results. Keyboard, controller, pointer and touch Skip
share one deferred handoff. Gameplay dialogue, objectives and save schema remain
unchanged. Original Docker/Blender poses and fortress scenery, ten cached
ElevenLabs cues and separate 90-second dialogue/atmosphere tracks are documented
in [cinematic reproduction](tools/cutscene/README.md#the-city-finds-its-voice).

The **30–35 minute first-playthrough goal remains UNVALIDATED**. Assisted blockout
traversal used real movement/abilities with diagnostic enemy kills, zero recoveries,
and about 145 seconds of simulation. It cannot measure normal combat or human
pacing. See [Neon Rust reproduction and acceptance](tools/gameplay/NEON_RUST.md).

Verified on 2026-10-31: NUnit 93/93, Godot 161/161 (zero errors/failures/orphans),
container Django 104/104, seven-asset validation, complete assisted route, all
four results actions and 24 aspect/sector captures. User editor preserved on
Level 1. Background refresh exited 0 with seven sidecars but retains editor/TLS/
shutdown diagnostics; final game logs were clear. Human pacing remains unvalidated.

Server state update (2026-10-31): the user requested shutdown before publication;
all four normal Compose services are stopped. Restart with docker compose up -d
before code editing or running services. Existing validation results remain valid.

Publication verified: Neon Rust implementation commit 3c92724 is on origin/main.
The services remain stopped at the user's request; prior test results still apply.

The City Finds Its Voice verification (2026-11-01): C# build passes; NUnit 93/93,
Godot 170/170 with zero errors/failures/orphans, isolated container Django 104/104.
All 33 shot/aspect views, full 90-second frozen playback, four results destinations,
raster/atlas QA and both complete 90-second audio tracks pass. Background import
exits 0 with five sidecars; existing addon/TLS/shutdown diagnostics remain. Physical
controller/touch and speaker-listening review remain unperformed. Docker Desktop’s
D: bind-mount fault remains unresolved (including backend /app); copied inputs in
owned unmounted containers enabled production and Django verification without
restarting the four running services. User editor stays stopped on Level 1.


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

Existing import diagnostics and Docker bind-mount fault remain. Physical input
hardware and speaker-listening checks are outstanding. Changes are local and
uncommitted; production/reproduction details are in tools/cutscene/README.md.
