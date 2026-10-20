# Original flat 2D character artwork

The character artwork is drawn from scratch by `draw_flat_characters.py`.
It does not read existing game artwork, models, archived Git assets, or imported
rigs. Shapes are planar polygons with ink outlines and solid emission colors;
an orthographic camera records the drawings without perspective or lighting.
Animation uses explicit 2D joint positions and shape changes.

Six operative silhouettes have distinct headwear, clothing and equipment:
samurai and katana, solar shield bearer, water lancer, hooded dual-blade scout,
heavy gauntlet fighter, and prism weaver. AXIOM-7 and the two enemy classes use
the same flat drawing vocabulary. No recovered 3D models are needed.

Regenerate from the project root:

```powershell
& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --python tools/art/draw_flat_characters.py
python tools/art/package_character_sprites.py
```

Output remains in `assets/characters/rendered` for compatibility with the
existing scene references. Each operative atlas has six rows of eight 256px
frames: idle, running, jumping, blade dash, sword attack, and martial strike.
Attack durations retain the controller's existing timing. PNGs are transparent.
Portraits are 768x1024; support portraits are 768x768.

Review the generated `.validation/flat_characters/lineup.png` and
`animations.gif`. These previews contain only the new drawings.
Runtime uses SpriteFrames and AnimatedSprite2D; no character model is loaded.
The accepted menu backgrounds and game icon are independent of this pipeline.

Technical web reference:
[Blender orthographic cameras](https://docs.blender.org/manual/en/4.4/render/cameras.html).
No artwork reference was used.
