"""Build the Neon Pulse modular environment kit with Blender 5.x.

Run: blender --background --python tools/assets/build_architecture.py
All dimensions are meters; Blender Z-up is exported as glTF Y-up.
No downloaded assets, add-ons, external textures, or gameplay changes required.
"""
import bpy
import json
import math
import sys
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "assets/models/props"
PREVIEW = ROOT / ".validation/asset-previews"
OUTPUT.mkdir(parents=True, exist_ok=True)
PREVIEW.mkdir(parents=True, exist_ok=True)
M = {}
records = []


def material(name, color, metallic=0.0, roughness=0.4, emission=0.0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get("Principled BSDF")
    shader.inputs['Base Color'].default_value = (*color, 1)
    shader.inputs['Metallic'].default_value = metallic
    shader.inputs['Roughness'].default_value = roughness
    if emission:
        shader.inputs['Emission Color'].default_value = (*color, 1)
        shader.inputs['Emission Strength'].default_value = emission
    mat['pulse_channel'] = name if emission else ''
    return mat


def reset(name):
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    M.clear()
    M.update({
        'black': material('Armor_Obsidian_Matte', (0.024, 0.031, 0.043), .65, .48),
        'steel': material('Industrial_Titanium', (.17, .21, .26), .85, .32),
        'light': material('Brushed_Aluminum', (.36, .43, .5), .9, .28),
        'floor': material('Polished_Obsidian', (.016, .025, .04), .72, .15),
        'rubber': material('Cable_Insulation', (.007, .012, .019), .1, .8),
        'cyan': material('Emissive_Cyan_Pulse', (.005, .65, 1), .15, .24, 3),
        'magenta': material('Emissive_Magenta_Pulse', (1, .01, .25), .1, .24, 3),
        'gold': material('Emissive_Gold_Pulse', (1, .57, .02), .15, .24, 3),
        'white': material('Emissive_White_Signage', (.65, .88, 1), .1, .28, 2),
    })
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1.0
    bpy.context.scene['asset_name'] = name


def finish(obj, name, mat, bevel=0):
    obj.name = name
    obj.data.materials.append(M[mat])
    if bevel:
        mod = obj.modifiers.new('Manufactured_edge_radius', 'BEVEL')
        mod.width, mod.segments = bevel, 2
        obj.modifiers.new('Weighted_surface_normals', 'WEIGHTED_NORMAL')
    return obj


def box(name, loc, size, mat='black', bevel=.02):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, name, mat, min(bevel, min(size) / 4))


def cylinder(name, loc, radius, depth, mat='steel', vertices=32):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc)
    return finish(bpy.context.object, name, mat, .015)


def tube(name, a, b, radius, mat='steel'):
    a, b = Vector(a), Vector(b)
    obj = cylinder(name, (a + b) / 2, radius, (b - a).length, mat, 12)
    obj.rotation_euler = (b - a).to_track_quat('Z', 'Y').to_euler()
    return obj


def ring(name, loc, radius, width=.025, mat='cyan'):
    bpy.ops.mesh.primitive_torus_add(major_segments=64, minor_segments=8,
        location=loc, major_radius=radius, minor_radius=width)
    return finish(bpy.context.object, name, mat)


def text(name, words, loc, size=.2, mat='white', rotation=(math.pi / 2, 0, 0)):
    curve = bpy.data.curves.new(name, 'FONT')
    curve.body = words
    curve.align_x, curve.align_y = 'CENTER', 'CENTER'
    curve.size, curve.extrude, curve.bevel_depth = size, .0015, .0005
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    obj.location, obj.rotation_euler = loc, rotation
    curve.materials.append(M[mat])
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.convert(target='MESH')
    obj.select_set(False)
    return obj


def fasteners(x, y, z, width, height):
    for dx in (-width / 2 + .08, width / 2 - .08):
        for dz in (-height / 2 + .08, height / 2 - .08):
            bolt = cylinder('Panel_fastener', (x + dx, y, z + dz), .023, .012, 'light', 6)
            bolt.rotation_euler.x = math.pi / 2


def wall():
    box('Wall_structural_panel_4m', (0, 0, 1.6), (4, .28, 3.2))
    for x in (-1.83, 1.83):
        box('Load_bearing_stile', (x, -.1, 1.6), (.18, .22, 3.2), 'steel')
        box('Vertical_neon_trim', (x, -.23, 1.6), (.025, .015, 2.9), 'cyan', .005)
    for i in range(4):
        x = -1.32 + i * .88
        box('Removable_armor_panel', (x, -.17, 1.72), (.8, .12, 2.48), 'black', .035)
        fasteners(x, -.238, 1.72, .8, 2.48)
        for j in range(5):
            box('Lower_vent_slot', (x, -.242, .5 + j * .064), (.52, .01, .028), 'rubber', 0)
    box('Cyan_header_rail', (0, -.22, 3.02), (3.36, .02, .027), 'cyan', .005)
    box('Steel_kick_plate', (0, -.2, .15), (3.56, .12, .3), 'steel')
    text('Panel_ID', 'AXIOM  /  W-04', (0, -.238, 2.65), .1)


def floor():
    box('Floor_tile_base_4m', (0, 0, .1), (4, 4, .2), 'black')
    for x in (-1, 1):
        for y in (-1, 1):
            box('Obsidian_inlay', (x, y, .21), (1.92, 1.92, .08), 'floor')
    for v in (-1.86, 1.86):
        box('Neon_edge_X', (v, 0, .258), (.024, 3.74, .01), 'cyan', .002)
        box('Neon_edge_Y', (0, v, .258), (3.74, .024, .01), 'cyan', .002)
    for i in range(15):
        box('Drain_grating_slat', (-1.68 + i * .24, 0, .27), (.11, .48, .04), 'steel', .006)
    for y in (-.29, .29):
        box('Grating_rim', (0, y, .28), (3.7, .06, .05), 'light', .008)


def ceiling():
    box('Ceiling_cassette_4m', (0, 0, .08), (4, 4, .16), 'black')
    for x in (-1.8, 1.8):
        box('Truss_beam', (x, 0, .3), (.22, 4, .45), 'steel')
        box('Service_strip', (x, 0, .54), (.09, 3.6, .025), 'white', .008)
    for x in (-.36, -.12, .12, .36):
        tube('Exposed_conduit', (x, -1.92, .34), (x, 1.92, .34), .047, 'rubber')
    for y in (-1.4, 0, 1.4):
        box('Cable_hanger', (0, y, .4), (1.2, .12, .16), 'steel')
    for x in (-1.05, 1.05):
        box('Ventilation_housing', (x, 0, .24), (.58, 2.6, .19), 'steel')
        for i in range(13):
            box('Vent_louver', (x, -1.14 + .19 * i, .38), (.51, .09, .05), 'black', .006)


def arena():
    box('Forge_foundation', (0, 0, .15), (12, 10, .3), 'black', .09)
    box('Digital_arena_floor', (0, 0, .31), (11.6, 9.6, .04), 'floor')
    for i in range(-5, 6):
        box('Hologrid_longitudinal', (i, 0, .338), (.014, 9.5, .01), 'cyan', .002)
    for i in range(-4, 5):
        box('Hologrid_lateral', (0, i, .338), (11.5, .014, .01), 'cyan', .002)
    for x in (-5.55, 5.55):
        box('Arena_edge_neon', (x, 0, .37), (.055, 9.35, .03), 'magenta', .008)
    for y in (-4.55, 4.55):
        box('Arena_edge_neon', (0, y, .37), (11.15, .055, .03), 'magenta', .008)
    for x in (-5.55, 5.55):
        for y in (-4.45, 4.45):
            cylinder('Projection_pillar_foot', (x, y, .46), .38, .22)
            box('Projection_pillar', (x, y, 1.1), (.32, .32, 1.4), 'black')
            box('Projection_light', (x, y - .171, 1.12), (.075, .02, 1.03), 'cyan')
            ring('Pillar_holo_halo', (x, y, 1.88), .32, .015, 'cyan')
    for rad in (1.05, 1.35):
        ring('Training_spawn_ring', (0, 0, .357), rad, .015)
    text('Forge_floor_mark', 'VIRTUAL FORGE', (0, 3.7, .346), .38, 'white', (0, 0, 0))
    text('Training_designation', 'TRAINING  /  01', (0, -3.75, .346), .18, 'cyan', (0, 0, 0))


def extraction():
    cylinder('Octagonal_landing_platform', (0, 0, .24), 4, .48, 'black', 8)
    cylinder('Landing_deck', (0, 0, .495), 3.72, .04, 'steel', 8)
    cylinder('Obsidian_deck_center', (0, 0, .522), 3.2, .025, 'floor', 64)
    for radius in (2.7, 3.2):
        ring('Holographic_landing_marker', (0, 0, .55), radius, .025)
    for i in range(8):
        a = i * math.tau / 8
        x, y = 3.4 * math.cos(a), 3.4 * math.sin(a)
        marker = box('Approach_vector', (x, y, .55), (.38, .08, .025), 'gold')
        marker.rotation_euler.z = a
    for x in (-3.35, 3.35):
        for y in (-1.1, 1.1):
            box('Beacon_base', (x, y, .65), (.36, .36, .28), 'black')
            cylinder('Landing_beacon', (x, y, .88), .06, .23, 'cyan', 12)
    for x in (-.68, .68):
        box('H_landing_mark', (x, 0, .543), (.16, 1.8, .018), 'white')
    box('H_landing_crossbar', (0, 0, .543), (1.5, .16, .018), 'white')
    text('Extraction_ID', 'EXTRACTION  /  E-01', (0, 2.2, .553), .23, 'cyan', (0, 0, 0))


def hub():
    box('Lobby_foundation', (0, 0, .15), (12, 10, .3), 'black', .04)
    for x in range(-5, 6, 2):
        for y in range(-4, 5, 2):
            box('Obsidian_lobby_tile', (x, y, .325), (1.97, 1.97, .05), 'floor', .005)
    box('Lobby_rear_wall', (0, 4.85, 3.15), (12, .3, 5.7), 'black')
    for x in (-5.55, 5.55):
        for y in (-3.7, 3.7):
            box('Column_plinth', (x, y, .47), (.9, .9, .34), 'steel')
            box('Corporate_pillar', (x, y, 3.25), (.5, .5, 5.5), 'black')
            box('Column_luminous_reveal', (x, y - .265, 3.25), (.04, .02, 4.8), 'gold')
            box('Column_capital', (x, y, 5.9), (.95, .95, .2), 'steel')
    for x in (-5.55, 5.55):
        box('Overhead_spine', (x, 0, 5.94), (.6, 8.8, .12), 'steel')
    box('Rear_spanning_beam', (0, 3.7, 5.94), (11.6, .5, .12), 'steel')
    # Open roof/front intentionally permits playable cameras and modular extension.
    for x, color, words in ((-3.5, 'cyan', 'NEURO\nLINK'), (0, 'magenta', 'BIO\nGEN'), (3.5, 'gold', 'AXIOM\nFORCE')):
        box('Holographic_ad_frame', (x, 4.58, 3.25), (2.4, .18, 3.5), 'steel', .05)
        box('Holographic_ad_backplate', (x, 4.465, 3.25), (2.16, .04, 3.26), 'floor')
        for z in (1.72, 4.78):
            box('Holographic_ad_edge', (x, 4.432, z), (2.08, .014, .018), color)
        text('Holographic_ad_copy', words, (x, 4.427, 3.5), .46, color)
        text('Corporate_tagline', 'TOMORROW, ENGINEERED.', (x, 4.425, 2.15), .105, 'white')
        for j in range(5):
            box('Holographic_readout', (x - .65 + j * .3, 4.43, 2.65), (.18, .018, .07 + .07 * j), color)
    box('Reception_counter', (0, 2.3, .83), (4.2, 1, 1.05), 'black', .1)
    box('Reception_top', (0, 2.3, 1.4), (4.4, 1.1, .12), 'floor', .03)
    box('Reception_neon', (0, 1.785, .88), (3.85, .02, .035), 'cyan')
    for x in (-1, 1):
        box('Reception_terminal', (x, 2.3, 1.52), (.55, .35, .12), 'steel')
        box('Reception_terminal_display', (x, 2.3, 1.587), (.48, .28, .014), 'cyan')
    for x in (-2.6, 2.6):
        box('Wayfinding_strip', (x, -.8, .36), (.025, 6.8, .02), 'cyan', .005)


def bounds():
    points = [o.matrix_world @ Vector(v) for o in bpy.context.scene.objects if o.type == 'MESH' for v in o.bound_box]
    return [min(p[i] for p in points) for i in range(3)], [max(p[i] for p in points) for i in range(3)]


def export(name, purpose, fn):
    reset(name)
    fn()
    bpy.context.view_layer.update()
    low, high = bounds()
    parent = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(parent)
    parent['units'] = 'meters'
    parent['origin'] = 'bottom_center'
    parent['purpose'] = purpose
    parent['source'] = 'Axiom Force procedural hard-surface kit'
    for obj in list(bpy.context.scene.objects):
        if obj != parent:
            obj.location.z -= low[2]
            obj.parent = parent
    bpy.context.view_layer.update()
    bpy.ops.object.select_all(action='SELECT')
    filepath = OUTPUT / f'{name}.glb'
    bpy.ops.export_scene.gltf(filepath=str(filepath), export_format='GLB', use_selection=True,
        export_apply=True, export_extras=True, export_yup=True, export_animations=False)
    low, high = bounds()
    records.append({'file': filepath.relative_to(ROOT).as_posix(), 'purpose': purpose,
        'dimensions_m_blender_xyz': [round(high[i]-low[i], 4) for i in range(3)],
        'gltf_up_axis': 'Y', 'origin': 'bottom center', 'mesh_objects': sum(o.type == 'MESH' for o in bpy.context.scene.objects),
        'emission': 'separate named Emissive_* materials; pulse their emission strength', 'bytes': filepath.stat().st_size})
    print(f'ARCHITECTURE_EXPORTED {name}', flush=True)


for name, purpose, fn in [
    ('modular_wall_panel', '4 m wide x 3.2 m tall wall; snap along X', wall),
    ('modular_floor_tile', '4 x 4 m polished floor with industrial drain grating', floor),
    ('modular_ceiling_panel', '4 x 4 m exposed service cassette; install at ceiling elevation', ceiling),
    ('virtual_forge', '12 x 10 m digital training arena with emissive grid', arena),
    ('extraction_point', '8 m octagonal landing pad with holographic approach markers', extraction),
    ('corporate_hub', '12 x 10 x 6 m open-front lobby with holographic advertisements', hub),
]:
    export(name, purpose, fn)

(OUTPUT / 'architecture_manifest.json').write_text(json.dumps({'generator': 'tools/assets/build_architecture.py',
    'style': 'Neon Pulse', 'assets': records}, indent=2) + '\n', encoding='utf-8')
print('ARCHITECTURE_COMPLETE', flush=True)
