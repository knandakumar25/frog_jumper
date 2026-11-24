"""Typeset every supported locale's title onto the shared SpriteCook title plate (game and web).

Usage (inside the cutscene image, which has Pillow with libraqm):

    docker compose --profile cutscene run --rm cutscene tools/art/render_titles.py PLATE.png

``PLATE.png`` is the word-free title plate already keyed to transparency, for example the Phase 7
plate snapped to the 480x147 working grid with ``snap_pixels.py --key magenta``. Any size works:
the plate is cropped to its opaque pixels and fitted (aspect kept, nearest-neighbour) into a
480x147 working canvas, which is a quarter of the final 1920x588 title size.

For each language in ``scripts/managers/LocaleCatalog.cs`` the script:

1. picks the wording: ``TITLES`` for scripts with their own title (the owner's 2026-11-10 wording),
   otherwise the English "AXIOM FORCE";
2. picks the font the catalog lists for that language (Orbitron for Latin scripts), with the
   ``FONT_OVERRIDES`` exceptions (Arabic uses Noto Sans Arabic as before; Vietnamese uses Noto
   Sans because Orbitron lacks its letters), stops if any character would draw as a missing-glyph
   box, and shrinks the font until the text fits ``--max-width``;
3. draws a soft pink glow, then the pale cyan text with a thin dark outline, centred on the
   banner at (240, ``--text-y``); right-to-left languages are shaped with ``direction="rtl"``;
4. enlarges the canvas 4x with nearest-neighbour to 1920x588 and writes it to both
   ``assets/images`` and optionally the shared website's ``public/images`` (``axiom_force_title_2d.png`` for English,
   ``axiom_force_title_2d_<key>.png`` otherwise).

No generation or network requests occur. Complex scripts (Arabic letter joining, right-to-left
order, Thai vowel marks) need Pillow's libraqm, so the script stops if it is missing; the Windows
host Pillow lacks it, which is why ``tools/cutscene/Dockerfile`` installs ``libraqm0``.
"""
from __future__ import annotations

import argparse
import re
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont, features

ROOT = Path(__file__).resolve().parents[2]
WORK = (480, 147)          # working canvas; the saved title is exactly 4x this (1920x588)
SCALE = 4
TITLES = {
    "arabic": "قوة أكسيوم", "japanese": "アクシオム・フォース",
    "korean": "엑시옴 포스", "mandarin": "公理部队", "hebrew": "כוח אקסיום",
    "traditionalchinese": "公理部隊", "indonesian": "PASUKAN AKSIOMA",
    "vietnamese": "LỰC LƯỢNG TIÊN ĐỀ", "thai": "กองกำลังสัจพจน์",
}
TEXT_FILL, TEXT_OUTLINE, GLOW = "#d9ffff", "#071526", "#e7a6e4"
FONT_OVERRIDES = {"arabic": "NotoSansArabic.ttf", "vietnamese": "NotoSans-Regular.ttf"}


def fit_plate(plate: Image.Image) -> Image.Image:
    """Crop the plate to its opaque pixels and centre it in the 480x147 canvas at a fitted size.

    Nearest-neighbour keeps pixel art crisp; a plate already 480x147 with a 1:1 fit is unchanged.
    """
    plate = plate.convert("RGBA")
    box = plate.getchannel("A").getbbox()
    if not box:
        raise ValueError("the plate has no opaque pixels")
    art = plate.crop(box)
    k = min(WORK[0] / art.width, WORK[1] / art.height)
    if k < 1 or k >= 2:   # leave small sub-integer differences alone: they would blur pixel blocks
        art = art.resize((max(1, round(art.width * k)), max(1, round(art.height * k))), Image.Resampling.NEAREST)
    canvas = Image.new("RGBA", WORK, (0, 0, 0, 0))
    canvas.alpha_composite(art, ((WORK[0] - art.width) // 2, (WORK[1] - art.height) // 2))
    return canvas


def languages() -> list[tuple[str, bool, str]]:
    """(key, right_to_left, font file) for every language in LocaleCatalog.cs, in catalog order."""
    catalog = (ROOT / "scripts/managers/LocaleCatalog.cs").read_text(encoding="utf-8")
    found = re.findall(r'new\("([^\"]+)", "([^\"]+)", "[^\"]+"(?:, (true|false), "([^\"]+)")?\)', catalog)
    if not found:
        raise RuntimeError("no languages found in LocaleCatalog.cs; has its format changed?")
    out = []
    for key, _iso, rtl, font_name in found:
        # The catalog's Arabic UI font (Cairo) lacks a heavy display weight; the old titles used Noto.
        # Orbitron has no Vietnamese letters (Ư, Ự, Đ...), so Vietnamese uses Noto Sans.
        font_name = FONT_OVERRIDES.get(key) or font_name or "Orbitron-VariableFont.ttf"
        out.append((key, rtl == "true", font_name))
    return out


def missing_glyphs(font: ImageFont.FreeTypeFont, text: str) -> list[str]:
    """Characters of ``text`` that ``font`` draws as its missing-glyph box (.notdef).

    A missing character still renders as a box, so drawing alone cannot reveal it; each character
    is drawn alone and compared with a private-use code point that no font here defines.
    """
    def drawn(ch: str) -> bytes:
        """Greyscale pixels of ``ch`` drawn alone on a canvas big enough for any glyph."""
        side = int(font.size * 3)
        image = Image.new("L", (side, side), 0)
        ImageDraw.Draw(image).text((font.size, font.size), ch, font=font, fill=255)
        return image.tobytes()

    notdef = drawn("\U000F0000")
    return sorted({c for c in text if not c.isspace() and drawn(c) == notdef})


def render(plate: Image.Image, text: str, font_path: Path, rtl: bool, text_y: int, max_width: int,
           max_size: int) -> Image.Image:
    """Draw one title onto a copy of the fitted plate and return it at the final 1920x588 size."""
    canvas = plate.copy()
    draw = ImageDraw.Draw(canvas)
    direction = "rtl" if rtl else "ltr"
    size = max_size
    font = ImageFont.truetype(str(font_path), size)
    missing = missing_glyphs(font, text)
    if missing:
        raise ValueError(f"{font_path.name} has no glyph for {missing!r} in {text!r}; add a FONT_OVERRIDES entry")
    # Shrink until the shaped text (stroke included) fits between the banner's bolt clusters.
    while size > 6 and draw.textbbox((0, 0), text, font=font, direction=direction, stroke_width=2)[2] > max_width:
        size -= 1
        font = ImageFont.truetype(str(font_path), size)
    anchor_xy = (WORK[0] // 2, text_y)
    # A wider pink stroke first gives the neon glow of the old titles; the text then sits on top.
    draw.text(anchor_xy, text, font=font, anchor="mm", direction=direction, fill=GLOW, stroke_width=2, stroke_fill=GLOW)
    draw.text(anchor_xy, text, font=font, anchor="mm", direction=direction, fill=TEXT_FILL, stroke_width=1, stroke_fill=TEXT_OUTLINE)
    return canvas.resize((WORK[0] * SCALE, WORK[1] * SCALE), Image.Resampling.NEAREST)


def main() -> None:
    """Render all catalog languages and optionally update the shared website checkout."""
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("plate", type=Path, help="word-free plate, keyed to transparency")
    parser.add_argument("--text-y", type=int, default=93, help="banner centre line in the 480x147 canvas")
    parser.add_argument("--max-width", type=int, default=330, help="widest text allowed, in canvas pixels")
    parser.add_argument("--max-size", type=int, default=40, help="largest font size, in canvas pixels")
    parser.add_argument("--website-root", type=Path, help="shared website checkout for matching copies")
    args = parser.parse_args()
    folders = [ROOT / "assets/images"]
    if args.website_root is not None:
        website_images = args.website_root / "public/images"
        if not website_images.is_dir():
            parser.error("--website-root must contain public/images")
        folders.append(website_images)
    if not features.check_feature("raqm"):
        raise RuntimeError("libraqm is required for Arabic, Hebrew and Thai titles; run in the cutscene image")
    plate = fit_plate(Image.open(args.plate))
    count = 0
    for key, rtl, font_name in languages():
        text = TITLES.get(key, "AXIOM FORCE")
        result = render(plate, text, ROOT / "assets/fonts" / font_name, rtl, args.text_y, args.max_width, args.max_size)
        name = "axiom_force_title_2d" + ("" if key == "english" else "_" + key) + ".png"
        for folder in folders:
            target = folder / name
            folder.mkdir(parents=True, exist_ok=True)
            result.save(target, optimize=True)
        count += 1
    print(f"Rendered {count} titles in {len(folders)} output directories")


if __name__ == "__main__":
    main()
