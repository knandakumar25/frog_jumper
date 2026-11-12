"""Pack SpriteCook pixel-art animation strips into the game's character atlas.

The game's SpriteFrames resources (``assets/characters/rendered/<id>.tres``,
restored unchanged from git commit ``ca92679``) cut 256x256 regions from
``<id>_atlas.png``: one row per animation, eight frames per row, feet on a
shared baseline at y=224 (see ``docs/ART_REGENERATION.md``, Phase 1). SpriteCook
returns one horizontal strip per animation, on its own canvas and frame count.
This script turns those strips into that atlas layout without changing the game
code or the ``.tres`` files.

How the packing works:
- Each strip is cut into square frames (frame size = strip height unless
  ``--frame-size`` is given), and the frame count is checked against the strip
  width, as ``spritecook-use-assets-in-godot`` requires.
- Each row is resampled to exactly eight frames. Extra frames are dropped
  evenly (a 10-frame run keeps 8 spread across the cycle); a short row repeats
  frames evenly.
- Every row is anchored on its first frame: SpriteCook animates from the base
  still, so frame 0 is the character's resting body. Frame 0's horizontal
  centre goes to the cell centre and its lowest opaque pixel goes to the
  baseline, so movement inside the row (a jump arc, a forward dash) is kept and
  the body does not jump between rows.
- One whole-number nearest-neighbour scale is used for every row of a
  character, chosen as the largest scale at which every frame of every row
  still fits inside its cell with a transparent margin. Whole-number nearest
  scaling keeps pixel art crisp.

The optional portrait is the base still enlarged with the same nearest-neighbour
rule onto a transparent canvas (768x1024 for operatives, 768x768 for AXIOM-7),
centred horizontally with its feet near the bottom edge. It costs no credits.

Run from the project root, for example:

    uv run --system-certs --no-project --with pillow python tools/art/pack_spritecook_character.py \
        --atlas assets/characters/rendered/cyber_shogun_atlas.png \
        --row idle=strips/idle.png --row running=strips/run.png ... \
        --portrait-source base.png \
        --portrait assets/characters/rendered/cyber_shogun_portrait.png --portrait-size 768x1024

Rows are written top to bottom in the order given, so give them in the order the
``.tres`` expects: idle, running, jumping, blade_dash, sword_attack,
karate_attack for operatives, or idle alone for the enemies.
"""
from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image

CELL = 256
FRAMES_PER_ROW = 8
BASELINE = 224
# Transparent pixels kept between the art and each cell edge. The asset
# validators fail when an opaque pixel touches a cell or canvas edge.
MARGIN = 3


def cut_strip(path: Path, frame_size: int | None) -> list[Image.Image]:
    """Cut a horizontal SpriteCook strip into frames.

    Args:
        path: PNG strip (SpriteCook ``spritesheet_url`` download).
        frame_size: Frame width in pixels; defaults to the strip height, since
            SpriteCook animation frames are square.

    Returns:
        The frames in order, as RGBA images.

    Raises:
        ValueError: if the strip width is not a whole number of frames.
    """
    strip = Image.open(path).convert("RGBA")
    width = frame_size or strip.height
    if strip.width % width:
        raise ValueError(f"{path}: strip width {strip.width} is not a multiple of frame width {width}")
    return [strip.crop((x, 0, x + width, strip.height)) for x in range(0, strip.width, width)]


def resample(frames: list[Image.Image], count: int = FRAMES_PER_ROW) -> list[Image.Image]:
    """Return exactly ``count`` frames spread evenly across the source frames."""
    total = len(frames)
    return [frames[min(total - 1, (index * total) // count)] for index in range(count)]


def row_metrics(frames: list[Image.Image]) -> tuple[float, int, tuple[int, int, int, int]]:
    """Measure a row: frame 0's centre x and feet y, and the union box of all frames.

    Returns:
        (anchor_x, anchor_y, (left, top, right, bottom)) in source pixels. The
        anchor is frame 0's opaque-box centre and lowest opaque row.

    Raises:
        ValueError: if frame 0 or the whole row is fully transparent.
    """
    first = frames[0].getchannel("A").getbbox()
    if not first:
        raise ValueError("frame 0 is empty")
    boxes = [box for box in (frame.getchannel("A").getbbox() for frame in frames) if box]
    union = (min(b[0] for b in boxes), min(b[1] for b in boxes), max(b[2] for b in boxes), max(b[3] for b in boxes))
    return (first[0] + first[2]) / 2, first[3], union


def largest_scale(rows: list[list[Image.Image]]) -> int:
    """Largest whole-number scale at which every row fits its cells with a margin.

    For each row the art may reach left and right of the anchor and above it
    (and a little below, for frames that dip under the resting feet line).

    Raises:
        ValueError: if even scale 1 does not fit.
    """
    best = None
    for frames in rows:
        anchor_x, anchor_y, (left, top, right, bottom) = row_metrics(frames)
        reach_left = anchor_x - left
        reach_right = right - anchor_x
        reach_up = anchor_y - top
        reach_down = max(0, bottom - anchor_y)
        fits = [
            (CELL / 2 - MARGIN) / max(reach_left, reach_right, 1),
            (BASELINE - MARGIN) / max(reach_up, 1),
        ]
        if reach_down:
            fits.append((CELL - BASELINE - MARGIN) / reach_down)
        scale = int(min(fits))
        best = scale if best is None else min(best, scale)
    if not best or best < 1:
        raise ValueError("the animation does not fit a 256x256 cell even at scale 1")
    return best


def place_row(frames: list[Image.Image], scale: int) -> list[Image.Image]:
    """Enlarge each frame and place it in a 256x256 cell, anchored on frame 0."""
    anchor_x, anchor_y, _ = row_metrics(frames)
    cells = []
    for frame in frames:
        big = frame.resize((frame.width * scale, frame.height * scale), Image.Resampling.NEAREST)
        cell = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
        offset = (round(CELL / 2 - anchor_x * scale), BASELINE - anchor_y * scale)
        # paste() clips art placed partly outside the cell, unlike alpha_composite().
        layer = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
        layer.paste(big, offset, big)
        cell.alpha_composite(layer)
        cells.append(cell)
    return cells


def build_atlas(rows: list[list[Image.Image]], scale: int) -> Image.Image:
    """Assemble the rows into one atlas, 8 columns wide and one row per animation."""
    atlas = Image.new("RGBA", (CELL * FRAMES_PER_ROW, CELL * len(rows)), (0, 0, 0, 0))
    for row_index, frames in enumerate(rows):
        for column, cell in enumerate(place_row(frames, scale)):
            atlas.alpha_composite(cell, (column * CELL, row_index * CELL))
    return atlas


def build_portrait(source: Path, size: tuple[int, int]) -> Image.Image:
    """Enlarge a base still by the largest whole-number nearest scale onto a canvas.

    The art is centred horizontally and sits a small margin above the bottom
    edge, so a full-body sprite reads as standing on the card's floor.
    """
    still = Image.open(source).convert("RGBA")
    box = still.getchannel("A").getbbox()
    if not box:
        raise ValueError(f"{source} is empty")
    art = still.crop(box)
    margin = max(8, size[1] // 32)
    scale = max(1, int(min((size[0] - 2 * margin) / art.width, (size[1] - 2 * margin) / art.height)))
    art = art.resize((art.width * scale, art.height * scale), Image.Resampling.NEAREST)
    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    canvas.alpha_composite(art, ((size[0] - art.width) // 2, size[1] - margin - art.height))
    return canvas


def parse_size(text: str) -> tuple[int, int]:
    """Parse ``WIDTHxHEIGHT``."""
    width, height = text.lower().split("x")
    return int(width), int(height)


def main() -> None:
    """Parse arguments, pack the atlas and optional portrait, and print a summary."""
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--atlas", type=Path, help="output atlas PNG")
    parser.add_argument("--row", action="append", default=[], metavar="NAME=STRIP.png",
                        help="one animation strip per row, top to bottom")
    parser.add_argument("--frame-size", type=int, help="frame width if frames are not square")
    parser.add_argument("--scale", type=int, help="force a scale (default: largest that fits)")
    parser.add_argument("--portrait-source", type=Path, help="base still for the portrait")
    parser.add_argument("--portrait", type=Path, help="output portrait PNG")
    parser.add_argument("--portrait-size", type=parse_size, default=(768, 1024))
    args = parser.parse_args()

    if args.row:
        if not args.atlas:
            parser.error("--atlas is required with --row")
        names, rows = [], []
        for item in args.row:
            name, path = item.split("=", 1)
            source = cut_strip(Path(path), args.frame_size)
            names.append(f"{name}:{len(source)}")
            rows.append(resample(source))
        scale = args.scale or largest_scale(rows)
        args.atlas.parent.mkdir(parents=True, exist_ok=True)
        build_atlas(rows, scale).save(args.atlas, optimize=True)
        print(f"ATLAS={args.atlas} rows={' '.join(names)} scale={scale}")
    if args.portrait_source:
        if not args.portrait:
            parser.error("--portrait is required with --portrait-source")
        args.portrait.parent.mkdir(parents=True, exist_ok=True)
        build_portrait(args.portrait_source, args.portrait_size).save(args.portrait, optimize=True)
        print(f"PORTRAIT={args.portrait} size={args.portrait_size[0]}x{args.portrait_size[1]}")


if __name__ == "__main__":
    main()
