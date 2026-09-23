import bpy
import bmesh
import math
import os
import numpy as np

# ─── 0. Clear Existing Scene ──────────────────────────────────────────────────
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete()

# ─── 1. Load PBR Texture Maps ──────────────────────────────────────────────────
img_dir = os.path.abspath("assets/images")
albedo_path = os.path.join(img_dir, "axiom_force_logo_clean.png")
emission_path = os.path.join(img_dir, "axiom_force_emission.png")
metallic_path = os.path.join(img_dir, "axiom_force_metallic.png")
roughness_path = os.path.join(img_dir, "axiom_force_roughness.png")
normal_path = os.path.join(img_dir, "axiom_force_normal.png")

albedo_img = bpy.data.images.load(albedo_path)
emission_img = bpy.data.images.load(emission_path)
metallic_img = bpy.data.images.load(metallic_path)
roughness_img = bpy.data.images.load(roughness_path)
normal_img = bpy.data.images.load(normal_path)
normal_img.colorspace_settings.name = 'Non-Color'
metallic_img.colorspace_settings.name = 'Non-Color'
roughness_img.colorspace_settings.name = 'Non-Color'

w, h = albedo_img.size
pix = np.array(albedo_img.pixels[:], dtype=np.float32).reshape((h, w, 4))
alpha = pix[:, :, 3]

# ─── 2. Create PBR Materials ───────────────────────────────────────────────────
# Mat 0: Main Logo Front Face PBR Material
mat_logo = bpy.data.materials.new("Mat_LogoPBR")
mat_logo.use_nodes = True
nodes = mat_logo.node_tree.nodes
links = mat_logo.node_tree.links
nodes.clear()

node_output = nodes.new(type='ShaderNodeOutputMaterial')
node_bsdf = nodes.new(type='ShaderNodeBsdfPrincipled')

# Albedo + Alpha
tex_albedo = nodes.new(type='ShaderNodeTexImage')
tex_albedo.image = albedo_img
links.new(tex_albedo.outputs['Color'], node_bsdf.inputs['Base Color'])
links.new(tex_albedo.outputs['Alpha'], node_bsdf.inputs['Alpha'])

# Metallic
tex_metallic = nodes.new(type='ShaderNodeTexImage')
tex_metallic.image = metallic_img
links.new(tex_metallic.outputs['Color'], node_bsdf.inputs['Metallic'])

# Roughness
tex_roughness = nodes.new(type='ShaderNodeTexImage')
tex_roughness.image = roughness_img
links.new(tex_roughness.outputs['Color'], node_bsdf.inputs['Roughness'])

# Normal Map
tex_normal = nodes.new(type='ShaderNodeTexImage')
tex_normal.image = normal_img
node_norm_map = nodes.new(type='ShaderNodeNormalMap')
node_norm_map.inputs['Strength'].default_value = 1.6
links.new(tex_normal.outputs['Color'], node_norm_map.inputs['Color'])
links.new(node_norm_map.outputs['Normal'], node_bsdf.inputs['Normal'])

# Emission
tex_emission = nodes.new(type='ShaderNodeTexImage')
tex_emission.image = emission_img
links.new(tex_emission.outputs['Color'], node_bsdf.inputs['Emission Color'])
node_bsdf.inputs['Emission Strength'].default_value = 3.5

links.new(node_bsdf.outputs['BSDF'], node_output.inputs['Surface'])

# Mat 1: Dark Cyber Titanium Chassis Armor
mat_armor = bpy.data.materials.new("Mat_ChassisArmor")
mat_armor.use_nodes = True
bsdf_armor = mat_armor.node_tree.nodes.get("Principled BSDF")
if bsdf_armor:
    bsdf_armor.inputs['Base Color'].default_value = (0.03, 0.05, 0.08, 1.0)
    bsdf_armor.inputs['Metallic'].default_value = 0.95
    bsdf_armor.inputs['Roughness'].default_value = 0.28
    if 'Emission Color' in bsdf_armor.inputs:
        bsdf_armor.inputs['Emission Color'].default_value = (0.0, 0.04, 0.08, 1.0)
        bsdf_armor.inputs['Emission Strength'].default_value = 0.3

# Mat 2: Glowing Cyan Neon (for 3D conduit piping)
mat_cyan = bpy.data.materials.new("Mat_CyanNeon")
mat_cyan.use_nodes = True
bsdf_cyan = mat_cyan.node_tree.nodes.get("Principled BSDF")
if bsdf_cyan:
    bsdf_cyan.inputs['Base Color'].default_value = (0.0, 0.94, 1.0, 1.0)
    bsdf_cyan.inputs['Metallic'].default_value = 0.1
    bsdf_cyan.inputs['Roughness'].default_value = 0.1
    if 'Emission Color' in bsdf_cyan.inputs:
        bsdf_cyan.inputs['Emission Color'].default_value = (0.0, 0.94, 1.0, 1.0)
        bsdf_cyan.inputs['Emission Strength'].default_value = 4.8

# Mat 3: Glowing Magenta Neon (for sub-dock trim & power cells)
mat_magenta = bpy.data.materials.new("Mat_MagentaNeon")
mat_magenta.use_nodes = True
bsdf_magenta = mat_magenta.node_tree.nodes.get("Principled BSDF")
if bsdf_magenta:
    bsdf_magenta.inputs['Base Color'].default_value = (1.0, 0.0, 0.55, 1.0)
    bsdf_magenta.inputs['Metallic'].default_value = 0.1
    bsdf_magenta.inputs['Roughness'].default_value = 0.1
    if 'Emission Color' in bsdf_magenta.inputs:
        bsdf_magenta.inputs['Emission Color'].default_value = (1.0, 0.0, 0.55, 1.0)
        bsdf_magenta.inputs['Emission Strength'].default_value = 4.8

# ─── 3. Extract Silhouette Contour ─────────────────────────────────────────────
cx, cy = 553.0, 249.0
scale = 3.2 / 1050.0  # 3.2 world units wide

step = 16
pts_top = []
for x in range(28, 1078, step):
    idx = np.where(alpha[:, x] > 0.25)[0]
    if len(idx) > 0:
        pts_top.append((x, idx[-1]))

pts_bot = []
for x in range(1078, 27, -step):
    idx = np.where(alpha[:, min(x, 1077)] > 0.25)[0]
    if len(idx) > 0:
        pts_bot.append((min(x, 1077), idx[0]))

poly = pts_top + pts_bot

# ─── 4. Construct 3D Beveled Chassis Mesh ──────────────────────────────────────
curve_data = bpy.data.curves.new(name='ChassisCurve', type='CURVE')
curve_data.dimensions = '2D'
spline = curve_data.splines.new('POLY')
spline.points.add(len(poly) - 1)

for i, (px, py) in enumerate(poly):
    wx = (px - cx) * scale
    wy = (py - cy) * scale
    spline.points[i].co = (wx, wy, 0, 1)

spline.use_cyclic_u = True
curve_data.extrude = 0.045
curve_data.bevel_depth = 0.014
curve_data.bevel_resolution = 2
curve_data.fill_mode = 'BOTH'

chassis_obj = bpy.data.objects.new('MainTitleChassis', curve_data)
bpy.context.collection.objects.link(chassis_obj)

# Convert 2D curve to 3D Mesh
bpy.context.view_layer.objects.active = chassis_obj
chassis_obj.select_set(True)
bpy.ops.object.convert(target='MESH')

# Assign Materials to Chassis
chassis_mesh = chassis_obj.data
chassis_mesh.materials.append(mat_logo)   # Material Index 0
chassis_mesh.materials.append(mat_armor)  # Material Index 1

# Setup UV Layer & Assign Face Materials
if not chassis_mesh.uv_layers:
    chassis_mesh.uv_layers.new(name="UVMap")
uv_layer = chassis_mesh.uv_layers.active.data

for poly_face in chassis_mesh.polygons:
    if poly_face.normal.z > 0.65:
        # Front Face: Use Mat_LogoPBR
        poly_face.material_index = 0
    else:
        # Side bevels & back: Use Mat_ChassisArmor
        poly_face.material_index = 1

    # Exact UV mapping from local world coords back to image pixel space
    for loop_idx in poly_face.loop_indices:
        vert_idx = chassis_mesh.loops[loop_idx].vertex_index
        co = chassis_mesh.vertices[vert_idx].co
        px = co.x / scale + cx
        py = co.y / scale + cy
        u = float(np.clip(px / w, 0.0, 1.0))
        v = float(np.clip(py / h, 0.0, 1.0))
        uv_layer[loop_idx].uv = (u, v)

# ─── 5. Stand Vertically (Blender +Z is Up, -Y is Forward) ────────────────────
bpy.ops.object.select_all(action='SELECT')
for obj in bpy.context.selected_objects:
    # Rotate 90 deg around X so local Y becomes 3D Z (Up) and local Z becomes -Y (Forward)
    obj.rotation_euler = (math.radians(90), 0, 0)

bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)

# ─── 8. Export High-Fidelity GLB ───────────────────────────────────────────────
output_dir = os.path.abspath("assets/models/hq")
os.makedirs(output_dir, exist_ok=True)
output_path = os.path.join(output_dir, "main_title_3d.glb")

bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.gltf(
    filepath=output_path,
    export_format='GLB',
    use_selection=True,
    export_materials='EXPORT',
    export_image_format='AUTO'
)

print("Exported authentic 3D title successfully to:", output_path)
