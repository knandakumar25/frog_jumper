"""Measure loudness, peaks, clipping and silence for every project audio file.

Run inside the cutscene container, which has ffmpeg:

    docker compose --profile cutscene run --rm --entrypoint python3 cutscene \
        tools/assets/audit_audio.py --out .validation/asset_quality/audio.json

Inputs: WAV/OGG/MP3 files under ``assets/``. Nothing is modified. Per file it records
duration, integrated loudness (LUFS, EBU R128), true peak (dBTP), sample peak (dBFS),
whether the sample peak reaches full scale (``clipped``), and leading/trailing silence
below -50 dBFS. Files shorter than 0.4 s get no integrated loudness (R128 needs 400 ms
blocks). A file ffmpeg cannot decode is listed with an ``error`` field and no figures,
and the script exits 1 after writing the report.
"""
import sys
import argparse
import json
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SILENCE_DB = -50


def ffmpeg_stats(path):
    """Run one ffmpeg pass with ebur128, astats and silencedetect.

    Returns ``(exit code, stderr text)``; ffmpeg writes all its measurements to stderr.
    """
    command = ['ffmpeg', '-hide_banner', '-nostats', '-i', str(path), '-af',
               f'ebur128=peak=true,astats=metadata=0:measure_overall=Peak_level+Number_of_samples,'
               f'silencedetect=noise={SILENCE_DB}dB:d=0.05', '-f', 'null', '-']
    result = subprocess.run(command, capture_output=True, text=True, errors='replace')
    return result.returncode, result.stderr


def number(pattern, text, default=None):
    """Return the last float captured by ``pattern`` in ``text``, or ``default``."""
    found = re.findall(pattern, text)
    try:
        return float(found[-1]) if found else default
    except ValueError:
        return default


def measure(path):
    """Collect loudness, peak, clipping and edge-silence figures for one file.

    Returns ``{'error': ...}`` instead when ffmpeg fails, so a broken file is reported
    rather than recorded as a row of empty measurements.
    """
    code, text = ffmpeg_stats(path)
    if code != 0:
        return {'error': text.strip().splitlines()[-1] if text.strip() else f'ffmpeg exit {code}'}
    duration = None
    match = re.search(r'Duration: (\d+):(\d+):([\d.]+)', text)
    if match:
        h, m, s = match.groups()
        duration = int(h) * 3600 + int(m) * 60 + float(s)
    summary = text.split('Summary:')[-1] if 'Summary:' in text else ''
    loudness = number(r'I:\s+(-?[\d.]+) LUFS', summary)
    true_peak = number(r'Peak:\s+(-?[\d.]+|-inf) dBFS', summary)
    starts = [float(x) for x in re.findall(r'silence_start: (-?[\d.]+)', text)]
    ends = [float(x) for x in re.findall(r'silence_end: ([\d.]+)', text)]
    lead = ends[0] if starts and abs(starts[0]) < 0.01 and ends else 0.0
    # Trailing silence: the last silent run either has no end, or (as the container's ffmpeg
    # reports it) "ends" at the end of the stream. Both mean the file finishes silent.
    tail = 0.0
    if duration and starts and starts[-1] > 0.01:
        if len(starts) > len(ends) or (ends and abs(ends[-1] - duration) < 0.05):
            tail = round(duration - starts[-1], 3)
    peak_level = number(r'Peak level dB:\s+(-?[\d.]+|-inf)', text)
    return {
        'duration': round(duration, 3) if duration else None,
        'loudness_lufs': loudness if duration and duration >= 0.4 else None,
        'true_peak_dbtp': true_peak,
        'peak_dbfs': peak_level,
        # A sample peak within 0.01 dB of full scale marks likely clipping.
        'clipped': peak_level is not None and peak_level >= -0.01,
        'lead_silence': round(lead, 3),
        'tail_silence': tail,
    }


def main():
    """Measure every audio file and write the JSON report named by ``--out``."""
    parser = argparse.ArgumentParser()
    parser.add_argument('--out', type=Path, required=True)
    args = parser.parse_args()
    files = sorted(p for p in (ROOT / 'assets').rglob('*') if p.suffix.lower() in ('.wav', '.ogg', '.mp3'))
    report = []
    for path in files:
        entry = {'path': path.relative_to(ROOT).as_posix(), 'bytes': path.stat().st_size}
        entry.update(measure(path))
        report.append(entry)
    out = args.out if args.out.is_absolute() else ROOT / args.out
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(report, indent=1), encoding='utf-8')
    failed = [entry['path'] for entry in report if 'error' in entry]
    print(f'audited {len(report)} audio files -> {out}; {len(failed)} could not be decoded {failed}')
    sys.exit(1 if failed else 0)


if __name__ == '__main__':
    main()
