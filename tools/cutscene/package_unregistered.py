"""Package cached ElevenLabs speech and original synthesized atmosphere in Docker.

Never calls a paid API. Requires ten cached cue1.mp3 through cue10.mp3 files under .validation/unregistered/speech.
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
OUT = ROOT / 'assets/cutscenes/act_ii_opening'
CACHE = ROOT / '.validation/unregistered/speech'
CUES = json.loads((OUT / 'cues.json').read_text(encoding='utf-8'))
STARTS = [cue['start'] + .2 for cue in CUES]
ENDS = [cue['end'] - .2 for cue in CUES]
PREFIXES = list(range(10))


def run(args):
    """Execute an argument vector, preserving errors while suppressing routine FFmpeg output."""
    subprocess.run(args, check=True)


def atmosphere():
    """Synthesize quiet ventilation, scanner pulses and restrained steps, fading into the silent final title."""
    rng = random.Random(20261102)
    rate = 22050
    data = array.array('h')
    air = 0.0
    for sample in range(rate * 120):
        t = sample / rate
        noise = rng.uniform(-1, 1)
        air = .98 * air + .02 * noise
        value = air * .16 + math.sin(t * math.tau * 55) * .013
        age = t % 3
        value += math.sin(t * math.tau * 1100) * .015 * math.exp(-age * 14)
        if 20 <= t < 32:
            value += math.sin(t * math.tau * 220) * .025 * math.exp(-age * 9)
        if 104 <= t < 114:
            step = (t - 104) % .65
            value += noise * .03 * math.exp(-step * 22)
        fade = max(0, min(1, t / 2, (114 - t) / 2))
        data.append(round(value * fade * 32767))
    path = CACHE / 'atmosphere.wav'
    with wave.open(str(path), 'wb') as output:
        output.setparams((1, 2, rate, 0, 'NONE', 'not compressed'))
        output.writeframes(data.tobytes())
    run(['ffmpeg', '-v', 'error', '-y', '-i', str(path), '-c:a', 'libvorbis', '-q:a', '4', str(OUT / 'atmosphere.ogg')])


def main():
    """Probe cue lengths, fit each voice window, mix a 120-second speech track and record timing evidence."""
    OUT.mkdir(parents=True, exist_ok=True)
    records = []
    rate = 44100
    timeline = array.array('h', [0]) * (rate * 120)
    for index, prefix in enumerate(PREFIXES):
        path = CACHE / f'cue{index+1}.mp3'
        length = float(subprocess.check_output(['ffprobe', '-v', 'error', '-show_entries', 'format=duration',
                                               '-of', 'default=nw=1:nk=1', str(path)], text=True))
        speed = max(1, length / (ENDS[index] - STARTS[index] - .08))
        assert speed < 1.2, (index, speed)
        eq = 'highpass=f=260,lowpass=f=3400,aecho=0.8:0.15:55:0.12,' if CUES[index]['speaker'] == 'VOSS' else 'highpass=f=150,lowpass=f=7000,' if CUES[index]['speaker'] == 'AXIOM-7' else 'highpass=f=80,'
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
        records.append({'cue': index + 1, 'speaker': CUES[index]['speaker'] + ' / ' + CUES[index]['voice'],
                        'start': STARTS[index], 'source_duration': length, 'tempo': speed,
                        'end': STARTS[index] + len(samples) / rate, 'cached_source': path.name})
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
