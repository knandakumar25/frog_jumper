# Axiom Force - Project Guide (Antigravity / Gemini)

## Project Overview
A high-fidelity cyber-action game developed in **Godot 4.7.2 .NET (C#)** with a **Django REST Framework** backend and React web client. The game is transitioning to a high-octane 2D Cyber-Action platformer (inspired by *Katana Zero*, *Hades*, and *Ghostrunner*), featuring multi-operative synergy, dynamic combat HUDs, wall mobility, multi-phase boss mechs, and synthesized audio.

## Technical Stack
- **Engine**: Godot 4.7.2 .NET
- **Language**: C# (.NET 10)
- **Backend**: Django REST Framework (Python 3.14)
- **Frontend Web**: React + Vite
- **Architecture**: Component-based with specialized Managers (e.g., `SessionManager`, `UISoundManager`, `OperativeRegistry`).
- **UI & Gameplay Approach**: High-fidelity 2D combat platforming with 4-level linear campaign chain, dynamic combat HUD, and operative selection cards.

## Development Workflows

### Asset Import Workflow
Whenever 3D/2D assets, textures, or fonts are added/modified:
1. Run Godot in the background: `"C:\Users\karthiknk21\Godot\Editors\4.7.2-stable-mono\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe" --headless`
2. Wait for `.import` files to be generated.
3. Stop the Godot process.
*This ensures assets are visible in the runtime.*

### Build & Test
- **Build project**: `dotnet build AxiomForce.csproj`
- **Verification**: Check Godot runtime logs for warnings or missing asset errors.

## Mandatory Testing Protocol (End of Every Prompt)
> [!IMPORTANT]
> At the conclusion of **EVERY** prompt or task, all AI agents (Codex, Claude, Antigravity) **MUST** execute the unit test suites and verify that all tests pass (0 failures, 0 errors, 0 orphans) before concluding your turn:
>
> ```powershell
> .\run_tests.ps1
> ```
>
> You can also run the individual test suites:
> 1. **Django Unit Tests (Backend)**:
>    ```powershell
>    python backend/manage.py test accounts
>    # Or directly using the virtual environment:
>    backend\.venv\Scripts\python.exe backend\manage.py test accounts
>    ```
> 2. **NUnit Tests (.NET 10 C#)**:
>    ```powershell
>    dotnet test tests/AxiomForce.Tests/AxiomForce.Tests.csproj
>    ```
> 3. **GdUnit4 Tests (Godot 4 GDScript)**:
>    ```powershell
>    & "C:\Users\karthiknk21\Desktop\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe" --headless --path . -s addons/gdUnit4/bin/GdUnitCmdTool.gd --add res://tests/ --ignoreHeadlessMode
>    ```
> *(Note: Always use the `_console.exe` binary on Windows to avoid stdout deadlocks during headless test runs).*

## Critical Operational Rules

> [!CAUTION]
> ### 1. Strict Git Push Prohibition
> **NEVER execute `git push` unless the user explicitly commands you to push** (e.g., *"push to git"*). Staging and committing locally are permitted when requested, but pushing to remote repositories without explicit instruction is strictly prohibited.

### 2. Node Lifecycle & Unit Test Safety
- Standalone unit tests instantiate scenes outside the active scene tree. **Always check `IsInsideTree()`** before calling `GetTree()`, `CreateTween()`, or `.Play()` on audio players to prevent engine error 100/105.
- Always call `.free()` or `.queue_free()` on test-instantiated nodes in GdUnit4 to guarantee 0 orphan nodes.
- Always check `LengthSquared() > 0` before calling `.Normalized()` on vectors.

## Core Components & Active Systems
- `HANDOFF.md`: Comprehensive inter-agent briefing detailing the 2D transition, inventory of remaining 3D components, and migration roadmap.
- `OperativeProfile.cs`: Model and central registry (`OperativeRegistry`) for all 6 operatives with unique passive perks and combat stats.
- `CyberShogun.cs`: Core 2D player controller (katana slashes, martial strike, blade dash, wall-sliding, wall-jumping, passive shield regen).
- `CyberBossMech2D.cs` & `CyberBossMissile2D.cs`: Multi-phase 2D boss mech encounter ("Vanguard-X") in `level4.tscn` with Overdrive Rage phase shift.
- `CyberCombatHud.cs`: Real-time 2D HUD tracking shields, health, combo streak (`DOUBLE STRIKE!`, `TACTICAL OVERDRIVE!`), operative abilities, and boss health bar.
- `CyberVictoryScorecard.cs`: Post-mission combat grade evaluation (S/A/B rank scoring) triggered upon level completion.
- `CyberLaserGrid2D.cs` & `CyberHazardFloor2D.cs`: Cycling environmental 2D hazards.
- `CharacterSelection.cs`: Operative selection matrix featuring high-resolution 2D portrait cards and attribute meters.
- `scenes/gameplay/level1.tscn` to `level4.tscn`: 4-level linear 2D campaign progression.

## Operative Roster
1. **Cyber Shogun (Kaito Kurogane)**: Balanced Vanguard Striker (Katana combo, blade dash, parry).
2. **Surya Prime (Arjun Varma)**: Solar Bulwark Tank (140 HP, 80 Shield, 15% armor damage mitigation, solar flare).
3. **Oceanus (Kiana Vaea)**: Hydro Vanguard Tactician (90 HP, 70 Shield, 25 HP/s hydro shield regen, hydro torrents).
4. **Mirage Blade (Zayd Al-Mansoor)**: Phantom Infiltrator Assassin (460 Speed, 1000 Dash velocity, 0.6s dash cooldown, twin phase daggers).
5. **Apex Titan (King Tariq Nzinga)**: Electro-Magnetic Juggernaut (160 HP, 60 Shield, 20% kinetic armor mitigation, knockback resistance).
6. **Aura Phoenicia (Maya Khoury)**: Ultrasonic Prism Weaver (95 HP, 55 Shield, 8% mitigation, 0.85s dash cooldown, harmonic resonance).
*(Note: Solaria has been completely removed from the project).*

## WSL Containers Architecture (NO DOCKER, NO DISTROS)
This project uses native Windows 11 WSL Containers via `C:\Program Files\WSL\container.exe`.
- **Do NOT use Docker Desktop, docker cli, or docker-compose.** (Docker is permanently removed from this system).
- **Do NOT spin up standard WSL Linux distros** (`wsl --install Ubuntu` etc.).
- **How to manage containers**: Use the pre-built project scripts:
  - `.\setup_wsl_containers.ps1`: Pulls base layers and builds container images (`frog_jumper_backend`, `frog_jumper_website`).
  - `.\run_wsl_containers.ps1`: Starts PostgreSQL 18, Django backend, and Nginx website on bridge network `frog_network`.
  - `.\stop_wsl_containers.ps1`: Stops and removes the active container instances.
  - `.\export_game_wsl.ps1`: Runs headless Godot 4.7.2 + Android SDK exporter container (`export/Containerfile`).
- **CLI path**: `& "C:\Program Files\WSL\container.exe" <command>` (supports standard OCI commands like `list`, `build`, `run`, `rm`, `image list`).

## Permissions Denylist
- **NO `git push`** without explicit user instruction.
- **NO Docker CLI / Docker Desktop** commands.
- **NO standard WSL distro installations** (`wsl --install`).
