"""Package cached Mission 5 speech and synthesized ambience with Docker FFmpeg."""
import array
import json
import math
import random
import subprocess
import wave
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'assets/cutscenes/names_without_numbers'
CACHE = ROOT/'.validation/names-without-numbers/speech'
CUES = json.loads((OUT/'cues.json').read_text(encoding='utf-8'))


def run(args):
    """Run a deterministic FFmpeg argument list and surface failures."""
    subprocess.run(args, check=True)


def atmosphere():
    """Synthesize subdued district air, paper movement, distant footsteps and warm access tones."""
    rate = 22050
    rng = random.Random(51090)
    samples = array.array('h')
    filtered = 0.0
    for index in range(rate*90):
        t = index/rate
        noise = rng.uniform(-1,1)
        filtered = .992*filtered + .008*noise
        value = filtered*.12 + math.sin(t*math.tau*51)*.012
        for start,gain in [(2,.06),(12,.035),(31,.025),(58,.03),(68,.04)]:
            age = t-start
            if 0 <= age < .65:
                value += noise*gain*math.exp(-age*6)
        if 12 <= t < 58:
            step_age = t % .72
            value += noise*.012*math.exp(-step_age*24)
        fade = min(1,t/1.2,(90-t)/2)
        samples.append(int(max(-1,min(1,value*fade))*32767))
    wav = CACHE/'atmosphere.wav'
    with wave.open(str(wav),'wb') as audio:
        audio.setparams((1,2,rate,0,'NONE','not compressed'))
        audio.writeframes(samples.tobytes())
    run(['ffmpeg','-v','error','-y','-i',str(wav),'-c:a','libvorbis','-q:a','4',str(OUT/'atmosphere.ogg')])


def main():
    """Fit full, cached voice cues into their authored windows and emit exact-duration Ogg tracks."""
    OUT.mkdir(parents=True,exist_ok=True)
    CACHE.mkdir(parents=True,exist_ok=True)
    rate = 44100
    timeline = array.array('h',[0])*(rate*90)
    timing = []
    provenance = []
    for index,cue in enumerate(CUES):
        source = CACHE/f'cue{index+1}.mp3'
        duration = float(subprocess.check_output(['ffprobe','-v','error','-show_entries','format=duration','-of','default=nw=1:nk=1',str(source)],text=True))
        start = cue['start']+.22
        end = cue['end']-.22
        tempo = max(1.0,duration/(end-start-.1))
        if tempo >= 1.2:
            raise ValueError(f"Cue {index+1} needs unsafe {tempo:.3f}x fitting")
        normalized = CACHE/f'normalized_{index+1}.wav'
        run(['ffmpeg','-v','error','-y','-i',str(source),'-af',f'highpass=f=90,lowpass=f=7000,atempo={tempo:.6f},loudnorm=I=-18:TP=-2:LRA=7,aresample=44100,asetpts=N/SR/TB','-ar',str(rate),'-ac','1','-c:a','pcm_s16le',str(normalized)])
        with wave.open(str(normalized),'rb') as wav:
            samples = array.array('h',wav.readframes(wav.getnframes()))
        offset = round(start*rate)
        if offset+len(samples) >= round(end*rate):
            raise ValueError(f"Cue {index+1} exceeds its subtitle window")
        timeline[offset:offset+len(samples)] = samples
        timing.append({'cue':index+1,'speaker':cue['speaker']+' / '+cue['voice'],'start':start,'source_duration':duration,'tempo':tempo,'end':start+len(samples)/rate,'cached_source':source.name})
        receipt = json.loads((CACHE/f'cue{index+1}.json').read_text(encoding='utf-8'))
        provenance.append(receipt)
    mix = CACHE/'dialogue.wav'
    with wave.open(str(mix),'wb') as wav:
        wav.setparams((1,2,rate,0,'NONE','not compressed'))
        wav.writeframes(timeline.tobytes())
    run(['ffmpeg','-v','error','-y','-i',str(mix),'-c:a','libvorbis','-q:a','5',str(OUT/'dialogue.ogg')])
    atmosphere()
    (OUT/'dialogue_timing.json').write_text(json.dumps(timing,indent=2)+'\n',encoding='utf-8')
    (OUT/'speech_provenance.json').write_text(json.dumps(provenance,indent=2)+'\n',encoding='utf-8')


if __name__ == '__main__':
    main()
