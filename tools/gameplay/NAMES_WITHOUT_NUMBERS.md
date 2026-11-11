# Mission 5 production and acceptance

> **Blender retired (2026-11-11).** The Blender render steps, the `render_*.py` scripts and every image they made were deleted. The image sections below are history only. The audio, narration, packaging and validation sections still apply. Run scripts with `docker compose --profile cutscene run --rm cutscene tools/<folder>/<script>.py`; the image now holds only Python and ffmpeg. The validators skip images that have not been regenerated yet. New art comes from SpriteCook: see `docs/ART_REGENERATION.md`.

| Sector | World X | Required route | Target |
| --- | --- | --- | --- |
| Service | 0–4300 | Scanner timing, service defenders, Mirage phase at 3950 | 3–4 min |
| Processing | 4300–8800 | Three formations, Aura control at 8200 | 4–5 min |
| Utilities | 8800–12800 | Three formations, Oceanus coolant 10900, Titan wall 12100 | 4–5 min |
| Originals | 12800–19800 | Two formations per terminal: 13900/400, 16350/200, 18300/500 | 4–5 min |
| Core | 19800–24500 | Isolate 20300, three sequential finite recovery formations | 6–7 min |
| Restored district | 24500–28000 | Commit 25000, accepted access, extract 27700 | 2–3 min |

The 25–30-minute target is unvalidated. No minimum timer or endless spawn pads play.
The assisted route uses real input and diagnostic enemy clearance; it cannot measure
combat balance. A normal-input service approach was attempted during this tuning pass,
but the Godot remote evaluator stalled the game loop before combat, so it is not a
baseline. Checkpoint state is scene-local; Replay starts fresh. Scanner timing remains
1.5s amber warning, 1.2s white pulse, 3.3s safe dark. Mandatory stairs are 100px, below
the existing 177px weakest jump estimate. Optional testimony is at (2650,400),
(10700,400), (17600,300). Terminals use attack context input across devices.

## Combat tuning — 2026-11-02

Kept all 17 three-member formations, enemy health and composition, six-enemy ceiling,
objective gates, checkpoints and core sequence. Adjusted route formation centers to
separate most neighboring activation ranges: service `2500/3800`, processing
`5000/6500/8000`, utilities `9400/10700/12000`, archives `13300/14600`,
`15700/17000`, `17900/19200`; core remains `21400/22500/23400`. The objective ledger
still prevents later archive groups from activating before their preceding records.
Mission 5 gunners now telegraph for 1.45s and rest for 2.6s after their burst; sentry
beams telegraph for 1.0s. Other missions keep the existing 1.1s/2.1s and 0.8s defaults.

These values address the statically confirmed adjacent-formation overlap and provide
longer response/recovery windows without raising health. The complete unassisted route,
death/recovery count, 25–30-minute target and subjective encounter feel remain to be
validated in a sustained playtest.

Final tuning verification (2026-11-02): background Godot import exited 0; the existing
asset-placer TLS and editor shutdown diagnostics remain. `dotnet build AxiomForce.csproj`
passed with 153 existing warnings and zero errors. `run_tests.ps1` passed Django
104/104, NUnit, and GdUnit4 214/214 (`reports/report_409`, zero errors/failures/orphans).
The full suite includes the Level 5 progress ledger and scene runtime checks. All four
normal Docker services remain up; the disposable Django test helper was removed. No save
or backend code changed. Full-route duration, combat deaths, and checkpoint recoveries
were not measured because the remote play session stalled before the first formation.

Production commands use the existing opt-in Docker media image, never game exports:

```powershell
python tools/gameplay/narrate_names.py
docker compose --profile cutscene run --rm -T --entrypoint python3 cutscene tools/gameplay/package_names.py
docker compose --profile cutscene run --rm -T --entrypoint python3 cutscene tools/gameplay/sound_names.py
docker compose --profile cutscene run --rm cutscene tools/gameplay/validate_names.py
```

This workstation's Docker D: bind mount failed; production used `docker cp` with
`axiom-force-cutscene`. All four normal services remained running. Original 512px
RGBA props are emission meshes; collision is independent. Cached new voice requests
live in `.validation/names/speech`; matching request hashes prevent regeneration.
`assets/audio/names/cues.json` preserves cast and localization keys; speech receipts
record complete Ogg durations and request/audio hashes. Original atmosphere/scanner
synthesis is deterministic. Music is routed through Music at -21dB, spatial cues
through SFX at -16dB and speech through SFX at -2dB. Subjective listening is separate.

`python tools/gameplay/review_names.py` refuses to replace a running game. It walks
mandatory routes using actual movement, Titan jumps, operative signature abilities
and terminal proximity, with diagnostic enemy clearance. It stops only its own game
and preserves the user's edited scene. Evidence: `.validation/names/blockout.json`.

Unit coverage checks exact ledger prerequisites, all records, stage ordering,
extraction, duplicate rejection, retry reward protection and Surya deployment. Runtime
coverage exercises indestructible/proximity terminals, all defense-stage recovery,
pause and final results. Physical hardware, subjective speech/mix listening and an
unassisted uninterrupted duration run remain outstanding. Final check results follow.

## Final verification

Verified 2026-11-02 after final engine edits: background import exit 0 with all
30 new media sidecars; final incremental C# build zero warnings/errors; NUnit 100/100;
GdUnit4 210/210 (`reports/report_406`, zero errors, failures and orphans); isolated
copied-source Django 104/104. The wrapper skips the absent host Django venv. Existing
backend `/app` and media D: mounts remain unreadable; isolated tests used the same
backend image and PostgreSQL network, with their test database removed normally.
The temporary production/test helpers were stopped; all four normal services remain
running. No exports, staging, commits or pushes; editor remains stopped on Level 1.

Runtime evidence under `.validation/names/`: assisted mandatory-route completion,
21 sector/restored/results captures across 1920×1080, 1920×1200 and 2560×1080,
actual keyboard/controller-event/mouse/touch terminal dispatch, repeated opening Skip,
natural completion and missing-art handoff, Mission 5 Replay and direct Level Select.
A temporary campaign stayed at Mission 4 until completion, then saved/resumed directly
into 5; its cleanup preserved the original campaign row hash. Seven transparent art
assets and nineteen complete voice cues pass alpha/margin, cast/hash, decoded duration
and clipping checks. The gallery spawn regression passes for all six archive groups.
Python tool compilation and diff hygiene pass. These checks do not establish an
unassisted uninterrupted 25–30-minute run, physical hardware or subjective listening.

Known diagnostics: a full rebuild retains 153 pre-existing nullable/addon warnings;
one concurrent build had a transient PDB-sharing retry, resolved by the final clean
incremental build. Headless editor imports still log existing addon TLS and shutdown
EditorSettings/ObjectDB/resource/allocator messages. The reference checker retains
10 pre-existing addon/menu connection errors and 27 warnings, none in Mission 5.

Current campaign entry checks: `python tools/gameplay/navigate_names.py`.
Visual isolation: `python tools/gameplay/inspect_names.py`. Both preserve the edited
scene and refuse to replace an existing running game. The navigation check creates
only its own temporary campaign fixture and removes it after checking saved progress.
