"""Render original Silent Freight layers and only missing poses using Docker Blender.

Uses the established planar emission art and character geometry. Scenery is transparent
1920x1080, new poses are a 3x3 atlas with 512px cells; existing walks are reused at runtime.
"""
import inspect
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from render_wildfire import city, character_library

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'assets/cutscenes/neon_rust_intro'
PREVIEW = ROOT / '.validation/neon-rust'


def reset():
    """Clear planar batches between independent layers without changing shared authoring code."""
    city.BATCHES.clear()
    city.ORDER = 0


def depot():
    """Build a distant industrial district, depot hangars and the patrol communication tower."""
    reset()
    for x, roof in [(30, 370), (340, 320), (680, 405), (1000, 350), (1620, 365)]:
        city.poly('structure', [(x, 660), (x, roof), (x+90, roof-40), (x+240, roof), (x+240, 660)], (.045, .085, .12))
        for y in range(roof+35, 630, 55):
            for dx in [35, 105, 175]:
                city.rect('lights', x+dx, y, 32, 6, (.17, .37, .43))
        city.line('structure', [(x+40, roof), (x+40, roof-65)], (.12, .2, .25), 10)
    city.poly('structure', [(1335, 650), (1380, 230), (1480, 230), (1535, 650)], (.07, .14, .19))
    for y in range(260, 650, 65):
        city.line('structure', [(1380, y), (1490, y+55), (1380, y+55)], (.18, .27, .31), 6)
    city.rect('structure', 1355, 205, 155, 45, (.16, .26, .3))
    city.line('structure', [(1430, 210), (1430, 100)], (.23, .35, .4), 8)
    city.line('lights', [(1380, 220), (1485, 220)], (.25, .84, .91), 7)
    city.rect('lights', 1424, 95, 12, 18, (.95, .3, .12))
    city.render('depot', OUT, PREVIEW)


def underpass():
    """Author a rail deck with occluding supports and pooled runoff beneath surveillance."""
    reset()
    city.rect('structure', 0, 515, 1920, 55, (.08, .14, .18))
    city.line('structure', [(0, 515), (1920, 515)], (.26, .35, .39), 6)
    for x in range(60, 1920, 340):
        city.rect('structure', x, 560, 35, 235, (.055, .105, .14))
        city.line('structure', [(x, 570), (x+150, 655), (x+300, 570)], (.12, .2, .24), 10)
    city.rect('structure', 0, 805, 1920, 275, (.025, .055, .08))
    for x in range(30, 1850, 115):
        city.line('lights', [(x, 825), (x+75, 825)], (.08, .32, .4), 3)
    city.render('underpass', OUT, PREVIEW)


def cargo():
    """Render warm moving energy freight with the siege battery's amber bars and triangular marking."""
    reset()
    for x in range(-200, 2200, 480):
        city.rect('structure', x, 330, 340, 170, (.09, .13, .16))
        city.poly('structure', [(x, 330), (x+25, 305), (x+315, 305), (x+340, 330)], (.16, .21, .23))
        for dx in range(25, 330, 45):
            city.line('structure', [(x+dx, 345), (x+dx, 485)], (.18, .24, .27), 4)
        city.line('lights', [(x+25, 355), (x+315, 355)], (.85, .5, .19), 5)
        city.poly('lights', [(x+150, 395), (x+185, 395), (x+168, 425)], (.93, .6, .22))
        for dx in [145, 166, 187]:
            city.rect('lights', x+dx, 442, 10, 25, (.9, .53, .18))
        city.line('structure', [(x-10, 500), (x+355, 500)], (.25, .3, .32), 12)
    city.render('cargo', OUT, PREVIEW)


def walkway():
    """Build a failing service walkway and protected insertion floor without opening the scrapyard wall."""
    reset()
    city.poly('structure', [(100, 680), (880, 680), (1000, 695), (1230, 680), (1460, 680), (1460, 710), (1230, 710), (1000, 725), (880, 710), (100, 710)], (.12, .19, .23))
    for x in range(160, 1460, 170):
        city.line('structure', [(x, 680), (x, 600)], (.22, .3, .34), 6)
    city.line('structure', [(100, 610), (900, 610)], (.26, .35, .38), 5)
    city.line('structure', [(1210, 610), (1460, 610)], (.26, .35, .38), 5)
    for x in [180, 520, 1310]:
        city.line('structure', [(x, 710), (x+80, 805)], (.11, .17, .21), 18)
        city.rect('lights', x, 687, 65, 4, (.55, .46, .27))
    city.render('walkway', OUT, PREVIEW)


def poses():
    """Create concealed, support and inspection variants while retaining all established operative designs."""
    lib = character_library()
    source = inspect.getsource(lib['character'])
    source = source.replace('length=47 if kind=="cyber_shogun" else 29', 'length=3 if kind=="cyber_shogun" else 29')
    source = source.replace('hand=(shoulder[0]+32,120)',
        'hand=(shoulder[0]+10,260) if action == "brace" else (shoulder[0]+48,160) if action == "inspect" else (shoulder[0]+12,135)')
    # Compress the upper body toward the hips for concealment; boots retain the established ground anchor.
    source = source.replace('(y-32+bob)*scale', '((112+(y-112)*.70 if action == "concealed" and y > 112 else y)-32+bob)*scale')
    exec(source, lib)
    lib['DESIGNS']['apex_titan'] = ('#67768b', '#bf91f2', '#303749', 1.35)
    lib['clear']()
    for row, kind in enumerate(['cyber_shogun', 'oceanus', 'apex_titan']):
        for col, action in enumerate(['concealed', 'brace', 'inspect']):
            # Only Titan braces overhead. Oceanus's tall staff would otherwise cross the atlas row boundary.
            if col == 1 and row != 2:
                action = 'concealed'
            lib['character'](kind, action, 2, offset=(col*512+256, (2-row)*512+64), scale=1.8)
    lib['render'](OUT / 'poses.png', 1536, 1536)


def main():
    """Render the family and record exact provenance and atlas addressing for reproducible imports."""
    OUT.mkdir(parents=True, exist_ok=True)
    PREVIEW.mkdir(parents=True, exist_ok=True)
    for build in [depot, underpass, cargo, walkway]:
        build()
    # These scenes use transparent structure/lights only; do not ship empty renderer base passes.
    for name in ['depot', 'underpass', 'cargo', 'walkway']:
        (OUT / f'{name}_base.png').unlink(missing_ok=True)
    poses()
    manifest = dict(generator='Docker Blender; original project planar emission geometry',
        scenery=[1920,1080], atlas=[1536,1536], cell=[512,512],
        rows=['Kaito','Oceanus','Titan'], columns=['concealed/runoff','brace','inspect'],
        reused=['deployment/live/city_base.png','wildfire_aftermath/poses.png'],
        palette='navy steel, cold cyan surveillance, amber cargo; established red/gold, hydro and violet operatives',
        sources='tools/cutscene/render_neon_rust.py; render_wildfire.character_library; no external stock art',
        audio='Eight requested ElevenLabs multilingual-v2 cues in cues.json; original seeded industrial synthesis',
        persistence='None; play on every fresh mission 3 launch, never checkpoint recovery')
    (OUT/'manifest.json').write_text(json.dumps(manifest, indent=2)+'\n')


if __name__ == '__main__':
    main()
