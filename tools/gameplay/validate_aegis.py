"""Validate cached, complete-word voice provenance with ffmpeg in the cutscene Docker image."""
import hashlib
import json
import struct
import subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]


def main():
    """Check audio hashes, full decoded duration and PCM peaks without listening claims."""
    # The art checks read the deleted Blender manifest; regenerated art sizes are checked by
    # ArtRegenerationSpecTests against docs/art_regeneration.json.
    audio=ROOT/'assets/audio/aegis'
    cues=json.loads((audio/'cues.json').read_text())
    records=json.loads((audio/'speech_provenance.json').read_text())
    assert len(cues)==len(records)==14
    for cue,receipt in zip(cues,records):
        path=audio/receipt['file']
        assert cue['text']==receipt['text'] and cue['id']==receipt['id']
        assert cue['voice_id']==receipt['voice_id'] and receipt['model']=='eleven_multilingual_v2'
        assert receipt['settings']==dict(stability=.7,similarity_boost=.75,style=.1,speed=1)
        assert hashlib.sha256(path.read_bytes()).hexdigest()==receipt['ogg_sha256']
        source=ROOT/'.validation/aegis/speech'/f"cue{receipt['cue']}.mp3"
        assert hashlib.sha256(source.read_bytes()).hexdigest()==receipt['audio_sha256']
        duration=float(subprocess.check_output(['ffprobe','-v','error','-show_entries','format=duration','-of','default=nw=1:nk=1',str(source)],text=True))
        assert abs(duration-receipt['duration'])<.1,'complete cue duration changed'
        raw=subprocess.check_output(['ffmpeg','-v','error','-i',str(path),'-f','f32le','-ac','1','-ar','44100','-'])
        peak=max(abs(value[0]) for value in struct.iter_unpack('<f',raw))
        assert peak<1,(cue['id'],peak)
    result=dict(art='awaiting SpriteCook regeneration',voice=len(cues),complete_words_duration=True,unclipped=True)
    out=ROOT/'.validation/aegis/media-validation.json'
    out.parent.mkdir(parents=True,exist_ok=True)
    out.write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(result))


if __name__=='__main__':main()
