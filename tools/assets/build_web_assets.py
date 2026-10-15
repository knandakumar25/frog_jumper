"""Build ORIGINAL 3D assets for the Axiom Force website (not game reuses).

Four pieces, authored for the site's scenes:
  axiom_beacon        -- the brand monument: a hexagonal resistance obelisk with
                         an AXIOM-7 optic core, holo ring and crest. The Home
                         hero's centerpiece, standing in the skyline.
  platform_kiosk      -- a deploy kiosk pod with an angled holo screen and a
                         glyph plate slot. One per platform row on TechHub.
  platform_glyph_*    -- four emissive glyph plates (WINDOWS / LINUX / ANDROID
                         / iOS) that slot into the kiosk's plate rail.
  axiom_orb           -- the AI companion orb: dark shell, optic iris, orbit
                         rings. Hovers in the auth terminal room.

Conventions match the project's builders: metric (1 unit = 1 m), origin at the
bottom-centre, PBR_*/EMISSIVE_* material names, a JSON manifest beside the
GLBs, and a studio preview render per asset.

Run headless:
  blender --background --factory-startup --python tools/assets/build_web_assets.py
Then optimize for the web:
  .\\tools\\optimize_web_models.ps1 -Source assets\\models\\web -OutDir website\\public\\models\\web
"""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT_DIR = ROOT / 'assets' / 'models' / 'web'
PREVIEW_DIR = OUTPUT_DIR / 'previews'
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
PREVIEW_DIR.mkdir(parents=True, exist_ok=True)

MATERIALS = {}
RECORDS = []


# ─── palette ────────────────────────────────────────────────────────────────
def make_mat(name, color, metallic=0.0, roughness=0.4, emission=0.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    if bsdf:
        bsdf.inputs['Base Color'].default_value = (*color, 1.0)
        bsdf.inputs['Metallic'].default_value = metallic
        bsdf.inputs['Roughness'].default_value = roughness
        if emission > 0:
            bsdf.inputs['Emission Color'].default_value = (*color, 1.0)
            bsdf.inputs['Emission Strength'].default_value = emission
    mat.diffuse_color = (*color, 1.0)
    return mat


def init_palette():
    MATERIALS.clear()
    MATERIALS.update({
        # dielectric lacquer + metals, per the character material doctrine
        'armor':     make_mat('PBR_Obsidian_Armor', (.026, .034, .046), 0.0, .38),
        'titanium':  make_mat('PBR_Industrial_Titanium', (.17, .21, .26), .88, .28),
        'trim':      make_mat('PBR_Brushed_Aluminum', (.42, .48, .55), .94, .22),
        'carbon':    make_mat('PBR_Matte_Carbon', (.012, .016, .022), 0.0, .70),
        'cyan':      make_mat('EMISSIVE_Cyan_Pulse', (.005, .85, 1.0), .1, .2, 2.4),
        'magenta':   make_mat('EMISSIVE_Magenta_Trim', (1.0, .01, .45), .1, .2, 3.0),
        'gold':      make_mat('EMISSIVE_Gold_Accent', (1.0, .58, .02), .1, .25, 2.6),
        'optic':     make_mat('EMISSIVE_White_Optic', (.85, .96, 1.0), .1, .25, 3.4),
    })


def reset_scene(name):
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for block in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for item in list(block):
            block.remove(item, do_unlink=True)
    init_palette()
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1.0
    bpy.context.scene['asset_name'] = name


# ─── geometry helpers ───────────────────────────────────────────────────────
def finish(obj, name, mat, bevel=0.0, smooth=False):
    obj.name = name
    obj.data.name = name + '_Data'
    obj.data.materials.append(MATERIALS[mat])
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel > 0:
        mod = obj.modifiers.new('EdgeBevel', 'BEVEL')
        mod.width = bevel
        mod.segments = 2
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    for p in obj.data.polygons:
        p.use_smooth = smooth
    return obj


def box(name, loc, dims, mat='armor', bevel=.02, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.dimensions = dims
    return finish(obj, name, mat, bevel)


def cyl(name, loc, radius, depth, mat='titanium', verts=16, rot=(0, 0, 0), bevel=.012, r2=None, smooth=False):
    if r2 is None:
        bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=radius, depth=depth, location=loc, rotation=rot)
    else:
        bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=radius, radius2=r2, depth=depth, location=loc, rotation=rot)
    return finish(bpy.context.object, name, mat, bevel, smooth)


def orb(name, loc, radius, mat='cyan', segments=24, rings=12, scale=(1, 1, 1)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=radius, location=loc)
    obj = bpy.context.object
    obj.scale = scale
    return finish(obj, name, mat, 0, smooth=True)


def ring(name, loc, radius, minor, mat='cyan', rot=(0, 0, 0), major=48, minor_seg=8):
    bpy.ops.mesh.primitive_torus_add(location=loc, rotation=rot, major_radius=radius, minor_radius=minor,
                                     major_segments=major, minor_segments=minor_seg)
    return finish(bpy.context.object, name, mat, 0, smooth=True)


def label(name, text, loc, size=.1, mat='gold', rot=(math.pi / 2, 0, 0), extrude=.004):
    bpy.ops.object.text_add(location=loc, rotation=rot)
    obj = bpy.context.object
    obj.data.body = text
    obj.data.size = size
    obj.data.align_x = 'CENTER'
    obj.data.align_y = 'CENTER'
    obj.data.extrude = extrude
    bpy.ops.object.convert(target='MESH')
    return finish(bpy.context.object, name, mat, 0)


# ─── asset 1: the brand monument ────────────────────────────────────────────
def build_axiom_beacon():
    # plinth
    cyl('Beacon_Plinth', (0, 0, .18), 1.15, .36, 'armor', 8, bevel=.03)
    cyl('Beacon_PlinthTrim', (0, 0, .38), 1.22, .06, 'trim', 32)
    cyl('Beacon_GroundRing', (0, 0, .02), 1.38, .02, 'cyan', 48)
    # tapered hexagonal shaft with an emissive spine
    cyl('Beacon_Shaft', (0, 0, 1.85), .62, 2.9, 'armor', 6, bevel=.03, r2=.36)
    for i in range(3):
        ang = i * (2 * math.pi / 3)
        box('Beacon_Fin_%d' % i, (math.cos(ang) * .58, math.sin(ang) * .58, 1.9),
            (.07, .34, 2.4), 'trim', bevel=.012, rot=(0, 0, ang))
    cyl('Beacon_SpineCore', (0, 0, 1.95), .17, 2.7, 'cyan', 24)
    for i in range(2):
        ang = i * math.pi
        orb('Beacon_Node_%d' % i, (math.cos(ang) * .5, math.sin(ang) * .5, 1.55), .07, 'magenta')
    # capital + optic
    box('Beacon_Capital', (0, 0, 3.42), (.86, .86, .2), 'armor', bevel=.025)
    cyl('Beacon_Collar', (0, 0, 3.56), .42, .1, 'titanium', 24)
    orb('Beacon_Optic', (0, 0, 3.76), .19, 'optic', 28, 14)
    ring('Beacon_OpticHalo', (0, 0, 3.76), .31, .035, 'cyan')
    # crown prongs + crest
    for i in range(3):
        ang = i * (2 * math.pi / 3) + math.pi / 6
        cyl('Beacon_Prong_%d' % i, (math.cos(ang) * .22, math.sin(ang) * .22, 4.12),
            .035, .9, 'titanium', 10, r2=.012)
    box('Beacon_Crest', (0, .3, 4.3), (.05, .34, .62), 'cyan', bevel=.006)
    # hovering holo ring
    ring('Beacon_HoloRing', (0, 0, 4.05), .92, .014, 'cyan')


# ─── asset 2: the deploy kiosk ───────────────────────────────────────────────
def build_platform_kiosk():
    cyl('Kiosk_Plinth', (0, 0, .09), .55, .18, 'armor', 8, bevel=.025)
    cyl('Kiosk_PlinthRing', (0, 0, .19), .5, .03, 'cyan', 40)
    box('Kiosk_Column', (0, .02, .58), (.28, .24, .72), 'titanium', bevel=.02)
    # screen frame + holo panel
    box('Kiosk_ScreenFrame', (0, .0, 1.12), (.84, .07, .54), 'armor', bevel=.03, rot=(math.radians(-10), 0, 0))
    box('Kiosk_HoloPanel', (0, -.045, 1.12), (.72, .012, .42), 'cyan', bevel=.004, rot=(math.radians(-10), 0, 0))
    # glyph plate rail
    box('Kiosk_PlateRail', (0, -.06, .84), (.54, .03, .17), 'carbon', bevel=.01, rot=(math.radians(-10), 0, 0))
    for i in (-1, 1):
        box('Kiosk_Rail_%d' % i, (i * .4, .01, .8), (.05, .22, .5), 'trim', bevel=.012)
    orb('Kiosk_Beacon', (0, .03, 1.44), .06, 'magenta', 16, 8)
    cyl('Kiosk_Antenna', (0, -.05, 1.58), .012, .34, 'titanium', 8)


# ─── asset 3: glyph plates ──────────────────────────────────────────────────
def build_platform_glyph(text, slug):
    box('Glyph_Plate', (0, 0, .085), (.52, .02, .17), 'carbon', bevel=.008)
    label('Glyph_Text', text, (0, -.016, .085), size=.075, mat='gold')


# ─── asset 4: the companion orb ─────────────────────────────────────────────
def build_axiom_orb():
    orb('Orb_Shell', (0, 0, 0), .3, 'carbon', 32, 16)
    orb('Orb_Iris', (0, -.24, .02), .12, 'optic', 24, 12, scale=(1, .5, 1))
    box('Orb_Pupil', (0, -.315, .02), (.03, .01, .09), 'cyan', bevel=.003, rot=(math.radians(90), 0, 0))
    ring('Orb_Equator', (0, 0, 0), .4, .022, 'titanium')
    ring('Orb_OrbitA', (0, 0, 0), .5, .012, 'cyan', rot=(math.radians(28), math.radians(14), 0))
    ring('Orb_OrbitB', (0, 0, 0), .56, .01, 'magenta', rot=(math.radians(-32), math.radians(-20), 0))
    for i in range(3):
        ang = i * (2 * math.pi / 3)
        orb('Orb_Node_%d' % i, (math.cos(ang) * .5, math.sin(ang) * .5, 0), .038, 'cyan', 12, 6)
    cyl('Orb_Emitter', (0, 0, -.33), .07, .06, 'cyan', 16, r2=.03)


# ─── preview ────────────────────────────────────────────────────────────────
def render_preview(name):
    scene = bpy.context.scene
    low, high = Vector((0, 0, 0)), Vector((0, 0, 0))
    pts = [o.matrix_world @ Vector(c) for o in scene.objects if o.type == 'MESH' for c in o.bound_box]
    low = Vector([min(p[i] for p in pts) for i in range(3)])
    high = Vector([max(p[i] for p in pts) for i in range(3)])
    center = (high + low) / 2
    span = max(max(high - low), .5)

    bpy.ops.mesh.primitive_plane_add(size=span * 20, location=(center.x, center.y, low.z - .005))
    ground = bpy.context.object
    ground.name = 'PreviewGround'
    ground.data.materials.append(make_mat('PreviewGroundMat', (.012, .016, .024), 0, .5))
    for loc, power, color, size in [((span * 1.1, -span * 1.1, span * 2.2), 260, (.9, .95, 1.0), span * .5),
                                    ((-span * 1.6, span * 1.2, span * 1.8), 320, (.3, .6, 1.0), span * .6),
                                    ((-span * 1.0, -span * 1.5, span * .8), 180, (1.0, .5, .3), span * .7)]:
        bpy.ops.object.light_add(type='AREA', location=loc)
        light = bpy.context.object
        light.data.energy = power
        light.data.color = color
        light.data.shape = 'DISK'
        light.data.size = size
        light.rotation_euler = (center - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
    bpy.ops.object.camera_add(location=(span * 1.35, -span * 1.8, center.z + span * .75))
    cam = bpy.context.object
    cam.data.lens = 42
    cam.rotation_euler = (center - cam.location).to_track_quat('-Z', 'Y').to_euler()
    scene.camera = cam
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 24
    scene.render.resolution_x = 640
    scene.render.resolution_y = 640
    scene.view_settings.view_transform = 'Standard'
    scene.world.color = (.04, .05, .06)
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(PREVIEW_DIR / (name + '.png'))
    bpy.ops.render.render(write_still=True)


def export_asset(name, notes):
    for o in bpy.context.scene.objects:
        o.select_set(o.type == 'MESH')
    glb_path = OUTPUT_DIR / (name + '.glb')
    bpy.ops.export_scene.gltf(
        filepath=str(glb_path), export_format='GLB', use_selection=True,
        export_apply=True, export_extras=True, export_yup=True, export_animations=False)
    tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons)
               for o in bpy.context.scene.objects if o.type == 'MESH')
    pts = [o.matrix_world @ Vector(c) for o in bpy.context.scene.objects
           if o.type == 'MESH' for c in o.bound_box]
    low = [round(min(p[i] for p in pts), 3) for i in range(3)]
    high = [round(max(p[i] for p in pts), 3) for i in range(3)]
    RECORDS.append({
        'name': name,
        'path': str(glb_path.relative_to(ROOT)).replace('\\', '/'),
        'bytes': glb_path.stat().st_size,
        'triangles': tris,
        'bbox_m': [round(high[i] - low[i], 3) for i in range(3)],
        'bounds': [low, high],
        'notes': notes,
    })
    try:
        render_preview(name)
    except Exception as exc:  # a preview failure must not lose the asset
        print('  preview failed for %s: %s' % (name, exc))
    print('BUILT %-22s %6d tris  %7.1f KB' % (name, tris, glb_path.stat().st_size / 1024), flush=True)


ASSETS = [
    ('axiom_beacon', build_axiom_beacon, 'Brand monument: hexagonal resistance obelisk with AXIOM-7 optic core, holo ring and crest ~4.3 m tall'),
    ('platform_kiosk', build_platform_kiosk, 'Deploy kiosk pod: plinth, column, angled holo screen, glyph plate rail ~1.5 m tall'),
    ('platform_glyph_windows', lambda: build_platform_glyph('WINDOWS', 'windows'), 'Emissive glyph plate: WINDOWS, fits the kiosk rail'),
    ('platform_glyph_linux', lambda: build_platform_glyph('LINUX', 'linux'), 'Emissive glyph plate: LINUX, fits the kiosk rail'),
    ('platform_glyph_android', lambda: build_platform_glyph('ANDROID', 'android'), 'Emissive glyph plate: ANDROID, fits the kiosk rail'),
    ('platform_glyph_ios', lambda: build_platform_glyph('iOS', 'ios'), 'Emissive glyph plate: iOS, fits the kiosk rail'),
    ('axiom_orb', build_axiom_orb, 'AI companion orb: dark shell, optic iris, orbit rings ~1.1 m across'),
]

for name, builder, notes in ASSETS:
    reset_scene(name)
    builder()
    export_asset(name, notes)

manifest = OUTPUT_DIR / 'web_assets_manifest.json'
manifest.write_text(json.dumps({
    'generator': 'tools/assets/build_web_assets.py',
    'purpose': 'original 3D assets authored for the Axiom Force website (not game reuses)',
    'units': 'meters',
    'origin': 'bottom-centre',
    'assets': RECORDS,
}, indent=2), encoding='utf-8')
print('WEB_ASSETS_COMPLETE %d assets' % len(RECORDS), flush=True)