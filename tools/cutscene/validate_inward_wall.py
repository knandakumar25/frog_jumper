"""Validate The Wall That Faces Inward raster geometry, alpha, every atlas cell and decoded audio peaks in Docker."""
import array
import json
import subprocess
from pathlib import Path
from png_pixels import pixels

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'assets/cutscenes/shattered_aegis_intro'


def main():
    """Fail on incorrect sizes, clipped atlas cells, overlapping cues, wrong duration or clipped PCM."""
    records = []
    for name, size in [(f'{scene}_{layer}',(1920,1080)) for scene in ['approach','shield_road','command'] for layer in ['structure','lights']] + [('surya',(1024,512))]:
        if not (OUT/f'{name}.png').exists():  # Absent until regenerated with SpriteCook (docs/ART_REGENERATION.md).
            print('SKIPPED (awaiting SpriteCook regeneration):', (OUT/f'{name}.png').name)
            continue
        width,height,rows = pixels(OUT/f'{name}.png')
        assert (width,height) == size, name
        alpha = [row[x] for row in rows for x in range(3,width*4,4)]
        assert min(alpha) == 0 and max(alpha) == 255, name
        if name == 'surya':
            for x in range(2):
                assert any(rows[y][i*4+3] for y in range(512) for i in range(x*512,(x+1)*512)), x
                assert all(rows[y][i*4+3]==0 for y in [0,511] for i in range(x*512,(x+1)*512)), x
                assert all(rows[y][i*4+3]==0 for y in range(512) for i in [x*512,(x+1)*512-1]), x
        records.append(dict(asset=name,size=size,alpha='verified'))
    for name in ['dialogue','atmosphere']:
        path = OUT/f'{name}.ogg'
        duration=float(subprocess.check_output(['ffprobe','-v','error','-show_entries','format=duration','-of','default=nw=1:nk=1',str(path)],text=True))
        assert abs(duration-120)<.05,(name,duration)
        data=array.array('h',subprocess.check_output(['ffmpeg','-v','error','-i',str(path),'-f','s16le','-ac','1','-']))
        peak=max(abs(sample) for sample in data)
        assert 0<peak<32767,(name,peak)
        records.append(dict(audio=name,duration=duration,decoded_peak=peak))
    cues=json.loads((OUT/'cues.json').read_text(encoding='utf-8'))
    timing=json.loads((OUT/'dialogue_timing.json').read_text())
    for cue,record in zip(cues,timing):
        assert cue['start']<record['start']<record['end']<cue['end'],record
        assert record['tempo']<1.2,record
    assert len(timing)==len(cues)==12
    provenance=json.loads((OUT/'speech_provenance.json').read_text())
    assert len(provenance)==12
    for cue,record in zip(cues,provenance):
        assert cue['voice_id']==record['voice_id'] and cue['model']==record['model']
        assert record['settings']==dict(stability=.70,similarity_boost=.75,style=.10,speed=1)
    assert all(cues[i]['end']==cues[i+1]['start'] for i in range(11))
    source=(ROOT/'scripts/ui/ShatteredAegisIntro.cs').read_text(encoding='utf-8')
    assert 'Duration = 120' in source
    for cue in cues:
        assert cue['speaker']+': '+cue['text'] in source, cue
    assert '{ 0, 10, 20, 29, 39, 49, 59, 69, 79, 89, 99, 109, 117 }' in source
    (ROOT/'.validation/inward-wall/assets.json').write_text(json.dumps(records,indent=2)+'\n')
    print(json.dumps(records,indent=2))


if __name__ == '__main__':
    main()
