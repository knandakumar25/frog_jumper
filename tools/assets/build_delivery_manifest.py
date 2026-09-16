"""Index the delivered GLBs for the standalone Godot review scene."""
import json
from pathlib import Path
from validate_delivery import ROOT, OPERATIVES, ENEMIES, PROPS, ARCHITECTURE, read_glb

assets = []
for directory, category, names in [('hq', 'operative', OPERATIVES), ('enemies', 'enemy', ENEMIES),
                                  ('props', 'tactical prop', PROPS), ('props', 'architecture', ARCHITECTURE)]:
    for name in names:
        path = Path('assets/models') / directory / (name + '.glb')
        record = {'name': name.replace('_', ' ').title(), 'category': category,
                  'path': 'res://' + path.as_posix(), 'origin': 'base', 'units': 'meters'}
        if (ROOT/path).exists():
            doc, _ = read_glb(ROOT/path)
            record['animations'] = [a.get('name','') for a in doc.get('animations',[])]
            record['bytes'] = (ROOT/path).stat().st_size
        assets.append(record)
manifest = {'name': 'Axiom Force / Neon Pulse asset delivery', 'godot_target': '4.7.2',
            'briefs': ['docs/ASSET_SPEC.md', 'docs/CODEX_DELIVERY_GUIDE.md'],
            'preview_scene': 'res://scenes/asset_preview/asset_showcase.tscn', 'assets': assets}
(ROOT/'assets/asset_delivery_manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
print(f'Indexed {len(assets)} delivery paths.')
