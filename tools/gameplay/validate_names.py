"""Validate registry RGBA art and cached, complete-word voice provenance inside the owned Docker/Blender helper."""
import hashlib
import json
import struct
import subprocess
from pathlib import Path
import bpy
ROOT=Path(__file__).resolve().parents[2]


def main():
    """Check dimensions, genuine alpha/margins, audio hashes, full decoded duration and PCM peaks without listening claims."""
    art=ROOT/'assets/environment/names'
    assets=json.loads((art/'manifest.json').read_text())['assets']
    for entry in assets:
        image=bpy.data.images.load(str(art/entry['file']),check_existing=False)
        assert list(image.size)==[512,512]
        pixels=list(image.pixels)
        alpha=pixels[3::4]
        assert min(alpha)==0 and max(alpha)>0.9
        for i in list(range(512))+list(range(511*512,512*512))+[y*512 for y in range(512)]+[y*512+511 for y in range(512)]:
            assert alpha[i]==0,entry['file']+' clipped at canvas edge'
        bpy.data.images.remove(image)
    audio=ROOT/'assets/audio/names'
    cues=json.loads((audio/'cues.json').read_text())
    records=json.loads((audio/'speech_provenance.json').read_text())
    assert len(cues)==len(records)==19
    for cue,receipt in zip(cues,records):
        path=audio/receipt['file']
        assert cue['text']==receipt['text'] and cue['id']==receipt['id']
        assert cue['voice_id']==receipt['voice_id'] and receipt['model']=='eleven_multilingual_v2'
        assert receipt['settings']==dict(stability=.7,similarity_boost=.75,style=.1,speed=1)
        assert hashlib.sha256(path.read_bytes()).hexdigest()==receipt['ogg_sha256']
        source=ROOT/'.validation/names/speech'/f"cue{receipt['cue']}.mp3"
        assert hashlib.sha256(source.read_bytes()).hexdigest()==receipt['audio_sha256']
        duration=float(subprocess.check_output(['ffprobe','-v','error','-show_entries','format=duration','-of','default=nw=1:nk=1',str(source)],text=True))
        assert abs(duration-receipt['duration'])<.1,'complete cue duration changed'
        raw=subprocess.check_output(['ffmpeg','-v','error','-i',str(path),'-f','f32le','-ac','1','-ar','44100','-'])
        peak=max(abs(value[0]) for value in struct.iter_unpack('<f',raw))
        assert peak<1,(cue['id'],peak)
    result=dict(art=len(assets),voice=len(cues),alpha_margins=True,complete_words_duration=True,unclipped=True)
    out=ROOT/'.validation/names/media-validation.json'
    out.parent.mkdir(parents=True,exist_ok=True)
    out.write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(result))


if __name__=='__main__':main()
