"""Validate the shipped arrival family with container Python, including alpha, atlas cells and cue timing."""
import json
import subprocess
from pathlib import Path

from validate_live import pixels

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'assets/cutscenes/wildfire'


def main():
    """Fail on bad dimensions, empty art, cropped pose cells or speech outside the seven cue windows."""
    records = []
    for name, size in [('checkpoint_structure', (1920, 1080)), ('checkpoint_lights', (1920, 1080)), ('civilians_structure', (1920, 1080)), ('siege_structure', (1920, 1080)),
                       ('poses', (2048, 512)), ('blade', (512, 512))]:
        width, height, rows = pixels(OUT / f'{name}.png')
        assert (width, height) == size, name
        alpha = [row[x] for row in rows for x in range(3, width * 4, 4)]
        assert min(alpha) == 0 and max(alpha) == 255, name
        if name == 'poses':
            for cell in range(4):
                assert sum(row[x * 4 + 3] > 0 for row in rows for x in range(cell * 512, (cell + 1) * 512)) > 5000
                assert not any(row[x * 4 + 3] for row in rows for x in [cell * 512, (cell + 1) * 512 - 1]), cell
        records.append({'asset': name, 'size': size, 'alpha': 'transparent and opaque pixels verified'})
    for name in ['dialogue', 'atmosphere']:
        duration = float(subprocess.check_output(['ffprobe', '-v', 'error', '-show_entries', 'format=duration',
                        '-of', 'default=nw=1:nk=1', str(OUT / f'{name}.ogg')], text=True))
        assert abs(duration - 60) < .05, (name, duration)
        records.append({'audio': name, 'duration': duration})
    timing = json.loads((OUT / 'dialogue_timing.json').read_text())
    for cue, boundary in zip(timing, [10, 20, 30, 40, 50, 57.5, 60]):
        assert cue['start'] < cue['end'] < boundary, cue
    manifest = json.loads((OUT / 'manifest.json').read_text())
    assert len(manifest['pose_order']) == 4
    report = ROOT / '.validation/wildfire/assets.json'
    report.write_text(json.dumps(records, indent=2) + '\n')
    print('Wildfire assets valid:', json.dumps(records))


if __name__ == '__main__':
    main()
