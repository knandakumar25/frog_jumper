"""Build before/after comparison sheets for replaced images (for example SpriteCook art
against the old Blender renders restored from git into a scratch folder).

Run with any Python that has Pillow and numpy, for example:

    uv run --system-certs --with pillow --with numpy python tools/assets/compare_images.py \
        --before <folder of old PNGs> --after <folder of new PNGs> --out <sheet.png>

It ran inside Blender's bundled Python until Blender was retired on 2026-11-11.

Images are paired by file name. For each pair the sheet shows the same 3x-enlarged crop
from both versions on a checkerboard (old on the left, new on the right), centred on the
window of the new image with the most horizontal alpha changes, and prints the
edge-softness score of each (see ``audit_images.edge_softness``). A pair whose sizes
differ, or a file missing from ``--after``, counts as a failure, because the game relies
on unchanged names, dimensions and anchors; the exit status is 1 if any occur. Only the
sheet is written.
"""
import argparse
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
from audit_images import edge_softness, pixels  # noqa: E402

CROP = 160   # source pixels per side of each crop
ZOOM = 3     # nearest-neighbour enlargement so edge pixels stay visible


def busiest_edge(alpha):
    """Return the (row, col) centre of the CROP window with the most horizontal alpha changes."""
    h, w = alpha.shape
    edge = np.abs(np.diff(alpha, axis=1, prepend=alpha[:, :1])) > 0
    best, centre = -1, (h // 2, w // 2)
    # Window centres step by half a crop; the last one is pinned to the far edge so the
    # final rows and columns are searched too.
    def centres(size):
        last = max(CROP // 2, size - CROP // 2)
        return sorted(set(list(range(CROP // 2, last, CROP // 2)) + [last]))
    for r in centres(h):
        for c in centres(w):
            score = int(edge[r - CROP // 2:r + CROP // 2, c - CROP // 2:c + CROP // 2].sum())
            if score > best:
                best, centre = score, (r, c)
    return centre


def crop(px, centre):
    """Cut a CROP-sized RGBA window around ``centre``, composite it over a checkerboard, and enlarge it."""
    h, w, _ = px.shape
    r = min(max(centre[0], CROP // 2), max(CROP // 2, h - CROP // 2))
    c = min(max(centre[1], CROP // 2), max(CROP // 2, w - CROP // 2))
    window = px[max(0, r - CROP // 2):r + CROP // 2, max(0, c - CROP // 2):c + CROP // 2]
    canvas = np.zeros((CROP, CROP, 4), np.float32)
    canvas[:window.shape[0], :window.shape[1]] = window
    yy, xx = np.mgrid[0:CROP, 0:CROP]
    checker = np.where(((yy // 8) + (xx // 8)) % 2, 0.45, 0.30)[..., None]
    a = canvas[:, :, 3:4]
    rgb = canvas[:, :, :3] * a + checker * (1 - a)
    out = np.concatenate([rgb, np.ones_like(a)], axis=2)
    return out.repeat(ZOOM, 0).repeat(ZOOM, 1)


def save(array, path):
    """Write a (height, width, 4) float array in 0..1, rows top-down, as an 8-bit RGBA PNG."""
    data = np.clip(np.rint(np.asarray(array) * 255.0), 0, 255).astype(np.uint8)
    Image.fromarray(data, 'RGBA').save(path)


def main():
    """Pair images by name, print scores, and write one stacked comparison sheet."""
    parser = argparse.ArgumentParser()
    parser.add_argument('--before', type=Path, required=True)
    parser.add_argument('--after', type=Path, required=True)
    parser.add_argument('--out', type=Path, required=True)
    args = parser.parse_args()
    rows, failures = [], 0
    for old_path in sorted(args.before.glob('*.png')):
        new_path = args.after / old_path.name
        if not new_path.exists():
            print(f'MISSING {old_path.name}')
            failures += 1
            continue
        old, _ = pixels(old_path)
        new, _ = pixels(new_path)
        if old.shape != new.shape:
            print(f'SIZE CHANGED {old_path.name}: {old.shape[:2]} -> {new.shape[:2]}')
            failures += 1
            continue
        print(f'{old_path.name}: edge softness {edge_softness(old[:, :, 3])} -> {edge_softness(new[:, :, 3])}')
        centre = busiest_edge(new[:, :, 3])
        gap = np.ones((CROP * ZOOM, 8, 4), np.float32)
        rows.append(np.concatenate([crop(old, centre), gap, crop(new, centre)], axis=1))
    if rows:
        args.out.parent.mkdir(parents=True, exist_ok=True)
        # Rows run top-down, so the first pair is at the top of the sheet.
        save(np.concatenate(rows, axis=0), args.out)
        print(f'sheet -> {args.out}')
    print(f'{failures} failure(s)')
    sys.exit(1 if failures else 0)


if __name__ == '__main__':
    main()
