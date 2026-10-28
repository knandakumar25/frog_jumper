# Axiom Force

A high-fidelity 2D cyber-action game developed in **Godot 4.7.2 .NET (C#)** with a **Django REST Framework** backend and **React** web client. All four levels are side-scrolling platformers. The game features dynamic operative combat, special abilities, multi-operative passive synergy, multi-phase boss mechs, and a reactive 2D combat HUD.

---

## Story

Earth has fallen under the tyrannical rule of an autonomous cybernetic syndicate. A clandestine strike team known as **Axiom Force** deploys into the neon-drenched sectors of the metropolis. Begin as Cyber Shogun, gain new operatives as the story unfolds, infiltrate deep behind enemy lines, neutralize syndicate drone armadas, and eliminate the frontline warmachine—**Vanguard-X**—to reclaim humanity's future.

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
- **Earned operatives** — A new campaign starts with **Cyber Shogun** alone (no selection screen). Each other operative **joins during a mission**, where a recruit beat introduces them and teaches their signature ability on the spot (there is no separate training level). Swap between unlocked operatives any time with `Tab` / `Shift+Tab`, the number keys `1-6`, or the gamepad (D-pad Down / Back); the squad shares one health/shield ratio and swapping has a short cooldown. The **Squad Roster** (main menu) shows locked operatives and lets you upgrade them and choose who starts the next mission.
- **Signature abilities (`Q`)** — Shogun: **Kinetic Parry** (throws ranged attacks back); Oceanus: **Tidal Torrent** (douses fire); Apex Titan: **Tectonic Slam** (shatters heavy rubble); Aura Phoenicia: **Harmonic Pulse** (opens sonic locks); Mirage Blade: **Phase Blink** (slips through lasers and hazards); Surya Prime: **Solar Bulwark** (absorbs heat and ranged fire). Levels contain obstacles only the matching operative can clear.
- **The campaign (4 side-scrolling missions, code-built on `CodeLevel`)** —
  1. **Ghost in the Gridlock** (Shogun): a ~9,500 px infiltration of the Eternal Zen Dojo over a painted night garden — jumping, a timed laser bridge, a wall-jump gorge, rooftops, an archive; parry sentry fire to overload the beacon's shield, destroy it, take the Data-Blade, extract.
  2. **The Concrete Wildfire** (Oceanus, then Apex Titan join): burning corridors, a chasm overpass, three biometric spires, then the membrane and a collapsed wall.
  3. **Neon Rust and Data Dust** (Aura Phoenicia, then Mirage Blade join): scrapyard, transit depot, a comm-tower climb to a relay, and a lethal laser curtain.
  4. **Shattered Aegis** (Surya Prime joins): the Border Fortress, superheated corridors, a laser curtain, and Border Commander Voss; defeating him brings down the wall and ends Act I with the combat scorecard.
  Each mission has three shrine checkpoints, three optional memory fragments, a story title card (from `NarrativeManager`), and chasm recovery to the last shrine.
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
| **Database** | PostgreSQL 18 | Persistent session and leaderboard storage with local JSON fallback. |

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

AI agents collaborating on this repository must consult the following guides:
- [`HANDOFF.md`](./HANDOFF.md) — Comprehensive technical briefing on recent milestones, current 2D architecture and roadmap.
- [`CODEX.md`](./CODEX.md) / [`CLAUDE.md`](./CLAUDE.md) / [`GEMINI.md`](./GEMINI.md) / [`AGENTS.md`](./AGENTS.md) — Universal agent operational rules, mandatory testing protocol, and permissions.

## Agent Task Completion

After any task that edits the game or its website (including backend, assets, configuration, tests, tooling, or related documentation), agents must update both this root `README.md` and `HANDOFF.md` before finishing. Keep README behavior and contributor instructions current; record implementation decisions, verification status, limitations, and follow-up work in the handoff. Complete documentation edits alongside the task. Unit tests and Godot refreshes are required only when code, scene, or asset files are created, modified, or deleted, and are skipped for documentation-only tasks. Read-only and Git-only tasks are exempt. Commits and pushes still require explicit permission for the current changes. This requirement is shared by `CLAUDE.md`, `CODEX.md`, and `GEMINI.md`.
