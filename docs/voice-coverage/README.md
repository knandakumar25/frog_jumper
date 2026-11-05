# English voice coverage

Automated evidence and listening acceptance are separate. See coverage.json for every cue and discrepancy.

| Mission | Speech cues | Missing audio | Transcribed | Decoded |
| --- | ---: | ---: | ---: | ---: |
| 0 | 8 | 0 | 8 | 8 |
| 1 | 15 | 0 | 15 | 15 |
| 2 | 24 | 0 | 24 | 24 |
| 3 | 27 | 0 | 27 | 27 |
| 4 | 30 | 0 | 30 | 30 |
| 5 | 41 | 0 | 41 | 41 |
| 6 | 22 | 0 | 22 | 22 |
| 7 | 22 | 0 | 22 | 22 |
| 8 | 22 | 0 | 22 | 22 |
| 9 | 21 | 0 | 21 | 21 |
| 10 | 21 | 0 | 21 | 21 |
| 11 | 21 | 0 | 21 | 21 |
| 12 | 22 | 0 | 22 | 22 |

| Speaker | Cues |
| --- | ---: |
| AURA | 25 |
| AXIOM-7 | 105 |
| CIVILIAN AMARA | 1 |
| CIVILIAN TESTIMONY | 3 |
| CIVILIANS | 1 |
| KAITO | 41 |
| MIRAGE | 33 |
| OCEANUS | 30 |
| SURYA | 25 |
| TITAN | 27 |
| VOID COMMANDER | 1 |
| VOSS | 4 |

## Outstanding evidence

- transcript discrepancy requires review: 7 cues

## Reproduce

Host: `python tools/gameplay/english_voice_coverage.py` (inventory); add `--generate` only to produce missing recordings.
Host: `python tools/gameplay/audit_english_voice.py --transcribe` (cached paid Scribe audit; no retries).
Media: `docker compose --profile cutscene run --rm -T --entrypoint python3 cutscene tools/gameplay/audit_english_voice.py --media`.
Host: `python tools/gameplay/audit_english_voice.py --check` exits nonzero while required evidence remains incomplete.

For Docker Desktop drive-mount failures: `python tools/gameplay/audit_english_voice.py --docker-media` streams only nonsecret media/source files into the existing media image.
Independent ASR: `uv --system-certs run --python 3.12 --with faster-whisper --with av==15.1.0 tools/gameplay/review_voice_discrepancies.py`.
Natural production playback: run Godot headless with `-s tools/gameplay/review_english_speech.gd`; evidence is saved in `playback.json`.
