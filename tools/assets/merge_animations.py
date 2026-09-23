import bpy
import os
import math

# Mapping of operative to their asset paths and animation mappings
OPERATIVES = {
    "cyber_shogun": {
        "base": "assets/models/hq/cyber_shogun.glb",
        "anims": {
            "Meshy_AI_cyber_shogun_rigged_biped_Animation_Running_withSkin.glb": "Running",
            "Meshy_AI_cyber_shogun_rigged_biped_Animation_Walking_withSkin.glb": "Walking",
            "Meshy_AI_cyber_shogun_rigged_biped_Animation_Thrust_Slash_withSkin.glb": "Thrust_Slash",
        },
        "vibe": "disciplined"
    },
    "solaria": {
        "base": "assets/models/hq/solaria.glb",
        "anims": {
            "Meshy_AI_solaris_heroine_rig_biped_Animation_Running_withSkin.glb": "Running",
            "Meshy_AI_solaris_heroine_rig_biped_Animation_Walking_withSkin.glb": "Walking",
            "Meshy_AI_solaris_heroine_rig_biped_Animation_Jump_Rope_withSkin.glb": "Jumping",
        },
        "vibe": "radiant"
    },
    "oceanus": {
        "base": "assets/models/hq/oceanus.glb",
        "anims": {
            "Meshy_AI_polynesian_superheroi_biped_Animation_Running_withSkin.glb": "Running",
            "Meshy_AI_polynesian_superheroi_biped_Animation_Walking_withSkin.glb": "Walking",
            "Meshy_AI_polynesian_superheroi_biped_Animation_mage_soell_cast_4_withSkin.glb": "Attack",
        },
        "vibe": "fluid"
    },
    "surya_prime": {
        "base": "assets/models/hq/surya_prime.glb",
        "anims": {
            "Meshy_AI_solar_guardian_rigged_biped_Animation_Running_withSkin.glb": "Running",
            "Meshy_AI_solar_guardian_rigged_biped_Animation_Walking_withSkin.glb": "Walking",
            "Meshy_AI_solar_guardian_rigged_biped_Animation_Charged_Ground_Slam_withSkin.glb": "Attack",
        },
        "vibe": "grand"
    },
    "mirage_blade": {
        "base": "assets/models/hq/mirage_blade.glb",
        "anims": {
            "Meshy_AI_arab_superhero_rig_biped_Animation_Running_withSkin.glb": "Running",
            "Meshy_AI_arab_superhero_rig_biped_Animation_Walking_withSkin.glb": "Walking",
            "Meshy_AI_arab_superhero_rig_biped_Animation_Double_Blade_H_S_withSkin.glb": "Attack",
        },
        "vibe": "twitchy"
    },
    "apex_titan": {
        "base": "assets/models/hq/apex_titan.glb",
        "anims": {
            "Meshy_AI_cyber_superhero_rig_biped_Animation_Running_withSkin.glb": "Running",
            "Meshy_AI_cyber_superhero_rig_biped_Animation_Walking_withSkin.glb": "Walking",
            "Meshy_AI_cyber_superhero_rig_biped_Animation_Heavy_Hammer_Swing_withSkin.glb": "Attack",
        },
        "vibe": "heavy"
    }
}

REQUIRED_CLIPS = {'Idle', 'Running', 'Walking', 'Jumping', 'Death', 'Hit', 'Thrust_Slash', 'Karate_Strike', 'Attack'}

def find_bone(armature, keyword):
    for bone in armature.pose.bones:
        if keyword.lower() in bone.name.lower():
            return bone
    return None

def create_procedural_action(name, armature, vibe):
    action = bpy.data.actions.new(name=name)
    if not armature.animation_data:
        armature.animation_data_create()
    armature.animation_data.action = action

    spine = find_bone(armature, "spine")
    chest = find_bone(armature, "chest")
    root = find_bone(armature, "root") or (armature.pose.bones[0] if armature.pose.bones else None)
    arm = find_bone(armature, "arm")

    if name == "Idle":
        if spine:
            for i in range(0, 60, 10):
                spine.rotation_euler[2] = math.sin(i * 0.1) * 0.05
                spine.keyframe_insert(data_path="rotation_euler", frame=i)

    elif name == "Hit":
        if chest:
            chest.location[0] = 0
            chest.keyframe_insert(data_path="location", frame=0)
            chest.location[0] = -0.2 if vibe != "heavy" else -0.05
            chest.keyframe_insert(data_path="location", frame=2)
            chest.location[0] = 0
            chest.keyframe_insert(data_path="location", frame=10)

    elif name == "Death":
        if root:
            root.rotation_euler[0] = 0
            root.keyframe_insert(data_path="rotation_euler", frame=0)
            root.rotation_euler[0] = math.radians(90)
            root.keyframe_insert(data_path="rotation_euler", frame=30)

    elif name == "Jumping":
        if root:
            root.location[2] = 0
            root.keyframe_insert(data_path="location", frame=0)
            root.location[2] = 0.5
            root.keyframe_insert(data_path="location", frame=10)
            root.location[2] = 0
            root.keyframe_insert(data_path="location", frame=20)

    elif name in ["Attack", "Thrust_Slash", "Karate_Strike"]:
        if arm:
            arm.rotation_euler[0] = 0
            arm.keyframe_insert(data_path="rotation_euler", frame=0)
            arm.rotation_euler[0] = math.radians(-45)
            arm.keyframe_insert(data_path="rotation_euler", frame=5)
            arm.rotation_euler[0] = math.radians(-50)
            arm.keyframe_insert(data_path="rotation_euler", frame=7)
            arm.rotation_euler[0] = 0
            arm.keyframe_insert(data_path="rotation_euler", frame=15)

    return action

def merge_animations():
    root_dir = os.getcwd()

    for op_name, config in OPERATIVES.items():
        print(f"Processing {op_name}...")

        bpy.ops.wm.read_factory_settings(use_empty=True)

        base_path = os.path.join(root_dir, config["base"])
        if not os.path.exists(base_path):
            print(f"Base model not found: {base_path}")
            continue

        bpy.ops.import_scene.gltf(filepath=base_path)

        armature = None
        for obj in bpy.data.objects:
            if obj.type == 'ARMATURE':
                armature = obj
                break

        if not armature:
            print(f"No armature found in {base_path}")
            continue

        all_actions = []

        for anim_file, target_name in config["anims"].items():
            anim_path = os.path.join(root_dir, "assets/models/hq", anim_file)
            if not os.path.exists(anim_path):
                print(f"Animation file not found: {anim_path}")
                continue

            bpy.ops.import_scene.gltf(filepath=anim_path)

            action = None
            for act in bpy.data.actions:
                if anim_file in act.name or target_name in act.name:
                    action = act
                    break

            if not action:
                action = bpy.data.actions[-1]

            if action:
                action.name = target_name
                all_actions.append(action)
                print(f"Merged {anim_file} as {target_name}")

        for clip in REQUIRED_CLIPS:
            if not any(a.name == clip for a in all_actions):
                action = create_procedural_action(clip, armature, config["vibe"])
                action.name = clip
                all_actions.append(action)
                print(f"Procedurally authored {clip} for {op_name}")

        if not armature.animation_data:
            armature.animation_data_create()

        for action in all_actions:
            # Set action as active to help export
            armature.animation_data.action = action

            # Blender 4.0+ Actions need slots.
            if hasattr(action, 'slots'):
                if len(action.slots) == 0:
                    try:
                        action.slots.new('OBJECT', name="DefaultSlot")
                    except Exception as e:
                        print(f"Could not create slot for {action.name}: {e}")

            track = armature.animation_data.nla_tracks.new()
            track.name = action.name
            try:
                # Positional order: (name, start, action)
                track.strips.new(action.name, 0, action)
            except Exception as e:
                print(f"Failed to create NLA strip for {action.name}: {e}")

        export_path = os.path.join(root_dir, config["base"])
        bpy.ops.export_scene.gltf(filepath=export_path, export_format='GLB')
        print(f"Exported {op_name} to {export_path}")

if __name__ == "__main__":
    merge_animations()
