# Free stand-in assets

> **Removed 2026-11-11.** At the owner's request, all four packs that were in the game (Kenney Racing, Kenney Sci-fi Sounds, Foozle Mecha Boss, Hormelz robot) were deleted along with `assets/stand_in/`. The Pixel Adventure leftovers in `assets/images` (`Blue`, `Green`, `Cherries`, `End (Idle)`, `Terrain (16x16)`) went too. `StandInAssets.cs` returns null for each missing file, so the bike, sentries and open-world drones are drawn in code and their sounds are silent. Art direction is now SpriteCook only, so don't bring these packs back unless the owner asks; this file is kept as a record of the sources and licenses.

Temporary art and audio candidates, checked 2026-11-09. Four packs were in the game until 2026-11-11; they are listed in the record table at the end of this file. The rest are candidates only.

## What to know first

- **Style will not match.** The game's characters and backgrounds are high-resolution renders and paintings. Almost every free pack that fits this setting is small pixel art. The stand-ins will look different until they are replaced.
- **The game needs few stand-ins.** Characters, mission backgrounds, city art and speech already exist. The remaining gaps are music and things still drawn in code, such as platforms, which were left that way on purpose.
- **Licenses can change.** Save a copy of the license page on the day a pack is downloaded, and add the pack to the record table at the end of this file.
- **"Free" is not a license.** Packs marked "No license stated" stay off the recommended list until the creator confirms the terms.

## Recommended

All of these are CC0 (public domain, with no credit required), confirmed on the creator's own page. CC0 packs are also safe if this repository becomes public.

| Need | Pack | Contents | Replaces |
|---|---|---|---|
| Side-scrolling world | [Warped City](https://ansimuz.itch.io/warped-city), ansimuz | 16×16 tileset, 3-layer parallax background, hero with 10 animations, 2 enemies, 4 vehicles, props, effects, and a Godot 4 project | `PalacePlatform`, `SkylineBackdrop` |
| Ground enemies | [Sci-fi Lab Droids](https://foozlecc.itch.io/sci-fi-lab-droids), Foozle | 3 droids, 6 animations each, 32×32 or 48×48 | Not used; available for future ground enemies |
| Boss and exploding drone | [Sci-fi Lab Mecha Boss](https://foozlecc.itch.io/sci-fi-lab-mecha-boss), Foozle | Boss with 10 animations, drone with idle, run, hurt and explode | `PalaceSentry` (added); the boss is unused |
| Top-down enemies | [8 Directional Drone Robot](https://hormelz.itch.io/8-directional-drone-robot), hormelz | Free file: idle, walk and shoot in 8 directions, 3 colors, 256×256 canvas. Other animations cost $2 | `WorldHostile` |
| Vehicles | [Racing Pack](https://www.kenney.nl/assets/racing-pack), Kenney | Top-down vehicle tiles, including motorcycles | Electric bike (added) |
| Menus and HUD | [UI Pack: Sci-Fi](https://www.kenney.nl/assets/ui-pack-sci-fi), Kenney | 130 files: buttons, panels, sliders | Only if a screen needs it |
| Sound effects | [Sci-Fi Sounds](https://www.kenney.nl/assets/sci-fi-sounds), Kenney | 70 files, including engines, explosions and lasers | Extra combat and machine sounds |
| Weapon sounds | [Sci-Fi Weapon Shots](https://lentikula.itch.io/sci-fi-weapon-shots-sfx-freecc0), Lentikula | 15 WAV files, 48 kHz/24-bit: rifle, pistol, beam | Sentry and drone fire |

Kenney's [support page](https://www.kenney.nl/support) confirms commercial use without credit. It reserves the Kenney logo, so don't use that.

Warped City's page says "Name your own price" and also shows a $9 tier. Check whether the base files download at $0. Its art is CC0, but search results say the music needs a credit, so leave the music out unless it is credited.

## Usable with limits

| Pack | Why it is useful | Limit |
|---|---|---|
| [Cyberpunk City Exterior](https://flambo19.itch.io/cyberpunk-city-exterior), Flambo19 | The closest top-down city set: 32×32 floors, walls, roofs, 50+ props, 2 walking characters | Allowed in commercial projects. **Must not be redistributed, even modified, or used for AI training.** Keep it out of any public repository |
| [Free Cyberpunk Loop Pack](https://whitebataudio.itch.io/free-cyberpunk-loop-pack), whitebataudio | 3 dark synthwave WAV loops, the closest music match | Says "100% royalty free" but names no license. Fine for private testing; get written terms before release |
| CraftPix free packs | Many enemy packs, and the [license](https://craftpix.net/file-licenses/) allows free assets in commercial games without credit | No selling source files; AI training is barred. The itch.io copies state no license, so download from CraftPix |

## Not recommended

- [Free Drones Pack](https://free-game-assets.itch.io/free-drones-pack-pixel-art) on itch.io: no license on that page. Use CraftPix's copy instead.
- [Workers and Drones](https://free-game-assets.itch.io/workers-and-drones-pixel-art-character-pack) and [Flying Robot](https://free-game-assets.itch.io/flying-robot-2d-sprites): $0.70 minimum, and no license on the page.
- [Warped City 2](https://ansimuz.itch.io/warped-city-2): no license on the itch.io page. Search results list it as CC0 on OpenGameArt, but that page could not be opened.

## Not fully checked

OpenGameArt blocked automated reading, so these were seen only in search results. Open each page and confirm the license by hand:

- [60 CC0 Sci-Fi SFX](https://opengameart.org/content/60-cc0-sci-fi-sfx)
- [Dark Sci-Fi Audio Pack](https://opengameart.org/content/dark-sci-fi-audio-pack): 5 atmospheric loops, listed as CC0

itch.io's [free cyberpunk tileset listing](https://itch.io/game-assets/free/tag-cyberpunk/tag-tileset) also blocked automated reading. Neon District FREE, Nasha's Neon City tiles and similar packs appeared there, but their licenses are unconfirmed.

## Record for each pack added

| Path in project | Source URL | Creator | Date | License | Credit needed | Edits | Replace by |
|---|---|---|---|---|---|---|---|
| `assets/stand_in/kenney_racing/` | https://www.kenney.nl/assets/racing-pack | Kenney | 2026-11-09 | CC0 (`License.txt` in folder) | No | None; one PNG copied | Original bike art |
| `assets/stand_in/kenney_scifi_sounds/` | https://www.kenney.nl/assets/sci-fi-sounds | Kenney | 2026-11-09 | CC0 (`License.txt` in folder) | No | None; five OGG files copied | Original sound design |
| `assets/stand_in/foozle_mecha_boss/` | https://foozlecc.itch.io/sci-fi-lab-mecha-boss | Foozle (commissioned from aimen23b) | 2026-11-09 | CC0 (`License.txt` in folder) | No | Renamed; one PNG copied | Original `PalaceSentry` art |
| `assets/stand_in/hormelz_robot/` | https://hormelz.itch.io/8-directional-drone-robot | hormelz | 2026-11-09 | CC0, per the page (`License.txt` records it; the zip has no license file) | No | Renamed; red variant only, 24 PNGs | Original `WorldHostile` art |

`WildfireGunner` already has generated art and needs no stand-in. The robot is a humanoid gunner, although the code still calls `WorldHostile` a drone.

When removing `assets/stand_in/`, also delete `tests/test_stand_in_assets.gd` and its `.uid` file, because the tests expect those assets.

Known small issues, left as they are: a dodged open-world strike still plays the hit sound, a killing strike is silent, and an explosion can overlap the start of an epilogue. A rider killed while mounted respawns still riding, with the engine running; that dismount was already missing before this work.

Keep stand-ins in `assets/stand_in/<pack>/` with the saved license, so they are easy to find and remove.
