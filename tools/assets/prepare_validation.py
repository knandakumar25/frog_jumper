"""Copy just delivery assets into an isolated Godot project, without game autoloads."""
import json
import shutil
import argparse
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
TARGET = ROOT / '.validation/asset-delivery-project'
TARGET.mkdir(parents=True, exist_ok=True)


def copy(relative):
    source = ROOT / relative
    target = TARGET / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, target)


manifest = json.loads((ROOT/'assets/asset_delivery_manifest.json').read_text(encoding='utf-8'))
parser = argparse.ArgumentParser()
parser.add_argument('--category', action='append', help='Optional incremental validation category')
options = parser.parse_args()
if options.category:
    manifest['assets'] = [asset for asset in manifest['assets'] if asset['category'] in options.category]
(TARGET/'assets').mkdir(exist_ok=True)
(TARGET/'assets/asset_delivery_manifest.json').write_text(json.dumps(manifest, indent=2)+'\n', encoding='utf-8')
for asset in manifest['assets']:
    copy(asset['path'].removeprefix('res://'))
for relative in ['scripts/tools/asset_showcase.gd', 'scenes/asset_preview/asset_showcase.tscn']:
    copy(relative)
if (ROOT/'assets/vfx').exists():
    for source in (ROOT/'assets/vfx').rglob('*'):
        if source.is_file() and source.suffix not in {'.import', '.uid'}:
            copy(source.relative_to(ROOT))
shutil.copy2(ROOT/'tools/assets/test_delivery.gd', TARGET/'test_delivery.gd')
(TARGET/'project.godot').write_text('''config_version=5

[application]
config/name="Axiom Force Asset Validation"
run/main_scene="res://scenes/asset_preview/asset_showcase.tscn"
config/features=PackedStringArray("4.7", "Mobile")

[display]
window/size/viewport_width=1440
window/size/viewport_height=900

[rendering]
renderer/rendering_method="mobile"
''', encoding='utf-8')
print(TARGET)
