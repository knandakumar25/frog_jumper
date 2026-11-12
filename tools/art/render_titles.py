"""Fit an existing SpriteCook title plate and typeset all supported locales for game/web.

Usage: python tools/art/render_titles.py PATH_TO_DOWNLOADED_PLATE
No generation or network requests occur. Fonts and wording come from the project's
locale catalog and website translations. Complex scripts require Pillow's libraqm.
"""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont, features

ROOT = Path(__file__).resolve().parents[2]
TITLES = {
    "arabic": "قوة أكسيوم", "japanese": "アクシオム・フォース",
    "korean": "엑시옴 포스", "mandarin": "公理部队", "hebrew": "כוח אקסיום",
    "traditionalchinese": "公理部隊", "indonesian": "PASUKAN AKSIOMA",
    "vietnamese": "LỰC LƯỢNG TIÊN ĐỀ", "thai": "กองกำลังสัจพจน์",
}


def main() -> None:
    """Render a shared plate with shaped localized text, then copy exact game files to web."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("plate", type=Path)
    args = parser.parse_args()
    if not features.check_feature("raqm"):
        raise RuntimeError("libraqm is required for Arabic, Hebrew and Thai titles")
    catalog = (ROOT / "scripts/managers/LocaleCatalog.cs").read_text(encoding="utf-8")
    languages = re.findall(r'new\("([^\"]+)", "([^\"]+)", "[^\"]+"(?:, (true|false), "([^\"]+)")?\)', catalog)
    plate = Image.open(args.plate).convert("RGBA")
    # Keep the entire winged emblem above the banner and its lower glow.
    plate = plate.crop((0, 12, plate.width, 103)).resize((480, 147), Image.Resampling.NEAREST)
    for key, iso, rtl, font_name in languages:
        canvas = plate.copy()
        draw = ImageDraw.Draw(canvas)
        direction = "rtl" if rtl == "true" else "ltr"
        font_name = "NotoSansArabic.ttf" if key == "arabic" else font_name
        font_path = ROOT / "assets/fonts" / (font_name or "Orbitron-VariableFont.ttf")
        # Preserve the owner's original title wording, including English in Latin locales.
        for text, y, max_size in ((TITLES.get(key, "AXIOM FORCE"), 77, 31),):
            size = max_size
            font = ImageFont.truetype(str(font_path), size)
            while draw.textbbox((0, 0), text, font=font, direction=direction)[2] > 380 and size > 5:
                size -= 1
                font = ImageFont.truetype(str(font_path), size)
            draw.text((240, y), text, font=font, anchor="mm", direction=direction,
                      fill="#d9ffff", stroke_width=1, stroke_fill="#071526")
        name = "axiom_force_title_2d" + ("" if key == "english" else "_" + key) + ".png"
        result = canvas.resize((1920, 588), Image.Resampling.NEAREST)
        for folder in ("assets/images", "website/public/images"):
            target = ROOT / folder / name
            target.parent.mkdir(parents=True, exist_ok=True)
            result.save(target)
    print(f"Rendered {len(languages)} game titles and matching website copies")


if __name__ == "__main__":
    main()
