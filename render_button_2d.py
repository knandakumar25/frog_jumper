import bpy
import bmesh
import math
import os

# ─── 0. Purge Existing Scene ──────────────────────────────────────────────────
for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj, do_unlink=True)
for mesh in list(bpy.data.meshes):
    bpy.data.meshes.remove(mesh, do_unlink=True)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat, do_unlink=True)
for cam in list(bpy.data.cameras):
    bpy.data.cameras.remove(cam, do_unlink=True)
for light in list(bpy.data.lights):
    bpy.data.lights.remove(light, do_unlink=True)

# ─── 1. Helper to Create PBR Material ─────────────────────────────────────────
def create_pbr_mat(name, base_color, metallic=0.40, roughness=0.60, emission_color=(0,0,0,1), emission_strength=0.0):
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

# Dimensions (XY plane, Z is depth towards camera at +Z)
hw = 1.60     # Half-width (3.2 total)
hh = 0.28     # Half-height (0.56 total) -> 3.2 / 0.56 = 5.714
chamf = 0.14  # Corner cut size
fd = 0.05     # Front depth towards camera (+Z)
bd = -0.05    # Back depth (-Z)

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
v_front = [bm_body.verts.new((p[0], p[1], fd)) for p in pts_chamfer]
v_back  = [bm_body.verts.new((p[0], p[1], bd)) for p in pts_chamfer]

bm_body.faces.new(v_front)
bm_body.faces.new(v_back[::-1])
for i in range(len(pts_chamfer)):
    j = (i + 1) % len(pts_chamfer)
    bm_body.faces.new([v_front[j], v_front[i], v_back[i], v_back[j]])

body_mesh = bpy.data.meshes.new("CyberButtonMesh")
bm_body.to_mesh(body_mesh)
bm_body.free()
body_obj = bpy.data.objects.new("CyberButton", body_mesh)
bpy.context.collection.objects.link(body_obj)

# 2. Glowing Neon Border Frame (Chamfered rim around front face)
bm_border = bmesh.new()
thick = 0.024
pts_border_outer = pts_chamfer
pts_border_inner = [
    (-hw + thick, hh - thick),
    (hw - chamf - thick * 0.4, hh - thick),
    (hw - thick, hh - chamf - thick * 0.4),
    (hw - thick, -hh + thick),
    (-hw + chamf + thick * 0.4, -hh + thick),
    (-hw + thick, -hh + chamf + thick * 0.4),
]

bf_outer = [bm_border.verts.new((p[0], p[1], fd + 0.012)) for p in pts_border_outer]
bf_inner = [bm_border.verts.new((p[0], p[1], fd + 0.012)) for p in pts_border_inner]
bb_outer = [bm_border.verts.new((p[0], p[1], fd - 0.01)) for p in pts_border_outer]

for i in range(len(pts_border_outer)):
    j = (i + 1) % len(pts_border_outer)
    bm_border.faces.new([bf_outer[j], bf_outer[i], bf_inner[i], bf_inner[j]])
    bm_border.faces.new([bf_outer[j], bb_outer[j], bb_outer[i], bf_outer[i]])

border_mesh = bpy.data.meshes.new("NeonBorderMesh")
bm_border.to_mesh(border_mesh)
bm_border.free()
border_obj = bpy.data.objects.new("NeonBorder", border_mesh)
bpy.context.collection.objects.link(border_obj)

# 3. Left Indicator Bar (Deep Dark Crimson/Pink Neon Accent)
bm_bar = bmesh.new()
bar_x0 = -hw + 0.04
bar_x1 = -hw + 0.10
bar_h  = hh - 0.06
bar_z  = fd + 0.015

bar_pts = [
    (bar_x0, bar_h, bar_z),
    (bar_x1, bar_h, bar_z),
    (bar_x1, -bar_h, bar_z),
    (bar_x0, -bar_h, bar_z)
]
bv_front = [bm_bar.verts.new(p) for p in bar_pts]
bv_back  = [bm_bar.verts.new((p[0], p[1], bar_z - 0.03)) for p in bar_pts]
bm_bar.faces.new(bv_front)
bm_bar.faces.new(bv_back[::-1])
for i in range(4):
    j = (i + 1) % 4
    bm_bar.faces.new([bv_front[j], bv_front[i], bv_back[i], bv_back[j]])

bar_mesh = bpy.data.meshes.new("NeonBarMesh")
bm_bar.to_mesh(bar_mesh)
bm_bar.free()
bar_obj = bpy.data.objects.new("NeonBar", bar_mesh)
bpy.context.collection.objects.link(bar_obj)

# 4. Top-Right Corner Reticle (+) Accent
bm_ret = bmesh.new()
rx = hw - chamf * 0.8
ry = hh - chamf * 0.8
rz = fd + 0.015
rw = 0.03
rl = 0.08

bm_ret.faces.new([
    bm_ret.verts.new((rx - rl, ry - rw/2, rz)),
    bm_ret.verts.new((rx + rl, ry - rw/2, rz)),
    bm_ret.verts.new((rx + rl, ry + rw/2, rz)),
    bm_ret.verts.new((rx - rl, ry + rw/2, rz))
])
bm_ret.faces.new([
    bm_ret.verts.new((rx - rw/2, ry - rl, rz)),
    bm_ret.verts.new((rx + rw/2, ry - rl, rz)),
    bm_ret.verts.new((rx + rw/2, ry + rl, rz)),
    bm_ret.verts.new((rx - rw/2, ry + rl, rz))
])
ret_mesh = bpy.data.meshes.new("ReticleMesh")
bm_ret.to_mesh(ret_mesh)
bm_ret.free()
ret_obj = bpy.data.objects.new("CornerReticle", ret_mesh)
bpy.context.collection.objects.link(ret_obj)

# ─── 2. Camera & Subtle Dark Lighting ─────────────────────────────────────────
cam_data = bpy.data.cameras.new("RenderCam")
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 3.32
cam_data.clip_start = 0.1
cam_data.clip_end = 20.0

cam_obj = bpy.data.objects.new("Camera", cam_data)
cam_obj.location = (0.0, 0.0, 5.0)
cam_obj.rotation_euler = (0, 0, 0)
bpy.context.collection.objects.link(cam_obj)
bpy.context.scene.camera = cam_obj

# Low-intensity soft key sun (keeps obsidian black body dark and matte)
light_key = bpy.data.lights.new(name="LightKey", type='SUN')
light_key.energy = 0.5
light_key.color = (0.75, 0.80, 0.88)
obj_key = bpy.data.objects.new(name="LightKeyObj", object_data=light_key)
obj_key.rotation_euler = (math.radians(35), math.radians(-25), 0)
bpy.context.collection.objects.link(obj_key)

# Soft cyan rim light for subtle bevel definition
light_cyan = bpy.data.lights.new(name="LightCyan", type='SUN')
light_cyan.energy = 0.3
light_cyan.color = (0.0, 0.40, 0.55)
obj_cyan = bpy.data.objects.new(name="LightCyanObj", object_data=light_cyan)
obj_cyan.rotation_euler = (math.radians(-35), math.radians(35), 0)
bpy.context.collection.objects.link(obj_cyan)

# ─── 3. Render Settings ────────────────────────────────────────────────────────
scene = bpy.context.scene
scene.render.resolution_x = 800
scene.render.resolution_y = 140
scene.render.resolution_percentage = 100
scene.render.film_transparent = True
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
scene.render.image_settings.color_depth = '8'

img_dir = os.path.abspath("assets/images")
os.makedirs(img_dir, exist_ok=True)

# ─── 4. Render 3 States with Deep Obsidian & Dark Cyber Accents ────────────────
states = [
    {
        "name": "cyber_button_normal.png",
        "body_color": (0.003, 0.005, 0.008, 1.0),   # Deep pitch black obsidian
        "body_metallic": 0.40,
        "body_roughness": 0.60,
        "border_color": (0.0, 0.38, 0.50, 1.0),     # Dark cyber cyan/teal
        "border_energy": 1.0,
        "bar_color": (0.48, 0.0, 0.22, 1.0),       # Dark deep crimson
        "bar_energy": 1.2,
        "reticle_color": (0.0, 0.38, 0.50, 1.0),
        "reticle_energy": 1.0
    },
    {
        "name": "cyber_button_hover.png",
        "body_color": (0.010, 0.016, 0.026, 1.0),  # Subtle dark charcoal blue
        "body_metallic": 0.50,
        "body_roughness": 0.45,
        "border_color": (0.0, 0.55, 0.72, 1.0),
        "border_energy": 2.0,
        "bar_color": (0.65, 0.02, 0.32, 1.0),
        "bar_energy": 2.2,
        "reticle_color": (0.0, 0.55, 0.72, 1.0),
        "reticle_energy": 2.0
    },
    {
        "name": "cyber_button_pressed.png",
        "body_color": (0.001, 0.002, 0.004, 1.0),  # Absolute pitch obsidian
        "body_metallic": 0.30,
        "body_roughness": 0.65,
        "border_color": (0.48, 0.0, 0.22, 1.0),
        "border_energy": 1.4,
        "bar_color": (0.0, 0.38, 0.50, 1.0),
        "bar_energy": 1.4,
        "reticle_color": (0.48, 0.0, 0.22, 1.0),
        "reticle_energy": 1.4
    }
]

for st in states:
    mat_body = create_pbr_mat(
        "MatBody_" + st["name"],
        st["body_color"],
        metallic=st["body_metallic"],
        roughness=st["body_roughness"],
        emission_color=st["body_color"],
        emission_strength=0.0
    )
    mat_border = create_pbr_mat(
        "MatBorder_" + st["name"],
        st["border_color"],
        metallic=0.1,
        roughness=0.1,
        emission_color=st["border_color"],
        emission_strength=st["border_energy"]
    )
    mat_bar = create_pbr_mat(
        "MatBar_" + st["name"],
        st["bar_color"],
        metallic=0.1,
        roughness=0.1,
        emission_color=st["bar_color"],
        emission_strength=st["bar_energy"]
    )
    mat_reticle = create_pbr_mat(
        "MatReticle_" + st["name"],
        st["reticle_color"],
        metallic=0.1,
        roughness=0.1,
        emission_color=st["reticle_color"],
        emission_strength=st["reticle_energy"]
    )

    body_obj.data.materials.clear()
    body_obj.data.materials.append(mat_body)

    border_obj.data.materials.clear()
    border_obj.data.materials.append(mat_border)

    bar_obj.data.materials.clear()
    bar_obj.data.materials.append(mat_bar)

    ret_obj.data.materials.clear()
    ret_obj.data.materials.append(mat_reticle)

    out_path = os.path.join(img_dir, st["name"])
    scene.render.filepath = out_path
    print(f"Rendering Dark {st['name']} to {out_path}...")
    bpy.ops.render.render(write_still=True)

print("All Darker 2D Button States Rendered Successfully!")
