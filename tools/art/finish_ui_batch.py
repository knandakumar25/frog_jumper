"""Finish the approved existing SpriteCook UI batch without generating new images.

Accepts the downloaded detailed three-button sheet. Extracts its three rows,
restores icon references, and records hashes/provenance for the delivered batch.
Run after render_titles.py. Existing unrelated manifest entries are preserved.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SOURCES = {
    "buttons": "90aa8c09-2693-4661-9dcc-2c70d3024ceb",
    "speech": "b33dbb5b-cf02-4d59-a9c5-19a70dc5d6f7",
    "icon": "bbcd60a0-c881-456b-bce3-d77300317c20",
    "title": "841996c4-b6bd-414c-8796-afe3de85c908",
    "background": "aedd07c1-d20c-475a-a9e9-922766be1e3a",
}


def main() -> None:
    """Extract approved crops, restore configuration, and merge manifest file records."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("buttons", type=Path)
    args = parser.parse_args()
    sheet = Image.open(args.buttons).convert("RGBA")
    for name, top, bottom in (("normal", 110, 345), ("hover", 470, 705), ("pressed", 825, 1060)):
        # Discard the generated background outside each rectangular button.
        crop = sheet.crop((65, top, 1985, bottom))
        crop.resize((800, 140), Image.Resampling.NEAREST).save(ROOT / f"assets/images/cyber_button_{name}.png")
    presets = ROOT / "export_presets.cfg"
    text = presets.read_text(encoding="utf-8")
    text = text.replace('application/icon=""', 'application/icon="res://assets/icons/axiom_force.ico"')
    text = text.replace('icons/icon_1024x1024=""', 'icons/icon_1024x1024="res://assets/icons/axiom_force.png"')
    presets.write_text(text, encoding="utf-8")
    for name in ("index.html", "website/index.html"):
        path = ROOT / name
        text = path.read_text(encoding="utf-8")
        if 'rel="icon"' not in text:
            text = text.replace("<head>", '<head>\n    <link rel="icon" type="image/svg+xml" href="/favicon.svg" />')
        path.write_text(text, encoding="utf-8")
    manifest_path = ROOT / "spritecook-assets.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    for label, asset_id in SOURCES.items():
        if not any(a["asset_id"] == asset_id for a in manifest["assets"]):
            manifest["assets"].append({"asset_id": asset_id, "label": f"Approved UI batch: {label}"})
    paths = list((ROOT / "assets/images").glob("axiom_force_title_2d*.png"))
    paths += list((ROOT / "website/public/images").glob("axiom_force_title_2d*.png"))
    paths += list((ROOT / "assets/images").glob("cyber_button_*.png"))
    paths += [ROOT / p for p in ("assets/textures/cyber_speech_frame.png", "assets/textures/cyber_speech_pointer.png",
                                "assets/icons/axiom_force.png", "assets/icons/axiom_force.ico",
                                "website/public/favicon.svg", "assets/images/menu_backdrops/city_base.png")]
    for path in paths:
        rel = path.relative_to(ROOT).as_posix()
        key = "title" if "title_2d" in rel else "buttons" if "cyber_button" in rel else "speech" if "speech" in rel else "background" if "city_base" in rel else "icon"
        entry = {"path": rel, "sha12": hashlib.sha256(path.read_bytes()).hexdigest()[:12], "source": SOURCES[key]}
        manifest["files"] = [f for f in manifest["files"] if f["path"] != rel] + [entry]
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(f"Recorded {len(paths)} UI batch files; no generation or credits used")


if __name__ == "__main__":
    main()
