"""Validate Act II layered raster, cached request integrity, timing, cast and decoded mix in Docker."""
import array
import hashlib
import json
import subprocess
from pathlib import Path
from validate_live import pixels

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'assets/cutscenes/act_ii_opening'


def main():
    """Reject malformed layers, incomplete words, changed requests, incorrect subtitles or clipped audio."""
    records = []
    for scene in ['queue','scanner','registry','service','civilians']:
        for layer in (['structure','lights'] if scene == 'civilians' else ['base','structure','lights']):
            name = f'{scene}_{layer}'
            width,height,rows = pixels(OUT/f'{name}.png')
            assert (width,height) == (1920,1080), name
            alpha = [row[x] for row in rows for x in range(3,width*4,4)]
            assert max(alpha) == 255, name
            if layer != 'base':
                assert min(alpha) == 0, name
            records.append(dict(asset=name,size=[width,height],sha256=hashlib.sha256((OUT/f'{name}.png').read_bytes()).hexdigest()))
    decoded = {}
    for name in ['dialogue','atmosphere']:
        path = OUT/f'{name}.ogg'
        duration = float(subprocess.check_output(['ffprobe','-v','error','-show_entries','format=duration','-of','default=nw=1:nk=1',str(path)],text=True))
        assert abs(duration-120) < .05, (name,duration)
        data = array.array('h',subprocess.check_output(['ffmpeg','-v','error','-i',str(path),'-ar','44100','-f','s16le','-ac','1','-']))
        peak = max(abs(sample) for sample in data)
        assert 0 < peak < 32767, (name,peak)
        assert max(abs(s) for s in data[round(114*44100):]) < 32, name
        decoded[name] = data
        records.append(dict(audio=name,duration=duration,decoded_peak=peak))
    peak = max(abs(a+round(b*.126)) for a,b in zip(decoded['dialogue'],decoded['atmosphere']))
    assert peak < 32767, peak
    cues = json.loads((OUT/'cues.json').read_text(encoding='utf-8'))
    timing = json.loads((OUT/'dialogue_timing.json').read_text())
    provenance = json.loads((OUT/'speech_provenance.json').read_text())
    cast = {c['speaker']:c['voice_id'] for c in json.loads((ROOT/'assets/cutscenes/shattered_aegis_intro/cues.json').read_text(encoding='utf-8'))}
    assert len(cues) == len(timing) == len(provenance) == 10
    source = (ROOT/'scripts/ui/ActIIOpening.cs').read_text(encoding='utf-8')
    for cue,record,receipt in zip(cues,timing,provenance):
        assert cue['start'] < record['start'] < record['end'] < cue['end'], record
        assert record['tempo'] < 1.2, record
        assert cue['voice_id'] == receipt['voice_id'] == cast[cue['speaker']]
        assert cue['model'] == receipt['model'] == 'eleven_multilingual_v2'
        body = dict(text=cue['text'],model_id=cue['model'],voice_settings=receipt['settings'])
        digest = hashlib.sha256(json.dumps(body,sort_keys=True).encode()+cue['voice_id'].encode()).hexdigest()
        assert digest == receipt['request_sha256']
        cached = ROOT/'.validation/unregistered/speech'/record['cached_source']
        assert hashlib.sha256(cached.read_bytes()).hexdigest() == receipt['audio_sha256']
        assert cue['speaker']+': '+cue['text'] in source
    assert 'Duration = 120' in source
    assert '{ 0, 8, 20, 32, 44, 56, 68, 80, 92, 104, 110, 114 }' in source
    records.append(dict(combined_decoded_peak=peak,cues=timing))
    (ROOT/'.validation/unregistered/assets.json').write_text(json.dumps(records,indent=2)+'\n')
    print('Act II raster, cast, hashes, exact 120-second tracks, silent ending and decoded mix pass.')


if __name__ == '__main__':
    main()
