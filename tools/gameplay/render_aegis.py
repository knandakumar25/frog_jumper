"""Original fortress gameplay family: navy armor, restrained violet, amber live systems and cyan safe systems.

Run in the established Docker/Blender image. Inspect --preview before producing the family.
All 512px RGBA side-view drawings are noncolliding emission meshes; operative and Voss art is preserved.
"""
import json
import sys
from pathlib import Path
import bpy
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'tools/gameplay'))
import render_wildfire as shared
art=shared.art
OUT=ROOT/'assets/environment/shattered_aegis'
REVIEW=ROOT/'.validation/aegis'
INK=(.025,.035,.07)
STEEL=(.14,.17,.25)
EDGE=(.32,.34,.45)
AMBER=(1,.60,.24)
CYAN=(.35,.85,.9)


def rect(x,y,w,h,color):
    """Append one emission quad in the shared fortress palette."""
    art.rect('fortress',x,y,w,h,color)


def fortress():
    """Heavy inward-facing curtain wall with power spines and recessed human-scale windows."""
    for x,h in [(24,310),(146,400),(270,350),(392,430)]:
        rect(x,480-h,96,h,INK)
        rect(x+8,490-h,80,h-20,STEEL)
        for y in range(510-h,450,48):rect(x+20,y,56,8,(.10,.24,.33))
        rect(x+12,484-h,72,10,EDGE)
    rect(24,400,464,78,INK)
    rect(28,409,456,13,EDGE)
    for x in range(40,470,44):rect(x,432,22,8,AMBER)


def gun():
    """Massive left-pointing armored gun on an independent power pedestal; runtime dims it after shutdown."""
    rect(190,280,145,200,INK)
    rect(207,300,110,170,STEEL)
    for x in [222,286]:rect(x,316,12,105,AMBER)
    art.poly('fortress',[(178,165),(235,117),(377,117),(440,195),(419,288),(193,288)],INK)
    rect(204,174,210,87,STEEL)
    rect(31,183,208,62,INK)
    rect(42,192,192,43,EDGE)
    rect(27,176,28,77,STEEL)
    rect(34,187,10,54,AMBER)
    rect(264,139,82,12,AMBER)
    rect(162,470,206,18,EDGE)


def coolant():
    """Twin thermal exchangers with an unmistakable cyan intake between amber pressure sleeves."""
    rect(45,400,422,80,INK)
    for x in [65,320]:
        rect(x,95,125,320,INK)
        rect(x+12,110,101,290,STEEL)
        for y in [142,230,318]:rect(x+8,y,109,14,AMBER)
    rect(208,160,96,270,EDGE)
    for y in range(180,400,45):rect(220,y,72,21,CYAN)
    art.line('fortress',[(32,432),(480,432)],CYAN,10)


def controller():
    """Local harmonic controller and guarded release socket; code supplies interactive state and labels."""
    rect(90,42,332,442,INK)
    rect(106,60,300,404,STEEL)
    for radius in [62,100,138]:
        import math
        art.line('fortress',[(256+radius*math.cos(i*math.tau/48),232+radius*math.sin(i*math.tau/48)) for i in range(49)],CYAN,7)
    rect(230,200,52,64,AMBER)
    for x in [135,222,309]:rect(x,415,60,15,EDGE)


def crossing():
    """Tall safe gateway with opened leaves and cyan wayfinding; leaves never imply gameplay collision."""
    for x in [43,398]:
        rect(x,32,71,448,INK)
        rect(x+12,48,47,420,STEEL)
        rect(x+20,66,11,370,CYAN)
    rect(43,32,426,53,INK)
    rect(107,47,291,12,CYAN)
    rect(26,473,460,17,EDGE)


def composition():
    """Preview one gun emplacement against the fortress and safe crossing at gameplay-relative scale."""
    art.W,art.H=1280,720
    rect(0,0,1280,720,(.035,.045,.09))
    rect(0,610,1280,110,STEEL)
    original=art.poly
    for draw,x,y,scale in [(fortress,0,70,1.1),(gun,360,220,.8),(coolant,720,290,.65),(crossing,960,180,.9)]:
        def transformed(layer,points,color):
            """Transform one original mesh family member into the representative scene."""
            original(layer,[(x+px*scale,y+py*scale) for px,py in points],color)
        art.poly=transformed
        draw()
    art.poly=original
    shared.render(REVIEW/'art-target.png',1280,720)


def main():
    """Render the approved target or family and record reproducible provenance and technical metadata."""
    art.BATCHES.clear();art.ORDER=0
    if '--preview' in sys.argv:
        composition()
        return
    art.W=art.H=512
    manifest=dict(generator='tools/gameplay/render_aegis.py',source='Original project-authored Blender emission meshes',license='Same terms as project source',native_size=[512,512],view='orthographic side',filter='linear',pivot='center; explicit C# size/offset',assets=[])
    for name,draw in [('fortress',fortress),('gun',gun),('coolant',coolant),('controller',controller),('crossing',crossing),('civilians',shared.civilians)]:
        art.BATCHES.clear();art.ORDER=0
        draw();shared.render(OUT/(name+'.png'))
        manifest['assets'].append(dict(file=name+'.png',rgba=True,collision='none; separate authored C#'))
    (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')


if __name__=='__main__':main()
