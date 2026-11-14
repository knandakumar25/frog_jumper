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
   collapse into one palette entry. The palette is built from foreground pixels only, so a
   large keyed background cannot crowd the figures' colours out of it.
3. **Block mode** down to the logical grid ``--grid`` (for example 480x270): every grid cell takes
   the most common palette colour among its foreground pixels, and stays transparent unless at
   least half of the cell is foreground. Cell edges follow the raw size divided by the grid, so
   non-integer ratios (2752/480) are handled.
4. **Upscale** with nearest-neighbour to ``--size`` (for example 1920x1080, an exact 4x), so
   every logical pixel becomes an even square block.
5. **Offset** (optional): ``--offset X,Y`` moves the result by whole output pixels with a
   transparent fill. It is used to put a roof line where the game's code expects it.
6. ``--despill`` (magenta key only) also clears bright pink or lilac cells: lamp glows that
   were blended into the magenta background. Use it only on layers with no magenta art.
7. ``--defringe N`` (magenta key only) clears up to N rings of outline cells that took a
   magenta-tinted colour, dark ones included (for example 60,20,70 where a navy edge was blended
   into the key). ``--despill`` misses these because they are not bright. Each pass clears only
   cells that touch a transparent cell, so a violet object's inside survives, but its violet
   outline would be eaten: leave it off for violet art (Phase 4 coolant and fortress).
8. ``--opaque`` flattens onto black and makes every pixel fully opaque (base plates must be). The file
   stays RGBA, because the cutscene validators read only 8-bit RGBA PNGs.

``--crop X,Y,W,H`` (optional, applied first) cuts one region out of the raw image before any
other step, in raw-image pixels. Phase 3 uses it to take one quadrant of a 4K 2x2 scene sheet
(for example ``0,0,2752,1536`` for the top-left of a 5504x3072 raw) or one cell of a pose sheet,
usually inset by a few pixels so the bleed along the dividing lines is dropped.

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


def magenta_halo(rgb: np.ndarray) -> np.ndarray:
    """True where a pixel is a bright glow blended into the magenta key (for ``--despill``).

    Lamp glows drawn over a magenta background come out pink or lilac (for example 230,120,200).
    They are bright, red and blue are within 55% of each other, and both clearly exceed green. Dark violet
    silhouettes (crowds, shadows) stay, because the test needs red or blue above 120.
    """
    r, g, b = rgb[..., 0].astype(int), rgb[..., 1].astype(int), rgb[..., 2].astype(int)
    return (np.maximum(r, b) > 120) & (r > g * 1.3) & (b > g * 1.3) & (np.abs(r - b) < 0.55 * np.maximum(r, b))


def magenta_tinted(rgb: np.ndarray) -> np.ndarray:
    """True where a colour leans magenta at any brightness (for ``--defringe``).

    Red and blue both clearly exceed green and are within 60% of each other. Unlike
    ``magenta_halo`` there is no brightness floor, so dark blended edges count too.
    """
    r, g, b = rgb[..., 0].astype(int), rgb[..., 1].astype(int), rgb[..., 2].astype(int)
    return (r > g * 1.25 + 4) & (b > g * 1.25 + 4) & (np.abs(r - b) < 0.6 * np.maximum(r, b))


def defringe(cells: np.ndarray, passes: int) -> None:
    """Clear magenta-tinted outline cells of an RGBA grid in place, ``passes`` rings deep.

    An outline cell is opaque and has a transparent 4-neighbour (or lies on the grid edge).
    """
    for _ in range(passes):
        opaque = cells[..., 3] > 0
        pad = np.pad(~opaque, 1, constant_values=True)
        outline = opaque & (pad[:-2, 1:-1] | pad[2:, 1:-1] | pad[1:-1, :-2] | pad[1:-1, 2:])
        cells[outline & magenta_tinted(cells[..., :3]), 3] = 0


def snap(image: Image.Image, grid: tuple[int, int], key: str, colors: int, tolerance: int,
         despill: bool = False, defringe_passes: int = 0) -> Image.Image:
    """Key, quantize and block-mode ``image`` down to ``grid``; returns an RGBA image of that size.

    With ``despill`` (magenta key only), output cells whose colour is a magenta glow halo
    (``magenta_halo``) are cleared too. ``defringe_passes`` (magenta key only) then clears that
    many rings of magenta-tinted outline cells (``defringe``).
    """
    rgb_img = image.convert("RGB")
    rgb = np.asarray(rgb_img)
    h, w, _ = rgb.shape
    bg = background_mask(rgb, key, tolerance)
    if bg.any() and not bg.all():
        # Build the palette from foreground pixels only. On a mostly-empty layer (a few turrets
        # on magenta) the key colour would otherwise take most palette entries, and the figures
        # would be snapped to magenta-tinted shades. Background pixels are painted with the mean
        # foreground colour first; their votes are ignored below anyway.
        filled = rgb.copy()
        filled[bg] = rgb[~bg].mean(axis=0).astype(np.uint8)
        rgb_img = Image.fromarray(filled, "RGB")
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
        if despill:
            leftover |= magenta_halo(out[:, :3].reshape(gh, gw, 3)).ravel()
        out[leftover, 3] = 0
    out = out.reshape(gh, gw, 4)
    if key == "magenta" and defringe_passes:
        defringe(out, defringe_passes)
    return Image.fromarray(out, "RGBA")


def parse_box(text: str) -> tuple[int, int, int, int]:
    """Parse ``X,Y,W,H`` into a Pillow crop box ``(left, top, right, bottom)``.

    Raises:
        ValueError: for anything other than four integers or a non-positive width or height.
    """
    x, y, w, h = (int(v) for v in text.split(","))
    if w <= 0 or h <= 0:
        raise ValueError("crop width and height must be positive")
    return x, y, x + w, y + h


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
    parser.add_argument("--opaque", action="store_true", help="flatten onto black, alpha 255 everywhere (still RGBA)")
    parser.add_argument("--despill", action="store_true",
                        help="with --key magenta, also clear bright pink glow halos (scene layers with no magenta art)")
    parser.add_argument("--defringe", type=int, default=0, metavar="N",
                        help="with --key magenta, clear N rings of magenta-tinted outline cells (not for violet art)")
    parser.add_argument("--crop", type=parse_box, help="X,Y,W,H region of the raw image to use (applied first)")
    args = parser.parse_args(argv)

    source = Image.open(args.src)
    if args.crop:
        # Pillow pads out-of-range boxes with black, which would become false foreground.
        if args.crop[2] > source.width or args.crop[3] > source.height or min(args.crop[:2]) < 0:
            parser.error(f"--crop {args.crop} is outside the {source.width}x{source.height} image")
        source = source.crop(args.crop)
    small = snap(source, args.grid, args.key, args.colors, args.tolerance, args.despill, args.defringe)
    big = small.resize(args.size, Image.Resampling.NEAREST)
    if args.offset != (0, 0):
        shifted = Image.new("RGBA", big.size, (0, 0, 0, 0))
        shifted.paste(big, args.offset, big)
        big = shifted
    if args.opaque:
        # Stay RGBA (every pixel alpha 255): the cutscene validators' decoder
        # (tools/cutscene/png_pixels.py) reads only 8-bit RGBA PNGs.
        flat = Image.new("RGBA", big.size, (0, 0, 0, 255))
        flat.alpha_composite(big)
        big = flat
    args.out.parent.mkdir(parents=True, exist_ok=True)
    big.save(args.out, optimize=True)
    print(f"SNAP {args.src.name} -> {args.out} grid={args.grid[0]}x{args.grid[1]} size={big.width}x{big.height} key={args.key}")


if __name__ == "__main__":
    main()
