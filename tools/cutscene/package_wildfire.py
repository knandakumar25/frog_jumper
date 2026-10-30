"""Package cached ElevenLabs MCP speech and original synthesized atmosphere in Docker.

Never calls a paid API. Requires seven cached tts_*.mp3 files under .validation/wildfire/speech.
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
OUT = ROOT / 'assets/cutscenes/wildfire'
CACHE = ROOT / '.validation/wildfire/speech'
STARTS = [0.25, 10.2, 20.2, 30.2, 40.2, 50.2, 57.6]
ENDS = [9.8, 19.8, 29.8, 39.8, 49.8, 57.3, 59.8]
PREFIXES = ['Conta', 'At_th', 'Siege', 'Three', 'Then_', 'Follo', 'I_m']


def run(args):
    """Execute an argument vector, preserving errors while suppressing routine FFmpeg output."""
    subprocess.run(args, check=True)


def atmosphere():
    """Synthesize seeded wind, falling debris, cloth, sheath and secure-radio textures below dialogue."""
    rng = random.Random(714)
    rate = 22050
    data = array.array('h')
    wind = 0.0
    for sample in range(rate * 60):
        t = sample / rate
        noise = rng.uniform(-1, 1)
        wind = .985 * wind + .015 * noise
        value = wind * .32
        # Debris and cloth are brief decaying noise envelopes, never a demolition blast.
        for start, gain, decay in [(2,.04,3), (11,.025,4), (21,.12,2), (23.6,.1,2), (26,.1,2), (42,.035,4), (51,.04,3)]:
            age = t - start
            if 0 <= age < 2:
                value += noise * gain * math.exp(-age * decay)
        if 45.1 <= t < 45.65:
            age = t - 45.1
            value += math.sin(age * 2 * math.pi * (1700 - age * 900)) * .028 * math.exp(-age * 8)
        for start in [0.2, 20.1, 30.1, 50.1]:
            age = t - start
            if 0 <= age < .22:
                value += (noise * .02 + math.sin(age * math.tau * 880) * .015) * math.sin(age / .22 * math.pi)
        # Low fire crackle, cloth on descent, and measured footsteps remain beneath voice.
        value += noise * .012 * max(0, math.sin(t * 17)) ** 14
        if 40 <= t < 57:
            age = (t - 40) % .65
            value += math.sin(age * math.tau * 95) * .04 * math.exp(-age * 30)
        fade = min(1, t / 1.5, (60 - t) / 1.5)
        data.append(int(max(-1, min(1, value * fade)) * 32767))
    path = CACHE / 'atmosphere.wav'
    with wave.open(str(path), 'wb') as output:
        output.setparams((1, 2, rate, 0, 'NONE', 'not compressed'))
        output.writeframes(data.tobytes())
    run(['ffmpeg', '-v', 'error', '-y', '-i', str(path), '-c:a', 'libvorbis', '-q:a', '4', str(OUT / 'atmosphere.ogg')])


def main():
    """Probe cue lengths, fit each voice window, mix a sixty-second speech track and record timing evidence."""
    OUT.mkdir(parents=True, exist_ok=True)
    records = []
    rate = 44100
    timeline = array.array('h', [0]) * (rate * 60)
    for index, prefix in enumerate(PREFIXES):
        record = json.loads((CACHE / f'cue-{index+1}.json').read_text())
        cached_name = record['result']['content'][0]['text'].split('File saved as: ')[1].split('.mp3')[0].split('\\')[-1] + '.mp3'
        path = CACHE / cached_name
        length = float(subprocess.check_output(['ffprobe', '-v', 'error', '-show_entries', 'format=duration',
                                               '-of', 'default=nw=1:nk=1', str(path)], text=True))
        speed = max(1, length / (ENDS[index] - STARTS[index] - .08))
        eq = 'highpass=f=150,lowpass=f=7000,' if index not in [1, 4, 6] else 'highpass=f=80,'
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
        records.append({'cue': index + 1, 'speaker': 'AXIOM-7 / Daniel' if index not in [1, 4, 6] else 'Kaito / Adam',
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
