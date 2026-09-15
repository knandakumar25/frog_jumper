# Axiom Force

A cyberpunk side-scroller with a 3D menu, built with **Godot 4.7.2 .NET**, **C#**, and **GDScript**. Battle through four levels, collect cherries, and race to the finish line while the fate of Earth hangs in the balance.

Design Spec #01 establishes the project identity and core infrastructure. Lead designer: **Claude Code**. Lead implementer: **GPT-6 Astra**.

---

## Story

Earth has fallen under a dystopian threat. A small team of warriors must fight their way through enemy territory to reclaim it. Choose your fighter, survive the gauntlet, and prove yourself worthy of saving the world.

---

## Features

- **4 Playable Characters** — Ninja Frog, Mask Dude, Pink Man, Virtual Guy, and a bonus unlockable Cyber Shogun
- **4 Levels** with increasing difficulty; levels unlock progressively as you complete each one
- **Training Protocol** — a practice level to learn the controls before diving in
- **Story Intro** — a typewriter-style narrative sequence that sets the scene
- **Collectables** — cherries scattered across levels, each worth points
- **Leaderboard** — session scores stored through PostgreSQL, with a local JSON fallback, and displayed on the main menu
- **Session System** — create multiple save sessions, continue where you left off
- **Pause Menu** — pause at any time with blur shader effect
- **Audio Options** — adjustable music and SFX volumes
- **50+ Language Support** — fully localised UI including Arabic, Hindi, Japanese, Malayalam, and many more
- **Custom Cursor** and animated cloud/character decorations on the main menu

---

## Controls

| Action | Keyboard | Gamepad |
|--------|----------|---------|
| Move Left | `A` / `←` | D-Pad Left / Left Stick |
| Move Right | `D` / `→` | D-Pad Right / Left Stick |
| Jump | `W` / `↑` / `Space` | A / Cross button |
| Sword Attack *(Cyber Shogun)* | Left Mouse Button | — |
| Karate Attack *(Cyber Shogun)* | `Q` | — |
| Pause | `Escape` | Start |

---

## Getting Started

### Requirements

- [Godot 4.7.2](https://godotengine.org/) with **.NET / C# support**
- [.NET 10 SDK](https://dotnet.microsoft.com/) for the `net10.0` target framework
- PostgreSQL for online session storage; `SessionManager` falls back to local JSON when the database is unavailable

### Running the Game

1. Clone or download this repository into `AxiomForce` (for example, `D:\Projects\AxiomForce`).
2. Open **Godot 4.7.2 .NET** and import `AxiomForce/project.godot`.
3. Let Godot finish importing the assets, then build the C# project and press **F5** to run.

To restore dependencies and build from a terminal in the project root:

```powershell
dotnet restore AxiomForce.csproj
dotnet build AxiomForce.csproj --no-restore
```

The startup scene is `res://scenes/main_menu/3d_menu_experience.tscn`. Database connection settings live in `scripts/managers/SessionManager.cs`.

> The game targets a 1920×1080 viewport and scales to fit any window size.

---

## Project Structure

```text
AxiomForce/
├── project.godot
├── AxiomForce.csproj
├── assets/
│   ├── models/hq/      50 GLB models, including Citadel and Cyber Shogun
│   ├── textures/       Reserved for future cyberpunk backgrounds and atlases
│   ├── images/         Existing backgrounds, sprites, and UI textures
│   ├── themes/         Shared resources, including cyber_glow.tres
│   ├── fonts/          Comic Neue, Noto Sans, and other font families
│   ├── music/          Background music and sound effects
│   ├── narrative/      Compiled Ink stories and translation tooling
│   └── translations/   CSV-based UI localisation
├── scenes/
│   ├── main_menu/      3D menu, story, selection, and related screens
│   └── gameplay/       Side-scroller levels, actors, and gameplay UI
├── scripts/
│   ├── managers/       Persistent autoloads, including GameManager.gd
│   ├── actors/         Player characters and enemies
│   ├── main_menu/      Menu and story presentation
│   ├── ui/             Reusable UI and loading controls
│   ├── gameplay/       Level logic, including the C# scoring GameManager
│   └── importers/      NeonMaterialImport.gd
├── tests/              Global state and infrastructure checks
└── addons/
    └── meshy-godot-plugin/
```

## Global State

`scripts/managers/GameManager.gd` is registered as the `GameManager` autoload. It persists across scene changes and exposes:

| Property | Default |
|----------|---------|
| `current_state` | `GameManager.State.MAIN_MENU` |
| `player_username` | `""` |
| `player_level` | `1` |

The `State` enum contains `MAIN_MENU`, `STORY_INTRO`, `GAMEPLAY`, `PAUSED`, and `GAME_OVER`.

```gdscript
func _ready() -> void:
    GameManager.state_changed.connect(_on_state_changed)
    GameManager.player_username = "CyberShogun"
    GameManager.set_game_state(GameManager.State.STORY_INTRO)

func _on_state_changed(new_state: GameManager.State) -> void:
    print("Current state: ", new_state)
```

`set_game_state()` updates `current_state` before emitting `state_changed(new_state)`. Repeating the current state emits no signal, and invalid enum values are rejected. The autoload remains available while the scene tree is paused. Scene loading and `get_tree().paused` remain the responsibility of the calling game flow.

The separate `scripts/gameplay/GameManager.cs` is the existing level-scoring node attached to gameplay scenes.

## Cyber Glow and Model Imports

`assets/themes/cyber_glow.tres` provides the shared Environment resource for WorldEnvironment nodes, including the 3D menu's SubViewport. It uses a dark background, **ACES** tonemapping, and **Additive** glow with an HDR threshold of **0.8**. The project uses the **Mobile** renderer.

All 50 GLBs live in `assets/models/hq/` alongside their 127 extracted texture maps. The source GLBs retain their embedded images, and Godot uses the extracted maps with their original compression and import settings. Their post-import script, `scripts/importers/NeonMaterialImport.gd`, enables per-pixel lighting and Schlick-GGX specular reflections with specular strength of at least `0.5`, while preserving existing metallic values and texture maps. This lets Citadel, Cyber Shogun, and the other models respond to neon scene lights. Let Godot finish reimporting after changing a model or its import settings.

---

## Validation

After building the C# project, run these commands with Godot 4.7.2 .NET available as `godot`:

```powershell
godot --headless --editor --path . --import
godot --headless --path . --script tests/test_game_manager.gd
godot --headless --path . --script tests/test_infrastructure.gd
```

The checks cover state transitions and signals, migrated scene loading, all 50 imported models, Citadel/Shogun texture preservation, and the 3D viewport environment. The state test deliberately passes two invalid states and expects warnings. Project autoloads start during both tests; session storage uses its JSON fallback if PostgreSQL is unavailable.

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Engine | Godot 4.7.2 .NET, Mobile renderer |
| Game Logic | C# (`net10.0`) |
| Scripting | GDScript |
| Database | PostgreSQL via Npgsql 10.0.3, with local JSON session fallback |
| Build | MSBuild / .NET SDK |

---

## Credits

- Character sprites from the **Pixel Adventure** asset pack
- Background art — Cyber City Dusk & Training Dojo
- Music generated with **MusicGPT** (*Dystopian Dawn*)
- Font — [Comic Neue](https://comicneue.com/) & [Noto Sans](https://fonts.google.com/noto)
- 3D asset workflow — Meshy Godot plugin

---

## Ink Narrative Integration

This project uses the official Ink C# runtime to power interactive narrative sequences.

### How to Use

1. **Compile your Ink stories to JSON**
   - Use [Inky](https://www.inklestudios.com/ink/) or the `inklecate` CLI to compile `.ink` files to `.json`.
   - Place the compiled JSON files in `assets/narrative/`.
   - Name them by locale, e.g. `english.json`, `japanese.json`.

2. **InkManager API**
   - The singleton `InkManager` loads the appropriate JSON file for the current locale.
   - **Methods:**
     - `InkManager.Instance.Continue()` — returns the next line of story text
     - `InkManager.Instance.CanContinue()` — returns `true` if more story text is available
     - `InkManager.Instance.GetChoiceCount()` — returns the number of current choices
     - `InkManager.Instance.GetChoiceText(index)` — returns the text for a given choice
     - `InkManager.Instance.ChooseChoiceIndex(index)` — selects a choice and advances the story

3. **Example Usage**
   ```csharp
   if (InkManager.Instance.CanContinue())
       GD.Print(InkManager.Instance.Continue());
   for (int i = 0; i < InkManager.Instance.GetChoiceCount(); i++)
       GD.Print($"Choice {i}: {InkManager.Instance.GetChoiceText(i)}");
   // To select a choice:
   InkManager.Instance.ChooseChoiceIndex(0); // choose the first option
   ```

> The Ink runtime expects compiled JSON, not raw `.ink` files.
