# Axiom Force - Context & State Handoff Briefing
**Generated for Inter-Agent Handoff (Antigravity -> Claude Code)**  
**Date / Timestamp:** 2026-10-05  
**Current Git Commit:** `3a55ae9` (`main` branch, in sync with `origin/main`)  
**Working Tree:** Clean  
**Test Status:** 100% PASSED (Django, NUnit, GdUnit4 — 0 failures, 0 errors)

---

## 1. Executive Summary & Recent Changes

Over the recent development sessions, several major architectural, character design, animation, and visual effect overhauls were executed:

### A. Roster Consolidation & Character Identity
1. **Solaria Deprecation (`b89ea21`)**:
   - Solaria was completely removed across the entire codebase: C# models, character selection UI, progression managers, and unit tests (`CharacterProgressionTests.cs`, `test_character_selection_abilities.gd`, `test_character_upgrade_screen.gd`).
2. **Aura Phoenicia / Maya Khoury Integration (`81881e4`)**:
   - Integrated as the 6th distinct operative (`assets/models/hq/phoenicia.glb`).
   - **Identity**: Ultrasonic Prism Weaver with distinct Levantine aesthetics (royal purple & radiant gold anime cel-shading, Lebanese Cedar emblem, custom curved pauldron pauldrons, acoustic ripple abilities).
   - High speed (96/100) and agility (98/100).
3. **Current 6-Operative Roster**:
   - **Cyber Shogun (Kaito Kurogane)**: Cybernetic Shogun Warrior (pure martial, katana dash & parry).
   - **Oceanus (Kiana Vaea)**: Polynesian Tidecaller (fluid hydro torrents, vortex geysers, aquatic slam).
   - **Surya Prime (Arjun Varma)**: Solar Guardian (upright solar flare blast, cosmic aegis shield).
   - **Mirage Blade (Zayd Al-Mansoor)**: Crystalline Phantom (pure martial, twin phase daggers).
   - **Apex Titan (King Tariq Nzinga)**: Seismic Titan (overhead hammer slam, graviton magnetic pull).
   - **Aura Phoenicia (Maya Khoury)**: Ultrasonic Prism Weaver (harmonic pulse, prism reverberation).

---

### B. Ability Animations & Native Bone Retargeting (`3a55ae9`)
- **Root Cause Fixed**: Meshy GLB rigs have radically different rest bone orientations (e.g. Oceanus Hips tail in `+X`, Surya in `-X`, Apex in `+Z`, Phoenicia in `+Y`). Cross-rig rotation transfers previously caused inverted poses (Oceanus flipping upside-down, Surya leaning 45° backwards).
- **Solution (`tools/assets/rebuild_all_hero_animations.py`)**:
  - Rebuilt all 6 operative models using 100% native skeleton action slicing directly in Blender 5.2.
  - Sliced clean, grounded casting stances:
    - Oceanus: `mage_soell_cast_4` (grounded forward cast) and upright crouch-cast.
    - Surya Prime: `Charged_Ground_Slam` (upright, feet planted).
    - Apex Titan: `Heavy_Hammer_Swing` and `Unarmed_Jump`.
    - Phoenicia: `Beam_Cast` and `Supernova_Burst`.
- **Elimination of Stray Meshes**: Cleaned all bone `custom_shape` pointers to remove stray black `Icosphere` polygons from GLB exports (`use_selection=True`).

---

### C. Elemental & Procedural Ability VFX (`scripts/gameplay/HeroAbilityVFX.cs`)
1. **Elemental Operatives (Oceanus, Surya, Apex, Phoenicia)**:
   - Rich procedural particle and mesh effects triggered on ability preview:
     - Oceanus: Expanding aqua whirlpools, volumetric water ribbons, splash shockwaves.
     - Surya Prime: Roaring solar flame plume, orbiting corona embers, solar crest shield.
     - Apex Titan: Tumbling 3D rock fragments, radial seismic ruptures.
     - Aura Phoenicia: Expanding acoustic torus rings, prismatic sparkle showers.
   - Startup delay reduced to 0.04s for instant response; angled along a 3D arc (`forward * 0.65f + Right * 0.55f`) for optimal menu camera visibility.
2. **Pure Martial Operatives (Cyber Shogun & Mirage Blade)**:
   - **STRICT RULE**: Cyber Shogun and Mirage Blade must have **0 spawned particle effects**. Their tactical presentation is 100% grounded martial choreography (clean katana cuts and dual blade spins). `TriggerAbilityVFX()` returns `null` for both.

---

## 2. Key Architecture & File Map

| System / Component | Primary File(s) | Description |
| :--- | :--- | :--- |
| **Character Selection UI** | `scripts/main_menu/CharacterSelection.cs` | Coordinates 3D podium rotation, operative switching, ability loadouts, and animation triggers. |
| **Hero Ability VFX** | `scripts/gameplay/HeroAbilityVFX.cs` | Procedural particle/mesh VFX generator for elemental operative abilities. |
| **HQ 3D Operative Models** | `assets/models/hq/*.glb` | 6 operative models with native animations, cel-shaded materials, and weapons. |
| **Animation Rebuild Tool** | `tools/assets/rebuild_all_hero_animations.py` | Headless Blender Python script that rebuilds all hero actions and exports clean GLB models. |
| **VFX Preview Tool** | `tools/assets/screenshot_ability_vfx_preview.gd` | Godot test script for rendering and capturing high-res screenshots of hero ability previews. |
| **Universal Test Suite** | `run_tests.ps1` | Unified PowerShell runner executing Django, NUnit (.NET 10), and GdUnit4 (Godot 4). |

---

## 3. Mandatory Development Rules

1. **Mandatory Testing Protocol**:
   - At the conclusion of **EVERY** prompt or task, run:
     ```powershell
     .\run_tests.ps1
     ```
   - Must achieve **0 failures, 0 errors** across all 3 test suites (NUnit, GdUnit4, Django) before completing the turn.
2. **Vector Safety**:
   - Always verify `LengthSquared() > 0.001f` before calling `.Normalized()` on `Vector3` or `Vector2` to prevent runtime engine warnings.
3. **Pure Martial Operative Restriction**:
   - Cyber Shogun and Mirage Blade must remain free of magical/elemental particle spawns.
4. **Asset Import Protocol**:
   - When modifying 3D models or textures, launch Godot headless (`Godot_v4.7.2-stable_mono_win64.exe --headless`) to generate `.import` cache files.
5. **WSL Containers System (Zero Docker / Zero VM Distros)**:
   - This project strictly uses native Windows 11 WSL Containers (`C:\Program Files\WSL\container.exe`), NOT Docker Desktop and NOT full WSL Linux distros.
   - Scripts: `.\setup_wsl_containers.ps1`, `.\run_wsl_containers.ps1`, `.\stop_wsl_containers.ps1`, `.\export_game_wsl.ps1`.
   - OCI Containerfiles: `backend/Containerfile`, `website/Containerfile`, `export/Containerfile`.

---

## 4. Suggested Next Steps / Open Areas
- In-game level integration of operative abilities (currently verified in menu selection podium).
- Additional sound effects for ability activations via `UISoundManager`.
- Localization strings verification for the new operative loadouts.
- Spinning up the backend and database stack via `.\run_wsl_containers.ps1` when needed.
