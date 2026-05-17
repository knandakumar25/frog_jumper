# Axiom Force: Reclaim Earth

A 2D side-scrolling platformer built with **Godot 4.6** and **C#**. Battle through four levels, collect cherries, and race to the finish line as one of four unique characters — all while the fate of Earth hangs in the balance.

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
