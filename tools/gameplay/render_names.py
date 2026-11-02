"""Render original surveillance-district props in the established Docker/Blender pipeline; collision is authored separately."""
import json
import sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'tools/gameplay'))
import render_wildfire as shared
art=shared.art
OUT=ROOT/'assets/environment/names'
INK=(.025,.04,.065)
GLASS=(.08,.13,.18)
STEEL=(.22,.30,.35)
WHITE=(.87,.94,.96)
AMBER=(.92,.62,.28)
ACCEPT=(.49,.78,.68)


def rect(x,y,w,h,color):
    """Add one orthographic emission mesh to the district palette."""
    art.rect('registry',x,y,w,h,color)


def district():
    """Dark glass facades with white scanner strips and sparse warm inhabited windows."""
    for x,h in [(25,400),(145,450),(265,370),(385,420)]:
        rect(x,490-h,100,h,INK)
        rect(x+8,500-h,84,h-20,GLASS)
        for y in range(510-h,460,46):
            rect(x+12,y,76,3,STEEL)
            rect(x+18,y+8,18,22,(.36,.29,.21) if y%3==0 else (.10,.19,.24))
        rect(x+84,500-h,5,h-30,WHITE)
    rect(25,465,460,25,STEEL)


def scanner():
    """Two narrow dark pillars frame the white emitter; runtime draws timed warning and active beams."""
    for x in [80,390]:
        rect(x,30,40,450,INK);rect(x+10,50,12,410,STEEL)
    rect(80,30,350,50,GLASS);rect(120,58,270,9,WHITE)
    rect(70,472,370,12,AMBER)


def archive():
    """Protected original record drawers separated from their coolant equipment."""
    rect(40,45,430,440,INK)
    for x in [60,205,350]:
        rect(x,65,120,400,GLASS)
        for y in range(85,440,60):
            rect(x+8,y,104,44,STEEL);rect(x+36,y+12,48,7,WHITE)
    rect(47,471,410,9,AMBER)


def terminal():
    """Human-scale authentication console with a bright protected glass display."""
    rect(130,160,250,320,INK);rect(150,180,210,160,STEEL)
    rect(165,195,180,120,GLASS)
    for y in [215,245,275]:rect(185,y,135,6,WHITE)
    rect(160,365,190,18,AMBER);rect(110,470,290,16,STEEL)


def access(restored=False):
    """Matching denied/accepted access states retain identical footprints and readable direction markings."""
    color=ACCEPT if restored else AMBER
    for x in [55,405]:rect(x,35,50,450,INK);rect(x+12,55,12,400,color)
    rect(55,35,400,45,GLASS);rect(118,48,270,12,color)
    if not restored:
        for x in range(110,400,42):rect(x,130,7,330,AMBER)
    else:
        art.line('registry',[(205,270),(240,310),(315,215)],ACCEPT,13)
    rect(40,475,430,12,color)


def main():
    """Generate transparent 512px assets and provenance, or a representative palette target for inspection."""
    art.W=art.H=512
    family=[('district',district),('scanner',scanner),('archive',archive),('terminal',terminal),
            ('access_denied',access),('access_restored',lambda:access(True)),('civilians',shared.civilians)]
    if '--preview' in sys.argv:family=[('art-target',district)]
    manifest=dict(generator='tools/gameplay/render_names.py',source='Original project-authored Blender emission meshes',native_size=[512,512],view='orthographic side',collision='separate C# blockout',assets=[])
    for name,draw in family:
        art.BATCHES.clear();art.ORDER=0;draw()
        shared.render((ROOT/'.validation/names' if '--preview' in sys.argv else OUT)/(name+'.png'))
        manifest['assets'].append(dict(file=name+'.png',rgba=True))
    if '--preview' not in sys.argv:(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')


if __name__=='__main__':main()
