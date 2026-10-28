# Axiom Force - Project Guide (Codex)

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

### Docker Container Preflight (Before Editing Code Files)
Before starting any task that involves editing code files (`.cs`, `.gd`, `.py`, `.ts`/`.tsx`/`.js`, tooling scripts, or configuration a running service reads), confirm the project's Docker containers are running:

```powershell
docker ps
```

- If Docker Desktop itself is down, start it and wait until `docker info` reports a live daemon before continuing.
- If the project containers (Django backend, PostgreSQL, website) are missing from `docker ps`, start them from the project root with `docker compose up -d` and re-check with `docker ps`. If a container exits immediately, read `docker compose logs <service>` before editing.
- Do not begin code edits while the stack is down, and never stop containers the user started.
- The stack is defined in `docker-compose.yml`: `db` (PostgreSQL 18, 5432), `backend` (Django, 8000), `website` (Nginx, 5173) and `ollama` (LLM runtime, 11434). All four are always-on members of `docker compose up`.
- **Game edits and website edits may run at the same time as the stack** — they do not conflict, so do not stop the containers to work on either.
- **Game exports are exclusive.** Never run one while another export, a Godot import refresh, a website image build, or a game/website edit batch is still in progress, and never chain an export into the same command as other work. Finish or park everything else first.
- The exporter is a one-shot service behind the `export` profile (`docker compose run --rm export <linux|windows|web|android|all>`, image `axiom-force-export`). A plain `docker compose up` never starts it, and starting the service by itself is inert: with no explicit target it prints usage and exits without building anything. Never add `export` to the default services, and rebuild the image after editing `export/export.sh` because the script is baked into it.
- Skip this preflight for read-only questions, explanations, reviews, status checks, documentation-only (`.md`) edits, and Git-only operations.

### Godot Background Refresh (After Code/Asset Edits Only)
Run a background Godot refresh only after creating, modifying, or deleting game engine files, scenes, scripts, shaders, or assets (.cs, .gd, .tscn, .res, .tres, images, audio, etc.) during the current task. Batch related edits and refresh once after the edits are complete; do not refresh after every individual file edit.

- Skip automatic refreshes for read-only questions, explanations, inspections, reviews, status checks, Git-only operations, and documentation-only (.md) edits or deletions that do not alter game assets or code.
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

## Mandatory Testing Protocol (After Code/Asset Edits Only)
> [!IMPORTANT]
> After creating, modifying, or deleting project code, scenes, shaders, or asset files (.cs, .gd, .tscn, .py, .json, art/sound) during the current task, all AI agents (Codex, Claude, Gemini / Antigravity) **MUST** run the unit test suites after the final edits and verify that all tests pass (0 failures, 0 errors, 0 orphans) before concluding the turn.
>
> **Skip automatic unit tests when no code, scene, or asset files were edited** — including read-only questions, explanations, inspections, reviews, status checks, Git-only operations, and **documentation-only (.md) edits or deletions**. Existing uncommitted changes or a new prompt alone do not require a rerun. Explicit user requests to run tests still apply. After a successful run, repeat only if further edits or unresolved failures require it.
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

## Mandatory In-Code Documentation

- **Every piece of code must be documented in the code itself.** This applies to all agents and all code they create or modify, including gameplay, UI, backend, frontend, tests, scripts, and tooling.
- Document each module, class, function, and method using the language's appropriate comments or documentation format (for example, C# XML documentation, Python docstrings, or GDScript/JavaScript documentation comments).
- Explain the code's purpose, behavior, and relevant inputs, outputs, side effects, assumptions, and constraints. Add inline comments for non-obvious logic, algorithms, state transitions, and workarounds, explaining why they are needed.
- Keep documentation accurate whenever code changes. Document previously undocumented code when working on it. Documentation in Markdown files, commit messages, or chat does not replace documentation in the source code.
- Use clear, useful explanations rather than comments that merely repeat the syntax.

## Mandatory README and Handoff Updates

- **Before finishing any task that creates, modifies, or deletes files for the game or its website, every agent MUST update both root `README.md` and root `HANDOFF.md`.** This applies to gameplay, scenes, assets, UI, website frontend, backend, configuration, tooling, tests, and related documentation.
- Update `README.md` with the relevant current behavior, features, setup, usage, or development workflow so it remains accurate for users and contributors.
- Update `HANDOFF.md` with what changed, important implementation decisions, verification results, and any remaining limitations or follow-up work so the next agent can continue accurately. Distinguish completed work from planned work; never claim checks passed before they have run.
- Make meaningful, task-specific updates to both files as part of the same task, before the final response. A chat summary, code comments, or commit message does not replace either update.
- Include these documentation changes in the final batch of edits before the required background Godot refresh and unit tests when code/assets are changed. Read-only tasks, Git-only operations, and documentation-only tasks that do not change project code or assets do not trigger refreshes or unit tests.
- Updating documentation does not authorize a Git commit or push; explicit user instructions for the current changes are still required.

## Permissions Denylist
- **NO Git commits or pushes** without explicit user instruction for the current changes.
- **NO standard WSL distro installations** (`wsl --install`).
