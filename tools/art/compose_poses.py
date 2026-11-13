"""Build cutscene pose sheets from Phase 1 animation frames and extra pose images.

Cutscene pose sheets (``poses.png``, ``surya.png``, the deployment ``shogun.png``) use
512x512 cells with each figure's feet on a pivot, by default (256, 448) (see
``docs/ART_REGENERATION.md``, "Phases 2 and 3"). Most of the poses they need already exist as
frames in the Phase 1 character atlases, so the Adventurer-plan budget composes them here
instead of paying for new animation runs: idle frames give calm and ready, running frames give
the walk cycle and approach, the attacks give gesture and shield, and blade_dash gives brace.
Only the poses no animation covers (survey, look_back, inspect, concealed, settle) come from
cells of the 2K pose sheets, cut first with ``slice_sheet.py ... trim``.

The layout comes from a JSON spec:

    {
      "out": "assets/cutscenes/wildfire_aftermath/poses.png",
      "columns": 6, "rows": 3,
      "cell": 512, "pivot": [256, 448],
      "cells": [
        {"row": 0, "col": 0, "atlas": "assets/characters/rendered/cyber_shogun_atlas.png",
         "animation": "idle", "frame": 0},
        {"row": 0, "col": 1, "image": "scratch/poses/kaito_gesture.png"},
        {"row": 0, "col": 2, "atlas": "...", "animation": "running", "frame": 0, "flip": true}
      ],
      "strips": [
        {"row": 1, "atlas": "...", "animation": "running", "frames": [0, 1, 2, 3, 4, 5, 6, 7]}
      ]
    }

- ``cells`` place one figure each. ``strips`` fill a row from column 0 with several frames,
  which suits the deployment sheet (8 columns, rows idle, running, jumping, blade_dash, settle).
- Atlas frames are read from the 256x256 Phase 1 atlas (rows in ``ATLAS_ROWS`` order, feet at
  y=224). They are enlarged by ``cell / 256`` with nearest-neighbour, so the figure keeps the
  same size and baseline in every cutscene.
- ``image`` cells are trimmed to their opaque pixels and enlarged by a whole-number factor
  chosen so the figure is as tall as the same character's idle frame would be in this sheet
  (``match_atlas`` names that atlas). Without ``match_atlas`` the largest factor that fits is
  used. The trimmed figure's bottom-centre goes on the pivot.
- ``flip`` mirrors a figure horizontally (a look back, or a walk in the other direction).

Run from the project root:

    uv run --system-certs --no-project --with pillow python tools/art/compose_poses.py spec.json

It writes only the spec's ``out`` file and makes no network calls.
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from PIL import Image

ATLAS_CELL = 256
ATLAS_BASELINE = 224
ATLAS_ROWS = ("idle", "running", "jumping", "blade_dash", "sword_attack", "karate_attack")
MARGIN = 3


def atlas_frame(atlas: Image.Image, animation: str, frame: int) -> Image.Image:
    """Cut one 256x256 frame from a Phase 1 atlas.

    Raises:
        ValueError: for an unknown animation name, a frame outside 0-7 or an empty frame.
    """
    if animation not in ATLAS_ROWS:
        raise ValueError(f"unknown animation {animation!r}; expected one of {ATLAS_ROWS}")
    row = ATLAS_ROWS.index(animation)
    if not 0 <= frame < atlas.width // ATLAS_CELL:
        raise ValueError(f"frame {frame} is outside the atlas row")
    if (row + 1) * ATLAS_CELL > atlas.height:
        raise ValueError(f"the atlas has no {animation!r} row")
    cut = atlas.crop((frame * ATLAS_CELL, row * ATLAS_CELL, (frame + 1) * ATLAS_CELL, (row + 1) * ATLAS_CELL))
    if not cut.getchannel("A").getbbox():
        raise ValueError(f"{animation} frame {frame} is empty")
    return cut


def place_atlas_frame(frame: Image.Image, cell: int, pivot: tuple[int, int], flip: bool) -> Image.Image:
    """Enlarge an atlas frame to the cell scale and align its baseline with the pivot."""
    scale = cell / ATLAS_CELL
    big = frame.resize((round(ATLAS_CELL * scale), round(ATLAS_CELL * scale)), Image.Resampling.NEAREST)
    if flip:
        big = big.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    # The atlas puts the figure's centre at x=128 and its feet at y=224.
    offset = (round(pivot[0] - ATLAS_CELL / 2 * scale), round(pivot[1] - ATLAS_BASELINE * scale))
    layer = Image.new("RGBA", (cell, cell), (0, 0, 0, 0))
    layer.paste(big, offset, big)
    return layer


def place_image(image: Image.Image, cell: int, pivot: tuple[int, int], flip: bool,
                target_height: int | None) -> Image.Image:
    """Trim a pose image and put its bottom-centre on the pivot at a whole-number scale."""
    used = image.getchannel("A").getbbox()
    if not used:
        raise ValueError("pose image is empty")
    art = image.crop(used)
    room = min(pivot[0] - MARGIN, cell - pivot[0] - MARGIN) * 2
    fit_scale = max(1, int(min(room / art.width, (pivot[1] - MARGIN) / art.height)))
    scale = fit_scale
    if target_height:
        scale = max(1, min(fit_scale, round(target_height / art.height)))
    art = art.resize((art.width * scale, art.height * scale), Image.Resampling.NEAREST)
    if flip:
        art = art.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    layer = Image.new("RGBA", (cell, cell), (0, 0, 0, 0))
    layer.paste(art, (pivot[0] - art.width // 2, pivot[1] - art.height), art)
    return layer


def idle_height(atlas: Image.Image, cell: int) -> int:
    """Height in sheet pixels of the character's idle frame 0 at this sheet's scale."""
    box = atlas_frame(atlas, "idle", 0).getchannel("A").getbbox()
    return round((box[3] - box[1]) * cell / ATLAS_CELL)


def compose(spec: dict, root: Path) -> Image.Image:
    """Build the pose sheet described by ``spec`` (see the module docstring)."""
    cell = int(spec.get("cell", 512))
    pivot = tuple(spec.get("pivot", (256, 448)))
    columns, rows = int(spec["columns"]), int(spec["rows"])
    sheet = Image.new("RGBA", (columns * cell, rows * cell), (0, 0, 0, 0))
    atlases: dict[str, Image.Image] = {}

    def load_atlas(path: str) -> Image.Image:
        """Open each atlas once."""
        if path not in atlases:
            atlases[path] = Image.open(root / path).convert("RGBA")
        return atlases[path]

    placements: list[tuple[int, int, Image.Image]] = []
    for strip in spec.get("strips", []):
        atlas = load_atlas(strip["atlas"])
        for column, frame in enumerate(strip["frames"]):
            figure = place_atlas_frame(atlas_frame(atlas, strip["animation"], frame), cell, pivot, strip.get("flip", False))
            placements.append((strip["row"], column, figure))
    for item in spec.get("cells", []):
        if "atlas" in item:
            atlas = load_atlas(item["atlas"])
            figure = place_atlas_frame(atlas_frame(atlas, item["animation"], item.get("frame", 0)), cell, pivot,
                                       item.get("flip", False))
        else:
            target = idle_height(load_atlas(item["match_atlas"]), cell) if "match_atlas" in item else None
            figure = place_image(Image.open(root / item["image"]).convert("RGBA"), cell, pivot,
                                 item.get("flip", False), target)
        placements.append((item["row"], item["col"], figure))

    filled: set[tuple[int, int]] = set()
    for row, column, figure in placements:
        if not (0 <= row < rows and 0 <= column < columns):
            raise ValueError(f"cell ({row}, {column}) is outside the {columns}x{rows} sheet")
        if (row, column) in filled:
            raise ValueError(f"cell ({row}, {column}) is filled twice")
        filled.add((row, column))
        sheet.alpha_composite(figure, (column * cell, row * cell))
    missing = [(r, c) for r in range(rows) for c in range(columns) if (r, c) not in filled]
    if missing:
        raise ValueError(f"cells with no figure: {missing}")
    return sheet


def main(argv: list[str] | None = None) -> None:
    """Read the spec, build the sheet, save it and print a summary."""
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("spec", type=Path, help="JSON layout (see the module docstring)")
    parser.add_argument("--root", type=Path, default=Path("."), help="folder that spec paths are relative to")
    args = parser.parse_args(argv)
    spec = json.loads(args.spec.read_text(encoding="utf-8"))
    sheet = compose(spec, args.root)
    out = args.root / spec["out"]
    out.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(out, optimize=True)
    print(f"POSES={out} {sheet.width}x{sheet.height}")


if __name__ == "__main__":
    sys.exit(main())
