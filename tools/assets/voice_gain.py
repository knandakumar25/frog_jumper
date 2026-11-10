"""Measure every played voice line and write the per-line playback gain table.

Run inside the cutscene container, which has ffmpeg:

    docker compose --profile cutscene run --rm --entrypoint python3 cutscene tools/assets/voice_gain.py

Voice files are never re-encoded: tests, transcripts and the owner's listening sign-off record
their hashes. Instead each line gets a gain in dB that ``scripts/managers/VoiceGain.cs`` (and
``deployment_cutscene.gd`` for the narration) adds to the player's volume at playback.

Inputs: exactly the recordings the game plays: the ``audio_path`` of every ``speech`` cue in
``assets/audio/english/cues.json`` (mission speech, including the Aegis and Names lines), the
``BRIEFING_LINES`` English AXIOM-7 recordings ``StoryIntro`` voices, and the deployment
narration. Paths follow ``scripts/gameplay/SpeechPaths.cs``. Output:
``assets/audio/voice_gain.json``:

    {"target_lufs": -18.0, "max_gain_db": 12.0, "ceiling_dbtp": -1.0, "limit_budget_db": 2.0,
     "files": {"res://<path>": {"gain_db": g, "loudness_lufs": l, "true_peak_dbtp": p,
                                "expected_limiting_db": r, "sha256": h}}}

Gain rule: ``TARGET_LUFS - loudness``, clamped to +/- ``MAX_GAIN_DB`` and capped so the boosted
true peak exceeds the Voice bus limiter's ceiling (``CEILING_DBTP``, set in
``default_bus_layout.tres``) by at most ``LIMIT_BUDGET_DB``. Without a cap the narration
(+6.8 dB onto -3.6 dBTP peaks) would be limited by about 4 dB; with it, lines with high peaks
play somewhat under target and the limiter only trims stray peaks. Capping at the ceiling
itself (no limiter work at all) would leave lines up to 3.6 LU apart, because a few already
peak near full scale. ``expected_limiting_db`` is the peak overshoot the limiter must remove,
before any player offset (mission speech plays 2 dB lower, ``MissionSpeechQueue.BaseVolumeDb``).

``sha256`` lets a test detect a recording that changed after it was measured. If a file is
missing or cannot be measured, the existing table is left unchanged and the script exits 1.
"""
import hashlib
import json
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from audit_audio import measure  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
# The level the mixed cutscene dialogue tracks already use (-18.1 to -18.5 LUFS).
TARGET_LUFS = -18.0
# Largest boost or cut; a line needing more than this has a recording problem to fix instead.
# Same value as VoiceGain.MaxGainDb (C#) and the clamp in deployment_cutscene.gd.
MAX_GAIN_DB = 12.0
# Ceiling of the Voice bus hard limiter (default_bus_layout.tres; checked by test_voice_bus.gd).
CEILING_DBTP = -1.0
# Most peak overshoot the limiter is asked to remove on any line.
LIMIT_BUDGET_DB = 2.0
# Must equal SpeechPaths.BriefingLineCount; VoiceGainTests fails if they differ.
BRIEFING_LINES = 4
NARRATION = 'assets/cutscenes/deployment/narration.ogg'
OUT = ROOT / 'assets/audio/voice_gain.json'


def voice_files():
    """Return repository-relative paths of every voice recording the game plays."""
    cues = json.loads((ROOT / 'assets/audio/english/cues.json').read_text(encoding='utf-8'))
    # Only "speech" cues are queued (MissionSpeechQueue.Enqueue); non-spoken cues such as
    # sector titles have no recording.
    paths = sorted({cue['audio_path'] for cue in cues if cue.get('classification') == 'speech'})
    # StoryIntro voices briefing lines 1..BRIEFING_LINES, in English in every locale.
    briefing = [f'assets/speech/AXIOM-7/english/AXIOM-7-{n}.mp3' for n in range(1, BRIEFING_LINES + 1)]
    return paths + briefing + [NARRATION]


def plan(loudness, true_peak):
    """Return (gain dB, expected limiter reduction dB) for one recording; see the module docstring."""
    gain = max(-MAX_GAIN_DB, min(MAX_GAIN_DB, TARGET_LUFS - loudness))
    gain = min(gain, CEILING_DBTP + LIMIT_BUDGET_DB - true_peak)
    return round(gain, 2), round(max(0.0, true_peak + gain - CEILING_DBTP), 2)


def main():
    """Measure each voice file and write the gain table; exit 1 on any missing or unmeasurable file."""
    files, problems = {}, []
    for rel in voice_files():
        path = ROOT / rel
        if not path.is_file():
            problems.append(f'missing {rel}')
            continue
        figures = measure(path)
        loudness, peak = figures.get('loudness_lufs'), figures.get('true_peak_dbtp')
        # A silent file has no finite loudness or peak, and JSON cannot hold -inf.
        if not isinstance(loudness, float) or not isinstance(peak, float) or not math.isfinite(peak):
            reason = figures.get('error') or f'loudness {loudness}, true peak {peak}, duration {figures.get("duration")}'
            problems.append(f'cannot measure {rel}: {reason}')
            continue
        gain, limiting = plan(loudness, peak)
        files['res://' + rel] = {'gain_db': gain, 'loudness_lufs': loudness, 'true_peak_dbtp': peak,
                                 'expected_limiting_db': limiting,
                                 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}
    if problems:
        # Keep the existing table rather than write a partial one.
        print('\n'.join(problems))
        sys.exit(1)
    table = {'target_lufs': TARGET_LUFS, 'max_gain_db': MAX_GAIN_DB, 'ceiling_dbtp': CEILING_DBTP,
             'limit_budget_db': LIMIT_BUDGET_DB, 'files': files}
    OUT.write_text(json.dumps(table, indent=1, ensure_ascii=False) + '\n', encoding='utf-8')
    gains = [entry['gain_db'] for entry in files.values()]
    capped = sum(1 for entry in files.values() if entry['gain_db'] < TARGET_LUFS - entry['loudness_lufs'] - 0.01)
    print(f'{len(files)} voice lines -> {OUT.relative_to(ROOT)}; gains {min(gains):+.1f} to {max(gains):+.1f} dB; '
          f'{capped} capped by the limiter budget')


if __name__ == '__main__':
    main()
