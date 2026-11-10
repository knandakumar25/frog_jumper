"""Render the localized "Axiom Force" title wordmarks as 3D Blender scenes.

    blender --background --factory-startup --python tools/art/render_title_logo.py -- [--only key ...] [--out <dir>] [--preview]

``--preview`` renders at half size with 32 samples into ``.validation/title_preview``
(unless ``--out`` is given), for checking a design change without touching shipped files.

Title texts reproduce the earlier painted logos exactly (Latin-script languages show the
English "AXIOM FORCE"). They differ from the ``AXIOM FORCE`` row of the translation catalog
for most languages; the owner chose (2026-11-10) to keep this wording.

Replaces the earlier painted title art, which was cut out of a JPEG with a hard black
matte (black fringes, crushed colour, noise). Each title is rebuilt from geometry: a
bevelled gunmetal plate, cyan and magenta emissive neon tubes, a winged-gear crest,
and extruded text with a chrome face. The cyan and magenta neon rims around the text
are drawn in 2D afterwards (``add_rims``), and the plate, which is the same for every
language, is rendered once and reused. Output keeps the file names and the 1920x588
transparent PNG size that ``scripts/ui/TitleLogo.cs`` and the loading screen expect.

Compositing works on the 8-bit display-encoded (sRGB, after AgX) values of the rendered
PNGs, as Godot composites 2D UI, and the rim colours were tuned by eye on that basis. The
PNGs are pinned to 8 bits and ``render_layer`` refuses float buffers, because a 16-bit
render would load as linear data and silently darken every title.

Complex scripts: Blender's 3D text does no shaping or bidirectional layout. Hebrew is
therefore reversed into visual order here, and Arabic is first converted to its
contextual presentation forms (``shape_arabic``), then reversed. Thai, Vietnamese and
CJK titles were inspected in Blender 5.2 output with no overlapping or misplaced marks;
Blender does no mark positioning, so recheck Thai after any title change.

Before anything is rendered, every requested title is shaped and checked: ``check_glyphs``
raises ``ValueError`` if a font lacks a character (Blender would otherwise draw an empty box),
and ``shape_arabic`` raises on text it cannot shape. So a failure never leaves a mix of old
and new titles.

Side effects: writes ``assets/images/axiom_force_title_2d*.png`` (or ``--out``); a full
render into the default folder also overwrites the copies in ``website/public/images`` and
``website/public/images/title_logos`` (``WEBSITE_DIRS``, ``WEBSITE_EXTRA``). Every run writes
its intermediate layers to ``.validation/title_layers``.
"""
import struct
import argparse
import math
import sys
from pathlib import Path

import bpy
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools/art'))
import render_quality  # noqa: E402

FONTS = ROOT / 'assets/fonts'
W, H = 1920, 588
UNIT = 100.0  # pixels per Blender unit; the camera spans W/UNIT units

# key -> (title text, font file, every file-name suffix that shows this title). Cairo and
# Tajawal lack the isolated Arabic forms of ة, أ and م that this title uses, so Arabic
# uses Noto Sans Arabic.
TITLES = {
    'english': ('AXIOM FORCE', 'Orbitron-VariableFont.ttf',
                ['', '_es', '_spanish', '_fr', '_eurofrench', '_pt', '_brazilianportuguese', '_de', '_german',
                 '_it', '_italian', '_pl', '_polish', '_tr', '_turkish']),
    'arabic': ('قوة أكسيوم', 'NotoSansArabic.ttf', ['_ar', '_arabic']),
    'japanese': ('アクシオム・フォース', 'NotoSansCJKjp-Regular.otf', ['_ja', '_japanese']),
    'korean': ('엑시옴 포스', 'NotoSansCJKkr-Regular.otf', ['_ko', '_korean']),
    'mandarin': ('公理部队', 'NotoSansCJKsc-Regular.otf', ['_zh', '_mandarin']),
    'hebrew': ('כוח אקסיום', 'NotoSansHebrew.ttf', ['_he', '_hebrew']),
    'traditionalchinese': ('公理部隊', 'NotoSansCJKtc-Regular.otf', ['_traditionalchinese']),
    'indonesian': ('PASUKAN AKSIOMA', 'Orbitron-VariableFont.ttf', ['_id', '_indonesian']),
    'vietnamese': ('LỰC LƯỢNG TIÊN ĐỀ', 'NotoSans-Regular.ttf', ['_vi', '_vietnamese']),
    'thai': ('กองกำลังสัจพจน์', 'NotoSansThai.ttf', ['_th', '_thai']),
}

# Fonts rendered without the chrome edge bevel (see build_text).
FLAT_EDGE_FONTS = {'NotoSansArabic.ttf'}

# Arabic presentation forms: letter -> (isolated, final, initial, medial). Letters with
# only two forms join to the previous letter but never to the next one.
ARABIC_FORMS = {
    'ا': (0xFE8D, 0xFE8E), 'أ': (0xFE83, 0xFE84), 'إ': (0xFE87, 0xFE88), 'آ': (0xFE81, 0xFE82),
    'د': (0xFEA9, 0xFEAA), 'ذ': (0xFEAB, 0xFEAC), 'ر': (0xFEAD, 0xFEAE), 'ز': (0xFEAF, 0xFEB0),
    'و': (0xFEED, 0xFEEE), 'ة': (0xFE93, 0xFE94), 'ى': (0xFEEF, 0xFEF0),
    'ب': (0xFE8F, 0xFE90, 0xFE91, 0xFE92), 'ت': (0xFE95, 0xFE96, 0xFE97, 0xFE98),
    'ث': (0xFE99, 0xFE9A, 0xFE9B, 0xFE9C), 'ج': (0xFE9D, 0xFE9E, 0xFE9F, 0xFEA0),
    'ح': (0xFEA1, 0xFEA2, 0xFEA3, 0xFEA4), 'خ': (0xFEA5, 0xFEA6, 0xFEA7, 0xFEA8),
    'س': (0xFEB1, 0xFEB2, 0xFEB3, 0xFEB4), 'ش': (0xFEB5, 0xFEB6, 0xFEB7, 0xFEB8),
    'ص': (0xFEB9, 0xFEBA, 0xFEBB, 0xFEBC), 'ض': (0xFEBD, 0xFEBE, 0xFEBF, 0xFEC0),
    'ط': (0xFEC1, 0xFEC2, 0xFEC3, 0xFEC4), 'ظ': (0xFEC5, 0xFEC6, 0xFEC7, 0xFEC8),
    'ع': (0xFEC9, 0xFECA, 0xFECB, 0xFECC), 'غ': (0xFECD, 0xFECE, 0xFECF, 0xFED0),
    'ف': (0xFED1, 0xFED2, 0xFED3, 0xFED4), 'ق': (0xFED5, 0xFED6, 0xFED7, 0xFED8),
    'ك': (0xFED9, 0xFEDA, 0xFEDB, 0xFEDC), 'ل': (0xFEDD, 0xFEDE, 0xFEDF, 0xFEE0),
    'م': (0xFEE1, 0xFEE2, 0xFEE3, 0xFEE4), 'ن': (0xFEE5, 0xFEE6, 0xFEE7, 0xFEE8),
    'ه': (0xFEE9, 0xFEEA, 0xFEEB, 0xFEEC), 'ي': (0xFEF1, 0xFEF2, 0xFEF3, 0xFEF4),
}


def shape_arabic(text):
    """Return ``text`` in contextual presentation forms, in visual (left-to-right) order.

    A letter takes its initial/medial/final form depending on whether its neighbours
    join to it. The result is reversed because Blender lays text out left to right.

    Only plain Arabic letters and spaces are supported: there is no lam-alef ligature,
    diacritics would wrongly break joining, and reversal would also reverse digits or
    Latin text. Anything else raises ``ValueError`` instead of rendering wrongly.
    """
    unsupported = sorted({ch for ch in text if ch != ' ' and ch not in ARABIC_FORMS})
    if unsupported or 'لا' in text or 'لأ' in text or 'لإ' in text or 'لآ' in text:
        raise ValueError(f'shape_arabic cannot shape {text!r}: unsupported {unsupported} or lam-alef')

    def joins_next(ch):
        return ch in ARABIC_FORMS and len(ARABIC_FORMS[ch]) == 4

    out = []
    for i, ch in enumerate(text):
        forms = ARABIC_FORMS.get(ch)
        if not forms:
            out.append(ch)
            continue
        prev_joins = i > 0 and joins_next(text[i - 1])
        next_joins = joins_next(ch) and i + 1 < len(text) and text[i + 1] in ARABIC_FORMS
        if prev_joins and next_joins:
            code = forms[3]
        elif prev_joins:
            code = forms[1]
        elif next_joins:
            code = forms[2]
        else:
            code = forms[0]
        out.append(chr(code))
    return ''.join(reversed(out))


def font_characters(path):
    """Return the set of code points mapped in a TrueType/OpenType font's ``cmap`` table.

    Reads cmap subtables of format 4 (Basic Multilingual Plane) and 12 (full range) with
    the standard library only, since Blender's Python has no font library.
    """
    data = Path(path).read_bytes()
    count = struct.unpack('>H', data[4:6])[0]
    tables = {data[12 + 16 * i:16 + 16 * i]: struct.unpack('>I', data[20 + 16 * i:24 + 16 * i])[0]
              for i in range(count)}
    base = tables[b'cmap']
    found = set()
    for i in range(struct.unpack('>H', data[base + 2:base + 4])[0]):
        offset = struct.unpack('>I', data[base + 8 + 8 * i:base + 12 + 8 * i])[0]
        t = base + offset
        fmt = struct.unpack('>H', data[t:t + 2])[0]
        if fmt == 4:
            segs = struct.unpack('>H', data[t + 6:t + 8])[0] // 2
            ends = struct.unpack(f'>{segs}H', data[t + 14:t + 14 + 2 * segs])
            p = t + 16 + 2 * segs
            starts = struct.unpack(f'>{segs}H', data[p:p + 2 * segs])
            deltas = struct.unpack(f'>{segs}h', data[p + 2 * segs:p + 4 * segs])
            ranges_at = p + 4 * segs
            for k, (start, end, delta) in enumerate(zip(starts, ends, deltas)):
                range_offset = struct.unpack('>H', data[ranges_at + 2 * k:ranges_at + 2 * k + 2])[0]
                for code in range(start, min(end, 0xFFFE) + 1):
                    if range_offset == 0:
                        glyph = (code + delta) & 0xFFFF
                    else:
                        # The offset is relative to its own position in the idRangeOffset array.
                        at = ranges_at + 2 * k + range_offset + 2 * (code - start)
                        glyph = struct.unpack('>H', data[at:at + 2])[0]
                        glyph = (glyph + delta) & 0xFFFF if glyph else 0
                    if glyph:
                        found.add(code)
        elif fmt == 12:
            for g in range(struct.unpack('>I', data[t + 12:t + 16])[0]):
                start, end, _ = struct.unpack('>III', data[t + 16 + 12 * g:t + 28 + 12 * g])
                found.update(range(start, end + 1))
    return found


def check_glyphs(text, font_file):
    """Raise ``ValueError`` if ``font_file`` has no glyph for a character of ``text``."""
    available = font_characters(FONTS / font_file)
    missing = [f'U+{ord(ch):04X}' for ch in dict.fromkeys(text) if ch != ' ' and ord(ch) not in available]
    if missing:
        raise ValueError(f'{font_file} lacks {missing} needed by {text!r}')


def visual_text(key, text):
    """Convert a title to the left-to-right glyph order Blender needs."""
    if key == 'arabic':
        return shape_arabic(text)
    if key == 'hebrew':
        return text[::-1]  # Hebrew letters do not join; order is the only fix needed.
    return text


# ---------------------------------------------------------------- materials
def principled(name, color, metallic=0.0, roughness=0.5, emission=None, strength=0.0):
    """Create a Principled BSDF material; ``emission`` makes it glow at ``strength``."""
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes['Principled BSDF']
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Metallic'].default_value = metallic
    bsdf.inputs['Roughness'].default_value = roughness
    if emission:
        bsdf.inputs['Emission Color'].default_value = (*emission, 1)
        bsdf.inputs['Emission Strength'].default_value = strength
    return mat


def chrome_face():
    """Chrome-like face: a vertical colour ramp across the height of the whole text object.

    An orthographic front camera sees flat faces at one angle, so real reflections would
    be a single flat tone; a strongly metallic face only mirrors the dark world and hides
    the ramp (the first trial rendered dull grey). So the ramp over the object's generated
    Y sets the colour directly, a little of it is emitted so the bright band reads on the
    dark plate, and low metallic keeps specular highlights on the bevels.
    """
    mat = principled('Chrome face', (0.8, 0.85, 0.9), metallic=0.25, roughness=0.25)
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    coord = nodes.new('ShaderNodeTexCoord')
    split = nodes.new('ShaderNodeSeparateXYZ')
    ramp = nodes.new('ShaderNodeValToRGB')
    stops = [(0.0, (0.10, 0.13, 0.20)), (0.42, (0.42, 0.50, 0.62)), (0.52, (0.96, 0.98, 1.0)),
             (0.58, (0.20, 0.24, 0.33)), (0.80, (0.62, 0.70, 0.82)), (1.0, (0.92, 0.96, 1.0))]
    ramp.color_ramp.elements[0].position, ramp.color_ramp.elements[0].color = stops[0][0], (*stops[0][1], 1)
    ramp.color_ramp.elements[1].position, ramp.color_ramp.elements[1].color = stops[-1][0], (*stops[-1][1], 1)
    for pos, col in stops[1:-1]:
        ramp.color_ramp.elements.new(pos).color = (*col, 1)
    links.new(coord.outputs['Generated'], split.inputs[0])
    links.new(split.outputs['Y'], ramp.inputs[0])
    bsdf = nodes['Principled BSDF']
    links.new(ramp.outputs[0], bsdf.inputs['Base Color'])
    links.new(ramp.outputs[0], bsdf.inputs['Emission Color'])
    bsdf.inputs['Emission Strength'].default_value = 0.35
    return mat


# ---------------------------------------------------------------- geometry
def link(obj):
    """Add ``obj`` to the scene and return it."""
    bpy.context.scene.collection.objects.link(obj)
    return obj


def prism(name, outline, z, depth, mat, bevel=0.03):
    """Extrude a simple (non-self-intersecting) 2D outline in Blender units into a bevelled slab at ``z``."""
    n = len(outline)
    verts = [(x, y, z) for x, y in outline] + [(x, y, z - depth) for x, y in outline]
    faces = [tuple(range(n)), tuple(range(2 * n - 1, n - 1, -1))]
    faces += [(i, (i + 1) % n, n + (i + 1) % n, n + i) for i in range(n)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    mesh.materials.append(mat)
    obj = link(bpy.data.objects.new(name, mesh))
    if bevel:
        mod = obj.modifiers.new('Bevel', 'BEVEL')
        mod.width, mod.segments, mod.limit_method = bevel, 3, 'ANGLE'
    return obj


def tube(name, points, z, radius, mat, closed=False):
    """A round neon tube along a 2D polyline at height ``z``."""
    curve = bpy.data.curves.new(name, 'CURVE')
    curve.dimensions = '3D'
    curve.bevel_depth, curve.bevel_resolution = radius, 4
    spline = curve.splines.new('POLY')
    spline.points.add(len(points) - 1)
    for p, (x, y) in zip(spline.points, points):
        p.co = (x, y, z, 1)
    spline.use_cyclic_u = closed
    curve.materials.append(mat)
    return link(bpy.data.objects.new(name, curve))


def ring(name, centre, inner, outer, teeth, z, depth, mat):
    """A gear: an annulus whose outer edge has ``teeth`` square teeth."""
    cx, cy = centre
    steps = teeth * 4
    outer_pts, inner_pts = [], []
    for i in range(steps):
        a = i / steps * math.tau
        r = outer if (i % 4) in (1, 2) else outer * 0.84
        outer_pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
        inner_pts.append((cx + math.cos(a) * inner, cy + math.sin(a) * inner))
    verts, faces = [], []
    for zz in (z, z - depth):
        verts += [(x, y, zz) for x, y in outer_pts] + [(x, y, zz) for x, y in inner_pts]
    o, i_, b = 0, steps, 2 * steps
    for k in range(steps):
        k1 = (k + 1) % steps
        faces.append((o + k, o + k1, i_ + k1, i_ + k))                    # front annulus
        faces.append((b + i_ + k, b + i_ + k1, b + o + k1, b + o + k))    # back annulus
        faces.append((o + k, b + o + k, b + o + k1, o + k1))              # outer wall
        faces.append((i_ + k1, b + i_ + k1, b + i_ + k, i_ + k))          # inner wall
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    mesh.materials.append(mat)
    obj = link(bpy.data.objects.new(name, mesh))
    mod = obj.modifiers.new('Bevel', 'BEVEL')
    mod.width, mod.segments, mod.limit_method = 0.015, 2, 'ANGLE'
    return obj


def mirror(points):
    """Mirror a list of 2D points across x = 0 (for symmetric parts)."""
    return [(-x, y) for x, y in points]


def build_frame(m):
    """Plate, inset panel, neon trims, hex details and the winged-gear crest."""
    # Outer plate: wide body with chamfered ends and a raised centre tab for the crest.
    half = [(0, 1.95), (1.25, 1.95), (2.35, 0.95), (8.55, 0.95), (9.35, 0.25), (9.35, -1.95),
            (8.75, -2.55), (0, -2.55)]
    right = half[1:-1]
    outline = [(0, 1.95)] + right + [(0, -2.55)] + list(reversed(mirror(right)))
    prism('Plate', outline, 0.0, 0.35, m['gunmetal'], bevel=0.06)
    inset = [(x * 0.965, y * 0.94 - 0.05) for x, y in outline]
    prism('Inset', inset, 0.06, 0.06, m['panel'], bevel=0.02)
    # Neon outline tubes: cyan around the inset, magenta accents along top and bottom.
    tube('Cyan trim', inset, 0.09, 0.035, m['cyan'], closed=True)
    for sign in (1, -1):
        tube('Magenta top', [(sign * 2.6, 0.72), (sign * 5.4, 0.72), (sign * 5.8, 0.52)], 0.1, 0.03, m['magenta'])
        tube('Magenta bottom', [(sign * 1.0, -2.25), (sign * 6.9, -2.25), (sign * 7.4, -1.95)], 0.1, 0.03, m['magenta'])
        # End caps: glowing vertical light bars, matching the side lamps of the replaced painted title.
        prism('Side lamp', [(sign * 9.0, -1.75), (sign * 9.2, -1.75), (sign * 9.2, 0.05), (sign * 9.0, 0.05)],
              0.12, 0.08, m['cyan_soft'], bevel=0.02)
        # Small hexagon outlines near each end.
        for row, col in [(0, 0), (1, 0.5), (2, 0), (1, -0.5)]:
            # Four hexes in a diamond: rows step up 0.42 units, the middle row sits 0.5 units in or out.
            hx, hy = sign * (8.2 - col), -1.55 + row * 0.42
            hexagon = [(hx + 0.2 * math.cos(a), hy + 0.2 * math.sin(a))
                       for a in [k * math.tau / 6 for k in range(6)]]
            tube('Hex', hexagon, 0.1, 0.012, m['cyan_dim'], closed=True)
    # Crest: gear ring, gem, and two fanned wings of chrome feathers.
    ring('Gear', (0, 1.62), 0.32, 0.62, 10, 0.4, 0.14, m['chrome'])
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.17, location=(0, 1.62, 0.42))
    gem = bpy.context.object
    gem.data.materials.append(m['magenta_gem'])
    for sign in (1, -1):
        for k in range(5):
            a = math.radians(12 + k * 11)
            length = 1.15 + k * 0.28
            root = (sign * 0.55, 1.45 + k * 0.03)
            tip = (root[0] + sign * math.cos(a) * length, root[1] + math.sin(a) * length * 0.55)
            nx, ny = -math.sin(a) * 0.11, math.cos(a) * 0.11
            feather = [root, (root[0] + nx * sign, root[1] + ny), (tip[0], tip[1] + 0.02),
                       (tip[0] - sign * 0.12, tip[1] - 0.05), (root[0], root[1] - 0.12)]
            if sign < 0:
                feather = list(reversed(feather))
            prism('Feather', feather, 0.36 - k * 0.01, 0.06, m['chrome'], bevel=0.012)


def mesh_size(obj):
    """Width and height of ``obj``'s evaluated mesh, in its local units.

    ``Object.dimensions`` of a text object is computed from curve control points, so it
    can be smaller than the generated surface; the evaluated mesh is what renders.
    """
    bpy.context.view_layer.update()
    evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = evaluated.to_mesh()
    try:
        xs = [v.co.x for v in mesh.vertices]
        ys = [v.co.y for v in mesh.vertices]
        return max(xs) - min(xs), max(ys) - min(ys)
    finally:
        evaluated.to_mesh_clear()


def build_text(key, text, font_file, m):
    """Add the chrome title text, fitted inside the plate, and return its object.

    Only the chrome face is geometry. Its neon rims are drawn afterwards in 2D by
    ``add_rims``: growing text outlines in 3D (curve ``offset`` or a large
    ``bevel_depth``) threw spikes on sharp corners (Arabic, Hebrew, Japanese), and the
    overlapping glyph outlines of the Korean font misfilled whole regions. The small fixed
    bevel here only catches highlights; Noto Sans Arabic gets none (``FLAT_EDGE_FONTS``).
    """
    font = bpy.data.fonts.load(str(FONTS / font_file))
    body = visual_text(key, text)
    check_glyphs(body, font_file)
    curve = bpy.data.curves.new('Chrome text', 'FONT')
    curve.body = body
    curve.font = font
    curve.align_x, curve.align_y = 'CENTER', 'CENTER'
    curve.size = 1.0
    curve.extrude = 0.06
    # Noto Sans Arabic's hamza on alef has a sharp cusp that even this bevel throws into a
    # long thin spike, so that font gets a flat edge; its rims still outline it.
    curve.bevel_depth = 0.0 if font_file in FLAT_EDGE_FONTS else 0.018
    curve.bevel_resolution = 3
    curve.materials.append(m['chrome_face'])
    obj = link(bpy.data.objects.new('Chrome text', curve))
    obj.location = (0, 0, 0.36)
    # Fit: at most 14.4 units wide and 2.2 tall. The rims and their glow reach about 0.4
    # units beyond the text; wider text put the Thai glow on the end hexagons, and taller
    # CJK text overlapped the trim lines.
    width, height = mesh_size(obj)
    scale = min(14.4 / max(width, 1e-6), 2.2 / max(height, 1e-6))
    obj.scale = (scale, scale, 1)
    obj.location.y = -0.85
    return obj


# Rim layers drawn by add_rims, bottom to top: (core colour, glow colour, radius px,
# glow px, shift px right/up). Colours are display values chosen to match the earlier
# emissive renders (neon tones that read pale after AgX). The magenta rim sits slightly
# down-left so it reads as light cast by the letters.
RIMS = [((1.0, 0.66, 0.92), (1.0, 0.25, 0.75), 12, 10, (-2, -3)),
        ((0.66, 0.98, 1.0), (0.2, 0.85, 1.0), 5, 6, (0, 0))]


def dilate(alpha, radius):
    """Grow an alpha mask by ``radius`` pixels with a disc kernel (max of shifted copies).

    Working on the anti-aliased alpha keeps the grown edge anti-aliased too.
    """
    out = alpha.copy()
    h, w = alpha.shape
    for dy in range(-radius, radius + 1):
        for dx in range(-radius, radius + 1):
            if dx * dx + dy * dy > radius * radius or (dx == 0 and dy == 0):
                continue
            ys, yd = (slice(0, h - dy), slice(dy, h)) if dy >= 0 else (slice(-dy, h), slice(0, h + dy))
            xs, xd = (slice(0, w - dx), slice(dx, w)) if dx >= 0 else (slice(-dx, w), slice(0, w + dx))
            np.maximum(out[yd, xd], alpha[ys, xs], out=out[yd, xd])
    return out


def blur(alpha, radius):
    """Approximate Gaussian blur: three passes of a separable box filter of ``radius`` px."""
    out = alpha
    for _ in range(3):
        for axis in (0, 1):
            pad = [(0, 0), (0, 0)]
            pad[axis] = (radius + 1, radius)
            c = np.cumsum(np.pad(out, pad, mode='edge'), axis=axis)
            n = out.shape[axis]
            hi = np.take(c, range(2 * radius + 1, 2 * radius + 1 + n), axis=axis)
            lo = np.take(c, range(0, n), axis=axis)
            out = (hi - lo) / (2 * radius + 1)
    return out


def over(top, bottom):
    """Composite straight-alpha RGBA ``top`` over ``bottom`` (both H x W x 4 floats)."""
    ta, ba = top[..., 3:4], bottom[..., 3:4]
    a = ta + ba * (1 - ta)
    rgb = np.where(a > 0, (top[..., :3] * ta + bottom[..., :3] * ba * (1 - ta)) / np.maximum(a, 1e-6), 0)
    return np.concatenate([rgb, a], axis=-1)


def add_rims(plate, text, scale=1.0):
    """Return plate + neon rims + text, with the rims grown from the text's alpha.

    ``scale`` shrinks the pixel radii for half-size previews. Arrays are Blender's
    bottom-up pixel order, so a positive row shift moves a layer up. ``np.roll`` wraps
    shifted pixels to the opposite edge; the text sits far enough from every edge (rows
    about 99-319 of 588) that nothing wraps.
    """
    alpha = text[..., 3]
    result = plate
    for core, glow, radius, glow_px, (sx, sy) in RIMS:
        r, g = max(1, round(radius * scale)), max(1, round(glow_px * scale))
        rim = np.roll(dilate(alpha, r), (round(sy * scale), round(sx * scale)), axis=(0, 1))
        halo = np.clip(blur(rim, g) * 0.6, 0, 1)
        for colour, layer in ((glow, halo), (core, rim)):
            rgba = np.empty(text.shape, dtype=np.float32)
            rgba[..., :3] = colour
            rgba[..., 3] = layer
            result = over(rgba, result)
    return over(text, result)


def render_layer(path):
    """Render the current scene to ``path`` and return its pixels (bottom-up RGBA floats).

    Raises ``RuntimeError`` if Blender loads the PNG as a float (linear) buffer; see the
    module docstring on colour handling.
    """
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    image = bpy.data.images.load(str(path), check_existing=False)
    try:
        if image.is_float:
            raise RuntimeError(f'{path} loaded as linear float data; title layers must be 8-bit PNGs')
        w, h = image.size
        data = np.empty(w * h * 4, dtype=np.float32)
        image.pixels.foreach_get(data)
        return data.reshape(h, w, 4)
    finally:
        bpy.data.images.remove(image)


def save_png(array, path):
    """Write a bottom-up RGBA float array as an 8-bit PNG through Blender."""
    h, w, _ = array.shape
    image = bpy.data.images.new('title', w, h, alpha=True)
    image.pixels.foreach_set(np.ascontiguousarray(array, dtype=np.float32).ravel())
    image.filepath_raw = str(Path(path).resolve())
    image.file_format = 'PNG'
    image.save()
    bpy.data.images.remove(image)


def scene_setup():
    """Empty the scene, add an orthographic front camera and two area lights, and apply render_quality.apply_lit."""
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    scene = bpy.context.scene
    cam = bpy.data.cameras.new('Title camera')
    cam.type, cam.ortho_scale = 'ORTHO', W / UNIT
    cam_obj = link(bpy.data.objects.new('Camera', cam))
    cam_obj.location = (0, 0, 30)
    scene.camera = cam_obj
    for loc, energy in [((-6, 6, 9), 900), ((7, -3, 7), 500)]:
        light = bpy.data.lights.new('Key', 'AREA')
        light.energy, light.size = energy, 8
        obj = link(bpy.data.objects.new('Light', light))
        obj.location = loc
        obj.rotation_euler = (-obj.location).to_track_quat('-Z', 'Y').to_euler()
    world = scene.world or bpy.data.worlds.new('World')
    scene.world = world
    world.use_nodes = True
    # The world is hidden by the transparent film but still lights and reflects; a mid
    # blue-grey keeps metal parts (wings, gear) from mirroring pure black.
    world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.16, 0.2, 0.27, 1)
    render_quality.apply_lit(scene)
    scene.render.resolution_x, scene.render.resolution_y = W, H
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'
    # 8-bit keeps layer pixels display-encoded for compositing (see render_layer).
    scene.render.image_settings.color_depth = '8'
    scene.view_settings.view_transform = 'AgX'
    scene.view_settings.look = 'AgX - Punchy'


def materials():
    """Shared palette: gunmetal plate, neon cyan/magenta, chrome and the chrome text face."""
    return {
        'gunmetal': principled('Gunmetal', (0.035, 0.045, 0.06), metallic=0.75, roughness=0.38),
        'panel': principled('Panel', (0.018, 0.024, 0.035), metallic=0.5, roughness=0.5),
        'cyan': principled('Neon cyan', (0.2, 0.9, 1.0), emission=(0.25, 0.92, 1.0), strength=7),
        'cyan_soft': principled('Lamp cyan', (0.6, 0.95, 1.0), emission=(0.55, 0.95, 1.0), strength=4),
        'cyan_dim': principled('Dim cyan', (0.1, 0.4, 0.45), emission=(0.2, 0.75, 0.85), strength=1.5),
        'magenta': principled('Neon magenta', (1.0, 0.3, 0.75), emission=(1.0, 0.25, 0.75), strength=6),
        'magenta_gem': principled('Gem', (1.0, 0.3, 0.8), emission=(1.0, 0.3, 0.8), strength=9),
        'chrome': principled('Chrome', (0.78, 0.82, 0.9), metallic=1.0, roughness=0.18),
        'chrome_face': chrome_face(),
    }


# The website keeps its own copies (``website/src/translations/titleLogo.js`` loads them),
# including names the game does not use. Full renders into assets/images refresh these too.
WEBSITE_DIRS = [ROOT / 'website/public/images', ROOT / 'website/public/images/title_logos']
WEBSITE_EXTRA = {'english': ['_en', '_english', '_french', '_portuguese'],
                 'mandarin': ['_simplified_chinese'],
                 'traditionalchinese': ['_traditional_chinese', '_zh_tw']}


def main():
    """Render each requested title and copy it to every file-name suffix listed for it.

    A full-size render into the default ``assets/images`` also copies each title into
    ``WEBSITE_DIRS`` under the game suffixes plus ``WEBSITE_EXTRA``.
    """
    parser = argparse.ArgumentParser()
    parser.add_argument('--only', nargs='*', choices=sorted(TITLES))
    parser.add_argument('--out', type=Path)
    parser.add_argument('--preview', action='store_true', help='half size, 32 samples')
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else [])
    shipped = args.out is None and not args.preview
    if args.out is None:
        args.out = ROOT / ('.validation/title_preview' if args.preview else 'assets/images')
    args.out.mkdir(parents=True, exist_ok=True)
    selected = {key: value for key, value in TITLES.items() if not args.only or key in args.only}
    # Shape and check every title first, so a bad font or text stops the run before any file changes.
    for key, (text, font, _) in selected.items():
        check_glyphs(visual_text(key, text), font)
    scene_setup()
    m = materials()
    build_frame(m)
    frame = [obj for obj in bpy.context.scene.objects if obj.type not in ('CAMERA', 'LIGHT')]
    if args.preview:
        bpy.context.scene.render.resolution_percentage = 50
        bpy.context.scene.cycles.samples = 32
    layers = ROOT / '.validation/title_layers'
    layers.mkdir(parents=True, exist_ok=True)
    # The plate is the same for every language, so it is rendered once.
    plate = render_layer(layers / 'plate.png')
    for obj in frame:
        obj.hide_render = True
    for key, (text, font, suffixes) in selected.items():
        words = build_text(key, text, font, m)
        title = render_layer(layers / f'text_{key}.png')
        bpy.data.objects.remove(words)
        first = args.out / f'axiom_force_title_2d{suffixes[0]}.png'
        save_png(add_rims(plate, title, 0.5 if args.preview else 1.0), first)
        image = first.read_bytes()
        for suffix in suffixes[1:]:
            (args.out / f'axiom_force_title_2d{suffix}.png').write_bytes(image)
        if shipped:
            for folder in WEBSITE_DIRS:
                for suffix in suffixes + WEBSITE_EXTRA.get(key, []):
                    (folder / f'axiom_force_title_2d{suffix}.png').write_bytes(image)
        print(f'TITLE {key}: {len(suffixes)} file(s)' + (' + website copies' if shipped else ''))


if __name__ == '__main__':
    main()
