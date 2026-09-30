import bpy
import os
import math
from mathutils import Matrix, Vector, Quaternion

MODELS = [
    "cyber_shogun.glb",
    "solaria.glb",
    "oceanus.glb",
    "surya_prime.glb",
    "mirage_blade.glb",
    "apex_titan.glb"
]

root_dir = r"D:\Projects\frog_jumper"

def ensure_nla_track(arm, track_name, action):
    """Ensures an NLA track exists with a single strip pointing to the specified action."""
    if not arm.animation_data:
        arm.animation_data_create()
    track = arm.animation_data.nla_tracks.get(track_name)
    if not track:
        track = arm.animation_data.nla_tracks.new()
        track.name = track_name
    for s in list(track.strips):
        track.strips.remove(s)
    
    start_frame = int(action.frame_range[0])
    strip = track.strips.new(track_name, start_frame, action)
    strip.action = action
    return track

def get_base_transforms(arm):
    bpy.ops.object.mode_set(mode='POSE')
    base = {}
    for b in arm.pose.bones:
        base[b.name] = {
            'mat': b.matrix.copy(),
            'loc': b.location.copy(),
            'rot': b.rotation_quaternion.copy() if b.rotation_mode == 'QUATERNION' else None,
            'scale': b.scale.copy()
        }
    bpy.ops.object.mode_set(mode='OBJECT')
    return base

def reset_pose_to_base(arm, base):
    bpy.ops.object.mode_set(mode='POSE')
    for b in arm.pose.bones:
        if b.name in base:
            b.location = base[b.name]['loc'].copy()
            if base[b.name]['rot']:
                b.rotation_quaternion = base[b.name]['rot'].copy()
            b.scale = base[b.name]['scale'].copy()
    bpy.ops.object.mode_set(mode='OBJECT')

def author_dash(arm, base, act_name):
    """Author forward dash with strike/slice follow-through (18 frames)."""
    old = bpy.data.actions.get(act_name)
    if old: bpy.data.actions.remove(old)
    act = bpy.data.actions.new(act_name)
    arm.animation_data.action = act
    
    hips = arm.pose.bones.get("Hips")
    orig_hips_mat = base["Hips"]['mat']
    pivot = orig_hips_mat.to_translation()
    
    key_frames = [
        # (frame, frac, y_trans, z_trans, pitch_deg)
        (0,  0.0,   0.0,   0.0,    0.0),
        (3,  0.15,  0.1,  -0.12,  15.0),   # coil / dip
        (7,  0.4,  -1.2,  -0.08,  25.0),   # surge forward
        (12, 0.7,  -1.6,  -0.04,  15.0),   # slide / slice finish
        (18, 1.0,   0.0,   0.0,    0.0),   # recover
    ]
    
    bpy.ops.object.mode_set(mode='POSE')
    for f, frac, y_t, z_t, pitch in key_frames:
        reset_pose_to_base(arm, base)
        bpy.ops.object.mode_set(mode='POSE')
        
        rot = Matrix.Rotation(math.radians(pitch), 4, Vector((1, 0, 0)))
        rot_pivot = Matrix.Translation(pivot) @ rot @ Matrix.Translation(-pivot)
        trans = Matrix.Translation(Vector((0, y_t, z_t)))
        hips.matrix = trans @ rot_pivot @ orig_hips_mat
        bpy.context.view_layer.update()
        
        hips.keyframe_insert(data_path="location", frame=f)
        hips.keyframe_insert(data_path="rotation_quaternion", frame=f)
        
        curl = math.sin(frac * math.pi)
        for sp in ["Spine", "Spine01"]:
            b = arm.pose.bones.get(sp)
            if b and base[sp]['rot']:
                b.rotation_quaternion = base[sp]['rot'] @ Quaternion(Vector((1, 0, 0)), curl * 0.3)
                b.keyframe_insert(data_path="rotation_quaternion", frame=f)
                
        ra = arm.pose.bones.get("RightArm")
        if ra and base["RightArm"]['rot']:
            slash_rot = Quaternion(Vector((0, 1, 0)), -curl * 0.9) @ Quaternion(Vector((1, 0, 0)), curl * 0.5)
            ra.rotation_quaternion = base["RightArm"]['rot'] @ slash_rot
            ra.keyframe_insert(data_path="rotation_quaternion", frame=f)
            
        la = arm.pose.bones.get("LeftArm")
        if la and base["LeftArm"]['rot']:
            la_rot = Quaternion(Vector((1, 0, 0)), -curl * 0.7)
            la.rotation_quaternion = base["LeftArm"]['rot'] @ la_rot
            la.keyframe_insert(data_path="rotation_quaternion", frame=f)
            
        ll = arm.pose.bones.get("LeftUpLeg")
        rl = arm.pose.bones.get("RightUpLeg")
        if ll and base["LeftUpLeg"]['rot']:
            ll.rotation_quaternion = base["LeftUpLeg"]['rot'] @ Quaternion(Vector((1, 0, 0)), curl * 0.6)
            ll.keyframe_insert(data_path="rotation_quaternion", frame=f)
        if rl and base["RightUpLeg"]['rot']:
            rl.rotation_quaternion = base["RightUpLeg"]['rot'] @ Quaternion(Vector((1, 0, 0)), -curl * 0.6)
            rl.keyframe_insert(data_path="rotation_quaternion", frame=f)

    bpy.ops.object.mode_set(mode='OBJECT')
    ensure_nla_track(arm, act_name, act)
    return act

def author_ground_slam(arm, base, act_name):
    """Author leaping ground slam impact (24 frames)."""
    old = bpy.data.actions.get(act_name)
    if old: bpy.data.actions.remove(old)
    act = bpy.data.actions.new(act_name)
    arm.animation_data.action = act
    
    hips = arm.pose.bones.get("Hips")
    orig_hips_mat = base["Hips"]['mat']
    pivot = orig_hips_mat.to_translation()
    
    key_frames = [
        # (frame, frac, y_trans, z_trans, pitch_deg)
        (0,  0.0,   0.0,   0.0,    0.0),
        (5,  0.2,   0.0,   0.45, -15.0),   # rise / windup
        (9,  0.375, 0.0,   0.55, -10.0),   # apex hold
        (13, 0.54, -0.2,  -0.45,  30.0),   # slam impact
        (18, 0.75, -0.2,  -0.40,  25.0),   # crater shockwave hold
        (24, 1.0,   0.0,   0.0,    0.0),   # recover
    ]
    
    bpy.ops.object.mode_set(mode='POSE')
    for f, frac, y_t, z_t, pitch in key_frames:
        reset_pose_to_base(arm, base)
        bpy.ops.object.mode_set(mode='POSE')
        
        rot = Matrix.Rotation(math.radians(pitch), 4, Vector((1, 0, 0)))
        rot_pivot = Matrix.Translation(pivot) @ rot @ Matrix.Translation(-pivot)
        trans = Matrix.Translation(Vector((0, y_t, z_t)))
        hips.matrix = trans @ rot_pivot @ orig_hips_mat
        bpy.context.view_layer.update()
        
        hips.keyframe_insert(data_path="location", frame=f)
        hips.keyframe_insert(data_path="rotation_quaternion", frame=f)
        
        if f <= 9:
            arm_raise = min(1.0, f / 6.0)
            for a_name in ["LeftArm", "RightArm"]:
                b = arm.pose.bones.get(a_name)
                if b and base[a_name]['rot']:
                    up_rot = Quaternion(Vector((1, 0, 0)), -arm_raise * 1.6)
                    b.rotation_quaternion = base[a_name]['rot'] @ up_rot
                    b.keyframe_insert(data_path="rotation_quaternion", frame=f)
        else:
            slam_factor = math.sin((f - 9) / 15.0 * math.pi)
            for a_name in ["LeftArm", "RightArm"]:
                b = arm.pose.bones.get(a_name)
                if b and base[a_name]['rot']:
                    down_rot = Quaternion(Vector((1, 0, 0)), slam_factor * 1.2)
                    b.rotation_quaternion = base[a_name]['rot'] @ down_rot
                    b.keyframe_insert(data_path="rotation_quaternion", frame=f)
                    
        if f >= 13:
            squat = math.sin((f - 13) / 11.0 * math.pi)
            for k_name in ["LeftLeg", "RightLeg"]:
                b = arm.pose.bones.get(k_name)
                if b and base[k_name]['rot']:
                    b.rotation_quaternion = base[k_name]['rot'] @ Quaternion(Vector((1, 0, 0)), squat * 1.0)
                    b.keyframe_insert(data_path="rotation_quaternion", frame=f)

    bpy.ops.object.mode_set(mode='OBJECT')
    ensure_nla_track(arm, act_name, act)
    return act

def author_shield_bash(arm, base, act_name):
    """Author forward shield bash / vanguard ram (18 frames)."""
    old = bpy.data.actions.get(act_name)
    if old: bpy.data.actions.remove(old)
    act = bpy.data.actions.new(act_name)
    arm.animation_data.action = act
    
    hips = arm.pose.bones.get("Hips")
    orig_hips_mat = base["Hips"]['mat']
    pivot = orig_hips_mat.to_translation()
    
    key_frames = [
        # (frame, frac, y_trans, z_trans, pitch_deg, yaw_deg)
        (0,  0.0,   0.0,   0.0,    0.0,   0.0),
        (3,  0.15,  0.1,  -0.12,  15.0, -15.0),   # coil back & drop into shield
        (7,  0.4,  -1.3,  -0.08,  20.0,  10.0),   # explosive ram forward
        (12, 0.7,  -1.5,  -0.04,  15.0,   5.0),   # impact follow-through
        (18, 1.0,   0.0,   0.0,    0.0,   0.0),   # recover
    ]
    
    bpy.ops.object.mode_set(mode='POSE')
    for f, frac, y_t, z_t, pitch, yaw in key_frames:
        reset_pose_to_base(arm, base)
        bpy.ops.object.mode_set(mode='POSE')
        
        rot_pitch = Matrix.Rotation(math.radians(pitch), 4, Vector((1, 0, 0)))
        rot_yaw = Matrix.Rotation(math.radians(yaw), 4, Vector((0, 0, 1)))
        rot = rot_yaw @ rot_pitch
        rot_pivot = Matrix.Translation(pivot) @ rot @ Matrix.Translation(-pivot)
        trans = Matrix.Translation(Vector((0, y_t, z_t)))
        hips.matrix = trans @ rot_pivot @ orig_hips_mat
        bpy.context.view_layer.update()
        
        hips.keyframe_insert(data_path="location", frame=f)
        hips.keyframe_insert(data_path="rotation_quaternion", frame=f)
        
        pulse = math.sin(frac * math.pi)
        la = arm.pose.bones.get("LeftArm")
        lf = arm.pose.bones.get("LeftForeArm")
        ra = arm.pose.bones.get("RightArm")
        
        if la and base["LeftArm"]['rot']:
            la_rot = Quaternion(Vector((1, 0, 0)), pulse * 1.1) @ Quaternion(Vector((0, 1, 0)), pulse * 0.4)
            la.rotation_quaternion = base["LeftArm"]['rot'] @ la_rot
            la.keyframe_insert(data_path="rotation_quaternion", frame=f)
        if lf and base["LeftForeArm"]['rot']:
            lf_rot = Quaternion(Vector((1, 0, 0)), pulse * 0.8)
            lf.rotation_quaternion = base["LeftForeArm"]['rot'] @ lf_rot
            lf.keyframe_insert(data_path="rotation_quaternion", frame=f)
        if ra and base["RightArm"]['rot']:
            ra_rot = Quaternion(Vector((1, 0, 0)), -pulse * 0.5)
            ra.rotation_quaternion = base["RightArm"]['rot'] @ ra_rot
            ra.keyframe_insert(data_path="rotation_quaternion", frame=f)
            
    bpy.ops.object.mode_set(mode='OBJECT')
    ensure_nla_track(arm, act_name, act)
    return act

def author_beam_or_blast(arm, base, act_name):
    """Author beam cast or fluid torrent blast (20 frames)."""
    old = bpy.data.actions.get(act_name)
    if old: bpy.data.actions.remove(old)
    act = bpy.data.actions.new(act_name)
    arm.animation_data.action = act
    
    hips = arm.pose.bones.get("Hips")
    orig_hips_mat = base["Hips"]['mat']
    pivot = orig_hips_mat.to_translation()
    
    key_frames = [
        # (frame, frac, y_trans, z_trans, pitch_deg)
        (0,  0.0,   0.0,   0.0,    0.0),
        (4,  0.2,   0.05, -0.08,   8.0),   # gather / coil
        (8,  0.4,  -0.25, -0.04,  15.0),   # thrust blast
        (14, 0.7,  -0.20, -0.03,  12.0),   # sustain & recoil
        (20, 1.0,   0.0,   0.0,    0.0),   # recover
    ]
    
    bpy.ops.object.mode_set(mode='POSE')
    for f, frac, y_t, z_t, pitch in key_frames:
        reset_pose_to_base(arm, base)
        bpy.ops.object.mode_set(mode='POSE')
        
        rot = Matrix.Rotation(math.radians(pitch), 4, Vector((1, 0, 0)))
        rot_pivot = Matrix.Translation(pivot) @ rot @ Matrix.Translation(-pivot)
        trans = Matrix.Translation(Vector((0, y_t, z_t)))
        hips.matrix = trans @ rot_pivot @ orig_hips_mat
        bpy.context.view_layer.update()
        
        hips.keyframe_insert(data_path="location", frame=f)
        hips.keyframe_insert(data_path="rotation_quaternion", frame=f)
        
        pulse = math.sin(frac * math.pi)
        for a_name in ["LeftArm", "RightArm"]:
            b = arm.pose.bones.get(a_name)
            if b and base[a_name]['rot']:
                thrust_rot = Quaternion(Vector((1, 0, 0)), pulse * 1.1)
                b.rotation_quaternion = base[a_name]['rot'] @ thrust_rot
                b.keyframe_insert(data_path="rotation_quaternion", frame=f)
                
        sp = arm.pose.bones.get("Spine")
        if sp and base["Spine"]['rot']:
            sp.rotation_quaternion = base["Spine"]['rot'] @ Quaternion(Vector((1, 0, 0)), pulse * 0.25)
            sp.keyframe_insert(data_path="rotation_quaternion", frame=f)

    bpy.ops.object.mode_set(mode='OBJECT')
    ensure_nla_track(arm, act_name, act)
    return act

def author_parry_or_guard(arm, base, act_name):
    """Author defensive parry / vanguard shield guard (20 frames)."""
    old = bpy.data.actions.get(act_name)
    if old: bpy.data.actions.remove(old)
    act = bpy.data.actions.new(act_name)
    arm.animation_data.action = act
    
    hips = arm.pose.bones.get("Hips")
    orig_hips_mat = base["Hips"]['mat']
    pivot = orig_hips_mat.to_translation()
    
    key_frames = [
        # (frame, frac, y_trans, z_trans, pitch_deg)
        (0,  0.0,   0.0,   0.0,    0.0),
        (3,  0.15,  0.0,  -0.12,  10.0),   # snap guard
        (7,  0.35,  0.06, -0.10,   5.0),   # deflection absorb
        (13, 0.65,  0.03, -0.08,   8.0),   # hold ready
        (20, 1.0,   0.0,   0.0,    0.0),   # recover
    ]
    
    bpy.ops.object.mode_set(mode='POSE')
    for f, frac, y_t, z_t, pitch in key_frames:
        reset_pose_to_base(arm, base)
        bpy.ops.object.mode_set(mode='POSE')
        
        rot = Matrix.Rotation(math.radians(pitch), 4, Vector((1, 0, 0)))
        rot_pivot = Matrix.Translation(pivot) @ rot @ Matrix.Translation(-pivot)
        trans = Matrix.Translation(Vector((0, y_t, z_t)))
        hips.matrix = trans @ rot_pivot @ orig_hips_mat
        bpy.context.view_layer.update()
        
        hips.keyframe_insert(data_path="location", frame=f)
        hips.keyframe_insert(data_path="rotation_quaternion", frame=f)
        
        guard_factor = math.sin(frac * math.pi)
        ra = arm.pose.bones.get("RightArm")
        la = arm.pose.bones.get("LeftArm")
        
        if ra and base["RightArm"]['rot']:
            g_rot = Quaternion(Vector((1, 0, 0)), guard_factor * 0.9) @ Quaternion(Vector((0, 1, 0)), -guard_factor * 0.7)
            ra.rotation_quaternion = base["RightArm"]['rot'] @ g_rot
            ra.keyframe_insert(data_path="rotation_quaternion", frame=f)
            
        if la and base["LeftArm"]['rot']:
            la_rot = Quaternion(Vector((1, 0, 0)), guard_factor * 0.6) @ Quaternion(Vector((0, 1, 0)), guard_factor * 0.5)
            la.rotation_quaternion = base["LeftArm"]['rot'] @ la_rot
            la.keyframe_insert(data_path="rotation_quaternion", frame=f)

    bpy.ops.object.mode_set(mode='OBJECT')
    ensure_nla_track(arm, act_name, act)
    return act

def author_radial_burst_or_vortex(arm, base, act_name):
    """Author supernova burst / radial vortex summon (22 frames)."""
    old = bpy.data.actions.get(act_name)
    if old: bpy.data.actions.remove(old)
    act = bpy.data.actions.new(act_name)
    arm.animation_data.action = act
    
    hips = arm.pose.bones.get("Hips")
    orig_hips_mat = base["Hips"]['mat']
    pivot = orig_hips_mat.to_translation()
    
    key_frames = [
        # (frame, frac, y_trans, z_trans, pitch_deg)
        (0,  0.0,   0.0,   0.0,    0.0),
        (5,  0.22,  0.0,  -0.12,  12.0),   # crouch & gather
        (9,  0.4,  -0.05,  0.15, -20.0),   # arch back & explosive radial flare
        (15, 0.68, -0.02,  0.05,  -5.0),   # energy sustain
        (22, 1.0,   0.0,   0.0,    0.0),   # recover
    ]
    
    bpy.ops.object.mode_set(mode='POSE')
    for f, frac, y_t, z_t, pitch in key_frames:
        reset_pose_to_base(arm, base)
        bpy.ops.object.mode_set(mode='POSE')
        
        rot = Matrix.Rotation(math.radians(pitch), 4, Vector((1, 0, 0)))
        rot_pivot = Matrix.Translation(pivot) @ rot @ Matrix.Translation(-pivot)
        trans = Matrix.Translation(Vector((0, y_t, z_t)))
        hips.matrix = trans @ rot_pivot @ orig_hips_mat
        bpy.context.view_layer.update()
        
        hips.keyframe_insert(data_path="location", frame=f)
        hips.keyframe_insert(data_path="rotation_quaternion", frame=f)
        
        ra = arm.pose.bones.get("RightArm")
        la = arm.pose.bones.get("LeftArm")
        
        if f <= 5:
            gather = f / 5.0
            if ra and base["RightArm"]['rot']:
                ra.rotation_quaternion = base["RightArm"]['rot'] @ Quaternion(Vector((0, 1, 0)), -gather * 0.5)
                ra.keyframe_insert(data_path="rotation_quaternion", frame=f)
            if la and base["LeftArm"]['rot']:
                la.rotation_quaternion = base["LeftArm"]['rot'] @ Quaternion(Vector((0, 1, 0)), gather * 0.5)
                la.keyframe_insert(data_path="rotation_quaternion", frame=f)
        elif f <= 15:
            flare = math.sin((f - 5) / 10.0 * math.pi)
            if ra and base["RightArm"]['rot']:
                ra.rotation_quaternion = base["RightArm"]['rot'] @ Quaternion(Vector((0, 0, 1)), flare * 1.3)
                ra.keyframe_insert(data_path="rotation_quaternion", frame=f)
            if la and base["LeftArm"]['rot']:
                la.rotation_quaternion = base["LeftArm"]['rot'] @ Quaternion(Vector((0, 0, 1)), -flare * 1.3)
                la.keyframe_insert(data_path="rotation_quaternion", frame=f)
        else:
            if ra and base["RightArm"]['rot']:
                ra.rotation_quaternion = base["RightArm"]['rot']
                ra.keyframe_insert(data_path="rotation_quaternion", frame=f)
            if la and base["LeftArm"]['rot']:
                la.rotation_quaternion = base["LeftArm"]['rot']
                la.keyframe_insert(data_path="rotation_quaternion", frame=f)

    bpy.ops.object.mode_set(mode='OBJECT')
    ensure_nla_track(arm, act_name, act)
    return act

def author_tether_pull(arm, base, act_name):
    """Author magnetic tether rail pull / yank (20 frames)."""
    old = bpy.data.actions.get(act_name)
    if old: bpy.data.actions.remove(old)
    act = bpy.data.actions.new(act_name)
    arm.animation_data.action = act
    
    hips = arm.pose.bones.get("Hips")
    orig_hips_mat = base["Hips"]['mat']
    pivot = orig_hips_mat.to_translation()
    
    key_frames = [
        # (frame, frac, y_trans, z_trans, pitch_deg)
        (0,  0.0,   0.0,   0.0,    0.0),
        (4,  0.2,  -0.2,  -0.05,  15.0),   # punch forward firing tether
        (9,  0.45,  0.15, -0.12, -10.0),   # violent yank back past hip
        (14, 0.7,   0.10, -0.08,  -5.0),   # power hold
        (20, 1.0,   0.0,   0.0,    0.0),   # recover
    ]
    
    bpy.ops.object.mode_set(mode='POSE')
    for f, frac, y_t, z_t, pitch in key_frames:
        reset_pose_to_base(arm, base)
        bpy.ops.object.mode_set(mode='POSE')
        
        rot = Matrix.Rotation(math.radians(pitch), 4, Vector((1, 0, 0)))
        rot_pivot = Matrix.Translation(pivot) @ rot @ Matrix.Translation(-pivot)
        trans = Matrix.Translation(Vector((0, y_t, z_t)))
        hips.matrix = trans @ rot_pivot @ orig_hips_mat
        bpy.context.view_layer.update()
        
        hips.keyframe_insert(data_path="location", frame=f)
        hips.keyframe_insert(data_path="rotation_quaternion", frame=f)
        
        ra = arm.pose.bones.get("RightArm")
        rf = arm.pose.bones.get("RightForeArm")
        
        if f <= 4:
            if ra and base["RightArm"]['rot']:
                ra.rotation_quaternion = base["RightArm"]['rot'] @ Quaternion(Vector((1, 0, 0)), 1.2)
                ra.keyframe_insert(data_path="rotation_quaternion", frame=f)
        elif f <= 14:
            if ra and base["RightArm"]['rot']:
                ra.rotation_quaternion = base["RightArm"]['rot'] @ Quaternion(Vector((1, 0, 0)), -0.8)
                ra.keyframe_insert(data_path="rotation_quaternion", frame=f)
            if rf and base["RightForeArm"]['rot']:
                rf.rotation_quaternion = base["RightForeArm"]['rot'] @ Quaternion(Vector((1, 0, 0)), 1.4)
                rf.keyframe_insert(data_path="rotation_quaternion", frame=f)
        else:
            if ra and base["RightArm"]['rot']:
                ra.rotation_quaternion = base["RightArm"]['rot']
                ra.keyframe_insert(data_path="rotation_quaternion", frame=f)
            if rf and base["RightForeArm"]['rot']:
                rf.rotation_quaternion = base["RightForeArm"]['rot']
                rf.keyframe_insert(data_path="rotation_quaternion", frame=f)

    bpy.ops.object.mode_set(mode='OBJECT')
    ensure_nla_track(arm, act_name, act)
    return act

# ─── Process All 6 Models ──────────────────────────────────────────────────
for m in MODELS:
    path = os.path.join(root_dir, "assets", "models", "hq", m)
    print(f"\n=======================================================")
    print(f"Authoring abilities for {m}...")
    print(f"=======================================================")
    
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=path)
    
    arm = [o for o in bpy.data.objects if o.type == 'ARMATURE'][0]
    bpy.context.view_layer.objects.active = arm
    arm.select_set(True)
    
    idle_act = bpy.data.actions.get("Idle")
    if idle_act:
        arm.animation_data.action = idle_act
        bpy.context.scene.frame_set(0)
    base = get_base_transforms(arm)
    
    # 1. Generic Dash on all 6 heroes
    author_dash(arm, base, "Dash")
    print("  [+] Authored: 'Dash'")
    
    # 2. Generic Ground Slam on all 6 heroes
    author_ground_slam(arm, base, "Ground_Slam")
    print("  [+] Authored: 'Ground_Slam'")
    
    # 3. Hero-specific abilities
    if m == "cyber_shogun.glb":
        act_p = author_dash(arm, base, "Blade_Dash")
        act_s = author_parry_or_guard(arm, base, "Parry_Stance")
        ensure_nla_track(arm, "Primary_Ability", act_p)
        ensure_nla_track(arm, "Secondary_Ability", act_s)
        print("  [+] Authored: 'Blade_Dash', 'Parry_Stance'")
    elif m == "solaria.glb":
        act_p = author_beam_or_blast(arm, base, "Beam_Cast")
        act_s = author_radial_burst_or_vortex(arm, base, "Supernova_Burst")
        ensure_nla_track(arm, "Primary_Ability", act_p)
        ensure_nla_track(arm, "Secondary_Ability", act_s)
        print("  [+] Authored: 'Beam_Cast', 'Supernova_Burst'")
    elif m == "oceanus.glb":
        act_p = author_beam_or_blast(arm, base, "Torrent_Blast")
        act_s = author_radial_burst_or_vortex(arm, base, "Vortex_Cast")
        ensure_nla_track(arm, "Primary_Ability", act_p)
        ensure_nla_track(arm, "Secondary_Ability", act_s)
        print("  [+] Authored: 'Torrent_Blast', 'Vortex_Cast'")
    elif m == "surya_prime.glb":
        act_p = author_shield_bash(arm, base, "Shield_Bash")
        act_s = author_parry_or_guard(arm, base, "Shield_Guard")
        ensure_nla_track(arm, "Primary_Ability", act_p)
        ensure_nla_track(arm, "Secondary_Ability", act_s)
        print("  [+] Authored: 'Shield_Bash', 'Shield_Guard'")
    elif m == "mirage_blade.glb":
        act_p = author_dash(arm, base, "Phase_Dash")
        act_s = author_parry_or_guard(arm, base, "Decoy_Deploy")
        ensure_nla_track(arm, "Primary_Ability", act_p)
        ensure_nla_track(arm, "Secondary_Ability", act_s)
        print("  [+] Authored: 'Phase_Dash', 'Decoy_Deploy'")
    elif m == "apex_titan.glb":
        act_p = author_ground_slam(arm, base, "Tectonic_Slam")
        act_s = author_tether_pull(arm, base, "Magnetic_Pull")
        ensure_nla_track(arm, "Primary_Ability", act_p)
        ensure_nla_track(arm, "Secondary_Ability", act_s)
        print("  [+] Authored: 'Tectonic_Slam', 'Magnetic_Pull'")
        
    bpy.ops.export_scene.gltf(
        filepath=path,
        export_format='GLB',
        export_animations=True,
        export_all_influences=True,
        export_skins=True
    )
    print(f"Successfully exported {m} with abilities to {path}")

print("\nAll 6 heroes successfully updated with specialized ability animations!")
