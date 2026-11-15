"""Cut the 2026-11-15 SpriteCook UI sheet into the game's button, speech and icon files.

Usage (from the repository root, with Pillow, numpy and scipy):

    uv run --system-certs --no-project --with pillow --with numpy --with scipy \n        python tools/art/cut_ui_sheet.py RAW_SHEET .

RAW_SHEET is the raw_url download of SpriteCook asset 99c9f6ae-b59e-4016-8423-98779b38699f.
Writes the three cyber_button_*.png files, cyber_speech_frame.png, cyber_speech_pointer.png and
assets/icons/axiom_force.png; run tools/art/derive_layers.py icon afterwards for the ICO and favicon.

The 2048x2048 raw sheet is snapped (unkeyed) to its 200x200 logical pixel grid with
tools/art/snap_pixels.snap. Each element's background is then removed by flood fill from the
element box border over magenta cells only, so the magenta accent stripe inside the hover button
survives. Outputs use nearest-neighbour scaling with an integer factor; shapes whose aspect differs
from the target are rebuilt nine-slice style (corners kept, plain middles stretched or trimmed).
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

raw, root = Path(sys.argv[1]), Path(sys.argv[2])
sys.path.insert(0, str(root / "tools/art"))
from snap_pixels import snap  # noqa: E402

G = 200
K = 2048 / G  # raw pixels per logical pixel
grid = np.asarray(snap(Image.open(raw), (G, G), "none", 64, 60))


def element(x, y, w, h):
    """Logical-pixel RGBA crop of a raw-space box, magenta outside the art made transparent."""
    x0, y0 = int(x / K) - 1, int(y / K) - 1
    x1, y1 = int(np.ceil((x + w) / K)) + 1, int(np.ceil((y + h) / K)) + 1
    cell = grid[y0:y1, x0:x1].copy()
    r, g, b = (cell[..., i].astype(int) for i in range(3))
    magenta = (r > 150) & (b > 150) & (g < 110)
    lab, _ = ndimage.label(magenta)
    edge = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
    cell[np.isin(lab, list(edge)), 3] = 0
    box = Image.fromarray(cell, "RGBA").getbbox()
    return Image.fromarray(cell, "RGBA").crop(box)


def nine_slice(img, size, border, border_y=None):
    """Rebuild ``img`` at ``size`` keeping ``border`` px corners; edges and centre are stretched."""
    w, h = img.size
    W, H = size
    b = border
    by = border if border_y is None else border_y
    out = Image.new("RGBA", size, (0, 0, 0, 0))
    xs = [(0, b, 0, b), (b, w - b, b, W - b), (w - b, w, W - b, W)]
    ys = [(0, by, 0, by), (by, h - by, by, H - by), (h - by, h, H - by, H)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            part = img.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.Resampling.NEAREST)
            out.alpha_composite(part, (dx0, dy0))
    return out


def scale(img, k):
    """Enlarge ``img`` by the whole number ``k`` with nearest-neighbour (keeps pixel blocks square)."""
    return img.resize((img.width * k, img.height * k), Image.Resampling.NEAREST)


images = root / "assets/images"
# Buttons: 28 logical rows at 5x = 140 px; the plain middle is trimmed to reach 800 px wide.
for name, box in (("normal", (81, 61, 1886, 288)), ("hover", (81, 389, 1886, 287)),
                  ("pressed", (81, 716, 1886, 288))):
    art = element(*box)
    k = 140 // art.height
    big = scale(art, k)
    big = nine_slice(big, (800, 140), 70, 16)
    big.save(images / f"cyber_button_{name}.png", optimize=True)
    print(name, art.size, "x", k)

# Speech frame: logical art at 4x, rebuilt to 512x320 with 48 px corners (Godot patch margin is 40).
frame = scale(element(102, 1044, 1148, 492), 4)
nine_slice(frame, (512, 320), 48).save(root / "assets/textures/cyber_speech_frame.png", optimize=True)
print("frame", frame.size)

# Pointer: 3x, turned to point left like the old one (it sits on the bubble's left edge), centred
# in a 128x128 transparent square.
pointer = scale(element(1403, 1208, 359, 185), 3).rotate(-90, expand=True)
canvas = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
canvas.alpha_composite(pointer, ((128 - pointer.width) // 2, (128 - pointer.height) // 2))
canvas.save(root / "assets/textures/cyber_speech_pointer.png", optimize=True)
print("pointer", pointer.size)

# Icon: largest whole multiple that fits 1254, centred on a transparent square.
icon = element(818, 1576, 412, 411)
k = 1254 // max(icon.size)
big = scale(icon, k)
canvas = Image.new("RGBA", (1254, 1254), (0, 0, 0, 0))
canvas.alpha_composite(big, ((1254 - big.width) // 2, (1254 - big.height) // 2))
(root / "assets/icons").mkdir(exist_ok=True)
canvas.save(root / "assets/icons/axiom_force.png", optimize=True)
print("icon", icon.size, "x", k)
