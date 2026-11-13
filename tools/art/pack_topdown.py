"""Pack SpriteCook top-down walk animations into an open-world operative sheet.

``assets/world/operative_<n>.png`` is 384x1024: 4 columns by 8 rows of 96x128 frames, cut by
``ExplorationPlayer`` (``Hframes = 4, Vframes = 8``). Rows are facing directions: 0 south,
1 south-west, 2 west, 3 north-west, 4 north, 5 north-east, 6 east, 7 south-east. Column 0 is
standing and columns 1 to 3 are the walk cycle (``docs/ART_REGENERATION.md``, Phase 6).

To fit one month of the Adventurer plan, each operative gets only three paid walk
animations: south, north and west. This script fills the other rows locally:

- east is west mirrored;
- each diagonal reuses the side view it is closest to in motion: south-west and north-west use
  west, south-east and north-east use east. Pass ``--row southwest=...`` (and so on) to use a
  real diagonal strip instead, if SpriteCook returns one.

Frame handling, per row:

- the strip is cut into square frames (frame width = strip height unless ``--frame-size``);
- column 0 is frame 0 (SpriteCook animates from the standing base), and columns 1 to 3 are
  three frames spread evenly over the rest of the cycle;
- one nearest-neighbour scale is used for the whole sheet, the largest that fits every frame in
  its 96x128 cell with a margin, so all directions show the figure at the same size;
- each frame's feet (lowest opaque row of the row's frame 0) sit on ``--baseline`` (default
  87, the in-cell point that lands on the player's position given the sprite's
  ``Offset = (0, -23)``), and frame 0's centre sits on the cell centre.

Run from the project root, for example:

    uv run --system-certs --no-project --with pillow python tools/art/pack_topdown.py \
        assets/world/operative_0.png --row south=s.png --row north=n.png --row west=w.png
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
from pack_spritecook_character import cut_strip  # noqa: E402

FRAME_W, FRAME_H = 96, 128
COLUMNS = 4
DIRECTIONS = ("south", "southwest", "west", "northwest", "north", "northeast", "east", "southeast")
# Rows filled locally when no strip is given: (source direction, mirror horizontally).
FALLBACKS = {
    "east": ("west", True),
    "southwest": ("west", False),
    "northwest": ("west", False),
    "southeast": ("west", True),
    "northeast": ("west", True),
}
MARGIN = 2


def pick_frames(frames: list[Image.Image]) -> list[Image.Image]:
    """Return the standing frame plus three walk frames spread evenly over the rest."""
    if len(frames) < 2:
        raise ValueError("a walk strip needs at least two frames")
    rest = frames[1:]
    walk = [rest[min(len(rest) - 1, (i * len(rest)) // 3)] for i in range(3)]
    return [frames[0], *walk]


def anchor(frame: Image.Image) -> tuple[float, int]:
    """Centre x and feet y (lowest opaque row) of a frame."""
    box = frame.getchannel("A").getbbox()
    if not box:
        raise ValueError("a standing frame is empty")
    return (box[0] + box[2]) / 2, box[3]


def sheet_scale(rows: dict[str, list[Image.Image]], baseline: int) -> float:
    """Largest scale at which every frame of every row fits its cell around its anchor."""
    best = None
    for frames in rows.values():
        ax, ay = anchor(frames[0])
        for frame in frames:
            box = frame.getchannel("A").getbbox()
            if not box:
                continue
            reach_x = max(ax - box[0], box[2] - ax, 1)
            fits = [(FRAME_W / 2 - MARGIN) / reach_x, (baseline - MARGIN) / max(ay - box[1], 1)]
            if box[3] > ay:
                fits.append((FRAME_H - baseline - MARGIN) / (box[3] - ay))
            scale = min(fits)
            best = scale if best is None else min(best, scale)
    if not best:
        raise ValueError("no frames to pack")
    return best


def place(frame: Image.Image, ax: float, ay: int, scale: float, baseline: int, mirror: bool) -> Image.Image:
    """Scale one frame and place it in a 96x128 cell with its anchor on (48, baseline)."""
    big = frame.resize((max(1, round(frame.width * scale)), max(1, round(frame.height * scale))),
                       Image.Resampling.NEAREST)
    x = ax * scale
    if mirror:
        big = big.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
        x = big.width - x
    cell = Image.new("RGBA", (FRAME_W, FRAME_H), (0, 0, 0, 0))
    cell.paste(big, (round(FRAME_W / 2 - x), round(baseline - ay * scale)), big)
    return cell


def build(rows: dict[str, list[Image.Image]], baseline: int) -> Image.Image:
    """Assemble the 384x1024 sheet from the given rows plus the local fallbacks."""
    for needed in ("south", "north", "west"):
        if needed not in rows:
            raise ValueError(f"missing the {needed} strip")
    scale = sheet_scale(rows, baseline)
    sheet = Image.new("RGBA", (FRAME_W * COLUMNS, FRAME_H * len(DIRECTIONS)), (0, 0, 0, 0))
    for row_index, direction in enumerate(DIRECTIONS):
        source, mirror = (direction, False) if direction in rows else FALLBACKS[direction]
        frames = rows[source]
        ax, ay = anchor(frames[0])
        for column, frame in enumerate(frames):
            sheet.alpha_composite(place(frame, ax, ay, scale, baseline, mirror), (column * FRAME_W, row_index * FRAME_H))
    return sheet


def main(argv: list[str] | None = None) -> None:
    """Parse arguments, build the sheet, save it and print a summary."""
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("out", type=Path, help="output sheet, e.g. assets/world/operative_0.png")
    parser.add_argument("--row", action="append", required=True, metavar="DIRECTION=STRIP.png",
                        help="walk strip per direction; south, north and west are required")
    parser.add_argument("--frame-size", type=int, help="frame width if frames are not square")
    parser.add_argument("--baseline", type=int, default=87, help="feet y inside each 96x128 cell")
    args = parser.parse_args(argv)
    rows: dict[str, list[Image.Image]] = {}
    for item in args.row:
        direction, path = item.split("=", 1)
        if direction not in DIRECTIONS:
            parser.error(f"unknown direction {direction!r}")
        rows[direction] = pick_frames(cut_strip(Path(path), args.frame_size))
    sheet = build(rows, args.baseline)
    args.out.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(args.out, optimize=True)
    print(f"TOPDOWN={args.out} {sheet.width}x{sheet.height} rows={','.join(sorted(rows))}")


if __name__ == "__main__":
    main()
