import bpy
import bmesh
import math
import os

# ─── 0. Complete Scene & Orphan Purge ─────────────────────────────────────────
for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj, do_unlink=True)
for mesh in list(bpy.data.meshes):
    bpy.data.meshes.remove(mesh, do_unlink=True)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat, do_unlink=True)

# ─── 1. Helper to Create PBR Material with Emission ───────────────────────────
def create_pbr_material(name, base_color, metallic=0.9, roughness=0.2, emission_color=(0,0,0,1), emission_strength=0.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs['Base Color'].default_value = base_color
        bsdf.inputs['Metallic'].default_value = metallic
        bsdf.inputs['Roughness'].default_value = roughness
        if 'Emission Color' in bsdf.inputs:
            bsdf.inputs['Emission Color'].default_value = emission_color
            bsdf.inputs['Emission Strength'].default_value = emission_strength
    return mat

# Button Materials
mat_btn_body    = create_pbr_material("Mat_ButtonBody", (0.03, 0.05, 0.09, 1.0), 0.92, 0.22, (0.01, 0.03, 0.06, 1.0), 0.5)
mat_neon_border = create_pbr_material("Mat_NeonBorder", (0.0, 0.85, 1.0, 1.0), 0.2, 0.1, (0.0, 0.94, 1.0, 1.0), 3.5)
mat_neon_bar    = create_pbr_material("Mat_NeonBar", (1.0, 0.0, 0.55, 1.0), 0.2, 0.1, (1.0, 0.0, 0.55, 1.0), 4.0)
mat_cyber_trim  = create_pbr_material("Mat_CyberTrim", (0.10, 0.14, 0.22, 1.0), 0.95, 0.18, (0.0, 0.4, 0.6, 1.0), 1.2)

# Dimensions for CyberButton (2.5D Plate)
# Standing in XZ plane: X is width, Z is height, Y is depth (-Y is front face towards camera in Godot +Z)
hw = 1.60    # Half-width (3.2 total)
hh = 0.35    # Half-height (0.7 total)
chamf = 0.16 # Corner cut size
fd = 0.06    # Front depth (-fd)
bd = 0.06    # Back depth (+bd)

# 1. Main Chamfered Body Plate (Top-Right and Bottom-Left 45-deg cuts)
pts_chamfer = [
    (-hw, hh),                     # Top Left
    (hw - chamf, hh),              # Top Right Start
    (hw, hh - chamf),              # Top Right End
    (hw, -hh),                     # Bottom Right
    (-hw + chamf, -hh),            # Bottom Left Start
    (-hw, -hh + chamf),            # Bottom Left End
]

bm_body = bmesh.new()
v_front = [bm_body.verts.new((p[0], -fd, p[1])) for p in pts_chamfer]
v_back  = [bm_body.verts.new((p[0], bd, p[1])) for p in pts_chamfer]

bm_body.faces.new(v_front[::-1])
bm_body.faces.new(v_back)
for i in range(len(pts_chamfer)):
    j = (i + 1) % len(pts_chamfer)
    bm_body.faces.new([v_front[i], v_front[j], v_back[j], v_back[i]])

body_mesh = bpy.data.meshes.new("CyberButtonMesh")
bm_body.to_mesh(body_mesh)
bm_body.free()

body_obj = bpy.data.objects.new("CyberButton", body_mesh)
bpy.context.collection.objects.link(body_obj)
body_obj.data.materials.append(mat_btn_body)

# 2. Glowing Neon Border Frame (Chamfered rim around the front face)
bm_border = bmesh.new()
thick = 0.028
pts_border_outer = pts_chamfer
pts_border_inner = [
    (-hw + thick, hh - thick),
    (hw - chamf - thick * 0.4, hh - thick),
    (hw - thick, hh - chamf - thick * 0.4),
    (hw - thick, -hh + thick),
    (-hw + chamf + thick * 0.4, -hh + thick),
    (-hw + thick, -hh + chamf + thick * 0.4),
]

bf_outer = [bm_border.verts.new((p[0], -fd - 0.015, p[1])) for p in pts_border_outer]
bf_inner = [bm_border.verts.new((p[0], -fd - 0.015, p[1])) for p in pts_border_inner]
bb_outer = [bm_border.verts.new((p[0], -fd + 0.01, p[1])) for p in pts_border_outer]

for i in range(len(pts_border_outer)):
    j = (i + 1) % len(pts_border_outer)
    bm_border.faces.new([bf_outer[i], bf_outer[j], bf_inner[j], bf_inner[i]])
    bm_border.faces.new([bf_outer[i], bb_outer[i], bb_outer[j], bf_outer[j]])

border_mesh = bpy.data.meshes.new("NeonBorderMesh")
bm_border.to_mesh(border_mesh)
bm_border.free()

border_obj = bpy.data.objects.new("NeonBorder", border_mesh)
bpy.context.collection.objects.link(border_obj)
border_obj.data.materials.append(mat_neon_border)

# 3. Left Indicator Bar (Magenta/Pink Neon Accent)
bm_bar = bmesh.new()
bar_x0 = -hw + 0.05
bar_x1 = -hw + 0.12
bar_y  = -fd - 0.02
bar_h  = hh - 0.08

bar_pts = [
    (bar_x0, bar_y, bar_h),
    (bar_x1, bar_y, bar_h),
    (bar_x1, bar_y, -bar_h),
    (bar_x0, bar_y, -bar_h)
]
bv_front = [bm_bar.verts.new(p) for p in bar_pts]
bv_back  = [bm_bar.verts.new((p[0], bar_y + 0.03, p[2])) for p in bar_pts]
bm_bar.faces.new(bv_front[::-1])
bm_bar.faces.new(bv_back)
for i in range(4):
    j = (i + 1) % 4
    bm_bar.faces.new([bv_front[i], bv_front[j], bv_back[j], bv_back[i]])

bar_mesh = bpy.data.meshes.new("NeonBarMesh")
bm_bar.to_mesh(bar_mesh)
bm_bar.free()

bar_obj = bpy.data.objects.new("NeonBar", bar_mesh)
bpy.context.collection.objects.link(bar_obj)
bar_obj.data.materials.append(mat_neon_bar)

# 4. Top-Right Corner Reticle (+) Accent
bm_ret = bmesh.new()
rx = hw - chamf * 0.8
rz = hh - chamf * 0.8
ry = -fd - 0.02
rw = 0.04
rl = 0.10

bm_ret.faces.new([
    bm_ret.verts.new((rx - rl, ry, rz + rw/2)),
    bm_ret.verts.new((rx + rl, ry, rz + rw/2)),
    bm_ret.verts.new((rx + rl, ry, rz - rw/2)),
    bm_ret.verts.new((rx - rl, ry, rz - rw/2))
])
bm_ret.faces.new([
    bm_ret.verts.new((rx - rw/2, ry, rz + rl)),
    bm_ret.verts.new((rx + rw/2, ry, rz + rl)),
    bm_ret.verts.new((rx + rw/2, ry, rz - rl)),
    bm_ret.verts.new((rx - rw/2, ry, rz - rl))
])

ret_mesh = bpy.data.meshes.new("ReticleMesh")
bm_ret.to_mesh(ret_mesh)
bm_ret.free()

ret_obj = bpy.data.objects.new("CornerReticle", ret_mesh)
bpy.context.collection.objects.link(ret_obj)
ret_obj.data.materials.append(mat_neon_border)

# Select all newly created objects to export
bpy.ops.object.select_all(action='SELECT')

output_dir = os.path.abspath("assets/models/hq")
output_path = os.path.join(output_dir, "cyber_button_mesh.glb")
bpy.ops.export_scene.gltf(
    filepath=output_path,
    export_format='GLB',
    use_selection=True,
    export_materials='EXPORT'
)

print("Exported clean 2.5D cyber button mesh successfully to:", output_path)
