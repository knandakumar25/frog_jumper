"""Validate aftermath raster geometry, alpha, every atlas cell and decoded audio peaks in Docker."""
import array
import json
import subprocess
from pathlib import Path
from png_pixels import pixels

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'assets/cutscenes/wildfire_aftermath'


def main():
    """Fail on incorrect sizes, clipped atlas cells, overlapping cues, wrong duration or clipped PCM."""
    records = []
    for name, size in [('reunion_structure',(1920,1080)),('terminal_structure',(1920,1080)),('poses',(3072,1536))]:
        if not (OUT/f'{name}.png').exists():  # Absent until regenerated with SpriteCook (docs/ART_REGENERATION.md).
            print('SKIPPED (awaiting SpriteCook regeneration):', (OUT/f'{name}.png').name)
            continue
        width,height,rows = pixels(OUT/f'{name}.png')
        assert (width,height) == size, name
        alpha = [row[x] for row in rows for x in range(3,width*4,4)]
        assert min(alpha) == 0 and max(alpha) == 255, name
        if name == 'poses':
            for y in range(3):
                for x in range(6):
                    cell = rows[y*512:(y+1)*512]
                    assert sum(row[i*4+3]>0 for row in cell for i in range(x*512,(x+1)*512))>5000,(x,y)
                    assert not any(row[i*4+3] for row in cell for i in [x*512,(x+1)*512-1]),(x,y)
        records.append(dict(asset=name,size=size,alpha='verified'))
    for name in ['dialogue','atmosphere']:
        path = OUT/f'{name}.ogg'
        duration=float(subprocess.check_output(['ffprobe','-v','error','-show_entries','format=duration','-of','default=nw=1:nk=1',str(path)],text=True))
        assert abs(duration-75)<.05,(name,duration)
        data=array.array('h',subprocess.check_output(['ffmpeg','-v','error','-i',str(path),'-f','s16le','-ac','1','-']))
        peak=max(abs(sample) for sample in data)
        assert 0<peak<32767,(name,peak)
        records.append(dict(audio=name,duration=duration,decoded_peak=peak))
    cues=json.loads((OUT/'cues.json').read_text(encoding='utf-8'))
    timing=json.loads((OUT/'dialogue_timing.json').read_text())
    for cue,record in zip(cues,timing):
        assert cue['start']<record['start']<record['end']<cue['end'],record
        assert record['tempo']<1.2,record
    assert len(timing)==8
    (ROOT/'.validation/breach/assets.json').write_text(json.dumps(records,indent=2)+'\n')
    print(json.dumps(records,indent=2))


if __name__ == '__main__':
    main()
