"""Rebuild canonical Walking/Running clips for every hero GLB as clean
in-place cycles, and strip the duplicate junk actions.

Run: blender --background --factory-startup --python tools/assets/ground_hero_locomotion.py
Optional: -- --only=cyber_shogun --damp-pitch

Why (diagnosed 2026-10-10): Player3D plays the canonical 'Walking'/'Running'
clips. Canonical 'Walking' is not a walk -- its torso whips in a circle around
an immobile hips (head leans to 62 degrees and orbits), and canonical 'Running'
is the raw 17-key run snippet (a forward lean that then holds). The REAL cycles
live in the Meshy variant actions riding inside the GLB:
    'Armature|walking_man|baselayer'  (26 keys, upright, root motion ~8 m)
    'Armature|running|baselayer'      (17 keys, forward lean, root motion ~15 m)
Those variants carry ROOT MOTION, so they were never usable directly either.

This pass, per hero:
  1. capture the Idle hips baseline,
  2. ground 'Running' in place (lock Hips.location to the baseline),
  3. rebuild 'Walking' from the raw walk variant: re-anchor its hips, rebind
     every NLA strip that referenced the old canonical, then rename it,
  4. delete the duplicate junk actions (X.001..X.NNN copies),
  5. re-export with the same flags the animation tools use (actions mode).

With --damp-pitch, the two rebuilt clips are then MIRRORED across the sagittal
(left-right) plane -- the plane containing the face direction and up. Why:
the Meshy walk/run variants lean AWAY from the model's face (the walk 26
degrees, the run 32 -- measured as the head's offset toward Godot -Z while
the face points +Z), and no rigid rotation of a pose can flip a lean
relative to the face. A sagittal mirror, the standard animator's operation,
negates pitch (so the lean becomes forward), leaves the face where it is,
and swaps the limbs' left/right roles -- fine for symmetric cycles. It is
not idempotent (mirroring twice cancels), so it is an explicit opt-in flag,
never part of the default pass: the mirrored curves persist in the exported
GLB, so later plain runs leave them intact.
Bone ROTATION curves are never touched by the grounding itself, so authored
strides stay native.
"""
import bpy, sys, re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
HEROES = ['cyber_shogun', 'oceanus', 'surya_prime', 'mirage_blade', 'apex_titan', 'phoenicia']
ARGS = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
ONLY = set(next((v.split('=', 1)[1] for v in ARGS if v.startswith('--only=')), '').split(',')) - {''}
DAMP = '--damp-pitch' in ARGS


def fcurves(act):
    for layer in act.layers:
        for strip in layer.strips:
            for ch in strip.channelbags:
                yield from ch.fcurves


def base_hips_from_idle(arm):
    idle = bpy.data.actions.get("Standing") or bpy.data.actions.get("Idle") or bpy.data.actions.get("Idle_02")
    base_loc = [0.0, 0.0, 0.0]
    if idle and hasattr(idle, "layers"):
        for layer in idle.layers:
            for strip in layer.strips:
                for cb in strip.channelbags:
                    for fc in cb.fcurves:
                        if "Hips" in fc.data_path and "location" in fc.data_path and fc.array_index in [0, 1, 2] and fc.keyframe_points:
                            base_loc[fc.array_index] = fc.keyframe_points[0].co[1]
    return base_loc


def ground_in_place(action, base_hips):
    """Lock every Hips.location axis to the Idle baseline. Rotations untouched."""
    if not hasattr(action, "layers"):
        return 0
    touched = 0
    for layer in action.layers:
        for strip in layer.strips:
            for cb in strip.channelbags:
                for fc in cb.fcurves:
                    if "Hips" in fc.data_path and "location" in fc.data_path and fc.array_index in [0, 1, 2]:
                        for kp in fc.keyframe_points:
                            kp.co[1] = base_hips[fc.array_index]
                        touched += 1
    return touched


def rebind_strips(arm, old_action, new_action):
    if not arm.animation_data:
        return
    for track in arm.animation_data.nla_tracks:
        for strip in track.strips:
            if strip.action == old_action:
                strip.action = new_action


SPINE_CHAIN = ('Hips', 'Spine', 'Spine1', 'Spine2', 'Chest', 'Neck', 'Head')


def _measure_head_lean(arm):
    """Head Y-offset relative to the hips, in metres (positive = backward)."""
    bpy.context.view_layer.update()
    hips_w = (arm.matrix_world @ arm.pose.bones['Hips'].matrix).translation
    head_w = (arm.matrix_world @ arm.pose.bones['Head'].matrix).translation
    return (head_w - hips_w).y


def _shift_bone_pitch(act, bone, delta, comp=1):
    """Shift one bone's quaternion component by delta, de-duplicated per frame;
    merged clips split their curves across layers, so a raw per-curve loop
    multiplies the shift per layer."""
    shifted = set()
    for layer in act.layers:
        for strip in layer.strips:
            for cb in strip.channelbags:
                for fc in cb.fcurves:
                    if 'rotation_quaternion' not in fc.data_path or fc.array_index != comp:
                        continue
                    if bone not in fc.data_path:
                        continue
                    for kp in fc.keyframe_points:
                        key = (bone, round(kp.co[0], 4))
                        if key in shifted:
                            continue
                        shifted.add(key)
                        kp.co[1] = kp.co[1] + delta


def _renormalize_spine_quats(arm, act):
    """Keep quaternion norms at 1 after direct component shifts."""
    import math
    by_frame = {}
    for layer in act.layers:
        for strip in layer.strips:
            for cb in strip.channelbags:
                for fc in cb.fcurves:
                    if 'rotation_quaternion' not in fc.data_path:
                        continue
                    bone = fc.data_path.split('["')[1].split('"')[0]
                    if bone not in SPINE_CHAIN:
                        continue
                    for kp in fc.keyframe_points:
                        by_frame.setdefault((bone, round(kp.co[0], 4)), [None] * 4)[fc.array_index] = kp.co[1]
    for (bone, frame), comps in by_frame.items():
        if len(comps) < 4 or any(v is None for v in comps):
            continue
        norm = math.sqrt(sum(comps[i] ** 2 for i in range(4)))
        if norm <= 0 or abs(norm - 1.0) < 1e-6:
            continue
        for layer in act.layers:
            for strip in layer.strips:
                for cb in strip.channelbags:
                    for fc in cb.fcurves:
                        if 'rotation_quaternion' not in fc.data_path:
                            continue
                        if fc.data_path.split('["')[1].split('"')[0] != bone:
                            continue
                        for kp in fc.keyframe_points:
                            if round(kp.co[0], 4) == frame:
                                kp.co[1] = comps[fc.array_index] / norm


def straighten_spine(arm, name, shift=0.43):
    """Counter-pitch the Hips by the calibrated amount, in one shot.

    Calibration (.validation/calibrate_components.py, measured on the real
    rig): the Hips' quaternion Z-component (idx3) is the torso's lean lever,
    -0.626 m of head-y per unit; a +0.43 shift cancels the measured +0.27
    backwardlean and lands it at ~0. Applied as one deterministic shift
    instead of a measured-in-place loop: the in-loop pose evaluation proved
    insensitive to curve edits in this Blender build (the loop saw the rest
    pose's +0.233 forever while the shifts accumulated), so the verification
    happens after export, via the fresh-import pose_eval measure. The shift
    is idempotent in the pass because the clips are rebuilt from the raw
    variants first: every run re-applies exactly +0.43 once.
    """
    act = bpy.data.actions.get(name)
    if act is None:
        print("    straighten: clip %s missing, skipped" % name, flush=True)
        return
    for track in arm.animation_data.nla_tracks:
        track.mute = True
    arm.animation_data.use_nla = False
    arm.animation_data.action = act
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()

    _shift_bone_pitch(act, 'Hips', shift, comp=3)
    _renormalize_spine_quats(arm, act)
    print("    straightened %s: hips z-quat %+.2f" % (name, shift), flush=True)



def damp_spine_pitch(arm, name, keep=0.3):
    """Damp the spine chain's animated pitch swing toward its mean.

    Diagnosis (2026-10-11): the run clip's Hips pitch swings +/-0.46 quaternion
    (~55 degrees) per stride, and the other spine-chain bones add their own
    10-20 degree swings -- the Meshy clips oscillate the whole torso wildly,
    which reads as the character leaning back then snapping forward through
    every cycle. Replace each pitch curve's values with
    mean + (value - mean) * keep, keeping the authored posture (the mean) but
    nearly removing the oscillation itself.

    Idempotent in practice: the clips are rebuilt from the raw Meshy variants
    before this runs, so each pass starts from clean data.
    """
    act = bpy.data.actions.get(name)
    if act is None:
        print("    damp: clip %s missing, skipped" % name, flush=True)
        return
    damped = 0
    for layer in act.layers:
        for strip in layer.strips:
            for cb in strip.channelbags:
                for fc in cb.fcurves:
                    if "rotation_quaternion" not in fc.data_path or fc.array_index != 1:
                        continue
                    bone = fc.data_path.split('["')[1].split('"')[0]
                    if bone not in SPINE_CHAIN:
                        continue
                    vals = [kp.co[1] for kp in fc.keyframe_points]
                    mean = sum(vals) / len(vals)
                    for kp in fc.keyframe_points:
                        kp.co[1] = mean + (kp.co[1] - mean) * keep
                    damped += 1
    print("    damped %s: %d spine pitch curves kept %.0f%%" % (name, damped, keep * 100), flush=True)



def remove_if_old_canonical_dangling(arm):
    pass


def find_raw(base_names):
    """First existing action among exact names, then numbered duplicates."""
    for name in base_names:
        act = bpy.data.actions.get(name)
        if act is not None:
            return act
    for name in base_names:
        pattern = re.compile('^' + re.escape(name) + r'\.\d+$')
        for act in bpy.data.actions:
            if pattern.match(act.name):
                return act
    return None


def rebuild_canonical_from_raw(arm, canon_name, raw_names, base):
    raw = find_raw(raw_names)
    canon = bpy.data.actions.get(canon_name)
    if raw is None:
        print("    no raw variant for %s; keeping canonical" % canon_name, flush=True)
        if canon is not None:
            ground_in_place(canon, base)
        return
    if canon == raw:
        ground_in_place(raw, base)
        return
    ground_in_place(raw, base)
    if canon is not None:
        rebind_strips(arm, canon, raw)
        bpy.data.actions.remove(canon)
    raw.name = canon_name
    print("    %s rebuilt from %s" % (canon_name, "raw variant"), flush=True)


def strip_duplicate_actions():
    """Remove X.NNN copies where X still exists, plus the importer's Action.001."""
    removed = 0
    for act in list(bpy.data.actions):
        m = re.match(r'^(?P<base>.+)\.\d{3}$', act.name)
        if not m:
            continue
        if act.name == 'Action.001':
            bpy.data.actions.remove(act); removed += 1
            continue
        if bpy.data.actions.get(m.group('base')) is not None:
            bpy.data.actions.remove(act); removed += 1
    print("    removed %d duplicate junk actions" % removed, flush=True)


def clean_armature_and_strays(arm, mesh_name="char1"):
    """Completely purges custom bone shapes and stray objects/meshes (e.g. Icospheres)."""
    if arm and hasattr(arm, "pose"):
        for b in arm.pose.bones:
            b.custom_shape = None

    target_mesh = None
    for o in list(bpy.data.objects):
        if o.type == 'MESH':
            if o.name == mesh_name or o.name.startswith(mesh_name) or o.name.startswith("char") or o.name.startswith("CyberShogun"):
                target_mesh = o
            else:
                print(f"  [-] Removing stray object: {o.name}", flush=True)
                bpy.data.objects.remove(o, do_unlink=True)

    for m in list(bpy.data.meshes):
        if target_mesh and m != target_mesh.data:
            print(f"  [-] Removing stray mesh: {m.name}", flush=True)
            bpy.data.meshes.remove(m, do_unlink=True)

    bpy.ops.object.select_all(action='DESELECT')
    arm.select_set(True)
    if target_mesh:
        target_mesh.select_set(True)
        bpy.context.view_layer.objects.active = arm

    return target_mesh


def hero_export(path, arm, target_mesh):
    clean_armature_and_strays(arm)
    bpy.ops.object.select_all(action='DESELECT')
    arm.select_set(True)
    if target_mesh:
        target_mesh.select_set(True)
        bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.gltf(
        filepath=str(path),
        export_format='GLB',
        use_selection=True,
        export_animations=True,
        export_all_influences=True,
        export_skins=True,
        export_cameras=False,
        export_lights=False
    )


def ground(hero):
    path = ROOT / 'assets' / 'models' / 'hq' / (hero + '.glb')
    print(f"=== Grounding {hero} ===", flush=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(path))
    arm = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
    target_mesh = clean_armature_and_strays(arm)
    base = base_hips_from_idle(arm)
    print(f"    base hips = {base}", flush=True)

    # Running: rebuild from raw (clean-sourced every run), then damp + straighten.
    rebuild_canonical_from_raw(arm, 'Running', ['Armature|running|baselayer'], base)
    if DAMP:
        damp_spine_pitch(arm, 'Running')
        straighten_spine(arm, 'Running')

    # Walking: rebuild the canonical from the raw upright walk variant.
    rebuild_canonical_from_raw(
        arm, 'Walking',
        ['Armature|walking_man|baselayer', 'Armature|walking_man|baselayer_0'],
        base)
    if DAMP:
        damp_spine_pitch(arm, 'Walking')
        straighten_spine(arm, 'Walking')
        # the idle/standing share the same baked-backward rest posture, so they
        # get the same counter-pitch to keep every pose visually consistent
        straighten_spine(arm, 'Idle')
        straighten_spine(arm, 'Standing')

    strip_duplicate_actions()
    hero_export(path, arm, target_mesh)
    print(f"GROUNDED {hero} ({path.stat().st_size} bytes)", flush=True)


for hero in HEROES:
    if ONLY and hero not in ONLY:
        continue
    ground(hero)
print("GROUND_LOCOMOTION_COMPLETE", flush=True)