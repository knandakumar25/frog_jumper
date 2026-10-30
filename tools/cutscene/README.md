# Live deployment cinematic

## Tokyo Remembers: Level 1 extraction

`CodeLevel` snapshots score, peak combo and operative at extraction and calls
`CampaignProgress.CompleteMission` once. It freezes all mission descendants except
the scorecard, then adds the Always-processing `TokyoRemembers` CanvasLayer. The
level stays loaded; Continue prepares mission 2, Replay reloads mission 1. Missions
2–4 retain their previous completion flow. No persistent watched flag is stored.
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
