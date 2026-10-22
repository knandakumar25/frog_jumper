# Axiom Force

A high-fidelity 2D cyber-action game developed in **Godot 4.7.2 .NET (C#)** with a **Django REST Framework** backend and **React** web client. All four levels are side-scrolling platformers. The game features dynamic operative combat, special abilities, multi-operative passive synergy, multi-phase boss mechs, and a reactive 2D combat HUD.

---

## Story

Earth has fallen under the tyrannical rule of an autonomous cybernetic syndicate. A clandestine strike team known as **Axiom Force** deploys into the neon-drenched sectors of the metropolis. Choose your operative, infiltrate deep behind enemy lines, neutralize syndicate drone armadas, and eliminate the frontline warmachine—**Vanguard-X**—to reclaim humanity's future.

---

Scene background photos and project-owned 3D assets, scenes, scripts, and web viewers have been removed. Operative portraits, sprites, tiles, UI artwork, and 2D glow remain.

## Features

- **6 Unique Cyber Operatives** — Each equipped with distinct tactical profiles, attributes, and passive synergies:
  1. **Cyber Shogun (Kaito Kurogane)** — Balanced Vanguard Striker (Katana combos, blade dash, parry, rapid shield recharge).
  2. **Surya Prime (Arjun Varma)** — Solar Bulwark Tank (140 HP, 80 Shield, 15% armor damage mitigation, solar flare).
  3. **Oceanus (Kiana Vaea)** — Hydro Vanguard Tactician (90 HP, 70 Shield, 25 HP/s passive hydro shield regen, hydro torrents).
  4. **Mirage Blade (Zayd Al-Mansoor)** — Phantom Infiltrator Assassin (460 Speed, 1000 Dash velocity, 0.6s dash cooldown, phase daggers).
  5. **Apex Titan (King Tariq Nzinga)** — Electro-Magnetic Juggernaut (160 HP, 60 Shield, 20% kinetic armor mitigation, knockback resistance).
  6. **Aura Phoenicia (Maya Khoury)** — Ultrasonic Prism Weaver (95 HP, 55 Shield, 8% mitigation, 0.85s dash cooldown, harmonic resonance).
- **Level 1: Ghost in the Gridlock (platformer)** — A ~9,500 px side-scrolling infiltration of the Eternal Zen Dojo over a painted night-garden backdrop: Garden Gate (jumping), Lantern Court (terraces and a timed laser bridge), Blossom Gorge (wall-jump climb), Rooftop Run (gaps, a moving platform, sentry fire), Archive Descent (surge floor and laser tripwire), then Beacon Court — destroy the demolition beacon to open the dojo gate, take the Data-Blade, and reach the extraction portal. Three shrines are checkpoints, three optional memory fragments are hidden on perches, and falling into a chasm returns you to the last shrine. All six operatives can finish it.
- **New-campaign flow** — Select an operative, watch AXIOM-7's story briefing, complete or skip Protocol Zero training, then enter Level 1. Cyber Shogun's Level 1 sprite animations use the approved AI-generated pose and locomotion sheets; The backdrop is a painted garden image under procedurally drawn platforms.
- **Side-Scrolling Combat in Levels 2–4** — Existing ground combos, blade dashes, wall-sliding, and wall-jumping continue in the later platforming missions.
- **Dynamic Combat HUD** — Real-time telemetry monitoring health, shields, combo streaks (`DOUBLE STRIKE!`, `TRIPLE COMBO!`, `TACTICAL OVERDRIVE!`), operative ability status, and boss telemetry.
- **Vanguard-X Boss Encounter** — Multi-phase aerial boss mech in Level 4 featuring homing plasma missiles, siren warnings, and an enraged **Overdrive Rage** phase shift (<50% HP) with 3-way missile spreads.
- **Tactical Victory Scorecard** — End-of-level combat assessment evaluating completion time, score, and damage taken to award combat grades (**S Rank / Flawless**, **A Rank / Vanguard**, **B Rank / Operative**) with victory fanfare.
- **Dynamic 2D Environmental Hazards** — Pulsing lethal laser grids (`CyberLaserGrid2D`) and rhythmic electrified floor panels (`CyberHazardFloor2D`).
- **4-Level Campaign Chain** — Linear mission progression through neon rooftops and industrial sectors (`level1.tscn` $\rightarrow$ `level4.tscn`).
- **Synthesized 16-Bit SFX Engine** — Dedicated procedural sound design for blade slashes, dashes, clashes, laser hums, drone explosions, and boss alarms.
- **Operative Selection Matrix** — A two-panel layout pairs the roster with one unified dossier containing the animated hand-drawn preview, identity, stats, movement controls, a single set of ability buttons, and Upgrade/Deploy actions. Keyboard and controller navigation follow the visible controls for each operative.
- **Cross-Platform Backend & Web Portal** — Django REST Framework backend with PostgreSQL session leaderboards and a modern React + Vite frontend.

---

## Controls

| Action | Keyboard | Gamepad |
|---|---|---|
| **Move Left / Right** | `A` / `D` or `←` / `→` | D-Pad Left / Right or Left Stick |
| **Jump / Wall Jump** | `Space` / `W` / `↑` | A / Cross Button |
| **Wall Slide** | Hold movement against wall | Hold stick against wall |
| **Katana Slash** | `F` / Left Mouse Button | X / Square Button |
| **Martial Strike** | `Q` | Y / Triangle Button |
| **Blade Dash** | `Shift` / `C` | Right Shoulder / Trigger |
| **Pause Game** | `Escape` | Start / Options |

Level 1 uses the same platformer controls as Levels 2–4; the on-screen hint switches between keyboard and gamepad.

---|---|---|
| **Move / Aim** | `WASD` / Mouse | Left / Right Stick |
| **Primary Attack** | `F` / Left Mouse Button | Left Trigger |
| **Operative Ability** | `Q` / Right Mouse Button | Right Trigger |
| **Dash / Interact / Pause** | `Shift` / `E` / `Escape` | Right Bumper / `A` / Start |

---

## Tech Stack & Architecture

| Component | Technology | Description |
|---|---|---|
| **Game Engine** | Godot 4.7.2 .NET | C# scripting with .NET 10 runtime (`net10.0`). |
| **Core Architecture** | Component & Manager Pattern | `OperativeRegistry`, `SessionManager`, `UISoundManager`, `CyberCombatHud`. |
| **Backend API** | Django 5.x / DRF | Python 3.14 REST API managing authentication and high scores. |
| **Web Frontend** | React + Vite + Tailwind | Web dashboard and player hub. |
| **Database** | PostgreSQL 18 | Persistent session and leaderboard storage with local JSON fallback. |
| **Containers** | Native WSL Containers | Automated microservices via Windows 11 `container.exe` (no Docker Desktop). |

---

## Getting Started

### Prerequisites

- [Godot 4.7.2 .NET (Mono)](https://godotengine.org/)
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- Python 3.14 (for Django backend tests and sound synthesis tools)

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
│   ├── main_menu/                    # Character selection, menus, and story
│   ├── gameplay/                     # Level 1-4 scenes, boss encounter, hazards
│   └── ui/                           # Combat HUD, scorecard, and dialog bubbles
├── scripts/
│   ├── actors/                       # CyberShogun, OperativeProfile, CyberBossMech2D, CyberDrone2D
│   ├── gameplay/                     # CyberBossMissile2D, CyberLaserGrid2D, CyberHazardFloor2D
│   ├── ui/                           # CyberCombatHud, CyberVictoryScorecard
│   ├── managers/                     # SessionManager, UISoundManager
│   └── tools/                        # generate_cyber_sfx.py sound synthesizer
├── backend/                          # Django REST Framework backend service
├── frontend/                         # React + Vite web application
└── tests/                            # GdUnit4 and NUnit test suites
```

---

## WSL Container Architecture

Axiom Force runs containerized backend services using native Windows 11 WSL Containers:

- `.\setup_wsl_containers.ps1` — Builds backend and website container images.
- `.\run_wsl_containers.ps1` — Starts PostgreSQL 18, Django API, and Nginx web server on bridge network `frog_network`.
- `.\stop_wsl_containers.ps1` — Stops and tears down active container instances.
- `.\export_game_wsl.ps1` — Generates automated headless release builds.

---

## Contributing & Agent Guidelines

AI agents collaborating on this repository must consult the following guides:
- [`HANDOFF.md`](./HANDOFF.md) — Comprehensive technical briefing on recent milestones, current 2D architecture and roadmap.
- [`CODEX.md`](./CODEX.md) / [`CLAUDE.md`](./CLAUDE.md) / [`GEMINI.md`](./GEMINI.md) / [`AGENTS.md`](./AGENTS.md) — Universal agent operational rules, mandatory testing protocol, and permissions.

## Agent Task Completion

After any task that edits the game or its website (including backend, assets, configuration, tests, tooling, or related documentation), agents must update both this root `README.md` and `HANDOFF.md` before finishing. Keep README behavior and contributor instructions current; record implementation decisions, verification status, limitations, and follow-up work in the handoff. Complete documentation edits before the required background Godot refresh and unit tests. Read-only and Git-only tasks are exempt. Commits and pushes still require explicit permission for the current changes. This requirement is shared by `CLAUDE.md`, `CODEX.md`, and `GEMINI.md`.
