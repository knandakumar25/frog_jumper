import bpy
import os

ALLOWED_KEYWORDS = ['idle', 'walk', 'run', 'jump', 'standing']

def should_keep(name):
    name_lower = name.lower()
    return any(kw in name_lower for kw in ALLOWED_KEYWORDS)

MODELS = [
    "cyber_shogun.glb",
    "solaria.glb",
    "oceanus.glb",
    "surya_prime.glb",
    "mirage_blade.glb",
    "apex_titan.glb"
]

root_dir = r"D:\Projects\frog_jumper"

for m in MODELS:
    path = os.path.join(root_dir, "assets", "models", "hq", m)
    print(f"\n========================================")
    print(f"Processing {m}...")
    print(f"========================================")

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=path)

    # 1. Clean animation_data on objects (active action and NLA tracks)
    for obj in bpy.data.objects:
        if obj.animation_data:
            # If active action shouldn't be kept, clear it
            if obj.animation_data.action and not should_keep(obj.animation_data.action.name):
                print(f"Clearing active action {obj.animation_data.action.name} on {obj.name}")
                obj.animation_data.action = None

            # Clear non-allowed NLA tracks / strips
            tracks_to_remove = []
            for track in obj.animation_data.nla_tracks:
                strips_to_remove = []
                for strip in track.strips:
                    strip_action_name = strip.action.name if strip.action else ""
                    if not should_keep(strip.name) or (strip_action_name and not should_keep(strip_action_name)):
                        strips_to_remove.append(strip)

                for s in strips_to_remove:
                    track.strips.remove(s)

                if len(track.strips) == 0:
                    tracks_to_remove.append(track)

            for t in tracks_to_remove:
                obj.animation_data.nla_tracks.remove(t)

    # 2. Remove all non-allowed actions from bpy.data.actions
    removed_count = 0
    kept_actions = []
    for action in list(bpy.data.actions):
        if not should_keep(action.name):
            bpy.data.actions.remove(action)
            removed_count += 1
        else:
            kept_actions.append(action.name)

    print(f"Removed {removed_count} actions. Kept {len(kept_actions)} actions: {kept_actions}")

    # 3. Export back to the original GLB file
    bpy.ops.export_scene.gltf(
        filepath=path,
        export_format='GLB',
        export_animations=True,
        export_all_influences=True,
        export_skins=True
    )
    print(f"Successfully exported cleaned {m} to {path}")

print("\nAll 6 models successfully cleaned and exported!")
