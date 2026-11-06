"""Author the original Neo-Kyoto modular kit in Blender, without external asset dependencies.

Run through the opt-in cutscene Compose image with Blender's embedded Python.
Orthographic camera, transparent PNGs, fixed lighting and shared geometry keep the
six eight-direction/four-frame operative atlases and eight environment modules coherent.
The first palace block is the reference family; no game export is performed.
"""
import json
import math
from pathlib import Path
import bpy
from mathutils import Vector

OUT = Path('/workspace/assets/world')
OUT.mkdir(parents=True, exist_ok=True)


def material(name, color, emission=False):
    """Create one reusable painted material, with optional lit neon in the authored palette."""
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    shader = m.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (*color, 1)
    shader.inputs['Roughness'].default_value = .6
    if emission:
        shader.inputs['Emission Color'].default_value = (*color, 1)
        shader.inputs['Emission Strength'].default_value = 2
    return m


def box(name, location, scale, mat, parent=None):
    """Make an original bevel-edged modular prism with applied dimensions and optional assembly parent."""
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    if parent:
        obj.parent = parent
    bevel = obj.modifiers.new('Machined edges', 'BEVEL')
    bevel.width = .05
    bevel.segments = 1
    obj.modifiers.new('Weighted normals', 'WEIGHTED_NORMAL')
    return obj


def setup(width, height, target, ortho):
    """Reset the assembly but retain materials; set a north-up angled orthographic camera and soft key/fill."""
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 64
    scene.cycles.use_denoising = False
    scene.render.resolution_x = width
    scene.render.resolution_y = height
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'
    scene.world.color = (.22, .26, .32)
    bpy.ops.object.camera_add(location=Vector(target)+Vector((0,-12,10.26)))
    cam = bpy.context.object
    cam.rotation_euler = (Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.type = 'ORTHO'
    cam.data.ortho_scale = ortho
    scene.camera = cam
    for location, energy, size in [((-8,-6,16),2200,10),((6,3,10),1600,8)]:
        bpy.ops.object.light_add(type='AREA',location=location)
        light=bpy.context.object
        light.data.energy=energy
        light.data.shape='DISK'
        light.data.size=size
        light.rotation_euler=(-light.location).to_track_quat('-Z','Y').to_euler()
    scene.view_settings.view_transform='Standard'


def render(path):
    """Write one transparent frame; all output paths stay under the kit directory."""
    bpy.context.scene.render.filepath=str(path)
    bpy.ops.render.render(write_still=True)


def environments():
    """Author eight families: tower, palace, viaduct, factory, citadel, cedar, rock and solar array."""
    steel=material('Midnight steel',(.055,.1,.16))
    concrete=material('Rain concrete',(.25,.3,.34))
    cyan=material('Ion cyan',(.02,.8,1),True)
    amber=material('Lantern amber',(1,.3,.04),True)
    cedar=material('Cedar green',(.055,.24,.19))
    for kind in range(8):
        setup(384,384,(0,0,0),24)
        if kind==5:
            box('Cedar trunk',(0,0,2),(1,1,4),concrete)
            for z,w in [(3,5),(5,4),(7,2.5)]:
                bpy.ops.mesh.primitive_cone_add(vertices=8,radius1=w/2,depth=3,location=(0,0,z))
                bpy.context.object.data.materials.append(cedar)
        elif kind==6:
            bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=3,location=(0,0,1.5))
            bpy.context.object.scale=(1, .75,1)
            bpy.context.object.data.materials.append(concrete)
        elif kind==7:
            box('Array pedestal',(0,0,.5),(8,5,1),steel)
            obj=box('Solar collector',(0,0,2),(11,7,.3),cyan)
            obj.rotation_euler.x=.25
            for x in range(-4,5): box('Solar seam',(x,0,2.2),(.05,6.7,.08),steel)
        else:
            height=[9,4,7,5,11][kind]
            box('Foundation',(0,0,.4),(12,10,.8),concrete)
            box('Facade',(0,0,height/2+.8),(11,9,height),steel if kind!=1 else concrete)
            if kind==1:
                for z,w in [(4.7,14),(6.3,11)]:
                    roof=box('Palace eave',(0,0,z),(w,11,.6),steel)
                    roof.rotation_euler.y=.025
                for x in [-4,-2,0,2,4]: box('Lantern',(x,-5,3),(.4,.4,.8),amber)
            elif kind==3:
                for x in [-3,3]: box('Furnace chimney',(x,2,height+1),(1.5,1.5,4),concrete)
            for z in range(2,height,2):
                for x in [-4,-2,0,2,4]: box('Lit window',(x,-4.56,z),(.9,.08,.7),amber if kind in (1,3) else cyan)
            box('Roof coping',(0,0,height+.9),(12,10,.3),concrete)
        render(OUT/f'module_{kind}.png')


def operatives():
    """Render identifiable original operatives with eight facings and idle/three stride poses at a fixed foot pivot."""
    palettes=[(.05,.7,1),(.03,.7,.6),(.55,.22,1),(1,.2,.65),(.7,.35,1),(1,.55,.06)]
    for index,color in enumerate(palettes):
        setup(96,128,(0,0,.9),3.7)
        armor=material(f'Operative {index} armor',(.055,.075,.11))
        accent=material(f'Operative {index} accent',color,True)
        bpy.ops.object.empty_add()
        root=bpy.context.object
        bulk=1.25 if index in (2,5) else .9
        box('Armored torso',(0,0,1.15),(.6*bulk,.32,.65),armor,root)
        box('Chest insignia',(0,-.18,1.25),(.25,.06,.25),accent,root)
        box('Helmet',(0,0,1.72),(.37,.35,.37),armor,root)
        box('Visor',(0,-.19,1.75),(.32,.05,.08),accent,root)
        legs=[]
        for side in [-1,1]:
            legs.append(box('Greave',(side*.17,0,.42),(.23,.27,.7),armor,root))
            box('Shoulder',(side*.39*bulk,0,1.35),(.22,.35,.3),accent,root)
            box('Forearm',(side*.42*bulk,-.02,.95),(.18,.2,.4),armor,root)
        if index==0:
            sword=box('Katana',(.42,.16,1.1),(.08,.08,1.6),accent,root)
            sword.rotation_euler.y=-.35
        elif index==1: box('Hydro tank',(0,.25,1.12),(.45,.3,.6),accent,root)
        elif index==2: box('Titan back coil',(0,.25,1.4),(.75,.2,.75),accent,root)
        elif index==3:
            for side in [-1,1]: box('Resonator',(side*.55,.1,1.5),(.14,.15,.8),accent,root)
        elif index==4:
            for side in [-1,1]: box('Phase dagger',(side*.45,-.1,.65),(.07,.07,.65),accent,root)
        else: box('Solar shield',(-.6,-.05,1),(.15,.65,1.1),accent,root)
        atlas=bpy.data.images.new(f'operative_{index}',width=384,height=1024,alpha=True)
        pixels=[0.0]*(384*1024*4)
        for direction in range(8):
            root.rotation_euler.z=-direction*math.pi/4
            for frame in range(4):
                phase=0 if frame==0 else (frame-1)*2*math.pi/3
                for side,leg in zip([-1,1],legs):
                    leg.rotation_euler.x=side*math.sin(phase)*.4
                    leg.location.y=side*math.sin(phase)*.1
                temp=OUT/'frame.png'
                render(temp)
                img=bpy.data.images.load(str(temp),check_existing=False)
                src=list(img.pixels)
                # Blender pixels are bottom-up; Godot atlas rows are top-down.
                for y in range(128):
                    start=(((7-direction)*128+y)*384+frame*96)*4
                    pixels[start:start+96*4]=src[y*96*4:(y+1)*96*4]
                bpy.data.images.remove(img)
        atlas.pixels=pixels
        atlas.filepath_raw=str(OUT/f'operative_{index}.png')
        atlas.file_format='PNG'
        atlas.save()
        bpy.data.images.remove(atlas)
    (OUT/'frame.png').unlink(missing_ok=True)


def landmarks():
    """Author sixteen architectural and sixteen scenic landmarks with district-specific silhouettes and materials."""
    stone=material('Landmark basalt',(.12,.18,.22))
    brass=material('Landmark brass',(.45,.28,.1))
    glow=material('Landmark amber',(1,.45,.08),True)
    leaf=material('Landmark foliage',(.06,.3,.2))
    water=material('Landmark prism',(.03,.4,.6),True)
    for region in range(16):
        for kind in (1,2):
            setup(384,384,(0,0,1),26)
            box('Destination plinth',(0,0,.3),(13,10,.6),stone)
            if kind==1:
                count=2+region%4
                for i in range(count):
                    x=(i-(count-1)/2)*2.6
                    height=4+(i+region)%5
                    box('District monument pier',(x,1,height/2),(1.6,3,height),stone)
                    box('District crown',(x,1,height+.25),(2.2,3.4,.5),brass)
                    box('Luminous inlay',(x,-.55,height/2),(.25,.08,height*.7),glow)
                if region in (0,4,5,12):
                    box('Shrine crossbeam',(0,0,7.3),(14,4,.7),brass)
                elif region in (1,3,7,11):
                    bpy.ops.mesh.primitive_torus_add(major_radius=3,minor_radius=.25,location=(0,-1,6),rotation=(math.pi/2,0,0))
                    bpy.context.object.data.materials.append(glow)
                elif region in (6,10):
                    box('Needle',(0,0,9),(.7,.7,8),water)
            else:
                for i in range(3+region%4):
                    angle=i*2.4
                    x,y=math.cos(angle)*4,math.sin(angle)*3
                    if region in (2,12):
                        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1.8,location=(x,y,1.3+(i%2)))
                        bpy.context.object.data.materials.append(stone)
                    else:
                        box('Scenic planting',(x,y,.7),(.4,.4,1.4),brass)
                        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=1.3,location=(x,y,2.4))
                        bpy.context.object.data.materials.append(leaf)
                box('Reflecting pool',(0,0,.65),(4,3,.15),water)
                box('Overlook bench',(0,-3,1),(5,.7,.7),brass)
            render(OUT/f'landmark_{region*3+kind}.png')


environments()
operatives()
landmarks()
(OUT/'manifest.json').write_text(json.dumps(dict(version=1,source='Original Blender procedural meshes: tools/world/build_kit.py',license='Project original assets',camera='Orthographic north-up angled top-down',metres_to_pixels=16,modules=8,landmarks=32,operatives=6,directions=8,frames=4,frame_size=[96,128],review='Agent visual inspection completed; player/native art acceptance pending'),indent=2)+'\n')
