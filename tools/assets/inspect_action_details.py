import bpy
import os

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
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=path)
    
    print(f"\n==================== {m} ====================")
    unique_action_names = sorted(list(set(a.name for a in bpy.data.actions)))
    for name in unique_action_names:
        act = bpy.data.actions.get(name)
        if not act: continue
        num_fcurves = len(act.fcurves) if hasattr(act, 'fcurves') else 0
        frame_range = act.frame_range if hasattr(act, 'frame_range') else (0, 0)
        print(f"  Action: '{act.name}' | curves: {num_fcurves} | frames: {frame_range[0]}..{frame_range[1]}")
