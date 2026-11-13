# Art tools

`fit_spritecook_image.py` crops/fits downloaded art to exact project sizes.
`render_titles.py <plate.png>` typesets the 17 supported title variants and writes
game/web copies; it requires Pillow with libraqm. Its crop box was fitted to the deleted
2026-11-12 plate, so adjust it to the new plate before use. `finish_ui_batch.py` belongs to
the deleted 2026-11-12 UI batch (its crops and asset IDs no longer apply); do not run it as is.

## Adventurer-plan tools (2026-11-13)

These make one paid generation cover many files, or make files with no generation at all.
Each image's `method` in `docs/art_regeneration.json` says which tool makes it (see
`docs/ART_REGENERATION.md`, "Methods"). They read local files only and spend no credits.
Run each with `uv run --system-certs --no-project --with pillow python tools/art/<tool>.py --help`.

- `slice_sheet.py`: cuts a 2x2 (4K scene) or 3x3/4x4 (2K sprite) grid sheet into exact-size files. It removes the gutter where cells bleed, keys out the magenta background, trims sprites, and fits with `fit_spritecook_image.fit`. Base plates are flattened to opaque.
- `derive_layers.py`:
  - `lights` makes a strictly 0/255-alpha lights layer from the bright, saturated pixels of a layer.
  - `aftermath` grades an environment layer into its aftermath version, with a repeatable seed for the embers.
  - `drone` places the `vanguard` idle frame inside the deployment drone box.
  - `icon` writes the ICO and SVG favicon from the icon PNG.
- `compose_poses.py <spec.json>`: builds a cutscene pose sheet (512 cells, feet on the pivot) from Phase 1 atlas frames plus trimmed pose images, following a JSON layout spec.
- `pack_topdown.py`: builds `assets/world/operative_<n>.png` (4x8 frames of 96x128, feet on y=87) from south, north and west walk strips. East is mirrored from west and the diagonals reuse the side views.

Blender was retired on 2026-11-11. The Blender generators that used to live here (character sprites, menu backdrops, the Level 1 map, title logos and their shared render settings) were deleted with the art they made. New art comes from SpriteCook; see `docs/ART_REGENERATION.md` for what to remake and the "Art pipeline" rule in `CLAUDE.md`.

What remains:

- `pack_spritecook_character.py` packs SpriteCook pixel-art animation strips into `assets/characters/rendered/<id>_atlas.png`. The layout is 256x256 cells, eight frames per row, feet on y=224, one whole-number nearest-neighbour scale per character, and each row anchored on its first frame. It can also write the portrait by enlarging the base still. The game's `.tres` files are restored unchanged from `ca92679` and cut that atlas. Run it with `uv run --system-certs --no-project --with pillow python tools/art/pack_spritecook_character.py --help`.
- `capture_menu_backdrops.gd`, `capture_character_renders.gd` and `capture_operative_studies.gd` capture the real menus in Godot to `.validation/`.
- The hand-drawn study art, `prepare_campaign_shogun.py` and `capture_shogun_study.gd` were deleted on 2026-11-12. Run them with a real rendering driver; headless mode cannot capture rendered UI. Use them to review SpriteCook art in place.

## Menu backdrop layers

`scripts/ui/animated_menu_backdrop.gd` draws three 1920x1080 layers per menu with aspect-preserving overscan, gentle parallax, breathing illumination, rain and transit lights for the city, and scan arcs and data motes for the command bay. It loads `assets/images/menu_backdrops/<city|command_bay>_<base|structure|lights>.png` by path in `_ready`, skips any layer that is missing, and stops advancing when hidden. Disable the exported `motion_enabled` setting for a still backdrop.
