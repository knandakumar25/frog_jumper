# Axiom Force - Strategic Inter-Agent Handoff Briefing

> **Character artwork update (2026-10-20, supersedes the model-render approach):** Characters now use original flat 2D drawings authored in Python and Blender, with outlined planar shapes, solid colors and 2D joint animation. No prior game artwork or models are inputs. `tools/art/draw_flat_characters.py` and `package_character_sprites.py` generate the portraits and SpriteFrames under `assets/characters/rendered`. Do not restore the recovered GLBs, old model-render pipeline, rollback JPG cards or legacy samurai/robot sheets. See `tools/art/CHARACTER_RENDERING.md` for regeneration.

> **Current cleanup status (2026-10-19):** The game now starts at `main_menu.tscn`, uses the 2D operative cards and briefing, and deploys into `level1.tscn`. Scene background photos and project-owned 3D models, animation libraries, gameplay code, shaders, tooling, and web viewers have been deleted at the user's request. The historical 3D inventory and migration tracks below no longer describe files present in the workspace. Shared add-ons remain intact. `TitleLogo.cs` now owns localized title lookup; the website uses text/icon panels. See `tests/test_scene_cleanup.gd` for coverage of all retained scenes.

**Target Audience:** Incoming AI Agent (Claude, Codex, Antigravity)  
**Date / Timestamp:** 2026-10-19  
**Current Branch:** `main` (clean working tree, in sync with `origin/main` at commit `a4b0de2`)  
**Test Suite Status:** 100% PASSED (NUnit: 23/23, GdUnit4: 12/12 suites, 59/59 tests, 0 failures, 0 errors, 0 orphans)

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
