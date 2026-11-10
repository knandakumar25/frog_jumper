# Asset quality audit — November 2026

What was measured, what was changed with Python and Blender, what was deliberately left alone, and what is still open.

## Open problems and decisions

- **Mission music level (fixed at playback, 2026-11-10, owner approved).** Mission music used to play at +24 dB on Master (`CodeLevel.BuildAudioAndPause`):
  - The atmosphere tracks (-25.8 LUFS) played at about -1.8 LUFS, about 18 LU over speech.
  - The default track (`Sakura-Girl-Yay`, -11.3 LUFS, used by missions 1–4 and the city operations) played at about +12.7 LUFS, far past full scale.
  - Names Without Numbers overrode its atmosphere to -21 dB, about -46 LUFS, close to inaudible.

  Now `tools/assets/music_gain.py` measures every `MusicPath` track and writes `assets/audio/music_gain.json`, and `CodeLevel` plays the track on the Music bus at that gain:
  - The target is -29 LUFS, about 9 LU under mission speech; cutscenes keep their atmosphere about 14 LU under dialogue.
  - The gains are -17.7 dB for the default track and -3.2 to -3.8 dB for the atmospheres, each capped so the true peak stays at or below -1 dBTP.
  - `MusicGainTests` fails if a track is added or changed without regenerating the table.
  - A GdUnit test reads every mission scene's real `MusicPath`, and `MusicGain` warns at runtime on a missing entry.
  - No ducking under speech was added. Listen for whether -29 LUFS is thin for the melodic default track, and for the step of about 2.7 LU from intro cutscenes (atmosphere at about -31.7 LUFS) into their missions.
- **Voice loudness (fixed at playback, 2026-11-10).** The 98 played recordings ranged from -24.8 to -13.9 LUFS (about 11 LU). The files were not re-encoded, because tests, transcripts and the owner's listening sign-off record their hashes. Instead:
  - `tools/assets/voice_gain.py` measures exactly the recordings the game plays: 93 mission speech cues, 4 AXIOM-7 briefing lines and the deployment narration. It writes `assets/audio/voice_gain.json`, with a gain per recording toward -18 LUFS (from -4.1 to +5.5 dB).
  - Each boost is capped so the limiter removes at most 2 dB of peak. 16 lines with high peaks therefore stay up to 2.2 LU under the target; the narration, for example, gets +4.6 dB and plays at -20.2 LUFS.
  - Played levels now span -20.2 to -18.0 LUFS, before the mission player's existing -2 dB offset (mission speech plays about 2 dB under the briefing).
  - `scripts/managers/VoiceGain.cs` adds the gain in `MissionSpeechQueue` and `StoryIntro`; `deployment_cutscene.gd` does the same for the narration.
  - The players use a new `Voice` bus that sends into SFX and has a hard limiter at -1 dB. Without the Voice bus, and on Web (where sample playback skips bus effects), only cuts are applied.
  - `VoiceGainTests` fails if a recording changes without the table being regenerated, or if a gain breaks the target or limiter-budget rule.
- **Clipping in third-party music** (`Street_Level_Siege.mp3`, `Dystopian_Dawn_FULL_SONG_MusicGPT (2).mp3`, `Sakura-Girl-Yay-chosic.com_.mp3`, `retro-coin-1-236677.mp3`). It is baked into the source files, so lowering the gain does not remove it. Left as is; the +24 dB playback that added far more clipping is fixed (above).
- **22 voice files start with more than 0.3 s of silence.** They were not trimmed, because subtitle and cutscene timings were authored against them. (`audit_audio.py` now also measures trailing silence; the first reports recorded none because of a detection bug.)
- **Title wording.** The owner chose to keep the logos' wording and ignore the translation catalog's `AXIOM FORCE` row. The smaller Arabic title is also accepted.
- **Not checked by ear or by a person in the running game.** All checks are measurements and side-by-side images. First listening check: the deployment narration, which gets the largest limiter reduction (2 dB).

## How it was measured

| Tool | Runs in | Measures |
|---|---|---|
| `tools/assets/audit_images.py` | Blender Python | size, alpha, **edge softness** (share of cut-out edge pixels with partial alpha: about 0 = stair-stepped, 1 = anti-aliased), margins, exact duplicates, generator script |
| `tools/assets/compare_images.py` | Blender Python | before/after zoomed crops, softness, size or name changes (failures) |
| `tools/assets/audit_audio.py` | cutscene container (ffmpeg) | EBU R128 loudness, true peak, sample peak, clipping, edge silence |

Reports and comparison sheets are in `.validation/asset_quality/`, which is not committed: `images.json` (before), `images_after.json`, `audio.json`, `kit_compare.png`, `later_compare.png`, `menu_compare.png`, `wildfire_trial.png`, `titles_sheet.png`. Copies of the old images are in `before/`.

The audit covered 563 images and 316 audio files.

## What changed

| Group | Before | After | How |
|---|---|---|---|
| Cutscene layers (`assets/cutscenes`, 54 files) | mean edge softness 0.0–0.5; rendered at 1 Cycles sample | 1.0 | `render_quality.apply_flat`: 16 samples and a 1.5 px Blackman-Harris filter; re-rendered by `rerender_flat_art.py` |
| Gameplay layers (`assets/environment`, 25) | 0.0 | 1.0 | same |
| Later-campaign art (`assets/environments/later_campaign`, 54 layers: 21 environment, 21 aftermath, 12 boss) | 0.0 | 1.0 (8 layers checked) | same; `--targets` now runs before `--produce`, which had only copied old renders |
| City kit (`assets/world`, 232) | 64 samples, visible grain | 512 samples, less grain; sizes unchanged | `render_quality.apply_lit` (no denoiser, so the container's Blender 4.3 and the host's 5.2 use the same settings) |
| Synthesized SFX (`assets/audio/sfx`, 9) | blade_clash +0.5 dBTP, drone_explosion +2.7 dBTP | every true peak -2.1 dBTP or lower | `generate_cyber_sfx.py`: seeded noise, DC removal, 2/5 ms fades, sample-peak ceiling 0.7. blade_clash and drone_explosion are about 3 dB quieter |
| Robot stand-in sheets (24) | shimmer when drawn at 0.3x | mipmaps | `.import` `mipmaps/generate=true`, plus `TextureFilterEnum.LinearWithMipmaps` in `WorldHostile.cs` |
| Title logos (32 game files plus 39 in each of the two website folders: 110 files) | painted JPEG cut-outs with black fringes and noise | Blender 3D scene, one script for all languages | `tools/art/render_title_logo.py` (see below) |

`images_after.json` shows no size change for any re-rendered image, and `compare_images.py` reported 0 failures on the kit, later-campaign, menu and Wildfire sets. The Wildfire `facade.png` windows moved 3 px left so its soft edge keeps the margin `validate_wildfire.py` requires. The image manifests that the NUnit tests hash were regenerated by the generators.

### Title logos

- Rebuilt as geometry: a bevelled plate, neon tubes, a winged gear crest, and chrome text with cyan and magenta rims.
- Blender text does no shaping. Hebrew is reversed. Arabic is converted to contextual presentation forms and then reversed.
- Before rendering anything, `check_glyphs` raises `ValueError` if a font lacks a character, so a failure never leaves a mix of old and new titles. Cairo and Tajawal lack the isolated ة, أ and م, so Arabic uses Noto Sans Arabic.
- The neon rims are drawn in 2D after rendering. The text's alpha mask is widened with numpy and composited under the chrome text, in 8-bit display values (as Godot composites UI); the render script refuses float layers. Growing the outlines in 3D was tried first and failed: spikes on sharp corners (Arabic, Hebrew, Japanese), and the overlapping glyph outlines of the Korean font misfilled whole regions.
- The plate is rendered once and reused for every language.
- Limits:
  - The Arabic title is smaller than the old painted one, because its tall letters hit the height limit.
  - Thai marks rely on the font's own positioning.
  - The shaper supports plain letters only. It raises an error on lam-alef, diacritics or mixed scripts rather than drawing them wrongly.

## Deliberately not changed

- **Menu backdrops.** They were already anti-aliased by EEVEE (softness 1.0). A Cycles re-render looked identical, so the committed files were kept.
- **Flat characters, hand-drawn pose sheets, the painted Level 1 backdrop, the story image.** They have no generator script and already score 1.0.
- **Fonts, pixel-art stand-ins** (hormelz robot, Foozle boss, Kenney), `world_tileset.png`, `cyber_speech_frame.png`. Hard pixel edges are intended in pixel art.
- **Kenney stand-in sounds.** Only `laserRetro_000.ogg` exceeded 0 dBTP (+1.2). Lowering it would need a lossy re-encode of a third-party file for a 1.2 dB overshoot.

## Unused files

These are found by name only. Nothing was deleted, because some paths are built at runtime.

- Title JPGs (`axiom_force_title_logo*.jpg`), `axiom_force_logo_clean.png`, the emblem, `blender_title_preview.png`, `cooltext474246181467297.png` and the story-screen image.
- Pixel Adventure leftovers: `Cherries.png`, `Terrain (16x16).png`, `Blue`/`Green` backgrounds, `End (Idle).png`.
- 187 image files have an exact duplicate somewhere, mostly website copies and the paired language names of each title.

## Reproducing

```text
python tools/assets/rerender_flat_art.py                       # flat art, host Blender 5.2
blender --background --factory-startup --python tools/world/build_kit.py
blender --background --factory-startup --python-exit-code 1 --python tools/art/render_title_logo.py
python scripts/tools/generate_cyber_sfx.py
```

Re-render a whole family on one Blender version. The 4.3 and 5.2 outputs have not been compared pixel for pixel.

## Review

The tool code went through two rounds of fresh-context EGM Goldfish review (codebase details, design critic, style).

Round 1 (2026-11-09), accepted and fixed:
- voice re-encoding (reverted);
- broken denoiser detection (removed);
- later-campaign targets not re-rendered;
- an unsafe default output folder for `--preview`;
- missing glyph checks;
- inaccurate docstrings.

Contested: "Latin titles were dropped". The old logos showed "AXIOM FORCE" for every Latin-script language, which this script reproduces.

Round 2 (2026-11-10), accepted and fixed:
- limiter budget cap and recorded expected limiting;
- Web and missing-bus cuts-only rule;
- 8-bit pin for the title layers;
- validating every title before rendering;
- trailing-silence detection;
- `rerender_flat_art.py` no longer overwriting the kept menu backdrops;
- the narration end-to-end test;
- consistency of documented numbers.

Partly contested: the Web limiter gap. Godot 4 cannot export C# to Web, so only the GDScript narration path could run there; it now applies cuts only on Web.

Reported, then fixed at the owner's request: the +24 dB mission music (above).
