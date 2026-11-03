"""Package cached Pre-Level 6 Cyber-Grave Intro speech and synthesized ambience with FFmpeg."""
import array
import json
import math
import random
import subprocess
import wave
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'assets/cutscenes/cyber_grave_intro'
CACHE = ROOT / '.validation/cyber-grave/speech'
CUES = json.loads((OUT / 'cues.json').read_text(encoding='utf-8'))
DURATION = 120


def run(args):
    """Run a deterministic FFmpeg argument list and surface failures."""
    subprocess.run(args, check=True)


def atmosphere():
    """Synthesize low-frequency elevator thrum, rushing shaft wind, hydraulic valve bursts, and bio-resonance drone."""
    rate = 22050
    rng = random.Random(60106)
    samples = array.array('h')
    filtered = 0.0
    for index in range(rate * DURATION):
        t = index / rate
        noise = rng.uniform(-1, 1)
        filtered = 0.991 * filtered + 0.009 * noise
        # Subterranean elevator drone at 42 Hz and 84 Hz harmonic
        drone = (math.sin(t * math.tau * 42.0) * 0.025) + (math.sin(t * math.tau * 84.0) * 0.012)
        # Bio-resonance pulse humming softly at 310 Hz
        pulse = math.sin(t * math.tau * 310.0) * (0.006 + 0.004 * math.sin(t * math.tau * 0.5))
        value = filtered * 0.14 + drone + pulse
        # Hydraulic hiss and pressure release steam bursts at cue transitions
        for start, gain in [(0.5, 0.07), (13.8, 0.05), (26.8, 0.04), (39.5, 0.05), (52.8, 0.06), (67.8, 0.05), (81.5, 0.04), (92.8, 0.05), (101.5, 0.07), (111.5, 0.09)]:
            age = t - start
            if 0 <= age < 0.85:
                value += noise * gain * math.exp(-age * 5.0)
        # Rushing elevator wind gusts
        wind_gust = math.sin(t * 0.6) * 0.015
        value += noise * max(0.0, wind_gust)
        # Elevator deceleration metallic grind in the final 10 seconds
        if 110.0 <= t < 118.0:
            age = t - 110.0
            decel = math.sin(age * math.tau * (120.0 - age * 8.0)) * 0.025
            value += decel + noise * 0.03 * (age / 8.0)
        fade = min(1.0, t / 1.5, (DURATION - t) / 2.0)
        samples.append(int(max(-1.0, min(1.0, value * fade)) * 32767))
    wav = CACHE / 'atmosphere.wav'
    with wave.open(str(wav), 'wb') as audio:
        audio.setparams((1, 2, rate, 0, 'NONE', 'not compressed'))
        audio.writeframes(samples.tobytes())
    run(['ffmpeg', '-v', 'error', '-y', '-i', str(wav), '-c:a', 'libvorbis', '-q:a', '4', str(OUT / 'atmosphere.ogg')])
    print(f'Generated atmosphere.ogg (exact {DURATION}s)', flush=True)


def main():
    """Fit full, cached voice cues into their authored windows and emit exact-duration Ogg tracks."""
    OUT.mkdir(parents=True, exist_ok=True)
    CACHE.mkdir(parents=True, exist_ok=True)
    rate = 44100
    timeline = array.array('h', [0]) * (rate * DURATION)
    timing = []
    provenance = []
    for index, cue in enumerate(CUES):
        source = CACHE / f'cue{index + 1}.mp3'
        duration = float(subprocess.check_output(
            ['ffprobe', '-v', 'error', '-show_entries', 'format=duration', '-of', 'default=nw=1:nk=1', str(source)],
            text=True))
        start = cue['start'] + 0.15
        end = cue['end'] - 0.15
        available = end - start
        tempo = max(1.0, duration / available) if duration > available else 1.0
        if tempo >= 1.25:
            raise ValueError(f"Cue {index + 1} needs unsafe {tempo:.3f}x fitting")
        normalized = CACHE / f'normalized_{index + 1}.wav'
        run(['ffmpeg', '-v', 'error', '-y', '-i', str(source),
             '-af', f'highpass=f=90,lowpass=f=7000,atempo={tempo:.6f},loudnorm=I=-18:TP=-2:LRA=7,aresample=44100,asetpts=N/SR/TB',
             '-ar', str(rate), '-ac', '1', '-c:a', 'pcm_s16le', str(normalized)])
        with wave.open(str(normalized), 'rb') as wav:
            samples = array.array('h', wav.readframes(wav.getnframes()))
        offset = round(start * rate)
        if offset + len(samples) > round(end * rate):
            # Clip tail if slight rounding overflow
            samples = samples[:round(end * rate) - offset]
        for i, s in enumerate(samples):
            timeline[offset + i] = max(-32768, min(32767, timeline[offset + i] + s))
        timing.append(dict(cue=index + 1, speaker=cue['speaker'], start=start,
                           duration=len(samples) / rate, window=cue['end'] - cue['start'], tempo=tempo))
        receipt = json.loads((CACHE / f'cue{index + 1}.json').read_text(encoding='utf-8'))
        provenance.append(receipt)

    mix_wav = CACHE / 'dialogue_mixed.wav'
    with wave.open(str(mix_wav), 'wb') as audio:
        audio.setparams((1, 2, rate, 0, 'NONE', 'not compressed'))
        audio.writeframes(timeline.tobytes())
    run(['ffmpeg', '-v', 'error', '-y', '-i', str(mix_wav), '-c:a', 'libvorbis', '-q:a', '5', str(OUT / 'dialogue.ogg')])
    print(f'Generated dialogue.ogg (exact {DURATION}s)', flush=True)

    # Generate atmospheric audio track
    atmosphere()

    # Save packaging report
    report = dict(timeline=f'Exact {DURATION}.00-second Pre-Level 6 Cyber-Grave Intro track',
                  rate=rate, cues=timing, sources=provenance)
    (OUT / 'packaging.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print('Saved packaging.json', flush=True)


if __name__ == '__main__':
    main()
