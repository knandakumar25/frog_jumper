"""Normalize the approved Phase 5 SpriteCook sheets into 53 runtime layers.

Usage: python tools/art/build_later_campaign_art.py reports/phase5
The folder must contain later_4k_1..4.png, boss_2k.png and sheets.json
(asset IDs only). Uses existing pixel snapping and derivation rules; no network,
generation or credits. Writes game art, manifest records, and review composites.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

from snap_pixels import snap
from derive_layers import aftermath, lights

ROOT = Path(__file__).resolve().parents[2]
FAMILIES = ["cyber_grave", "foundry_forge", "stratos_docks", "spire_outer_hull",
            "cyber_nexus", "apex_bastion", "judgment_day"]


def quadrant(sheet: Image.Image, cell: int, columns: int = 2) -> Image.Image:
    """Extract a grid cell with a small inset to remove neighboring panel bleed."""
    w, h = sheet.size
    x, y = cell % columns, cell // columns
    return sheet.crop((x * w // columns + 4, y * h // columns + 4,
                       (x + 1) * w // columns - 4, (y + 1) * h // columns - 4))


def main() -> None:
    """Build complete families, check alpha/size, and merge durable provenance."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    args = parser.parse_args()
    sources = json.loads((args.source / "sheets.json").read_text(encoding="utf-8"))
    manifest_path = ROOT / "spritecook-assets.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    records = []
    review = Image.new("RGB", (960, 270 * 7), "#151b28")
    boss_review = Image.new("RGB", (768, 256 * 2), "#303641")
    for index, family in enumerate(FAMILIES):
        name = f"later_4k_{index // 2 + 1}"
        sheet = Image.open(args.source / (name + ".png"))
        base_cell = 0 if index % 2 == 0 else 2
        base_source = quadrant(sheet, base_cell)
        if family == "cyber_nexus":
            # Remove the generated 'AI' label from its small dark sign panel.
            draw = ImageDraw.Draw(base_source)
            w, h = base_source.size
            draw.rectangle((int(w*.475), int(h*.555), int(w*.525), int(h*.635)), fill="#172631")
        base = snap(base_source, (480, 270), "none", 96, 60).resize((1920, 1080), Image.Resampling.NEAREST)
        structure = snap(quadrant(sheet, base_cell + 1), (480, 270), "magenta", 96, 60,
                         despill=family not in ("judgment_day", "spire_outer_hull"))
        structure = structure.resize((1920, 1080), Image.Resampling.NEAREST)
        alpha = np.asarray(structure.getchannel("A"))
        rows = np.flatnonzero((alpha > 0).mean(axis=1) >= .95)
        if not len(rows):
            raise ValueError(f"{family}: no full-width floor found; review before shipping")
        # Find the deck, ignoring any full-width overhead machinery.
        rows = rows[rows > 540]
        # Apex's wall is already fully opaque at y541; its inspected deck begins at y808.
        floor = 808 if family == "apex_bastion" else int(rows[0])
        dy = 765 - floor
        shifted = Image.new("RGBA", structure.size)
        shifted.paste(structure, (0, dy))
        if dy < 0:
            # Continue the existing bottom slab through the gap left by upward alignment.
            strip = structure.crop((0, 1076, 1920, 1080))
            shifted.paste(strip.resize((1920, -dy), Image.Resampling.NEAREST), (0, 1080 + dy))
        structure = shifted
        layers = {"environment_base": base, "environment_structure": structure,
                  "environment_lights": lights(structure, .38, .35, .9),
                  "aftermath_structure": aftermath(structure, 50 + index)}
        if family != "judgment_day":
            layers["aftermath_base"] = aftermath(base, 50 + index)
        layers["aftermath_lights"] = lights(layers["aftermath_structure"], .38, .35, .9)
        if index > 0:
            boss_sheet = Image.open(args.source / "boss_2k.png")
            cell = index - 1
            armor = snap(quadrant(boss_sheet, cell, 3), (128, 128), "magenta", 96, 60,
                         despill=family not in ("judgment_day", "spire_outer_hull"))
            box = armor.getchannel("A").getbbox()
            if not box:
                raise ValueError(f"{family}: empty armor")
            armor = armor.crop(box)
            # Match old plates' broad torso coverage: 480 px becomes 216 at scale .45.
            armor = armor.resize((480, 460), Image.Resampling.NEAREST)
            canvas = Image.new("RGBA", (512, 512))
            armor_y = 4 if family == "judgment_day" else 26
            canvas.paste(armor, (16, armor_y))
            if family == "judgment_day":
                # Close the inspected neck gap with the plate's own dark collar colour.
                # This small mask hides the flat chassis showing through the hollow bust;
                # opaque source detail and the external silhouette remain unchanged.
                pixels = np.array(canvas)
                collar = pixels[140:260, 190:320]
                swatches = collar[collar[..., 3] > 0, :3]
                shade = swatches[np.argmin(swatches.astype(int).sum(axis=1))]
                neck = pixels[153:240, 214:292]
                gaps = neck[..., 3] == 0
                neck[gaps, :3] = shade
                neck[gaps, 3] = 255
                canvas = Image.fromarray(pixels, "RGBA")
            layers["boss_structure"] = canvas
            layers["boss_lights"] = lights(canvas, .38, .35, .9)
            preview = canvas.resize((230, 230), Image.Resampling.NEAREST)
            slot = ((cell % 3) * 256, (cell // 3) * 256)
            # Overlay the actual first chassis polygon from each boss, at runtime scale.
            chassis = ImageDraw.Draw(boss_review)
            ox, oy = slot[0] + 128, slot[1] + 128
            if cell == 0:
                for x, y, w, h in ((-78, -76, 156, 150), (-100, -55, 44, 96), (56, -55, 44, 96)):
                    chassis.rectangle((ox+x, oy+y, ox+x+w, oy+y+h), fill="#526172")
            else:
                import re
                boss_class = ("", "AeroCenturion", "SpireSentinel", "NexusOverseer", "BastionPraetor", "VoidCommander")[cell]
                code = (ROOT / "scripts/actors" / (boss_class + "Boss2D.cs")).read_text(encoding="utf-8")
                body = code.split("var chassis", 1)[1].split("Color =", 1)[0]
                points = [(ox+float(x), oy+float(y)) for x, y in re.findall(r'new Vector2\(([-\d.]+),\s*([-\d.]+)\)', body)]
                if len(points) >= 3:
                    chassis.polygon(points, fill="#526172")
            boss_review.paste(preview, (slot[0] + 13, slot[1] + 6), preview)
        for kind, image in layers.items():
            image = image.convert("RGBA")
            alpha = image.getchannel("A")
            extrema = alpha.getextrema()
            if kind.endswith("base"):
                assert extrema == (255, 255), (family, kind, extrema)
            else:
                assert extrema == (0, 255), (family, kind, extrema)
            path = ROOT / "assets/environments/later_campaign" / family / (kind + ".png")
            path.parent.mkdir(parents=True, exist_ok=True)
            image.save(path, optimize=True)
            source_name = "boss_2k" if kind.startswith("boss") else name
            source_cell = index - 1 if kind.startswith("boss") else base_cell if kind.endswith("base") else base_cell + 1
            applied_offset = (16, armor_y) if kind.startswith("boss") else (0, 0) if kind.endswith("base") else (0, dy)
            records.append({"path": path.relative_to(ROOT).as_posix(),
                            "sha12": hashlib.sha256(path.read_bytes()).hexdigest()[:12],
                            "source": sources[source_name]["asset_id"],
                            "recipe": f"{source_name}, family {family}, cell {source_cell}, offset {applied_offset}; {kind}; aftermath seed {50+index}"
                            + ("; collar mask (214,153)-(292,240), transparent pixels only, darkest opaque source collar swatch from (190,140)-(320,260)"
                               if family == "judgment_day" and kind.startswith("boss") else "")})
        for column, prefix in enumerate(("environment", "aftermath")):
            composite = base.copy() if family != "judgment_day" else Image.new("RGBA", base.size, "#101827")
            if prefix == "aftermath" and family != "judgment_day":
                composite = layers["aftermath_base"].copy()
            composite.alpha_composite(layers[prefix + "_structure"])
            composite.alpha_composite(layers[prefix + "_lights"])
            draw = ImageDraw.Draw(composite)
            draw.line((0, 765, 1920, 765), fill="#ffffff", width=2)
            review.paste(composite.convert("RGB").resize((480, 270)), (column*480, index*270))
        print(f"{family}: {len(layers)} files, floor {floor} -> 765")
    assert len(records) == 53, len(records)
    for source in sources.values():
        if not any(a["asset_id"] == source["asset_id"] for a in manifest["assets"]):
            manifest["assets"].append(source)
    paths = {r["path"] for r in records}
    manifest["files"] = [r for r in manifest["files"] if r["path"] not in paths] + records
    manifest_path.write_text(json.dumps(manifest, indent=1) + "\n", encoding="utf-8")
    review.save(args.source / "composites.png")
    boss_review.save(args.source / "boss-review.png")


if __name__ == "__main__":
    main()
