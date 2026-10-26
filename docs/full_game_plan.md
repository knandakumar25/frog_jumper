# Full Development Plan for Axiom Force (Entire Game)

> [!NOTE]
> **Active 2D Platformer Transition (Current Canon)**:
> Axiom Force has officially transitioned from the legacy 3D prototype described in sections below to a **high-fidelity 2D Cyber-Action platformer** (inspired by *Katana Zero*, *Hades*, and *Ghostrunner*).
> - **Active Architecture**: `CodeLevel.cs` base class with procedural geometry, painted/procedural parallax backdrops, dynamic combat HUD, multi-operative synergy (`OperativeRegistry`), and the 4-mission linear campaign (`level1.tscn` to `level4.tscn`).
> - **Operatives**: 6 distinct operatives unlocked progressively across the campaign (Cyber Shogun, Oceanus, Apex Titan, Aura Phoenicia, Mirage Blade, Surya Prime).
> - **Boss Encounter**: "Vanguard-X" / Border Commander Voss in Level 4 with multi-phase rage mechanics.
> - The sections below are retained as historical architectural reference for legacy systems.

**Purpose**: Provide a single, detailed source of truth for the work required to bring the full Axiom Force game to its final state, covering asset import, core gameplay, UI, audio, localization, performance, testing, and release.

---

## 1. Project Overview & Goals
- Transform the main menu into a high‑fidelity 3‑D "Cyber Command Center" while keeping the title and buttons as 2‑D UI elements.
- Implement the "Neon Pulse" visual pass across the entire game (menus, HUD, in‑game UI, and world environments).
- Ensure all core gameplay loops (player movement, abilities, enemy AI, combat, health, sessions) are fully functional, well‑tested, and performant on target platforms.

---

## 2. Asset Import Workflow (Applicable to All New/Modified Assets)
1. **Run Godot head‑less** to generate import files:
   ```
   "C:\Users\karthiknk21\Desktop\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe" --headless
   ```
2. **Wait** until corresponding `.import` files appear for each asset (GLB, textures, fonts, shaders, etc.).
3. **Stop** the Godot process.
4. **Commit** the generated `.import` files alongside the source assets.
5. **Validate** by launching the game (or relevant scene) and confirming the assets appear correctly.

---

## 3. Build & Test Protocol (Mandatory after Every Change)
- **Build**: `dotnet build AxiomForce.csproj`
- **Run Unit Tests**:
  - **NUnit**: `dotnet test tests/AxiomForce.Tests/AxiomForce.Tests.csproj`
  - **GdUnit4**: 
    ```
    "C:\Users\karthiknk21\Desktop\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe" \
      --headless --path . -s addons/gdUnit4/bin/GdUnitCmdTool.gd \
      --add res://tests/ --ignoreHeadlessMode
    ```
- **All test suites must pass** (0 failures, 0 errors) before closing the prompt.

---

## 4. Coding Guidelines (Re‑affirmed)
- **Naming**: PascalCase for public members, `_camelCase` for private fields.
- **Godot API**: Use `GetNodeOrNull<T>()` for safe node retrieval; use `Callable.From()` for signal connections.
- **Safety**: Always check `LengthSquared() > 0` before `.Normalized()` on vectors.
- **SubViewport UI**: Ensure a `DirectionalLight3D` and a `WorldEnvironment` exist inside each viewport to guarantee visibility.

---

## 5. Core Components & Their Target States
| Component | Current Status | Target Completion | Key Tasks |
|-----------|----------------|-------------------|-----------|
| **MainMenu.cs** | Fixed compile errors; title & buttons remain 2‑D | Fully functional UI, transitions to 3‑D background | Add camera zoom, integrate 3‑D background, ensure 2‑D title/buttons stay visible |
| **CyberButton.cs** | Size and visibility fixed; still 3‑D‑backed but UI remains 2‑D | Consistent button behavior, parallax, hover/press animations | Verify material/emission settings, finalize hover/press tweens |
| **ThreeDMenuExperience.cs** | Handles 3‑D background, camera, ambient objects | Complete cinematic intro, integrate holographic shader, manage ambient vehicle animations | Add missing asset references, polish camera easing |
| **Player3D.cs / MainCharacter.cs** | Core movement and animation present | Fully polished character controller, state machine, ability integration | Refactor to use `LengthSquared()` safety, add unit tests for movement |
| **Enemy** scripts (`Enemy3D.cs`, `Vanguard`, `Stalker`, etc.) | Basic AI present | Robust AI behaviours, health/damage, loot drops | Implement AI state machine, optimize pathfinding, add tests |
| **Ability** system (`Ability.cs`, `OperativeAbility.cs`, etc.) | Ability framework exists | Fully implemented abilities (dash, heal, etc.) | Add cooldown logic, UI feedback, unit tests |
| **GameManager** | Handles game state transitions | Stable game flow (main menu ↔ gameplay ↔ pause ↔ game over) | Ensure all state transitions are covered in tests |
| **SessionManager** | Persists sessions via JSON | Full save/load system for player progress | Add encryption or versioning if needed |
| **UISoundManager** | Plays UI SFX | Consistent audio cues for all UI actions | Verify all UI elements call the manager, add missing sounds |
| **MenuBackgroundManager** | Renders background 3‑D environment | Dynamic background with ambient animations | Fine‑tune lighting, add optional weather effects |
| **MenuSettingsManager, MenuInputManager, MenuAuthUI, MenuSessionUI** | Functional but may lack polish | Complete UI/UX, localisation, key‑rebinding, auth flow | Add localisation keys, error handling, visual polish |
| **Audio** (`audio_options.gd`, etc.) | Basic music/sfx control | Full audio mixing (music, SFX, voice) with dynamic volume |
| **Localization** (`TranslationServer`) | `Tr` helper in place | All UI strings localized for supported languages |
| **Shaders** (`holographic.gdshader`) | Added, needs tweaking | Final visual effect for holograms, terminals, UI glows |
| **HUD / In‑Game UI** (`GameOverUI.cs`, `PlayerHealthUI.cs`, `MissionSummaryUI.cs`, etc.) | Basic UI elements | Consistent UI style matching Neon Pulse theme |
| **Debug / Diagnostics** | Debug prints added | Clean up debug output, integrate proper logger |

---

## 6. UI / Visuals (Beyond the Main Menu)
1. **HUD**: Design a neon‑styled HUD (health bar, ammo, ability cooldowns). Use `Label3D` or `Control` nodes with custom fonts (Orbitron). Ensure all elements are anchored correctly and scale with screen size.
2. **In‑Game Menus**: Pause menu (`pause_menu.gd`) must use the same visual style; verify that the menu appears correctly when the game is paused (check `process_mode = Node.PROCESS_MODE_ALWAYS`).
3. **Dialogue UI**: `DialogueUI.cs` to adopt Neon Pulse colours and fonts.
4. **Mission Summary / Game Over**: Apply consistent theming, add transition animations.
5. **Localization**: Replace hard‑coded strings with `Tr()` calls; populate translation CSVs for each supported language.

---

## 7. Audio & Sound Design
- Integrate `audio_options.gd` with UI sliders that affect `UISoundManager` volumes.
- Add ambient tracks for each game zone (e.g., city canyon, combat arenas).
- Ensure all UI actions (button hover, press, menu navigation) trigger appropriate SFX via `UISoundManager`.
- Verify audio mixing works on both desktop and potential mobile builds.

---

## 8. Gameplay Mechanics (Core Loop)
1. **Player Movement**: Verify physics, acceleration, and deceleration; add wall‑run or double‑jump if desired.
2. **Abilities**: Implement ability activation, cooldown UI, and visual feedback (shader effects, particle systems).
3. **Combat**: Ensure enemy AI can detect and chase the player, attack, and take damage.
4. **Health System**: Consistent health/shield values; clamp health correctly (tests already cover this).
5. **Session Persistence**: Save player progress, unlocked abilities, and settings between runs.
6. **Level Design**: Load level scenes (`prototype_3d.tscn`, etc.) with proper lighting and post‑processing.
7. **Performance**: Profile FPS; optimise heavy SubViewports or large meshes; enable `godot` profiling tools.

---

## 9. Performance & Optimization
- Use **Godot’s built‑in profiler** to identify bottlenecks (render, physics, script).
- Optimize shaders (e.g., `holographic.gdshader`) by limiting texture reads.
- Reduce draw calls by merging static meshes where possible.
- Use **LOD** for distant assets in the 3‑D background.
- Enable **culling** for invisible objects in the menu scene.

---

## 10. Testing Strategy
| Layer | Type | Scope |
|------|------|-------|
| **Unit Tests** | NUnit (C#) | Core logic (player, abilities, health, session manager). |
| **Integration Tests** | GdUnit4 (GDScript) | Scene loading, UI flow, pause/resume, audio playback. |
| **Manual Playtests** | QA | Visual consistency, performance on target hardware, user experience. |
| **Automated UI Tests** (future) | Godot UI automation or external tool | Verify button navigation, localisation, and sound triggers. |

All new code must include appropriate unit tests; existing tests must remain green.

---

## 11. Documentation & Knowledge Transfer
- **README / Asset Import Guide** (`README_Asset_Import.md`) – keep up‑to‑date with any new asset pipelines.
- **Developer Wiki** – add sections for each subsystem (Gameplay, UI, Audio, Localization, Shaders).
- **Code Comments** – follow the existing comment style; add `///` XML docs for public APIs.
- **Change Log** – maintain a `CHANGELOG.md` for major milestones.

---

## 12. Release & Deployment
1. **Pre‑Release QA**: Run full test suite, perform performance profiling on target hardware.
2. **Build Packages**: Use `dotnet publish` for the C# binaries; export Godot project for Windows, macOS, Linux.
3. **Versioning**: Follow Semantic Versioning (`MAJOR.MINOR.PATCH`). Update `ProjectSettings` version number.
4. **Distribution**: Upload builds to the chosen platform (Steam, itch.io, internal server).
5. **Post‑Release Monitoring**: Collect crash reports, telemetry (if any), and player feedback.

---

## 13. Responsibilities & Timeline (High‑Level)
| Phase | Owner | Duration | Deliverables |
|-------|-------|----------|--------------|
| **Asset Import & Environment Setup** | Antigravity | 2 days | All GLB assets imported, `3d_menu_experience.tscn` finalized, holographic shader tuned. |
| **UI Polish** | UI Lead | 3 days | Menu UI, HUD, pause menu, dialogues fully themed and localized. |
| **Gameplay Core** | Gameplay Team | 5 days | Player controller, abilities, enemy AI, health system stable and tested. |
| **Audio Integration** | Audio Engineer | 2 days | All music/SFX linked, volume sliders functional, audio tests passing. |
| **Performance Optimisation** | Optimisation Engineer | 2 days | FPS target met, profiling reports, LOD/culled assets. |
| **Testing & QA** | QA Team | 3 days | Full test suite green, manual playtest sign‑off, bug list cleared. |
| **Release Prep** | Release Manager | 1 day | Build packages, version bump, release notes. |

---

## 14. Immediate Next Steps for Antigravity
1. **Finalize the 3‑D menu environment**
   - Place and align the Holo projector, terminal kiosk, and Cyber Shogun models.
   - Apply and tweak the `holographic.gdshader` on the projector.
   - Verify the scene runs without missing resources (run head‑less). 
2. **Commit any new GLB assets** and their `.import` files.
3. **Run the full test suite** to ensure no regressions.
4. **Document any remaining open tasks** in `docs/full_game_plan.md` (this file).

---

*All automated test suites have been executed after the latest changes and report **0 failures, 0 errors**.*
