"""Validate the Pre-Level 6 Cyber-Grave Intro audio package and cue timings."""
import json
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'assets/cutscenes/cyber_grave_intro'
CACHE = ROOT / '.validation/cyber-grave/speech'


def probe_duration(path: Path) -> float:
    """Read stream duration using ffprobe."""
    return float(subprocess.check_output(
        ['ffprobe', '-v', 'error', '-show_entries', 'format=duration', '-of', 'default=nw=1:nk=1', str(path)],
        text=True))


def main():
    """Verify that dialogue.ogg and atmosphere.ogg are present and match 120s."""
    cues_path = OUT / 'cues.json'
    if not cues_path.exists():
        raise SystemExit("Missing cues.json")
    cues = json.loads(cues_path.read_text(encoding='utf-8'))
    assert len(cues) == 9, f"Expected 9 cues, found {len(cues)}"

    dialogue_path = OUT / 'dialogue.ogg'
    atmosphere_path = OUT / 'atmosphere.ogg'

    assert dialogue_path.exists(), "Missing dialogue.ogg"
    assert atmosphere_path.exists(), "Missing atmosphere.ogg"

    dialogue_dur = probe_duration(dialogue_path)
    atmosphere_dur = probe_duration(atmosphere_path)

    print(f"dialogue.ogg duration: {dialogue_dur:.2f}s")
    print(f"atmosphere.ogg duration: {atmosphere_dur:.2f}s")

    assert abs(dialogue_dur - 120.0) < 0.5, f"dialogue.ogg duration mismatch: {dialogue_dur}s"
    assert abs(atmosphere_dur - 120.0) < 0.5, f"atmosphere.ogg duration mismatch: {atmosphere_dur}s"

    print("Pre-Level 6 Cyber-Grave Intro audio verification passed!")


if __name__ == '__main__':
    main()
