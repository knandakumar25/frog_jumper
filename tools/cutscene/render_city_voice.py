"""Render original aftermath scenery and five-operative poses with Docker Blender.

Planar emission matches Silent Freight. Preview renders one representative squad
composition before the complete 6-column, 5-row family. Cells have a shared foot pivot.
"""
import inspect
import json
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from render_wildfire import city, character_library

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'assets/cutscenes/neon_rust_aftermath'
PREVIEW = ROOT/'.validation/city-voice'
KINDS = ['cyber_shogun', 'oceanus', 'apex_titan', 'phoenicia', 'mirage_blade']


def library():
    """Keep existing silhouettes and restore story palette while authoring noncombat gestures."""
    lib = character_library()
    source = inspect.getsource(lib['character'])
    source = source.replace('length=47 if kind=="cyber_shogun" else 29', 'length=3 if kind=="cyber_shogun" else 29')
    source = source.replace('hand=(shoulder[0]+32,120)', 'hand=(shoulder[0]+48,160) if action == "gesture" else (shoulder[0]+32,120)')
    exec(source, lib)
    lib['DESIGNS']['apex_titan'] = ('#67768b', '#bf91f2', '#303749', 1.35)
    lib['DESIGNS']['mirage_blade'] = ('#39b986', '#a8f5c6', '#223e42', .87)
    return lib


def fortress():
    """Author distant independent fortress defenses; no arrival, new recruit or named antagonist appears."""
    city.BATCHES.clear()
    city.ORDER = 0
    city.poly('structure', [(0,740),(0,670),(900,670),(1100,460),(1920,460),(1920,840),(0,840)], (.035,.065,.095))
    for x in range(1080, 1920, 120):
        city.rect('structure',x,390,85,220,(.08,.12,.16))
        city.rect('structure',x+8,368,70,28,(.14,.19,.22))
        city.line('structure',[(x+40,375),(x+5,335)],(.22,.27,.3),13)
        city.rect('lights',x+14,430,42,5,(.95,.43,.21))
    city.poly('structure',[(0,805),(1000,805),(1690,590),(1750,620),(1020,865),(0,865)],(.13,.19,.23))
    city.line('lights',[(0,807),(1000,807),(1690,592)],(.25,.58,.58),4)
    city.render('fortress',OUT,PREVIEW)
    (OUT/'fortress_base.png').unlink(missing_ok=True)


def main():
    """Render preview first or the complete 30-cell atlas, distant road and provenance manifest."""
    OUT.mkdir(parents=True,exist_ok=True)
    PREVIEW.mkdir(parents=True,exist_ok=True)
    lib = library()
    lib['clear']()
    if '--preview' in sys.argv:
        for row, kind in enumerate(KINDS):
            lib['character'](kind,'gesture',2,offset=(240+row*320,130),scale=2.4)
        lib['render'](PREVIEW/'squad.png',1920,1080)
        return
    for row,kind in enumerate(KINDS):
        for col in range(6):
            lib['character'](kind,'running' if col>=2 else 'gesture' if col==1 else 'idle',
                (col-2)*2 if col>=2 else 2,offset=(col*512+256,(4-row)*512+64),scale=1.8)
    lib['render'](OUT/'poses.png',3072,2560)
    fortress()
    (OUT/'manifest.json').write_text(json.dumps(dict(
        generator='Docker Blender 4.3.2; original project planar emission geometry',
        scenery=[1920,1080],atlas=[3072,2560],cell=[512,512],rows=KINDS,
        columns=['calm','gesture','walk0','walk2','walk4','walk6'],
        palette='red/gold Kaito; hydro cyan Oceanus; violet Titan; prism magenta Aura; emerald Mirage',
        reused=['neon_rust_intro/depot_structure.png','neon_rust_intro/depot_lights.png','neon_rust_intro/cargo_structure.png','neon_rust_intro/cargo_lights.png','deployment/live/city_base.png'],
        rights='Original project geometry; no third-party stock imagery or sampled atmosphere',
        audio='Ten ElevenLabs multilingual_v2 cues, exact text and voice IDs in cues.json; original seeded synthesis',
        source='tools/cutscene/render_city_voice.py; render_wildfire.character_library'),indent=2)+'\n')


if __name__ == '__main__':
    main()
