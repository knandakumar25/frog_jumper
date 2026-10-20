# Axiom Force application icon

`axiom_force.png` is the original generated artwork used by the project.
`axiom_force.ico` packages the same artwork at 16, 32, 48, 64, 128,
and 256 pixels for the Windows executable and taskbar. Both Windows
export presets explicitly use it; other exports inherit the project icon
where supported by the platform.

Created with the built-in image generation tool. Design prompt: a bold
geometric A-shaped insignia formed from angular cyan blade strokes and a
violet energy crossbar, centered on a midnight navy rounded-square tile;
flat graphic design, strong silhouette, restrained luminous accents,
legible at small sizes, no words or scenery.

To regenerate the ICO from the PNG using Pillow:

```python
from PIL import Image

Image.open('assets/icons/axiom_force.png').convert('RGBA').save(
    'assets/icons/axiom_force.ico',
    sizes=[(s, s) for s in (16, 32, 48, 64, 128, 256)],
)
```
