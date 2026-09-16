"""Check the binary GLB delivery contract using only the Python standard library.

This does not claim visual quality, rig deformation quality, or gameplay integration.
Godot import and rendered inspections supplement it; see docs/ASSET_DELIVERY.md.
"""
import json
import math
import struct
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OPERATIVES = ['cyber_shogun', 'solaria', 'oceanus', 'surya_prime', 'mirage_blade', 'apex_titan']
ENEMIES = ['vanguard', 'heavy', 'stalker']
PROPS = ['explosive_barrel', 'hazard_zone_marker', 'timed_trap', 'switch_object', 'destructible_crate']
ARCHITECTURE = ['modular_wall_panel', 'modular_floor_tile', 'modular_ceiling_panel', 'virtual_forge', 'extraction_point', 'corporate_hub']
CLIPS = {'Idle', 'Running', 'Walking', 'Jumping', 'Death', 'Hit', 'Thrust_Slash', 'Karate_Strike', 'Attack'}


def read_glb(path):
    raw = path.read_bytes()
    assert len(raw) >= 20, 'incomplete header'
    magic, version, size = struct.unpack_from('<III', raw)
    assert magic == 0x46546C67 and version == 2 and size == len(raw), 'invalid GLB header'
    chunks, offset = {}, 12
    while offset < len(raw):
        length, kind = struct.unpack_from('<II', raw, offset)
        assert length % 4 == 0, 'unaligned chunk'
        assert offset + 8 + length <= len(raw), 'chunk outside GLB'
        chunks[kind] = raw[offset+8:offset+8+length]
        offset += length + 8
    doc = json.loads(chunks[0x4E4F534A])
    binary = chunks.get(0x004E4942, b'')
    return doc, binary


def validate(path, operative=False):
    doc, binary = read_glb(path)
    assert doc.get('meshes'), 'no meshes'
    assert all('uri' not in img for img in doc.get('images', [])), 'external image dependency'
    for view in doc.get('bufferViews', []):
        assert view.get('buffer', 0) == 0, 'unexpected external buffer'
        assert view.get('byteOffset', 0) + view['byteLength'] <= len(binary), 'buffer view outside binary'
    for material in doc.get('materials', []):
        assert 'pbrMetallicRoughness' in material, 'non-PBR material'
        assert all(math.isfinite(float(v)) for v in material.get('emissiveFactor', [])), 'invalid emissive value'
    emissive = [m.get('name','unnamed') for m in doc.get('materials', [])
                if any(v > 0 for v in m.get('emissiveFactor', []))]
    assert emissive, 'no explicitly defined emissive material'
    animations = doc.get('animations', [])
    names = [a.get('name','') for a in animations]
    assert len(set(names)) == len(names), 'duplicate clip names'
    if operative:
        assert doc.get('skins'), 'operative lacks a skin'
        assert CLIPS <= set(names), 'missing clips: ' + ', '.join(sorted(CLIPS-set(names)))
    for anim in animations:
        assert anim.get('channels') and anim.get('samplers'), 'empty animation'
        assert any(doc['accessors'][s['input']]['count'] >= 2 for s in anim['samplers']), 'single-frame animation'
    return {'path': path.relative_to(ROOT).as_posix(), 'bytes': path.stat().st_size,
            'mesh_count': len(doc['meshes']), 'skin_count': len(doc.get('skins',[])),
            'animations': names, 'emissive_materials': emissive,
            'embedded_images': len(doc.get('images',[]))}


def main():
    results, errors = [], []
    for directory, names in [('hq', OPERATIVES), ('enemies', ENEMIES), ('props', PROPS+ARCHITECTURE)]:
        for name in names:
            path = ROOT / 'assets/models' / directory / (name + '.glb')
            try:
                results.append(validate(path, directory == 'hq'))
            except Exception as exc:
                errors.append(f'{path.relative_to(ROOT)}: {exc}')
    report = {'checks': 'GLB2 headers, embedded dependencies, PBR/emission, rig/animation contract',
              'assets_checked': len(results), 'total_bytes': sum(r['bytes'] for r in results),
              'assets': results, 'errors': errors}
    output = ROOT / '.validation/asset-delivery-validation.json'
    output.parent.mkdir(exist_ok=True)
    output.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'assets_checked': len(results), 'errors': errors}, indent=2))
    return 1 if errors else 0


if __name__ == '__main__':
    sys.exit(main())
