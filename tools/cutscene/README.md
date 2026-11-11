# Live deployment cinematic

> **Blender retired (2026-11-11).** The Blender render steps, `render_*.py` scripts, `build.py`, the `--live`/`--reveal-only` entry points and every image they made were deleted. The image sections below are history only. The audio, narration, packaging and validation sections still apply. Run scripts with `docker compose --profile cutscene run --rm cutscene tools/<folder>/<script>.py`; the image now holds only Python and ffmpeg. The validators skip images that have not been regenerated yet. New art comes from SpriteCook: see `docs/ART_REGENERATION.md`.

## Mission 5 epilogue: Names Without Numbers

`NamesWithoutNumbersAftermath` runs after extraction and before the existing Mission
Complete scorecard. The captured score, combo and operative are immutable during the
presentation; Mission 5 and its descendants stay frozen. `Seek(seconds)` samples the
eight dialogue windows, subtitle and analytic tableau from one 90-second clock.
`SeekAudio(seconds)` repositions both owned tracks. The full sequence remains readable
without speech. Natural completion, Escape/Space/Enter, controller Start/A, pointer or
touch Skip, missing essential art, and independent overlay removal stop audio and
trigger one results handoff. The frozen scene stays available so Replay reloads a clean
Mission 5 run. No unlock or epilogue progression state is written.

Original 1920x1080 transparent district plates come from the existing Blender helpers.
The scene stage draws its inhabitants and five operative reactions analytically, with
Surya represented as a comms signal. The final shot belongs to Kaito and fades into the
unchanged **MISSION COMPLETE** result card. Exact lines, voice IDs and shot windows are
in `assets/cutscenes/names_without_numbers/cues.json`; generation and source hashes are
recorded in `speech_provenance.json` and measured timing in `dialogue_timing.json`.

Production on a host with the Docker D: bind mount available:

```powershell
# Inspect .validation/names-without-numbers/district_artwork.png before full rendering.
python tools/cutscene/narrate_names_without_numbers.py
docker compose --profile cutscene run --rm --entrypoint python3 cutscene tools/cutscene/package_names_without_numbers.py
docker compose --profile cutscene run --rm --entrypoint python3 cutscene tools/cutscene/validate_names_without_numbers.py
```

Speech generation uses the project's existing ElevenLabs cast, verifies all voice IDs,
reuses exact hashed caches and never retries a failed cue automatically. It makes paid
requests only when a matching cache is absent. Packaging is offline and fits complete
lines inside their windows, targets -18 LUFS/-2 dBTP, then creates independent 90-second
dialogue and seeded district atmosphere Ogg tracks. The validator checks transparent
plate dimensions, complete cue timing, decoded track duration and clipping headroom.
Runtime tests are in `tests/test_names_without_numbers_aftermath.gd` and the updated
`tests/test_names_without_numbers.gd`.

## Tokyo Remembers: Level 1 extraction

`CodeLevel` snapshots score, peak combo and operative at extraction and calls
`CampaignProgress.CompleteMission` once. It freezes all mission descendants except
the scorecard, then adds the Always-processing `TokyoRemembers` CanvasLayer. The
level stays loaded; Continue prepares mission 2, Replay reloads mission 1. Level 2
uses the separate Through the Breach aftermath described below. Level 3 plays The City Finds Its Voice before
results with Continue to Level 4; Level 4 retains its final results. No persistent watched flag is stored.
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
# Inspect .validation/tokyo/dojo_artwork.png before producing the complete family.
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
# Revalidate without re-rendering:
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
# Inspect .validation/wildfire/checkpoint_artwork.png before the complete family.
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
Continue to Level 4, Replay, Level Select and Main Menu. The City Finds Its Voice
now precedes those results, as described below.

## The City Finds Its Voice

Level 3 extraction freezes the mission, snapshots results and runs a live
90-second aftermath. Eleven shots start at 0, 9, 18, 27, 36, 45, 54, 64, 74,
82 and 88 seconds. The last two seconds hold NEXT: SHATTERED AEGIS in silence.
The squad has Kaito, Oceanus, Titan, Aura and Mirage; AXIOM-7 is voice-only.
The fortress is distant and independently powered. Surya and Voss remain in Level 4.

Production commands from the project root:

```powershell
docker compose build cutscene
# Inspect .validation/city-voice/squad.png before rendering the family.
python tools/cutscene/narrate_city_voice.py
docker compose run --rm --entrypoint python3 cutscene tools/cutscene/package_city_voice.py
docker compose run --rm --entrypoint python3 cutscene tools/cutscene/validate_city_voice.py
python tools/cutscene/review_city_voice.py
```

Narration calls the paid ElevenLabs API only for absent cues and validates the
cast before generating. It reads the ignored .env key without logging it and
rejects mismatched cache hashes. cues.json owns exact requested text, voice IDs,
model and shot windows. speech_provenance.json records request/audio hashes and
settings. Packaging never calls an API, preserves complete words and fits each
cue with atempo only if necessary (rejecting tempo >= 1.2). It records measured
bounds in dialogue_timing.json. The original seeded wind, fading relay carrier,
service rail clatter, radio and footsteps contain no stock samples. Both Ogg
tracks last 90 seconds and are checked after decoding for clipping. Runtime
owns the timeline and English subtitle fallbacks; validation checks manifest
text/window alignment. Raster QA checks 1920×1080 scenery, alpha and all thirty
512px atlas cells including transparent boundaries.

If Docker Desktop cannot bind D:, use an owned container with copied inputs:

```powershell
docker run -d --name axiom-city-voice-production --entrypoint sleep axiom-force-cutscene infinity
docker exec axiom-city-voice-production mkdir -p /workspace/tools /workspace/assets/cutscenes /workspace/.validation/city-voice /workspace/scripts/ui
docker cp tools/art axiom-city-voice-production:/workspace/tools/art
docker cp tools/cutscene axiom-city-voice-production:/workspace/tools/cutscene
docker cp assets/cutscenes/neon_rust_aftermath axiom-city-voice-production:/workspace/assets/cutscenes/neon_rust_aftermath
docker cp .validation/city-voice/speech axiom-city-voice-production:/workspace/.validation/city-voice/speech
docker cp scripts/ui/NeonRustAftermath.cs axiom-city-voice-production:/workspace/scripts/ui/NeonRustAftermath.cs
# Replace compose run commands above with docker exec axiom-city-voice-production.
# Copy generated assets back before Godot import:
docker cp axiom-city-voice-production:/workspace/assets/cutscenes/neon_rust_aftermath/. assets/cutscenes/neon_rust_aftermath
$env:CITY_VOICE_CONTAINER = 'axiom-city-voice-production'
python tools/cutscene/review_city_voice.py
docker stop axiom-city-voice-production
```

The MCP review preserves the open editor scene, requires a stopped game and no
active save, seeds completed objectives diagnostically, captures all 33 shot/aspect
compositions and runs uninterrupted playback while checking position/health/score,
freeze state and audio-clock agreement. It then tests the four real results
navigation handlers, including five operatives in Level 4 and three plus arrival
on Replay. Evidence stays under .validation/city-voice/review. This does not
measure combat pacing or physical controller/touch hardware.

The City Finds Its Voice verification (2026-11-01): C# build passes; NUnit 93/93,
Godot 170/170 with zero errors/failures/orphans, isolated container Django 104/104.
All 33 shot/aspect views, full 90-second frozen playback, four results destinations,
raster/atlas QA and both complete 90-second audio tracks pass. Background import
exits 0 with five sidecars; existing addon/TLS/shutdown diagnostics remain. Physical
controller/touch and speaker-listening review remain unperformed. Docker Desktop’s
D: bind-mount fault remains unresolved (including backend /app); copied inputs in
owned unmounted containers enabled production and Django verification without
restarting the four running services. User editor stays stopped on Level 1.

## Level 4 arrival: The Wall That Faces Inward

`ShatteredAegisIntro` / `ShatteredAegisStage` use a shared deterministic clock and
thirteen shots at 0, 10, 20, 29, 39, 49, 59, 69, 79, 89, 99, 109 and 117 seconds.
117–120 holds LEVEL 4: SHATTERED AEGIS in silence. Cues, English fallbacks,
voice/settings/hashes, measured lengths and art provenance live under
`assets/cutscenes/shattered_aegis_intro/`. The five squad members use the existing
City Finds Its Voice atlas. Surya uses the original solar palette; Voss uses the
actual `heavy_atlas.png` boss silhouette, without instantiating a gameplay actor.

Reproduce from the project root:

```powershell
# Inspect .validation/inward-wall/approach_artwork.png before rendering the family.
python tools/cutscene/narrate_inward_wall.py
docker compose run --rm --entrypoint python3 cutscene tools/cutscene/package_inward_wall.py
docker compose run --rm --entrypoint python3 cutscene tools/cutscene/validate_inward_wall.py
python tools/cutscene/review_inward_wall.py
```

Narration validates all required cast IDs and only requests absent cues with a
matching request hash. It reads the ignored local API key without printing it.
Packaging makes no paid requests, keeps complete words, normalizes to -18 LUFS /
-2 dBTP and rejects fitting at 1.2x or faster. AXIOM retains radio EQ; Voss receives
260–3400 Hz speaker EQ and restrained 55 ms echo. The atmosphere contains original
seeded wind, machinery, steel, vent and step synthesis, with no sampled audio.

For the existing Docker Desktop D: bind fault, create an owned unmounted container
with the same image, copy tools/art and tools/cutscene under /workspace/tools,
assets/cutscenes/shattered_aegis_intro under /workspace/assets/cutscenes, cached
.validation/inward-wall/speech under /workspace/.validation/inward-wall and the
intro source under /workspace/scripts/ui. Run commands with `docker exec` from
/workspace and copy generated assets back before Godot import. The review tool
uses `INWARD_WALL_CONTAINER`, detects a running `axiom-inward-wall-production`
helper, or falls back to Compose for FFmpeg contact sheets. Keep the four normal services running. Stop only the owned
production helper when done.

Runtime review requires a stopped game, preserves the user's editor scene and
captures all thirteen shots at 1920×1080, 1920×1200 and 2560×1080. It measures a
full uninterrupted run and tests Continue, Level Select, Replay and loading one
owned temporary PostgreSQL save, removing that save afterwards. Diagnostic
navigation isolates cinematic acceptance from combat pacing. Physical controller,
touch hardware and speaker-listening checks must be reported separately.

The residential insert also composites the original deployment city structure and
light layers behind a lowered parapet; no civilian markers are drawn. Aura first
reveals a faint shield signal; Surya's sprite appears on his own following shot.
The first AXIOM cue needs approximately 1.0482x fitting; the remaining eleven retain
original tempo. Source narration speed remains 1 for every cue.

Final verification: C# build has zero errors (154 existing addon warnings);
background import exits 0 with all nine PNG/Ogg sidecars. NUnit 93/93, Godot
180/180 (report_375; zero failures/errors/orphans), and isolated container Django
104/104 pass. All thirteen shots were inspected at 1920×1080, 1920×1200 and
2560×1080. Full playback holds the mission frozen, hands control back naturally,
and stays within 32 ms sampled audio/visual separation. Continue, Level Select,
Replay and loading an owned temporary save activate arrival; the save was removed.

Raster/alpha, Surya atlas boundaries, twelve complete cue windows, runtime text/
manifest agreement, unclipped PCM and both exact 120-second tracks pass. Python
compilation and diff hygiene pass. Runtime review has 95 log entries with zero
warnings/errors; evidence is in .validation/inward-wall/, including 39 captures
and three inspected contact sheets. Unit coverage includes all Skip routes,
missing speech/art, repeated completion, teardown/removal, exact mixed snapshot
restoration, full briefing lifetime, all-shot state invariants, recruitment after
handoff and recovery without replay. Existing arena tests still verify Voss and
the wall after arrival; earlier cinematics/results regress in the full suite.

Existing import TLS/editor shutdown diagnostics and the reference checker's ten
unchanged addon/main-menu signal-path errors (27 warnings) remain separate from
runtime/test results. Host Django is skipped for the absent venv; the backend
bind mount still fails with ENODEV, so current backend source/environment were
copied into an owned unmounted container for the explicit 104-test pass. The
production helper also used copied inputs. Both owned helpers are now stopped;
the four normal services remain running. Editor remains stopped on Level 1.
Physical controller/touch hardware and speaker-listening checks remain unperformed.
No export, staging, commit or push; changes remain local and uncommitted.

## Act II — The Unregistered

The 120-second campaign opening is reached through Level 4’s Act I results
**CONTINUE TO ACT II** action. Natural completion, Skip and missing essential artwork
stop owned audio and enter Mission 5 once. Level Select, Resume and Mission 5 Replay
enter gameplay directly. The pre-Mission-5 preview-isolation checks in
`review_unregistered.py` are historical; use `tools/gameplay/navigate_names.py` for
current campaign handoff acceptance. Shots begin
at 0, 8, 20, 32, 44, 56, 68, 80, 92, 104 and 114; the 104–114 shot switches from Surya
to Kaito at 110. Titles and objective are silent; text remains in Godot. Established
five-member poses plus the existing Surya atlas preserve character designs. Existing
voice IDs/settings come from the fortress arrival; only ten new dialogue requests
are made. Provenance and measured fit bounds accompany cues.json.

```powershell
docker compose build cutscene
# Inspect .validation/unregistered/queue_artwork.png before rendering the family.
python tools/cutscene/narrate_unregistered.py
docker compose run --rm --entrypoint python3 cutscene tools/cutscene/package_unregistered.py
docker compose run --rm --entrypoint python3 cutscene tools/cutscene/validate_unregistered.py
python tools/cutscene/review_unregistered.py
```

If the documented Docker Desktop D: mount fault persists, use an owned unmounted
axiom-force-cutscene container and copy tools/art, tools/cutscene, assets/cutscenes/
act_ii_opening, assets/cutscenes/shattered_aegis_intro/cues.json, scripts/ui/
ActIIOpening.cs and .validation/unregistered/speech into their /workspace paths.
Run Blender and packaging with docker exec, then copy the generated assets and
.validation/unregistered evidence back. Stop only the owned helper. No exports.

Narration uses ignored local credentials without logging them, verifies cast access,
and refuses cache request mismatches. Packaging normalizes complete cues to -18 LUFS,
fits below 1.2x, applies the established AXIOM EQ and synthesizes seeded ventilation,
scanner pulses and steps. Runtime atmosphere gain is -18 dB relative to speech; both
120-second tracks are silent after 114. Validation checks all raster sizes/alpha,
cast/request/audio hashes, measured cue windows, decoded track and summed mix peaks.
The review script captures every shot and completion at three aspect ratios, measures
uninterrupted playback/clock agreement, checks read-only progression snapshots, and
exercises results entry, replay, repeated Skip and Main Menu without creating saves.

Act II verification (2026-11-02): fourteen PNG layers, two exact 120-second Ogg
tracks, all ten existing cast IDs, cached request/audio hashes, subtitle text,
measured fitting windows, silent ending and decoded mix peak pass. First AXIOM
line fits at 1.1633x; all other cues retain source tempo. Inspected all 36 shot,
three completion and one results captures; all three requested image dimensions
were checked. Full playback completes with sound stopped and under 28.5 ms sampled
clock separation. Runtime navigation/progression checks pass; PostgreSQL campaign
row hashes match. Full playback and navigation evidence are separate because the
QA harness originally assumed zero polling overhead and created a duplicate results
node. The corrected review uses the real existing scorecard. --reuse-captures skips
previously inspected captures; --navigation-only isolates destination checks while
retaining full playback evidence. Neither option implies uninterrupted playback.
Speaker listening and physical controller/touch hardware remain unverified.

Final verification after all source/tool edits: background import exit 0 with all
sixteen sidecars; incremental build zero warnings/errors; NUnit 88/88, Godot 203/203
(report_402; zero failures/errors/orphans), copied-source container Django 104/104.
Known import/addon/reference diagnostics remain as documented. Keyboard arrows/
physical Enter and injected controller D-pad/A also pass actual GUI focus, Replay
and Main Menu dispatch. The owned artwork helper is stopped; all four normal
services and the original stopped Level 1 editor are preserved. No game exports.
