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
