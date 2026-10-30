# Live deployment cinematic

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
