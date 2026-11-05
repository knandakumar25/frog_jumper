"""Render seven original environment families and six boss plates in container Blender.

Run --targets first, inspect one representative composition per family, then --produce.
The existing planar-emission renderer preserves the campaign's illustrated art direction.
All plates use independent base, architecture and emissive layers; none supplies collision.
"""
import argparse
import hashlib
import json
import math
import shutil
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parent))
from render_wildfire import city

FAMILIES = ['cyber_grave', 'foundry_forge', 'stratos_docks', 'spire_outer_hull',
            'cyber_nexus', 'apex_bastion', 'judgment_day']
OUT = ROOT / 'assets/environments/later_campaign'
REVIEW = ROOT / '.validation/later-campaign/art'
STEEL = (.12, .18, .23)
EDGE = (.28, .39, .46)
DARK = (.035, .055, .08)
CYAN = (.17, .85, .9)
AMBER = (.96, .59, .22)


def panel(x, y, w, h, color=STEEL):
    """Draw a beveled plate with bolts, seams and a separate illuminated service strip."""
    city.poly('structure', [(x, y+12), (x+12, y), (x+w-12, y), (x+w, y+12),
                           (x+w, y+h-12), (x+w-12, y+h), (x+12, y+h), (x, y+h-12)], color)
    city.line('structure', [(x+8, y+h-12), (x+8, y+15), (x+18, y+8), (x+w-18, y+8)], EDGE, 2)
    for bx in [x+18, x+w-22]:
        for by in [y+18, y+h-22]:
            city.rect('structure', bx, by, 4, 4, (.5, .58, .61))


def pipe(points, color=CYAN, width=22):
    """Route a thick insulated conduit with a thin emissive center and repeated coupling collars."""
    city.line('structure', points, (.035, .07, .09), width+10)
    city.line('structure', points, EDGE, width)
    city.line('lights', points, color, max(2, width/6))
    for x, y in points[1:-1]:
        city.rect('structure', x-width*.7, y-8, width*1.4, 16, STEEL)


def turbine(x, y, radius, color):
    """Build a readable industrial turbine or reactor iris with segmented housing and radial vanes."""
    for r, c, width in [(radius+18, DARK, 24), (radius+7, EDGE, 10), (radius, STEEL, 10)]:
        city.arc('structure', x, y, r, 0, math.tau, c, width)
    for i in range(12):
        a = i*math.tau/12
        city.poly('structure', [(x+math.cos(a)*radius*.22, y+math.sin(a)*radius*.22),
                               (x+math.cos(a+.22)*radius*.9, y+math.sin(a+.22)*radius*.9),
                               (x+math.cos(a+.52)*radius*.8, y+math.sin(a+.52)*radius*.8)], EDGE)
    city.arc('lights', x, y, radius*.29, 0, math.tau, color, 6)
    city.arc('lights', x, y, radius+14, 0, math.tau, color, 3)


def floor():
    """Add a receding walkway with distinct leading edges and unobtrusive hazard markings."""
    city.poly('structure', [(0, 805), (1920, 805), (1920, 1080), (0, 1080)], (.065, .095, .125))
    city.line('structure', [(0, 806), (1920, 806)], EDGE, 5)
    for x in range(-200, 2100, 160):
        city.line('structure', [(x, 1080), (960+(x-960)*.7, 805)], (.12, .17, .2), 2)
    for y in [840, 900, 995]:
        city.line('structure', [(0, y), (1920, y)], (.12, .17, .2), 2)
    for x in range(40, 1900, 130):
        city.rect('lights', x, 815, 42, 3, CYAN)


def laboratory():
    """Enclosed lift shafts, stasis pods and coolant manifolds distinguish the underground Cyber-Grave."""
    city.gradient((.012, .028, .04), (.055, .15, .16))
    for x in range(-80, 2000, 260):
        panel(x, 30, 210, 740, (.07, .12, .15))
        pipe([(x+24, 60), (x+24, 220), (x+80, 280), (x+80, 700)], (.2, .64, .55), 18)
    for x in [330, 600, 1300, 1570]:
        panel(x-64, 310, 128, 400, DARK)
        city.poly('structure', [(x-44, 375), (x-30, 345), (x+30, 345), (x+44, 375),
                               (x+44, 650), (x+25, 680), (x-25, 680), (x-44, 650)], (.10, .25, .26))
        city.rect('lights', x-36, 380, 3, 245, (.27, .93, .65))
        for y in [375, 475, 610, 668]:
            city.rect('structure', x-58, y, 116, 14, EDGE)
        city.arc('lights', x, 460, 21, 0, math.tau, (.2, .6, .5), 3)
        city.poly('structure', [(x-18, 630), (x-23, 515), (x, 492), (x+23, 515), (x+18, 630)], (.08, .17, .19))
    panel(790, 160, 350, 620, (.09, .16, .19))
    city.rect('structure', 848, 235, 234, 530, (.018, .035, .05))
    for x in [820, 1090]:
        city.line('lights', [(x, 210), (x, 730)], AMBER, 5)
    for y in range(260, 750, 65):
        city.rect('structure', 858, y, 214, 14, STEEL)
    floor()


def foundry():
    """Basalt banks, geothermal conduits, turbines and a contained molten core replace the city skyline."""
    city.gradient((.035, .018, .03), (.23, .075, .035))
    for x in range(-80, 2000, 190):
        city.poly('base', [(x, 820), (x-35, 360), (x+20, 160+(x%90)), (x+100, 220), (x+180, 820)], (.065, .045, .055))
    for x in [260, 1660]:
        turbine(x, 420, 165, AMBER)
        pipe([(x, 70), (x, 180), (x+100, 240)], AMBER, 34)
    for radius in [120, 175, 205]:
        city.arc('structure', 960, 430, radius, 0, math.tau, EDGE, 18)
    for radius in [75, 108, 160, 193]:
        city.arc('lights', 960, 430, radius, 0, math.tau, (1, .38+radius/1200, .1), 10)
    for x in [670, 1180]:
        panel(x, 180, 70, 570)
        pipe([(x+35, 220), (x+35, 640), (960, 710)], AMBER, 24)
    city.poly('base', [(0, 780), (550, 750), (960, 920), (1370, 750), (1920, 780), (1920, 1080), (0, 1080)], (.45, .11, .025))
    floor()


def sky():
    """Layered cloud strata and distant weather provide altitude without a repeating city silhouette."""
    city.gradient((.045, .095, .17), (.25, .39, .48))
    for row in range(6):
        y = 130+row*110
        city.poly('base', [(0, y+60), (250, y+15), (600, y+40), (960, y), (1350, y+32),
                          (1700, y+10), (1920, y+45), (1920, y+100), (0, y+120)], (.12+row*.014, .2+row*.018, .28+row*.022))


def docks():
    """Aviation gantries, fuel equipment, a hangar and a clearly readable VTOL establish Stratos Docks."""
    sky()
    for x in [60, 1740]:
        panel(x, 80, 110, 720)
        city.line('structure', [(x+40, 110), (x+40, 700)], EDGE, 18)
        city.line('structure', [(x+20, 150), (960, 245)], EDGE, 12)
    panel(1170, 270, 620, 510, (.09, .15, .20))
    city.rect('structure', 1225, 355, 510, 400, DARK)
    for y in range(360, 760, 40):
        city.rect('structure', 1230, y, 500, 10, (.16, .23, .28))
    for x in [260, 410, 560]:
        panel(x, 530, 90, 240)
        pipe([(x+45, 530), (x+45, 490), (800, 490)], AMBER, 18)
    # Angular fuselage with lift rotors, cockpit glazing and deployment ramp.
    city.poly('structure', [(650, 585), (735, 480), (970, 455), (1170, 545), (1195, 620), (1010, 655), (760, 640)], (.19, .28, .35))
    city.poly('structure', [(965, 480), (1050, 493), (1125, 545), (960, 530)], (.11, .38, .45))
    for x in [730, 1100]:
        city.arc('structure', x, 490, 94, 0, math.tau, EDGE, 22)
        city.arc('lights', x, 490, 75, 0, math.tau, CYAN, 3)
    city.line('lights', [(750, 620), (1000, 635), (1160, 605)], CYAN, 4)
    floor()


def hull():
    """Exposed hull plating and diagonal structural braces frame a readable, high-altitude ascent."""
    sky()
    city.poly('structure', [(0, 0), (900, 0), (1300, 1080), (0, 1080)], (.07, .12, .19))
    for row in range(4):
        for col in range(3):
            panel(col*270+row*50-130, row*240-60, 250, 220, (.12+row*.012, .19, .25))
    for y in [160, 440, 720]:
        city.line('structure', [(80, y), (900, y-140)], EDGE, 28)
        city.line('lights', [(90, y+12), (910, y-128)], CYAN, 3)
    panel(1010, 120, 180, 660)
    for y in range(160, 760, 60):
        city.line('structure', [(1030, y), (1170, y+45)], EDGE, 12)
    for x, y in [(1200, 735), (1410, 585), (1620, 435)]:
        city.poly('structure', [(x, y), (x+250, y), (x+220, y+35), (x, y+35)], STEEL)
        city.line('lights', [(x, y), (x+250, y)], CYAN, 4)
    floor()


def nexus():
    """Server vaults, optical trunks, cooling pipes and a suspended quantum control node define the Cyber-Nexus."""
    city.gradient((.015, .035, .085), (.055, .14, .22))
    for x in [50, 290, 530, 1190, 1430, 1670]:
        panel(x, 150, 190, 590, (.07, .12, .20))
        for row in range(12):
            panel(x+18, 185+row*43, 154, 32, (.12, .20, .27))
            for col in range(6):
                city.rect('lights', x+28+col*20, 195+row*43, 6, 4, CYAN if (row+col)%3 else AMBER)
    for y in [75, 120]:
        pipe([(0, y), (730, y), (850, 260)], CYAN, 24)
        pipe([(1920, y), (1190, y), (1070, 260)], CYAN, 24)
    turbine(960, 390, 122, CYAN)
    for radius in [160, 195]:
        city.arc('lights', 960, 390, radius, .2, math.tau-.2, (.25, .55, .88), 3)
    panel(845, 645, 230, 125)
    city.poly('lights', [(865, 660), (1055, 660), (1035, 715), (885, 715)], (.16, .65, .76))
    floor()


def bastion():
    """Obsidian battlements, flak batteries, hydraulics and colossal fortress gates distinguish Apex Bastion."""
    sky()
    for x in range(-60, 2000, 300):
        panel(x, 180, 270, 620, (.08, .07, .13))
        city.poly('structure', [(x, 200), (x, 100), (x+60, 100), (x+90, 180), (x+180, 180), (x+210, 100), (x+270, 100), (x+270, 200)], (.18, .17, .24))
    panel(720, 180, 480, 600, (.14, .12, .19))
    for x in [750, 980]:
        panel(x, 225, 190, 540, DARK)
        city.line('lights', [(x+90, 260), (x+90, 710)], AMBER, 4)
    for x in [380, 1460]:
        panel(x-65, 620, 130, 160)
        city.line('structure', [(x, 630), (x-90, 500)], EDGE, 35)
        city.line('structure', [(x+30, 625), (x-60, 495)], EDGE, 18)
        pipe([(x-150, 790), (x-150, 400), (x-40, 300)], AMBER, 24)
    floor()


def judgment():
    """Cathedral arches, a throne dais and singularity machinery establish the campaign's final chamber."""
    city.gradient((.012, .006, .035), (.15, .055, .20))
    for x in [80, 420, 1400, 1740]:
        panel(x, 70, 100, 730, (.11, .075, .16))
        pipe([(x+50, 750), (x+50, 240), (960, 80)], (.6, .22, .72), 14)
    for radius in [145, 175, 205]:
        city.arc('structure', 960, 280, radius, 0, math.tau, (.24, .15, .32), 12)
    city.arc('lights', 960, 280, 160, 0, math.tau, (.72, .26, .91), 12)
    city.arc('lights', 960, 280, 187, -.5, 4.5, CYAN, 4)
    for i in range(18):
        a=i*math.tau/18
        city.line('lights', [(960+math.cos(a)*215, 280+math.sin(a)*215),
                             (960+math.cos(a+.08)*240, 280+math.sin(a+.08)*240)], AMBER, 3)
    city.poly('structure', [(700, 800), (780, 655), (1140, 655), (1220, 800)], (.18, .12, .24))
    panel(855, 450, 210, 215, (.15, .095, .22))
    city.poly('structure', [(835, 640), (820, 440), (860, 510), (900, 390), (960, 460),
                           (1020, 390), (1060, 510), (1100, 440), (1085, 640)], (.25, .17, .32))
    city.line('lights', [(845, 638), (960, 674), (1075, 638)], AMBER, 5)
    floor()


def boss_plate(index, output):
    """Draw a distinct armored boss in the same beveled palette; separate glowing features preserve runtime effects."""
    city.BATCHES.clear(); city.ORDER = 0
    city.W = city.H = 512
    colors = [AMBER, CYAN, (.45, .65, .95), (.25, .9, .85), (.88, .6, .24), (.88, .18, .45)]
    accent = colors[index]
    for x in [176, 280]:
        panel(x, 320, 56, 125, (.23, .28, .35))
    panel(165, 150, 180, 205, (.16, .20, .28))
    panel(209, 85, 94, 85, (.3, .34, .43))
    city.rect('lights', 221, 119, 70, 9, accent)
    if index in [0, 4]:
        for x in [95, 335]:
            panel(x, 160, 80, 160, (.3, .34, .4))
        turbine(255, 247, 44, accent)
    elif index in [1, 2]:
        for side in [-1, 1]:
            city.poly('structure', [(256+side*70, 165), (256+side*210, 205),
                                   (256+side*200, 275), (256+side*80, 235)], (.24, .31, .39))
            city.line('lights', [(256+side*95, 215), (256+side*190, 240)], accent, 6)
        turbine(256, 250, 43, accent)
    elif index == 3:
        for r in [120, 147]:
            city.arc('lights', 256, 245, r, .1, math.tau-.1, accent, 4)
        for x in [120, 350]:
            panel(x, 170, 40, 120)
    else:
        city.poly('structure', [(178, 130), (145, 440), (255, 405), (370, 440), (334, 130)], (.08, .035, .14))
        panel(190, 165, 130, 175, (.2, .11, .28))
        for side in [-1, 1]:
            city.line('structure', [(256+side*75, 240), (256+side*170, 110)], EDGE, 12)
            city.line('lights', [(256+side*78, 225), (256+side*165, 100)], accent, 6)
    city.render('boss', output, output)
    city.W, city.H = 1920, 1080


def aftermath_plate(index):
    """Author extraction settings that respect destroyed cores, empty pods, open gates and the final dawn."""
    if index == 0:
        city.gradient((.018, .04, .055), (.08, .19, .19))
        for x in range(0, 1920, 320):
            panel(x+30, 0, 90, 800)
            pipe([(x+65, 0), (x+65, 740)], (.3, .7, .57), 18)
        for x in [80, 1660]:
            panel(x, 300, 180, 420, DARK)
            city.rect('lights', x+15, 360, 150, 4, CYAN)
    elif index == 1:
        city.gradient((.07, .04, .08), (.37, .12, .05))
        for x in range(-100, 2000, 240):
            city.poly('structure', [(x, 820), (x+30, 610), (x+90, 560), (x+180, 720), (x+250, 820)], (.11, .075, .09))
        for x in [400, 1400]:
            pipe([(x, 680), (x+80, 430), (x+100, 200)], AMBER, 25)
            city.line('lights', [(x+85, 410), (x+130, 340)], (.95, .25, .12), 7)
    elif index in [2, 3]:
        sky()
        if index == 2:
            # The docks are far below the airborne extraction transport, without a second parked hero ship.
            for x in range(180, 1800, 300):
                panel(x, 630+(x%80), 220, 100)
                city.line('lights', [(x, 640+(x%80)), (x+200, 640+(x%80))], CYAN, 3)
        else:
            panel(1170, 20, 680, 790)
            city.rect('structure', 1260, 180, 460, 600, DARK)
            for x in [1240, 1740]:
                city.line('lights', [(x, 200), (x, 750)], CYAN, 7)
    elif index == 4:
        nexus()
        # The disabled core and accepted override remain distinct from the active intro machine.
        city.rect('structure', 800, 215, 320, 350, (.025, .075, .12))
        panel(875, 300, 170, 170, (.06, .12, .17))
    elif index == 5:
        bastion()
        city.rect('structure', 760, 220, 400, 555, (.022, .018, .04))
        for y in range(630, 800, 32):
            city.rect('structure', 710-(y-630)*.4, y, 500+(y-630)*.8, 28, (.24, .20, .29))
    else:
        # Dawn overlooks twelve autonomous districts; the throne and singularity are absent after their destruction.
        city.gradient((.22, .15, .29), (.72, .43, .29))
        for radius in [95, 115, 135]:
            city.arc('lights', 1250, 340, radius, 0, math.tau, (.94, .72, .4), 14)
        for district in range(12):
            x=50+district*155
            for col in range(3):
                h=100+((district*13+col*27)%150)
                panel(x+col*42, 690-h, 38, h, (.13, .17, .23))
                for y in range(int(710-h), 670, 24):
                    city.rect('lights', x+col*42+10, y, 7, 4, AMBER if district%2 else CYAN)
        for x in [40, 1770]:
            panel(x, 230, 100, 560, (.12, .10, .18))
            city.poly('structure', [(x, 235), (x+10, 130), (x+60, 195), (x+100, 230)], (.21, .16, .27))
        city.line('structure', [(1450, 780), (1580, 745)], EDGE, 8)
    floor()


def main():
    """Render review targets, or produce families only after the agent has inspected those representative targets."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--targets', action='store_true')
    parser.add_argument('--produce', action='store_true')
    args = parser.parse_args(sys.argv[sys.argv.index('--')+1:])
    builders = [laboratory, foundry, docks, hull, nexus, bastion, judgment]
    if args.targets:
        for family, build in zip(FAMILIES, builders):
            target = REVIEW / family; target.mkdir(parents=True, exist_ok=True)
            city.BATCHES.clear(); city.ORDER = 0
            build(); city.render('environment', target, target)
        return
    if not args.produce:
        parser.error('Choose --targets or --produce explicitly.')
    for index, family in enumerate(FAMILIES):
        source = REVIEW / family
        if not (source/'environment_artwork.png').is_file():
            raise SystemExit('Render and inspect every target before production.')
        destination = OUT / family; destination.mkdir(parents=True, exist_ok=True)
        for layer in ['base', 'structure', 'lights']:
            shutil.copyfile(source/f'environment_{layer}.png', destination/f'environment_{layer}.png')
        city.BATCHES.clear(); city.ORDER = 0
        aftermath_plate(index)
        city.render('aftermath', destination, REVIEW / family)
        if index > 0:
            boss_plate(index-1, destination)
            (destination/'boss_base.png').unlink()
            (destination/'boss_artwork.png').unlink()
        manifest = dict(generator='Original planar emission geometry rendered in Docker Blender 4.3.2',
                        source='tools/cutscene/render_later_campaign.py', dimensions=[1920,1080],
                        layers=['opaque base', 'transparent architecture', 'transparent emissive accents'],
                        boss_dimensions=[512,512] if index else None, collision='None; authoritative collision remains in Godot',
                        rights='Original project geometry; no external stock assets', representative_review='Agent-inspected native-scale target before remaining family assets',
                        files={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in destination.glob('*.png')})
        (destination/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')


if __name__ == '__main__':
    main()
