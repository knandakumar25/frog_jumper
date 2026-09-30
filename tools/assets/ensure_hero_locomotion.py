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

for m in MODELS:
    path = os.path.join(root_dir, "assets", "models", "hq", m)
    print(f"\n=======================================================")
    print(f"Processing {m}...")
    print(f"=======================================================")
    
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=path)
    
    arm = None
    for o in bpy.data.objects:
        if o.type == 'ARMATURE':
            arm = o
            break
            
    if not arm:
        print(f"ERROR: No armature found in {m}!")
        continue
        
    bpy.context.view_layer.objects.active = arm
    arm.select_set(True)
    if not arm.animation_data:
        arm.animation_data_create()

    # ─────────────────────────────────────────────────────────────────────────
    # 1. Ensure "Idle"
    # ─────────────────────────────────────────────────────────────────────────
    idle_act = bpy.data.actions.get("Idle")
    if not idle_act:
        for a in bpy.data.actions:
            if "idle" in a.name.lower() and (a.frame_range[1] - a.frame_range[0] > 10):
                idle_act = a
                break
    if idle_act:
        ensure_nla_track(arm, "Idle", idle_act)
        print(f"  [OK] Idle: '{idle_act.name}' ({idle_act.frame_range[0]}..{idle_act.frame_range[1]})")

    # ─────────────────────────────────────────────────────────────────────────
    # 2. Ensure "Walking"
    # ─────────────────────────────────────────────────────────────────────────
    walk_act = bpy.data.actions.get("Walking")
    if not walk_act:
        for a in bpy.data.actions:
            if "walk" in a.name.lower() and (a.frame_range[1] - a.frame_range[0] > 10):
                walk_act = a
                break
    if walk_act:
        ensure_nla_track(arm, "Walking", walk_act)
        print(f"  [OK] Walking: '{walk_act.name}' ({walk_act.frame_range[0]}..{walk_act.frame_range[1]})")

    # ─────────────────────────────────────────────────────────────────────────
    # 3. Ensure "Running" (Fix 1-frame stubs)
    # ─────────────────────────────────────────────────────────────────────────
    run_act = bpy.data.actions.get("Running")
    needs_run_fix = (run_act is None) or ((run_act.frame_range[1] - run_act.frame_range[0]) <= 2)
    if needs_run_fix:
        # Search for full running action
        full_run = None
        for a in bpy.data.actions:
            if "run" in a.name.lower() and (a.frame_range[1] - a.frame_range[0] > 8):
                full_run = a
                break
        if full_run:
            print(f"  Fixing Running: copying from '{full_run.name}' ({full_run.frame_range[0]}..{full_run.frame_range[1]})")
            if run_act:
                bpy.data.actions.remove(run_act)
            run_act = full_run.copy()
            run_act.name = "Running"
        else:
            print(f"  WARNING: Could not find full running action for {m}!")
            
    if run_act:
        ensure_nla_track(arm, "Running", run_act)
        print(f"  [OK] Running: '{run_act.name}' ({run_act.frame_range[0]}..{run_act.frame_range[1]})")

    # ─────────────────────────────────────────────────────────────────────────
    # 4. Ensure "Jumping"
    # ─────────────────────────────────────────────────────────────────────────
    jump_act = bpy.data.actions.get("Jumping")
    if not jump_act:
        for a in bpy.data.actions:
            if "jump" in a.name.lower() and "double" not in a.name.lower() and (a.frame_range[1] - a.frame_range[0] > 10):
                jump_act = a
                break
    if jump_act:
        ensure_nla_track(arm, "Jumping", jump_act)
        print(f"  [OK] Jumping: '{jump_act.name}' ({jump_act.frame_range[0]}..{jump_act.frame_range[1]})")

    # ─────────────────────────────────────────────────────────────────────────
    # 5. Create "Standing" (Upright poised standing stance with breathing wave)
    # ─────────────────────────────────────────────────────────────────────────
    # Remove existing Standing if present to ensure clean authoring
    old_standing = bpy.data.actions.get("Standing")
    if old_standing:
        bpy.data.actions.remove(old_standing)
        
    standing_act = bpy.data.actions.new("Standing")
    arm.animation_data.action = standing_act
    
    # Base pose on frame 0 of Idle
    if idle_act:
        arm.animation_data.action = idle_act
        bpy.context.scene.frame_set(0)
    else:
        bpy.context.scene.frame_set(0)
        
    bpy.ops.object.mode_set(mode='POSE')
    base_transforms = {}
    for b in arm.pose.bones:
        base_transforms[b.name] = {
            'loc': b.location.copy(),
            'rot': b.rotation_quaternion.copy() if b.rotation_mode == 'QUATERNION' else None,
            'scale': b.scale.copy()
        }
        
    arm.animation_data.action = standing_act
    
    # 48-frame breathing standing cycle
    for f in [0, 12, 24, 36, 48]:
        frac = f / 48.0
        breath = math.sin(frac * 2.0 * math.pi)
        
        for b in arm.pose.bones:
            b_data = base_transforms[b.name]
            b.location = b_data['loc'].copy()
            if b_data['rot']:
                b.rotation_quaternion = b_data['rot'].copy()
            b.scale = b_data['scale'].copy()
            
            # Add subtle breathing rise to spine
            if b.name in ['Spine01', 'Spine02']:
                if b_data['rot']:
                    pitch_delta = Quaternion(Vector((1, 0, 0)), breath * 0.02)
                    b.rotation_quaternion = b_data['rot'] @ pitch_delta
            elif b.name in ['LeftArm', 'RightArm']:
                if b_data['rot']:
                    sway = Quaternion(Vector((0, 1, 0)), breath * 0.015)
                    b.rotation_quaternion = b_data['rot'] @ sway
                    
            b.keyframe_insert(data_path="location", frame=f)
            if b.rotation_mode == 'QUATERNION':
                b.keyframe_insert(data_path="rotation_quaternion", frame=f)
                
    bpy.ops.object.mode_set(mode='OBJECT')
    ensure_nla_track(arm, "Standing", standing_act)
    print(f"  [OK] Standing: created ({standing_act.frame_range[0]}..{standing_act.frame_range[1]})")

    # ─────────────────────────────────────────────────────────────────────────
    # 6. Create "Double_Jump" (Acrobatic aerial somersault + vertical boost)
    # ─────────────────────────────────────────────────────────────────────────
    old_dj = bpy.data.actions.get("Double_Jump")
    if old_dj:
        bpy.data.actions.remove(old_dj)
        
    # Sample mid-air pose from Jumping frame 10 (or base pose)
    if jump_act:
        arm.animation_data.action = jump_act
        bpy.context.scene.frame_set(10)
    else:
        bpy.context.scene.frame_set(0)
        
    bpy.ops.object.mode_set(mode='POSE')
    jump_transforms = {}
    for b in arm.pose.bones:
        jump_transforms[b.name] = {
            'mat': b.matrix.copy(),
            'loc': b.location.copy(),
            'rot': b.rotation_quaternion.copy() if b.rotation_mode == 'QUATERNION' else None
        }
        
    double_jump_act = bpy.data.actions.new("Double_Jump")
    arm.animation_data.action = double_jump_act
    
    hips = arm.pose.bones.get("Hips")
    orig_hips_mat = jump_transforms["Hips"]['mat']
    pivot = orig_hips_mat.to_translation()
    
    frames_data = [
        (0,  0.0,   0.0,   0.0),    # start air pose
        (3,  0.125, 0.125, 0.2),    # gather / pop
        (6,  0.25,  0.25,  0.45),   # quarter flip, tucking
        (9,  0.375, 0.375, 0.62),   # rotating into apex
        (12, 0.5,   0.5,   0.7),    # half flip - inverted full tuck apex
        (15, 0.625, 0.625, 0.62),   # rotating out of apex
        (18, 0.75,  0.75,  0.45),   # three-quarters, opening
        (21, 0.875, 0.875, 0.2),    # near finish
        (24, 1.0,   1.0,   0.0),    # full somersault landing prep
    ]
    
    for f, frac, rot_frac, z_mult in frames_data:
        # Hips rotation & vertical trajectory
        angle = rot_frac * 2.0 * math.pi
        rot = Matrix.Rotation(angle, 4, Vector((1, 0, 0)))
        rot_pivot = Matrix.Translation(pivot) @ rot @ Matrix.Translation(-pivot)
        z_lift = z_mult * 0.7
        lift_mat = Matrix.Translation(Vector((0, 0, z_lift)))
        
        hips.matrix = lift_mat @ rot_pivot @ orig_hips_mat
        bpy.context.view_layer.update()
        
        hips.keyframe_insert(data_path="location", frame=f)
        hips.keyframe_insert(data_path="rotation_quaternion", frame=f)
        
        # Tuck factor: 0 at start/end, peaks at mid-flip
        tuck = math.sin(frac * math.pi)
        
        for leg_name in ["LeftUpLeg", "RightUpLeg"]:
            b = arm.pose.bones.get(leg_name)
            if b and jump_transforms.get(leg_name) and jump_transforms[leg_name]['rot']:
                tuck_rot = Quaternion(Vector((1, 0, 0)), tuck * 0.8)
                b.rotation_quaternion = jump_transforms[leg_name]['rot'] @ tuck_rot
                b.keyframe_insert(data_path="rotation_quaternion", frame=f)
                
        for knee_name in ["LeftLeg", "RightLeg"]:
            b = arm.pose.bones.get(knee_name)
            if b and jump_transforms.get(knee_name) and jump_transforms[knee_name]['rot']:
                bend_rot = Quaternion(Vector((1, 0, 0)), tuck * 1.2)
                b.rotation_quaternion = jump_transforms[knee_name]['rot'] @ bend_rot
                b.keyframe_insert(data_path="rotation_quaternion", frame=f)
                
        for arm_name in ["LeftArm", "RightArm"]:
            b = arm.pose.bones.get(arm_name)
            if b and jump_transforms.get(arm_name) and jump_transforms[arm_name]['rot']:
                arm_rot = Quaternion(Vector((0, 1, 0)), tuck * 0.5)
                b.rotation_quaternion = jump_transforms[arm_name]['rot'] @ arm_rot
                b.keyframe_insert(data_path="rotation_quaternion", frame=f)
                
        for sp_name in ["Spine", "Spine01"]:
            b = arm.pose.bones.get(sp_name)
            if b and jump_transforms.get(sp_name) and jump_transforms[sp_name]['rot']:
                curl_rot = Quaternion(Vector((1, 0, 0)), tuck * 0.25)
                b.rotation_quaternion = jump_transforms[sp_name]['rot'] @ curl_rot
                b.keyframe_insert(data_path="rotation_quaternion", frame=f)
                
    bpy.ops.object.mode_set(mode='OBJECT')
    ensure_nla_track(arm, "Double_Jump", double_jump_act)
    ensure_nla_track(arm, "Double_Jumping", double_jump_act)
    print(f"  [OK] Double_Jump: created ({double_jump_act.frame_range[0]}..{double_jump_act.frame_range[1]})")

    # ─────────────────────────────────────────────────────────────────────────
    # 7. Export back to original GLB
    # ─────────────────────────────────────────────────────────────────────────
    bpy.ops.export_scene.gltf(
        filepath=path,
        export_format='GLB',
        export_animations=True,
        export_all_influences=True,
        export_skins=True
    )
    print(f"Successfully exported {m} to {path}")

print("\nAll 6 heroes successfully updated with all basic movements!")
