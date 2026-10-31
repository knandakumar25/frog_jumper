# Live deployment cinematic

## Tokyo Remembers: Level 1 extraction

`CodeLevel` snapshots score, peak combo and operative at extraction and calls
`CampaignProgress.CompleteMission` once. It freezes all mission descendants except
the scorecard, then adds the Always-processing `TokyoRemembers` CanvasLayer. The
level stays loaded; Continue prepares mission 2, Replay reloads mission 1. Level 2
uses the separate Through the Breach aftermath described below. Level 3 shows
results directly with Continue to Level 4; Level 4 retains its final results. No persistent watched flag is stored.
The scorecard's original three-argument `ShowScorecard` binding is retained for
GDScript callers; `ShowMissionScorecard` adds the explicit next-mission target.

`TokyoStage` draws separate Blender scenery/pose layers, analytic petals, lanterns,
dust and limited debris. `Seek(seconds)` deterministically samples all six shots
and subtitles. `SeekAudio(seconds)` also restarts audio at that position for review.
Set `SpeechPath` empty to test timed subtitles, or `ArtPath` to a missing directory
to test immediate guarded results. Skip, natural completion and missing art stop
all owned audio before emitting `Finished` once. Keyboard, controller, pointer and
direct touch share that route. The scorecard handles pointer input before Global's
legacy mouse suppression and restores the previous cursor policy on exit.

```powershell
docker compose --profile cutscene run --rm --entrypoint blender cutscene --background --python tools/cutscene/render_tokyo.py -- --preview
# Inspect .validation/tokyo/dojo_artwork.png before producing the complete family.
docker compose --profile cutscene run --rm --entrypoint blender cutscene --background --python tools/cutscene/render_tokyo.py
docker compose --profile cutscene run --rm --entrypoint python3 cutscene tools/cutscene/package_tokyo.py
docker compose --profile cutscene run --rm --entrypoint python3 cutscene tools/cutscene/validate_tokyo.py
python tools/cutscene/review_tokyo.py
```

Speech packaging reads six cached ElevenLabs MCP cues from
`.validation/tokyo/speech/tts_*.mp3`; it makes no API requests. Daniel voices AXIOM-7
and Adam voices Kaito, with stability .72/style .08/speed 1.04. The shipped
`dialogue_timing.json` records lengths and fitting; every cue ends inside its shot.
The separate seeded atmosphere track adds quiet wind, debris, cloth, sheath and
transmission sounds. Both tracks last 40 seconds. Art sizes, pivots, palette and
provenance are in `assets/cutscenes/tokyo/manifest.json`. `review_tokyo.py` starts a
separate game via Godot AI MCP, completes real extraction prerequisites, and writes
shots/layout diagnostics under `.validation/tokyo/review`, preserving the open editor
scene. It does not export or publish the game.

Verified 2026-10-30: asset validation and background import completed; C# build had
zero errors (153 existing warnings). `run_tests.ps1` passed NUnit 34/34 and GdUnit4
114/114 with zero errors/failures/orphans (`reports/report_346`). The host Django
venv was absent, so `docker compose exec -T backend python manage.py test accounts`
provided the backend verification: 104/104 passed. MCP evidence records all shots,
three aspect ratios, extraction snapshots, Replay, natural completion from the
audio track's ending, Continue/solo squad in Level 2, Level Select and Main Menu.
The test game was stopped and the editor's Level 1 scene preserved. Existing
PostgreSQL fallback, addon TLS and headless editor shutdown diagnostics remain.
An additional uninterrupted real-time playback verified audio/visual alignment,
natural completion at timeline 40, and constant health/score; see
`.validation/tokyo/full-playback.json`. Physical touch/controller hardware and
exports were not verified.

## Opening deployment

The game uses `deployment_cutscene.tscn`, with editable scenery, Camera2D,
Shogun/afterimage sprites, transmission artwork and a separate responsive UI.
`deployment_cutscene.gd` owns the 40-second compensated narration clock, original
subtitle cues, fallback and guarded transition. `deployment_stage.gd` samples
all animation analytically, so seeking never accumulates timers or particles.

```powershell
docker compose --profile cutscene run --rm cutscene --reveal-only
docker compose --profile cutscene run --rm cutscene --live
# Revalidate without re-rendering:
docker compose --profile cutscene run --rm --entrypoint python3 cutscene tools/cutscene/validate_live.py
```

Build the tool image first if absent (`docker compose --profile cutscene build cutscene`).
It is opt-in, does not restart services, and performs no game export.
Blender authors original planar city/rooftop/drone layers and an outlined red/gold
Shogun atlas based on `tools/art/draw_flat_characters.py`. The first command renders
`.validation/deployment/live/reveal_character.png` for visual review before the family.
The second writes `assets/cutscenes/deployment/live/manifest.json` with dimensions,
provenance, atlas rows and pivot. Python validates PNG RGBA, alpha, and atlas regions.
Scenery is 1920x1080; the atlas is 4096x2560 (8x5 512px cells). Filtering is linear.

Run the scene through Godot AI MCP `project_run(mode="custom", scene=..., autosave=false)`
without switching the open editor scene. `mcp_probe.py` uses standard-library Python
and the configured local private capability, pinning MCP 2025-11-25; it prints no secrets.
For captures use `editor_screenshot(source="game")`; for diagnostics read both editor
and game logs. Runtime QA may disable processing and call `seek_visuals(seconds)`.
`transition_to_briefing=false` prevents replacement during tests; an empty
`narration_path` exercises the fallback. Production defaults preserve the original flow.

The original narration stays unchanged and is loaded separately. No new paid API
request is made. Its existing regeneration script `narrate.py` caches the four
ElevenLabs cues and uses Docker FFmpeg for radio EQ/mixing. Existing subtitle windows
remain 1-9.5, 11-19.5, 21-31 and 33-39 seconds, with English fallback translations.

## Legacy video preview

`deployment.ogv` remains available. Run the tool with no arguments to rebuild its
four-plate video, or `--package-only` to reuse plates in `.validation/deployment`.
The legacy renderer uses the same portable CPU emission rendering as live assets;
its original Eevee identifier differed across Blender releases. This video is not referenced by normal cutscene playback.

## Concrete Wildfire: Level 2 arrival

`ConcreteWildfireIntro` and `WildfireStage` provide the independent 60-second arrival.
The mission captures processing/HUD/audio pause state before adding the overlay and
restores it once on completion, Skip or missing-art fallback. The opening dialogue
then receives its full timer. No save format, watched flag, unlock or objective changes.
Checkpoint recovery does not reconstruct the level. Exploration maps remain deferred.
`Seek`, `SeekAudio`, `Finish`, `Finished`, `SpeechPath` and `ArtPath` mirror the epilogue
review interface. `WILDFIRE_CUE_1` through `_7` and `WILDFIRE_SKIP` use English fallback.

```powershell
docker compose --profile cutscene run --rm --entrypoint blender cutscene --background --python tools/cutscene/render_wildfire.py -- --preview
# Inspect .validation/wildfire/checkpoint_artwork.png before the complete family.
docker compose --profile cutscene run --rm --entrypoint blender cutscene --background --python tools/cutscene/render_wildfire.py
docker compose --profile cutscene run --rm --entrypoint python3 cutscene tools/cutscene/package_wildfire.py
docker compose --profile cutscene run --rm --entrypoint python3 cutscene tools/cutscene/validate_wildfire.py
```

Packaging reads seven cached ElevenLabs MCP result records (`cue-1.json` through
`cue-7.json`) and their MP3 files in `.validation/wildfire/speech`; no API calls occur
in packaging. Daniel voices cues 1/3/4/6, Adam voices 2/5/7. Deliberate pauses separate
shots; `dialogue_timing.json` records source length, tempo, start/end and provenance.
Quiet seeded wind/fire, distant impacts, cloth, footsteps and radio effects occupy
the separate atmosphere track. Manifest records native RGBA sizes and pose pivots.

Run `python tools/cutscene/review_wildfire.py` for deterministic six-shot/layout
captures and `python tools/cutscene/verify_wildfire_routes.py` for real menu routes
and uninterrupted playback. Both preserve the open editor scene and stop their
separate review game. The route tool creates and removes one temporary save.
Verified 2026-10-30: all assets valid, background import exit 0, build zero errors
(153 existing warnings), NUnit 34/34, Godot 120/120 with zero failures/errors/orphans
(report_349), container Django 104/104. MCP verified three aspect ratios, real
Continue/Level Select/saved-session/direct launches, constant position/health/score
through full playback and natural handoff. Additional synthetic Space review verified
Jump released and mission music resumed. Existing addon/database diagnostics remain;
physical controller/touch hardware, speaker listening and exports were not tested.

## Level 2 aftermath: Through the Breach

`assets/cutscenes/wildfire_aftermath/cues.json` is the exact eight-line English
production script with speaker/voice IDs and windows 0–8, 8–17, 17–26, 26–34,
34–43, 43–54, 54–64, 64–72 seconds. 72–75 is the unvoiced Level 3 title.
ElevenLabs MCP generated each cue with `eleven_multilingual_v2`, stability .55,
speed 1.05. Cache each response as `.validation/breach/speech/cue-N.json` and
its MP3 in that directory. Packaging never invokes a paid API. It fits complete
cues with atempo (no cutting), normalizes to -18 LUFS/-2 dB true peak and emits
actual decoded cue ends in `dialogue_timing.json`. The recovered manifest cue
needs only approximately 1.033x fitting; all other cues remain at original speed.

Reproduce from the project root, leaving the normal services running:

```powershell
docker compose run --rm --entrypoint blender cutscene --background --python tools/cutscene/render_breach.py
docker compose run --rm --entrypoint python3 cutscene tools/cutscene/package_breach.py
docker compose run --rm --entrypoint python3 cutscene tools/cutscene/validate_breach.py
python tools/cutscene/review_breach.py
```

Provenance: original project planar Blender geometry, using the existing character
library and checkpoint/city layers; no external stock art. Navy/cyan infrastructure,
amber embers, red/gold Kaito, hydro Oceanus and violet Titan. `manifest.json` records
3072×1536 atlas dimensions (512 cells), row/column order and shared resources.
The new scenery is transparent 1920×1080. Atmosphere uses seeded synthesized
wind, debris, radio textures and measured footsteps; no sampled third-party audio.
The runtime stage animates camera, parallax, smoke, water dissipation, walking,
child reunion and manifest routing from one seekable clock.

QA artifacts belong under `.validation/breach/`. Full physical controller/touch
and speaker listening remain separate from synthetic input/audio-file checks.

Final verification (2026-10-31): background import exit 0 and all nine new import
files present; C# build 0 errors (154 existing warnings on recompilation, final
incremental build 0 warnings). `run_tests.ps1` passes NUnit 47/47 and GdUnit4
142/142 with zero failures/errors/orphans (`report_361`). Host Django is skipped
because its venv is absent; explicit container Django passes 104/104. Docker
asset/audio validation and Python compilation pass. Both tracks are 75 seconds;
all eight complete cues fit their windows and decoded peaks remain below clipping.

Godot AI MCP captured and visually reviewed all nine shots at 1920×1080,
1920×1200 and 2560×1080. Uninterrupted 75-second playback kept player position,
health and score fixed; maximum sampled audio/visual clock separation was under
30 ms. Natural results, Continue to Level 3 with three earned operatives, Replay
with Level 2 arrival, Level Select and Main Menu all pass. Runtime review logs
contain no errors or warnings. Level 1 regression passes in the Godot suite.
Evidence: `.validation/breach/`, including 27 captures and aspect contact sheets.

Existing import diagnostics remain separate: asset-placer TLS failures, editor
shutdown settings/resource/ObjectDB/PagedAllocator messages, and optional
`gdunit4.api` warning. No physical controller/touch hardware, speaker-listening
review or export was performed. Review game stopped; the user's editor remains
on Level 1, all four services running. Changes are local/uncommitted; unrelated
PostgreSQL/website work is preserved.

## Silent Freight (Level 3 arrival)

The sequence runs after Level 2 results Continue has loaded Level 3, and on all
other fresh Level 3 launches. Its nine shots span 0–9–18–27–36–45–55–64–72–75s.
The final three seconds hold the mission title without speech.

Reproduce the original transparent 1920×1080 depot, rail underpass, cargo and
walkway layers and 1536×1536 missing-pose atlas (512 cells):

```powershell
docker compose run --rm --entrypoint blender cutscene --background --python tools/cutscene/render_neon_rust.py
docker compose run --rm --entrypoint python3 cutscene tools/cutscene/package_neon_rust.py
docker compose run --rm --entrypoint python3 cutscene tools/cutscene/validate_neon_rust.py
python tools/cutscene/review_neon_rust.py
```

Packaging reads eight cached ElevenLabs responses and MP3 files from
`.validation/neon-rust/speech/`; it never calls the paid API. Generate cues from
`assets/cutscenes/neon_rust_intro/cues.json` using the recorded voice IDs and
`eleven_multilingual_v2`, stability .65, similarity .75, style .15, speed 1.
Retain each MCP response as cue-1.json through cue-8.json. Normalization fits
complete words within each window; dialogue and seeded industrial atmosphere
are separate 75-second Ogg tracks. The cast remains Daniel/AXIOM-7, Adam/Kaito,
Sarah/Oceanus and George/Titan. `manifest.json`, `localization.json` and
`dialogue_timing.json` record provenance, speaker-labelled English fallbacks
and measured cue bounds. Existing project city and six-column operative walks
are reused. No third-party stock imagery or sampled audio is added.

Review uses Godot AI MCP, requires the correct project and a stopped game,
launches a separate Level 3 process without autosaving, and captures nine shots
at 16:9, 16:10 and ultrawide under `.validation/neon-rust/review/`.

The review also tests Continue, Level Select and Replay. Saved-session loading
uses a newly created owned PostgreSQL campaign at Level 3, then deletes that
one test save and verifies it is gone; existing campaigns are never selected.
Contact sheets use Docker FFmpeg with no Pillow dependency on the host.

Final verification (2026-10-31): background Godot import exited 0 and all eleven
PNG/Ogg import files exist. C# build has zero errors (154 pre-existing addon
warnings). Final run_tests.ps1 passes NUnit 47/47 and Godot 151/151, zero
failures/errors/orphans (report_363). The host wrapper skips Django because
backend/.venv is absent; the explicit container suite passes 104/104. Docker
raster/audio QA and Python compilation pass: 1920×1080 transparent scenery,
512-cell atlas bounds, eight complete nonoverlapping cues, two exact 75-second
tracks and decoded peaks below clipping. Diff hygiene passes.

Existing import diagnostics: asset-placer TLS failures and editor shutdown
EditorSettings/ObjectDB/resource/PagedAllocator messages; runtime review logs
are clean. No physical controller/touch hardware, speaker-listening review or
export was performed. Evidence: .validation/neon-rust/ (including 27 captures
and three contact sheets). All four normal Docker services remain running.
User's editor remains on Level 1; review game stopped; changes are local and
uncommitted. No watched flag, save migration or local persistence was added.

Level 3 completion update (2026-10-31): Silent Freight remains the unchanged
arrival. Expanded Neon Rust gameplay now goes directly to a scorecard with
Continue to Level 4, Replay, Level Select and Main Menu; it has no ending film.
