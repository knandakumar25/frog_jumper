# Axiom Force

A 3D platformer built with **Godot 4.6** and **C#**. Battle through four levels, collect cherries, and race to the finish line as one of four unique characters — all while the fate of Earth hangs in the balance.

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
- **Leaderboard** — scores saved locally via SQLite and displayed on the main menu
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

- [Godot 4.6](https://godotengine.org/) with **.NET / C# support**
- [.NET 8 SDK](https://dotnet.microsoft.com/) or later

### Running the Game

1. Clone or download this repository.
2. Open **Godot 4.6 (C# edition)** and import the project (`project.godot`).
3. Press **F5** or click **Play** to run.

> The game targets a 1920×1080 viewport and scales to fit any window size.

---

## Project Structure

```
assets/
├── fonts/          Comic Neue & Noto Sans font families
├── images/         Character sprite sheets, backgrounds, UI graphics
│   ├── Ninja Frog/
│   ├── Mask Dude/
│   ├── Pink Man/
│   └── Virtual Guy/
├── music/          Background music and sound effects
├── scenes/         All game scenes (.tscn) and scripts (.cs / .gd)
└── translations/   CSV-based localisation for 50+ languages
addons/
└── godot-sqlite/   SQLite GDExtension for score & session persistence
```

---

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Engine | Godot 4.6 |
| Game Logic | C# (.NET 8) |
| Scripting | GDScript |
| Database | SQLite (via godot-sqlite GDExtension) |
| Build | MSBuild / .NET SDK |

---

## Credits

- Character sprites from the **Pixel Adventure** asset pack
- Background art — Cyber City Dusk & Training Dojo
- Music generated with **MusicGPT** (*Dystopian Dawn*)
- Font — [Comic Neue](https://comicneue.com/) & [Noto Sans](https://fonts.google.com/noto)
- SQLite plugin — [godot-sqlite](https://github.com/2shady4u/godot-sqlite)

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
