"""Produce original reunion, terminal and three-operative pose family in Docker Blender.

Reuses the project's navy/cyan planar emission geometry and character silhouettes.
Run: docker compose run --rm --entrypoint blender cutscene --background --python tools/cutscene/render_breach.py
"""
import inspect
import json
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from render_wildfire import city, character_library

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'assets/cutscenes/wildfire_aftermath'
PREVIEW = ROOT / '.validation/breach'


def reunion():
    """Render reunited adults and children holding hands on a clear elevated service road."""
    city.BATCHES.clear()
    city.ORDER = 0
    city.poly('structure', [(0,840),(1550,840),(1920,700),(1920,750),(1550,890),(0,890)],(.05,.10,.14))
    city.line('structure',[(10,832),(1550,832),(1920,692)],(.23,.41,.46),6)
    for group, x in enumerate([130,370,1460]):
        for dx, scale in [(0,1),(55,.65),(110,1)]:
            cx, feet = x+dx, 820
            city.poly('structure',[(cx-12*scale,feet),(cx-18*scale,feet-65*scale),(cx,feet-85*scale),(cx+18*scale,feet-65*scale),(cx+12*scale,feet)],(.14+group*.02,.23,.29))
            city.rect('structure',cx-9*scale,feet-109*scale,18*scale,23*scale,(.65,.53,.43))
            city.line('structure',[(cx-14*scale,feet-66*scale),(cx-28*scale,feet-43*scale)],(.33,.45,.51),6*scale)
            city.line('structure',[(cx+14*scale,feet-66*scale),(cx+28*scale,feet-43*scale)],(.33,.45,.51),6*scale)
            city.line('lights',[(cx-12*scale,feet-62*scale),(cx+10*scale,feet-62*scale)],(.45,.69,.66),3)
        city.line('structure',[(x+25,777),(x+37,788),(x+55,792),(x+88,777)],(.49,.61,.61),5)
    city.render('reunion',OUT,PREVIEW)


def terminal():
    """Render a recovered console and freight depot silhouette beneath the live manifest projection."""
    city.BATCHES.clear()
    city.ORDER = 0
    city.poly('structure',[(540,810),(570,660),(755,660),(780,810)],(.08,.14,.19))
    city.poly('structure',[(555,670),(575,585),(775,585),(755,670)],(.12,.27,.31))
    city.line('structure',[(585,607),(750,607)],(.28,.83,.88),5)
    for x in range(1050,1850,160):
        city.rect('structure',x,480,120,100,(.04,.085,.12))
        city.line('lights',[(x+15,510),(x+105,510)],(.57,.39,.2),3)
    city.render('terminal',OUT,PREVIEW)


def main():
    """Render coherent transparent art and a 6x3 512-pixel atlas with four walking frames."""
    OUT.mkdir(parents=True,exist_ok=True)
    PREVIEW.mkdir(parents=True,exist_ok=True)
    reunion()
    terminal()
    lib = character_library()
    # Alter only the aftermath library: sheath Kaito's blade and give Titan violet highlights.
    source = inspect.getsource(lib['character'])
    source = source.replace('length=47 if kind=="cyber_shogun" else 29','length=3 if kind=="cyber_shogun" else 29')
    source = source.replace('hand=(shoulder[0]+32,120)',
        'hand=(shoulder[0]+24,226) if action == "support" else (shoulder[0]+50,140) if action == "mark" else (shoulder[0]+32,120)')
    exec(source,lib)
    lib['DESIGNS']['apex_titan'] = ('#67768b','#bf91f2','#303749',1.35)
    lib['clear']()
    for row, kind in enumerate(['cyber_shogun','oceanus','apex_titan']):
        for col in range(6):
            action = 'running' if col >= 2 else 'support' if row == 2 and col == 1 else 'mark' if row == 1 and col == 1 else 'idle'
            # Blender's Y axis points up; invert row placement to match PNG top-left atlas addressing.
            lib['character'](kind,action,(col-2)*2 if col >= 2 else 2,
                offset=(col*512+256,(2-row)*512+64),scale=1.8)
    lib['render'](OUT/'poses.png',3072,1536)
    manifest = dict(generator='Blender 4.3.2 / Docker Python; original project planar geometry',
        scenery=[1920,1080],atlas=[3072,1536],cell=[512,512],
        rows=['Kaito','Oceanus','Titan'],columns=['calm','gesture/support','walk0','walk2','walk4','walk6'],
        palette='navy/cyan checkpoint; amber fire; red/gold Kaito; hydro Oceanus; violet Titan',
        reused=['wildfire/checkpoint_structure.png','wildfire/siege_structure.png','deployment/live/city_base.png'],
        voices='ElevenLabs multilingual v2: Daniel AXIOM-7; Adam Kaito; Sarah Oceanus; George Titan')
    (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')


if __name__ == '__main__':
    main()
