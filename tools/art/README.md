# Art tools

Blender was retired on 2026-11-11. The Blender generators that used to live here (character sprites, menu backdrops, the Level 1 map, title logos and their shared render settings) were deleted with the art they made. New art comes from SpriteCook; see `docs/ART_REGENERATION.md` for what to remake and the "Art pipeline" rule in `CLAUDE.md`.

What remains:

- `prepare_campaign_shogun.py` builds `assets/characters/shogun_study/campaign_atlas.png` from the AI-generated Shogun pose sheets with Pillow. It does not use Blender.
- `capture_menu_backdrops.gd`, `capture_character_renders.gd`, `capture_operative_studies.gd` and `capture_shogun_study.gd` capture the real menus in Godot to `.validation/`. Run them with a real rendering driver; headless mode cannot capture rendered UI. Use them to review SpriteCook art in place.

## Menu backdrop layers

`scripts/ui/animated_menu_backdrop.gd` draws three 1920x1080 layers per menu with aspect-preserving overscan, gentle parallax, breathing illumination, rain and transit lights for the city, and scan arcs and data motes for the command bay. It loads `assets/images/menu_backdrops/<city|command_bay>_<base|structure|lights>.png` by path in `_ready`, skips any layer that is missing, and stops advancing when hidden. Disable the exported `motion_enabled` setting for a still backdrop.
