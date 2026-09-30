import bpy

path = r"D:\Projects\frog_jumper\assets\models\hq\cyber_shogun.glb"
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=path)

armature = None
for obj in bpy.data.objects:
    if obj.type == 'ARMATURE':
        armature = obj
        break

if armature:
    print(f"Armature: {armature.name}")
    print("Bones (" + str(len(armature.pose.bones)) + "):")
    for b in armature.pose.bones:
        print(f"  {b.name}")
