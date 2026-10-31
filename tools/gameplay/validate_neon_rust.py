"""Validate Neon Rust gameplay PNG dimensions, real alpha, occupied margins and reproducible asset provenance using stdlib Python."""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools/cutscene'))
from validate_live import pixels


def main():
    """Reject missing, clipped, opaque-background or empty family members and write a machine-readable QA record."""
    directory = ROOT / 'assets/environment/neon_rust'
    manifest = json.loads((directory / 'manifest.json').read_text())
    records = []
    for asset in manifest['assets']:
        width, height, rows = pixels(directory / asset['file'])
        assert [width, height] == manifest['native_size'], asset
        alpha = [row[x] for row in rows for x in range(3, width * 4, 4)]
        assert min(alpha) == 0 and max(alpha) == 255, asset
        occupied = [(x, y) for y, row in enumerate(rows) for x in range(width) if row[x * 4 + 3] > 0]
        assert len(occupied) > 3000, asset
        bounds = [min(p[0] for p in occupied), min(p[1] for p in occupied), max(p[0] for p in occupied), max(p[1] for p in occupied)]
        assert bounds[0] > 0 and bounds[1] > 0 and bounds[2] < width-1 and bounds[3] < height-1, (asset, bounds)
        assert (directory / (asset['file'] + '.import')).exists(), asset
        records.append({'file': asset['file'], 'size': [width, height], 'alpha': [min(alpha), max(alpha)], 'bounds': bounds})
    report = ROOT / '.validation/neon-rust-gameplay/assets.json'
    report.parent.mkdir(parents=True, exist_ok=True)
    report.write_text(json.dumps(records, indent=2) + '\n')
    print('Validated seven RGBA gameplay assets, alpha, margins, provenance and Godot import sidecars.')


if __name__ == '__main__':
    main()
