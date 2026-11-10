"""Measure the quality of every project image and write a JSON report.

Run with Blender's bundled Python, which provides numpy and image loading:

    blender --background --factory-startup --python tools/assets/audit_images.py -- --out .validation/asset_quality/images.json

Inputs: all PNG/JPG files under ``assets/`` and ``website/public/``, except inside any
``.godot``, ``addons``, ``reports`` or ``.validation`` folder. Nothing is modified.
``--out`` is resolved against the repository root.

Per image it records:
* ``width``, ``height`` and ``bytes`` (file size);
* ``transparent_share``: the share of fully transparent pixels;
* ``edge_softness``: the share of transparent-boundary pixels whose alpha is below
  0.999. Smooth, anti-aliased cut-outs score high; jagged, 1-sample renders score close
  to 0. It is ``None`` when the image has fewer than 50 boundary pixels or less than 0.1%
  transparent area (opaque images included);
* ``padding``: empty transparent margin on each side, as a share of the image;
* ``content_hash``: SHA-1 of the file, used to find exact duplicates;
* ``depth``: Blender's bits per pixel (for example 32 for 8-bit RGBA);
* ``source``: the generator script for the image, the string ``'third-party (CC0)'`` for
  stand-ins, or ``None`` for art with no generator script (hand-made or painted), which
  cannot be re-rendered;
* ``duplicates``: other audited paths with identical content (only when there are any);
* ``error``: the load error, instead of the figures, for a file Blender cannot read.
"""
import argparse
import hashlib
import json
import sys
from pathlib import Path

import bpy
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
# Folder names skipped wherever they appear in a path.
SKIP = ('.godot', 'addons', 'reports', '.validation')

# Folder or file path prefix -> script that produces it. Longest prefix wins.
SOURCES = {
    'assets/cutscenes/': 'tools/cutscene/render_*.py',
    'assets/environments/later_campaign/': 'tools/cutscene/render_later_campaign.py',
    'assets/environment/': 'tools/gameplay/*.py',
    'assets/world/': 'tools/world/build_kit.py',
    'assets/characters/rendered/': 'tools/art/draw_flat_characters.py',
    'assets/characters/shogun_study/campaign_atlas.png': 'tools/art/prepare_campaign_shogun.py',
    'assets/images/menu_backdrops/': 'tools/art/render_menu_backdrops.py',
    'assets/images/level1/ghost_gridlock.png': 'tools/art/render_level1_iso.py',
    'assets/stand_in/': 'third-party (CC0)',
}


def source_for(rel):
    """Return the generator for ``rel`` using the longest matching prefix, or None."""
    best = None
    for prefix, script in SOURCES.items():
        if rel.startswith(prefix) and (best is None or len(prefix) > len(best[0])):
            best = (prefix, script)
    return best[1] if best else None


def pixels(path):
    """Load an image; return (height x width x 4 float32 array, bits per pixel). Frees the datablock.

    The path is made absolute first: Blender does not resolve a relative path against the
    process's working directory, so ``.validation/...`` would fail to load.
    """
    image = bpy.data.images.load(str(Path(path).resolve()), check_existing=False)
    try:
        w, h = image.size
        data = np.empty(w * h * 4, dtype=np.float32)
        image.pixels.foreach_get(data)
        return data.reshape(h, w, 4), image.depth
    finally:
        bpy.data.images.remove(image)


def edge_softness(alpha):
    """Share of boundary pixels with partial alpha; None when there is no transparent edge.

    A boundary pixel is any non-transparent pixel with a transparent 4-neighbour.
    """
    solid = alpha > 0.0
    clear = ~solid
    near_clear = np.zeros_like(solid)
    near_clear[1:, :] |= clear[:-1, :]
    near_clear[:-1, :] |= clear[1:, :]
    near_clear[:, 1:] |= clear[:, :-1]
    near_clear[:, :-1] |= clear[:, 1:]
    boundary = solid & near_clear
    count = int(boundary.sum())
    if count < 50 or clear.mean() < 0.001:
        return None
    partial = boundary & (alpha < 0.999)
    return round(float(partial.sum()) / count, 3)


def padding(alpha):
    """Empty margin per side (left, top, right, bottom) as a share of width/height."""
    rows = np.where(alpha.max(axis=1) > 0)[0]
    cols = np.where(alpha.max(axis=0) > 0)[0]
    h, w = alpha.shape
    if len(rows) == 0:
        return [1.0, 1.0, 1.0, 1.0]
    # Blender stores rows bottom-up, so the first non-empty row is the bottom margin.
    return [round(cols[0] / w, 3), round((h - 1 - rows[-1]) / h, 3),
            round((w - 1 - cols[-1]) / w, 3), round(rows[0] / h, 3)]


def main():
    """Audit every image and write the JSON report named by ``--out``."""
    parser = argparse.ArgumentParser()
    parser.add_argument('--out', type=Path, required=True)
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
    files = sorted(p for base in ('assets', 'website/public') for p in (ROOT / base).rglob('*')
                   if p.suffix.lower() in ('.png', '.jpg', '.jpeg'))
    report = []
    for path in files:
        rel = path.relative_to(ROOT).as_posix()
        if any(part in SKIP for part in Path(rel).parts[:-1]):
            continue
        entry = {'path': rel, 'bytes': path.stat().st_size, 'source': source_for(rel),
                 'content_hash': hashlib.sha1(path.read_bytes()).hexdigest()}
        try:
            px, depth = pixels(path)
            alpha = px[:, :, 3]
            entry.update(width=px.shape[1], height=px.shape[0], depth=depth,
                         transparent_share=round(float((alpha <= 0).mean()), 3),
                         edge_softness=edge_softness(alpha), padding=padding(alpha))
        except Exception as exc:  # A broken file is itself a finding, so keep going.
            entry['error'] = str(exc)
        report.append(entry)
    groups = {}
    for entry in report:
        groups.setdefault(entry['content_hash'], []).append(entry['path'])
    for entry in report:
        twins = [p for p in groups[entry['content_hash']] if p != entry['path']]
        if twins:
            entry['duplicates'] = twins
    out = args.out if args.out.is_absolute() else ROOT / args.out
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(report, indent=1), encoding='utf-8')
    print(f'audited {len(report)} images -> {out}')


if __name__ == '__main__':
    main()
