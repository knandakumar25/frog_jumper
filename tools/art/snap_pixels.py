"""Snap a SpriteCook raw image to a clean pixel grid and write it at the game's exact size.

Why this exists: SpriteCook's own pixel snap is unreliable on 2K images. It returned 64x96 and
32x32 for the Phase 1 lineup and irregular sizes (367x205, 100x100) for Phase 2 layers. The raw
2K image (``raw_url`` from ``check_job_status(verbose=true)``) is fine, but its "pixels" are
uneven blocks rather than a true grid. This tool turns that raw image into real pixel art:

1. **Key** (optional) the background:
   - ``magenta``: pixels close to #FF00FF, plus strongly magenta-tinted fringe pixels, become
     transparent. Use this for layers generated on a flat magenta background.
   - ``flood-white``: near-white pixels connected to the image border become transparent, so
     white parts inside a figure survive. This is the Phase 1 lineup method.
   - ``none``: keep every pixel (opaque base plates).
2. **Quantize** the colours to ``--colors`` (median cut, no dithering), so near-duplicate shades
   collapse into one palette entry.
3. **Block mode** down to the logical grid ``--grid`` (for example 480x270): every grid cell takes
   the most common palette colour among its foreground pixels, and stays transparent unless at
   least half of the cell is foreground. Cell edges follow the raw size divided by the grid, so
   non-integer ratios (2752/480) are handled.
4. **Upscale** with nearest-neighbour to ``--size`` (for example 1920x1080, an exact 4x), so
   every logical pixel becomes an even square block.
5. **Offset** (optional): ``--offset X,Y`` moves the result by whole output pixels with a
   transparent fill. It is used to put a roof line where the game's code expects it.
6. ``--opaque`` flattens onto black with no alpha channel (base plates must be fully opaque).

Run from the project root, for example:

    uv run --system-certs --no-project --with pillow --with numpy python tools/art/snap_pixels.py \
        raw.png assets/cutscenes/deployment/live/city_base.png \
        --grid 480x270 --size 1920x1080 --key none --opaque

It reads one local file and writes one, with no network calls and no credits.
"""
from __future__ import annotations

import argparse
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image


def parse_pair(text: str, sep: str = "x") -> tuple[int, int]:
    """Parse ``AxB`` (or ``A,B`` with ``sep=','``) into two integers."""
    a, b = text.lower().split(sep)
    return int(a), int(b)


def background_mask(rgb: np.ndarray, mode: str, tolerance: int) -> np.ndarray:
    """Return a boolean array that is True where a pixel is background (see the module docstring)."""
    h, w, _ = rgb.shape
    if mode == "none":
        return np.zeros((h, w), bool)
    r, g, b = rgb[..., 0].astype(int), rgb[..., 1].astype(int), rgb[..., 2].astype(int)
    if mode == "magenta":
        near = (np.abs(r - 255) <= tolerance) & (g <= tolerance) & (np.abs(b - 255) <= tolerance)
        # Anti-aliased edges blend magenta into the art, often darkened (for example 140,6,152 next
        # to a navy roof). Drop any pixel whose red and blue are both strong, roughly equal, and far
        # above green: that hue does not occur in the navy/cyan/orange art these layers use.
        fringe = (r > 90) & (b > 90) & (np.abs(r - b) < 70) & (g < 0.35 * np.minimum(r, b))
        return near | fringe
    if mode == "flood-white":
        white = (np.min(rgb, axis=2) > 225) & ((np.max(rgb, axis=2).astype(int) - np.min(rgb, axis=2)) < 25)
        bg = np.zeros((h, w), bool)
        queue: deque[tuple[int, int]] = deque()
        for x in range(w):
            for y in (0, h - 1):
                if white[y, x] and not bg[y, x]:
                    bg[y, x] = True
                    queue.append((y, x))
        for y in range(h):
            for x in (0, w - 1):
                if white[y, x] and not bg[y, x]:
                    bg[y, x] = True
                    queue.append((y, x))
        while queue:
            y, x = queue.popleft()
            for ny, nx in ((y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)):
                if 0 <= ny < h and 0 <= nx < w and white[ny, nx] and not bg[ny, nx]:
                    bg[ny, nx] = True
                    queue.append((ny, nx))
        return bg
    raise ValueError(f"unknown key mode {mode!r}")


def snap(image: Image.Image, grid: tuple[int, int], key: str, colors: int, tolerance: int) -> Image.Image:
    """Key, quantize and block-mode ``image`` down to ``grid``; returns an RGBA image of that size."""
    rgb_img = image.convert("RGB")
    rgb = np.asarray(rgb_img)
    h, w, _ = rgb.shape
    bg = background_mask(rgb, key, tolerance)
    pal_img = rgb_img.quantize(colors=colors, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    labels = np.asarray(pal_img).astype(np.int64)
    palette = np.array(pal_img.getpalette()[: colors * 3], dtype=np.uint8).reshape(-1, 3)
    gw, gh = grid
    # Map every source pixel to its grid cell; integer division handles non-integer ratios.
    cell_x = (np.arange(w) * gw) // w
    cell_y = (np.arange(h) * gh) // h
    cell = cell_y[:, None] * gw + cell_x[None, :]
    fg = ~bg
    cells = gw * gh
    total = np.bincount(cell.ravel(), minlength=cells)
    fg_count = np.bincount(cell[fg], minlength=cells)
    votes = np.bincount(cell[fg] * colors + labels[fg], minlength=cells * colors).reshape(cells, colors)
    winner = votes.argmax(axis=1)
    out = np.zeros((cells, 4), np.uint8)
    out[:, :3] = palette[np.clip(winner, 0, len(palette) - 1)]
    out[:, 3] = np.where(fg_count * 2 >= np.maximum(total, 1), 255, 0)
    if key == "magenta":
        # A cell can still win a magenta-ish palette colour when its foreground pixels were
        # fringe shades the per-pixel test missed; clear those cells with the same rule.
        leftover = background_mask(out[:, :3].reshape(gh, gw, 3), "magenta", tolerance).ravel()
        out[leftover, 3] = 0
    return Image.fromarray(out.reshape(gh, gw, 4), "RGBA")


def main(argv: list[str] | None = None) -> None:
    """Parse arguments, snap, upscale, offset and save (see the module docstring)."""
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("src", type=Path)
    parser.add_argument("out", type=Path)
    parser.add_argument("--grid", type=parse_pair, required=True, help="logical pixel grid, e.g. 480x270")
    parser.add_argument("--size", type=parse_pair, required=True, help="final size, e.g. 1920x1080")
    parser.add_argument("--key", choices=("none", "magenta", "flood-white"), default="none")
    parser.add_argument("--colors", type=int, default=48, help="palette size before block mode")
    parser.add_argument("--tolerance", type=int, default=60, help="magenta key tolerance per channel")
    parser.add_argument("--offset", type=lambda t: parse_pair(t, ","), default=(0, 0), help="X,Y shift in output px")
    parser.add_argument("--opaque", action="store_true", help="flatten onto black, no alpha")
    args = parser.parse_args(argv)

    small = snap(Image.open(args.src), args.grid, args.key, args.colors, args.tolerance)
    big = small.resize(args.size, Image.Resampling.NEAREST)
    if args.offset != (0, 0):
        shifted = Image.new("RGBA", big.size, (0, 0, 0, 0))
        shifted.paste(big, args.offset, big)
        big = shifted
    if args.opaque:
        flat = Image.new("RGBA", big.size, (0, 0, 0, 255))
        flat.alpha_composite(big)
        big = flat.convert("RGB")
    args.out.parent.mkdir(parents=True, exist_ok=True)
    big.save(args.out, optimize=True)
    print(f"SNAP {args.src.name} -> {args.out} grid={args.grid[0]}x{args.grid[1]} size={big.width}x{big.height} key={args.key}")


if __name__ == "__main__":
    main()
