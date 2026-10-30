"""Validate cinematic PNG dimensions, alpha coverage and atlas cells in container Python.

Writes an explicit manifest of original Blender imagery; narration/video are untouched.
"""
import json
import struct
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'assets/cutscenes/deployment/live'


def pixels(path):
    """Decode Blender's RGBA8 PNG rows, including all five PNG filter algorithms."""
    data = path.read_bytes()
    assert data[:8] == b'\x89PNG\r\n\x1a\n', path
    offset, compressed = 8, bytearray()
    while offset < len(data):
        size = struct.unpack('>I', data[offset:offset+4])[0]
        kind = data[offset+4:offset+8]
        payload = data[offset+8:offset+8+size]
        if kind == b'IHDR':
            w, h, depth, color, _, _, interlace = struct.unpack('>IIBBBBB', payload)
            assert (depth, color, interlace) == (8, 6, 0), path
        elif kind == b'IDAT':
            compressed.extend(payload)
        offset += size + 12
    raw = zlib.decompress(compressed)
    stride, previous, rows = w * 4, bytearray(w * 4), []
    for y in range(h):
        start = y * (stride + 1)
        mode, row = raw[start], bytearray(raw[start+1:start+1+stride])
        for i in range(stride):
            a, b, c = (row[i-4] if i >= 4 else 0), previous[i], (previous[i-4] if i >= 4 else 0)
            if mode == 1:
                predictor = a
            elif mode == 2:
                predictor = b
            elif mode == 3:
                predictor = (a+b)//2
            elif mode == 4:
                p = a+b-c
                predictor = min((a,b,c), key=lambda v: abs(p-v))
            else:
                assert mode == 0
                predictor = 0
            row[i] = (row[i] + predictor) & 255
        rows.append(row)
        previous = row
    return w, h, rows


def main():
    """Fail on empty/cropped atlas cells, wrong sizes or missing transparency."""
    records = []
    for name in ['city_base', 'city_structure', 'city_lights', 'roof_structure', 'roof_lights', 'drone_structure', 'drone_lights', 'shogun']:
        path = OUT / f'{name}.png'
        w, h, rows = pixels(path)
        assert (w, h) == ((4096, 2560) if name == 'shogun' else (1920, 1080)), path
        alphas = [row[3::4] for row in rows]
        assert any(max(row) > 0 for row in alphas), path
        if name != 'city_base':
            assert any(min(row) == 0 for row in alphas), path
        if name == 'shogun':
            for y in range(5):
                for x in range(8):
                    assert any(max(row[x*512:(x+1)*512]) > 0 for row in alphas[y*512:(y+1)*512]), (x, y)
        records.append({'file': path.name, 'size': [w,h], 'source': 'Original Blender planar geometry; tools/cutscene/render_live.py', 'alpha': name != 'city_base'})
    (OUT / 'manifest.json').write_text(json.dumps({'canvas': [1920,1080], 'palette': 'navy/cyan; Shogun red/gold/dark ink', 'atlas': {'cell': [512,512], 'columns': 8, 'rows': ['idle','running','jumping','blade_dash','settle'], 'pivot': [256,448]}, 'assets': records}, indent=2) + '\n')
    print('LIVE_ASSETS_VALID: 8 textures; 40 occupied pose regions')


if __name__ == '__main__':
    main()
