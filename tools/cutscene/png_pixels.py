"""Pure-Python RGBA8 PNG decoder shared by the cutscene and gameplay media validators.

Moved out of the retired validate_live.py on 2026-11-11 so the validators no longer depend on
Blender-era scripts. It reads only 8-bit RGBA, non-interlaced PNGs: the format the game art uses.
"""
import struct
import zlib


def pixels(path):
    """Return (width, height, rows) for an RGBA8 PNG, undoing all five PNG row filters."""
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
