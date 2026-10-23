# Axiom Force - Universal Agent Guidelines

## Project Overview
A high-fidelity cyber-action game developed in **Godot 4.7.2 .NET (C#)** with a **Django REST Framework** backend and React web client. The game is transitioning to a high-octane 2D Cyber-Action platformer (inspired by *Katana Zero*, *Hades*, and *Ghostrunner*), featuring multi-operative synergy, dynamic combat HUDs, wall mobility, multi-phase boss mechs, and synthesized audio.

## Technical Stack
- **Engine**: Godot 4.7.2 .NET
- **Language**: C# (.NET 10)
- **Backend**: Django REST Framework (Python 3.14)
- **Frontend Web**: React + Vite
- **Architecture**: Component-based with specialized Managers (e.g., `SessionManager`, `UISoundManager`, `OperativeRegistry`).
- **UI & Gameplay Approach**: High-fidelity 2D combat platforming with 4-level linear campaign chain, dynamic combat HUD, and a squad of operatives that are unlocked through the story (roster screen, in-level swap).

## Development Workflows

### Godot Background Refresh (After Edits Only)
Run a background Godot refresh only after creating, modifying, or deleting project files during the current task. Batch related edits and refresh once after the edits are complete; do not refresh after every individual file edit.

- Skip automatic refreshes for read-only questions, explanations, inspections, reviews, status checks, or Git-only operations that do not change project content.
- Existing uncommitted changes alone do not trigger another refresh. Repeat only after further edits, an unresolved import issue, or an explicit user request.
- After edits, use the console executable from the project root:

  ```powershell
  & "C:\Users\karthiknk21\Desktop\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe" --headless --path . --editor --import
  ```

- Keep the refresh in the background without opening an interactive editor. If using `Start-Process`, pass `-WindowStyle Hidden`.
- Wait for import completion and process exit, and check the log for errors. When assets changed, verify their `.import` files were generated. Do not terminate the user's existing Godot editor.

### Build & Test
- **Build project**: `dotnet build AxiomForce.csproj`
- **Verification**: Check Godot runtime logs for warnings or missing asset errors.

## Mandatory Testing Protocol (After Edits Only)
> [!IMPORTANT]
> After creating, modifying, or deleting project files during the current task, all AI agents (Codex, Claude, Gemini / Antigravity) **MUST** run the unit test suites after the final edits and verify that all tests pass (0 failures, 0 errors, 0 orphans) before concluding the turn. This includes documentation and configuration edits.
>
> **Skip automatic unit tests when no project files were edited**, including read-only questions, explanations, inspections, reviews, status checks, and Git-only operations. Existing uncommitted changes or a new prompt alone do not require a rerun. Explicit user requests to run tests still apply. After a successful run, repeat only if further edits or unresolved failures require it.
>
> Run from the project root:
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
> ### 1. Explicit Permission Required for Git Commits and Pushes
> **NEVER create a Git commit or push to a remote repository unless the user explicitly instructs you to perform that action on the current changes.**

- This applies to every agent and every tool, including Git commands, IDE integrations, APIs, and automation scripts. It includes creating or amending commits and publishing commits remotely.
- A request to commit authorizes a local commit only. A request to commit and push authorizes both. If the user explicitly asks to push the current uncommitted changes, creating the commit needed for that push is included.
- Authorization applies only to the requested operation and current changes. Previous commit or push instructions do **not** authorize committing or pushing later edits or future tasks.
- Requests to edit, fix, test, refresh Godot, or continue work do **not** authorize commits or pushes.
- Without explicit authorization, finish the requested work and leave changes uncommitted and local. Do not automatically commit or push, or ask for permission merely because the work is complete.
- Stage changes only when requested or as part of an authorized commit. Never force-push without explicit authorization for that action.

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
- `CharacterSelection.cs`: Squad roster screen (main menu): locked/unlocked operatives, Set Active, upgrades; no longer part of the new-game flow.
- `OperativeRoster.cs` / `LevelCatalog.cs` / `CampaignProgress.cs`: engine-free unlock rules (who joins in which mission), the ordered mission list, and the glue to `Global` and saves. Operatives are earned through play; the squad is rebuilt from mission progress.
- `CodeLevel.cs`: base class for code-built missions (`PalaceMission`, `ConcreteWildfireMission`, `NeonRustMission`, `ShatteredAegisMission`) with `PalacePlatform`, `AbilityGate` (operative-gated obstacles), `RecruitBeat` (unlock + in-level ability lesson) and `SquadStrip` HUD.
- `scenes/gameplay/level1.tscn` to `level4.tscn`: 4-level linear 2D campaign progression (thin scenes that attach a `CodeLevel` subclass).

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
- **NO Git commits or pushes** without explicit user instruction for the current changes.
- **NO Docker CLI / Docker Desktop** commands.
- **NO standard WSL distro installations** (`wsl --install`).
