"""Author Concrete Wildfire arrival' planar Blender art in the existing opt-in Docker image.

Run with --preview first to inspect checkpoint composition, then without it for poses/blade.
Native scenery is 1920x1080 RGBA, poses are four 512px cells, blade is 512px.
The shared red/gold character geometry preserves the opening's outlined identity.
"""
import json
import inspect
import ast
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools/cutscene'))
from render_live import character_library, city
# Debian's Blender omits OpenImageDenoise; planar emission renders need no denoising.
renderer_source = (ROOT / 'tools/art/render_menu_backdrops.py').read_text()
renderer = next(node for node in ast.parse(renderer_source).body if isinstance(node, ast.FunctionDef) and node.name == 'render')
exec(ast.get_source_segment(renderer_source, renderer).replace("scene.render.engine = 'BLENDER_EEVEE'",
     "scene.render.engine = 'CYCLES'; scene.cycles.samples = 1; scene.cycles.use_denoising = False"), city.__dict__)
OUT = ROOT / 'assets/cutscenes/wildfire'
PREVIEW = ROOT / '.validation/wildfire'


def checkpoint():
    """Author layered damaged checkpoint, shelters, abandoned cars and biometric architecture."""
    city.BATCHES.clear()
    city.ORDER = 0
    city.rect('structure', 0, 820, 1920, 260, (.025, .048, .07))
    for x in range(0, 1920, 180):
        city.line('lights', [(x, 930), (x + 90, 930)], (.35, .26, .13), 3)
    for x, h in [(50, 440), (420, 340), (1450, 470), (1730, 300)]:
        city.poly('structure', [(x,820),(x,h),(x+60,h-40),(x+120,h+35),(x+210,h),(x+250,820)], (.035,.075,.105))
        for y in range(h+65, 780, 70):
            city.rect('lights', x+35, y, 26, 15, (.18,.45,.5))
        city.line('structure', [(x+90,h),(x+85,h+190),(x+150,h+230)], (.012,.02,.03), 7)
    # Shelters, elevated approach, abandoned vehicles and protective concrete barriers.
    city.rect('structure', 0, 590, 580, 26, (.09,.14,.18))
    for x in [80, 390]:
        city.rect('structure', x, 615, 30, 205, (.06,.10,.14))
    for x in [190, 600, 1360]:
        city.poly('structure', [(x,800),(x+20,690),(x+175,690),(x+210,800)], (.12,.17,.20))
        city.line('lights', [(x+30,710),(x+170,710)], (.25,.6,.65), 3)
    for x in [750, 1610]:
        city.poly('structure', [(x,803),(x+12,755),(x+60,748),(x+90,709),(x+160,709),(x+200,751),(x+225,770),(x+225,810)], (.06,.10,.13))
        city.rect('lights', x+95, 720, 53, 22, (.1,.29,.34))
    for x in [1100, 1260, 1420]:
        city.rect('structure', x, 410, 26, 400, (.055,.11,.15))
        city.poly('structure', [(x-20,450),(x+13,355),(x+48,450)], (.1,.18,.22))
        city.rect('lights', x+7, 435, 10, 330, (.28,.83,.88))
    city.render('checkpoint', OUT, PREVIEW)
    (OUT / 'checkpoint_base.png').unlink(missing_ok=True)


def silhouettes():
    """Render a protected civilian family and remote mortar silhouettes as transparent reusable layers."""
    city.BATCHES.clear()
    city.ORDER = 0
    for x, y, scale in [(280,750,1), (330,760,.72), (385,750,1), (1510,750,1)]:
        city.poly('structure', [(x-12*scale,y),(x-17*scale,y-65*scale),(x,y-85*scale),(x+17*scale,y-65*scale),(x+12*scale,y)], (.08,.14,.18))
        city.rect('structure', x-9*scale,y-108*scale,18*scale,22*scale,(.13,.21,.25))
        city.line('lights', [(x-12*scale,y-64*scale),(x+10*scale,y-65*scale)], (.42,.48,.43), 3)
    city.render('civilians', OUT, PREVIEW)
    (OUT / 'civilians_base.png').unlink(missing_ok=True)
    city.BATCHES.clear()
    city.ORDER = 0
    for x in [1050, 1270, 1510]:
        city.poly('structure', [(x-65,560),(x-40,490),(x+40,490),(x+65,560)], (.012,.027,.045))
        city.line('structure', [(x,510),(x-40,438)], (.035,.07,.09), 22)
        city.rect('lights', x-20,510,40,5,(.7,.23,.11))
    city.render('siege', OUT, PREVIEW)
    (OUT / 'siege_base.png').unlink(missing_ok=True)


def main():
    """Generate review composition, then the matching pose family and transparent Data-Blade."""
    OUT.mkdir(parents=True, exist_ok=True)
    PREVIEW.mkdir(parents=True, exist_ok=True)
    checkpoint()
    if '--preview' in sys.argv:
        return
    silhouettes()
    lib = character_library()
    # Two extraction-only poses reuse the same geometry: lower the hand to the sheath,
    # retract the exposed steel, then reflect the look-back pose toward the surviving dojo.
    source = inspect.getsource(lib['character'])
    source = source.replace('hand=(shoulder[0]+32,120)',
                            "hand=(shoulder[0]+16,112) if action == 'settle' else (shoulder[0]+32,120)")
    source = source.replace('length=47 if kind=="cyber_shogun" else 29',
                            'length=(3 if action in ("idle", "look_back") else 47) if kind=="cyber_shogun" else 29')
    exec(source, lib)
    lib['clear']()
    original_poly = lib['poly']
    for cell, action in enumerate(['running', 'idle', 'settle', 'look_back']):
        if cell == 3:
            def reflected(points, color, outline=2):
                """Mirror the final pose about its own cell pivot, preserving atlas placement."""
                original_poly([(3584 - x, y) for x, y in points], color, outline)
            lib['poly'] = reflected
        lib['character']('cyber_shogun', action, 7 if cell == 2 else 2,
                         offset=(cell * 512 + 256, 64), scale=2)
    lib['poly'] = original_poly
    lib['render'](OUT / 'poses.png', 2048, 512)
    lib['clear']()
    # Original flat geometry in the same emission renderer as the Shogun atlas.
    lib['poly']([(200, 100), (225, 92), (360, 390), (349, 448), (320, 415)], '#75e7ec')
    lib['poly']([(215, 110), (225, 105), (347, 392), (344, 425)], '#d5ffff')
    lib['poly']([(174, 101), (252, 78), (256, 94), (180, 118)], '#e4b766')
    lib['render'](OUT / 'blade.png', 512, 512)
    manifest = {'generator': 'Blender 4.3.2 / Python in opt-in cutscene Docker service',
                'provenance': 'Original project planar geometry; shared Shogun character_library',
                'palette': 'outlined red/gold operative, navy/cyan checkpoint, amber firelight',
                'filtering': 'linear', 'scenery': [1920, 1080], 'poses': [2048, 512],
                'pose_order': ['approach', 'survey', 'ready', 'look_back'],
                'pose_cell': [512, 512], 'pose_pivot': [256, 448], 'blade': [512, 512],
                'speech': 'ElevenLabs MCP: Daniel AXIOM-7; Adam Kaito; seven cached English cues'}
    (OUT / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')


if __name__ == '__main__':
    main()
