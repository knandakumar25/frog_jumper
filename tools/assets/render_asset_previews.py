"""Render GLB delivery previews without modifying assets (Blender background).

blender -b -t 4 --python tools/assets/render_asset_previews.py -- --files path.glb ...
"""
import argparse
import bpy
import math
import sys
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
args = argparse.ArgumentParser()
args.add_argument('--files', nargs='+', required=True)
args.add_argument('--output', default=str(ROOT / '.validation/asset-previews'))
args.add_argument('--size', type=int, default=768)
args.add_argument('--samples', type=int, default=20)
options = args.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
out = Path(options.output)
out.mkdir(parents=True, exist_ok=True)


def area(name, at, target, energy, size, color):
    data = bpy.data.lights.new(name, 'AREA')
    data.energy, data.shape, data.size, data.color = energy, 'DISK', size, color
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = at
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()


for relative in options.files:
    source = Path(relative)
    if not source.is_absolute():
        source = ROOT / source
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(source))
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    points = []
    for obj in bpy.context.scene.objects:
        if obj.type == 'MESH':
            ev = obj.evaluated_get(dg)
            points.extend(ev.matrix_world @ Vector(v) for v in ev.bound_box)
    low = Vector([min(p[i] for p in points) for i in range(3)])
    high = Vector([max(p[i] for p in points) for i in range(3)])
    center, extent = (high+low)/2, high-low
    span = max(extent)
    bpy.ops.mesh.primitive_plane_add(size=span*200, location=(center.x,center.y,low.z-.025))
    ground = bpy.context.object
    mat = bpy.data.materials.new('Preview_only_ground')
    mat.diffuse_color = (.025,.031,.044,1)
    mat.use_nodes = True
    bs = mat.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (.025,.031,.044,1)
    bs.inputs['Roughness'].default_value = .45
    ground.data.materials.append(mat)
    camera_data = bpy.data.cameras.new('Preview_camera')
    camera = bpy.data.objects.new('Preview_camera', camera_data)
    bpy.context.collection.objects.link(camera)
    camera.location = center + Vector((1.0,-1.5,.9)).normalized()*span*2.8
    camera.rotation_euler = (center-camera.location).to_track_quat('-Z','Y').to_euler()
    camera_data.type = 'ORTHO'
    camera_data.ortho_scale = span*1.48
    camera_data.clip_end = span*100
    scene = bpy.context.scene
    scene.camera = camera
    area('Key', center+Vector((-1,-1,1.9))*span, center, span*span*350, span*1.1,(.76,.87,1))
    area('Fill', center+Vector((1.5,-.5,.6))*span, center, span*span*170, span,(.22,.66,1))
    area('Rim', center+Vector((.2,1.2,1.2))*span, center, span*span*330, span*.75,(1,.23,.5))
    world = bpy.data.worlds.new('Preview_world')
    world.use_nodes=True
    world.node_tree.nodes['Background'].inputs['Color'].default_value = (.04,.055,.09,1)
    world.node_tree.nodes['Background'].inputs['Strength'].default_value = .35
    scene.world=world
    scene.render.engine='CYCLES'
    scene.cycles.samples=options.samples
    scene.cycles.use_denoising=True
    scene.render.resolution_x=scene.render.resolution_y=options.size
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'
    scene.render.filepath=str(out/(source.stem+'.png'))
    scene.view_settings.view_transform='AgX'
    scene.render.film_transparent=False
    bpy.ops.render.render(write_still=True)
    print('PREVIEW_RENDERED '+source.name,flush=True)
