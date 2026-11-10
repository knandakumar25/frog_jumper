"""Measure every mission music track and write the playback gain table.

Run inside the cutscene container, which has ffmpeg:

    docker compose --profile cutscene run --rm --entrypoint python3 cutscene tools/assets/music_gain.py

Mission music used to play at +24 dB on Master (``CodeLevel.BuildAudioAndPause``): about 18 LU
over speech for the atmosphere tracks and about 33 LU for the default track, which was driven
far past full scale. ``scripts/managers/MusicGain.cs`` now plays each track on the Music bus at
the gain this script computes, so every mission's music sits at ``MUSIC_LUFS``, about 9 LU
under mission speech (which plays at about -20 LUFS; see ``voice_gain.py``). The cutscenes
already keep their atmosphere about 14 LU under dialogue; gameplay music is a little more present.

Inputs: every ``MusicPath => "res://..."`` in ``scripts/gameplay/*.cs`` (the ``CodeLevel``
default plus each mission's override). Output: ``assets/audio/music_gain.json`` with the same
shape as the voice table: ``{"target_lufs", "max_gain_db", "ceiling_dbtp", "files":
{"res://<path>": {"gain_db", "loudness_lufs", "true_peak_dbtp", "sha256"}}}``.

Gain rule: ``MUSIC_LUFS - loudness``, clamped to +/- ``MAX_GAIN_DB`` and capped so the true
peak stays at or below ``CEILING_DBTP`` (the Music bus has no limiter). Files are never
re-encoded. If a file is missing or cannot be measured, the existing table is left unchanged
and the script exits 1.
"""
import hashlib
import json
import math
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from audit_audio import measure  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
# About 9 LU under mission speech (-20 LUFS after MissionSpeechQueue's -2 dB offset).
MUSIC_LUFS = -29.0
# Largest boost or cut; same value as MusicGain.MaxGainDb (C#).
MAX_GAIN_DB = 30.0
# Highest true peak allowed after the gain, since the Music bus has no limiter.
CEILING_DBTP = -1.0
OUT = ROOT / 'assets/audio/music_gain.json'
# Matches the expression-bodied MusicPath properties in CodeLevel and its subclasses.
MUSIC_PATH = re.compile(r'MusicPath\s*=>\s*"res://([^"]+)"')


def music_files():
    """Return the sorted repository-relative paths of every mission music track."""
    found = set()
    for source in (ROOT / 'scripts/gameplay').glob('*.cs'):
        found.update(MUSIC_PATH.findall(source.read_text(encoding='utf-8')))
    return sorted(found)


def plan(loudness, true_peak):
    """Return the gain in dB for one track; see the module docstring."""
    gain = max(-MAX_GAIN_DB, min(MAX_GAIN_DB, MUSIC_LUFS - loudness))
    return round(min(gain, CEILING_DBTP - true_peak), 2)


def main():
    """Measure each music track and write the gain table; exit 1 on any missing or unmeasurable file."""
    files, problems = {}, []
    for rel in music_files():
        path = ROOT / rel
        if not path.is_file():
            problems.append(f'missing {rel}')
            continue
        figures = measure(path)
        loudness, peak = figures.get('loudness_lufs'), figures.get('true_peak_dbtp')
        if not isinstance(loudness, float) or not isinstance(peak, float) or not math.isfinite(peak):
            problems.append(f'cannot measure {rel}: {figures.get("error") or (loudness, peak)}')
            continue
        files['res://' + rel] = {'gain_db': plan(loudness, peak), 'loudness_lufs': loudness,
                                 'true_peak_dbtp': peak,
                                 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}
    if problems or not files:
        # Keep the existing table rather than write a partial or empty one.
        print('\n'.join(problems) or 'no MusicPath found in scripts/gameplay')
        sys.exit(1)
    table = {'target_lufs': MUSIC_LUFS, 'max_gain_db': MAX_GAIN_DB, 'ceiling_dbtp': CEILING_DBTP, 'files': files}
    OUT.write_text(json.dumps(table, indent=1, ensure_ascii=False) + '\n', encoding='utf-8')
    for res, entry in files.items():
        print(f'{entry["gain_db"]:+6.1f} dB  {res}')


if __name__ == '__main__':
    main()
