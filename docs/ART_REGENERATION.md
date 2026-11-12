# Art regeneration list (SpriteCook)

Blender was retired on 2026-11-11 and every image it made was deleted. On 2026-11-12 every remaining image was deleted too: the hand-drawn operative studies, older logos, UI art, backgrounds and icons (see "Deleted on 2026-11-12"). This file is the work order for remaking them with SpriteCook. The Blender originals are in git at commit `ca92679`; the 2026-11-12 originals are at `67eab0a`.

## Rules

**Approved UI batch update (2026-11-12):** the button states, dialogue frame/pointer,
application icon, city menu base, and 17 localized title plates with website copies
have been restored. The tables below retain the original deletion work order;
check files and `spritecook-assets.json` for delivered assets. This batch reused
existing SpriteCook output; Codex spent no additional credits. The Starter plan
allows 1K source generation; older 2K/4K recommendations below require a different
plan and are not authorization to spend credits. Other art remains pending.

- **Same path, same size.** Save each new image at its listed path and exact pixel size, and the game loads it with no code change. Some screens appear only once their whole set exists (a cutscene's stage, a later-campaign family, a city); see each phase's shared rules. `ArtRegenerationSpecTests` (NUnit) fails on a wrong size; `docs/art_regeneration.json` holds the sizes and statuses.
- **Status column.**
  - `remake`: generate it.
  - `copy of …`: copy the named file once it exists; do not generate it separately.
  - `skip: unused`: nothing loads or draws it; do not spend credits on it.
  - `skip: unused alias`: an extra file name for a title that no code loads; do not generate or copy it.
- **Credits.** SpriteCook spends paid credits. Before each phase, call `get_credit_balance` and `list_generation_models` for current costs, then get the owner's approval with the asset list and expected cost.
- **Asset IDs.** Record each saved file's `asset_id` and `sha12` in `spritecook-assets.json` at the repository root (create it on first use), as `spritecook-workflow-essentials` describes.
- **Old images as references (required).**
  - Restore each original to a scratch folder before writing its prompt: `git archive ca92679 <path> | tar -x -C <scratch>`.
    - This works in any shell. `git show ...:<path> > file` also works in Git Bash and PowerShell 7.4+, but Windows PowerShell 5.1 corrupts the binary output.
  - The old image shows the content, composition and layout the code expects. Where a section lists fixed positions, the new image must keep the old layout.
  - Upload the old image with `spritecook-upload-assets`, then pass it to the generation call:
    - for a new image, as `reference_asset_id`;
    - for a layer made as an edit of a new base (`edit_asset_id`), in `style_asset_ids`. SpriteCook accepts `style_asset_ids` with either, but not `reference_asset_id` together with `edit_asset_id`.
  - The "Old sha256" column is the first 12 hex characters of the deleted file's SHA-256, for checking a restored copy.
- **Also deleted files:** do not recreate the old `manifest.json` or `provenance.json` files; no code reads them. Character `.tres` files are restored from git, not generated (Phase 1).
- **Settings and cost.** SpriteCook offers only the 1:1, 16:9 and 9:16 aspect ratios, and 1K, 2K or 4K source resolution. Nano Banana 2.1 costs 12, 18 or 30 credits at those resolutions at release; confirm with `list_generation_models`. Default plan:
  - 1920x1080 layers: 16:9 at 2K.
  - 512x512 and 384x384 sprites and portraits: 1:1 or 9:16 at 1K.
  - The 4608x1536 map and the 1920x588 titles: 16:9 at 4K, cropped to the band.
  - Pose and walk sheets: the character workflow (Phase 1).
  - Estimate a phase's cost as images x per-image cost x 1.5 for retries, and put that figure in the approval request.

## Open owner decisions (block all generation)

1. **Art style:** decided 2026-11-12, **pixel art** for the whole set.
2. **SpriteCook sign-in.** The owner subscribed to the Starter plan on 2026-11-12. Each agent still has to sign in once before the tools connect.
3. Approval of each phase's asset list and credit cost.

Already decided: titles keep the wording of the old title images (owner, 2026-11-10).

## Until the art returns

Gaps are intentional (owner decision):
- **Visible:** all six operatives, the `vanguard` drone and the `heavy` boss (Phase 1 done 2026-11-12).
- **Drawn in code:** sentries, open-world hostiles and Aegis civilians.
- Cities, menus and the Level 1 map have no art. Level 1 uses its procedural skyline instead of the painted garden.
- Buttons show text only, the AXIOM-7 speech bubble has no frame, and the game window and browser tab use default icons.
- Cutscenes skip.
- The title slot is empty in the game; the website shows the words "AXIOM FORCE".

## Work order

Phases run in this order because later images reuse earlier ones (see each section's "Needs first" line). Counts are images to generate.

| Phase | Groups | Images to generate |
|---|---|---|
| 1. Characters | Operatives, AXIOM-7, enemies | 15 |
| 2. Deployment cutscene | City, rooftop, drone layers and Shogun poses, reused by six later cutscenes | 8 |
| 3. Other cutscenes, in story order | Concrete Wildfire arrival, Tokyo Remembers, Concrete Wildfire aftermath, Neon Rust intro (Silent Freight), Neon Rust aftermath, Shattered Aegis intro, Names without Numbers epilogue, Act II opening | 50 |
| 4. Mission props | Concrete Wildfire props, Neon Rust props, Shattered Aegis props, Names without Numbers props | 25 |
| 5. Later-campaign environments | 7 families | 53 |
| 6. City kits | Los Angeles first, then 6 cities | 141 |
| 7. Menus, Level 1 map, titles | Menu backdrops, isometric map, 17 language titles | 24 |

Totals: 316 to generate, 17 copies, 89 skipped (of 422 deleted images), plus 9 to remake and 106 skipped from the 115 deleted on 2026-11-12. All 537 are in `docs/art_regeneration.json`.

## Fitting SpriteCook output to the exact size

SpriteCook returns its own sizes: it trims margins (`smart_crop_mode`), may add padding, and gives one sheet per animation. Normalize every image with Pillow before saving it into the game. Keep the step in a script, not by hand.
- **Scenes and layers** (1920x1080, 4608x1536, 1920x588 and similar): crop to the target aspect ratio, then resize to the exact size.
  - Base plates must be fully opaque.
  - Structure, lights and sprite layers must have real transparency.
  - The layers of one scene must line up. Generate structure and lights as edits of the base (`edit_asset_id`), then check them stacked.
- **Sprite sheets:** resize each frame to fit its cell, place it so the feet sit on the cell's pivot (the baseline, listed per group), and paste the frames into one sheet in the listed row and column order.
  - Leave transparent pixels along every edge of each cell and each prop. The validators fail if an opaque pixel touches a cell or canvas edge.
  - SpriteCook can return one frame more or fewer than `output_frames`. Read `frame_count` from the result; if it is not 8, regenerate, or drop or repeat a middle frame so the timing stays even.
- **Mission props:** the game stretches each 512x512 prop to its display size (Phase 4 lists it). Draw the prop at its display aspect, then resize it non-uniformly into 512x512, so it looks right once stretched back.
- **Check:** run the checks below on the normalized file, not on the raw download.

## Acceptance checks

1. `dotnet test tests/AxiomForce.Tests/AxiomForce.Tests.csproj`: `ArtRegenerationSpecTests` checks sizes.
2. `uv run --system-certs --no-project --with pillow --with numpy python tools/assets/audit_images.py --out .validation/asset_quality/images.json`: checks transparency share, edge softness and padding.
3. Compare against the old images restored from git: `uv run --system-certs --no-project --with pillow --with numpy python tools/assets/compare_images.py --before <scratch with old PNGs> --after <folder with new PNGs> --out .validation/compare/<group>.png`. It pairs files by name and fails on a size change.
4. Cutscene validators: `docker compose --profile cutscene run --rm cutscene tools/cutscene/validate_<name>.py`.
   - They check size, alpha, pose cells and margins, and print `SKIPPED` for images not done yet.
   - Every layer except a base needs some fully transparent and some fully opaque pixels (alpha 0 and 255).
   - Script for each cutscene:
     - Tokyo: `validate_tokyo.py`
     - Concrete Wildfire arrival: `validate_wildfire.py`
     - Concrete Wildfire aftermath: `validate_breach.py`
     - Neon Rust intro: `validate_neon_rust.py`
     - Neon Rust aftermath: `validate_city_voice.py`
     - Shattered Aegis intro: `validate_inward_wall.py`
     - Names without Numbers: `validate_names_without_numbers.py`
     - Act II: `validate_unregistered.py`
   - The deployment cutscene has no validator; use checks 1, 2 and 7.
5. Background Godot import: each new PNG gets its `.import` file and no errors.
6. `.\run_tests.ps1`: GdUnit checks the character `.tres` layout, menu layers, city art (all 16 files or none per city) and cutscene playback, which runs once a stage's art is complete.
7. Look at it in the game: the menus, Level 1, one cutscene of each kind and one city.
8. **Titles:** compare each title's text with the old image by eye. Have a reader of Arabic, Hebrew, Thai, Japanese, Korean or Chinese check those titles. If SpriteCook cannot draw a script correctly, generate the plate without text and add the text with Pillow, using the font that `LocaleCatalog` lists for that language.

## Phase 1: Characters

**Progress (2026-11-12):**
- **Lineup:** all nine designs were made as one 3x3 pixel-art sheet (12 credits) and split into separate base stills (free). The asset IDs are in `spritecook-assets.json`.
- **Cyber Shogun is done:**
  - `cyber_shogun_atlas.png`, packed by `tools/art/pack_spritecook_character.py` at a 3x nearest scale;
  - `cyber_shogun_portrait.png`, the base still enlarged;
  - `cyber_shogun.tres`, restored from `ca92679`.
  - Credits: one animation run of 102, plus a 20-credit run redo.
- **Phase 1 complete (2026-11-12):**
  - Atlases, portraits and restored `.tres` for all six operatives, plus `vanguard` and `heavy` idle atlases and `axiom7_portrait.png` (the AXIOM-7 base still, enlarged, free).
  - Each operative was one run of six custom animations: 90 credits, plus 12 for the run-pose prep. Each enemy idle was 20.
  - Phase 1 total: **696 credits**:
    - lineup 12;
    - Shogun 102 + 20;
    - the other five 510;
    - enemies 40.
- **Lesson:** preset animations write their own prompt from the source asset's stored prompt. Slices of the lineup sheet inherit the nine-character prompt, which put other operatives into Shogun's first run. Use `custom_animations` with explicit single-character prompts for every animation, or upload a clean crop first.

**Shows:** six operatives with distinct silhouettes: Cyber Shogun (samurai, katana), Surya Prime (solar shield bearer), Oceanus (water lancer), Mirage Blade (hooded dual-blade scout), Apex Titan (heavy gauntlet fighter) and Aura Phoenicia (prism weaver). Also AXIOM-7 (the guide AI), the `vanguard` drone and the `heavy` boss mech. The old style was flat shapes with ink outlines and solid colours, and no lighting.

**Sheet layout** (`<id>_atlas.png`, 2048x1536):
- Six rows of eight 256x256 frames. The rows, top to bottom, are idle, running, jumping, blade_dash, sword_attack and karate_attack.
- Feet sit on a shared baseline at y=224 in every frame; `ShogunArtPreview` draws with that anchor.
- Enemy atlases (`vanguard`, `heavy`) need only the idle row; their old atlases were 2048x256.
- AXIOM-7 needs only its portrait: nothing plays an AXIOM-7 sprite, so `axiom7_atlas.png` and `axiom7.tres` are skipped.

**Portraits:** operative portraits are 768x1024. `axiom7_portrait.png` is 768x768 and is used by the deployment cutscene.

**SpriteFrames `.tres`:** after the atlas exists, restore the old file unchanged with `git show ca92679:assets/characters/rendered/<id>.tres > assets/characters/rendered/<id>.tres`.
- It is a grid of 256x256 regions over `<id>_atlas.png`. Do not use the `.tres` that `spritecook-use-assets-in-godot` packages: it writes under `res://SpriteCook/` and uses different names.
- Each operative `.tres` defines 11 animations, all cut from the atlas:
  - `idle`, `default` and `finished`: idle row, 8 fps, looping.
  - `running`: 12 fps, looping.
  - `jumping`, `falling` and `double_jump`: jumping row, 12 fps, looping.
  - `blade_dash`: 56 fps, one-shot.
  - `sword_attack`: 32 fps, one-shot.
  - `karate_attack`: 26.67 fps, one-shot.
  - `hit`: idle row, 8 fps, one-shot.
- Enemy `.tres` files (`vanguard`, `heavy`) define only `idle`. Do not restore `axiom7.tres`.

**Method:**
1. **Base character:** `generate_character` with `perspective="platformer"`, facing right, one per operative and enemy. The old portrait or atlas is the reference.
2. **Animations:** `generate_character_animations` on that base, with `output_frames=8`:
   - preset ids for idle, run and jump (see `list_character_workflows`);
   - `custom_animations` for `blade_dash`, `sword_attack` and `karate_attack`, which are not presets.
3. **Fit** the frames into the 2048x1536 atlas (see Fitting).
4. **Portraits:** `generate_game_art` with the base character as `reference_asset_id`, at 9:16, cropped to 3:4 (768x1024).

The character workflow is built for pixel art. If the owner chooses a non-pixel style, generate each pose with `generate_game_art` from the reference instead, and check the frame-to-frame consistency by eye.

**Tested by:** `tests/test_rendered_character_assets.gd`, when a `.tres` exists. It checks:
- the 768x1024 portrait size;
- 8 frames for each of the six base animations, cut from `<id>_atlas.png` (that exact file name);
- that the first and last frames differ;
- that the attacks do not loop;
- the attack timings;
- that the enemies have an 8-frame `idle`.

It does not check the 2048 px width, the row order, the baseline or the extra five animations, so check those by eye.
**Loaded by:** `scripts/actors/CyberBossMech2D.cs` (58), `scripts/actors/CyberDrone2D.cs` (36), `scripts/actors/OperativeVisuals.cs` (34, 38), `scripts/main_menu/CharacterSelection.cs` (493), `scripts/main_menu/deployment_stage.gd` (20), `scripts/ui/ShatteredAegisStage.cs` (25)

**Also deleted (do not regenerate):** `assets/characters/rendered/apex_titan.tres`, `assets/characters/rendered/axiom7.tres`, `assets/characters/rendered/cyber_shogun.tres`, `assets/characters/rendered/heavy.tres`, `assets/characters/rendered/mirage_blade.tres`, `assets/characters/rendered/oceanus.tres`, `assets/characters/rendered/phoenicia.tres`, `assets/characters/rendered/surya_prime.tres`, `assets/characters/rendered/vanguard.tres`, `assets/characters/rendered/provenance.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/characters/rendered/apex_titan_atlas.png` | 2048x1536 | sprite sheet, transparent | remake | `7a07374ed13d` |
| `assets/characters/rendered/apex_titan_portrait.png` | 768x1024 | portrait, transparent | remake | `3d8b018b4a08` |
| `assets/characters/rendered/axiom7_atlas.png` | 2048x256 | sprite sheet, transparent | skip: unused | `870613047feb` |
| `assets/characters/rendered/axiom7_portrait.png` | 768x768 | portrait, transparent | remake | `697ee858ce8e` |
| `assets/characters/rendered/axiom7_side.png` | 256x256 | sprite, transparent | skip: unused | `3aab3a2ad564` |
| `assets/characters/rendered/cyber_shogun_atlas.png` | 2048x1536 | sprite sheet, transparent | remake | `807c7a97f953` |
| `assets/characters/rendered/cyber_shogun_portrait.png` | 768x1024 | portrait, transparent | remake | `3289625bebc5` |
| `assets/characters/rendered/heavy_atlas.png` | 2048x256 | sprite sheet, transparent | remake | `49d110abaf2f` |
| `assets/characters/rendered/heavy_portrait.png` | 768x768 | portrait, transparent | skip: unused | `153068aa41c5` |
| `assets/characters/rendered/heavy_side.png` | 256x256 | sprite, transparent | skip: unused | `03e48bc6dffd` |
| `assets/characters/rendered/mirage_blade_atlas.png` | 2048x1536 | sprite sheet, transparent | remake | `126e38ea8526` |
| `assets/characters/rendered/mirage_blade_portrait.png` | 768x1024 | portrait, transparent | remake | `5acc987e5202` |
| `assets/characters/rendered/oceanus_atlas.png` | 2048x1536 | sprite sheet, transparent | remake | `2e858f7714ee` |
| `assets/characters/rendered/oceanus_portrait.png` | 768x1024 | portrait, transparent | remake | `d0d8b29051f2` |
| `assets/characters/rendered/phoenicia_atlas.png` | 2048x1536 | sprite sheet, transparent | remake | `574a510ae51e` |
| `assets/characters/rendered/phoenicia_portrait.png` | 768x1024 | portrait, transparent | remake | `310b70657d0f` |
| `assets/characters/rendered/surya_prime_atlas.png` | 2048x1536 | sprite sheet, transparent | remake | `bc41dbfc3897` |
| `assets/characters/rendered/surya_prime_portrait.png` | 768x1024 | portrait, transparent | remake | `a9a84779d7a7` |
| `assets/characters/rendered/vanguard_atlas.png` | 2048x256 | sprite sheet, transparent | remake | `9abe9621e249` |
| `assets/characters/rendered/vanguard_portrait.png` | 768x768 | portrait, transparent | skip: unused | `c17a260690e8` |
| `assets/characters/rendered/vanguard_side.png` | 256x256 | sprite, transparent | skip: unused | `3face2adc88d` |

## Phases 2 and 3: Cutscenes (deployment first)

**Shared rules:**
- Every scene is a 1920x1080 layer stack: an opaque base plate (where listed) plus transparent structure and lights layers.
- Pose sheets use the cell size and pivot listed per cutscene, with each pose's feet on the pivot.
- **Route:** one approved key frame per cutscene with `generate_game_art`, then the other layers as edits of it.
- Pose sheets come from `generate_character` + `generate_character_animations`, reusing the Phase 1 designs, then are normalized into the grid.

### Deployment (opening cutscene)

**Shows:** Night rooftop city above the opening deployment: far skyline (opaque base), mid city structure and lights, the rooftop, a patrol drone, and Cyber Shogun's pose sheet.

**Fixed positions:** the game draws only the region x 840-1080, y 440-500 of `drone_structure.png` and `drone_lights.png` as the patrol drone; the drone must fill that box and the rest stays empty. Shogun's pose sheet is 8 columns by 5 rows of 512x512 cells, with feet at (256, 448).

**Old layout:**
- Palette: navy/cyan; Shogun red/gold/dark ink
- Canvas: 1920x1080
- Atlas: cell 512x512; columns 8; rows idle, running, jumping, blade_dash, settle; pivot 256x448

**Loaded by:** `scripts/main_menu/deployment_stage.gd` (11, 12, 13, 14, 15, 16, 17, 18, 19), `scripts/ui/NeonRustAftermathStage.cs` (20), `scripts/ui/NeonRustStage.cs` (19), `scripts/ui/ShatteredAegisStage.cs` (21, 22, 23), `scripts/ui/TokyoStage.cs` (22, 23, 24), `scripts/ui/WildfireAftermathStage.cs` (19), `scripts/ui/WildfireStage.cs` (18, 19)

**Also deleted (do not regenerate):** `assets/cutscenes/deployment/live/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/cutscenes/deployment/live/city_base.png` | 1920x1080 | opaque base plate | remake | `83cab5e0e4b6` |
| `assets/cutscenes/deployment/live/city_lights.png` | 1920x1080 | transparent lights layer | remake | `9341513a8d45` |
| `assets/cutscenes/deployment/live/city_structure.png` | 1920x1080 | transparent structure layer | remake | `be19a6b02ded` |
| `assets/cutscenes/deployment/live/drone_lights.png` | 1920x1080 | transparent lights layer | remake | `6871c27f0467` |
| `assets/cutscenes/deployment/live/drone_structure.png` | 1920x1080 | transparent structure layer | remake | `9fa7f2a3f1e1` |
| `assets/cutscenes/deployment/live/roof_lights.png` | 1920x1080 | transparent lights layer | remake | `f30004a582d3` |
| `assets/cutscenes/deployment/live/roof_structure.png` | 1920x1080 | transparent structure layer | remake | `88c8c263fa0d` |
| `assets/cutscenes/deployment/live/shogun.png` | 4096x2560 | sprite sheet, transparent | remake | `886359865a3c` |

### Concrete Wildfire arrival

**Shows:** Concrete Wildfire arrival: a checkpoint, civilians and the siege, plus four Shogun poses and a blade. The sky reuses the deployment city.

**Needs first:** `deployment/live/city_base.png` and `city_lights.png`.

**Old layout:**
- Palette: outlined red/gold operative, navy/cyan checkpoint, amber firelight
- Scene size: 1920x1080
- Pose sheet size: 2048x512
- Pose order: approach, survey, ready, look_back
- Pose cell: 512x512
- Pose pivot (x, y in each cell): 256x448
- Blade size: 512x512

**Loaded by:** `scripts/ui/WildfireAftermathStage.cs` (20, 21, 22), `scripts/ui/WildfireStage.cs` (9)

**Also deleted (do not regenerate):** `assets/cutscenes/wildfire/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/cutscenes/wildfire/blade.png` | 512x512 | sprite, transparent | remake | `3317fcb6f6b1` |
| `assets/cutscenes/wildfire/checkpoint_lights.png` | 1920x1080 | transparent lights layer | remake | `67e44b73274e` |
| `assets/cutscenes/wildfire/checkpoint_structure.png` | 1920x1080 | transparent structure layer | remake | `9c6fbf1da978` |
| `assets/cutscenes/wildfire/civilians_lights.png` | 1920x1080 | transparent lights layer | remake | `c07a4558a853` |
| `assets/cutscenes/wildfire/civilians_structure.png` | 1920x1080 | transparent structure layer | remake | `89404ebaf9b5` |
| `assets/cutscenes/wildfire/poses.png` | 2048x512 | sprite sheet, transparent | remake | `b8d8fe694b79` |
| `assets/cutscenes/wildfire/siege_lights.png` | 1920x1080 | transparent lights layer | remake | `ac5a4a557247` |
| `assets/cutscenes/wildfire/siege_structure.png` | 1920x1080 | transparent structure layer | remake | `a2f9a791445d` |

### Tokyo Remembers

**Shows:** Tokyo Remembers: a dojo interior (structure and lights layers), Cyber Shogun's four story poses and a lone blade. The city behind the dojo reuses the deployment city layers.

**Needs first:** `deployment/live/city_base.png`, `city_structure.png` and `city_lights.png`.

**Fixed positions:** the blade is drawn at a fixed offset from the escape and testimony poses' hand (`TokyoStage.cs`), so keep the old poses' hand positions.

**Old layout:**
- Palette: outlined red/gold operative, navy/cyan dojo, amber lanterns
- Scene size: 1920x1080
- Pose sheet size: 2048x512
- Pose order: escape, testimony, sheathe, look_back
- Pose cell: 512x512
- Pose pivot (x, y in each cell): 256x448
- Blade size: 512x512

**Loaded by:** `scripts/ui/TokyoStage.cs` (13)

**Also deleted (do not regenerate):** `assets/cutscenes/tokyo/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/cutscenes/tokyo/blade.png` | 512x512 | sprite, transparent | remake | `5303cc5ef2dd` |
| `assets/cutscenes/tokyo/dojo_lights.png` | 1920x1080 | transparent lights layer | remake | `583ab7036ea3` |
| `assets/cutscenes/tokyo/dojo_structure.png` | 1920x1080 | transparent structure layer | remake | `25a19d27c393` |
| `assets/cutscenes/tokyo/poses.png` | 2048x512 | sprite sheet, transparent | remake | `029427ec6474` |

### Concrete Wildfire aftermath

**Shows:** Concrete Wildfire aftermath: a reunion scene and a terminal scene, and a three-operative pose sheet.

**Needs first:** `deployment/live/city_base.png` and the Concrete Wildfire `checkpoint_structure.png`, `siege_structure.png` and `siege_lights.png`.

**Layout:** Poses are drawn as `Rect2(column*512, row*512, 512, 512)`, so each row is one operative and each column one pose; feet at y=448 in each cell.

**Old layout:**
- Palette: navy/cyan checkpoint; amber fire; red/gold Kaito; hydro Oceanus; violet Titan
- Scene size: 1920x1080
- Atlas: 3072x1536
- Cell: 512x512
- Rows (top to bottom): Kaito, Oceanus, Titan
- Columns (left to right): calm, gesture/support, walk0, walk2, walk4, walk6

**Loaded by:** `scripts/ui/NeonRustStage.cs` (20), `scripts/ui/WildfireAftermathStage.cs` (9)

**Also deleted (do not regenerate):** `assets/cutscenes/wildfire_aftermath/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/cutscenes/wildfire_aftermath/poses.png` | 3072x1536 | sprite sheet, transparent | remake | `cb0fd098c305` |
| `assets/cutscenes/wildfire_aftermath/reunion_base.png` | 1920x1080 | opaque base plate | skip: unused | `dca39aece580` |
| `assets/cutscenes/wildfire_aftermath/reunion_lights.png` | 1920x1080 | transparent lights layer | skip: unused | `49b58661c825` |
| `assets/cutscenes/wildfire_aftermath/reunion_structure.png` | 1920x1080 | transparent structure layer | remake | `504eb9d217bf` |
| `assets/cutscenes/wildfire_aftermath/terminal_base.png` | 1920x1080 | opaque base plate | skip: unused | `859e65ae6af1` |
| `assets/cutscenes/wildfire_aftermath/terminal_lights.png` | 1920x1080 | transparent lights layer | skip: unused | `ed16749903fa` |
| `assets/cutscenes/wildfire_aftermath/terminal_structure.png` | 1920x1080 | transparent structure layer | remake | `96eeaf3104e2` |

### Neon Rust intro (Silent Freight)

**Shows:** Silent Freight: depot, underpass, cargo and walkway scenes, and a 3x3 sheet of new poses.

**Needs first:** `deployment/live/city_base.png` and `wildfire_aftermath/poses.png` (walk cycles).

**Layout:** Poses are drawn as `Rect2(column*512, row*512, 512, 512)`, so each row is one operative and each column one pose; feet at y=448 in each cell.

**Old layout:**
- Palette: navy steel, cold cyan surveillance, amber cargo; established red/gold, hydro and violet operatives
- Scene size: 1920x1080
- Atlas: 1536x1536
- Cell: 512x512
- Rows (top to bottom): Kaito, Oceanus, Titan
- Columns (left to right): concealed/runoff, brace, inspect

**Loaded by:** `scripts/ui/NeonRustAftermathStage.cs` (21, 22, 23, 24), `scripts/ui/NeonRustStage.cs` (9)

**Also deleted (do not regenerate):** `assets/cutscenes/neon_rust_intro/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/cutscenes/neon_rust_intro/cargo_lights.png` | 1920x1080 | transparent lights layer | remake | `2f91571885c5` |
| `assets/cutscenes/neon_rust_intro/cargo_structure.png` | 1920x1080 | transparent structure layer | remake | `6000e9191818` |
| `assets/cutscenes/neon_rust_intro/depot_lights.png` | 1920x1080 | transparent lights layer | remake | `a6619bbc08d4` |
| `assets/cutscenes/neon_rust_intro/depot_structure.png` | 1920x1080 | transparent structure layer | remake | `67285e7a189f` |
| `assets/cutscenes/neon_rust_intro/poses.png` | 1536x1536 | sprite sheet, transparent | remake | `51ce78fd710a` |
| `assets/cutscenes/neon_rust_intro/underpass_lights.png` | 1920x1080 | transparent lights layer | remake | `9807408d3bb0` |
| `assets/cutscenes/neon_rust_intro/underpass_structure.png` | 1920x1080 | transparent structure layer | remake | `0a06f6889ade` |
| `assets/cutscenes/neon_rust_intro/walkway_lights.png` | 1920x1080 | transparent lights layer | remake | `66b89e6237a7` |
| `assets/cutscenes/neon_rust_intro/walkway_structure.png` | 1920x1080 | transparent structure layer | remake | `c7b5092ff336` |

### Neon Rust aftermath

**Shows:** Neon Rust aftermath: a fortress scene and a five-operative pose sheet.

**Needs first:** `deployment/live/city_base.png` and the Neon Rust intro's depot and cargo layers.

**Layout:** Poses are drawn as `Rect2(column*512, row*512, 512, 512)`, so each row is one operative and each column one pose; feet at y=448 in each cell.

**Old layout:**
- Palette: red/gold Kaito; hydro cyan Oceanus; violet Titan; prism magenta Aura; emerald Mirage
- Scene size: 1920x1080
- Atlas: 3072x2560
- Cell: 512x512
- Rows (top to bottom): cyber_shogun, oceanus, apex_titan, phoenicia, mirage_blade
- Columns (left to right): calm, gesture, walk0, walk2, walk4, walk6

**Loaded by:** `scripts/ui/ActIIStage.cs` (30), `scripts/ui/NeonRustAftermathStage.cs` (9), `scripts/ui/ShatteredAegisStage.cs` (24)

**Also deleted (do not regenerate):** `assets/cutscenes/neon_rust_aftermath/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/cutscenes/neon_rust_aftermath/fortress_lights.png` | 1920x1080 | transparent lights layer | remake | `bff415977a12` |
| `assets/cutscenes/neon_rust_aftermath/fortress_structure.png` | 1920x1080 | transparent structure layer | remake | `08314f01fcd8` |
| `assets/cutscenes/neon_rust_aftermath/poses.png` | 3072x2560 | sprite sheet, transparent | remake | `1e0cc94c184b` |

### Shattered Aegis intro

**Shows:** The Wall That Faces Inward: approach, shield road and command scenes (guns turned inward, a sheltered road) and two Surya Prime poses.

**Needs first:** the three `deployment/live/city_*` layers, `neon_rust_aftermath/poses.png` and `characters/rendered/heavy_atlas.png` (Voss).

**Fixed positions:** the scene layers are drawn 400 px lower than the screen top (`ShatteredAegisStage.cs`), so only their top 680 px show; the city layers behind them are drawn 160 px higher.

**Old layout:**
- Palette: Established red/gold Kaito, hydro Oceanus, violet Titan, prism Aura, emerald Mirage, solar gold Surya; original armored heavy Voss silhouette
- Scene size: 1920x1080
- Atlas: 1024x512
- Cell: 512x512
- Rows (top to bottom): surya_prime
- Columns (left to right): calm, shield

**Loaded by:** `scripts/ui/ActIIStage.cs` (31), `scripts/ui/ShatteredAegisStage.cs` (9)

**Also deleted (do not regenerate):** `assets/cutscenes/shattered_aegis_intro/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/cutscenes/shattered_aegis_intro/approach_lights.png` | 1920x1080 | transparent lights layer | remake | `cfe9752a575b` |
| `assets/cutscenes/shattered_aegis_intro/approach_structure.png` | 1920x1080 | transparent structure layer | remake | `69d225be93de` |
| `assets/cutscenes/shattered_aegis_intro/command_lights.png` | 1920x1080 | transparent lights layer | remake | `e7cc6b7d945b` |
| `assets/cutscenes/shattered_aegis_intro/command_structure.png` | 1920x1080 | transparent structure layer | remake | `d1e4c1e873f1` |
| `assets/cutscenes/shattered_aegis_intro/shield_road_lights.png` | 1920x1080 | transparent lights layer | remake | `6ccb9a7f86e1` |
| `assets/cutscenes/shattered_aegis_intro/shield_road_structure.png` | 1920x1080 | transparent structure layer | remake | `3993399ecf60` |
| `assets/cutscenes/shattered_aegis_intro/surya.png` | 1024x512 | sprite sheet, transparent | remake | `3a79a5ef3424` |

### Names without Numbers epilogue

**Shows:** The Mission 5 epilogue's district, as a structure layer and a lights layer.

**Old layout:**
- Scene size: 1920x1080

**Loaded by:** `scripts/ui/NamesWithoutNumbersAftermathStage.cs` (11)

**Also deleted (do not regenerate):** `assets/cutscenes/names_without_numbers/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/cutscenes/names_without_numbers/district_lights.png` | 1920x1080 | transparent lights layer | remake | `e8d3e8a11d09` |
| `assets/cutscenes/names_without_numbers/district_structure.png` | 1920x1080 | transparent structure layer | remake | `d902f53ef62f` |

### Act II opening

**Shows:** Act II opening: a surveillance district with queue, scanner, registry, service and civilians scenes. White scanner geometry, dark glass, amber denial lights. All on-screen text comes from Godot, so the images contain no words.

**Needs first:** `neon_rust_aftermath/poses.png` and `shattered_aegis_intro/surya.png`.

**Old layout:**
- Palette: Dark glass, white scanning lights, amber denial, restrained civilian warmth; established operative designs
- Scenes: queue, scanner, registry, service

**Loaded by:** `scripts/ui/ActIIStage.cs` (12)

**Also deleted (do not regenerate):** `assets/cutscenes/act_ii_opening/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/cutscenes/act_ii_opening/civilians_lights.png` | 1920x1080 | transparent lights layer | remake | `db4deb7edec1` |
| `assets/cutscenes/act_ii_opening/civilians_structure.png` | 1920x1080 | transparent structure layer | remake | `4a42fc649a7d` |
| `assets/cutscenes/act_ii_opening/queue_base.png` | 1920x1080 | opaque base plate | remake | `ec8b3bd11803` |
| `assets/cutscenes/act_ii_opening/queue_lights.png` | 1920x1080 | transparent lights layer | remake | `b7b3ceb1708a` |
| `assets/cutscenes/act_ii_opening/queue_structure.png` | 1920x1080 | transparent structure layer | remake | `a4d04fae6f27` |
| `assets/cutscenes/act_ii_opening/registry_base.png` | 1920x1080 | opaque base plate | remake | `9f6c0c999b19` |
| `assets/cutscenes/act_ii_opening/registry_lights.png` | 1920x1080 | transparent lights layer | remake | `746305809512` |
| `assets/cutscenes/act_ii_opening/registry_structure.png` | 1920x1080 | transparent structure layer | remake | `4ee49f908a7e` |
| `assets/cutscenes/act_ii_opening/scanner_base.png` | 1920x1080 | opaque base plate | remake | `1867e26cc989` |
| `assets/cutscenes/act_ii_opening/scanner_lights.png` | 1920x1080 | transparent lights layer | remake | `95f19e55dc33` |
| `assets/cutscenes/act_ii_opening/scanner_structure.png` | 1920x1080 | transparent structure layer | remake | `2643e641fe5a` |
| `assets/cutscenes/act_ii_opening/service_base.png` | 1920x1080 | opaque base plate | remake | `038e426d9bdd` |
| `assets/cutscenes/act_ii_opening/service_lights.png` | 1920x1080 | transparent lights layer | remake | `801020456d54` |
| `assets/cutscenes/act_ii_opening/service_structure.png` | 1920x1080 | transparent structure layer | remake | `22959003be55` |

## Phase 4: Mission props

**Shared rules:**
- 512x512 side-view sprites with a transparent background and empty margins (nothing touching the edge). The game stretches each one to the display size listed below, which is rarely square; see Fitting.
- Pivot at the centre: the game centres each sprite on its node with an offset.
- The C# code scales them and owns their collision.
- **Route:** `generate_game_art` per prop, with one approved prop per mission as the style reference.

### Concrete Wildfire props

**Shows:** Side-view props for the Concrete Wildfire mission: building facade, shelter, civilians, gunner and battery. Navy, cyan and rust palette.

**Old layout:**
- View: orthographic side
- Pivot: bottom center; explicitly scaled by WildfireVisual
- Size: 512x512

**Display sizes (width x height in game pixels):** facade 950x600, battery 400x230, gunner 110x100, civilians 160x110, shelter 240x210.

**Loaded by:** `scripts/gameplay/WildfireVisual.cs` (9)

**Also deleted (do not regenerate):** `assets/environment/wildfire/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/environment/wildfire/battery.png` | 512x512 | sprite, transparent | remake | `473b47d7f3b5` |
| `assets/environment/wildfire/civilians.png` | 512x512 | sprite, transparent | remake | `44e08ab5a9e9` |
| `assets/environment/wildfire/facade.png` | 512x512 | sprite, transparent | remake | `277a7966f478` |
| `assets/environment/wildfire/gunner.png` | 512x512 | sprite, transparent | remake | `65ff3f592f83` |
| `assets/environment/wildfire/shelter.png` | 512x512 | sprite, transparent | remake | `1c690bb80951` |

### Neon Rust props

**Shows:** Side-view props for the Neon Rust mission (freight depot and rail yard). Navy, cyan and amber palette.

**Old layout:**
- View: orthographic side
- Pivot: center; C# explicit size and offset
- Size: 512x512

**Display sizes (width x height in game pixels):** depot 1900x950, freight 900x390, machinery 330x350 (150x180 on gates), support 410x650, coupler 150x180, sonic 150x180, laser 110x240.

**Loaded by:** `scripts/gameplay/NeonRustVisual.cs` (9)

**Also deleted (do not regenerate):** `assets/environment/neon_rust/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/environment/neon_rust/coupler.png` | 512x512 | sprite, transparent | remake | `ad4a07b9268f` |
| `assets/environment/neon_rust/depot.png` | 512x512 | sprite, transparent | remake | `47ade6900bd7` |
| `assets/environment/neon_rust/freight.png` | 512x512 | sprite, transparent | remake | `48e7bd6586f2` |
| `assets/environment/neon_rust/laser.png` | 512x512 | sprite, transparent | remake | `964951fae564` |
| `assets/environment/neon_rust/machinery.png` | 512x512 | sprite, transparent | remake | `4c325af7be46` |
| `assets/environment/neon_rust/sonic.png` | 512x512 | sprite, transparent | remake | `00ee4ea7a6c9` |
| `assets/environment/neon_rust/support.png` | 512x512 | sprite, transparent | remake | `19c35763fdc0` |

### Shattered Aegis props

**Shows:** Side-view fortress props for Shattered Aegis: navy armour, a little violet, amber for live systems, cyan for safe systems.

**Old layout:**
- View: orthographic side
- Pivot: center; explicit C# size/offset
- Size: 512x512

**Display sizes (width x height in game pixels):** fortress 1900x1000, gun 600x550, coolant 400x400, crossing 800x800, controller 150x180, civilians 100x100.

**Loaded by:** `scripts/gameplay/AegisCivilian.cs` (11), `scripts/gameplay/AegisVisual.cs` (9)

**Also deleted (do not regenerate):** `assets/environment/shattered_aegis/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/environment/shattered_aegis/civilians.png` | 512x512 | sprite, transparent | remake | `d96d02ecf299` |
| `assets/environment/shattered_aegis/controller.png` | 512x512 | sprite, transparent | remake | `589cfca3d7b5` |
| `assets/environment/shattered_aegis/coolant.png` | 512x512 | sprite, transparent | remake | `489813ca3e55` |
| `assets/environment/shattered_aegis/crossing.png` | 512x512 | sprite, transparent | remake | `46f1627dc17c` |
| `assets/environment/shattered_aegis/fortress.png` | 512x512 | sprite, transparent | remake | `a4f357d7dd8a` |
| `assets/environment/shattered_aegis/gun.png` | 512x512 | sprite, transparent | remake | `dda71237cd59` |

### Names without Numbers props

**Shows:** Side-view surveillance-district props for the Names without Numbers mission, including the access gate shown once civilian access is restored (`access_restored.png`).

**Old layout:**
- View: orthographic side
- Size: 512x512

**Display sizes (width x height in game pixels):** district 2600x1500, scanner 280x600, archive 520x600, terminal 140x170, access_denied and access_restored 460x600, civilians 270x180.

**Loaded by:** `scripts/gameplay/NamesWithoutNumbersMission.cs` (258, 341)

**Also deleted (do not regenerate):** `assets/environment/names/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/environment/names/access_denied.png` | 512x512 | sprite, transparent | remake | `461b3b51846c` |
| `assets/environment/names/access_restored.png` | 512x512 | sprite, transparent | remake | `0f210177e045` |
| `assets/environment/names/archive.png` | 512x512 | sprite, transparent | remake | `94eb9af6d3ee` |
| `assets/environment/names/civilians.png` | 512x512 | sprite, transparent | remake | `a2170dd05591` |
| `assets/environment/names/district.png` | 512x512 | sprite, transparent | remake | `ca15767db492` |
| `assets/environment/names/scanner.png` | 512x512 | sprite, transparent | remake | `907ec27bb1a8` |
| `assets/environment/names/terminal.png` | 512x512 | sprite, transparent | remake | `a4b10306be9c` |

## Phase 5: Later-campaign environments

**Shared rules:**
- Each family has an environment scene and an aftermath scene. Each scene has three 1920x1080 layers: `_base` (opaque), `_structure` and `_lights` (transparent).
- Six families also have 512x512 `boss_structure` and `boss_lights` plates; Cyber Grave has none.
- **Shows:** industrial fortresses and facilities built from beveled steel panels with bolts and seams, insulated conduits, and turbines or reactor irises, with cyan and amber lights. Each family is a different site; view its old images.
- **Where they appear:** the environment layers are the gameplay backdrop of Missions 6 to 12 (`LaterEnvironmentView`), and both scenes appear in the family's intro and aftermath cutscenes.
- **Cutscene rule:** `CampaignArtwork.Available` plays a family's cutscenes only when `environment_structure.png`, `aftermath_structure.png` and the cutscene's `cues.json` all exist.
- **Exception:** Judgment Day's aftermath is drawn without a base, so its `aftermath_base.png` is skipped.
- **Boss plates:** the game draws them as a sprite centred on the boss at scale 0.45, offset (0, -7). Judgment Day's commander is also drawn at 260x260 above its feet.
- **Route:** a base plate per scene, then structure and lights as edits of it.

### Cyber Grave

**Loaded by:** `scripts/gameplay/LaterCodeLevel.cs` (41), `scripts/gameplay/LaterEnvironmentView.cs` (23), `scripts/ui/CampaignArtwork.cs` (43, 130, 142)

**Also deleted (do not regenerate):** `assets/environments/later_campaign/cyber_grave/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/environments/later_campaign/cyber_grave/aftermath_base.png` | 1920x1080 | opaque base plate | remake | `cd45938975f7` |
| `assets/environments/later_campaign/cyber_grave/aftermath_lights.png` | 1920x1080 | transparent lights layer | remake | `790c31b80661` |
| `assets/environments/later_campaign/cyber_grave/aftermath_structure.png` | 1920x1080 | transparent structure layer | remake | `a93948ffe182` |
| `assets/environments/later_campaign/cyber_grave/environment_base.png` | 1920x1080 | opaque base plate | remake | `fbb39ac47142` |
| `assets/environments/later_campaign/cyber_grave/environment_lights.png` | 1920x1080 | transparent lights layer | remake | `9bda626cfe16` |
| `assets/environments/later_campaign/cyber_grave/environment_structure.png` | 1920x1080 | transparent structure layer | remake | `f6de31208509` |

### Foundry Forge

**Loaded by:** `scripts/gameplay/LaterCodeLevel.cs` (41), `scripts/gameplay/LaterEnvironmentView.cs` (23), `scripts/ui/CampaignArtwork.cs` (43, 130, 142)

**Also deleted (do not regenerate):** `assets/environments/later_campaign/foundry_forge/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/environments/later_campaign/foundry_forge/aftermath_base.png` | 1920x1080 | opaque base plate | remake | `a00a52dc484e` |
| `assets/environments/later_campaign/foundry_forge/aftermath_lights.png` | 1920x1080 | transparent lights layer | remake | `ded163e3d7a1` |
| `assets/environments/later_campaign/foundry_forge/aftermath_structure.png` | 1920x1080 | transparent structure layer | remake | `8d07ca72f0da` |
| `assets/environments/later_campaign/foundry_forge/boss_lights.png` | 512x512 | transparent lights layer | remake | `5393b3f81ef9` |
| `assets/environments/later_campaign/foundry_forge/boss_structure.png` | 512x512 | transparent structure layer | remake | `42abc1cc351a` |
| `assets/environments/later_campaign/foundry_forge/environment_base.png` | 1920x1080 | opaque base plate | remake | `56c2e98ce256` |
| `assets/environments/later_campaign/foundry_forge/environment_lights.png` | 1920x1080 | transparent lights layer | remake | `8d89dea1946c` |
| `assets/environments/later_campaign/foundry_forge/environment_structure.png` | 1920x1080 | transparent structure layer | remake | `2ac7524be8dc` |

### Stratos Docks

**Loaded by:** `scripts/gameplay/LaterCodeLevel.cs` (41), `scripts/gameplay/LaterEnvironmentView.cs` (23), `scripts/ui/CampaignArtwork.cs` (43, 130, 142)

**Also deleted (do not regenerate):** `assets/environments/later_campaign/stratos_docks/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/environments/later_campaign/stratos_docks/aftermath_base.png` | 1920x1080 | opaque base plate | remake | `1cf305c341ba` |
| `assets/environments/later_campaign/stratos_docks/aftermath_lights.png` | 1920x1080 | transparent lights layer | remake | `c00e095bb4d4` |
| `assets/environments/later_campaign/stratos_docks/aftermath_structure.png` | 1920x1080 | transparent structure layer | remake | `b4159cfc7c19` |
| `assets/environments/later_campaign/stratos_docks/boss_lights.png` | 512x512 | transparent lights layer | remake | `55d2dea35332` |
| `assets/environments/later_campaign/stratos_docks/boss_structure.png` | 512x512 | transparent structure layer | remake | `fee1782f1ee4` |
| `assets/environments/later_campaign/stratos_docks/environment_base.png` | 1920x1080 | opaque base plate | remake | `301ae1f0d098` |
| `assets/environments/later_campaign/stratos_docks/environment_lights.png` | 1920x1080 | transparent lights layer | remake | `1bc817549a5d` |
| `assets/environments/later_campaign/stratos_docks/environment_structure.png` | 1920x1080 | transparent structure layer | remake | `162530b11a88` |

### Spire Outer Hull

**Loaded by:** `scripts/gameplay/LaterCodeLevel.cs` (41), `scripts/gameplay/LaterEnvironmentView.cs` (23), `scripts/ui/CampaignArtwork.cs` (43, 130, 142)

**Also deleted (do not regenerate):** `assets/environments/later_campaign/spire_outer_hull/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/environments/later_campaign/spire_outer_hull/aftermath_base.png` | 1920x1080 | opaque base plate | remake | `7a73ec3285e9` |
| `assets/environments/later_campaign/spire_outer_hull/aftermath_lights.png` | 1920x1080 | transparent lights layer | remake | `f00f36ba3ed8` |
| `assets/environments/later_campaign/spire_outer_hull/aftermath_structure.png` | 1920x1080 | transparent structure layer | remake | `a23c60d6002a` |
| `assets/environments/later_campaign/spire_outer_hull/boss_lights.png` | 512x512 | transparent lights layer | remake | `c8cc093bc95e` |
| `assets/environments/later_campaign/spire_outer_hull/boss_structure.png` | 512x512 | transparent structure layer | remake | `ecc035dd7d9b` |
| `assets/environments/later_campaign/spire_outer_hull/environment_base.png` | 1920x1080 | opaque base plate | remake | `c887783eb26f` |
| `assets/environments/later_campaign/spire_outer_hull/environment_lights.png` | 1920x1080 | transparent lights layer | remake | `fb542d83549f` |
| `assets/environments/later_campaign/spire_outer_hull/environment_structure.png` | 1920x1080 | transparent structure layer | remake | `88946f4fb5f7` |

### Cyber Nexus

**Loaded by:** `scripts/gameplay/LaterCodeLevel.cs` (41), `scripts/gameplay/LaterEnvironmentView.cs` (23), `scripts/ui/CampaignArtwork.cs` (43, 130, 142)

**Also deleted (do not regenerate):** `assets/environments/later_campaign/cyber_nexus/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/environments/later_campaign/cyber_nexus/aftermath_base.png` | 1920x1080 | opaque base plate | remake | `056e552cb3f6` |
| `assets/environments/later_campaign/cyber_nexus/aftermath_lights.png` | 1920x1080 | transparent lights layer | remake | `021b45b6f508` |
| `assets/environments/later_campaign/cyber_nexus/aftermath_structure.png` | 1920x1080 | transparent structure layer | remake | `c27b354143eb` |
| `assets/environments/later_campaign/cyber_nexus/boss_lights.png` | 512x512 | transparent lights layer | remake | `5396aa49504e` |
| `assets/environments/later_campaign/cyber_nexus/boss_structure.png` | 512x512 | transparent structure layer | remake | `fc1887e7f3b8` |
| `assets/environments/later_campaign/cyber_nexus/environment_base.png` | 1920x1080 | opaque base plate | remake | `0828311917a7` |
| `assets/environments/later_campaign/cyber_nexus/environment_lights.png` | 1920x1080 | transparent lights layer | remake | `54dd292b04f9` |
| `assets/environments/later_campaign/cyber_nexus/environment_structure.png` | 1920x1080 | transparent structure layer | remake | `73d281fb0963` |

### Apex Bastion

**Loaded by:** `scripts/gameplay/LaterCodeLevel.cs` (41), `scripts/gameplay/LaterEnvironmentView.cs` (23), `scripts/ui/CampaignArtwork.cs` (43, 130, 142)

**Also deleted (do not regenerate):** `assets/environments/later_campaign/apex_bastion/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/environments/later_campaign/apex_bastion/aftermath_base.png` | 1920x1080 | opaque base plate | remake | `012a1c49af19` |
| `assets/environments/later_campaign/apex_bastion/aftermath_lights.png` | 1920x1080 | transparent lights layer | remake | `15b053dba8e3` |
| `assets/environments/later_campaign/apex_bastion/aftermath_structure.png` | 1920x1080 | transparent structure layer | remake | `6ebc0c622300` |
| `assets/environments/later_campaign/apex_bastion/boss_lights.png` | 512x512 | transparent lights layer | remake | `71fa238166b4` |
| `assets/environments/later_campaign/apex_bastion/boss_structure.png` | 512x512 | transparent structure layer | remake | `a86dd64be0d0` |
| `assets/environments/later_campaign/apex_bastion/environment_base.png` | 1920x1080 | opaque base plate | remake | `719e146cffd1` |
| `assets/environments/later_campaign/apex_bastion/environment_lights.png` | 1920x1080 | transparent lights layer | remake | `7769825b42aa` |
| `assets/environments/later_campaign/apex_bastion/environment_structure.png` | 1920x1080 | transparent structure layer | remake | `9a274c73beac` |

### Judgment Day

**Loaded by:** `scripts/gameplay/LaterCodeLevel.cs` (41), `scripts/gameplay/LaterEnvironmentView.cs` (23), `scripts/ui/CampaignArtwork.cs` (43, 130, 142)

**Also deleted (do not regenerate):** `assets/environments/later_campaign/judgment_day/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/environments/later_campaign/judgment_day/aftermath_base.png` | 1920x1080 | opaque base plate | skip: unused | `a65d6615ab79` |
| `assets/environments/later_campaign/judgment_day/aftermath_lights.png` | 1920x1080 | transparent lights layer | remake | `4e32ea01162b` |
| `assets/environments/later_campaign/judgment_day/aftermath_structure.png` | 1920x1080 | transparent structure layer | remake | `79b8d22287ac` |
| `assets/environments/later_campaign/judgment_day/boss_lights.png` | 512x512 | transparent lights layer | remake | `f2e3a0957a86` |
| `assets/environments/later_campaign/judgment_day/boss_structure.png` | 512x512 | transparent structure layer | remake | `3185408f9c1d` |
| `assets/environments/later_campaign/judgment_day/environment_base.png` | 1920x1080 | opaque base plate | remake | `bcbb3cf71e5b` |
| `assets/environments/later_campaign/judgment_day/environment_lights.png` | 1920x1080 | transparent lights layer | remake | `d0125651437f` |
| `assets/environments/later_campaign/judgment_day/environment_structure.png` | 1920x1080 | transparent structure layer | remake | `27552d3fda1f` |

## Phase 6: City kits

**Shared rules:**
- The view is an angled, north-up, top-down orthographic camera at 16 pixels per metre.
- `module_0` to `module_7` are 384x384 buildings and props.
- Landmarks are 384x384, in each city's own style.
- Los Angeles has 32 landmarks. Every third ID is a plaza with no image (`WorldCatalog.cs` makes it a plaza and `NeoKyotoWorld.cs` skips it), which is why IDs 0, 3, 6 and so on are missing.
- **The six home cities** each need all 16 module and landmark files or none: `test_all_cities.gd` fails on a partial set.
- **Los Angeles** has 7 used modules (kind 2 is never placed, so `module_2.png` is skipped), 32 landmarks and the operative sheets.
- **Route:** `generate_game_art` per item with one approved module per city as the style reference.


### Los Angeles

**Old layout:**
- Frame size: 96x128
- Directions: 8
- Frames per direction: 4

**Operative sheets** (`operative_0.png` to `operative_5.png`, 384x1024): they live in `assets/world/` and are used in every city.
- **Method:** `generate_character` with `perspective="topdown"` from the Phase 1 design, then walk animations for each direction. Mirror east-facing rows from west-facing ones if SpriteCook returns fewer than 8 directions.
- `ExplorationPlayer` cuts them into 4 columns by 8 rows of 96x128 frames, drawn with offset (0, -23).
- **Rows** are facing directions: 0 south, 1 south-west, 2 west, 3 north-west, 4 north, 5 north-east, 6 east, 7 south-east.
- **Columns:** column 0 is standing; columns 1 to 3 are the walk cycle.
- **File number to operative:** 0 Cyber Shogun, 1 Oceanus, 2 Apex Titan, 3 Aura Phoenicia, 4 Mirage Blade, 5 Surya Prime.

**Loaded by:** `scripts/world/ExplorationPlayer.cs` (122), `scripts/world/NeoKyotoWorld.cs` (848), `scripts/world/WorldCatalog.cs` (79), `scripts/world/WorldChunkView.cs` (32)

**Also deleted (do not regenerate):** `assets/world/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/world/landmark_1.png` | 384x384 | sprite, transparent | remake | `eea5c2788fe8` |
| `assets/world/landmark_10.png` | 384x384 | sprite, transparent | remake | `823be33972ab` |
| `assets/world/landmark_11.png` | 384x384 | sprite, transparent | remake | `d41082117583` |
| `assets/world/landmark_13.png` | 384x384 | sprite, transparent | remake | `c6443d9c10ae` |
| `assets/world/landmark_14.png` | 384x384 | sprite, transparent | remake | `e726057cf148` |
| `assets/world/landmark_16.png` | 384x384 | sprite, transparent | remake | `c5eded467af7` |
| `assets/world/landmark_17.png` | 384x384 | sprite, transparent | remake | `bfa23ec94ecd` |
| `assets/world/landmark_19.png` | 384x384 | sprite, transparent | remake | `73294259380a` |
| `assets/world/landmark_2.png` | 384x384 | sprite, transparent | remake | `9cf666b94eec` |
| `assets/world/landmark_20.png` | 384x384 | sprite, transparent | remake | `fec37edd739c` |
| `assets/world/landmark_22.png` | 384x384 | sprite, transparent | remake | `e012836bbd5f` |
| `assets/world/landmark_23.png` | 384x384 | sprite, transparent | remake | `5f4941d9e818` |
| `assets/world/landmark_25.png` | 384x384 | sprite, transparent | remake | `c632327ba256` |
| `assets/world/landmark_26.png` | 384x384 | sprite, transparent | remake | `370a7eeefea6` |
| `assets/world/landmark_28.png` | 384x384 | sprite, transparent | remake | `efa1dbbfa30e` |
| `assets/world/landmark_29.png` | 384x384 | sprite, transparent | remake | `3bce7f731ee3` |
| `assets/world/landmark_31.png` | 384x384 | sprite, transparent | remake | `0d47e0571e3f` |
| `assets/world/landmark_32.png` | 384x384 | sprite, transparent | remake | `16d59d168c27` |
| `assets/world/landmark_34.png` | 384x384 | sprite, transparent | remake | `610869659983` |
| `assets/world/landmark_35.png` | 384x384 | sprite, transparent | remake | `2fed71af71fb` |
| `assets/world/landmark_37.png` | 384x384 | sprite, transparent | remake | `b365234c5e3d` |
| `assets/world/landmark_38.png` | 384x384 | sprite, transparent | remake | `c10ad11a378b` |
| `assets/world/landmark_4.png` | 384x384 | sprite, transparent | remake | `fa5b6aa02631` |
| `assets/world/landmark_40.png` | 384x384 | sprite, transparent | remake | `f1415d16844b` |
| `assets/world/landmark_41.png` | 384x384 | sprite, transparent | remake | `c2a1a32431fd` |
| `assets/world/landmark_43.png` | 384x384 | sprite, transparent | remake | `7504d533ff14` |
| `assets/world/landmark_44.png` | 384x384 | sprite, transparent | remake | `3c4731800a61` |
| `assets/world/landmark_46.png` | 384x384 | sprite, transparent | remake | `e794d7ae3560` |
| `assets/world/landmark_47.png` | 384x384 | sprite, transparent | remake | `1ea0df63ed44` |
| `assets/world/landmark_5.png` | 384x384 | sprite, transparent | remake | `22eb3a848142` |
| `assets/world/landmark_7.png` | 384x384 | sprite, transparent | remake | `93dc46b0a265` |
| `assets/world/landmark_8.png` | 384x384 | sprite, transparent | remake | `401c073587f8` |
| `assets/world/module_0.png` | 384x384 | sprite, transparent | remake | `765307710a9c` |
| `assets/world/module_1.png` | 384x384 | sprite, transparent | remake | `96dd804e909c` |
| `assets/world/module_2.png` | 384x384 | sprite, transparent | skip: unused | `00baff3a35df` |
| `assets/world/module_3.png` | 384x384 | sprite, transparent | remake | `700a42022389` |
| `assets/world/module_4.png` | 384x384 | sprite, transparent | remake | `c17a3574ac83` |
| `assets/world/module_5.png` | 384x384 | sprite, transparent | remake | `848cbdd57ab7` |
| `assets/world/module_6.png` | 384x384 | sprite, transparent | remake | `515590d017aa` |
| `assets/world/module_7.png` | 384x384 | sprite, transparent | remake | `5dd5372d68f5` |
| `assets/world/operative_0.png` | 384x1024 | sprite sheet, transparent | remake | `ec6a4778d86a` |
| `assets/world/operative_1.png` | 384x1024 | sprite sheet, transparent | remake | `64d80d1a52d2` |
| `assets/world/operative_2.png` | 384x1024 | sprite sheet, transparent | remake | `2888adc2581a` |
| `assets/world/operative_3.png` | 384x1024 | sprite sheet, transparent | remake | `64ed8be740b1` |
| `assets/world/operative_4.png` | 384x1024 | sprite sheet, transparent | remake | `42367b7f51de` |
| `assets/world/operative_5.png` | 384x1024 | sprite sheet, transparent | remake | `d7295e74a487` |

### Kyoto

**Loaded by:** `scripts/world/NeoKyotoWorld.cs` (848), `scripts/world/WorldChunkView.cs` (32), `scripts/world/cities/KyotoCity.cs` (38)

**Also deleted (do not regenerate):** `assets/world/kyoto/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/world/kyoto/landmark_0.png` | 384x384 | sprite, transparent | remake | `67e1f67afb4f` |
| `assets/world/kyoto/landmark_1.png` | 384x384 | sprite, transparent | remake | `c1ce3d54a796` |
| `assets/world/kyoto/landmark_2.png` | 384x384 | sprite, transparent | remake | `bc6990a02668` |
| `assets/world/kyoto/landmark_3.png` | 384x384 | sprite, transparent | remake | `714268f2fd34` |
| `assets/world/kyoto/landmark_4.png` | 384x384 | sprite, transparent | remake | `d6dcbfcd9e57` |
| `assets/world/kyoto/landmark_5.png` | 384x384 | sprite, transparent | remake | `17e3b74eeb37` |
| `assets/world/kyoto/landmark_6.png` | 384x384 | sprite, transparent | remake | `49d07b1809b9` |
| `assets/world/kyoto/landmark_7.png` | 384x384 | sprite, transparent | remake | `a7dae605475c` |
| `assets/world/kyoto/module_0.png` | 384x384 | sprite, transparent | remake | `7d538ba3ba78` |
| `assets/world/kyoto/module_1.png` | 384x384 | sprite, transparent | remake | `e4f558f83ab5` |
| `assets/world/kyoto/module_2.png` | 384x384 | sprite, transparent | remake | `12b1b571c44e` |
| `assets/world/kyoto/module_3.png` | 384x384 | sprite, transparent | remake | `8a38dca504d2` |
| `assets/world/kyoto/module_4.png` | 384x384 | sprite, transparent | remake | `1193f8608bae` |
| `assets/world/kyoto/module_5.png` | 384x384 | sprite, transparent | remake | `4d0957835da9` |
| `assets/world/kyoto/module_6.png` | 384x384 | sprite, transparent | remake | `2b109e025351` |
| `assets/world/kyoto/module_7.png` | 384x384 | sprite, transparent | remake | `2f4319b62a6f` |

### Beirut

**Loaded by:** `scripts/world/NeoKyotoWorld.cs` (848), `scripts/world/WorldChunkView.cs` (32), `scripts/world/cities/CityBuilder.cs` (80)

**Also deleted (do not regenerate):** `assets/world/beirut/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/world/beirut/landmark_0.png` | 384x384 | sprite, transparent | remake | `a6916f33d94b` |
| `assets/world/beirut/landmark_1.png` | 384x384 | sprite, transparent | remake | `e27f201cf724` |
| `assets/world/beirut/landmark_2.png` | 384x384 | sprite, transparent | remake | `04194130aeee` |
| `assets/world/beirut/landmark_3.png` | 384x384 | sprite, transparent | remake | `5fc06bccc394` |
| `assets/world/beirut/landmark_4.png` | 384x384 | sprite, transparent | remake | `083b006534b8` |
| `assets/world/beirut/landmark_5.png` | 384x384 | sprite, transparent | remake | `5ed9356d4937` |
| `assets/world/beirut/landmark_6.png` | 384x384 | sprite, transparent | remake | `205c73fb197a` |
| `assets/world/beirut/landmark_7.png` | 384x384 | sprite, transparent | remake | `c2bd03b4be1d` |
| `assets/world/beirut/module_0.png` | 384x384 | sprite, transparent | remake | `6df43f4d4094` |
| `assets/world/beirut/module_1.png` | 384x384 | sprite, transparent | remake | `0ed168a7385b` |
| `assets/world/beirut/module_2.png` | 384x384 | sprite, transparent | remake | `fcad9fc8a502` |
| `assets/world/beirut/module_3.png` | 384x384 | sprite, transparent | remake | `73f7a697c850` |
| `assets/world/beirut/module_4.png` | 384x384 | sprite, transparent | remake | `e7b2073c3601` |
| `assets/world/beirut/module_5.png` | 384x384 | sprite, transparent | remake | `e6c1f9969305` |
| `assets/world/beirut/module_6.png` | 384x384 | sprite, transparent | remake | `7fda9d00af6b` |
| `assets/world/beirut/module_7.png` | 384x384 | sprite, transparent | remake | `9adfb156c78c` |

### Dubai

**Loaded by:** `scripts/world/NeoKyotoWorld.cs` (848), `scripts/world/WorldChunkView.cs` (32), `scripts/world/cities/CityBuilder.cs` (80)

**Also deleted (do not regenerate):** `assets/world/dubai/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/world/dubai/landmark_0.png` | 384x384 | sprite, transparent | remake | `5b8c58efd65d` |
| `assets/world/dubai/landmark_1.png` | 384x384 | sprite, transparent | remake | `6a9abfc5c7d1` |
| `assets/world/dubai/landmark_2.png` | 384x384 | sprite, transparent | remake | `545d9c6e036a` |
| `assets/world/dubai/landmark_3.png` | 384x384 | sprite, transparent | remake | `cd4628b899fb` |
| `assets/world/dubai/landmark_4.png` | 384x384 | sprite, transparent | remake | `f0a3a0cd64d9` |
| `assets/world/dubai/landmark_5.png` | 384x384 | sprite, transparent | remake | `da2d5b912bfa` |
| `assets/world/dubai/landmark_6.png` | 384x384 | sprite, transparent | remake | `ab4664213c06` |
| `assets/world/dubai/landmark_7.png` | 384x384 | sprite, transparent | remake | `adda7a98dad9` |
| `assets/world/dubai/module_0.png` | 384x384 | sprite, transparent | remake | `8646eac21991` |
| `assets/world/dubai/module_1.png` | 384x384 | sprite, transparent | remake | `8cb049c69e97` |
| `assets/world/dubai/module_2.png` | 384x384 | sprite, transparent | remake | `55596395b4b7` |
| `assets/world/dubai/module_3.png` | 384x384 | sprite, transparent | remake | `d9a5d9514e52` |
| `assets/world/dubai/module_4.png` | 384x384 | sprite, transparent | remake | `4219709ccd64` |
| `assets/world/dubai/module_5.png` | 384x384 | sprite, transparent | remake | `deb67ec24000` |
| `assets/world/dubai/module_6.png` | 384x384 | sprite, transparent | remake | `0f9088355236` |
| `assets/world/dubai/module_7.png` | 384x384 | sprite, transparent | remake | `86e9c74b48a1` |

### Honolulu

**Loaded by:** `scripts/world/NeoKyotoWorld.cs` (848), `scripts/world/WorldChunkView.cs` (32), `scripts/world/cities/CityBuilder.cs` (80)

**Also deleted (do not regenerate):** `assets/world/honolulu/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/world/honolulu/landmark_0.png` | 384x384 | sprite, transparent | remake | `bc8d1f466915` |
| `assets/world/honolulu/landmark_1.png` | 384x384 | sprite, transparent | remake | `95ee899f8700` |
| `assets/world/honolulu/landmark_2.png` | 384x384 | sprite, transparent | remake | `17433d527b3b` |
| `assets/world/honolulu/landmark_3.png` | 384x384 | sprite, transparent | remake | `12aaffd16170` |
| `assets/world/honolulu/landmark_4.png` | 384x384 | sprite, transparent | remake | `3dc6b45d9076` |
| `assets/world/honolulu/landmark_5.png` | 384x384 | sprite, transparent | remake | `4ef00446dbc8` |
| `assets/world/honolulu/landmark_6.png` | 384x384 | sprite, transparent | remake | `148dddae3ed3` |
| `assets/world/honolulu/landmark_7.png` | 384x384 | sprite, transparent | remake | `f09833469aa6` |
| `assets/world/honolulu/module_0.png` | 384x384 | sprite, transparent | remake | `e8e98df9ee2d` |
| `assets/world/honolulu/module_1.png` | 384x384 | sprite, transparent | remake | `2d0c333f5d9e` |
| `assets/world/honolulu/module_2.png` | 384x384 | sprite, transparent | remake | `128cc09b1d31` |
| `assets/world/honolulu/module_3.png` | 384x384 | sprite, transparent | remake | `2c83290406b7` |
| `assets/world/honolulu/module_4.png` | 384x384 | sprite, transparent | remake | `56076c920dcb` |
| `assets/world/honolulu/module_5.png` | 384x384 | sprite, transparent | remake | `0be89a92fd8f` |
| `assets/world/honolulu/module_6.png` | 384x384 | sprite, transparent | remake | `06b59f1655f1` |
| `assets/world/honolulu/module_7.png` | 384x384 | sprite, transparent | remake | `2cdcdd3305f9` |

### Luanda

**Loaded by:** `scripts/world/NeoKyotoWorld.cs` (848), `scripts/world/WorldChunkView.cs` (32), `scripts/world/cities/CityBuilder.cs` (80)

**Also deleted (do not regenerate):** `assets/world/luanda/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/world/luanda/landmark_0.png` | 384x384 | sprite, transparent | remake | `7a439abfb6c7` |
| `assets/world/luanda/landmark_1.png` | 384x384 | sprite, transparent | remake | `d604ab7ce69d` |
| `assets/world/luanda/landmark_2.png` | 384x384 | sprite, transparent | remake | `cbb2d9b9781c` |
| `assets/world/luanda/landmark_3.png` | 384x384 | sprite, transparent | remake | `0b9fa5d2fecc` |
| `assets/world/luanda/landmark_4.png` | 384x384 | sprite, transparent | remake | `fc7bbadc01f1` |
| `assets/world/luanda/landmark_5.png` | 384x384 | sprite, transparent | remake | `5e78da7144ce` |
| `assets/world/luanda/landmark_6.png` | 384x384 | sprite, transparent | remake | `8299385af5b0` |
| `assets/world/luanda/landmark_7.png` | 384x384 | sprite, transparent | remake | `1ffcb2cc43de` |
| `assets/world/luanda/module_0.png` | 384x384 | sprite, transparent | remake | `efed72ae7238` |
| `assets/world/luanda/module_1.png` | 384x384 | sprite, transparent | remake | `ca51649f0dec` |
| `assets/world/luanda/module_2.png` | 384x384 | sprite, transparent | remake | `14053e3abc67` |
| `assets/world/luanda/module_3.png` | 384x384 | sprite, transparent | remake | `ac2fdc03c8e8` |
| `assets/world/luanda/module_4.png` | 384x384 | sprite, transparent | remake | `f929caa4f96e` |
| `assets/world/luanda/module_5.png` | 384x384 | sprite, transparent | remake | `cd2d14fcd501` |
| `assets/world/luanda/module_6.png` | 384x384 | sprite, transparent | remake | `19d93bc2c53b` |
| `assets/world/luanda/module_7.png` | 384x384 | sprite, transparent | remake | `dd193374fc6a` |

### Mumbai

**Loaded by:** `scripts/world/NeoKyotoWorld.cs` (848), `scripts/world/WorldChunkView.cs` (32), `scripts/world/cities/CityBuilder.cs` (80)

**Also deleted (do not regenerate):** `assets/world/mumbai/manifest.json`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/world/mumbai/landmark_0.png` | 384x384 | sprite, transparent | remake | `4c63b1146846` |
| `assets/world/mumbai/landmark_1.png` | 384x384 | sprite, transparent | remake | `0520a39d309b` |
| `assets/world/mumbai/landmark_2.png` | 384x384 | sprite, transparent | remake | `fefa6124d618` |
| `assets/world/mumbai/landmark_3.png` | 384x384 | sprite, transparent | remake | `109bbe0fd4d0` |
| `assets/world/mumbai/landmark_4.png` | 384x384 | sprite, transparent | remake | `5c8e042374b9` |
| `assets/world/mumbai/landmark_5.png` | 384x384 | sprite, transparent | remake | `93a47023bd4b` |
| `assets/world/mumbai/landmark_6.png` | 384x384 | sprite, transparent | remake | `8f73abc68df0` |
| `assets/world/mumbai/landmark_7.png` | 384x384 | sprite, transparent | remake | `b643de21f4c2` |
| `assets/world/mumbai/module_0.png` | 384x384 | sprite, transparent | remake | `e18ce425e81f` |
| `assets/world/mumbai/module_1.png` | 384x384 | sprite, transparent | remake | `87fa8fc06b09` |
| `assets/world/mumbai/module_2.png` | 384x384 | sprite, transparent | remake | `ddef68e8caec` |
| `assets/world/mumbai/module_3.png` | 384x384 | sprite, transparent | remake | `d53d55651166` |
| `assets/world/mumbai/module_4.png` | 384x384 | sprite, transparent | remake | `63e64da95eb5` |
| `assets/world/mumbai/module_5.png` | 384x384 | sprite, transparent | remake | `91a2f0be1f17` |
| `assets/world/mumbai/module_6.png` | 384x384 | sprite, transparent | remake | `cb5019b531f5` |
| `assets/world/mumbai/module_7.png` | 384x384 | sprite, transparent | remake | `a8fe0766e0c9` |

## Phase 7: Menus, Level 1 map and titles

### Menu backdrops

**Shows:**
- **City menu:** a rainy night skyline. The game adds rain and transit lights.
- **Command bay:** an interior. The game adds scan arcs and data motes.

Each menu has three 1920x1080 layers: `_base` (opaque), `_structure` (drifts for parallax) and `_lights` (pulses). They load by path in `animated_menu_backdrop.gd`.

**Method:** `generate_game_art` at 16:9 for the base, then structure and lights as edits of it (`edit_asset_id`).

**Loaded by:** `scripts/ui/animated_menu_backdrop.gd` (7, 28, 39)

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/images/menu_backdrops/city_base.png` | 1920x1080 | opaque base plate | remake | `5795434161e0` |
| `assets/images/menu_backdrops/city_lights.png` | 1920x1080 | transparent lights layer | remake | `44d2ffff9dc3` |
| `assets/images/menu_backdrops/city_structure.png` | 1920x1080 | transparent structure layer | remake | `244a767d90e9` |
| `assets/images/menu_backdrops/command_bay_base.png` | 1920x1080 | opaque base plate | remake | `887526ba30cb` |
| `assets/images/menu_backdrops/command_bay_lights.png` | 1920x1080 | transparent lights layer | remake | `4a855bd8d9c9` |
| `assets/images/menu_backdrops/command_bay_structure.png` | 1920x1080 | transparent structure layer | remake | `f96d61b1a131` |

### Level 1 isometric map

**Shows:** the Level 1 palace from an isometric view: the floor, a dojo, a garden, a demolition yard and the Data-Blade altar. 4608x1536, transparent background.

**Fixed positions:**
- The game draws the map upside down (`FlipV`) and stretched to 4800x1600, so store the image flipped vertically.
- `IsometricMission.BuildCoverCollisions` puts ten solid props at fixed points. The lanterns, trees and archive shelves in the image must sit on them: (1440, 1010), (1790, 1340), (2150, 1040), (1980, 1510), (2670, 1000), (2940, 1420), (3370, 1030), (4110, 1000), (4480, 1040), (4710, 1010) in the 4800x1600 space.
- Use the old image as `reference_asset_id`.

**Loaded by:** `scripts/gameplay/IsometricMission.cs` (67)

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/images/level1/ghost_gridlock.png` | 4608x1536 | isometric map, transparent background | remake | `6db2035c6bac` |

### Title logos (game)

**Shows:** the game title, 1920x588 and transparent: a gunmetal plate, cyan and magenta neon outlines, and a winged-gear crest.

- **Wording:** keep each old image's wording. Latin-script languages show the English "AXIOM FORCE". View the old file for each language.
- **Arabic and Hebrew:** need correct letter joining and right-to-left order.
- **Files to make:** only the 17 marked `remake`, one per language in `LocaleCatalog`. The website copies come from these.
- **Route:** `spritecook-build-ui-kits`, with one approved English title and each language as an edit of it.

**Loaded by:** `scripts/main_menu/GameHome.cs` (33), `scripts/ui/LoadingScreen.cs` (214), `scripts/ui/TitleLogo.cs` (17, 20, 26), `website/src/translations/titleLogo.js` (10, 15, 19, 29, 39, 43, 47, 51, 55, 59, 63, 67, 71, 75, 79, 83, 87, 91)

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/images/axiom_force_title_2d.png` | 1920x588 | title wordmark, transparent | remake | `6bc08827e53b` |
| `assets/images/axiom_force_title_2d_ar.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `0c5296a48ffb` |
| `assets/images/axiom_force_title_2d_arabic.png` | 1920x588 | title wordmark, transparent | remake | `0c5296a48ffb` |
| `assets/images/axiom_force_title_2d_brazilianportuguese.png` | 1920x588 | title wordmark, transparent | remake | `6bc08827e53b` |
| `assets/images/axiom_force_title_2d_de.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `assets/images/axiom_force_title_2d_es.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `assets/images/axiom_force_title_2d_eurofrench.png` | 1920x588 | title wordmark, transparent | remake | `6bc08827e53b` |
| `assets/images/axiom_force_title_2d_fr.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `assets/images/axiom_force_title_2d_german.png` | 1920x588 | title wordmark, transparent | remake | `6bc08827e53b` |
| `assets/images/axiom_force_title_2d_he.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `59f96cc1a460` |
| `assets/images/axiom_force_title_2d_hebrew.png` | 1920x588 | title wordmark, transparent | remake | `59f96cc1a460` |
| `assets/images/axiom_force_title_2d_id.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `4c2fd5abba52` |
| `assets/images/axiom_force_title_2d_indonesian.png` | 1920x588 | title wordmark, transparent | remake | `4c2fd5abba52` |
| `assets/images/axiom_force_title_2d_it.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `assets/images/axiom_force_title_2d_italian.png` | 1920x588 | title wordmark, transparent | remake | `6bc08827e53b` |
| `assets/images/axiom_force_title_2d_ja.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `e6f2eb8e1064` |
| `assets/images/axiom_force_title_2d_japanese.png` | 1920x588 | title wordmark, transparent | remake | `e6f2eb8e1064` |
| `assets/images/axiom_force_title_2d_ko.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `986fba64dbbf` |
| `assets/images/axiom_force_title_2d_korean.png` | 1920x588 | title wordmark, transparent | remake | `986fba64dbbf` |
| `assets/images/axiom_force_title_2d_mandarin.png` | 1920x588 | title wordmark, transparent | remake | `2dee3e7a2e30` |
| `assets/images/axiom_force_title_2d_pl.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `assets/images/axiom_force_title_2d_polish.png` | 1920x588 | title wordmark, transparent | remake | `6bc08827e53b` |
| `assets/images/axiom_force_title_2d_pt.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `assets/images/axiom_force_title_2d_spanish.png` | 1920x588 | title wordmark, transparent | remake | `6bc08827e53b` |
| `assets/images/axiom_force_title_2d_th.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `d56fdd5ac6a3` |
| `assets/images/axiom_force_title_2d_thai.png` | 1920x588 | title wordmark, transparent | remake | `d56fdd5ac6a3` |
| `assets/images/axiom_force_title_2d_tr.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `assets/images/axiom_force_title_2d_traditionalchinese.png` | 1920x588 | title wordmark, transparent | remake | `6ca99514187c` |
| `assets/images/axiom_force_title_2d_turkish.png` | 1920x588 | title wordmark, transparent | remake | `6bc08827e53b` |
| `assets/images/axiom_force_title_2d_vi.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `ab8525146ac2` |
| `assets/images/axiom_force_title_2d_vietnamese.png` | 1920x588 | title wordmark, transparent | remake | `ab8525146ac2` |
| `assets/images/axiom_force_title_2d_zh.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `2dee3e7a2e30` |

### Title logos (website copies)

**Shows:** the same images as the game titles. Only the 17 files that `titleLogo.js` names are used; copy them from `assets/images`.

**Loaded by:** `website/src/components/TitleLogo.jsx` (21), `website/src/translations/titleLogo.js` (10, 15, 19, 29, 39, 43, 47, 51, 55, 59, 63, 67, 71, 75, 79, 83, 87, 91)

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `website/public/images/axiom_force_title_2d.png` | 1920x588 | title wordmark, transparent | copy of `assets/images/axiom_force_title_2d.png` | `6bc08827e53b` |
| `website/public/images/axiom_force_title_2d_ar.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `0c5296a48ffb` |
| `website/public/images/axiom_force_title_2d_arabic.png` | 1920x588 | title wordmark, transparent | copy of `assets/images/axiom_force_title_2d_arabic.png` | `0c5296a48ffb` |
| `website/public/images/axiom_force_title_2d_brazilianportuguese.png` | 1920x588 | title wordmark, transparent | copy of `assets/images/axiom_force_title_2d_brazilianportuguese.png` | `6bc08827e53b` |
| `website/public/images/axiom_force_title_2d_de.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/axiom_force_title_2d_en.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/axiom_force_title_2d_english.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/axiom_force_title_2d_es.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/axiom_force_title_2d_eurofrench.png` | 1920x588 | title wordmark, transparent | copy of `assets/images/axiom_force_title_2d_eurofrench.png` | `6bc08827e53b` |
| `website/public/images/axiom_force_title_2d_fr.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/axiom_force_title_2d_french.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/axiom_force_title_2d_german.png` | 1920x588 | title wordmark, transparent | copy of `assets/images/axiom_force_title_2d_german.png` | `6bc08827e53b` |
| `website/public/images/axiom_force_title_2d_he.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `59f96cc1a460` |
| `website/public/images/axiom_force_title_2d_hebrew.png` | 1920x588 | title wordmark, transparent | copy of `assets/images/axiom_force_title_2d_hebrew.png` | `59f96cc1a460` |
| `website/public/images/axiom_force_title_2d_id.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `4c2fd5abba52` |
| `website/public/images/axiom_force_title_2d_indonesian.png` | 1920x588 | title wordmark, transparent | copy of `assets/images/axiom_force_title_2d_indonesian.png` | `4c2fd5abba52` |
| `website/public/images/axiom_force_title_2d_it.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/axiom_force_title_2d_italian.png` | 1920x588 | title wordmark, transparent | copy of `assets/images/axiom_force_title_2d_italian.png` | `6bc08827e53b` |
| `website/public/images/axiom_force_title_2d_ja.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `e6f2eb8e1064` |
| `website/public/images/axiom_force_title_2d_japanese.png` | 1920x588 | title wordmark, transparent | copy of `assets/images/axiom_force_title_2d_japanese.png` | `e6f2eb8e1064` |
| `website/public/images/axiom_force_title_2d_ko.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `986fba64dbbf` |
| `website/public/images/axiom_force_title_2d_korean.png` | 1920x588 | title wordmark, transparent | copy of `assets/images/axiom_force_title_2d_korean.png` | `986fba64dbbf` |
| `website/public/images/axiom_force_title_2d_mandarin.png` | 1920x588 | title wordmark, transparent | copy of `assets/images/axiom_force_title_2d_mandarin.png` | `2dee3e7a2e30` |
| `website/public/images/axiom_force_title_2d_pl.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/axiom_force_title_2d_polish.png` | 1920x588 | title wordmark, transparent | copy of `assets/images/axiom_force_title_2d_polish.png` | `6bc08827e53b` |
| `website/public/images/axiom_force_title_2d_portuguese.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/axiom_force_title_2d_pt.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/axiom_force_title_2d_simplified_chinese.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `2dee3e7a2e30` |
| `website/public/images/axiom_force_title_2d_spanish.png` | 1920x588 | title wordmark, transparent | copy of `assets/images/axiom_force_title_2d_spanish.png` | `6bc08827e53b` |
| `website/public/images/axiom_force_title_2d_th.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `d56fdd5ac6a3` |
| `website/public/images/axiom_force_title_2d_thai.png` | 1920x588 | title wordmark, transparent | copy of `assets/images/axiom_force_title_2d_thai.png` | `d56fdd5ac6a3` |
| `website/public/images/axiom_force_title_2d_tr.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/axiom_force_title_2d_traditional_chinese.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6ca99514187c` |
| `website/public/images/axiom_force_title_2d_traditionalchinese.png` | 1920x588 | title wordmark, transparent | copy of `assets/images/axiom_force_title_2d_traditionalchinese.png` | `6ca99514187c` |
| `website/public/images/axiom_force_title_2d_turkish.png` | 1920x588 | title wordmark, transparent | copy of `assets/images/axiom_force_title_2d_turkish.png` | `6bc08827e53b` |
| `website/public/images/axiom_force_title_2d_vi.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `ab8525146ac2` |
| `website/public/images/axiom_force_title_2d_vietnamese.png` | 1920x588 | title wordmark, transparent | copy of `assets/images/axiom_force_title_2d_vietnamese.png` | `ab8525146ac2` |
| `website/public/images/axiom_force_title_2d_zh.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `2dee3e7a2e30` |
| `website/public/images/axiom_force_title_2d_zh_tw.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6ca99514187c` |
| `website/public/images/title_logos/axiom_force_title_2d.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/title_logos/axiom_force_title_2d_ar.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `0c5296a48ffb` |
| `website/public/images/title_logos/axiom_force_title_2d_arabic.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `0c5296a48ffb` |
| `website/public/images/title_logos/axiom_force_title_2d_brazilianportuguese.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/title_logos/axiom_force_title_2d_de.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/title_logos/axiom_force_title_2d_en.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/title_logos/axiom_force_title_2d_english.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/title_logos/axiom_force_title_2d_es.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/title_logos/axiom_force_title_2d_eurofrench.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/title_logos/axiom_force_title_2d_fr.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/title_logos/axiom_force_title_2d_french.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/title_logos/axiom_force_title_2d_german.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/title_logos/axiom_force_title_2d_he.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `59f96cc1a460` |
| `website/public/images/title_logos/axiom_force_title_2d_hebrew.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `59f96cc1a460` |
| `website/public/images/title_logos/axiom_force_title_2d_id.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `4c2fd5abba52` |
| `website/public/images/title_logos/axiom_force_title_2d_indonesian.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `4c2fd5abba52` |
| `website/public/images/title_logos/axiom_force_title_2d_it.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/title_logos/axiom_force_title_2d_italian.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/title_logos/axiom_force_title_2d_ja.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `e6f2eb8e1064` |
| `website/public/images/title_logos/axiom_force_title_2d_japanese.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `e6f2eb8e1064` |
| `website/public/images/title_logos/axiom_force_title_2d_ko.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `986fba64dbbf` |
| `website/public/images/title_logos/axiom_force_title_2d_korean.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `986fba64dbbf` |
| `website/public/images/title_logos/axiom_force_title_2d_mandarin.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `2dee3e7a2e30` |
| `website/public/images/title_logos/axiom_force_title_2d_pl.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/title_logos/axiom_force_title_2d_polish.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/title_logos/axiom_force_title_2d_portuguese.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/title_logos/axiom_force_title_2d_pt.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/title_logos/axiom_force_title_2d_simplified_chinese.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `2dee3e7a2e30` |
| `website/public/images/title_logos/axiom_force_title_2d_spanish.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/title_logos/axiom_force_title_2d_th.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `d56fdd5ac6a3` |
| `website/public/images/title_logos/axiom_force_title_2d_thai.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `d56fdd5ac6a3` |
| `website/public/images/title_logos/axiom_force_title_2d_tr.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/title_logos/axiom_force_title_2d_traditional_chinese.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6ca99514187c` |
| `website/public/images/title_logos/axiom_force_title_2d_traditionalchinese.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6ca99514187c` |
| `website/public/images/title_logos/axiom_force_title_2d_turkish.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6bc08827e53b` |
| `website/public/images/title_logos/axiom_force_title_2d_vi.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `ab8525146ac2` |
| `website/public/images/title_logos/axiom_force_title_2d_vietnamese.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `ab8525146ac2` |
| `website/public/images/title_logos/axiom_force_title_2d_zh.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `2dee3e7a2e30` |
| `website/public/images/title_logos/axiom_force_title_2d_zh_tw.png` | 1920x588 | title wordmark, transparent | skip: unused alias | `6ca99514187c` |

## Not to regenerate

### Old Blender title preview (unused)

Unused early test render.

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|
| `assets/images/blender_title_preview.png` | 1280x600 | title wordmark, transparent | skip: unused | `6d29daad9aa9` |

### Cutscene: deployment video (legacy)

The legacy video `deployment.ogv` is retired: the live deployment cutscene replaced it, and no code loads it. Do not remake it.

**Also deleted (do not regenerate):** `assets/cutscenes/deployment/deployment.ogv`, `assets/cutscenes/deployment/deployment.ogv.uid`

| File | Size | Role | Status | Old sha256 (first 12) |
|---|---|---|---|---|

## Deleted but not on this list

The downloaded stand-in packs (`assets/stand_in/`) and the Pixel Adventure leftovers in `assets/images` were deleted on 2026-11-11 at the owner's request. They were never Blender art; `docs/STAND_IN_ASSETS.md` records their sources.

## Kept (not made by Blender)

These are unchanged:
- the hand-drawn and AI art in `assets/characters/hand_drawn` and `assets/characters/shogun_study`;
- the painted Level 1 backdrop (`palace_garden_painted.png`) and the story image;
- the painted `axiom_force_title_logo*.jpg` files;
- the fonts;
- all cutscene audio, cue and dialogue files.

## Deleted on 2026-11-12

On the owner's instruction, every image left after the Blender deletion was removed. The originals are at commit `67eab0a`.

**Groups in `docs/art_regeneration.json`** (each image has its size, status and `sha12`):

| Group | Files |
|---|---|
| `hand_drawn_characters` | 9 |
| `legacy_logos_game` | 32 |
| `legacy_website_images` | 56 |
| `ui_and_backgrounds` | 14 |
| `app_icons` | 4 |

**To remake (9).** Each is loaded by path, so a file saved at the same path and size returns with no code change:

| File | Size | Loaded by | Until it returns |
|---|---|---|---|
| `assets/images/cyber_button_normal.png`, `_hover.png`, `_pressed.png` | 800x140 each | `scripts/ui/CyberButton.cs` (`OptionalArt`) | buttons show text only |
| `assets/textures/cyber_speech_frame.png`, `cyber_speech_pointer.png` | 512x320 and 128x128 | `scripts/ui/CyberDialogueBubble.cs` (`OptionalArt`) | bubble has no frame or pointer |
| `assets/images/level1/palace_garden_painted.png` | 2149x732 | `PalaceMission.BackdropPath` | procedural skyline |
| `assets/icons/axiom_force.png`, `assets/icons/axiom_force.ico` | 1254x1254 PNG; ICO up to 256x256 | **re-add** `config/icon` and `config/windows_native_icon` to `project.godot`, and `application/icon` and `icons/icon_1024x1024` to `export_presets.cfg` | default Godot icon |
| `website/public/favicon.svg` | SVG, 100x100 viewBox | **re-add** `<link rel="icon" type="image/svg+xml" href="/favicon.svg" />` to `website/index.html` and `index.html` | browser default icon |

**Skipped (106):**
- **Superseded:** the hand-drawn studies (`assets/characters/hand_drawn`, `assets/characters/shogun_study`), replaced by the pixel atlases. The code that drew them (`TrainingArtwork`, `OperativeStudyAtlas`, the hand-drawn branch of `ShogunArtPreview`, Level 1's `campaign_atlas` frames) was removed, and every mission and the selection preview now use `OperativeVisuals`.
- **Unused:** the older title-logo JPGs and emblems, the website's title-logo copies, `hero.png`, `phoenicia.png`, `icons.svg` and the React/Vite template images, plus `sky`, `clouds`, `world_tileset`, `story-screen-image`, the cursors, the engine plume and slot, and the default `icon.svg`.

