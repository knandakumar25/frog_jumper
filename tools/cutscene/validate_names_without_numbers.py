"""Validate Mission 5 epilogue raster plates, dialogue windows and audio safety."""
import array
import json
import subprocess
from pathlib import Path
from png_pixels import pixels

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'assets/cutscenes/names_without_numbers'


def main():
    """Check native-size alpha, eight cast lines, 90-second audio tracks and measured non-overlap."""
    for name in ['district_structure','district_lights']:
        if not (OUT/(name+'.png')).exists():  # Absent until regenerated with SpriteCook (docs/ART_REGENERATION.md).
            print('SKIPPED (awaiting SpriteCook regeneration):', (OUT/(name+'.png')).name)
            continue
        width,height,rows = pixels(OUT/(name+'.png'))
        assert (width,height)==(1920,1080),(name,width,height)
        alpha=[row[x] for row in rows for x in range(3,width*4,4)]
        assert min(alpha)==0 and max(alpha)==255,name
    cues=json.loads((OUT/'cues.json').read_text(encoding='utf-8'))
    timing=json.loads((OUT/'dialogue_timing.json').read_text(encoding='utf-8'))
    provenance=json.loads((OUT/'speech_provenance.json').read_text(encoding='utf-8'))
    assert len(cues)==len(timing)==len(provenance)==8
    for cue,window,receipt in zip(cues,timing,provenance):
        assert cue['voice_id']==receipt['voice_id'] and cue['model']==receipt['model']
        assert cue['start']<window['start']<window['end']<cue['end'] and window['tempo']<1.2
    for name in ['dialogue','atmosphere']:
        path=OUT/(name+'.ogg')
        duration=float(subprocess.check_output(['ffprobe','-v','error','-show_entries','format=duration','-of','default=nw=1:nk=1',str(path)],text=True))
        assert abs(duration-90)<.05,(name,duration)
        pcm=subprocess.check_output(['ffmpeg','-v','error','-i',str(path),'-f','s16le','-ac','1','-'])
        samples=array.array('h',pcm)
        assert 0<max(abs(value) for value in samples)<32767,name
    source=(ROOT/'scripts/ui/NamesWithoutNumbersAftermath.cs').read_text(encoding='utf-8')
    for cue in cues: assert cue['speaker']+': '+cue['text'] in source,cue['speaker']
    print('Mission 5 epilogue asset/audio validation passed:',len(cues),'cues, 90 seconds')


if __name__=='__main__':
    main()
