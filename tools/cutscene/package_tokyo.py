"""Package cached ElevenLabs MCP speech and original synthesized atmosphere in Docker.

Never calls a paid API. Requires six cached tts_*.mp3 files under .validation/tokyo/speech.
Cues are normalized and fitted inside their shot windows without truncating words.
"""
import array
import json
import math
import random
import subprocess
import wave
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'assets/cutscenes/tokyo'
CACHE = ROOT / '.validation/tokyo/speech'
STARTS = [0.25, 8.2, 15.2, 23.2, 29.2, 37.2]
ENDS = [7.8, 14.8, 22.8, 28.8, 36.8, 39.8]
PREFIXES = ['Extra', 'They_', 'The_a', 'The_d', 'Enfor', 'Then_']


def run(args):
    """Execute an argument vector, preserving errors while suppressing routine FFmpeg output."""
    subprocess.run(args, check=True)


def atmosphere():
    """Synthesize seeded wind, falling debris, cloth, sheath and secure-radio textures below dialogue."""
    rng = random.Random(714)
    rate = 22050
    data = array.array('h')
    wind = 0.0
    for sample in range(rate * 40):
        t = sample / rate
        noise = rng.uniform(-1, 1)
        wind = .985 * wind + .015 * noise
        value = wind * .32
        # Debris and cloth are brief decaying noise envelopes, never a demolition blast.
        for start, gain, decay in [(1, .07, 3), (3.7, .055, 4), (16, .04, 2), (23.6, .035, 4), (37, .04, 3)]:
            age = t - start
            if 0 <= age < 2:
                value += noise * gain * math.exp(-age * decay)
        if 24.1 <= t < 24.65:
            age = t - 24.1
            value += math.sin(age * 2 * math.pi * (1700 - age * 900)) * .028 * math.exp(-age * 8)
        for start in [0.2, 15.1, 29.1]:
            age = t - start
            if 0 <= age < .22:
                value += (noise * .02 + math.sin(age * math.tau * 880) * .015) * math.sin(age / .22 * math.pi)
        fade = min(1, t / 1.5, (40 - t) / 1.5)
        data.append(int(max(-1, min(1, value * fade)) * 32767))
    path = CACHE / 'atmosphere.wav'
    with wave.open(str(path), 'wb') as output:
        output.setparams((1, 2, rate, 0, 'NONE', 'not compressed'))
        output.writeframes(data.tobytes())
    run(['ffmpeg', '-v', 'error', '-y', '-i', str(path), '-c:a', 'libvorbis', '-q:a', '4', str(OUT / 'atmosphere.ogg')])


def main():
    """Probe cue lengths, fit each voice window, mix a forty-second speech track and record timing evidence."""
    OUT.mkdir(parents=True, exist_ok=True)
    records = []
    rate = 44100
    timeline = array.array('h', [0]) * (rate * 40)
    for index, prefix in enumerate(PREFIXES):
        path = sorted(CACHE.glob(f'tts_{prefix}*.mp3'))[-1]
        length = float(subprocess.check_output(['ffprobe', '-v', 'error', '-show_entries', 'format=duration',
                                               '-of', 'default=nw=1:nk=1', str(path)], text=True))
        speed = max(1, length / (ENDS[index] - STARTS[index]))
        eq = 'highpass=f=150,lowpass=f=7000,' if index % 2 == 0 else 'highpass=f=80,'
        normalized = CACHE / f'normalized_{index + 1}.wav'
        # Explicit PCM assembly avoids FFmpeg filter-graph timebase overflow when mixing
        # several loudnorm outputs (192 kHz internally) with long millisecond delays.
        run(['ffmpeg', '-v', 'error', '-y', '-i', str(path), '-af',
             f'{eq}atempo={speed:.6f},loudnorm=I=-18:TP=-2:LRA=7,aresample=44100,asetpts=N/SR/TB',
             '-ar', str(rate), '-ac', '1', '-c:a', 'pcm_s16le', str(normalized)])
        with wave.open(str(normalized), 'rb') as source:
            samples = array.array('h', source.readframes(source.getnframes()))
        start = round(STARTS[index] * rate)
        assert start + len(samples) < round(ENDS[index] * rate), (index, len(samples))
        timeline[start:start + len(samples)] = samples
        records.append({'cue': index + 1, 'speaker': 'AXIOM-7 / Daniel' if index % 2 == 0 else 'Kaito / Adam',
                        'start': STARTS[index], 'source_duration': length, 'tempo': speed,
                        'end': STARTS[index] + length / speed, 'cached_source': path.name})
    mixed = CACHE / 'dialogue.wav'
    with wave.open(str(mixed), 'wb') as output:
        output.setparams((1, 2, rate, 0, 'NONE', 'not compressed'))
        output.writeframes(timeline.tobytes())
    run(['ffmpeg', '-v', 'error', '-y', '-i', str(mixed), '-c:a', 'libvorbis', '-q:a', '5', str(OUT / 'dialogue.ogg')])
    atmosphere()
    (OUT / 'dialogue_timing.json').write_text(json.dumps(records, indent=2) + '\n')
    print(json.dumps(records, indent=2))


if __name__ == '__main__':
    main()
