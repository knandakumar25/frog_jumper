"""Synthesize restrained registry atmosphere and scanner cues using Python/FFmpeg inside the Docker media helper."""
import json
import math
import struct
import subprocess
import wave
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'assets/audio/names'


def main():
    """Write deterministic low-amplitude tones, preserving headroom below speech, with reproducible provenance."""
    OUT.mkdir(parents=True,exist_ok=True)
    for name,seconds in [('atmosphere',24),('scanner_warning',.28),('scanner_live',.18),('accepted',.6)]:
        pcm=bytearray()
        for i in range(int(22050*seconds)):
            t=i/22050
            if name=='atmosphere':value=.12*math.sin(math.tau*55*t)+.035*math.sin(math.tau*82.5*t)*(.6+.4*math.cos(math.tau*t/8))
            else:
                frequency={'scanner_warning':660,'scanner_live':1040,'accepted':880}[name]
                value=.15*math.sin(math.tau*frequency*t)*math.sin(math.pi*t/seconds)**2
            pcm.extend(struct.pack('<h',int(value*32767)))
        source=OUT/(name+'.wav')
        with wave.open(str(source),'wb') as audio:
            audio.setnchannels(1);audio.setsampwidth(2);audio.setframerate(22050);audio.writeframes(pcm)
        subprocess.run(['ffmpeg','-v','error','-y','-i',str(source),'-c:a','libvorbis','-q:a','4',str(OUT/(name+'.ogg'))],check=True)
        source.unlink()
    (OUT/'sound_provenance.json').write_text(json.dumps(dict(generator='tools/gameplay/sound_names.py',source='Original deterministic sine synthesis; no sampled speech',rate=22050,atmosphere_seconds=24,peak_amplitude_bound=.155),indent=2)+'\n')


if __name__=='__main__':main()
