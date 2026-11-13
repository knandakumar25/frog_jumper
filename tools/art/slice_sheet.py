"""Cut a SpriteCook grid sheet into the game's exact-size files.

The Adventurer-plan budget (``docs/ART_REGENERATION.md``, "Methods") makes many images as cells
of one shared sheet instead of one generation each:

- scene sheets: a 4K 16:9 image laid out 2x2, each quadrant one 1920x1080 layer;
- sprite sheets: a 2K 1:1 image laid out 3x3 or 4x4, each cell one prop, module or landmark.

Images in ``docs/art_regeneration.json`` whose ``method`` is ``sheet:<id>`` come from here.

How a cell is processed, in order:

1. The sheet is divided into an even ``COLUMNSxROWS`` grid. ``--gutter`` pixels are cut from
   every inner edge of each cell, because generated grids often bleed a few pixels across the
   dividing lines.
2. Unless the cell is ``opaque``, pixels close to the key colour (default magenta ``#ff00ff``,
   the flat background the prompts ask for) become transparent. The distance is the largest
   difference over the R, G and B channels, compared with ``--tolerance``.
3. ``trim`` crops the cell to its opaque pixels (sprites), so ``contain`` can centre them.
4. The result is fitted to the target size with ``fit()`` from ``fit_spritecook_image.py``
   (stretch, contain or cover, nearest-neighbour by default to keep pixel art crisp).
5. ``opaque`` cells (base plates) are flattened onto black with no alpha channel.

Each ``--cell`` is ``INDEX=OUT.png@WxH`` followed by optional comma-separated options:
``mode=stretch|contain|cover`` (default contain), ``opaque``, ``trim``, ``margin=N``
(transparent border kept by contain; default 4 so nothing touches the edge).
Cells are numbered from 0, left to right, then top to bottom.

Example (one Act II scene sheet, 4K 2x2):

    uv run --system-certs --no-project --with pillow python tools/art/slice_sheet.py sheet.png \
        --grid 2x2 --gutter 6 \
        --cell "0=assets/cutscenes/act_ii_opening/queue_base.png@1920x1080,mode=cover,opaque" \
        --cell "1=assets/cutscenes/act_ii_opening/queue_structure.png@1920x1080,mode=cover"

It writes only the files named by ``--cell``, makes no network calls and spends no credits.
"""
from __future__ import annotations

import argparse
import sys
from dataclasses import dataclass
from pathlib import Path

from PIL import Image

# Reuse the shared fitting helpers. tools/art is not a package, so put this folder on the path.
sys.path.insert(0, str(Path(__file__).resolve().parent))
from fit_spritecook_image import fit, parse_size  # noqa: E402


@dataclass
class CellSpec:
    """One output file taken from one grid cell."""

    index: int
    out: Path
    size: tuple[int, int]
    mode: str = "contain"
    opaque: bool = False
    trim: bool = False
    margin: int = 4


def parse_cell(text: str) -> CellSpec:
    """Parse ``INDEX=OUT.png@WxH[,option...]`` (see the module docstring)."""
    index_text, rest = text.split("=", 1)
    target, *options = rest.split(",")
    out_text, size_text = target.rsplit("@", 1)
    spec = CellSpec(int(index_text), Path(out_text), parse_size(size_text))
    for option in options:
        option = option.strip()
        if option == "opaque":
            spec.opaque = True
        elif option == "trim":
            spec.trim = True
        elif option.startswith("mode="):
            spec.mode = option[5:]
        elif option.startswith("margin="):
            spec.margin = int(option[7:])
        elif option:
            raise ValueError(f"unknown cell option {option!r} in {text!r}")
    if spec.mode not in ("stretch", "contain", "cover"):
        raise ValueError(f"unknown mode {spec.mode!r} in {text!r}")
    return spec


def parse_colour(text: str) -> tuple[int, int, int]:
    """Parse ``#rrggbb`` into an RGB tuple."""
    text = text.lstrip("#")
    if len(text) != 6:
        raise ValueError(f"colour must be #rrggbb, got {text!r}")
    return int(text[0:2], 16), int(text[2:4], 16), int(text[4:6], 16)


def cell_box(sheet: Image.Image, columns: int, rows: int, index: int, gutter: int) -> tuple[int, int, int, int]:
    """Return the crop box of cell ``index``, shrunk by ``gutter`` on inner edges only.

    Outer sheet edges are not shrunk, so a scene quadrant keeps as much of its picture as
    possible; inner edges are where neighbouring cells bleed into each other.
    """
    if not 0 <= index < columns * rows:
        raise ValueError(f"cell {index} is outside a {columns}x{rows} grid")
    column, row = index % columns, index // columns
    x0 = sheet.width * column // columns
    x1 = sheet.width * (column + 1) // columns
    y0 = sheet.height * row // rows
    y1 = sheet.height * (row + 1) // rows
    if column > 0:
        x0 += gutter
    if column < columns - 1:
        x1 -= gutter
    if row > 0:
        y0 += gutter
    if row < rows - 1:
        y1 -= gutter
    return x0, y0, x1, y1


def key_out(image: Image.Image, key: tuple[int, int, int], tolerance: int) -> Image.Image:
    """Make pixels within ``tolerance`` of the key colour fully transparent.

    The distance is the largest per-channel difference, which suits the flat, unshaded
    background a pixel-art generation is asked for. Other pixels keep their alpha.
    """
    rgba = image.convert("RGBA")
    pixels = rgba.load()
    for y in range(rgba.height):
        for x in range(rgba.width):
            r, g, b, a = pixels[x, y]
            if max(abs(r - key[0]), abs(g - key[1]), abs(b - key[2])) <= tolerance:
                pixels[x, y] = (0, 0, 0, 0)
    return rgba


def process_cell(sheet: Image.Image, spec: CellSpec, columns: int, rows: int, gutter: int,
                 key: tuple[int, int, int], tolerance: int, resample: int) -> Image.Image:
    """Cut, key, trim and fit one cell into its final image (see the module docstring)."""
    cell = sheet.crop(cell_box(sheet, columns, rows, spec.index, gutter)).convert("RGBA")
    if not spec.opaque:
        cell = key_out(cell, key, tolerance)
    if spec.trim:
        used = cell.getchannel("A").getbbox()
        if not used:
            raise ValueError(f"cell {spec.index} is empty after keying; check --key and --tolerance")
        cell = cell.crop(used)
    result = fit(cell, spec.size, spec.mode, resample, spec.margin if spec.mode == "contain" else 0)
    if spec.opaque:
        flat = Image.new("RGBA", result.size, (0, 0, 0, 255))
        flat.alpha_composite(result)
        result = flat.convert("RGB")
    return result


def main(argv: list[str] | None = None) -> None:
    """Parse arguments, write every requested cell and print one line per file."""
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("sheet", type=Path, help="downloaded SpriteCook grid sheet")
    parser.add_argument("--grid", type=parse_size, required=True, help="COLUMNSxROWS, e.g. 2x2 or 4x4")
    parser.add_argument("--cell", action="append", type=parse_cell, required=True,
                        metavar="INDEX=OUT@WxH[,opts]")
    parser.add_argument("--gutter", type=int, default=4, help="pixels cut from inner cell edges")
    parser.add_argument("--key", type=parse_colour, default=(255, 0, 255), help="background #rrggbb")
    parser.add_argument("--tolerance", type=int, default=48, help="max per-channel distance to the key")
    parser.add_argument("--resample", choices=("nearest", "lanczos"), default="nearest")
    args = parser.parse_args(argv)

    columns, rows = args.grid
    sheet = Image.open(args.sheet).convert("RGBA")
    resample = Image.Resampling.NEAREST if args.resample == "nearest" else Image.Resampling.LANCZOS
    for spec in args.cell:
        result = process_cell(sheet, spec, columns, rows, args.gutter, args.key, args.tolerance, resample)
        spec.out.parent.mkdir(parents=True, exist_ok=True)
        result.save(spec.out, optimize=True)
        print(f"CELL {spec.index} -> {spec.out} {result.width}x{result.height} {spec.mode}"
              f"{' opaque' if spec.opaque else ''}")


if __name__ == "__main__":
    main()
