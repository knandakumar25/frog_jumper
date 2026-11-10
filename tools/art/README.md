# Animated menu art

`render_menu_backdrops.py` authors flat polygons in Blender with an orthographic
camera and emission materials. No models, lights, perspective cameras, or Blender
files are loaded by the game. The output is six 1920 × 1080 PNG layers (about
48 MiB of uncompressed textures across both screens; only one screen is active).

Regenerate using Blender's Python runtime and absolute paths:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background `
  --python 'D:\Projects\frog_jumper\tools\art\render_menu_backdrops.py' -- `
  --output 'D:\Projects\frog_jumper\assets\images\menu_backdrops' `
  --preview 'D:\Projects\frog_jumper\.validation\menu-backdrops'
```

Import the new PNGs with the Godot headless editor before running the game.
`scripts/ui/animated_menu_backdrop.gd` draws the layers with aspect-preserving
overscan, gentle parallax, breathing illumination, rain/transit lights for the
city, and rotating scan arcs/data motes for the command bay. It ignores input,
does not load resources during animation, and stops advancing when hidden.
Disable the exported `motion_enabled` setting for a still backdrop.

`capture_menu_backdrops.gd` captures both actual menus at two animation times
to `.validation/menu-backdrops`. Run it through Godot with a real rendering driver;
headless mode cannot capture rendered UI. Preview files are deliberately outside
the game assets.

## Render quality and title logos

`render_quality.py` holds the shared Cycles settings. `apply_flat` (16 samples, 1.5 px
Blackman-Harris filter, no denoiser) is used by the `render()` above, so by every cutscene
and later-campaign generator that imports it, and by `tools/gameplay/render_wildfire.render`
for the gameplay layers. `apply_lit` (512 samples, no denoiser, so Blender 4.3 and 5.2 use
the same settings; their output has not been compared) is used by
`tools/world/build_kit.py` and the title logos. `draw_flat_characters.py` keeps its own
settings. A Cycles re-render of the menu backdrops looked identical to the older EEVEE
files, so the EEVEE files were kept.

`render_title_logo.py` renders every localized `axiom_force_title_2d*.png` (1920 x 588)
from one 3D scene and also refreshes the website copies:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --factory-startup `
  --python-exit-code 1 --python tools/art/render_title_logo.py -- [--only arabic hebrew] [--preview]
```

`--preview` writes half-size images to `.validation/title_preview` and never touches
shipped files. The plate renders once; each title's text renders alone, and its cyan and
magenta rims are grown from the text's alpha in numpy. 3D outline growth threw spikes on
sharp corners (Arabic, Hebrew, Japanese) and misfilled the Korean font's overlapping
outlines. Intermediate layers go to `.validation/title_layers`. Before rendering anything,
the script stops if a font lacks a glyph or the Arabic shaper meets text it cannot shape.
See `docs/ASSET_QUALITY_AUDIT.md`.
