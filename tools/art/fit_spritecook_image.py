"""Fit a SpriteCook image (or one piece of it) to the exact size the game expects.

SpriteCook returns its own canvas size: pixel-art images come back at their detected pixel
grid (for example 608x342 for a 16:9 scene, or about 100x90 for an icon), and one image may
hold several pieces (three button states, a panel plus its pointer). The game loads every image
by path at an exact size (`docs/art_regeneration.json`; `ArtRegenerationSpecTests` checks it).
This script bridges the two without touching game code.

Subcommands:

    pieces  SRC                      List the separate opaque pieces (connected alpha regions,
                                     merged when close) as x0,y0,x1,y1 boxes, largest first.
    fit     SRC OUT --size WxH       Crop (optional --box), then fit to WxH:
            --mode stretch           scale each axis independently (nine-patch frames, buttons);
            --mode contain           keep the aspect ratio, centre on a transparent canvas;
            --mode cover             keep the aspect ratio, fill the canvas, centre-crop the rest
                                     (full-screen layers).
            --resample nearest|lanczos   nearest keeps pixel art crisp (default); lanczos suits
                                     detailed (non-pixel) art being reduced.
            --opaque                 composite onto black and drop alpha (base layers must be
                                     fully opaque).
            --margin N               leave N transparent pixels on every side (contain mode).

Run from the project root with Pillow, for example:

    uv run --system-certs --no-project --with pillow python tools/art/fit_spritecook_image.py \
        fit scene.png assets/images/menu_backdrops/city_base.png --size 1920x1080 --mode cover --opaque
"""
from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


def parse_size(text: str) -> tuple[int, int]:
    """Parse ``WIDTHxHEIGHT`` into integers."""
    width, height = text.lower().split("x")
    return int(width), int(height)


def parse_box(text: str) -> tuple[int, int, int, int]:
    """Parse ``x0,y0,x1,y1`` into a Pillow crop box."""
    x0, y0, x1, y1 = (int(v) for v in text.split(","))
    return x0, y0, x1, y1


def pieces(image: Image.Image, gap: int = 2, min_area: int = 16) -> list[tuple[int, int, int, int]]:
    """Find the separate opaque pieces of an image.

    Opaque pixels (alpha > 0) are grouped by 4-connectivity with a flood fill, then boxes that
    lie within ``gap`` pixels of each other are merged, so a piece with a thin detached detail
    (a corner cap, a plus mark) stays one piece. Tiny specks below ``min_area`` are dropped.

    Returns:
        Boxes as (x0, y0, x1, y1) with exclusive right/bottom edges, largest area first.
    """
    alpha = image.getchannel("A")
    width, height = image.size
    data = alpha.load()
    seen = bytearray(width * height)
    boxes: list[list[int]] = []
    for start_y in range(height):
        for start_x in range(width):
            if seen[start_y * width + start_x] or data[start_x, start_y] == 0:
                continue
            stack = [(start_x, start_y)]
            seen[start_y * width + start_x] = 1
            x0 = x1 = start_x
            y0 = y1 = start_y
            count = 0
            while stack:
                x, y = stack.pop()
                count += 1
                x0, x1, y0, y1 = min(x0, x), max(x1, x), min(y0, y), max(y1, y)
                for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                    if 0 <= nx < width and 0 <= ny < height and not seen[ny * width + nx] and data[nx, ny] > 0:
                        seen[ny * width + nx] = 1
                        stack.append((nx, ny))
            if count >= min_area:
                boxes.append([x0, y0, x1 + 1, y1 + 1])
    # Merge boxes that overlap or nearly touch until nothing changes.
    merged = True
    while merged:
        merged = False
        for i in range(len(boxes)):
            for j in range(i + 1, len(boxes)):
                a, b = boxes[i], boxes[j]
                if a[0] - gap <= b[2] and b[0] - gap <= a[2] and a[1] - gap <= b[3] and b[1] - gap <= a[3]:
                    boxes[i] = [min(a[0], b[0]), min(a[1], b[1]), max(a[2], b[2]), max(a[3], b[3])]
                    del boxes[j]
                    merged = True
                    break
            if merged:
                break
    boxes.sort(key=lambda b: (b[2] - b[0]) * (b[3] - b[1]), reverse=True)
    return [tuple(b) for b in boxes]  # type: ignore[misc]


def fit(image: Image.Image, size: tuple[int, int], mode: str, resample: int, margin: int) -> Image.Image:
    """Fit ``image`` into ``size`` using stretch, contain or cover (see the module docstring)."""
    target_w, target_h = size
    if mode == "stretch":
        return image.resize(size, resample)
    if mode == "contain":
        scale = min((target_w - 2 * margin) / image.width, (target_h - 2 * margin) / image.height)
        scaled = image.resize((max(1, round(image.width * scale)), max(1, round(image.height * scale))), resample)
        canvas = Image.new("RGBA", size, (0, 0, 0, 0))
        canvas.alpha_composite(scaled, ((target_w - scaled.width) // 2, (target_h - scaled.height) // 2))
        return canvas
    if mode == "cover":
        scale = max(target_w / image.width, target_h / image.height)
        scaled = image.resize((max(target_w, round(image.width * scale)), max(target_h, round(image.height * scale))), resample)
        left = (scaled.width - target_w) // 2
        top = (scaled.height - target_h) // 2
        return scaled.crop((left, top, left + target_w, top + target_h))
    raise ValueError(f"unknown mode {mode!r}")


def main() -> None:
    """Parse arguments and run the chosen subcommand."""
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    sub = parser.add_subparsers(dest="command", required=True)
    list_cmd = sub.add_parser("pieces", help="list opaque pieces")
    list_cmd.add_argument("src", type=Path)
    list_cmd.add_argument("--gap", type=int, default=2)
    fit_cmd = sub.add_parser("fit", help="crop and fit to an exact size")
    fit_cmd.add_argument("src", type=Path)
    fit_cmd.add_argument("out", type=Path)
    fit_cmd.add_argument("--size", type=parse_size, required=True)
    fit_cmd.add_argument("--box", type=parse_box)
    fit_cmd.add_argument("--trim", action="store_true", help="trim transparent borders after cropping")
    fit_cmd.add_argument("--mode", choices=("stretch", "contain", "cover"), default="contain")
    fit_cmd.add_argument("--resample", choices=("nearest", "lanczos"), default="nearest")
    fit_cmd.add_argument("--opaque", action="store_true")
    fit_cmd.add_argument("--margin", type=int, default=0)
    args = parser.parse_args()

    image = Image.open(args.src).convert("RGBA")
    if args.command == "pieces":
        for box in pieces(image, args.gap):
            print(",".join(str(v) for v in box), f"({box[2] - box[0]}x{box[3] - box[1]})")
        return
    if args.box:
        image = image.crop(args.box)
    if args.trim:
        used = image.getchannel("A").getbbox()
        if used:
            image = image.crop(used)
    resample = Image.Resampling.NEAREST if args.resample == "nearest" else Image.Resampling.LANCZOS
    result = fit(image, args.size, args.mode, resample, args.margin)
    if args.opaque:
        flat = Image.new("RGBA", result.size, (0, 0, 0, 255))
        flat.alpha_composite(result)
        result = flat.convert("RGB")
    args.out.parent.mkdir(parents=True, exist_ok=True)
    result.save(args.out, optimize=True)
    print(f"FIT={args.out} size={result.size[0]}x{result.size[1]} mode={args.mode}")


if __name__ == "__main__":
    main()
