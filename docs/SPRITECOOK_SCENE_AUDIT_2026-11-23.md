# SpriteCook scene audit — 2026-11-23

The restored files are complete, but their use in the game is not complete.
The briefing portrait is disconnected, the world bike still draws a substitute,
and the Level 2 aftermath still draws a civilian from shapes. This audit changed
no production code or assets, spent no credits, and ran no exports.

## Confirmed gaps

| Priority | Finding and evidence | Next repair |
|---|---|---|
| High | Briefing AXIOM portrait is blank. `scenes/main_menu/story_intro.tscn:110` has an untextured `RobotPortrait`. `scripts/main_menu/StoryIntro.cs:36` looks for obsolete `RobotContainer/RobotSprite`. Runtime returned `story_robot_has_texture: false`; `story_portrait_blank.png` shows the gap. Scene lines 11–53 also retain six atlasless AtlasTextures. | Connect existing `assets/characters/rendered/axiom7_portrait.png` to the actual node; check animation dependencies before removing obsolete resources. |
| High | World bike remains procedural. `scripts/world/ExplorationPlayer.cs:73–87` tries `StandInAssets.BikeTexture`, then builds a cyan frame and two wheels. `scripts/gameplay/StandInAssets.cs:15` names absent retired `assets/stand_in/kenney_racing/motorcycle_blue.png`. V is reachable at `NeoKyotoWorld.cs:465–466`. Runtime confirmed riding; see `la_bike_fallback.png`. | Plan a SpriteCook bike batch and obtain credit approval. The controller-image exception does not cover the retired racing pack. |
| High | Level 2 aftermath adds a civilian drawn from shapes. `scripts/ui/WildfireAftermathStage.cs:91–94` draws a child's head, body and legs in shot 2. `wildfire_reunion_shot2.png` at 20 seconds shows this figure beside TITAN while the SpriteCook reunion image is also visible. | Check whether existing civilian/reunion imagery can supply this role without drawing a second figure. New generation requires approval. |
| Low | Dialogue avatar remains empty. `scenes/ui/CyberDialogueBubble.tscn:80` has a textureless `AvatarIcon`; `scripts/ui/CyberDialogueBubble.cs:64,159–164` retrieves and rotates it without assigning an image. Frame and pointer are connected at lines 62–63. | Bind an existing speaker portrait or remove the unused avatar area if intended. |

## Connected art and remaining mixed drawing

Active operative bodies, normal enemy animation resources and later boss plates
reference restored art. All eight character SpriteFrames resources resolve.
Mission 1's painting, Missions 2–5's props and Missions 6–12's environment families
are connected. Menu backdrop layers, character-selection portraits, button states,
dialogue frame and pointer also use restored images.

World module images are attached in `scripts/world/WorldChunkView.cs:32–36` and
landmarks in `scripts/world/NeoKyotoWorld.cs:820–821`. Six top-down operative sheets
are sliced into four columns by eight rows; hostiles use the vanguard resource.
Runtime texture inspection found the correct city-specific modules and landmarks
in Los Angeles, Kyoto, Mumbai, Honolulu, Dubai, Luanda and Beirut.

The following limit a claim that everything is replaced, but are not all missing files:

- Ground, water, roads and rail remain procedural (`WorldChunkView.cs:49–69`),
  as does the vector map (`WorldMap.cs:52–155`). Named residents are labeled
  diamonds (`NeoKyotoWorld.cs:797–813`), alongside station/terminal/safehouse markers.
  City screenshots visibly combine SpriteCook buildings with flat ground and roads.
- Some cinematic foreground architecture remains procedural. For example,
  `ApexBastionStage.cs:87–93,155,194` draws the restored environment followed by
  stairway/vault/battlement geometry. Apex Bastion and Judgment Day captures
  visibly contain SpriteCook art; these are mixed compositions, not blank scenes.
  Foreground overdraw warrants a shot-by-shot design review.
- `JudgmentDayStage.cs:102` and `JudgmentDayAftermathStage.cs:99` set
  `drawBase: false`, while retaining structures and lights. Gameplay uses all three
  layers. This is selective reuse of the generated plates.
- Dormant `IsometricPurgeDrone.cs:77–83` draws a procedural enemy body. Its
  construction is in `IsometricMission.cs:212,436`; no serialized scene or active
  production caller was found. Active Mission 1 uses `PalaceMission`.
- `ExplorationPlayer.cs:92–100` creates an untextured boat sprite, but player boat
  input is withheld (`NeoKyotoWorld.cs:465`).
- Legacy `scenes/gameplay/collectable.tscn:6` has an atlasless texture and
  `finish.tscn:12` an untextured Sprite2D. No current caller was found. Standalone
  startup errors require missing manager/audio context and do not demonstrate an
  active campaign asset failure.
- Act II/III interstitial buttons and operation-result panels retain native UI.
  Fonts, audio and uploaded profile images are outside authored-art replacement.
- The 124 Kenney controller images are the owner's explicit CC0 exception.
  Addon/vendor editor icons are not game art requiring regeneration.

## Inventory and checks

| Check | Result |
|---|---|
| Tracked game scenes | 61: 31 gameplay, 15 cutscenes, 10 main-menu, 4 UI, 1 world; reviewed with their code-built content |
| Other non-addon resources | 14; all 138 literal `res://` references across these resources and scenes exist |
| Tracked images | 2,232: 349 SpriteCook manifest files, 124 approved controller images, 1,759 addon/vendor images |
| Manifest integrity | 349/349 paths exist and match recorded SHA-12; no duplicate paths |
| Regeneration specification | All 342 required files exist; all 340 required PNGs have exact dimensions (the others are one ICO and one SVG); skipped files have not returned |
| Existing game imports | 323 manifested textures have matching source paths, existing compiled textures and matching source MD5s |
| World PNGs | 141: 45 in the world root and 16 per additional city; skipped Los Angeles module/landmark IDs are intentional |
| Pillow/numpy image audit | `tools/assets/audit_images.py` examined 473 images, with no per-image error records |
| Automated suites | Backend 182/182, NUnit 526/526, GdUnit4 408/408; runner exit 0, Godot reports zero failures, errors or orphans |
| Docker | PostgreSQL and Express healthy, website running; API health OK and website HTTP 200. Left running. |

Two ignored September screenshots remain locally in `assets/images/`:
`screenshot_home_2d.png` and `screenshot_main_menu_2d.png`. Neither is tracked or
referenced by inspected sources. They were left untouched. Export wildcard
inclusion was not tested.

Existing tests alone cannot prove replacement completeness:
`ArtRegenerationSpecTests.cs:75–81` permits missing files, `test_all_cities.gd:74`
permits zero images, and `audit_images.py:44–68` infers provenance from directories.
Independent manifest hashes, dimensions and source checks address those inventory gaps.

## Runtime evidence and limits

Local evidence is in ignored `.validation/spritecook-audit-2026-11-23/`: 85
screenshots, image-audit JSON, focused runtime JSON and `full-tests.log`.
These are local diagnostic artifacts, not committed game assets.

The broad preview captured 57 serialized scenes before an asynchronous transition
freed an inspected node. It did not produce its final runtime JSON and is not a
clean 61-scene runtime pass. Separate completed previews checked the remaining
dialogue/scorecard UI, all seven city definitions, all 16 Los Angeles region
centers, bike riding, briefing portrait and aftermath shot. A projectile removes
itself outside gameplay. Static coverage includes all 61 scenes; runtime sampling
does not prove every frame, world coordinate, animation or full playthrough.

The previews used a separate project identity/user-data folder and the in-memory
test store, with `AccountReady: false`. They did not sign in or modify account
saves. The owner's editor was left running. No production import refresh was
needed because production engine files and assets were unchanged.

The local Graphify CLI was queried and results checked against source. Godot AI
MCP was unavailable, so hidden Godot console processes and local diagnostic scripts
provided runtime checks. Three fresh reviewers independently checked campaign/
cutscenes, world/menus and provenance.

Graphical startup exposed a separate existing issue: `AuthManager.cs:55` defers
a callable returning `Task<bool>`, which Godot cannot convert to Variant. This
remains open despite passing headless suites. Signed-out settings warnings and
out-of-context helper errors are recorded in logs. Early restricted previews
also hit cache/certificate permissions; focused previews completed with the
required process access. Runtime sampling was not error-free.

SpriteCook balance was 604 credits; no generation was requested or charged.
No store/export settings changed, so the compliance guard was not triggered.
