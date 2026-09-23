"""Build the six delivery-guide operatives from the preserved Meshy sources.

Run: blender --background --python tools/assets/build_operatives.py -- --root .
Requires Blender 5.2 (bpy, mathutils and bundled numpy). No source mesh is replaced
with proxy geometry. Original clips are retargeted in world deformation space;
missing clips are newly authored and identified as procedural in the manifest.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import shutil
import sys

import bpy
import numpy as np
from mathutils import Matrix, Quaternion, Vector

CLIPS = ['Idle', 'Running', 'Walking', 'Jumping', 'Death', 'Hit', 'Thrust_Slash', 'Karate_Strike', 'Attack']
ROSTER = {
    'cyber_shogun': dict(idle='cyber_shogun_source.glb', family='cyber_shogun_rigged', height=1.82, color=(0.70, 0.93, 1.0), skill='Thrust_Slash', skill_target='Thrust_Slash'),
    'solaria': dict(idle='Meshy_AI_Solaris_Heroine_Rig_biped_Animation_Idle_02_withSkin.glb', family='solaris_heroine_rig', height=1.76, color=(1.0, 0.035, 0.38)),
    'oceanus': dict(idle='Meshy_AI_Polynesian_Superheroi_biped_Animation_Idle_02_withSkin.glb', family='polynesian_superheroi', height=1.90, color=(0.01, 0.68, 1.0), skill='mage_soell_cast_4', skill_target='Attack'),
    'surya_prime': dict(idle='Meshy_AI_Solar_Guardian_Rig_biped_Animation_Idle_02_withSkin.glb', family='solar_guardian_rigged', height=1.88, color=(1.0, 0.53, 0.015), skill='Charged_Ground_Slam', skill_target='Attack'),
    'mirage_blade': dict(idle='Meshy_AI_Arab_Superhero_Rig_biped_Animation_Idle_02_withSkin.glb', family='arab_superhero_rig', height=1.80, color=(1.0, 0.87, 0.02), skill='Double_Blade_Spin', skill_target='Attack'),
    'apex_titan': dict(idle='Meshy_AI_Cyber_Superhero_Rig_biped_Animation_Idle_02_withSkin.glb', family='cyber_superhero_rig', height=2.16, color=(1.0, 0.16, 0.015), skill='Heavy_Hammer_Swing', skill_target='Attack'),
}


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def import_rig(path):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(path))
    objects = [o for o in bpy.data.objects if o not in before]
    arm = next(o for o in objects if o.type == 'ARMATURE')
    meshes = [o for o in objects if o.type == 'MESH' and any(m.type == 'ARMATURE' and m.object == arm for m in o.modifiers)]
    assert len(meshes) > 0, f'No skinned mesh: {path}'
    return arm, meshes, objects


def activate_action(arm, name):
    arm.animation_data_create()
    action = bpy.data.actions.new(name)
    arm.animation_data.action = action
    for track in list(arm.animation_data.nla_tracks):
        arm.animation_data.nla_tracks.remove(track)
    return action


def key_basis(arm, name, matrix, frame):
    bone = arm.pose.bones[name]
    loc, rot, scale = matrix.decompose()
    if bone.rotation_quaternion.dot(rot) < 0:
        rot.negate()
    bone.rotation_mode = 'QUATERNION'
    bone.location, bone.rotation_quaternion, bone.scale = loc, rot, scale
    bone.keyframe_insert('location', frame=frame, group=name)
    bone.keyframe_insert('rotation_quaternion', frame=frame, group=name)
    bone.keyframe_insert('scale', frame=frame, group=name)


def canonical_basis_from_world(arm, world_poses):
    arm_inv = arm.matrix_world.inverted()
    desired = {name: arm_inv @ mat for name, mat in world_poses.items()}
    result = {}
    for bone in arm.data.bones:
        if bone.parent:
            result[bone.name] = bone.matrix_local.inverted() @ bone.parent.matrix_local @ desired[bone.parent.name].inverted() @ desired[bone.name]
        else:
            result[bone.name] = bone.matrix_local.inverted() @ desired[bone.name]
    return result


def capture_current(arm):
    bpy.context.view_layer.update()
    return {b.name: arm.matrix_world @ b.matrix for b in arm.pose.bones}


def retarget_clip(target, source_path, clip_name):
    """Copy the source's world-space deformation to the target's own bind rig.

    Idle and motion GLBs share names but not bind transforms/proportions. Matching
    channel names alone is insufficient. This method preserves each original
    model's bind and skin weights while baking one consistent target skeleton.
    """
    source, _, imported = import_rig(source_path)
    source_action = source.animation_data.action
    start, end = source_action.frame_range
    frames = list(range(int(math.ceil(start)), int(math.floor(end)) + 1))
    if len(frames) < 2:
        frames = [start, end]
    source_rest = {b.name: source.matrix_world @ b.matrix_local for b in source.data.bones}
    target_rest = {b.name: target.matrix_world @ b.matrix_local for b in target.data.bones}
    assert set(source_rest) == set(target_rest), 'Source joint names do not match canonical rig'
    samples = []
    for frame in frames:
        bpy.context.scene.frame_set(int(frame), subframe=float(frame) % 1)
        poses = capture_current(source)
        world = {name: poses[name] @ source_rest[name].inverted() @ target_rest[name] for name in target_rest}
        samples.append(canonical_basis_from_world(target, world))
    action = activate_action(target, clip_name)
    for index, basis in enumerate(samples):
        for name, matrix in basis.items():
            key_basis(target, name, matrix, index + 1)
    action['provenance'] = 'Existing Meshy animation, bind-aware retarget to canonical operative rig'
    action['source_file'] = source_path.name
    for obj in imported:
        bpy.data.objects.remove(obj, do_unlink=True)
    return action, dict(type='source_retargeted', source=source_path.name, sha256=sha(source_path), source_clip=source_action.name, duration_seconds=(len(samples) - 1) / 30)


def pulse(t, a, b):
    if t <= a or t >= b:
        return 0.0
    return math.sin(math.pi * (t - a) / (b - a))


def smooth(t):
    return t * t * (3 - 2 * t)


def procedural_clip(arm, baseline, clip, operative):
    durations = dict(Jumping=0.9, Hit=0.55, Death=1.35, Thrust_Slash=0.8, Karate_Strike=0.9, Attack=1.25)
    duration = durations[clip]
    action = activate_action(arm, clip)
    base_bases = canonical_basis_from_world(arm, baseline)
    base_components = {n: m.decompose() for n, m in base_bases.items()}
    frames = int(duration * 30) + 1
    for frame in range(frames):
        t = frame / (frames - 1)
        rotations = {}
        hip_offset = Vector((0, 0, 0))
        def rotate(name, axis, degrees):
            if name in baseline:
                rotations.setdefault(name, []).append((Vector(axis), math.radians(degrees)))
        if clip == 'Jumping':
            crouch = pulse(t, 0, .32)
            tuck = pulse(t, .20, .85)
            hip_offset.z = -.12 * crouch
            rotate('LeftUpLeg', (1,0,0), -30*crouch-38*tuck)
            rotate('RightUpLeg', (1,0,0), -30*crouch-38*tuck)
            rotate('LeftLeg', (1,0,0), 54*crouch+65*tuck)
            rotate('RightLeg', (1,0,0), 54*crouch+65*tuck)
            rotate('Spine', (1,0,0), 10*crouch)
            rotate('LeftArm', (1,0,0), -45*tuck)
            rotate('RightArm', (1,0,0), -45*tuck)
        elif clip == 'Hit':
            hit = pulse(t, 0, .65)
            rotate('Spine02', (1,0,0), -18*hit)
            rotate('Head', (1,0,0), -12*hit)
            rotate('LeftArm', (0,1,0), -15*hit)
            rotate('RightArm', (0,1,0), 15*hit)
            hip_offset.y = .07*hit
        elif clip == 'Death':
            fall = smooth(min(1.0, t/.82))
            root_q = Quaternion((1,0,0), math.radians(-87)*fall)
            new_hip = root_q @ baseline['Hips'].translation
            new_hip.z += .14*fall
            hip_offset = new_hip - baseline['Hips'].translation
            rotate('Hips', (1,0,0), -87*fall)
            rotate('Head', (1,0,0), 16*fall)
            rotate('LeftArm', (0,1,0), -25*fall)
            rotate('RightArm', (0,1,0), 32*fall)
            rotate('LeftLeg', (1,0,0), 20*fall)
        elif clip == 'Thrust_Slash':
            wind = pulse(t, 0, .4)
            strike = pulse(t, .27, .82)
            rotate('Spine', (0,0,1), 20*wind-35*strike)
            rotate('RightArm', (1,0,0), 40*wind-85*strike)
            rotate('RightForeArm', (1,0,0), -35*wind+45*strike)
            rotate('LeftArm', (1,0,0), -22*strike)
        elif clip == 'Karate_Strike':
            wind = pulse(t, 0, .35)
            strike = pulse(t, .24, .83)
            rotate('Spine', (0,0,1), -18*wind+30*strike)
            rotate('LeftArm', (1,0,0), 35*wind-95*strike)
            rotate('LeftForeArm', (1,0,0), -40*wind+50*strike)
            rotate('RightUpLeg', (1,0,0), -52*strike)
            rotate('RightLeg', (1,0,0), 65*strike)
        else:
            cast = pulse(t, .05, .94)
            rotate('LeftArm', (0,1,0), -58*cast)
            rotate('RightArm', (0,1,0), 58*cast)
            rotate('LeftForeArm', (1,0,0), -22*cast)
            rotate('RightForeArm', (1,0,0), -22*cast)
            rotate('Spine', (1,0,0), -12*cast)
            rotate('Head', (1,0,0), -10*cast)
        for name, (base_loc, base_rot, base_scale) in base_components.items():
            rot = base_rot.copy()
            pose_q = baseline[name].to_quaternion()
            for axis, angle in rotations.get(name, []):
                local_axis = pose_q.inverted() @ axis
                rot = rot @ Quaternion(local_axis, angle)
            loc = base_loc.copy()
            if name == 'Hips':
                rest_world = arm.matrix_world @ arm.data.bones[name].matrix_local
                loc += rest_world.inverted().to_3x3() @ hip_offset
            key_basis(arm, name, Matrix.LocRotScale(loc, rot, base_scale), frame + 1)
    action['provenance'] = 'Procedurally authored joint motion; not a supplied Meshy clip'
    return action, dict(type='procedurally_authored', duration_seconds=duration, notes='Joint-keyframed motion built for this delivery; Jumping is an in-place pose for physics-driven airtime. Artist polish recommended for final combat timing.')


def isolate_emission(meshes, operative, color, scratch):
    maps = []
    for mesh in meshes:
        for material in mesh.data.materials:
            if not material or not material.use_nodes:
                continue
            material.name = f'AXIOM_{operative}_Armor_Emissive'
            nodes = material.node_tree.nodes
            principled = next(n for n in nodes if n.type == 'BSDF_PRINCIPLED')
            base_link = next(iter(principled.inputs['Base Color'].links), None)
            source = base_link.from_node.image if base_link and hasattr(base_link.from_node, 'image') else None
            if not source:
                continue
            width, height = source.size
            pixels = np.empty(width * height * 4, dtype=np.float32)
            source.pixels.foreach_get(pixels)
            rgba = pixels.reshape(height, width, 4)
            step = max(1, math.ceil(max(width, height) / 1024))
            rgb = rgba[::step, ::step, :3]
            peak = rgb.max(axis=2)
            chroma = peak - rgb.min(axis=2)
            mask = np.clip((peak - .22) / .65, 0, 1) ** 1.6 * np.clip((chroma - .12) / .42, 0, 1)
            output = np.ones((*mask.shape, 4), dtype=np.float32)
            output[:,:,:3] = mask[:,:,None] * np.array(color, dtype=np.float32)
            image = bpy.data.images.new(f'{operative}_EmissionMap', width=mask.shape[1], height=mask.shape[0], alpha=True)
            image.pixels.foreach_set(output.reshape(-1))
            image.filepath_raw = str(scratch / f'{operative}_emission.png')
            image.file_format = 'PNG'
            image.save()
            image.pack()
            texture = nodes.new('ShaderNodeTexImage')
            texture.name = f'{operative}_EmissionMap'
            texture.image = image
            for link in list(principled.inputs['Emission Color'].links):
                material.node_tree.links.remove(link)
            material.node_tree.links.new(texture.outputs['Color'], principled.inputs['Emission Color'])
            principled.inputs['Emission Strength'].default_value = 1.8
            principled.inputs['Metallic'].default_value = .65 if operative in ('surya_prime', 'apex_titan') else .42
            principled.inputs['Roughness'].default_value = .40 if operative != 'mirage_blade' else .26
            material['pulse_control'] = 'Emission Strength (separate embedded emissive texture)'
            maps.append(dict(material=material.name, image=image.name, embedded=True, source='chromatic-accent mask extracted from original diffuse texture', accent_color=list(color)))
    return maps


def mesh_bounds(meshes):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    pts = []
    for obj in meshes:
        evaluated = obj.evaluated_get(depsgraph)
        pts.extend(evaluated.matrix_world @ Vector(c) for c in evaluated.bound_box)
    return Vector(tuple(min(p[k] for p in pts) for k in range(3))), Vector(tuple(max(p[k] for p in pts) for k in range(3)))


def make_actor(root, operative, info):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.render.fps = 30
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1.0
    hq = root / 'assets/models/hq'
    scratch = root / '.validation/roster'
    scratch.mkdir(parents=True, exist_ok=True)
    idle_path = root / 'assets/models/sources' / info['idle'] if operative == 'cyber_shogun' else hq / info['idle']
    arm, meshes, imported = import_rig(idle_path)
    original = arm.animation_data.action
    original.name = 'Idle'
    original_start, original_end = original.frame_range
    bpy.context.scene.frame_set(math.ceil(original_start))
    baseline = capture_current(arm)
    # Avoid glTF importer's debug custom-shape geometry appearing in exports.
    for obj in imported:
        if obj != arm and obj not in meshes:
            bpy.data.objects.remove(obj, do_unlink=True)
    actions = {'Idle': original}
    provenance = {'Idle': dict(type='source_original', source=str(idle_path.relative_to(root)).replace('\\','/'), sha256=sha(idle_path), source_clip='Armature|Idle_02|baselayer', duration_seconds=(original_end-original_start)/30)}
    for clip in ('Running', 'Walking'):
        source = next(p for p in hq.glob('*.glb') if info['family'].lower() in p.name.lower() and f'_Animation_{clip}_'.lower() in p.name.lower())
        actions[clip], provenance[clip] = retarget_clip(arm, source, clip)
    if info.get('skill'):
        source = next(p for p in hq.glob('*.glb') if info['family'].lower() in p.name.lower() and f'_Animation_{info["skill"]}_'.lower() in p.name.lower())
        target = info['skill_target']
        actions[target], provenance[target] = retarget_clip(arm, source, target)
    for clip in CLIPS:
        if clip not in actions:
            actions[clip], provenance[clip] = procedural_clip(arm, baseline, clip, operative)
    arm.animation_data.action = original
    arm.animation_data.action_slot = next(iter(original.slots))
    bpy.context.scene.frame_set(math.ceil(original_start))
    bpy.context.view_layer.update()
    lower, upper = mesh_bounds(meshes)
    scale = info['height'] / (upper.z - lower.z)
    container = bpy.data.objects.new(operative, None)
    bpy.context.collection.objects.link(container)
    arm.parent = container
    container.scale = (scale, scale, scale)
    container.location = (-.5*(lower.x+upper.x)*scale, -.5*(lower.y+upper.y)*scale, -lower.z*scale)
    container['units'] = 'meters'
    container['origin'] = 'feet'
    container['rig'] = 'AXIOM_Humanoid24_v1'
    container['authored_clips'] = ','.join(n for n in CLIPS if provenance[n]['type'] == 'procedurally_authored')
    emission = isolate_emission(meshes, operative, info['color'], scratch)
    # One NLA track per canonical clip makes exported names deterministic.
    arm.animation_data.action = None
    for track in list(arm.animation_data.nla_tracks):
        arm.animation_data.nla_tracks.remove(track)
    for clip in CLIPS:
        action = actions[clip]
        track = arm.animation_data.nla_tracks.new()
        track.name = clip
        track.strips.new(clip, 1, action)
        track.mute = True
    bpy.ops.object.select_all(action='DESELECT')
    for obj in [container, arm] + meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = arm
    output = hq / f'{operative}.glb'
    bpy.ops.export_scene.gltf(filepath=str(output), export_format='GLB', use_selection=True, export_animations=True, export_animation_mode='NLA_TRACKS', export_nla_strips_merged_animation_name='Animation', export_force_sampling=True, export_frame_range=False, export_skins=True, export_def_bones=False, export_extras=True, export_yup=True, export_materials='EXPORT', export_image_format='AUTO', export_cameras=False, export_lights=False)
    result = dict(file=str(output.relative_to(root)).replace('\\','/'), sha256=sha(output), source_mesh=str(idle_path.relative_to(root)).replace('\\','/'), source_sha256=sha(idle_path), height_m=info['height'], origin='feet centered at standing Idle first frame', rig='AXIOM_Humanoid24_v1', joint_names=[b.name for b in arm.data.bones], animation_provenance=provenance, emissive_maps=emission, normalization_scale=scale, source_idle_bounds=[list(lower),list(upper)], notes=['Original Meshy surface geometry, UVs and base-color texture retained.', 'Same 24 named joints/hierarchy across roster; individual bone proportions retained. Use per-character retargeted clips for blending.', 'Idle/Running/Walking and recorded skill clips are source motions; missing clips are newly procedural authored motion.'])
    print('OPERATIVE_DONE',operative,output.stat().st_size,flush=True)
    return result


def main():
    args = sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', default='.')
    parser.add_argument('--only', choices=list(ROSTER))
    opts = parser.parse_args(args)
    root = Path(opts.root).resolve()
    hq = root / 'assets/models/hq'
    archive = root / 'assets/models/sources/cyber_shogun_source.glb'
    backup = root / '.validation/roster/cyber_shogun_original.glb'
    if not archive.exists():
        archive.parent.mkdir(parents=True, exist_ok=True)
        backup.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(hq/'cyber_shogun.glb', archive)
        shutil.copy2(archive, backup)
        assert sha(archive) == sha(backup) == sha(hq/'cyber_shogun.glb')
    manifest_path = hq / 'operative_manifest.json'
    manifest = json.loads(manifest_path.read_text()) if manifest_path.exists() else dict(schema_version=1, target_engine='Godot 4.7.2', generator='tools/assets/build_operatives.py', source_archive=dict(file=str(archive.relative_to(root)).replace('\\','/'), sha256=sha(archive)), operatives={})
    for operative in ([opts.only] if opts.only else ROSTER):
        manifest['operatives'][operative] = make_actor(root, operative, ROSTER[operative])
        manifest_path.write_text(json.dumps(manifest, indent=2)+'\n')
    print('OPERATIVE_MANIFEST', manifest_path, flush=True)


if __name__ == '__main__':
    main()
