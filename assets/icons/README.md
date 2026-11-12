# Axiom Force application icon

The icon images were deleted on 2026-11-12 with the rest of the project art. The originals are in git at commit `67eab0a`. Until a new icon exists, the game window, taskbar and exports use Godot's default icon.

The new icon is made with SpriteCook; see `docs/ART_REGENERATION.md` ("Deleted on 2026-11-12"). To restore it:

1. Save the SpriteCook PNG as `assets/icons/axiom_force.png` (the old one was 1254x1254).
2. Build the Windows ICO from it with Pillow:

   ```python
   from PIL import Image

   Image.open('assets/icons/axiom_force.png').convert('RGBA').save(
       'assets/icons/axiom_force.ico',
       sizes=[(s, s) for s in (16, 32, 48, 64, 128, 256)],
   )
   ```

3. Re-add the references that were removed:
   - `project.godot`, under `[application]`: `config/icon="res://assets/icons/axiom_force.png"` and `config/windows_native_icon="res://assets/icons/axiom_force.ico"`.
   - `export_presets.cfg`: `application/icon="res://assets/icons/axiom_force.ico"` in both Windows presets and `icons/icon_1024x1024="res://assets/icons/axiom_force.png"` in the macOS preset.

The old design, for reference: an A-shaped insignia of angular cyan blade strokes with a violet energy crossbar, on a midnight navy rounded square. Flat, strong silhouette, no words.
