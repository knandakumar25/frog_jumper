"""Author four flat cinematic plates in Blender using the existing art vocabulary.

Run with Blender --background --python tools/cutscene/render.py. Intermediate
plates go to .validation/deployment; only the packaged video enters the game.
"""
import sys
import inspect
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools/art'))
import render_menu_backdrops as art

# The legacy helper names Eevee differently across Blender releases. Portable
# emission-only CPU rendering keeps the legacy preview build usable in Docker.
exec(inspect.getsource(art.render).replace("scene.render.engine = 'BLENDER_EEVEE'", "scene.render.engine = 'CYCLES'; scene.cycles.samples = 1; scene.cycles.use_denoising = False"), art.__dict__)


def main():
    """Draw city, rooftop reveal, deployment trail, and transmission plates."""
    output = ROOT / '.validation/deployment'
    output.mkdir(parents=True, exist_ok=True)
    for index in range(4):
        art.BATCHES.clear()
        art.ORDER = 0
        if index == 3:
            art.command_bay()
        else:
            art.city()
            if index in (1, 2):
                # Original outlined silhouette complements the established planar artwork.
                art.rect('structure', 0, 910, 1920, 170, (0.015, 0.025, 0.045))
                x = 590 if index == 1 else 1150
                art.poly('structure', [(x-70, 905), (x-45, 650), (x+45, 650),
                                      (x+85, 905), (x+20, 905), (x, 760), (x-15, 905)], (0.03, 0.08, 0.12))
                art.poly('structure', [(x-65, 650), (x-42, 560), (x+38, 560),
                                      (x+65, 650)], (0.09, 0.21, 0.26))
                art.line('lights', [(x-32, 615), (x+32, 615)], (0.2, 0.9, 0.95), 6)
                art.line('lights', [(x+55, 755), (x+240, 515)], (0.55, 0.95, 1.0), 5)
                if index == 2:
                    for offset in range(6):
                        art.line('lights', [(100, 710+offset*18), (1000, 680+offset*18)], (0.1, 0.5, 0.6), 2)
        art.render(str(index), output, output)


if __name__ == '__main__':
    main()
