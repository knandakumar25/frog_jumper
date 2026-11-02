"""Render original district plates for the Mission 5 names epilogue with Docker Blender."""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from render_wildfire import city

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'assets/cutscenes/names_without_numbers'
PREVIEW = ROOT / '.validation/names-without-numbers'


def render_district():
    """Build an open district exit: the registry remains behind, public service doors glow ahead."""
    city.BATCHES.clear()
    city.ORDER = 0
    city.poly('structure', [(0,0),(1920,0),(1920,690),(1580,650),(1320,690),(980,640),(650,685),(330,635),(0,690)], (.025,.05,.075))
    for x, height, width in [(75,330,210),(330,440,180),(550,300,250),(820,410,220),(1110,330,240),(1400,460,250),(1710,340,180)]:
        top = 660-height
        city.poly('structure', [(x,660),(x,top+42),(x+width*.18,top),(x+width*.82,top),(x+width,top+38),(x+width,660)], (.09,.15,.19))
        for wx in range(int(x+28), int(x+width-20), 48):
            city.rect('lights',wx,top+85,14,40,(.12,.38,.42))
        city.rect('structure',x+width*.4,570,42,90,(.04,.075,.09))
    # The processing arch stays in silhouette; its inactive red scanner gives way to warm civic lighting.
    city.poly('structure', [(300,660),(300,465),(360,365),(620,365),(680,465),(680,660)], (.13,.18,.2))
    city.rect('lights',386,444,208,9,(.15,.5,.5))
    for i in range(6):
        city.rect('structure',405+i*28,466,18,16,(.32,.38,.36))
    # A long, clear public walkway leads toward the district rather than another military objective.
    city.poly('structure', [(0,660),(1920,660),(1920,1080),(0,1080)], (.075,.105,.125))
    city.line('structure',[(0,662),(1920,662)],(.24,.36,.38),5)
    city.line('lights',[(720,675),(1920,675)],(.85,.63,.32),4)
    for x in [750,1040,1330,1620,1890]:
        city.rect('structure',x,704,7,80,(.15,.25,.28))
        city.rect('lights',x-7,698,21,8,(.96,.66,.34))
    city.render('district', OUT, PREVIEW)
    (OUT/'district_base.png').unlink(missing_ok=True)


def main():
    """Render a preview on request, or ship the original plates and source provenance."""
    OUT.mkdir(parents=True, exist_ok=True)
    PREVIEW.mkdir(parents=True, exist_ok=True)
    render_district()
    if '--preview' in sys.argv:
        return
    (OUT/'manifest.json').write_text(json.dumps({
        'generator': 'Original project planar geometry rendered with Docker Blender',
        'scenery': [1920,1080],
        'files': ['district_structure.png','district_lights.png'],
        'rights': 'Original generated project artwork; no external stock material.',
        'story': 'Restored public district, processing scanner deactivated, open walkways lead inward.',
        'source': 'tools/cutscene/render_names_without_numbers.py'
    }, indent=2)+'\n', encoding='utf-8')


if __name__ == '__main__':
    main()
