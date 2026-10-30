"""Generate live cinematic layers and poses with Blender in the opt-in container.

--reveal-only makes the identity/composition review target before the full family.
All imagery is original planar geometry using the established project art helpers.
"""
import ast
import inspect
import sys
from pathlib import Path
import bpy

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'assets/cutscenes/deployment/live'
PREVIEW = ROOT / '.validation/deployment/live'
sys.path.insert(0, str(ROOT / 'tools/art'))
import render_menu_backdrops as city

# Debian Blender 4.3 uses a different Eevee identifier than the original helper.
# Emission-only Cycles keeps colors identical and renders portably on the CPU.
exec(inspect.getsource(city.render).replace("scene.render.engine = 'BLENDER_EEVEE'", "scene.render.engine = 'CYCLES'; scene.cycles.samples = 1"), city.__dict__)


def character_library():
    """Reuse documented character geometry without executing its batch-render loop."""
    source = ROOT / 'tools/art/draw_flat_characters.py'
    text = source.read_text().replace("hand=(shoulder[0]+32,120)", "hand=(shoulder[0]+32,120 + (1-frame/7)*18 if action == 'settle' else 120)")
    tree = ast.parse(text)
    # The source's final module-level for loop produces every operative; load only
    # imports, constants and function definitions for this focused cutscene batch.
    tree.body = [node for node in tree.body if not isinstance(node, (ast.For, ast.Expr))]
    namespace = {'__file__': str(source), '__name__': 'cinematic_character'}
    exec(compile(tree, str(source), 'exec'), namespace)
    return namespace


def rooftop():
    """Draw a separated ledge, support faces and cyan rain-wet edge details."""
    city.BATCHES.clear()
    city.ORDER = 0
    city.poly('structure', [(0, 765), (670, 765), (770, 795), (770, 1080), (0, 1080)], (.018, .035, .065))
    city.poly('structure', [(1050, 765), (1920, 765), (1920, 1080), (1050, 1080)], (.018, .035, .065))
    for a, b in [(0, 760), (1050, 1920)]:
        city.line('lights', [(a, 766), (b, 766)], (.19, .64, .70), 4)
        for x in range(a + 20, b, 80):
            city.rect('structure', x, 810, 4, 220, (.035, .075, .11))
            city.rect('lights', x + 10, 777, 40, 2, (.11, .30, .36))
    city.rect('structure', 180, 642, 140, 120, (.025, .06, .09))
    for y in range(656, 741, 14):
        city.rect('lights', 192, y, 114, 3, (.09, .22, .27))
    city.render('roof', OUT, PREVIEW)


def main():
    """Render a representative reveal first, then deterministic layered scenery/atlas."""
    OUT.mkdir(parents=True, exist_ok=True)
    PREVIEW.mkdir(parents=True, exist_ok=True)
    lib = character_library()
    lib['clear']()
    lib['character']('cyber_shogun', 'idle', 2, offset=(384, 110), scale=3.0)
    lib['render'](PREVIEW / 'reveal_character.png', 768, 768)
    if '--reveal-only' in sys.argv:
        return
    lib['clear']()
    actions = ['idle', 'running', 'jumping', 'blade_dash', 'settle']
    for row, action in enumerate(actions):
        for frame in range(8):
            lib['character']('cyber_shogun', action, frame,
                             offset=(frame * 512 + 256, (4-row)*512 + 64), scale=2)
    lib['render'](OUT / 'shogun.png', 4096, 2560)
    city.BATCHES.clear()
    city.ORDER = 0
    city.city()
    city.render('city', OUT, PREVIEW)
    rooftop()
    city.BATCHES.clear()
    city.ORDER = 0
    city.poly('structure', [(850, 470), (915, 450), (1005, 450), (1070, 470), (1005, 490), (915, 490)], (.025, .06, .10))
    city.line('lights', [(860, 467), (925, 467), (945, 480), (980, 480), (995, 467), (1060, 467)], (.25, .72, .80), 4)
    city.rect('lights', 952, 479, 16, 6, (.85, .18, .20))
    city.render('drone', OUT, PREVIEW)
    # The shared renderer emits blank base layers for isolated props; do not ship them.
    for name in ['roof_base.png', 'drone_base.png']:
        (OUT / name).unlink(missing_ok=True)


if __name__ == '__main__':
    main()
