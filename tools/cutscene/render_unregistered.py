"""Render original layered surveillance-district artwork with the established Docker Blender library.

Native frame is 1920x1080 RGBA. White scanner geometry, dark glass and amber
denial lights share one family; all localized text is supplied by Godot.
"""
import json
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from render_wildfire import city

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'assets/cutscenes/act_ii_opening'
PREVIEW = ROOT / '.validation/unregistered'


def scenery(name):
    """Build the public queue, scanner insert, registry tower or hidden service corridor."""
    city.BATCHES.clear()
    city.ORDER = 0
    city.rect('base', 0, 0, 1920, 1080, (.018, .028, .043))
    for x in range(-50, 1920, 250):
        city.poly('structure', [(x,800),(x,180),(x+70,105),(x+220,140),(x+220,800)], (.055,.08,.11))
        for y in range(230,760,65):
            city.rect('structure', x+20,y,175,32,(.08,.115,.15))
            city.line('lights',[(x+25,y),(x+190,y)],(.3,.38,.42),2)
    city.poly('structure',[(0,800),(1920,800),(1920,1080),(0,1080)],(.07,.09,.115))
    for x in range(-500,2500,230):
        city.line('structure',[(960,730),(x,1080)],(.16,.19,.21),2)
    if name in ('queue','scanner'):
        for x in [750,1110,1470]:
            city.rect('structure',x,280,38,520,(.23,.27,.3))
            city.rect('structure',x+260,280,38,520,(.23,.27,.3))
            city.rect('structure',x,270,298,40,(.3,.34,.37))
            city.rect('lights',x+42,309,214,6,(.88,.95,1))
            city.rect('structure',x+60,650,180,150,(.04,.06,.08))
            city.rect('lights',x+83,670,134,9,(1,.59,.18))
            city.line('lights',[(x+105,720),(x+155,770)],(1,.55,.14),5)
            city.line('lights',[(x+155,720),(x+105,770)],(1,.55,.14),5)
        for y in [850,940]:
            city.line('structure',[(100,y),(700,y-100)],(.29,.33,.35),7)
        for x in [180,380,580]:
            city.rect('structure',x,735,12,140,(.28,.32,.34))
        for x in [550,1350]:
            city.line('structure',[(x,150),(x+70,180)],(.38,.42,.44),16)
            city.poly('structure',[(x+50,160),(x+105,172),(x+95,202),(x+45,191)],(.16,.20,.24))
            city.rect('lights',x+78,184,12,6,(1,.57,.16))
    elif name == 'registry':
        city.poly('structure',[(760,800),(800,150),(960,65),(1120,150),(1160,800)],(.12,.17,.22))
        for y in range(180,750,42):
            city.rect('lights',825,y,270,3,(.68,.78,.82))
        for x in [200,480,1440,1730]:
            city.line('lights',[(960,300),(x,530),(x,800)],(.43,.64,.69),3)
            city.rect('structure',x-60,650,120,150,(.13,.18,.22))
            city.rect('lights',x-40,670,80,7,(1,.58,.16))
    else:
        city.poly('structure',[(0,230),(1450,230),(1790,430),(1790,800),(1450,900),(0,900)],(.035,.052,.07))
        for x in range(120,1700,210):
            city.line('structure',[(x,300),(x,810)],(.2,.25,.28),10)
            city.line('lights',[(x+15,330),(x+15,460)],(.7,.79,.84),4)
        city.rect('structure',1390,500,260,310,(.009,.018,.027))
        city.line('lights',[(1390,810),(1390,500),(1650,500)],(.82,.60,.32),4)
    city.render(name,OUT,PREVIEW)


def civilians():
    """Make a separate warm civilian layer, including the rejected adult and child at the scanner."""
    city.BATCHES.clear()
    city.ORDER = 0
    for x,y,s in [(160,820,1),(270,825,.78),(410,800,1),(520,805,.72),(670,790,1),(930,790,1),(1000,805,.65)]:
        city.poly('structure',[(x-20*s,y),(x-26*s,y-95*s),(x,y-120*s),(x+26*s,y-95*s),(x+20*s,y)],(.22,.23,.23))
        city.rect('structure',x-12*s,y-153*s,24*s,29*s,(.53,.42,.32))
        city.line('lights',[(x-18*s,y-100*s),(x+15*s,y-97*s)],(.68,.52,.32),4)
    city.render('civilians',OUT,PREVIEW)
    (OUT/'civilians_base.png').unlink(missing_ok=True)


def main():
    """Render a representative queue before the full coherent family and reproducible provenance."""
    OUT.mkdir(parents=True,exist_ok=True)
    PREVIEW.mkdir(parents=True,exist_ok=True)
    scenery('queue')
    if '--preview' in sys.argv:
        return
    for name in ['scanner','registry','service']:
        scenery(name)
    civilians()
    (OUT/'manifest.json').write_text(json.dumps(dict(
        generator='Original planar Blender emission geometry in axiom-force-cutscene Docker image',
        source='tools/cutscene/render_unregistered.py; render_wildfire.city',
        dimensions=[1920,1080], layers=['base','structure','lights'],
        scenes=['queue','scanner','registry','service'], civilians='Separate transparent structure/lights',
        reused=['neon_rust_aftermath/poses.png','shattered_aegis_intro/surya.png'],
        palette='Dark glass, white scanning lights, amber denial, restrained civilian warmth; established operative designs',
        rights='Original project geometry; no stock images or audio',
        text='No cinematic text baked into artwork',
        audio='Existing cast; new requests only; cues, speech_provenance and dialogue_timing accompany tracks'
    ),indent=2)+'\n')


if __name__ == '__main__':
    main()
