"""Package and validate later-campaign dialogue and distinct atmosphere in container Python/FFmpeg.

Every complete take must fit its original window at 1.0x speed. Overruns fail with
an actionable report; no word is truncated and no tempo filter is used. Cue MP3s
remain in the ignored validation cache, while provenance and exact timelines ship.
"""
import argparse
import array
import hashlib
import json
import math
import random
import subprocess
import wave
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CACHE = ROOT / '.validation/later-campaign/speech'
REPORT = ROOT / '.validation/later-campaign/audio'
FAMILIES = ['cyber_grave', 'foundry_forge', 'stratos_docks', 'spire_outer_hull',
            'cyber_nexus', 'apex_bastion', 'judgment_day']
RATE = 44100


def run(arguments):
    """Run a deterministic media command, retaining failures rather than claiming successful packaging."""
    subprocess.run(arguments, check=True, stdout=subprocess.DEVNULL)


def digest(path):
    """Identify exact audio bytes for provenance, deduplication and uncertain production recovery."""
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read_pcm(path):
    """Read complete mono signed-16 PCM without silently trimming the start or tail."""
    with wave.open(str(path), 'rb') as stream:
        if stream.getnchannels() != 1 or stream.getsampwidth() != 2:
            raise ValueError('Expected mono signed-16 PCM.')
        return array.array('h', stream.readframes(stream.getnframes())), stream.getframerate()


def write_pcm(path, samples, rate):
    """Write an exact-length PCM source; Vorbis encoding is checked by full decode afterwards."""
    with wave.open(str(path), 'wb') as stream:
        stream.setparams((1, 2, rate, 0, 'NONE', 'not compressed'))
        stream.writeframes(samples.tobytes())


def source_for(receipt_path):
    """Resolve the native plugin's Windows output path inside the same mounted container workspace."""
    receipt = json.loads(receipt_path.read_text(encoding='utf-8'))
    path = receipt_path.parent / receipt['audio_path'].replace('\\', '/').split('/')[-1]
    if not path.is_file():
        raise FileNotFoundError(path)
    return receipt, path


def dialogue(folder):
    """Place complete normalized cues in their authored windows and emit cue timing plus voice/hash provenance."""
    output = ROOT / 'assets/cutscenes' / folder
    cues = json.loads((output/'cues.json').read_text(encoding='utf-8'))
    duration = int(max(cue['end'] for cue in cues))
    timeline = array.array('h', [0]) * (RATE * duration)
    timing, provenance, overruns = [], [], []
    for cue in cues:
        receipt_path = CACHE/folder/f"cue{cue['cue']:02}.json"
        receipt, source = source_for(receipt_path)
        if receipt['text'] != cue['text'] or receipt['voice_id'] != cue['voice_id']:
            raise ValueError(f"Stale speech cache: {folder} cue {cue['cue']}")
        normalized = REPORT/f"{folder}-{cue['cue']:02}.wav"
        run(['ffmpeg','-v','error','-y','-i',str(source),'-af',
             'highpass=f=80,lowpass=f=7500,loudnorm=I=-18:TP=-2:LRA=7',
             '-ar',str(RATE),'-ac','1','-c:a','pcm_s16le',str(normalized)])
        samples, rate = read_pcm(normalized)
        start, end = cue['start']+.15, cue['end']-.15
        take_duration = len(samples)/rate
        if take_duration > end-start:
            overruns.append(dict(scene=folder,cue=cue['cue'],duration=take_duration,
                                 available=end-start,text=cue['text']))
            continue
        offset = round(start*RATE)
        timeline[offset:offset+len(samples)] = samples
        timing.append(dict(cue=cue['cue'],speaker=cue['speaker'],start=start,end=start+take_duration,
                           window_end=end,decoded_samples=len(samples),tempo=1.0,complete_take=True))
        request = dict(text=cue['text'],voice_id=cue['voice_id'],model=cue['model'],settings=receipt['settings'])
        provenance.append(dict(cue=cue['cue'],speaker=cue['speaker'],voice=cue['voice'],
                               voice_id=cue['voice_id'],model=cue['model'],settings=receipt['settings'],
                               text=cue['text'],cast_source=cue['cast_source'],
                               source='ElevenLabs text-to-speech plugin; complete unmodified MP3 take',
                               source_sha256=digest(source),
                               request_sha256=hashlib.sha256(json.dumps(request,sort_keys=True).encode()).hexdigest()))
    if overruns:
        return overruns
    mixed = REPORT/f'{folder}-dialogue.wav'
    write_pcm(mixed,timeline,RATE)
    run(['ffmpeg','-v','error','-y','-i',str(mixed),'-c:a','libvorbis','-q:a','5',str(output/'dialogue.ogg')])
    (output/'dialogue_timing.json').write_text(json.dumps(dict(duration=duration,rate=RATE,cues=timing),indent=2)+'\n')
    (output/'speech_provenance.json').write_text(json.dumps(provenance,indent=2)+'\n')
    print('Packaged complete dialogue:',folder,flush=True)
    return []


def atmosphere(family, aftermath, duration):
    """Synthesize distinct machinery/wind/steam/flight/fortress/singularity/dawn combinations, never duplicate full beds."""
    index = FAMILIES.index(family)
    folder = family + ('_aftermath' if aftermath else '_intro')
    rng = random.Random(90601+index*37+int(aftermath)*101)
    rate = 22050
    samples = array.array('h')
    low = 0.0
    machinery = [42,56,78,63,108,48,37][index]
    if aftermath:
        machinery *= .83
    dawn = index == 6 and aftermath
    for frame in range(round(rate*duration)):
        t = frame/rate
        noise = rng.uniform(-1,1)
        low = low*.985 + noise*.015
        drone = math.sin(t*math.tau*machinery)*.026
        harmonic = math.sin(t*math.tau*(machinery*2.01))*.009
        gust = .3+.7*(.5+.5*math.sin(t*.43+index))
        wind = low * ([.12,.08,.24,.30,.06,.26,.11][index]) * gust
        hiss = noise * .016 * max(0,math.sin(t*.6+index))**12
        value = drone+harmonic+wind+hiss
        # Every family gets a different foreground texture and cue-linked transient cadence.
        pulse = (t+index*1.3) % (8.7+index*.73)
        if index in [0,1,4] and pulse < .7:
            value += noise*.042*math.exp(-pulse*8)
        if index in [2,3]:
            value += math.sin(t*math.tau*(170+math.sin(t*.16)*9))*.012*gust
        if index == 5 and pulse < 1.2:
            value += math.sin(t*math.tau*(65+pulse*16))*.035*math.exp(-pulse*3)
        if index == 6:
            value += math.sin(t*math.tau*(37+math.sin(t*.18)*12))*.018
        if dawn:
            value = wind*.5 + math.sin(t*math.tau*196)*.009 + math.sin(t*math.tau*293.66)*.006
            value += math.sin(t*math.tau*392)*.004*(.5+.5*math.sin(t*.13))
        fade = max(0,min(1,t/2,(duration-t)/3))
        samples.append(round(max(-.95,min(.95,value*fade))*32767))
    source = REPORT/f'{folder}-atmosphere.wav'
    write_pcm(source,samples,rate)
    output = ROOT/'assets/cutscenes'/folder/'atmosphere.ogg'
    run(['ffmpeg','-v','error','-y','-i',str(source),'-af','loudnorm=I=-26:TP=-4:LRA=9',
         '-ar',str(rate),'-c:a','libvorbis','-q:a','4',str(output)])
    return dict(scene=folder,duration=duration,rate=rate,sha256=digest(output),
                source='Original seeded container Python synthesis; no sampled or duplicated complete atmosphere',
                layers=['machinery','steam'] if index in [0,1,4] else ['wind','flight'] if index in [2,3]
                else ['wind','hydraulics','fortress'] if index==5 else ['wind','dawn harmonics'] if dawn else ['singularity','machinery'])


def validate(folder, duration):
    """Fully decode both tracks and reject wrong duration or clipped peaks rather than relying on file presence."""
    rows = []
    for name in ['dialogue','atmosphere']:
        source = ROOT/'assets/cutscenes'/folder/f'{name}.ogg'
        decoded = REPORT/f'{folder}-{name}-decoded.wav'
        run(['ffmpeg','-v','error','-y','-i',str(source),'-ar',str(RATE),'-ac','1','-c:a','pcm_s16le',str(decoded)])
        samples, rate = read_pcm(decoded)
        seconds = len(samples)/rate
        peak = max(abs(sample) for sample in samples)/32768
        if abs(seconds-duration) > .03 or peak >= .95:
            raise ValueError(f'{folder}/{name}: duration={seconds}, peak={peak}')
        rows.append(dict(track=name,decoded_duration=seconds,peak_dbfs=20*math.log10(max(peak,1e-9)),sha256=digest(source)))
    return rows


def main():
    """Package requested complete dialogue, generate all fourteen beds, then produce a machine-readable full-decode report."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dialogue-only',action='store_true')
    parser.add_argument('--validate-only',action='store_true')
    args = parser.parse_args()
    REPORT.mkdir(parents=True,exist_ok=True)
    overruns, scenes = [], []
    for family in FAMILIES:
        for aftermath in [False,True]:
            folder = family+('_aftermath' if aftermath else '_intro')
            duration = 120 if folder=='cyber_grave_intro' else 90
            if not args.validate_only and folder!='cyber_grave_intro':
                overruns.extend(dialogue(folder))
            if not args.validate_only and not args.dialogue_only:
                receipt = atmosphere(family,aftermath,duration)
                (ROOT/'assets/cutscenes'/folder/'atmosphere_provenance.json').write_text(json.dumps(receipt,indent=2)+'\n')
            scenes.append((folder,duration))
    (REPORT/'overruns.json').write_text(json.dumps(overruns,indent=2)+'\n')
    if overruns:
        print(json.dumps(overruns,indent=2))
        raise SystemExit('Complete takes exceed their windows; revise those lines and regenerate only those cues.')
    if args.dialogue_only:
        return
    report = {folder:validate(folder,duration) for folder,duration in scenes}
    hashes = [row[1]['sha256'] for row in report.values()]
    if len(set(hashes)) != len(hashes):
        raise SystemExit('Atmosphere tracks must be distinct.')
    (REPORT/'validation.json').write_text(json.dumps(report,indent=2)+'\n')
    # Ship hashes and full-decode evidence alongside the media, independent of ignored local review artifacts.
    (ROOT/'assets/cutscenes/later_campaign_validation.json').write_text(json.dumps(report,indent=2)+'\n')
    print('Validated fourteen complete dialogue/atmosphere timelines and fourteen distinct beds.',flush=True)


if __name__ == '__main__':
    main()
