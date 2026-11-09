"""Author the original Neo-Kyoto modular kit in Blender, without external asset dependencies.

Run through the opt-in cutscene Compose image with Blender's embedded Python.
Orthographic camera, transparent PNGs, fixed lighting and shared geometry keep the
six eight-direction/four-frame operative atlases and eight environment modules coherent.
The first palace block is the reference family; no game export is performed.
City names after Blender's "--" (kyoto, mumbai, honolulu, dubai, luanda, beirut) render only those cities.
"""
import json
import math
import sys
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


def roof(width, depth, height, mat):
    """Build a stepped, gently overhanging tile roof from three stacked slabs."""
    for level, inset in enumerate((0, 1.4, 2.8)):
        box('Roof tier', (0, 0, height+.2+level*.5), (width-inset, depth-inset, .45), mat)


def kyoto_palette():
    """Kyoto's palette roles: dark timber and slate for mass, plaster for light, amber lantern glow as the accent."""
    return dict(
        timber=material('Kyoto timber', (.1, .055, .04)),
        slate=material('Kyoto slate', (.07, .09, .12)),
        plaster=material('Kyoto plaster', (.62, .6, .54)),
        stone=material('Kyoto stone', (.26, .27, .27)),
        iron=material('Kyoto iron', (.12, .12, .15)),
        lantern=material('Kyoto lantern', (1, .34, .08), True),
        cyan=material('Kyoto signal', (.02, .75, .9), True),
        maple=material('Kyoto maple', (.5, .07, .05)),
        bamboo=material('Kyoto bamboo', (.2, .36, .13)),
        moss=material('Kyoto moss', (.09, .2, .1)),
        water=material('Kyoto water', (.03, .35, .5), True),
    )


def kyoto_modules():
    """Author eight Kyoto modules: townhouse, teahouse, storehouse, foundry, crane, maple, bamboo and stone garden."""
    p=kyoto_palette()
    folder=OUT/'kyoto'
    folder.mkdir(parents=True, exist_ok=True)
    for kind in range(8):
        setup(384,384,(0,0,0),24)
        if kind in (0,1,2):
            height=[5,4,6][kind]
            box('Foundation',(0,0,.3),(12,10,.6),p['stone'])
            box('Walls',(0,0,height/2+.6),(11,9,height),p['plaster'] if kind==2 else p['timber'])
            roof(14,12,height+.6,p['slate'])
            if kind==0:
                for x in range(-5,6): box('Lattice bar',(x,-4.56,3),(.15,.08,3.6),p['plaster'])
                box('Street light',(0,-4.6,1.6),(3,.08,.5),p['lantern'])
            elif kind==1:
                for x in [-4,-2,0,2,4]: box('Lantern',(x,-5.6,3.4),(.5,.5,.9),p['lantern'])
            else:
                box('Plaster band',(0,-4.55,3.2),(11,.08,.5),p['slate'])
                box('Iron door',(0,-4.56,2),(2.2,.08,2.8),p['iron'])
        elif kind==3:
            box('Foundation',(0,0,.4),(12,10,.8),p['stone'])
            box('Furnace hall',(0,0,3.8),(11,9,6),p['iron'])
            for x in [-3,3]: box('Stack',(x,2,9),(1.6,1.6,5),p['stone'])
            for x in [-4,-1.3,1.3,4]: box('Furnace mouth',(x,-4.56,2.4),(1.6,.08,1.5),p['lantern'])
        elif kind==4:
            box('Crane base',(0,0,.4),(12,10,.8),p['stone'])
            for x in [-4,4]: box('Crane leg',(x,0,5),(.9,.9,9),p['iron'])
            box('Gantry',(0,0,9.6),(11,1.6,.9),p['iron'])
            box('Hook line',(1.5,0,7.3),(.15,.15,3.6),p['cyan'])
            box('Cargo',(1.5,0,4.8),(2.6,2.2,1.6),p['timber'])
        elif kind==5:
            box('Trunk',(0,0,1.6),(.6,.6,3.2),p['timber'])
            for x,y,z,r in [(0,0,4.2,2.2),(1.3,.4,3.5,1.5),(-1.2,-.3,3.6,1.6)]:
                bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=r,location=(x,y,z))
                bpy.context.object.data.materials.append(p['maple'])
        elif kind==6:
            for i in range(11):
                angle=i*2.4
                x,y=math.cos(angle)*(.4+i*.17),math.sin(angle)*(.3+i*.13)
                h=6+(i*3)%4
                box('Culm',(x,y,h/2),(.2,.2,h),p['bamboo'])
                box('Leaf cap',(x,y,h),(.9,.9,.5),p['moss'])
        else:
            box('Moss bed',(0,0,.15),(4.8,3.8,.3),p['moss'])
            for x,y,r in [(-1.3,.4,.8),(1.2,-.3,1),(.1,1,.55)]:
                bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=r,location=(x,y,.6))
                bpy.context.object.data.materials.append(p['stone'])
            box('Lantern post',(0,-1.2,.8),(.4,.4,1.3),p['stone'])
            box('Lantern light',(0,-1.2,1.5),(.5,.5,.4),p['lantern'])
        render(folder/f'module_{kind}.png')


def kyoto_landmarks():
    """Author Kyoto's eight landmarks, matching KyotoCity's order: a built and a scenic site per district."""
    p=kyoto_palette()
    folder=OUT/'kyoto'
    folder.mkdir(parents=True, exist_ok=True)
    for index in range(8):
        setup(384,384,(0,0,1),26)
        box('Destination plinth',(0,0,.3),(13,10,.6),p['stone'])
        if index==0:
            for x in [-5,-1.7,1.7,5]: box('Bridge pier',(x,0,1.5),(.8,3,1.8),p['stone'])
            box('Bridge deck',(0,0,2.7),(13,3.4,.5),p['timber'])
            for x in range(-6,7,2): box('Rail post',(x,-1.6,3.3),(.2,.2,.9),p['timber'])
        elif index==1:
            for i in range(26):
                angle=i*2.4
                x,y=math.cos(angle)*(1+i*.2),math.sin(angle)*(.8+i*.14)
                if abs(x)<1.1: continue
                h=6+(i*5)%5
                box('Culm',(x,y,h/2+.6),(.22,.22,h),p['bamboo'])
            box('Path',(0,0,.65),(1.8,9.5,.1),p['plaster'])
        elif index==2:
            box('Track',(0,1,3.4),(1.8,11,.3),p['iron']).rotation_euler.x=.5
            box('Funicular car',(0,0,4),(3,4,2.6),p['timber'])
            box('Car windows',(0,-2.05,4.4),(2.4,.08,1),p['cyan'])
        elif index==3:
            for z,w in [(1,11),(2,8),(3,5)]: box('Terrace',(0,1,z),(w,w*.6,1),p['stone'])
            for x,z,r in [(-3.5,3.6,1.8),(3.5,3.4,1.6),(0,5.6,2)]:
                bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=r,location=(x,1,z))
                bpy.context.object.data.materials.append(p['maple'])
        elif index==4:
            for i,x in enumerate([-4,0,4]):
                h=8+i%2*2
                box('Stack',(x,1,h/2+.6),(2,2,h),p['iron'])
                box('Stack glow',(x,-.05,h*.7),(1.4,.08,.5),p['lantern'])
        elif index==5:
            box('Canal',(0,0,.65),(11,4,.12),p['water'])
            for x in [-5.4,5.4]: box('Lock gate',(x,0,1.6),(.7,4.4,2),p['timber'])
            for x in [-3,3]:
                bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=1.2,location=(x,3.6,1.6))
                bpy.context.object.data.materials.append(p['moss'])
        elif index==6:
            box('Bridge deck',(0,0,2.3),(13,3.6,.5),p['stone'])
            for x in range(-6,7,3):
                box('Light mast',(x,-1.5,4),(.25,.25,3),p['iron'])
                box('Light head',(x,-1.5,5.7),(.6,.6,.5),p['lantern'])
        else:
            box('Lane',(0,0,.65),(3,9.5,.1),p['stone'])
            for side in [-1,1]:
                box('Lane house',(side*4,1,2.4),(4,7,3.6),p['timber'])
                box('Lane roof',(side*4,1,4.5),(5,8,.5),p['slate'])
                for y in [-2,0,2,4]: box('Hanging lantern',(side*1.8,y,3),(.5,.5,.8),p['lantern'])
        render(folder/f'landmark_{index}.png')
    (folder/'manifest.json').write_text(json.dumps(dict(version=1,source='Original Blender procedural meshes: tools/world/build_kit.py kyoto',license='Project original assets',camera='Orthographic north-up angled top-down',metres_to_pixels=16,modules=8,landmarks=8,review='Agent visual inspection only; player art acceptance pending'),indent=2)+'\n')


# Each city's visual identity: colour roles, roof style, its two tree species, and eight landmark shapes
# in KyotoCity-style order (a built then a scenic site for each of the four districts).
CITY_STYLES = {
    'mumbai': dict(wall=(.55,.36,.18), wall2=(.3,.42,.5), roof='flat', roofc=(.3,.27,.25), glow=(1,.5,.1), leaf=(.1,.33,.12), ground=(.3,.27,.22),
                   trees=('banyan','palm'), landmarks=['seawall','palms','watchtower','grove','clocktower','green','stacks','yard']),
    'honolulu': dict(wall=(.62,.5,.34), wall2=(.36,.55,.55), roof='tile', roofc=(.35,.12,.08), glow=(.1,.8,.75), leaf=(.08,.4,.15), ground=(.28,.3,.2),
                     trees=('palm','olive'), landmarks=['hall','canal','mast','grove','lighthouse','lane','gate','beach']),
    'dubai': dict(wall=(.7,.58,.38), wall2=(.2,.4,.55), roof='flat', roofc=(.55,.45,.3), glow=(.2,.75,1), leaf=(.25,.35,.1), ground=(.6,.48,.3),
                  trees=('palm','olive'), landmarks=['cranes','canal','windtowers','lane','watchtower','springs','bridge','beach']),
    'luanda': dict(wall=(.72,.42,.28), wall2=(.75,.68,.42), roof='tile', roofc=(.45,.16,.08), glow=(1,.6,.15), leaf=(.25,.35,.12), ground=(.4,.27,.16),
                   trees=('baobab','palm'), landmarks=['gate','palms','hall','green','clocktower','beach','watchtower','grove']),
    'beirut': dict(wall=(.72,.66,.52), wall2=(.48,.5,.46), roof='tile', roofc=(.5,.14,.08), glow=(1,.75,.3), leaf=(.08,.28,.14), ground=(.34,.32,.26),
                   trees=('cedar','olive'), landmarks=['cranes','canal','steps','green','marquee','lane','watchtower','grove']),
}


def city_palette(style, name):
    """Build a city's materials from its colour roles; names are prefixed so cities can't share a material by accident."""
    make=lambda role,colour,glowing=False: material(f'{name} {role}',colour,glowing)
    return dict(wall=make('wall',style['wall']), wall2=make('wall2',style['wall2']), roof=make('roof',style['roofc']),
                stone=make('stone',(.3,.3,.3)), iron=make('iron',(.11,.12,.15)), glow=make('glow',style['glow'],True),
                leaf=make('leaf',style['leaf']), trunk=make('trunk',(.14,.09,.06)), water=make('water',(.03,.35,.5),True), ground=make('ground',style['ground']))


def sphere(location, radius, mat, subdivisions=2):
    """Add one faceted ball, the shared shape for foliage and boulders."""
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions,radius=radius,location=location)
    bpy.context.object.data.materials.append(mat)


def tree(kind, p, x=0, y=0, scale=1):
    """Add one tree of the given species at ground level; each species has a distinct silhouette at game scale."""
    if kind=='palm':
        box('Palm trunk',(x,y,2.6*scale),(.3*scale,.3*scale,5.2*scale),p['trunk'])
        for i in range(6):
            angle=i*math.pi/3
            frond=box('Frond',(x+math.cos(angle)*1.1*scale,y+math.sin(angle)*1.1*scale,5.2*scale),(2.2*scale,.5*scale,.12*scale),p['leaf'])
            frond.rotation_euler.z=angle
    elif kind=='banyan':
        for dx,dy in [(-1.3,0),(0,0),(1.3,.3),(.6,-.8),(-.6,.8)]: box('Aerial root',(x+dx*scale,y+dy*scale,1.5*scale),(.25*scale,.25*scale,3*scale),p['trunk'])
        for dx,r in [(-1.2,1.8),(0,2.3),(1.3,1.7)]: sphere((x+dx*scale,y,3.8*scale),r*scale,p['leaf'])
    elif kind=='baobab':
        box('Baobab trunk',(x,y,2*scale),(1.8*scale,1.8*scale,4*scale),p['trunk'])
        for dx,dy in [(-1.3,0),(1.3,.2),(0,1),(0,-1)]: sphere((x+dx*scale,y+dy*scale,4.4*scale),.8*scale,p['leaf'],1)
    elif kind=='cedar':
        box('Cedar trunk',(x,y,1*scale),(.5*scale,.5*scale,2*scale),p['trunk'])
        for z,w in [(2,4.4),(3.4,3.4),(4.8,2.2)]:
            bpy.ops.mesh.primitive_cone_add(vertices=8,radius1=w/2*scale,depth=1.7*scale,location=(x,y,z*scale))
            bpy.context.object.data.materials.append(p['leaf'])
    else:
        box('Olive trunk',(x,y,1.1*scale),(.45*scale,.45*scale,2.2*scale),p['trunk'])
        for dx,r in [(-.7,1),(.7,1.1),(0,1.3)]: sphere((x+dx*scale,y,2.8*scale),r*scale,p['leaf'],1)


def city_modules(name, style):
    """Render a city's eight modules: two homes, a shop block, a factory, a crane, two trees and a small garden."""
    p=city_palette(style,name)
    folder=OUT/name
    folder.mkdir(parents=True,exist_ok=True)
    for kind in range(8):
        setup(384,384,(0,0,0),24)
        if kind in (0,1,2):
            # Kept under 8 m: taller roofs reach the fill light at z=10 and blow out.
            height=[5,6,7][kind]
            box('Foundation',(0,0,.3),(12,10,.6),p['stone'])
            box('Walls',(0,0,height/2+.6),(11,9,height),p['wall2'] if kind==1 else p['wall'])
            if style['roof']=='tile':
                for level,inset in enumerate((0,1.8,3.6)): box('Tile roof',(0,0,height+.85+level*.55),(13-inset,11-inset,.55),p['roof'])
            else:
                box('Flat roof',(0,0,height+.75),(11.6,9.6,.3),p['roof'])
                box('Roof water tank',(3,2,height+1.5),(1.8,1.8,1.3),p['stone'])
            for z in range(2,height,2):
                for x in [-4,-2,0,2,4]:
                    box('Window',(x,-4.56,z+.3),(1,.08,.9),p['glow'] if (x+z)%4==0 else p['iron'])
                if kind!=2: box('Balcony',(0,-4.9,z-.3),(9.5,.7,.15),p['iron'])
        elif kind==3:
            box('Foundation',(0,0,.4),(12,10,.8),p['stone'])
            box('Factory hall',(0,0,3.8),(11,9,6),p['iron'])
            box('Roof vent',(0,0,7.2),(8,3,.9),p['wall2'])
            for x in [-3.5,3.5]: box('Stack',(x,2.5,9),(1.5,1.5,5),p['stone'])
            for x in [-4,-1.3,1.3,4]: box('Furnace light',(x,-4.56,2.4),(1.6,.08,1.4),p['glow'])
        elif kind==4:
            box('Crane base',(0,0,.4),(12,10,.8),p['stone'])
            for x in [-4,4]: box('Crane leg',(x,0,5),(.9,.9,9),p['iron'])
            box('Gantry',(0,0,9.6),(11,1.6,.9),p['wall2'])
            box('Hook line',(-1.5,0,7.3),(.15,.15,3.6),p['glow'])
            box('Cargo',(-1.5,0,4.8),(3,2.2,1.8),p['wall'])
        elif kind in (5,6):
            tree(style['trees'][kind-5],p)
        else:
            box('Garden bed',(0,0,.15),(4.8,3.8,.3),p['ground'])
            for x,y,r in [(-1.3,.4,.8),(1.2,-.3,.9),(.1,1,.55)]: sphere((x,y,.6),r,p['stone'],1)
            box('Lamp post',(0,-1.2,.9),(.3,.3,1.5),p['iron'])
            box('Lamp',(0,-1.2,1.7),(.5,.5,.35),p['glow'])
        render(folder/f'module_{kind}.png')


def landmark_shape(shape, p, style):
    """Build one landmark on the already-placed plinth. Shapes are secular civic and natural sites only."""
    if shape=='seawall':
        box('Sea wall',(0,1,2.2),(13,2,3.2),p['stone'])
        box('Wall walk',(0,1,3.95),(13.6,3,.3),p['iron'])
        for x in range(-6,7,3): box('Wall lamp',(x,0,4.8),(.4,.4,1.4),p['glow'])
    elif shape in ('palms','grove'):
        if shape=='grove': box('Forest floor',(0,0,.65),(12,9,.1),p['ground'])
        for x,y,s in [(-4,1.5,1),(0,2.5,1.25),(4,1.2,.95),(-2,-2.5,.8),(2.6,-2.8,.75)]: tree(style['trees'][0],p,x,y,s)
    elif shape=='watchtower':
        for x,y in [(-2,-2),(2,-2),(-2,2),(2,2)]: box('Tower leg',(x,y,4),(.5,.5,7),p['iron'])
        box('Cabin',(0,0,8.4),(5.6,5.6,2.2),p['wall'])
        box('Cabin glass',(0,-2.85,8.6),(4.6,.08,1),p['glow'])
    elif shape=='clocktower':
        box('Tower shaft',(0,1,5.5),(3.6,3.6,10),p['wall'])
        box('Clock face',(0,-.85,8.6),(2.2,.08,2.2),p['glow'])
        box('Tower cap',(0,1,11),(4.6,4.6,.8),p['roof'])
        for x in [-5,5]: box('Arcade wing',(x,1,2.6),(5,3,4),p['wall2'])
    elif shape in ('green','yard'):
        box('Open ground',(0,0,.65),(12,9,.1),p['ground'])
        if shape=='green':
            for x,y in [(-4.5,2.5),(4.5,2.5)]: tree(style['trees'][1],p,x,y,.8)
            box('Bandstand',(0,0,1.2),(3,3,1),p['wall2'])
        else:
            for x,y,h in [(-4,1,2),(-1,2.4,3),(3,1.2,2.4),(4.2,-2.4,1.4)]: box('Bale stack',(x,y,h/2+.7),(2.6,2,h),p['wall'])
    elif shape=='stacks':
        for i,x in enumerate([-4,0,4]):
            h=8+i%2*2
            box('Stack',(x,1,h/2+.6),(2,2,h),p['iron'])
            box('Stack light',(x,-.05,h*.7),(1.4,.08,.5),p['glow'])
    elif shape=='hall':
        box('Hall body',(0,1,3.2),(12,7,5),p['iron'])
        box('Hall roof',(0,1,6),(12.8,7.8,.6),p['roof'])
        for x in [-4.5,-1.5,1.5,4.5]: box('Hall window',(x,-2.55,3.4),(2,.08,2.4),p['glow'])
    elif shape in ('canal','springs'):
        box('Water',(0,0,.65),(11,5,.12),p['water'])
        if shape=='canal':
            for x in [-5.4,5.4]: box('Gate post',(x,0,1.6),(.7,5.4,2),p['iron'])
        else:
            for x,y,r in [(-4,3.4,.9),(4,3.2,1.1)]: sphere((x,y,1),r,p['stone'],1)
        for x in [-3,3]: tree(style['trees'][1],p,x,3.8,.7)
    elif shape=='mast':
        box('Mast',(0,1,6),(.7,.7,11),p['iron'])
        box('Hut',(0,2.2,1.9),(5,3.6,2.6),p['wall'])
        for z,w in [(5,5),(8,4),(11,3)]: box('Mast arm',(0,1,z),(w,.3,.3),p['glow'])
    elif shape=='lighthouse':
        for z,w in [(2.5,4),(5.5,3.2),(8.5,2.4)]: box('Light tower',(0,1,z),(w,w,3.2),p['wall'] if z!=5.5 else p['wall2'])
        box('Lamp room',(0,1,10.6),(2,2,1.2),p['glow'])
    elif shape=='lane':
        box('Lane',(0,0,.65),(3,9.5,.1),p['stone'])
        for side in [-1,1]:
            box('Lane house',(side*4,1,2.6),(4,7,4),p['wall'])
            box('Lane roof',(side*4,1,4.8),(5,8,.5),p['roof'])
            for y in [-2,0,2,4]: box('Stall light',(side*1.8,y,3),(.5,.5,.8),p['glow'])
    elif shape=='gate':
        for x in [-4.5,4.5]: box('Gate tower',(x,1,3.5),(3,3,6),p['wall'])
        box('Gate lintel',(0,1,6.4),(12,3.2,1.2),p['roof'])
        box('Gate barrier',(0,1,2),(6,.4,.4),p['glow'])
    elif shape=='beach':
        box('Sand',(0,1.5,.65),(12.5,6,.12),p['ground'])
        box('Shallows',(0,-3,.65),(12.5,3,.14),p['water'])
        for x in [-3,1.5]: box('Beached boat',(x,1.5,1.1),(1.3,4.6,.8),p['wall2'])
        tree(style['trees'][0],p,5,3,.75)
    elif shape=='cranes':
        for x in [-4,3]:
            box('Crane tower',(x,1,5),(1,1,9),p['iron'])
            box('Crane jib',(x+1.5,1,9.6),(6,.7,.6),p['wall2'])
            box('Crane line',(x+3.4,1,7.4),(.12,.12,3.8),p['glow'])
    elif shape=='windtowers':
        box('Arcade',(0,1,2.4),(12,6,3.6),p['wall'])
        for x in [-4,0,4]:
            box('Wind tower',(x,1,5.8),(1.9,1.9,3.4),p['wall'])
            box('Tower slots',(x,.03,6.3),(1.3,.08,1.5),p['iron'])
    elif shape=='bridge':
        for x in [-4,4]: box('Bridge tower',(x,0,4.2),(1.1,3,7),p['iron'])
        box('Bridge deck',(0,0,2.8),(13,3.4,.5),p['wall2'])
        for x in range(-6,7,3): box('Deck light',(x,-1.6,3.4),(.3,.3,.7),p['glow'])
    elif shape=='steps':
        for i in range(7): box('Stair flight',(0,3.5-i,.9+i*.55),(9,1,1.1),p['wall'])
        for x in [-5,5]: box('Stair house',(x,1,3.4),(2.4,6,5.4),p['wall2'])
    elif shape=='marquee':
        box('Cinema front',(0,1,4),(11,6,6.6),p['wall'])
        box('Marquee canopy',(0,-2.6,3.4),(11.6,2,.7),p['iron'])
        for x in range(-5,6,2): box('Marquee bulb',(x,-3.65,3.4),(.5,.1,.4),p['glow'])
    else:
        raise ValueError('Unknown landmark shape: '+shape)


def city_landmarks(name, style):
    """Render a city's eight landmarks and record the kit's provenance."""
    p=city_palette(style,name)
    folder=OUT/name
    folder.mkdir(parents=True,exist_ok=True)
    for index,shape in enumerate(style['landmarks']):
        setup(384,384,(0,0,1),26)
        box('Destination plinth',(0,0,.3),(13,10,.6),p['stone'])
        landmark_shape(shape,p,style)
        render(folder/f'landmark_{index}.png')
    (folder/'manifest.json').write_text(json.dumps(dict(version=1,source=f'Original Blender procedural meshes: tools/world/build_kit.py {name}',license='Project original assets',camera='Orthographic north-up angled top-down',metres_to_pixels=16,modules=8,landmarks=8,review='Agent visual inspection only; player art acceptance pending'),indent=2)+'\n')


# Naming cities renders only those, leaving the existing Los Angeles art untouched.
requested=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
if requested:
    for city in requested:
        if city=='kyoto':
            kyoto_modules()
            kyoto_landmarks()
        else:
            city_modules(city,CITY_STYLES[city])
            city_landmarks(city,CITY_STYLES[city])
    sys.exit(0)

environments()
operatives()
landmarks()
(OUT/'manifest.json').write_text(json.dumps(dict(version=1,source='Original Blender procedural meshes: tools/world/build_kit.py',license='Project original assets',camera='Orthographic north-up angled top-down',metres_to_pixels=16,modules=8,landmarks=32,operatives=6,directions=8,frames=4,frame_size=[96,128],review='Agent visual inspection completed; player/native art acceptance pending'),indent=2)+'\n')
