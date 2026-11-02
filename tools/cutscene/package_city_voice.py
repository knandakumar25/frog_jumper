"""Package cached ElevenLabs speech and original synthesized atmosphere in Docker.

Never calls a paid API. Requires ten cached cue1.mp3 through cue10.mp3 files under .validation/city-voice/speech.
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
OUT = ROOT / 'assets/cutscenes/neon_rust_aftermath'
CACHE = ROOT / '.validation/city-voice/speech'
CUES = json.loads((OUT / 'cues.json').read_text(encoding='utf-8'))
STARTS = [cue['start'] + .2 for cue in CUES]
ENDS = [cue['end'] - .2 for cue in CUES]
PREFIXES = list(range(10))


def run(args):
    """Execute an argument vector, preserving errors while suppressing routine FFmpeg output."""
    subprocess.run(args, check=True)


def atmosphere():
    """Synthesize seeded wind, rail vibration, steel creaks, footsteps and surveillance-radio textures below dialogue."""
    rng = random.Random(731)
    rate = 22050
    data = array.array('h')
    wind = 0.0
    for sample in range(rate * 90):
        t = sample / rate
        noise = rng.uniform(-1, 1)
        wind = .985 * wind + .015 * noise
        value = wind * .32 + math.sin(t * math.tau * 47) * .018 * max(0, 1-t/12)
        # Rail clatter rises as recovered cargo leaves; the oppressive carrier fades into district wind.
        rail_age = t % .78
        value += noise * (.04 if 36 <= t < 54 else .008) * math.exp(-rail_age * 24)
        # Steel creaks and cloth use brief decaying noise envelopes beneath the train bed.
        for start, gain, decay in [(2,.04,3), (11,.025,4), (18,.08,2), (23.6,.04,2), (34,.03,2), (54,.035,4), (64,.04,3)]:
            age = t - start
            if 0 <= age < 2:
                value += noise * gain * math.exp(-age * decay)
        if 54.1 <= t < 54.65:
            age = t - 54.1
            value += math.sin(age * 2 * math.pi * (1700 - age * 900)) * .028 * math.exp(-age * 8)
        for start in [0.2, 43.2]:
            age = t - start
            if 0 <= age < .22:
                value += (noise * .02 + math.sin(age * math.tau * 880) * .015) * math.sin(age / .22 * math.pi)
        # Rail grit, cloth on descent, and measured footsteps remain beneath voice.
        value += noise * .012 * max(0, math.sin(t * 17)) ** 14
        if 64 <= t < 74:
            age = (t - 64) % .65
            value += math.sin(age * math.tau * 95) * .04 * math.exp(-age * 30)
        fade = min(1, t / 1.5, (90 - t) / 1.5)
        data.append(int(max(-1, min(1, value * fade)) * 32767))
    path = CACHE / 'atmosphere.wav'
    with wave.open(str(path), 'wb') as output:
        output.setparams((1, 2, rate, 0, 'NONE', 'not compressed'))
        output.writeframes(data.tobytes())
    run(['ffmpeg', '-v', 'error', '-y', '-i', str(path), '-c:a', 'libvorbis', '-q:a', '4', str(OUT / 'atmosphere.ogg')])


def main():
    """Probe cue lengths, fit each voice window, mix a ninety-second speech track and record timing evidence."""
    OUT.mkdir(parents=True, exist_ok=True)
    records = []
    rate = 44100
    timeline = array.array('h', [0]) * (rate * 90)
    for index, prefix in enumerate(PREFIXES):
        path = CACHE / f'cue{index+1}.mp3'
        length = float(subprocess.check_output(['ffprobe', '-v', 'error', '-show_entries', 'format=duration',
                                               '-of', 'default=nw=1:nk=1', str(path)], text=True))
        speed = max(1, length / (ENDS[index] - STARTS[index] - .08))
        assert speed < 1.2, (index, speed)
        eq = 'highpass=f=150,lowpass=f=7000,' if CUES[index]['speaker'] == 'AXIOM-7' else 'highpass=f=80,'
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
