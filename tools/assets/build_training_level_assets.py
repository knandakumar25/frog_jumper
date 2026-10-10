"""Build the Protocol Zero training-level GLBs in Blender 5.2 (standard glTF 2.0).
Run: blender --background --factory-startup --python tools/assets/build_training_level_assets.py
All geometry is original procedural hard-surface art. No third-party source assets.

Kit covers the five-step tutorial in TrainingManager.cs + english.json levels["0"]:
  training_grid_floor        (step 1: move around the grid)
  training_divider_platform  (step 2: jump the divide -- sized to the double jump)
  training_drone             (step 3: passive ability target / "Xelthara Heavy Unit")
  training_calibration_fragment (step 4: retrieve the fragment)
  training_waypoint_beacon   (step signposting; instance 5x with a Label3D numeral)

Environment dressing is intentionally NOT here: reuse virtual_forge.glb,
extraction_point.glb, modular_wall_panel.glb, perimeter_blast_barrier.glb and
cyber_city_skyline.glb. Nothing goes into assets/models/hq/ on purpose --
tests/test_infrastructure.gd asserts a hard-coded model count over that dir.
"""
import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'assets' / 'models' / 'props'
PREVIEW = ROOT / '.validation' / 'training-previews'
MANIFEST = OUTPUT / 'training_level_manifest.json'
ARGS = sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
OVERWRITE = '--overwrite-owned' in ARGS
ONLY = set(next((v.split('=',1)[1] for v in ARGS if v.startswith('--only=')), '').split(',')) - {''}
PREVIEW.mkdir(parents=True, exist_ok=True)
OUTPUT.mkdir(parents=True, exist_ok=True)
PARTS=[]; MAT={}; RIG=None; RECORDS=[]; ASSET=''; BONES={}
if ONLY and MANIFEST.exists():
    RECORDS=[r for r in json.loads(MANIFEST.read_text(encoding='utf8')).get('assets',[]) if r['name'] not in ONLY]

def material(name, color, metallic=0.0, roughness=.45, emission=0):
    mat=bpy.data.materials.new(name); mat.use_nodes=True
    p=mat.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Metallic'].default_value=metallic; p.inputs['Roughness'].default_value=roughness
    if emission:
        p.inputs['Emission Color'].default_value=(*color,1); p.inputs['Emission Strength'].default_value=emission
    mat.diffuse_color=(*color,1)
    mat['zone']='independently_pulsable_emission' if emission else 'physical_surface'
    return mat

def begin(name):
    global PARTS,MAT,RIG,ASSET,BONES
    ASSET=name; PARTS=[]; BONES={}; RIG=None
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    for act in list(bpy.data.actions): bpy.data.actions.remove(act)
    for mat in list(bpy.data.materials): bpy.data.materials.remove(mat, do_unlink=True)
    MAT={
      'armor':material('PBR_Obsidian_Armor',(.038,.052,.065),.78,.32),
      'carbon':material('PBR_Matte_Carbon',(.014,.02,.027),.25,.66),
      'grey':material('PBR_Industrial_Grey',(.21,.27,.31),.85,.3),
      'edge':material('PBR_Titanium_Edges',(.43,.51,.55),.93,.24),
      'rubber':material('PBR_Joint_Rubber',(.008,.012,.015),0,.88),
      'warning':material('PBR_Hazard_Yellow',(.95,.54,.012),.38,.37),
      'obsidian':material('PBR_Polished_Obsidian',(.03,.05,.075),.72,.2),
      'cyan':material('EMISSIVE_Cyan_Security',(.01,.74,1),.3,.24,7),
      'cyan_pulse':material('EMISSIVE_Cyan_Pulse',(.005,.65,1),.15,.24,7),
      'magenta':material('EMISSIVE_Magenta_Pulse',(1,.01,.25),.1,.24,6.5),
      'gold':material('EMISSIVE_Gold_Pulse',(1,.57,.02),.15,.24,6.5),
      'red':material('EMISSIVE_Red_Danger',(1,.008,.015),.2,.3,7),
      'yellow':material('EMISSIVE_Yellow_Warning',(1,.65,.01),.2,.3,7),
      'green':material('EMISSIVE_Green_Online',(.025,1,.22),.2,.3,7),
      'white':material('EMISSIVE_White_Signage',(.65,.88,1),.1,.28,4.5),
      'optic':material('EMISSIVE_White_Optics',(.75,.9,1),.2,.25,5),
    }

def finish_obj(obj,name,mat,bone='Root',bevel=0):
    obj.name=name; obj.data.materials.append(MAT[mat])
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=obj.modifiers.new('Manufactured edge bevel','BEVEL');mod.width=bevel;mod.segments=2
        bpy.context.view_layer.objects.active=obj;bpy.ops.object.modifier_apply(modifier=mod.name)
    for face in obj.data.polygons: face.use_smooth=False
    obj['role']=name; obj['rig_bone']=bone; PARTS.append((obj,bone)); return obj

def box(name,loc,scale,mat='armor',bone='Root',bevel=.025,rot=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc,rotation=rot)
    obj=bpy.context.object; obj.dimensions=scale; return finish_obj(obj,name,mat,bone,bevel)

def cyl(name,a,b,r,mat='grey',bone='Root',vertices=12,r2=None):
    a,b=Vector(a),Vector(b); d=b-a
    if r2 is None:bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=d.length,location=(a+b)/2)
    else:bpy.ops.mesh.primitive_cone_add(vertices=vertices,radius1=r,radius2=r2,depth=d.length,location=(a+b)/2)
    obj=bpy.context.object; obj.rotation_euler=d.to_track_quat('Z','Y').to_euler()
    return finish_obj(obj,name,mat,bone,min(r*.16,.015))

def orb(name,loc,scale,mat='rubber',bone='Root'):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,radius=1,location=loc)
    obj=bpy.context.object; obj.scale=scale; return finish_obj(obj,name,mat,bone)

def ring(name,loc,radius,width=.025,mat='cyan',bone='Root',rot=(0,0,0),major=48):
    bpy.ops.mesh.primitive_torus_add(major_segments=major,minor_segments=8,location=loc,rotation=rot,major_radius=radius,minor_radius=width)
    return finish_obj(bpy.context.object,name,mat,bone)

def label(text_,loc,size=.12,mat='edge',bone='Root',rot=(math.pi/2,0,0)):
    bpy.ops.object.text_add(location=loc,rotation=rot)
    obj=bpy.context.object;obj.data.body=text_;obj.data.size=size;obj.data.align_x='CENTER';obj.data.extrude=.0008
    bpy.ops.object.convert(target='MESH');return finish_obj(bpy.context.object,'Marking_'+text_.replace(' ','_'),mat,bone)

def rig(bones):
    """Floor-snap, then consolidate parts by material into skinned meshes on
    hand-built named bones. Names drive runtime state (lamps/pistons) as in
    build_tactical_assets.py."""
    global RIG,BONES
    bpy.context.view_layer.update()
    floor=min((obj.matrix_world@Vector(c)).z for obj,bone in PARTS for c in obj.bound_box)
    if abs(floor)>0.000001:
        for obj,bone in PARTS: obj.location.z-=floor
        bones=[(name,(head[0],head[1],head[2]-floor),(tail[0],tail[1],tail[2]-floor),parent) if name!='Root' else (name,head,tail,parent) for name,head,tail,parent in bones]
    arm=bpy.data.armatures.new(ASSET+'_Skeleton');RIG=bpy.data.objects.new(ASSET+'_Rig',arm)
    bpy.context.collection.objects.link(RIG);bpy.context.view_layer.objects.active=RIG;RIG.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    for name,head,tail,parent in bones:
        b=arm.edit_bones.new(name);b.head=head;b.tail=tail
        if parent:b.parent=arm.edit_bones[parent]
        BONES[name]=(head,tail,parent)
    bpy.ops.object.mode_set(mode='OBJECT');RIG['units']='meters';RIG['origin']='feet/base at (0,0,0)';RIG['forward']='-Y in Blender; +Z in exported Godot glTF convention'
    grouped={}
    for obj,bone in PARTS:
        vg=obj.vertex_groups.new(name=bone);vg.add(list(range(len(obj.data.vertices))),1,'REPLACE')
        grouped.setdefault(obj.data.materials[0].name,[]).append(obj)
    for material_name,objects in grouped.items():
        bpy.ops.object.select_all(action='DESELECT')
        for obj in objects:obj.select_set(True)
        bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join()
        obj=bpy.context.object;obj.name=ASSET+'__'+material_name
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        modifier=obj.modifiers.new('Rigid mechanical bone weights','ARMATURE');modifier.object=RIG
        obj.parent=RIG
    bpy.ops.object.select_all(action='DESELECT')
    return RIG

def translate(pb,world_delta):
    pb.location=pb.bone.matrix_local.to_3x3().inverted() @ Vector(world_delta)

def clip(name,length,pose_function):
    bpy.context.view_layer.objects.active=RIG
    RIG.animation_data_create();RIG.animation_data.action=None
    act=bpy.data.actions.new(name);RIG.animation_data.action=act
    frames=sorted(set([1,length]+list(range(1,length+1,4))))
    for frame in frames:
        t=(frame-1)/(length-1)
        for pb in RIG.pose.bones:
            pb.rotation_mode='XYZ';pb.rotation_euler=(0,0,0);pb.location=(0,0,0);pb.scale=(1,1,1)
        pose_function(t,RIG.pose.bones)
        for pb in RIG.pose.bones:
            pb.keyframe_insert('rotation_euler',frame=frame,group=pb.name)
            pb.keyframe_insert('location',frame=frame,group=pb.name)
            pb.keyframe_insert('scale',frame=frame,group=pb.name)
    RIG.animation_data.action=None
    track=RIG.animation_data.nla_tracks.new();track.name=name
    strip=track.strips.new(name,1,act);strip.name=name;strip.extrapolation='NOTHING';strip.blend_type='REPLACE'
    return name

def lamps(p,red=False,yellow=False,green=False):
    for key,visible in [('DangerRed',red),('AlertYellow',yellow),('ReadyGreen',green)]:
        if key in p: p[key].scale=(1 if visible else .0001,)*3

# ----------------------------------------------------------------------------
# training_grid_floor -- step 1: 12x12 m single-tile emissive wireframe grid
# ----------------------------------------------------------------------------
def grid_floor():
    begin('training_grid_floor')
    box('Forge_foundation',(0,0,.15),(12,12,.3),'armor','Root',.09)
    box('Digital_arena_floor',(0,0,.31),(11.6,11.6,.04),'obsidian')
    for i in range(-5,6):
        box('Hologrid_longitudinal',(i,0,.336),(.014,11.5,.012),'cyan_pulse',bevel=.002)
        box('Hologrid_lateral',(0,i,.338),(11.5,.014,.012),'cyan_pulse',bevel=.002)
    for v in (-5.55,5.55):
        box('Arena_edge_neon',(v,0,.37),(.055,11.35,.03),'magenta',bevel=.008)
        box('Arena_edge_neon',(0,v,.37),(11.35,.055,.03),'magenta',bevel=.008)
    for x in (-5.45,5.45):
        for y in (-5.45,5.45):
            cyl('Projection_pillar_foot',(x,y,.36),(x,y,.58),.38)
            box('Projection_pillar',(x,y,1.1),(.32,.32,1.4),'armor')
            box('Projection_light',(x,y-.171,1.12),(.075,.02,1.03),'cyan',bevel=.004)
            ring('Pillar_holo_halo',(x,y,1.9),.32,.015,'cyan')
    for rad in (1.05,1.35):
        ring('Training_spawn_ring',(0,0,.357),rad,.015,'cyan',major=64)
    label('PROTOCOL ZERO',(0,3.4,.346),.38,'white',rot=(0,0,0))
    label('CALIBRATION  /  00',(0,-3.6,.346),.18,'cyan_pulse',rot=(0,0,0))
    export_static('12 x 12 m single-tile simulation floor with 1 m emissive wireframe grid -- supersedes tiling modular_floor_tile; sibling of virtual_forge')

# ----------------------------------------------------------------------------
# training_divider_platform -- step 2: ledge pair + 3.5 m chasm (double-jump size)
# ----------------------------------------------------------------------------
def divider_platform():
    begin('training_divider_platform')
    gaps=1.75   # gap runs y=-1.75..+1.75 (3.5 m, comfortable double jump)
    deck_z=1.1  # deck tops sit 1.1 m up so the chasm reads as danger
    for sign,dirn in [(-1,'Entry'),(1,'Landing')]:
        y0=sign*(gaps+3)
        box(dirn+'_ledge_slab',(0,y0,deck_z/2),(12,6,deck_z),'armor',bevel=.06)
        box(dirn+'_deck_plate',(0,y0,deck_z+.025),(11.6,5.6,.05),'obsidian')
        for i in range(-5,6):
            box(dirn+'_deck_grid',(i,y0,deck_z+.052),(.012,5.5,.008),'cyan_pulse',bevel=.002)
        lip_y=sign*(gaps+.06)
        box(dirn+'_gap_lip',(0,sign*gaps,deck_z-.045),(12,.10,.14),'magenta',bevel=.004)
        box(dirn+'_gap_understrip',(0,sign*(gaps-.10),.06),(12,.05,.06),'cyan',bevel=.004)
        for k in range(12):
            x=-5.5+k
            box(dirn+'_warning_slat',(x,lip_y,deck_z+.06),(.34,.16,.014),'warning',bevel=.003,rot=(0,0,.55))
        for side in (-1,1):
            for ycol in (y0-sign*2.2,y0+sign*2.2):
                cyl(dirn+'_support_column',(side*5.2,ycol,0),(side*5.2,ycol,deck_z-.1),.1)
            box(dirn+'_outer_guard_rail',(side*5.94,y0,deck_z+.32),(.09,5.6,.28),'grey',bevel=.02)
            box(dirn+'_rail_neon',(side*5.94,y0,deck_z+.48),(.025,5.5,.03),'cyan',bevel=.004)
    label('DIVIDE / 03',(0,-4.75,deck_z+.065),.24,'edge',rot=(0,0,0))
    export_static('12 m-wide ledge pair with a 3.5 m chasm sized to the double jump -- step 2 of five')

# ----------------------------------------------------------------------------
# training_waypoint_beacon -- step signposting marker
# ----------------------------------------------------------------------------
def waypoint_beacon():
    begin('training_waypoint_beacon')
    bones=[('Root',(0,0,0),(0,0,.2),None),
           ('Mast',(0,0,.08),(0,0,1.05),'Root'),
           ('Holo',(0,0,1.05),(0,0,1.6),'Mast')]
    box('Beacon_base',(0,0,.04),(.5,.5,.08),'carbon',bevel=.03)
    for a in range(3):
        ang=a*2*math.pi/3
        cyl('Tripod_leg',(0,0,.3),(math.cos(ang)*.24,math.sin(ang)*.24,.03),.025,'edge')
    cyl('Telescoping_mast',(0,0,.08),(0,0,1.05),.045,'edge',bone='Mast')
    box('Mast_collar',(0,0,.5),(.13,.13,.1),'grey',bone='Mast')
    cyl('Holo_mount',(0,0,1.05),(0,0,1.12),.06,'armor',bone='Holo')
    bpy.ops.mesh.primitive_cone_add(vertices=8,radius1=.16,radius2=0,depth=.30,location=(0,0,1.56))
    finish_obj(bpy.context.object,'Holo_diamond_top','white','Holo')
    bpy.ops.mesh.primitive_cone_add(vertices=8,radius1=0,radius2=.16,depth=.30,location=(0,0,1.26))
    finish_obj(bpy.context.object,'Holo_diamond_bottom','cyan','Holo')
    ring('Ground_marker_ring',(0,0,.05),.42,.02,'cyan_pulse')
    ring('Holo_ring',(0,0,1.41),.24,.012,'white','Holo',rot=(math.pi/2,0,0))
    rig(bones)
    def idle(t,p):
        ph=t*2*math.pi;p['Holo'].rotation_euler.y=ph
        translate(p['Holo'],(0,0,.05*math.sin(ph*2)))
    def active(t,p):
        ph=t*2*math.pi;p['Holo'].rotation_euler.y=ph*3
        translate(p['Holo'],(0,0,.10*math.sin(ph*2)));translate(p['Mast'],(0,0,.15*math.sin(math.pi*t)**2))
    clips=[clip('Idle',61,idle),clip('Active',49,active)]
    export_rigged(clips,{'purpose':'in-world five-step signposting; instance 5x with a Label3D numeral (1..5)','design':'floor-planted holo waypoint marker'})

# ----------------------------------------------------------------------------
# training_calibration_fragment -- step 4: floating pickup data shard
# ----------------------------------------------------------------------------
def calibration_fragment():
    begin('training_calibration_fragment')
    bones=[('Root',(0,0,0),(0,0,.2),None),
           ('Core',(0,0,1.55),(0,0,1.95),'Root'),
           ('RingA',(0,0,1.55),(0,0,1.7),'Root'),
           ('RingB',(0,0,1.55),(0,0,1.7),'Root')]
    box('Anchor_pedestal',(0,0,.05),(.7,.7,.1),'armor',bevel=.03)
    ring('Pedestal_neon_ring',(0,0,.115),.24,.035,'cyan_pulse')
    box('Pedestal_well',(0,0,.09),(.3,.3,.05),'carbon')
    for s in (-1,1):
        box('Pedestal_gold_edge',(s*.345,0,.08),(.02,.7,.06),'gold',bevel=.004)
        box('Pedestal_gold_edge',(0,s*.345,.08),(.7,.02,.06),'gold',bevel=.004)
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=.2,location=(0,0,1.55))
    core=bpy.context.object;core.scale=(1,1,1.28);finish_obj(core,'Fragment_core','cyan','Core')
    for s in (-1,1):
        cyl('Cage_strut',(s*.26,0,1.2),(s*.26,0,1.9),.022,'edge')
        cyl('Cage_strut',(0,s*.26,1.2),(0,s*.26,1.9),.022,'edge')
    ring('Orbit_ring_A',(0,0,1.55),.34,.015,'white','RingA',rot=(math.pi/2,0,0))
    ring('Orbit_ring_B',(0,0,1.55),.34,.015,'gold','RingB',rot=(0,math.pi/2,0))
    label('CAL-FRAG',(0,-.36,.13),.07,'edge')
    rig(bones)
    def idle(t,p):
        ph=t*2*math.pi;p['Core'].rotation_euler.y=ph
        translate(p['Core'],(0,0,.06*math.sin(ph*2)))
        p['RingA'].rotation_euler.z=ph;p['RingB'].rotation_euler.x=-ph
    def collect(t,p):
        q=t*t;translate(p['Core'],(0,0,.35*q));p['Core'].scale=(1-.4*q,)*3
        for key in ('RingA','RingB'):p[key].scale=(1-q,)*3
    clips=[clip('Idle',61,idle),clip('Collect',25,collect)]
    export_rigged(clips,{'purpose':'step 4 pickup -- floating calibration data fragment, PointValue-driven via Collectable3D','design':'encased faceted cyan data shard over anchor pedestal'})

# ----------------------------------------------------------------------------
# training_drone -- step 3: passive hovering target ("simulated Xelthara Heavy Unit")
# ----------------------------------------------------------------------------
def training_drone():
    begin('training_drone')
    H=1.5  # hull hover height
    bones=[('Root',(0,0,0),(0,0,.2),None),
           ('Hull',(0,0,H),(0,0,H+.2),'Root'),
           ('Gyro',(0,0,H),(0,0,H+.12),'Hull'),
           ('Eye',(0,0,H),(0,0,H-.12),'Hull'),
           ('ReadyGreen',(0,.30,H+.14),(0,.30,H+.24),'Hull'),
           ('AlertYellow',(0,.30,H+.02),(0,.30,H+.12),'Hull'),
           ('DangerRed',(0,.30,H-.10),(0,.30,H),'Hull')]
    for side in ('FL','FR','BL','BR'):
        s=1 if side[0]=='F' else -1   # unused placeholder for readability
        x=(1 if 'L' in side else -1)*.44
        y=(-.40 if side.startswith('F') else .40)
        s=1 if side[0]=='F' else -1
        bones.append(('Rotor'+side,(x,y,H+.08),(x,y,H+.22),'Hull'))
    # ground anchor pad (also hands the floor-snapping rig() a clean zero floor)
    cyl('Target_pad',(0,0,0),(0,0,.09),.55,'carbon',vertices=20)
    ring('Pad_neon_ring',(0,0,.115),.40,.03,'cyan')
    label('TGT-01',(0,.10,.12),.075,'edge',rot=(0,0,0))
    # hull -- angular armoured core
    box('Hull_main',(0,0,H),(.62,.58,.34),'armor','Hull',.05)
    box('Hull_top_plate',(0,0,H+.2),(.5,.46,.12),'carbon','Hull',.04)
    box('Hull_lower_plate',(0,0,H-.2),(.5,.46,.12),'carbon','Hull',.04)
    for s in (-1,1):
        box('Hull_side_panel',(s*.315,0,H),(.03,.4,.2),'grey','Hull',.015)
        box('Hull_side_neon',(s*.332,0,H),(.012,.3,.04),'cyan','Hull',.003)
    box('Hull_front_plate',(0,-.30,H),(.5,.03,.2),'grey','Hull',.015)
    box('Hull_rear_plate',(0,.30,H),(.5,.03,.2),'grey','Hull',.015)
    # gyro halo (flat ring around the hull, spins about world Z in clips)
    ring('Gyro_ring',(0,0,H),.42,.045,'edge','Gyro',major=48)
    for a in range(4):
        ang=a*math.pi/2+math.pi/4
        box('Gyro_neon_segment',(math.cos(ang)*.42,math.sin(ang)*.42,H),(.1,.1,.12),'cyan','Gyro',bevel=.01)
    # nose sensor block (forward) + under-slung eye turret
    box('Nose_mount',(0,-.36,H),(.2,.12,.12),'armor','Eye',.02)
    box('Eye_turret_mount',(0,0,H-.28),(.22,.22,.1),'armor','Eye',.02)
    orb('Eye_housing',(0,-.02,H-.34),(.11,.11,.11),'rubber','Eye')
    orb('Eye_iris',(0,-.075,H-.33),(.05,.05,.05),'red','Eye')
    ring('Eye_optic_ring',(0,-.095,H-.33),.075,.012,'white','Eye',rot=(math.pi/2,0,0))
    # four thruster pods + rotors
    for side in ('FL','FR','BL','BR'):
        x=(1 if 'L' in side else -1)*.44
        y=(-.40 if side.startswith('F') else .40)
        bone='Rotor'+side
        cyl('Pod_body',(x,y,H-.02),(x,y,H+.26),.13,'armor',bone=bone)
        cyl('Rotor_disc',(x,y,H+.30),(x,y,H+.33),.20,'rubber',bone=bone)
        for bl in range(4):
            box('Rotor_blade',(x,y,H+.325),(.36,.05,.018),'edge',bone=bone,rot=(0,0,bl*math.pi/4))
        ring('Pod_under_rim',(x,y,H-.04),.145,.025,'cyan',bone=bone)
    # status diodes on the rear face, one per state bone
    box('Lamp_ready',(0,.30,H+.14),(.05,.02,.08),'green','ReadyGreen',.005)
    box('Lamp_alert',(0,.30,H+.02),(.05,.02,.08),'yellow','AlertYellow',.005)
    box('Lamp_danger',(0,.30,H-.10),(.05,.02,.08),'red','DangerRed',.005)
    # antenna
    cyl('Antenna',(.18,.2,H+.33),(.22,.24,H+.60),.018,'edge','Hull')
    orb('Antenna_tip',(.22,.24,H+.62),(.03,.03,.03),'cyan','Hull')
    rig(bones)
    def idle(t,p):
        ph=t*2*math.pi;lamps(p,green=True)
        translate(p['Hull'],(0,0,.04*math.cos(ph*2)));p['Gyro'].rotation_euler.y=ph
        for b in ('RotorFL','RotorFR','RotorBL','RotorBR'):p[b].rotation_euler.y=ph*3*(1 if 'F' in b else -1)
        p['Eye'].rotation_euler.z=.15*math.sin(ph)
    def warning(t,p):
        ph=t*2*math.pi;lamps(p,yellow=True)
        pulse=.7+.3*math.sin(ph*2)**2;p['AlertYellow'].scale=(pulse,)*3
        translate(p['Hull'],(0,0,.06*math.cos(ph*3)));p['Gyro'].rotation_euler.y=ph*2
        for b in ('RotorFL','RotorFR','RotorBL','RotorBR'):p[b].rotation_euler.y=ph*5*(1 if 'F' in b else -1)
        p['Eye'].rotation_euler.z=.25*math.sin(ph*2)
    def active(t,p):
        ph=t*2*math.pi;lamps(p,red=True)
        translate(p['Hull'],(0,.05*math.sin(ph),.09*math.sin(ph*2)));p['Hull'].rotation_euler.z=.12*math.sin(ph)
        p['Gyro'].rotation_euler.y=ph*3
        for b in ('RotorFL','RotorFR','RotorBL','RotorBR'):p[b].rotation_euler.y=ph*6*(1 if 'F' in b else -1)
        p['Eye'].rotation_euler.z=.35*math.sin(ph*2)
    def hit(t,p):
        q=math.sin(math.pi*t);lamps(p,red=True)
        p['Hull'].rotation_euler.z=.14*q;translate(p['Hull'],(0,.08*q,0))
        p['Eye'].scale=(1-.25*q,)*3;p['Gyro'].scale=(1-.12*q,)*3
    def death(t,p):
        q=t*t*(3-2*t)
        pulse=.6+.4*math.sin(t*8*math.pi)**2
        p['ReadyGreen'].scale=(.0001,)*3;p['AlertYellow'].scale=(.0001,)*3;p['DangerRed'].scale=(pulse,)*3
        translate(p['Hull'],(0,0,-.98*q));p['Hull'].rotation_euler.x=math.radians(84)*q
        p['Gyro'].rotation_euler.y=(1-q)*2*math.pi*t;p['Gyro'].rotation_euler.x=.5*q
        translate(p['Eye'],(0,-.1*q,-.05*q))
    clips=[clip('Idle',61,idle),clip('Warning',49,warning),clip('Active',49,active),clip('Hit',21,hit),clip('Death',49,death)]
    export_rigged(clips,{'purpose':'step 3 passive ability target -- "simulated Xelthara Heavy Unit"; driven by Health + WeaponArea3D + TutorialTarget, never by Enemy3D (no hostile AI)','design':'hovering target drone over a TGT-01 pad; state lamps on named ReadyGreen/AlertYellow/DangerRed bones'})

# ----------------------------------------------------------------------------
# exporters + previews
# ----------------------------------------------------------------------------
def export_static(purpose):
    parent=bpy.data.objects.new(ASSET,None);bpy.context.collection.objects.link(parent)
    parent['units']='meters';parent['origin']='bottom_center';parent['purpose']=purpose;parent['source']='Axiom Force procedural training kit'
    bpy.context.view_layer.update()
    floor=min((obj.matrix_world@Vector(c)).z for obj,bone in PARTS for c in obj.bound_box)
    for obj,bone in PARTS:
        obj.location.z-=floor
        obj.parent=parent
    bpy.context.view_layer.update()
    target=OUTPUT/(ASSET+'.glb')
    if target.exists() and not OVERWRITE:raise RuntimeError('Refusing to overwrite existing asset: %s'%target)
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.gltf(filepath=str(target),export_format='GLB',use_selection=True,export_apply=True,export_animations=False,export_yup=True,export_extras=True)
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
    points=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box];lo=[min(p[i] for p in points) for i in range(3)];hi=[max(p[i] for p in points) for i in range(3)]
    RECORDS.append({'name':ASSET,'path':str(target.relative_to(ROOT)).replace('\\','/'),'bytes':target.stat().st_size,'triangles':triangles,'mesh_objects':len(meshes),'animation_clips':[],'bounds_blender_xyz_m':[lo,hi],'bbox_m':[round(hi[i]-lo[i],4) for i in range(3)],'origin':'0,0,0 at base plane','units':'meters','materials':[m.name for m in {slot.material for obj in meshes for slot in obj.material_slots}],'purpose':purpose,'preview':str((PREVIEW/(ASSET+'.png')).relative_to(ROOT)).replace('\\','/')})
    MANIFEST.write_text(json.dumps({'generator':'tools/assets/build_training_level_assets.py','style':'Neon Pulse / Protocol Zero simulation kit','blender_version':bpy.app.version_string,'format':'glTF 2.0 GLB','assets':RECORDS},indent=2),encoding='utf8')
    print('TRAINING_ASSET_COMPLETE',ASSET,triangles,'triangles',target.stat().st_size,'bytes',flush=True)

def export_rigged(clips,extra):
    target=OUTPUT/(ASSET+'.glb')
    if target.exists() and not OVERWRITE:raise RuntimeError('Refusing to overwrite existing asset: %s'%target)
    for p in RIG.pose.bones:p.location=(0,0,0);p.rotation_euler=(0,0,0);p.scale=(1,1,1)
    bpy.context.scene.render.fps=24;bpy.context.scene.frame_start=1;bpy.context.scene.frame_end=61
    bpy.ops.object.select_all(action='DESELECT');RIG.select_set(True)
    for obj in RIG.children:obj.select_set(True)
    bpy.context.view_layer.objects.active=RIG
    bpy.ops.export_scene.gltf(filepath=str(target),export_format='GLB',use_selection=True,export_animations=True,export_animation_mode='NLA_TRACKS',export_nla_strips=True,export_force_sampling=True,export_skins=True,export_yup=True,export_extras=True,export_cameras=False,export_lights=False)
    meshes=[o for o in RIG.children if o.type=='MESH'];triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
    RIG.data.pose_position='REST'
    bpy.context.view_layer.update()
    points=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box];lo=[min(p[i] for p in points) for i in range(3)];hi=[max(p[i] for p in points) for i in range(3)]
    RECORDS.append({'name':ASSET,'path':str(target.relative_to(ROOT)).replace('\\','/'),'bytes':target.stat().st_size,'triangles':triangles,'mesh_objects':len(meshes),'bones':len(BONES),'animation_clips':clips,'rest_bounds_blender_xyz_m':[lo,hi],'bbox_m':[round(hi[i]-lo[i],4) for i in range(3)],'origin':'0,0,0 at feet or base','units':'meters','materials':[m.name for m in {slot.material for obj in meshes for slot in obj.material_slots}],'preview':str((PREVIEW/(ASSET+'.png')).relative_to(ROOT)).replace('\\','/'),**extra})
    MANIFEST.write_text(json.dumps({'generator':'tools/assets/build_training_level_assets.py','style':'Neon Pulse / Protocol Zero simulation kit','blender_version':bpy.app.version_string,'format':'glTF 2.0 GLB','assets':RECORDS},indent=2),encoding='utf8')
    print('TRAINING_ASSET_COMPLETE',ASSET,triangles,'triangles',target.stat().st_size,'bytes',flush=True)

def render_preview(size,zcenter):
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24
    scene.render.resolution_x=700;scene.render.resolution_y=700;scene.render.resolution_percentage=100
    scene.world.color=(.04,.04,.04)
    bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.03));ground=bpy.context.object
    ground.data.materials.append(MAT['carbon']);ground.name='PreviewGround_QAOnly'
    bpy.ops.object.camera_add(location=(size*1.6,-size*2.6,size*1.55));cam=bpy.context.object;cam.name='PreviewCam_QAOnly'
    cam.rotation_euler=(Vector((0,0,zcenter))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=size*1.40;scene.camera=cam
    for name,loc,power,color,area in [('Key',(size*2,-size*2,size*3),1000,(.70,.85,1),size*3),('Rim',(-size*2,size,size*2),1500,(.1,.6,1),size*2),('Fill',(-size*2,-size*2,size),650,(1,.45,.30),size*3)]:
        bpy.ops.object.light_add(type='AREA',location=loc);light=bpy.context.object;light.name='PreviewLight_QAOnly'+name;light.data.energy=power;light.data.color=color;light.data.shape='DISK';light.data.size=area;light.rotation_euler=(Vector((0,0,zcenter))-light.location).to_track_quat('-Z','Y').to_euler()
    scene.view_settings.view_transform='AgX';scene.render.image_settings.file_format='PNG';scene.render.filepath=str(PREVIEW/(ASSET+'.png'));scene.render.film_transparent=False
    bpy.ops.render.render(write_still=True)

BUILDERS=[('training_grid_floor',grid_floor),('training_divider_platform',divider_platform),
          ('training_waypoint_beacon',waypoint_beacon),('training_calibration_fragment',calibration_fragment),
          ('training_drone',training_drone)]
for name,fn in BUILDERS:
    if ONLY and name not in ONLY:continue
    fn()
    if RIG is not None:
        RIG.data.pose_position='REST'
        bpy.context.view_layer.update()
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    pts=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
    lo=[min(p[i] for p in pts) for i in range(3)];hi=[max(p[i] for p in pts) for i in range(3)]
    render_preview(max(hi[2],hi[0]-lo[0],hi[1]-lo[1]),(hi[2]+lo[2])/2)
print('TRAINING_DELIVERY_COMPLETE',len(RECORDS),flush=True)
