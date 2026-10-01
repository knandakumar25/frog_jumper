import bpy
import os
import sys

def unify_model(input_glb, output_glb):
    print(f"\n==========================================")
    print(f"Processing: {os.path.basename(input_glb)}")
    print(f"==========================================")
    
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=input_glb)
    
    armatures = [ob for ob in bpy.data.objects if ob.type == 'ARMATURE']
    if not armatures:
        print("ERROR: No armatures found!")
        return False
        
    primary_armature = bpy.data.objects.get("Armature")
    if not primary_armature:
        primary_armature = armatures[0]
        
    print(f"Armatures found: {len(armatures)}. Selected primary: {primary_armature.name}")
    
    # Collect all actions
    actions = list(bpy.data.actions)
    print(f"Actions found: {len(actions)}")
    
    if not primary_armature.animation_data:
        primary_armature.animation_data_create()
        
    primary_armature.animation_data.use_nla = True
    nla_tracks = primary_armature.animation_data.nla_tracks
    
    # Clear existing NLA tracks on primary armature to avoid duplicate actions
    while nla_tracks:
        nla_tracks.remove(nla_tracks[0])
        
    for action in actions:
        track = nla_tracks.new()
        track.name = action.name
        start_frame = int(action.frame_range[0])
        strip = track.strips.new(action.name, start_frame, action)
        strip.action = action
        
    # Find primary mesh
    primary_mesh = None
    for child in primary_armature.children:
        if child.type == 'MESH':
            primary_mesh = child
            break
    if not primary_mesh:
        meshes = [ob for ob in bpy.data.objects if ob.type == 'MESH']
        if meshes:
            primary_mesh = meshes[0]
            
    print(f"Primary mesh: {primary_mesh.name if primary_mesh else 'None'}")
    
    # Delete all other objects
    to_delete = [ob for ob in bpy.data.objects if ob != primary_armature and ob != primary_mesh]
    print(f"Deleting {len(to_delete)} duplicate objects...")
    for ob in to_delete:
        bpy.data.objects.remove(ob, do_unlink=True)
        
    # Clean up orphan data
    for mesh in list(bpy.data.meshes):
        if primary_mesh and mesh != primary_mesh.data:
            bpy.data.meshes.remove(mesh)
    for arm in list(bpy.data.armatures):
        if primary_armature and arm != primary_armature.data:
            bpy.data.armatures.remove(arm)
            
    print(f"Exporting to {output_glb}...")
    bpy.ops.export_scene.gltf(
        filepath=output_glb,
        export_format='GLB',
        use_selection=False,
        export_animations=True,
        export_nla_strips=True
    )
    
    orig_size = os.path.getsize(input_glb) / (1024 * 1024)
    new_size = os.path.getsize(output_glb) / (1024 * 1024)
    reduction = ((orig_size - new_size) / orig_size) * 100
    print(f"Complete: {orig_size:.1f} MB -> {new_size:.1f} MB ({reduction:.1f}% reduction)")
    return True

models = [
    ("cyber_shogun.glb", "cyber_shogun_clean.glb"),
    ("solaria.glb", "solaria_clean.glb"),
    ("oceanus.glb", "oceanus_clean.glb"),
    ("surya_prime.glb", "surya_prime_clean.glb"),
    ("mirage_blade.glb", "mirage_blade_clean.glb"),
    ("apex_titan.glb", "apex_titan_clean.glb"),
]

hq_dir = os.path.abspath("assets/models/hq")
for in_name, out_name in models:
    in_path = os.path.join(hq_dir, in_name)
    out_path = os.path.join(hq_dir, out_name)
    if os.path.exists(in_path):
        unify_model(in_path, out_path)
