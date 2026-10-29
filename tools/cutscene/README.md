# Deployment film

Run `docker compose --profile cutscene run --build --rm cutscene` from the
repository root. To reuse plates in `.validation/deployment`, append
`--package-only`. The output is `assets/cutscenes/deployment/deployment.ogv`.
The container pins Blender 4.3.2; the initial plates were rendered with the
installed Windows Blender 5.2.3. Both use the existing planar art helper.

The current film is a four-plate animatic with camera moves, fades, and generated
low-frequency atmosphere. It does not yet contain animated running, rain,
surveillance sweeps. These remain visual polish work. ElevenLabs narration is
now provided separately in `assets/cutscenes/deployment/narration.ogg`.

## ElevenLabs narration handoff

The ElevenLabs API key is configured in the gitignored root `.env`. MCP tools
remain unavailable, so `python tools/cutscene/narrate.py` uses the direct API.
Daniel (selected from the live voice list) supplies a steady mission-control voice
with multilingual v2. The script generates and caches four cues, then saves the combined
40-second track as `assets/cutscenes/deployment/narration.ogg`, placing each cue at
the indicated start and preserving the silent gaps. Do not stretch speech past its
window. The script checks cue lengths and applies subtle radio EQ before mixing.
Existing cached cues are reused without another paid request. Set
`DEPLOYMENT_DOCKER` to the Docker executable path if it is absent from PATH.
The game loads the voice separately from the video after Godot import.

| Window | Script |
| --- | --- |
| 1–9.5 s | Beyond the neon, the city waits. Every rooftop is watched. Every signal is traced. |
| 11–19.5 s | But one operative moves beyond their reach. Cyber Shogun. Your deployment begins tonight. |
| 21–31 s | Follow the rooftops. Keep your blade ready. Somewhere beneath these lights, the resistance is still alive. |
| 33–39 s | This is AXIOM-7. Secure channel established. Stand by for your mission briefing. |

Subtitles use these English strings as translation keys, with English fallback.
Translations are not supplied yet. Preview frames and tool logs stay in
`.validation`, outside game assets. No game export is needed to regenerate media.
