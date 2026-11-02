# Axiom Force - Project Guide (Antigravity / Gemini)

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

### Mandatory Game-Development Skills (All Game-Related Work)

- Whenever an agent performs game-related work, it **MUST first read and apply the relevant skills from `gamedev-skills/awesome-gamedev-agent-skills`**. This applies to Codex, Claude Code, and Google Antigravity, including implementation, debugging, reviews, advice, planning, assets, and game-related documentation.
- Start a new game-development task by reading the installed `router/SKILL.md`, then read the complete `SKILL.md` files for the engine, discipline, genre, or workflow it selects before acting. Load the relevant set rather than every installed skill, and re-route when the task changes concern.
- This project uses Godot 4.7.2 .NET/C# and a 2D platformer campaign. Choose the Godot/C# and relevant 2D/task skills; adapt version-specific examples to the actual project instead of changing engines, languages, or architecture to match a skill.
- For Godot C#/.NET tasks involving setup, scenes/nodes, scripting, debugging, prototyping, interactions, or UI, agents **MUST also read and apply** the installed godot-csharp-essentials/SKILL.md and the relevant bundled course lesson. It supplements the specialized Godot C# and task skills; its Godot 4.5 examples must be adapted to this project's Godot 4.7.2 .NET/C# and 2D architecture.
- Global installation roots: Codex `~/.agents/skills/`; Claude Code `~/.claude/skills/`; Antigravity app/IDE `~/.gemini/config/skills/`; Antigravity CLI `~/.gemini/antigravity-cli/skills/`. Each contains the router and all 77 specialized skills, including bundled supporting files.
- Use skill discovery first. If it does not expose the installed skills, read their files directly from the appropriate root. Resolve missing skill access before continuing game-related work; do not silently bypass this requirement.
- Skills supplement this project's lifecycle safety, testing, documentation, Docker, and permission rules. Explicit user instructions take precedence, and skills never authorize commits, pushes, publishing, or destructive operations on their own.

### Mandatory Docker Skills (All Docker-Related Work)

- Whenever an agent performs any Docker-related work, it **MUST first read and apply the relevant skills from Docker's official `docker/skills` package**, installed as `docker-skills@docker`. This applies to every agent, including Codex, Claude, and Gemini.
- This requirement covers Docker commands (including preflight/status checks, starting, pausing, stopping, and inspecting containers), Docker Desktop troubleshooting, Dockerfiles, image builds, Compose, networking, volumes, registries, exports, and Docker-related advice, reviews, and documentation.
- Select the skills that match the task, such as `docker-compose-patterns`, `docker-build-strategies`, `docker-project-foundations`, and `docker-docs`. Read their `SKILL.md` instructions before acting and follow the applicable workflow; merely having the plugin installed does not satisfy this requirement.
- Use the installed package through the agent's skill discovery mechanism. If it is not exposed there, locate and read the installed `docker/skills` skill files directly. If the relevant skills cannot be accessed, explain the missing dependency and resolve access before continuing Docker work; do not silently bypass them.
- These skills supplement the project's container preflight, export exclusivity, and permission rules below. Explicit user instructions take precedence. Use `docker` directly from PATH for CLI commands.

### Mandatory PostgreSQL and Android Skills (All Relevant Work)

- Whenever an agent performs PostgreSQL-related work, it **MUST first read and apply** `postgresql-best-practices/SKILL.md`. This covers database commands and tools, schema/query design, migrations, performance, reliability, security, troubleshooting, and PostgreSQL-related advice, planning, reviews, and documentation. For graph modeling, Apache AGE, or openCypher work, it **MUST also read and apply** `pg-graph/SKILL.md` before acting.
- Whenever an agent performs Android-related work, it **MUST first identify and read the complete relevant Android `SKILL.md` and its bundled references**, then apply that guidance. This covers Android tools and commands, implementation, debugging, testing, architecture, advice, planning, reviews, and documentation. Choose every skill relevant to the task (for example Compose, navigation, security, performance, testing, or Android tooling).
- Use normal skill discovery first. If discovery does not expose the installed skills, read them directly from the active agent's user-level root below. If a required skill cannot be accessed, resolve access before continuing; do not silently bypass this requirement. Merely having a skill installed does not satisfy the requirement to read and apply it for relevant work.
- User-level installation roots on this machine: Codex `C:\Users\karthiknk21\.agents\skills\`; Claude Code `C:\Users\karthiknk21\.claude\skills\`; Antigravity app/IDE `C:\Users\karthiknk21\.gemini\config\skills\`; Antigravity CLI `C:\Users\karthiknk21\.gemini\antigravity-cli\skills\`. Each requested skill is a direct child directory containing `SKILL.md`; keep its bundled files alongside it.
- Sources: PostgreSQL skills from `microsoft/postgres-skills` (`plugin/skills/`); Android skills from `android/skills` (all skill directories). The PostgreSQL skills are guidance only and do not configure a PostgreSQL MCP server or database connection.

### Docker-Only Game Exports

- Game exports must run only through the local Docker Compose `export` service, with an explicit target (`linux`, `windows`, `web`, `android`, `macos`, `ios`, or `all`). Use the root `export.bat` / `export.sh` launchers or the documented Compose command; never invoke host Godot export commands except the Apple completion steps described below.
- Apple release exception: after Docker preparation, macOS signing/notarization and native iOS Godot/Xcode project generation, archiving and distribution must be completed locally on a Mac. The `ios` Docker target produces a source handoff, not an IPA; see `export/APPLE_RELEASE.md`. Native Apple completion is authorized as a workflow capability, not permission to export during a setup-only task.
- Do not create, restore, or run GitHub Actions export workflows or export jobs, including manual, scheduled, push, pull-request, release, reusable, or Docker-based Actions jobs. GitHub Actions must not produce game export artifacts.
- A request to reorganize export configuration does not authorize an actual export. Keep the exporter opt-in and inert without an explicit target; preserve export exclusivity.

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
- The exporter is a one-shot service behind the `export` profile (`docker compose run --rm export <linux|windows|web|android|macos|ios|all>`, image `axiom-force-export`). A plain `docker compose up` never starts it, and starting the service by itself is inert: with no explicit target it prints usage and exits without building anything. Never add `export` to the default services, and rebuild the image after editing `export/export.sh` because the script is baked into it.
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

## Mandatory Project Memory Updates (After Every Task)

- **Before finishing every task, every agent MUST update root `MEMORY.MD`**, including implementation, debugging, planning, reviews, read-only investigations, documentation-only tasks, and Git-only tasks. This applies to Codex, Claude Code, and Google Antigravity.
- Record the task's outcome, durable decisions or preferences, verification actually performed, and unresolved limitations or next steps. Keep entries concise; revise outdated facts rather than accumulating duplicate summaries. If nothing durable changed, add a brief dated outcome noting that fact.
- Read the existing memory before updating it. Preserve unrelated context, distinguish completed work from plans and historical checks, and never include secrets, tokens, or credentials.
- Use the existing exact filename `MEMORY.MD`; do not create a second file named `MEMORY.md`. This requirement supplements the README and HANDOFF rules rather than replacing them.
- A memory-only Markdown edit does not trigger Docker preflight, Godot import, or unit tests. For code/asset tasks, include the memory update in the final documentation batch and accurately record subsequent verification results.
- Updating memory never grants permission to stage, commit, or push. Explicit authorization for current changes is still required.

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
