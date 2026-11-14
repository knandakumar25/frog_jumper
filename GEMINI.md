# Axiom Force - Project Guide (Antigravity / Gemini)

Phase 5 art update (2026-11-14): the five approved sheets cost 138 credits, with
1,618 remaining. Generation is finished; see the Phase 5 status in
`docs/ART_REGENERATION.md` for integration and verification. The 2,170-credit
plan total is unchanged. Future phases still require their own approval.

## Project Overview
A high-fidelity cyber-action game developed in **Godot 4.7.2 .NET (C#)** with a **Django REST Framework** backend and React web client. It is a 2D cyber-action platformer (inspired by *Katana Zero*, *Hades*, and *Ghostrunner*) with a 12-mission, 3-act campaign, multi-operative synergy, dynamic combat HUDs, wall mobility, and multi-phase boss mechs, set in Los Angeles in 2157. Finishing Mission 1 opens a top-down open world (`scripts/world/`). In that world only Little Tokyo and Concrete Wards have playable quests and combat; the other 14 regions can be explored, but their quests, operations and endgame content are data only (see the status table in `docs/NEO_KYOTO_EXPANSION_PLAN.md`). Saves are PostgreSQL-only, and the desktop game signs in through a Clerk browser flow.

## Technical Stack
- **Engine**: Godot 4.7.2 .NET
- **Language**: C# (.NET SDK 11.0.100, the latest installed; projects target `net10.0`)
- **Backend**: Django REST Framework (Python 3.15.2)
- **Frontend Web**: React + Vite
- **Architecture**: Component-based with specialized Managers (e.g., `SessionManager`, `UISoundManager`, `OperativeRegistry`).
- **UI & Gameplay Approach**: High-fidelity 2D combat platforming with a 12-mission linear campaign (`LevelCatalog.cs`), dynamic combat HUD, and a squad of operatives that are unlocked through the story (roster screen, in-level swap).

## Development Workflows

### Mandatory Elephant-Goldfish Model (EGM) Skills (Every Run & All Tasks)

- Whenever an agent executes ANY task, it **MUST read and apply the relevant skills from Google's Elephant-Goldfish Model (`google/elephant-goldfish-model`) on EVERY RUN, regardless of the prompt or task type**. This applies to all AI agents: Google Antigravity (Gemini), Claude Code, and OpenAI Codex.
- The EGM architecture pairs a persistent contextual agent (**The Elephant**) with isolated, fresh-context adversarial reviewers (**The Goldfish**) to systematically eliminate unstated assumptions, design blind spots, and architectural drift:
  1. **`egm-review-loop` & `egm-review`**: Orchestrate automated adversarial Goldfish review loops to audit proposed architectures, implementation plans, code edits, and documentation against structural and quality standards before finalizing.
  2. **`egm-draft`**: Construction of technical specifications, plans, and design documents. Enforces **Artifact-First Delivery** (writing plans and large summaries directly to files/artifacts, never dumping raw blocks into chat), the **Plain-English Standard** (zero promotional fluff or decorative adjectives like "seamless" or "robust"), and **Default to Stress-Testing** without sycophancy or echo-chamber validation.
  3. **`egm-brainstorm`**: Upstream interactive ideation, problem clarification, and architectural trade-off analysis utilizing TRIZ inventive principles.
  4. **`egm-resolve`**: Primary Elephant resolution workflow for ingesting Goldfish deficiency reports, defending deliberate design decisions, and applying surgical fixes.
  5. **`egm-bootstrap`**: Repository and directory-level documentation contract generation ("peanuts and hay"), establishing unambiguous file-level contracts, invariants, and side effects.
  6. **`egm-drift-report`**: Continuous drift detection comparing documented designs against actual code implementation to report discrepancies.
- **Execution Constraints & Personas**:
  - **Artifact-First Delivery (MANDATORY)**: Never dump large proposals, specifications, or audit summaries directly into the chat pane. Always write them to files or the harness artifact directory and link them cleanly in chat.
  - **Plain-English Standard & No Glazing**: Write all documents and communications in clear, direct, jargon-free plain English. Explain technical mechanisms plainly. Eliminate sycophancy (do not praise ideas as "great" or "brilliant"); lead with what is wrong, missing, or risky before affirming merits.
  - **Adversarial Goldfish Subagents**: Leverage the specialized review personas registered in the environment (`egm-design`, `egm-implementation-readiness`, `egm-codebase-details`, `egm-structural`, `egm-style`, `egm-comprehension`, `egm-zoom-out`, `egm-inventive-catalyst`, `egm-synthesizer`) to audit proposals and changes with fresh context.
  - **Stress-Testing by Default**: Actively find edge cases, failure modes, and counter-arguments before validating any solution.
- **Installation Roots on this Machine**:
  - Antigravity app/IDE: `C:\Users\karthiknk21\.gemini\config\skills\` & plugin `C:\Users\karthiknk21\.gemini\config\plugins\egm\`
  - Antigravity CLI: `C:\Users\karthiknk21\.gemini\antigravity-cli\skills\`
  - Claude Code: `C:\Users\karthiknk21\.claude\skills\` & marketplace plugin `C:\Users\karthiknk21\.claude\plugins\marketplaces\egm\`
  - Codex: `C:\Users\karthiknk21\.agents\skills\`
  - Each skill directory (`egm-bootstrap`, `egm-brainstorm`, `egm-draft`, `egm-drift-report`, `egm-resolve`, `egm-review`, `egm-review-loop`) contains its authoritative `SKILL.md` instructions and bundled resources. Always read and apply them on every run.

### Mandatory Graphify Knowledge-Graph Lookup (Every Run & All Tasks)

- On **every run**, before answering a codebase question or editing code, query the local Graphify knowledge graph of this repository. This applies to every agent: Claude Code, Codex and Google Antigravity (Gemini).
- **Local setup:** this uses the local `graphifyy` Python package only. There is no hosted service, account or network call.
  - **Install:** `uv tool install --system-certs --python 3.14 "graphifyy[mcp]"`. `--system-certs` is needed because this machine's HTTPS inspection breaks uv's built-in certificates. Python 3.14 is needed because 3.15 has no prebuilt `tree-sitter` packages and no C++ compiler is installed.
  - **Graph:** `graphify-out/graph.json`, which is git-ignored.
  - **Claude Code:** a local-scope stdio MCP server named `graphify` (`claude mcp add --transport stdio graphify -- C:/Users/karthiknk21/.local/bin/graphify-mcp.exe D:/Projects/frog_jumper/graphify-out/graph.json`).
  - **Codex:** a global stdio server named `graphify` in `~/.codex/config.toml`, same command and graph path (`codex mcp add graphify -- <exe> <graph.json>`).
  - **Google Antigravity:** the app/IDE and the `agy` CLI share `~/.gemini/config/mcp_config.json`, which has a `graphify` entry (`agy mcp add graphify <exe> <graph.json>`).
  - Check the entries with `claude mcp get graphify`, `codex mcp list` and `agy mcp list`. If MCP is unavailable, use the CLI below.
- **How to use it:**
  - **MCP tools:** `query_graph`, `get_node`, `get_neighbors`, `get_community`, `god_nodes`, `graph_stats` and `shortest_path`.
  - **CLI equivalents:** `graphify query "<question>"`, `graphify explain "<Name>"`, `graphify path "<A>" "<B>"` and `graphify affected "<Name>"`.
  - Use these to find callers, dependencies, the impact of a change, and paths between components.
  - Confirm graph results against the source files before relying on them. Cite file paths and line numbers, not only node names.
- **Keep it current:**
  - After editing code, run `graphify update .` from the project root. It re-extracts code with tree-sitter and **does not call an LLM**, and it takes about 2 minutes from scratch, less when cached. The MCP server reloads `graph.json` when it changes.
  - Do not run `/graphify`, `graphify label` or other LLM-backed commands without the owner's approval, because they send content to a model provider.
- **Coverage gaps:** the graph holds C#, Python, JS/JSX, Markdown, JSON and YAML. It has **no GDScript (`.gd`)**, no scenes (`.tscn`) and no assets, so use normal file search for those. When the graph is missing, stale or disagrees with the working tree, trust the files and say so in the reply. Never claim a Graphify result you did not get.
- Graphify supplements the EGM, game-development, Docker, testing and documentation rules above. It never authorizes commits, pushes or other outward-facing actions.

### Art Pipeline: SpriteCook Only (Blender Retired)

- **Blender is retired** (2026-11-11). Do not install or run Blender, and do not add `bpy` scripts, Blender add-ons, `.blend` files or Blender render steps. The Blender-made art, its generator scripts and the `blendot`, `blender_godot_pipeline` and `gltf_fix_blender_light_intensity` add-ons were deleted. The originals are in git history at `ca92679`. `addons/animation_combiner` (an FBX/GLB tool with Blender helper scripts) is still installed, pending an owner decision; do not use its Blender scripts.
- **All new or replacement game and website art comes from SpriteCook.** Use SpriteCook through each agent's own setup: the `spritecook@spritecook` plugin (server `/mcp/claude`) in Claude Code; an API-key server entry named `spritecook` (`https://api.spritecook.ai/mcp/`) in Antigravity's `~/.gemini/config/mcp_config.json`, with the skills in `~/.gemini/config/skills` and `~/.gemini/antigravity-cli/skills` (Antigravity has no plugin; `npx spritecook-mcp setup` writes to `~/.gemini/antigravity/`, which this install does not read, and also adds duplicates for other apps); and SpriteCook's official Codex plugin `spritecook@openai-curated-remote` (server `/mcp/openai`, signed in as a Codex app through `/plugins`) in Codex. Do not install the Claude plugin in Codex; its `/mcp/claude` server fails Codex sign-in. Read and apply the SpriteCook skills for all asset work, as set out in "Mandatory SpriteCook Skills" below.
- **Generation spends paid credits.** Get the owner's explicit approval before each generation batch, with the asset list and expected credit cost. The owner subscribed to the SpriteCook Starter plan on 2026-11-12 and is upgrading to the Adventurer plan (3,000 credits a month) for one month to make the whole list. Generate at 2K or 4K only, never 1K (owner, 2026-11-13), and follow each image's `method` and the phase budget in `docs/ART_REGENERATION.md` ("Methods", "Adventurer budget"; 2,170 planned credits against about 2,780 available after the owner's top-up; stop at 1.5x a phase's budget). Each agent must sign in to SpriteCook once (in Claude Code: `/mcp`, pick `spritecook`, Authenticate; in Codex: the app setup in `/plugins`) before its tools work; a "Failed to connect" before sign-in is expected.
- **What to regenerate:** `docs/ART_REGENERATION.md` lists every deleted image, with its path, pixel size, layer role, frame layout, pivot and the code that loads it. `docs/art_regeneration.json` holds the same sizes.
  - Save each new image at its listed path and exact size, so the game uses it with no code change.
  - `ArtRegenerationSpecTests` (NUnit) fails if a regenerated file has the wrong size.
  - Record each saved file's `asset_id` in `spritecook-assets.json` at the repository root (create it on first use).
- **Until art returns, the game leaves gaps on purpose:** any actor whose SpriteCook atlas is missing would be invisible (an empty placeholder in `OperativeVisuals`); the 2026-11-12 SpriteCook set was deleted on 2026-11-13 and Phase 1 (the six operatives, the drone and the boss) was redone the same day on the Adventurer plan, with Phases 2 and 3 (all nine cutscenes) and Phase 4 (the 25 mission props) done by 2026-11-14; UI, titles and icons are still to be regenerated. Also: sentries, open-world hostiles and Aegis civilians drawn in code, blank cities and menus, an empty title slot (a text title on the website), text-only buttons, a frameless speech bubble and default app and browser icons. Every other image (the hand-drawn studies, older logos, UI art, backgrounds and icons) was deleted on 2026-11-12; `docs/ART_REGENERATION.md` lists the 9 of those to remake. The art style is pixel art (owner, 2026-11-12). Do not add stand-in art to fill these gaps unless the owner asks.
- Check new art with `tools/assets/audit_images.py` and `tools/assets/compare_images.py`, which run on Pillow and numpy.
- This rule never authorizes commits, pushes or spending credits.

### Mandatory SpriteCook Skills (All Asset-Related Work)

- Whenever an agent does **any** work related to visual assets, it **MUST first read and apply the SpriteCook skills**. This applies to every agent: Claude Code, Codex and Google Antigravity.
- **What counts as asset work:** creating, regenerating, editing, animating, polishing, cleaning up, uploading, importing, slicing, organizing, budgeting or planning any game or website art. This includes sprites, characters, portraits, enemies, props, tilesets, maps, backgrounds, parallax layers, cutscene layers, menus, UI kits, icons, title logos and textures. It also includes reviewing art, choosing an art style, and writing art specs or prompts. It covers both requests that name SpriteCook and requests that do not.
- **Which skills to read:**
  - Always read `spritecook-workflow-essentials` first (credits, downloads, asset manifests, safe sign-in handling, defaults).
  - Then read every skill the task needs:

    | Task | Skill |
    |---|---|
    | Still images: sprites, characters, props, backgrounds, layers, icons | `spritecook-generate-sprites` |
    | Animations and character animation sets | `spritecook-animate-assets` |
    | Tilesets and autotiles | `spritecook-generate-tilesets` |
    | Using top-down 15-piece tilesets in a dual-grid renderer | `spritecook-use-dual-grid-tilesets` |
    | Menus, HUDs and matching UI component sets | `spritecook-build-ui-kits` |
    | Outline fixes, halos, edge debris and alpha cleanup | `spritecook-polish-sprites` |
    | Bringing a local image into SpriteCook (references, edits, animation) | `spritecook-upload-assets` |
    | Saved presets for a consistent style | `spritecook-use-presets` |
    | Putting SpriteCook output into Godot (SpriteFrames, AnimatedSprite2D, TileSet) | `spritecook-use-assets-in-godot` |
- **Where each agent finds them:**
  - Claude Code: the `spritecook@spritecook` plugin skills (named `spritecook:spritecook-...`).
  - Codex: the `spritecook@openai-curated-remote` plugin skills.
  - Antigravity: `~/.gemini/config/skills/spritecook-*` (app/IDE) and `~/.gemini/antigravity-cli/skills/spritecook-*` (CLI).
  - Use skill discovery first. If it does not show them, read the `SKILL.md` files directly from those locations. If they cannot be read, stop and resolve access before doing asset work; do not silently skip them.
- **How they fit with the other rules:**
  - Read them together with the game-development `create-game-assets` skill (art direction, asset manifest, quality gates) and with this project's Art Pipeline rule above (exact paths and sizes from `docs/ART_REGENERATION.md`, `spritecook-assets.json`, no stand-in art).
  - When a SpriteCook skill and this project's rules disagree, the project's rules win.
  - The skills never authorize spending credits: the owner's approval of each generation batch, with its asset list and expected cost, is still required. They also never authorize commits or pushes.

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
- The stack is defined in `docker-compose.yml`: `db` (`postgres:latest`, 5432), `backend` (Django, 8000), `website` (Nginx, 5173). All three are always-on members of `docker compose up`. The `cutscene`, `playwright` and `export` services only run under their own profiles. Back up saves manually with `tools/db/backup.ps1`.
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
>    docker compose exec -T backend python manage.py test accounts
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
- `CodeLevel.cs`: base class for code-built missions (`PalaceMission`, `ConcreteWildfireMission`, `NeonRustMission`, `ShatteredAegisMission`) and the later missions, with `PalacePlatform`, `AbilityGate` (operative-gated obstacles), `RecruitBeat` (unlock + in-level ability lesson) and `SquadStrip` HUD.
- `scenes/gameplay/level1.tscn` to `level12.tscn`: 12-mission linear 2D campaign (thin scenes that attach a `CodeLevel` subclass).
- `scripts/world/`: the open world. `NeoKyotoWorld.cs` keeps its original class name for save and scene stability. `WorldQuestRules.cs` lists the eight playable quests; `WorldEncounterController.cs` runs combat waves.

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
