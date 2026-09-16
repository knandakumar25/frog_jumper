"""Build Axiom Force tactical GLBs in Blender 5.2 (standard glTF 2.0).
Run: blender --background --factory-startup --python tools/assets/build_tactical_assets.py
All geometry is original procedural hard-surface art. No third-party source assets.
"""
import bpy, math, json, argparse, sys, random
from pathlib import Path
from mathutils import Vector
ROOT = Path(__file__).resolve().parents[2]
PREVIEW = ROOT / '.validation' / 'tactical-previews'
MANIFEST = ROOT / 'docs' / 'assets' / 'tactical_assets_manifest.json'
ARGS = sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
OVERWRITE = '--overwrite-owned' in ARGS
ONLY = set(next((v.split('=',1)[1] for v in ARGS if v.startswith('--only=')), '').split(',')) - {''}
PREVIEW.mkdir(parents=True, exist_ok=True)
MANIFEST.parent.mkdir(parents=True, exist_ok=True)
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
    ASSET=name; PARTS=[]; BONES={};RIG=None
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
      'cyan':material('EMISSIVE_Cyan_Security',(.01,.74,1),.3,.24,4),
      'magenta':material('EMISSIVE_Magenta_Stealth',(.92,.012,.31),.3,.24,4),
      'cloak':material('EMISSIVE_Magenta_Cloak_Emitter',(.45,.012,1),.2,.30,5),
      'orange':material('EMISSIVE_Orange_Heavy', (1,.16,.008),.3,.24,4),
      'red':material('EMISSIVE_Red_Danger',(1,.008,.015),.2,.3,5),
      'yellow':material('EMISSIVE_Yellow_Warning',(1,.65,.01),.2,.3,4),
      'blue':material('EMISSIVE_Blue_Slow',(.025,.2,1),.2,.3,4),
      'green':material('EMISSIVE_Green_Online',(.025,1,.22),.2,.3,4),
      'white':material('EMISSIVE_White_Optics',(.75,.9,1),.2,.25,3),
    }

def finish_obj(obj,name,mat,bone='Root',bevel=0):
    obj.name=name; obj.data.materials.append(MAT[mat])
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=obj.modifiers.new('Manufactured edge bevel','BEVEL');mod.width=bevel;mod.segments=2
        bpy.context.view_layer.objects.active=obj;bpy.ops.object.modifier_apply(modifier=mod.name)
    for face in obj.data.polygons: face.use_smooth=False
    obj['role']=name;obj['rig_bone']=bone;PARTS.append((obj,bone));return obj

def box(name,loc,scale,mat='armor',bone='Root',bevel=.025,rot=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc,rotation=rot)
    obj=bpy.context.object;obj.dimensions=scale;return finish_obj(obj,name,mat,bone,bevel)

def cyl(name,a,b,r,mat='grey',bone='Root',vertices=12,r2=None):
    a,b=Vector(a),Vector(b);d=b-a
    if r2 is None:bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=d.length,location=(a+b)/2)
    else:bpy.ops.mesh.primitive_cone_add(vertices=vertices,radius1=r,radius2=r2,depth=d.length,location=(a+b)/2)
    obj=bpy.context.object;obj.rotation_euler=d.to_track_quat('Z','Y').to_euler()
    return finish_obj(obj,name,mat,bone,min(r*.16,.015))

def orb(name,loc,scale,mat='rubber',bone='Root'):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,radius=1,location=loc)
    obj=bpy.context.object;obj.scale=scale;return finish_obj(obj,name,mat,bone)

def beam(name,a,b,width,depth,mat='armor',bone='Root'):
    a,b=Vector(a),Vector(b); obj=box(name,(a+b)/2,(width,depth,(b-a).length),mat,bone,min(width*.1,.035))
    obj.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return obj

def label(text,loc,size=.12,mat='edge',bone='Root'):
    bpy.ops.object.text_add(location=loc,rotation=(math.pi/2,0,0))
    obj=bpy.context.object;obj.data.body=text;obj.data.size=size;obj.data.align_x='CENTER';obj.data.extrude=.0008
    bpy.ops.object.convert(target='MESH');return finish_obj(bpy.context.object,'Marking_'+text,mat,bone)

def panel(name,loc,width,height,mat='armor',bone='Root',glow=None):
    x,y,z=loc;box(name,loc,(width,.10,height),mat,bone,.018)
    for dx in [-1,1]:
        for dz in [-1,1]:cyl(name+'_fastener',(x+dx*(width/2-.03),y-.052,z+dz*(height/2-.03)),(x+dx*(width/2-.03),y-.065,z+dz*(height/2-.03)),.013,'edge',bone,6)
    if glow:box(name+'_light',(x,y-.061,z+height*.28),(width*.66,.016,.023),glow,bone,.004)

def rig(bones):
    global RIG,BONES
    bpy.context.view_layer.update()
    floor=min((obj.matrix_world@Vector(c)).z for obj,bone in PARTS for c in obj.bound_box)
    if abs(floor)>0.000001:
        for obj,bone in PARTS: obj.location.z-=floor
        bones=[(name, (head[0],head[1],head[2]-floor), (tail[0],tail[1],tail[2]-floor), parent) if name!='Root' else (name,head,tail,parent) for name,head,tail,parent in bones]
    arm=bpy.data.armatures.new(ASSET+'_Skeleton');RIG=bpy.data.objects.new(ASSET+'_Rig',arm)
    bpy.context.collection.objects.link(RIG);bpy.context.view_layer.objects.active=RIG;RIG.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    for name,head,tail,parent in bones:
        b=arm.edit_bones.new(name);b.head=head;b.tail=tail
        if parent:b.parent=arm.edit_bones[parent]
        BONES[name]=(head,tail,parent)
    bpy.ops.object.mode_set(mode='OBJECT');RIG['units']='meters';RIG['origin']='feet/base at (0,0,0)';RIG['forward']='-Y in Blender; +Z in exported Godot glTF convention'
    # Consolidate by material. Emission remains a separate named mesh and material.
    grouped={}
    for obj,bone in PARTS:
        vg=obj.vertex_groups.new(name=bone);vg.add(list(range(len(obj.data.vertices))),1,'REPLACE')
        grouped.setdefault((obj.data.materials[0].name, bone if bone.startswith('Fragment_') else ''),[]).append(obj)
    for (material_name,fragment_name),objects in grouped.items():
        bpy.ops.object.select_all(action='DESELECT')
        for obj in objects:obj.select_set(True)
        bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join()
        obj=bpy.context.object;obj.name=ASSET+'__'+material_name+('__'+fragment_name if fragment_name else '')
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

def enemy(kind):
    begin(kind);heavy=kind=='heavy';stalker=kind=='stalker';accent='orange' if heavy else 'magenta' if stalker else 'cyan'
    h=2.55 if heavy else 1.88 if stalker else 2.05
    hipz=h*.47; chestz=h*.72;headz=h*.90; width=.84 if heavy else .51 if stalker else .60
    hipy=.08 if stalker else 0;shouldery=-.14 if stalker else 0
    bones=[('Root',(0,0,0),(0,0,.2),None),('Pelvis',(0,hipy,hipz),(0,hipy,hipz+.25),'Root'),('Chest',(0,hipy,hipz+.2),(0,shouldery,chestz+.18),'Pelvis'),('Head',(0,shouldery,chestz+.18),(0,shouldery,headz+.18),'Chest')]
    for side,s in [('L',1),('R',-1)]:
        hip=(s*width*.28,hipy,hipz);knee=(s*width*.33,-.18 if stalker else -.035,h*.28);ankle=(s*width*.37,.10 if stalker else 0,.22)
        shoulder=(s*(width/2+.08),shouldery,chestz+.04);elbow=(s*(width/2+.20),-.03,chestz-.31);hand=(s*(width/2+.21),-.20,chestz-.63)
        bones += [('UpperLeg.'+side,hip,knee,'Pelvis'),('Shin.'+side,knee,ankle,'UpperLeg.'+side),('Foot.'+side,ankle,(ankle[0],-.25,.12),'Shin.'+side),('UpperArm.'+side,shoulder,elbow,'Chest'),('Forearm.'+side,elbow,hand,'UpperArm.'+side),('Hand.'+side,hand,(hand[0],hand[1],hand[2]-.13),'Forearm.'+side)]
        orb('Hip actuator',hip,(.15,.15,.15),'rubber','UpperLeg.'+side)
        beam('Armored thigh',hip,knee,.31 if heavy else .19,.31 if heavy else .20,'armor','UpperLeg.'+side)
        beam('Thigh edge rail',(hip[0]+s*.105,hip[1]-.11,hip[2]-.1),(knee[0]+s*.105,knee[1]-.11,knee[2]+.08),.032,.025,accent,'UpperLeg.'+side)
        orb('Knee hydraulic joint',knee,(.14,.13,.12),'grey','Shin.'+side)
        beam('Shin armor',knee,ankle,.27 if heavy else .16,.26 if heavy else .18,'grey','Shin.'+side)
        box('Knee shield',(knee[0],knee[1]-.15,knee[2]+.02),(.30 if heavy else .20,.13,.23),'armor','Shin.'+side)
        if stalker:
            beam('Digitigrade rear tendon',(knee[0]+s*.08,knee[1]+.10,knee[2]),(ankle[0]+s*.08,ankle[1]+.10,ankle[2]),.045,.045,'edge','Shin.'+side)
        box('Magnetic armored foot',(ankle[0],-.10,.115),(.34 if heavy else .24,.52 if heavy else .38,.23),'armor','Foot.'+side,.04)
        for toe in [-1,1]:box('Toe tread',(ankle[0]+toe*.055,-.28,.105),(.065,.17,.11),'grey','Foot.'+side,.014)
        orb('Shoulder gimbal',shoulder,(.22 if heavy else .13,)*3,'rubber','UpperArm.'+side)
        beam('Upper arm frame',shoulder,elbow,.30 if heavy else .16,.27 if heavy else .17,'grey','UpperArm.'+side)
        box('Shoulder armor',(shoulder[0],shoulder[1],shoulder[2]+.08),(.50 if heavy else .28,.50 if heavy else .28,.31 if heavy else .21),'armor','UpperArm.'+side,.045)
        orb('Elbow',elbow,(.12,.12,.12),'rubber','Forearm.'+side)
        beam('Forearm gauntlet',elbow,hand,.32 if heavy else .20,.30 if heavy else .22,'armor','Forearm.'+side)
        beam('Forearm luminous rail',(elbow[0],elbow[1]-.14,elbow[2]-.08),(hand[0],hand[1]-.14,hand[2]+.06),.032,.022,accent,'Forearm.'+side)
        box('Mechanical palm',hand,(.19,.16,.19),'grey','Hand.'+side,.025)
        for finger in range(3):
            fx=hand[0]+(finger-1)*.052
            cyl('Finger phalanx',(fx,hand[1]-.025,hand[2]-.07),(fx,hand[1]-.075,hand[2]-.20),.022,'edge','Hand.'+side,8)
        if stalker:
            for blade in [-1,1]:cyl('Retractable talon',(hand[0]+blade*.055,hand[1]-.10,hand[2]-.10),(hand[0]+blade*.055,hand[1]-.31,hand[2]-.28),.028,accent,'Hand.'+side,6,.002)
        if heavy:
            cname='Cannon.'+side;bones.append((cname,(shoulder[0],.04,chestz+.23),(shoulder[0],-.50,chestz+.23),'Chest'))
            cyl('Shoulder cannon housing',(shoulder[0],.23,chestz+.30),(shoulder[0],-.34,chestz+.30),.16,'armor',cname,12)
            cyl('Cannon muzzle brake',(shoulder[0],-.33,chestz+.30),(shoulder[0],-.58,chestz+.30),.115,'grey',cname,12)
            cyl('Cannon dark bore',(shoulder[0],-.583,chestz+.30),(shoulder[0],-.591,chestz+.30),.077,'rubber',cname,12)
            for q in range(4):
                angle=q*math.pi/2
                cyl('Muzzle status diode',(shoulder[0]+math.cos(angle)*.09,-.592,chestz+.30+math.sin(angle)*.09),(shoulder[0]+math.cos(angle)*.09,-.601,chestz+.30+math.sin(angle)*.09),.015,accent,cname,6)
    box('Pelvic armored chassis',(0,hipy,hipz+.09),(width*.72,.34,.28),'armor','Pelvis',.05)
    cyl('Spinal actuator',(0,hipy,hipz+.20),(0,shouldery,chestz),.10,'grey','Chest',12)
    for k in range(3):box('Abdominal segmented plate',(0,-.18,hipz+.26+k*.10),(width*.54,.15,.08),'carbon','Chest',.015)
    box('Main chest shell',(0,shouldery,chestz),(width,.43 if heavy else .33,.53 if heavy else .40),'armor','Chest',.075)
    panel('Chest service hatch',(0,shouldery-.23,chestz+.015),width*.76,.30,'grey','Chest',accent)
    box('Reactor slit',(0,shouldery-.297,chestz-.025),(.12,.02,.10),accent,'Chest',.01)
    label('SEC-'+('09' if heavy else '03' if stalker else '01'),(0,shouldery-.302,chestz-.11),.058,'edge','Chest')
    for k in range(5):box('Cooling vent',(width*.33,shouldery-.255,chestz-.12+k*.045),(.09,.025,.014),'rubber','Chest',.003)
    box('Backpack power unit',(0,.27,chestz-.04),(width*.62,.26,.48),'carbon','Chest',.04)
    for side in [-1,1]:cyl('Dorsal capacitor',(side*width*.22,.36,chestz-.24),(side*width*.22,.36,chestz+.20),.065,'grey','Chest',10)
    cyl('Neck motor',(0,shouldery,chestz+.19),(0,shouldery,headz),.08,'rubber','Head',12)
    box('Angular helmet',(0,shouldery-.03,headz),(.39 if heavy else .29,.34,.30),'armor','Head',.05)
    box('Visor glass',(0,shouldery-.217,headz+.01),(.32 if heavy else .245,.025,.06),'rubber','Head',.005)
    box('Optic slit',(0,shouldery-.234,headz+.016),(.28 if heavy else .215,.015,.022),accent,'Head',.003)
    for side in [-1,1]:cyl('Temple sensor',(side*.145,shouldery-.045,headz),(side*.18,shouldery-.045,headz),.052,'grey','Head',10)
    if stalker:
        for side in [-1,1]:cyl('Swept antenna',(side*.10,.03,headz+.1),(side*.20,.13,headz+.30),.025,'armor','Head',6,.004)
        bones.append(('CloakEmitter',(0,.41,chestz),(0,.41,chestz+.12),'Chest'))
        box('Cloak spine emitter',(0,.41,chestz),(.16,.07,.28),'cloak','CloakEmitter',.018)
    if not heavy and not stalker:
        box('Tactical hip pouch',(-.29,.02,hipz+.04),(.15,.25,.22),'carbon','Pelvis',.018)
        box('Rank badge',(.22,shouldery-.19,chestz+.11),(.045,.026,.11),'cyan','Chest',.006)
    rig(bones)
    def movement(t,p,rate=1,amp=.2):
        phase=t*2*math.pi*rate;translate(p['Pelvis'],(0,0,.022*math.cos(phase*2)))
        p['Chest'].rotation_euler.y=.025*math.sin(phase)
        for side,s in [('L',1),('R',-1)]:
            wave=math.sin(phase)*s;p['UpperLeg.'+side].rotation_euler.x=amp*wave
            p['Shin.'+side].rotation_euler.x=max(0,-wave)*amp*.75
            p['UpperArm.'+side].rotation_euler.x=-amp*wave*.7;p['Forearm.'+side].rotation_euler.x=-.1-abs(wave)*.10
            p['Foot.'+side].rotation_euler.x=-amp*wave*.2
    def idle(t,p):
        p['Chest'].rotation_euler.x=.012*math.sin(t*2*math.pi);p['Head'].rotation_euler.y=.06*math.sin(t*2*math.pi)
        if stalker:translate(p['Pelvis'],(0,0,-.10));p['Chest'].rotation_euler.x=.14
    def attack(t,p):
        pulse=math.sin(math.pi*t)**2
        if heavy:
            p['Chest'].rotation_euler.x=-.09*pulse
            for side in ['L','R']:p['Cannon.'+side].location.y=-.09*math.sin(math.pi*min(1,t*2))**2
        elif stalker:
            translate(p['Root'],(0,-.70*pulse,0));translate(p['Pelvis'],(0,0,-.18*(1-pulse)))
            p['Chest'].rotation_euler.x=.25*pulse
            for side in ['L','R']:p['UpperArm.'+side].rotation_euler.x=-1.0*pulse;p['Forearm.'+side].rotation_euler.x=-.35*pulse;p['UpperLeg.'+side].rotation_euler.x=.4*pulse
        else:
            p['Chest'].rotation_euler.y=.22*math.sin(2*math.pi*t);p['UpperArm.R'].rotation_euler.x=-1.25*pulse;p['Forearm.R'].rotation_euler.x=-.50*(1-pulse);p['UpperArm.L'].rotation_euler.x=-.25*pulse
    def hit(t,p):
        q=math.sin(math.pi*t);p['Chest'].rotation_euler.x=-.20*q;p['Head'].rotation_euler.x=.12*q;translate(p['Pelvis'],(0,.06*q,0))
    def death(t,p):
        q=t*t*(3-2*t);p['Root'].rotation_euler.x=math.radians(84)*q;translate(p['Root'],(0,0,.13*q))
        p['UpperArm.L'].rotation_euler.z=-.6*q;p['UpperArm.R'].rotation_euler.z=.6*q;p['Shin.L'].rotation_euler.x=.40*q
    clips=[clip('Idle',61,idle),clip('Walking',49,lambda t,p:movement(t,p,1,.22 if heavy else .32)),clip('Running',33,lambda t,p:movement(t,p,1,.40 if heavy else .62)),clip('Attack',33 if not heavy else 45,attack),clip('Hit',21,hit),clip('Death',49,death)]
    clips.append(clip('Thrust_Slash',45 if heavy else 33,attack))
    if stalker:
        clips.append(clip('Lunge',25,attack))
        def cloak(t,p):
            pulse=math.sin(math.pi*t);p['CloakEmitter'].scale=(.12+.88*math.sin(t*6*math.pi)**2,)*3
            translate(p['Pelvis'],(0,0,-.10*pulse));p['Head'].rotation_euler.x=.10*pulse
            for side in ['L','R']:p['UpperArm.'+side].rotation_euler.x=-.24*pulse
        clips.append(clip('Cloak',49,cloak))
    if heavy:clips.append(clip('Recoil',21,attack))
    export_asset('enemies',clips,{'nominal_height_m':round(h,3),'design':'bulky armored shoulder-cannon tank' if heavy else 'digitigrade prowling assassin' if stalker else 'sleek tactical humanoid android','notes':'Cloak clip is a posture/emitter cue; true material transparency must be driven by game renderer.' if stalker else 'Rigid mechanical skinning; looping locomotion is in place.'})

def prop(kind):
    begin(kind);bones=[('Root',(0,0,0),(0,0,.2),None)]
    def bone(name,head=(0,0,0),parent='Root'):bones.append((name,head,(head[0],head[1],head[2]+.1),parent))
    for name in ['AlertYellow','DangerRed','SlowBlue','ReadyGreen']:bone(name)
    if kind=='explosive_barrel':
        cyl('Pressure canister',(0,0,.10),(0,0,1.12),.38,'armor',vertices=20)
        for z in [.12,.37,.82,1.08]:cyl('Reinforcing ring',(0,0,z-.035),(0,0,z+.035),.405,'grey',vertices=20)
        cyl('Top cap',(0,0,1.11),(0,0,1.18),.34,'carbon',vertices=20)
        cyl('Valve',(0,0,1.18),(0,0,1.26),.07,'edge',vertices=10)
        box('Valve handle',(0,0,1.27),(.26,.05,.04),'warning')
        panel('Hazard placard',(0,-.379,.65),.40,.32,'warning')
        label('HAZARD',(0,-.444,.65),.072,'carbon');label('VOLATILE',(0,-.444,.54),.047,'carbon')
        for a in range(8):
            angle=a*math.pi/4;x,y=.383*math.cos(angle),.383*math.sin(angle)
            obj=box('Low-health warning lamp',(x,y,.96),(.065,.022,.12),'red','DangerRed',.006);obj.rotation_euler.z=angle-math.pi/2
        for side in [-1,1]:box('Carry handle',(side*.31,0,1.19),(.08,.24,.14),'grey')
    elif kind=='hazard_zone_marker':
        box('Hazard field base',(0,0,.045),(2,2,.09),'armor',bevel=.045)
        for x in [-.94,.94]:box('Perimeter rail',(x,0,.11),(.09,2,.13),'grey')
        for y in [-.94,.94]:box('Perimeter rail',(0,y,.11),(2,.09,.13),'grey')
        for k in range(-4,5):
            box('Industrial grid X',(k*.20,0,.102),(.025,1.80,.015),'edge',bevel=.003)
            box('Industrial grid Y',(0,k*.20,.103),(1.80,.025,.015),'edge',bevel=.003)
            box('Burn field strip',(k*.20,0,.118),(.038,1.75,.012),'red','DangerRed',.002)
            box('Slow field strip',(0,k*.20,.12),(1.75,.038,.012),'blue','SlowBlue',.002)
        for x in [-.78,.78]:
            for y in [-.78,.78]:
                box('Field emitter',(x,y,.19),(.23,.23,.22),'carbon')
                cyl('Warning diode',(x,y,.30),(x,y,.32),.065,'yellow','AlertYellow',12)
        label('FIELD / 04',(0,-1.006,.055),.09,'warning')
    elif kind=='timed_trap':
        box('Trap foundation',(0,0,.12),(1.5,1.25,.24),'armor',bevel=.045)
        box('Recessed spike bed',(0,0,.255),(1.3,1.06,.035),'rubber')
        bone('SpikePiston',(0,0,.24))
        for x in [-.42,0,.42]:
            for y in [-.32,0,.32]:
                cyl('Spike socket',(x,y,.24),(x,y,.30),.092,'grey',vertices=12)
                cyl('Retractable spike',(x,y,.14),(x,y,.76),.073,'edge','SpikePiston',6,.003)
        for x in [-.70,.70]:
            box('Yellow warning rail',(x,0,.28),(.045,1.06,.025),'yellow','AlertYellow',.004)
            box('Red active rail',(x,0,.30),(.045,1.06,.025),'red','DangerRed',.004)
        label('CAUTION',(0,-.631,.14),.12,'warning')
        for x in [-.5,-.3,-.1,.1,.3,.5]:box('Safety stripe',(x,.575,.265),(.085,.10,.012),'warning',rot=(0,0,.35),bevel=.002)
    elif kind=='switch_object':
        box('Terminal foot',(0,0,.07),(.70,.58,.14),'grey',bevel=.045)
        box('Terminal pedestal',(0,.04,.63),(.34,.29,1.12),'armor',bevel=.05)
        box('Console housing',(0,-.02,1.15),(.66,.35,.52),'armor',bevel=.04)
        panel('Console face',(0,-.225,1.19),.53,.38,'carbon')
        box('Red locked screen',(0,-.29,1.24),(.40,.014,.17),'red','DangerRed',.008)
        box('Green active screen',(0,-.307,1.24),(.40,.014,.17),'green','ReadyGreen',.008)
        for x in [-.15,-.05,.05,.15]:box('Keypad',(x,-.295,1.03),(.065,.027,.035),'grey',bevel=.006)
        bone('SwitchLever',(0,-.23,.90))
        cyl('Toggle lever',(0,-.24,.90),(0,-.40,1.03),.018,'edge','SwitchLever',8)
        orb('Toggle knob',(0,-.40,1.03),(.04,.04,.04),'warning','SwitchLever')
        label('ACCESS',(0,-.296,1.38),.064,'edge')
    elif kind=='destructible_crate':
        for ix,x in enumerate([-.33,.33]):
            for iy,y in enumerate([-.27,.27]):
                for iz,z in enumerate([.26,.74]):
                    name=f'Fragment_{ix}{iy}{iz}';bone(name,(x,y,z))
                    box('Carbon cover fragment '+name,(x,y,z),(.645,.525,.465),'carbon',name,.025)
                    box('Fragment shell rib',(x,y-.235,z),(.55,.055,.035),'grey',name,.008)
                    for zz in [-.18,.18]:box('Fragment edge reinforcement',(x,y,z+zz),(.65,.53,.035),'armor',name,.008)
                    if iy==0:panel('Fragment front plate',(x,y-.26,z),.49,.32,'armor',name,'cyan')
        label('AXIOM / CARGO',(0,-.588,.98),.064,'edge','Fragment_101')
    rig(bones)
    def lights(p,red=False,yellow=False,blue=False,green=False):
        for key,visible in [('DangerRed',red),('AlertYellow',yellow),('SlowBlue',blue),('ReadyGreen',green)]:p[key].scale=(1 if visible else .0001,)*3
    def state(t,p,mode):
        pulse=.62+.38*math.sin(t*4*math.pi)**2
        if kind=='explosive_barrel':
            lights(p,red=mode!='Idle')
            if mode=='Warning':p['DangerRed'].scale=(pulse,)*3
            if mode=='Active':p['Root'].location.x=.006*math.sin(t*18*math.pi)
        elif kind=='hazard_zone_marker':lights(p,red=mode in ['Active','Burn'],yellow=mode=='Warning',blue=mode=='Slow')
        elif kind=='timed_trap':
            lights(p,red=mode=='Active',yellow=mode=='Warning');translate(p['SpikePiston'],(0,0,0 if mode=='Active' else -.54))
            if mode=='Active':translate(p['SpikePiston'],(0,0,-.05+.05*math.sin(t*2*math.pi)))
        elif kind=='switch_object':lights(p,red=mode=='Idle',yellow=mode=='Warning',green=mode=='Active');p['SwitchLever'].rotation_euler.x=-.5 if mode=='Active' else .3
        elif kind=='destructible_crate':lights(p)
    clips=[clip(name,49,lambda t,p,name=name:state(t,p,name)) for name in (['Idle'] if kind=='destructible_crate' else ['Idle','Warning','Active'])]
    if kind=='hazard_zone_marker':clips += [clip(name,49,lambda t,p,name=name:state(t,p,name)) for name in ['Burn','Slow']]
    if kind=='destructible_crate':
        def shatter(t,p):
            for i,name in enumerate(n for n in BONES if n.startswith('Fragment_')):
                x,y,z=BONES[name][0];pb=p[name];translate(pb,(x*t*2.0,y*t*2.1,(.8*t-1.0*t*t)*(1+z*.3)));pb.rotation_euler=(t*(i%3-.9),t*(i%2-.4),t*(i%4-1.4))
        clips += [clip('Shatter',37,shatter)]
    export_asset('props',clips,{'design':kind.replace('_',' '),'notes':'State colors are separate named emissive material zones, selected with skeletal scale channels; game can independently pulse materials. Shatter fragments are separate rigid vertex groups.' if kind=='destructible_crate' else 'Named color/state bones and separate emissive mesh/material zones support runtime effects.'})

def export_asset(category,clips,extra):
    target=ROOT/'assets'/'models'/category/(ASSET+'.glb');target.parent.mkdir(parents=True,exist_ok=True)
    if target.exists() and not OVERWRITE:raise RuntimeError('Refusing to overwrite existing asset: '+str(target))
    for p in RIG.pose.bones:p.location=(0,0,0);p.rotation_euler=(0,0,0);p.scale=(1,1,1)
    bpy.context.scene.render.fps=24;bpy.context.scene.frame_start=1;bpy.context.scene.frame_end=61
    bpy.ops.object.select_all(action='DESELECT');RIG.select_set(True)
    for obj in RIG.children:obj.select_set(True)
    bpy.context.view_layer.objects.active=RIG
    bpy.ops.export_scene.gltf(filepath=str(target),export_format='GLB',use_selection=True,export_animations=True,export_animation_mode='NLA_TRACKS',export_nla_strips=True,export_force_sampling=True,export_skins=True,export_yup=True,export_extras=True,export_cameras=False,export_lights=False)
    meshes=[o for o in RIG.children if o.type=='MESH'];triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
    RIG.data.pose_position='REST'
    points=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box];lo=[min(p[i] for p in points) for i in range(3)];hi=[max(p[i] for p in points) for i in range(3)]
    render_preview(max(hi[2],hi[0]-lo[0],hi[1]-lo[1]),hi[2]/2)
    RECORDS.append({'name':ASSET,'path':str(target.relative_to(ROOT)).replace('\\','/'),'bytes':target.stat().st_size,'triangles':triangles,'mesh_objects':len(meshes),'bones':len(BONES),'animation_clips':clips,'rest_bounds_blender_xyz_m':[lo,hi],'origin':'0,0,0 at feet or base','units':'meters','materials':[m.name for m in {slot.material for obj in meshes for slot in obj.material_slots}],'preview':str((PREVIEW/(ASSET+'.png')).relative_to(ROOT)).replace('\\','/'),**extra})
    MANIFEST.write_text(json.dumps({'generator':'tools/assets/build_tactical_assets.py','style':'Original procedural stylized hard-surface Neon Pulse assets','blender_version':bpy.app.version_string,'format':'glTF 2.0 GLB','assets':RECORDS},indent=2),encoding='utf8')
    print('TACTICAL_ASSET_COMPLETE',ASSET,triangles,'triangles',target.stat().st_size,'bytes',flush=True)

def render_preview(size,zcenter):
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24
    scene.render.resolution_x=700;scene.render.resolution_y=700;scene.render.resolution_percentage=100
    scene.world.color=(.04,.04,.04)
    bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.03));ground=bpy.context.object
    ground.data.materials.append(MAT['carbon'])
    bpy.ops.object.camera_add(location=(size*1.6,-size*2.6,size*1.55));cam=bpy.context.object
    cam.rotation_euler=(Vector((0,0,zcenter))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=size*1.40;scene.camera=cam
    for name,loc,power,color,area in [('Key',(size*2,-size*2,size*3),1000,(.70,.85,1),size*3),('Rim',(-size*2,size,size*2),1500,(.1,.6,1),size*2),('Fill',(-size*2,-size*2,size),650,(1,.45,.30),size*3)]:
        bpy.ops.object.light_add(type='AREA',location=loc);light=bpy.context.object;light.name='Preview_'+name;light.data.energy=power;light.data.color=color;light.data.shape='DISK';light.data.size=area;light.rotation_euler=(Vector((0,0,zcenter))-light.location).to_track_quat('-Z','Y').to_euler()
    scene.view_settings.view_transform='AgX';scene.render.image_settings.file_format='PNG';scene.render.filepath=str(PREVIEW/(ASSET+'.png'));scene.render.film_transparent=False
    bpy.ops.render.render(write_still=True)

for item in ['vanguard','heavy','stalker']:
    if not ONLY or item in ONLY:enemy(item)
for item in ['explosive_barrel','hazard_zone_marker','timed_trap','switch_object','destructible_crate']:
    if not ONLY or item in ONLY:prop(item)
print('TACTICAL_DELIVERY_COMPLETE',len(RECORDS),flush=True)
