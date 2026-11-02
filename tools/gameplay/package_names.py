"""Package complete cached cues as individual Ogg streams in the owned Docker media container.

Never trims words, changes tempo, synthesizes speech or mixes overlapping lines. Measures duration and hashes.
"""
import hashlib
import json
import subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'assets/audio/names'


def main():
    """Decode every cached response, encode complete audio and record nonsecret production receipts."""
    cues=json.loads((OUT/'cues.json').read_text())
    records=json.loads((OUT/'speech_provenance.json').read_text())
    for i,(cue,receipt) in enumerate(zip(cues,records),1):
        source=ROOT/'.validation/names/speech'/f'cue{i}.mp3'
        target=OUT/(cue['id']+'.ogg')
        subprocess.run(['ffmpeg','-v','error','-y','-i',str(source),'-af','alimiter=limit=0.9','-c:a','libvorbis','-q:a','5',str(target)],check=True)
        duration=float(subprocess.check_output(['ffprobe','-v','error','-show_entries','format=duration','-of','default=nw=1:nk=1',str(target)],text=True))
        receipt.update(id=cue['id'],text=cue['text'],file=target.name,duration=duration,ogg_sha256=hashlib.sha256(target.read_bytes()).hexdigest())
        print(cue['id'],duration,flush=True)
    (OUT/'speech_provenance.json').write_text(json.dumps(records,indent=2)+'\n')


if __name__=='__main__':main()
