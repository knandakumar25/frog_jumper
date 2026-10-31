"""Original navy/cyan/amber Neon Rust sprite family, rendered as Blender emission meshes inside Docker.

Use --preview before the production family. All drawings use 512px orthographic RGBA canvases;
collision remains separate C# geometry. No external art, paid voice or generated music is used.
"""
import json
import math
import sys
from pathlib import Path

import bpy

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools/gameplay'))
import render_wildfire as shared
art = shared.art
INK, STEEL, EDGE = (.018,.035,.065), (.085,.15,.22), (.20,.30,.38)
CYAN, AMBER, RUST = (.20,.78,.91), (.92,.56,.19), (.35,.18,.12)
OUT = ROOT / 'assets/environment/neon_rust'
REVIEW = ROOT / '.validation/neon-rust-gameplay'


def rect(x, y, w, h, color):
    """Append a palette-controlled emission quad to the active mesh batch."""
    art.rect('structure',x,y,w,h,color)


def freight():
    """Broad cargo silhouette with amber doors, cyan tracking strips, recessed wheels and ribbed armor."""
    rect(24,148,464,290,INK)
    rect(34,160,444,257,STEEL)
    for x in range(48,468,48):
        rect(x,169,8,237,EDGE)
    rect(150,177,210,225,AMBER)
    for x in [159,254]:
        rect(x,186,95,206,RUST)
        rect(x+8,194,78,188,(.48,.28,.12))
        rect(x+65,270,9,43,AMBER)
    rect(34,156,444,7,CYAN)
    for x in [80,384]:
        rect(x,427,54,40,INK)
        rect(x+7,434,40,26,EDGE)
    rect(24,411,464,16,EDGE)
    for x in [48,410]:
        rect(x,364,48,16,AMBER)


def depot():
    """Distant surveillance depot silhouette with gantry lattice and narrow cool windows, quieter than gameplay props."""
    for x,h in [(24,260),(124,350),(251,300),(379,400)]:
        rect(x,480-h,108,h,INK)
        rect(x+8,488-h,92,h-16,(.04,.08,.13))
        for y in range(500-h,456,44):
            rect(x+20,y,64,7,(.08,.28,.36))
    art.line('structure',[(44,201),(448,201)],EDGE,13)
    art.line('structure',[(70,201),(70,480)],STEEL,14)
    art.line('structure',[(440,201),(440,480)],STEEL,14)
    for x in range(80,422,76):
        art.line('structure',[(x,201),(x+38,231),(x+76,201)],STEEL,5)
    rect(30,472,452,15,STEEL)


def machinery():
    """Cooling machinery with three amber pressure cylinders and cyan conduits."""
    rect(32,443,448,42,INK)
    rect(53,287,406,155,STEEL)
    for x in [76,205,334]:
        rect(x,111,103,306,INK)
        rect(x+9,123,85,280,EDGE)
        rect(x+19,145,65,225,STEEL)
        for y in [166,246,326]: rect(x+14,y,75,13,AMBER)
        rect(x+37,66,30,53,EDGE)
    art.line('structure',[(48,403),(464,403)],CYAN,8)
    rect(180,420,152,35,INK)
    for x in [193,237,281]: rect(x,427,27,11,CYAN)


def support():
    """Structural tower lattice designed for background placement with a visible cyan surveillance node."""
    for x in [60,416]: rect(x,28,36,456,INK)
    for y in [44,170,296]:
        rect(62,y,390,15,EDGE)
        art.line('structure',[(96,y+15),(416,y+126)],STEEL,12)
        art.line('structure',[(416,y+15),(96,y+126)],STEEL,12)
    rect(62,422,390,15,EDGE)
    rect(210,70,92,50,INK)
    rect(220,81,72,13,CYAN)
    rect(41,470,430,18,EDGE)


def coupler():
    """Twin-feed power coupler with a bright amber core and cyan armored socket."""
    art.poly('structure',[(90,474),(52,384),(52,152),(142,62),(370,62),(460,152),(460,384),(422,474)],INK)
    rect(98,131,316,300,STEEL)
    for x in [116,344]:
        rect(x,153,52,247,EDGE)
        rect(x+14,169,24,215,CYAN)
    rect(193,169,126,225,INK)
    for y in range(189,373,45): rect(209,y,94,22,AMBER)
    rect(170,425,172,23,EDGE)


def sonic():
    """Harmonic receiver's concentric cyan rings identify Aura's targets without baking any text."""
    rect(66,43,380,432,INK)
    rect(82,60,348,399,STEEL)
    for radius in [60,98,137]:
        points=[(256+radius*math.cos(i*math.tau/48),256+radius*math.sin(i*math.tau/48)) for i in range(49)]
        art.line('structure',points,CYAN,8)
    rect(243,221,26,70,AMBER)
    for x in [100,391]: rect(x,80,21,43,EDGE)
    rect(178,427,156,12,CYAN)


def laser():
    """Paired laser emitter fixtures; code supplies the beam so art never controls damage/collision."""
    for y in [45,371]:
        rect(130,y,252,96,INK)
        rect(144,y+12,224,70,STEEL)
        rect(201,y+28,110,44,EDGE)
        rect(221,y+35,70,22,CYAN)
        for x in [158,336]: rect(x,y+22,17,46,AMBER)


def main():
    """Render the representative freight target or the complete reproducible seven-member art family."""
    art.W=art.H=512
    art.BATCHES.clear()
    art.ORDER=0
    if '--preview' in sys.argv:
        freight()
        shared.render(REVIEW/'art-target.png')
        return
    manifest={'generator':'tools/gameplay/render_neon_rust.py','source':'Original project-authored Blender planar emission meshes',
              'license':'Same terms as project source','native_size':[512,512],'view':'orthographic side',
              'filter':'linear','mipmaps':False,'pivot':'center; C# explicit size and offset','assets':[]}
    for name,draw in [('depot',depot),('freight',freight),('machinery',machinery),('support',support),('coupler',coupler),('sonic',sonic),('laser',laser)]:
        art.BATCHES.clear()
        art.ORDER=0
        draw()
        shared.render(OUT/f'{name}.png')
        manifest['assets'].append({'file':f'{name}.png','rgba':True,'collision':'none; authored separately in NeonRustMission'})
    (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')


if __name__=='__main__':
    main()
