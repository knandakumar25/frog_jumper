"""Author Tokyo Remembers' planar Blender art in the existing opt-in Docker image.

Run with --preview first to inspect dojo composition, then without it for poses/blade.
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
OUT = ROOT / 'assets/cutscenes/tokyo'
PREVIEW = ROOT / '.validation/tokyo'


def dojo():
    """Render damaged outer roof, unbroken archive core and warm lights as separate layers."""
    city.BATCHES.clear()
    city.ORDER = 0
    city.rect('structure', 0, 820, 1920, 260, (.025, .048, .07))
    for x in range(0, 1920, 120):
        city.line('structure', [(x, 830), (x + 190, 1080)], (.07, .12, .15), 2)
    city.rect('structure', 130, 430, 640, 370, (.025, .055, .075))
    city.poly('structure', [(65, 440), (180, 388), (260, 280), (340, 346),
                            (395, 326), (435, 381), (540, 354), (650, 388), (800, 440)], (.075, .13, .17))
    city.line('lights', [(65, 440), (180, 388), (260, 280)], (.24, .55, .6), 3)
    for x in [160, 340, 610, 740]:
        city.rect('structure', x, 445, 22, 355, (.16, .105, .09))
        city.rect('lights', x + 4, 448, 3, 340, (.6, .33, .16))
    city.rect('structure', 420, 510, 210, 210, (.015, .032, .05))
    city.line('lights', [(420, 720), (420, 510), (630, 510), (630, 720)], (.2, .7, .76), 4)
    # Split extraction gate: broken lintel, intact right post; no beacon explosion.
    city.rect('structure', 900, 290, 30, 530, (.08, .11, .14))
    city.rect('structure', 1380, 290, 30, 530, (.08, .11, .14))
    city.poly('structure', [(860, 270), (1110, 285), (1130, 320), (890, 310)], (.14, .16, .18))
    city.poly('structure', [(1170, 285), (1450, 270), (1420, 310), (1150, 325)], (.14, .16, .18))
    for x in [910, 1390]:
        city.rect('lights', x, 345, 3, 460, (.16, .72, .77))
    for x, y in [(195, 460), (680, 460), (1350, 330)]:
        city.rect('lights', x - 18, y, 36, 50, (.86, .48, .22))
        for offset in [-12, 0, 12]:
            city.rect('structure', x + offset, y, 2, 50, (.29, .15, .10))
    city.render('dojo', OUT, PREVIEW)
    # The shared scenery renderer emits a blank base; the opening's city supplies ours.
    (OUT / 'dojo_base.png').unlink(missing_ok=True)


def main():
    """Generate review composition, then the matching pose family and transparent Data-Blade."""
    OUT.mkdir(parents=True, exist_ok=True)
    PREVIEW.mkdir(parents=True, exist_ok=True)
    dojo()
    if '--preview' in sys.argv:
        return
    lib = character_library()
    # Two extraction-only poses reuse the same geometry: lower the hand to the sheath,
    # retract the exposed steel, then reflect the look-back pose toward the surviving dojo.
    source = inspect.getsource(lib['character'])
    source = source.replace('hand=(shoulder[0]+32,120)',
                            "hand=(shoulder[0]+16,112) if action == 'settle' else (shoulder[0]+32,120)")
    source = source.replace('length=47 if kind=="cyber_shogun" else 29',
                            'length=(3 if action in ("idle", "settle", "look_back") else 47) if kind=="cyber_shogun" else 29')
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
                'palette': 'outlined red/gold operative, navy/cyan dojo, amber lanterns',
                'filtering': 'linear', 'scenery': [1920, 1080], 'poses': [2048, 512],
                'pose_order': ['escape', 'testimony', 'sheathe', 'look_back'],
                'pose_cell': [512, 512], 'pose_pivot': [256, 448], 'blade': [512, 512],
                'speech': 'ElevenLabs MCP: Daniel AXIOM-7; Adam Kaito; six cached English cues'}
    (OUT / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')


if __name__ == '__main__':
    main()
