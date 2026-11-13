"""Make layers locally from other art, so they cost no SpriteCook credits.

Images in ``docs/art_regeneration.json`` whose ``method`` starts with ``derive:`` come from
here (see ``docs/ART_REGENERATION.md``, "Methods"). Every subcommand reads finished game
files and writes one or more files. It makes no network calls.

Subcommands:

    lights SRC OUT
        A transparent lights layer: the bright, saturated pixels of SRC (window glow, neon,
        signal lamps) kept fully opaque, everything else fully transparent. The game pulses
        this layer over the structure, so only light sources should survive.
        --min-value V    brightness (HSV value, 0-1) a pixel needs (default 0.72)
        --min-sat S      saturation a coloured light needs (default 0.45)
        --white V        brightness at which unsaturated (white) pixels also count (default 0.93)
        Fails if the result is all transparent or all opaque: the cutscene validators need
        some alpha 0 and some alpha 255 in every non-base layer.

    aftermath SRC OUT [--seed N]
        The later-campaign aftermath version of an environment layer: desaturated, darker,
        tinted toward ember orange, with a smoke haze over the upper part and scattered
        ember pixels. Alpha is unchanged, so a base stays opaque and a structure keeps its
        cut-out. The seed makes the embers repeatable.

    drone ATLAS STRUCTURE_OUT [--lights LIGHTS_OUT]
        The deployment drone layers. The game draws only the box x 840-1080, y 440-500 of
        ``drone_structure.png``/``drone_lights.png`` (Phase 2, "Fixed positions"). Frame 0 of
        the ``vanguard`` idle row (the 256x256 cell at the atlas origin) is trimmed, scaled to
        fill that box with nearest-neighbour, and placed on a transparent 1920x1080 canvas.
        ``--lights`` also writes its lights layer.

    icon SRC --ico OUT.ico --svg OUT.svg
        The Windows icon (16 to 256 px) and the website favicon (an SVG with a 100x100
        viewBox wrapping a pixelated PNG, the format used before) from the 1254x1254 icon PNG.

Run from the project root, for example:

    uv run --system-certs --no-project --with pillow python tools/art/derive_layers.py lights \
        assets/cutscenes/deployment/live/city_structure.png assets/cutscenes/deployment/live/city_lights.png
"""
from __future__ import annotations

import argparse
import base64
import colorsys
import io
import random
from pathlib import Path

from PIL import Image

CANVAS = (1920, 1080)
DRONE_BOX = (840, 440, 1080, 500)
ATLAS_CELL = 256


def lights(source: Image.Image, min_value: float, min_sat: float, white: float) -> Image.Image:
    """Keep only light-source pixels of ``source`` (see the ``lights`` subcommand).

    Returns:
        An RGBA image the size of ``source`` whose kept pixels are fully opaque and whose
        other pixels are fully transparent (alpha 0 or 255 only).

    Raises:
        ValueError: if no pixel or every pixel is kept, which would fail the validators.
    """
    rgba = source.convert("RGBA")
    out = Image.new("RGBA", rgba.size, (0, 0, 0, 0))
    src, dst = rgba.load(), out.load()
    kept = 0
    for y in range(rgba.height):
        for x in range(rgba.width):
            r, g, b, a = src[x, y]
            if a == 0:
                continue
            _, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            if (v >= min_value and s >= min_sat) or v >= white:
                dst[x, y] = (r, g, b, 255)
                kept += 1
    total = rgba.width * rgba.height
    if kept == 0:
        raise ValueError("no pixel is bright enough; lower --min-value or --min-sat")
    if kept == total:
        raise ValueError("every pixel was kept; raise --min-value")
    return out


def aftermath(source: Image.Image, seed: int, darken: float = 0.62, desaturate: float = 0.55,
              haze: float = 0.35, ember_rate: float = 0.0015) -> Image.Image:
    """Grade an environment layer into its aftermath version (see the ``aftermath`` subcommand).

    Args:
        source: the environment layer (base or structure).
        seed: random seed for the ember pixels, so reruns give the same file.
        darken: brightness multiplier after desaturation.
        desaturate: 0 keeps colour, 1 makes grey.
        haze: strength of the grey-brown smoke at the top edge; it fades to 0 at 60% height.
        ember_rate: share of opaque pixels turned into bright ember sparks.

    Returns:
        A new RGBA image with the source alpha unchanged.
    """
    rgba = source.convert("RGBA")
    out = rgba.copy()
    pixels = out.load()
    rng = random.Random(seed)
    smoke = (74, 62, 58)
    ember_colours = ((255, 150, 60), (255, 196, 92), (232, 92, 40))
    fade_end = rgba.height * 0.6
    for y in range(rgba.height):
        smoke_weight = haze * max(0.0, 1 - y / fade_end)
        for x in range(rgba.width):
            r, g, b, a = pixels[x, y]
            if a == 0:
                continue
            grey = 0.299 * r + 0.587 * g + 0.114 * b
            # Desaturate, darken, then warm the shadows slightly toward the fire's glow.
            r2 = (r + (grey - r) * desaturate) * darken * 1.06
            g2 = (g + (grey - g) * desaturate) * darken * 0.97
            b2 = (b + (grey - b) * desaturate) * darken * 0.88
            r2 += (smoke[0] - r2) * smoke_weight
            g2 += (smoke[1] - g2) * smoke_weight
            b2 += (smoke[2] - b2) * smoke_weight
            if rng.random() < ember_rate:
                r2, g2, b2 = rng.choice(ember_colours)
            pixels[x, y] = (min(255, round(r2)), min(255, round(g2)), min(255, round(b2)), a)
    return out


def drone(atlas: Image.Image) -> Image.Image:
    """Place the ``vanguard`` idle frame 0 into the deployment drone box on a 1920x1080 canvas."""
    frame = atlas.convert("RGBA").crop((0, 0, ATLAS_CELL, ATLAS_CELL))
    used = frame.getchannel("A").getbbox()
    if not used:
        raise ValueError("the atlas's first cell is empty; is this the vanguard atlas?")
    art = frame.crop(used)
    box_w, box_h = DRONE_BOX[2] - DRONE_BOX[0], DRONE_BOX[3] - DRONE_BOX[1]
    # Keep the aspect ratio and leave a 1-pixel margin so nothing touches the drawn region's edge.
    scale = min((box_w - 2) / art.width, (box_h - 2) / art.height)
    art = art.resize((max(1, round(art.width * scale)), max(1, round(art.height * scale))), Image.Resampling.NEAREST)
    canvas = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    canvas.alpha_composite(art, (DRONE_BOX[0] + (box_w - art.width) // 2, DRONE_BOX[1] + (box_h - art.height) // 2))
    return canvas


def favicon_svg(icon: Image.Image) -> str:
    """Wrap a 100x100 pixelated copy of the icon in an SVG, as the old favicon did."""
    small = icon.convert("RGBA").resize((100, 100), Image.Resampling.NEAREST)
    buffer = io.BytesIO()
    small.save(buffer, format="PNG", optimize=True)
    data = base64.b64encode(buffer.getvalue()).decode("ascii")
    return ('<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" '
            'viewBox="0 0 100 100" width="100" height="100"><image width="100" height="100" '
            f'style="image-rendering:pixelated" href="data:image/png;base64,{data}"/></svg>\n')


def save(image: Image.Image, path: Path) -> None:
    """Create the folder if needed, save, and print what was written."""
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, optimize=True)
    print(f"WROTE {path} {image.width}x{image.height}")


def main(argv: list[str] | None = None) -> None:
    """Parse arguments and run one subcommand (see the module docstring)."""
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    sub = parser.add_subparsers(dest="command", required=True)
    light_cmd = sub.add_parser("lights")
    light_cmd.add_argument("src", type=Path)
    light_cmd.add_argument("out", type=Path)
    light_cmd.add_argument("--min-value", type=float, default=0.72)
    light_cmd.add_argument("--min-sat", type=float, default=0.45)
    light_cmd.add_argument("--white", type=float, default=0.93)
    after_cmd = sub.add_parser("aftermath")
    after_cmd.add_argument("src", type=Path)
    after_cmd.add_argument("out", type=Path)
    after_cmd.add_argument("--seed", type=int, default=7)
    drone_cmd = sub.add_parser("drone")
    drone_cmd.add_argument("atlas", type=Path)
    drone_cmd.add_argument("structure_out", type=Path)
    drone_cmd.add_argument("--lights", type=Path)
    icon_cmd = sub.add_parser("icon")
    icon_cmd.add_argument("src", type=Path)
    icon_cmd.add_argument("--ico", type=Path, required=True)
    icon_cmd.add_argument("--svg", type=Path, required=True)
    args = parser.parse_args(argv)

    if args.command == "lights":
        save(lights(Image.open(args.src), args.min_value, args.min_sat, args.white), args.out)
    elif args.command == "aftermath":
        save(aftermath(Image.open(args.src), args.seed), args.out)
    elif args.command == "drone":
        structure = drone(Image.open(args.atlas))
        save(structure, args.structure_out)
        if args.lights:
            save(lights(structure, 0.72, 0.45, 0.93), args.lights)
    elif args.command == "icon":
        icon = Image.open(args.src).convert("RGBA")
        args.ico.parent.mkdir(parents=True, exist_ok=True)
        icon.save(args.ico, sizes=[(s, s) for s in (16, 32, 48, 64, 128, 256)])
        print(f"WROTE {args.ico}")
        args.svg.parent.mkdir(parents=True, exist_ok=True)
        args.svg.write_text(favicon_svg(icon), encoding="utf-8")
        print(f"WROTE {args.svg}")


if __name__ == "__main__":
    main()
