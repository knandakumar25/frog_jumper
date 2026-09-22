import bpy
import os
import shutil
import math
import numpy as np

# ─── 0. Purge Existing Scene ──────────────────────────────────────────────────
for obj in list(bpy.data.objects): bpy.data.objects.remove(obj, do_unlink=True)
for mesh in list(bpy.data.meshes): bpy.data.meshes.remove(mesh, do_unlink=True)
for mat in list(bpy.data.materials): bpy.data.materials.remove(mat, do_unlink=True)
for cam in list(bpy.data.cameras): bpy.data.cameras.remove(cam, do_unlink=True)
for light in list(bpy.data.lights): bpy.data.lights.remove(light, do_unlink=True)

img_dir = os.path.abspath("assets/images")

# ─── 1. Polygon Mask for Emblem Cutout (1376 x 768) ───────────────────────────
poly = [
    (688, 616), (670, 600), (650, 576), (620, 580), (600, 608), (580, 614), (565, 555), (545, 526),
    (485, 520), (245, 520), (170, 435), (165, 345), (245, 252), (335, 245),
    (375, 210), (645, 210), (665, 175), (688, 172), (711, 175), (731, 210),
    (1001, 210), (1041, 245), (1131, 252), (1211, 345), (1206, 435), (1131, 520),
    (891, 520), (830, 526), (810, 555), (796, 614), (776, 608), (756, 580), (726, 576), (706, 600)
]

w_full, h_full = 1376, 768
mask = np.zeros((h_full, w_full), dtype=np.float32)
n = len(poly)
for y in range(h_full):
    intersections = []
    for i in range(n):
        x1, y1 = poly[i]
        x2, y2 = poly[(i + 1) % n]
        if min(y1, y2) <= y < max(y1, y2):
            x_int = x1 + (y - y1) * (x2 - x1) / (y2 - y1)
            intersections.append(x_int)
    intersections.sort()
    for j in range(0, len(intersections) - 1, 2):
        xs = max(0, int(np.floor(intersections[j])))
        xe = min(w_full, int(np.ceil(intersections[j + 1])))
        mask[y, xs:xe] = 1.0

pad = 2
padded = np.pad(mask, pad, mode='edge')
smooth_mask = np.zeros_like(mask)
for dy in range(-pad, pad + 1):
    for dx in range(-pad, pad + 1):
        smooth_mask += padded[pad + dy : pad + dy + h_full, pad + dx : pad + dx + w_full]
smooth_mask /= ((2 * pad + 1) ** 2)

# Dimensions for cropping
crop_w = 1074  # 1089 - 16 + 1
crop_h = 329   # 484 - 156 + 1
aspect = crop_w / crop_h  # ~3.264
plane_w = 4.0
plane_h = plane_w / aspect

# ─── 2. Setup Blender Scene, Camera, Lights, Plane ─────────────────────────────
bpy.ops.mesh.primitive_plane_add(size=1.0, enter_editmode=False, location=(0, 0, 0))
plane_obj = bpy.context.active_object
plane_obj.name = "TitlePlane"
plane_obj.scale = (plane_w, plane_h, 1.0)
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

mat_logo = bpy.data.materials.new("Mat_LocalizedTitle")
mat_logo.use_nodes = True
nodes = mat_logo.node_tree.nodes
links = mat_logo.node_tree.links
nodes.clear()

node_output = nodes.new(type='ShaderNodeOutputMaterial')
node_bsdf = nodes.new(type='ShaderNodeBsdfPrincipled')
links.new(node_bsdf.outputs['BSDF'], node_output.inputs['Surface'])

tex_alb = nodes.new(type='ShaderNodeTexImage')
links.new(tex_alb.outputs['Color'], node_bsdf.inputs['Base Color'])
links.new(tex_alb.outputs['Alpha'], node_bsdf.inputs['Alpha'])

tex_met = nodes.new(type='ShaderNodeTexImage')
links.new(tex_met.outputs['Color'], node_bsdf.inputs['Metallic'])

tex_rou = nodes.new(type='ShaderNodeTexImage')
links.new(tex_rou.outputs['Color'], node_bsdf.inputs['Roughness'])

tex_nor = nodes.new(type='ShaderNodeTexImage')
node_norm_map = nodes.new(type='ShaderNodeNormalMap')
node_norm_map.inputs['Strength'].default_value = 2.4
links.new(tex_nor.outputs['Color'], node_norm_map.inputs['Color'])
links.new(node_norm_map.outputs['Normal'], node_bsdf.inputs['Normal'])

tex_emi = nodes.new(type='ShaderNodeTexImage')
links.new(tex_emi.outputs['Color'], node_bsdf.inputs['Emission Color'])
node_bsdf.inputs['Emission Strength'].default_value = 2.2

if hasattr(mat_logo, 'blend_method'): mat_logo.blend_method = 'BLEND'
if hasattr(mat_logo, 'shadow_method'): mat_logo.shadow_method = 'NONE'
plane_obj.data.materials.append(mat_logo)

# High-Contrast Studio Lights
light_key = bpy.data.lights.new(name="LightKey", type='SUN')
light_key.energy = 1.4
light_key.color = (0.90, 0.95, 1.0)
obj_key = bpy.data.objects.new(name="LightKeyObj", object_data=light_key)
obj_key.rotation_euler = (math.radians(35), math.radians(-25), 0)
bpy.context.collection.objects.link(obj_key)

light_cyan = bpy.data.lights.new(name="LightCyan", type='SUN')
light_cyan.energy = 1.0
light_cyan.color = (0.0, 0.70, 0.95)
obj_cyan = bpy.data.objects.new(name="LightCyanObj", object_data=light_cyan)
obj_cyan.rotation_euler = (math.radians(-35), math.radians(40), 0)
bpy.context.collection.objects.link(obj_cyan)

light_mag = bpy.data.lights.new(name="LightMag", type='SUN')
light_mag.energy = 0.8
light_mag.color = (0.85, 0.05, 0.50)
obj_mag = bpy.data.objects.new(name="LightMagObj", object_data=light_mag)
obj_mag.rotation_euler = (math.radians(25), math.radians(50), 0)
bpy.context.collection.objects.link(obj_mag)

light_point = bpy.data.lights.new(name="LightGlint", type='POINT')
light_point.energy = 15.0
light_point.color = (0.85, 0.92, 1.0)
obj_point = bpy.data.objects.new(name="LightGlintObj", object_data=light_point)
obj_point.location = (0.1, -0.05, 1.4)
bpy.context.collection.objects.link(obj_point)

# Ortho Camera
cam_data = bpy.data.cameras.new("RenderCam")
cam_data.type = 'ORTHO'
cam_data.ortho_scale = plane_w
cam_obj = bpy.data.objects.new("Camera", cam_data)
cam_obj.location = (0.0, 0.0, 4.0)
cam_obj.rotation_euler = (0, 0, 0)
bpy.context.collection.objects.link(cam_obj)
bpy.context.scene.camera = cam_obj

# Render Settings
scene = bpy.context.scene
scene.render.resolution_x = 1920
scene.render.resolution_y = int(1920 / aspect)
scene.render.film_transparent = True
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
scene.render.image_settings.color_depth = '8'
if hasattr(scene, 'eevee'):
    scene.eevee.taa_render_samples = 16

# ─── 3. Image Helper ──────────────────────────────────────────────────────────
def create_or_update_image(name, data_rgba, is_non_color=False):
    img = bpy.data.images.get(name)
    if img is None or img.size[0] != crop_w or img.size[1] != crop_h:
        if img is not None: bpy.data.images.remove(img)
        img = bpy.data.images.new(name, width=crop_w, height=crop_h, alpha=True)
    img.pixels = data_rgba.flatten().tolist()
    if is_non_color:
        img.colorspace_settings.name = 'Non-Color'
    return img

# ─── 4. Processing Pipeline per Language ───────────────────────────────────────
languages = [
    ("arabic", "ar"),
    ("brazilianportuguese", "pt"),
    ("eurofrench", "fr"),
    ("german", "de"),
    ("hebrew", "he"),
    ("indonesian", "id"),
    ("italian", "it"),
    ("japanese", "ja"),
    ("korean", "ko"),
    ("mandarin", "zh"),
    ("polish", "pl"),
    ("spanish", "es"),
    ("thai", "th"),
    ("traditionalchinese", "traditionalchinese"),
    ("turkish", "tr"),
    ("vietnamese", "vi"),
]

print(f"Starting localized title rendering for {len(languages)} languages...")

for lang, alias in languages:
    src_file = f"axiom_force_title_logo_{lang}.jpg"
    src_path = os.path.join(img_dir, src_file)
    if not os.path.exists(src_path):
        print(f"Skipping missing file: {src_file}")
        continue

    print(f"\nProcessing {lang.upper()} ({src_file})...")
    src_img = bpy.data.images.load(src_path)
    p_full = np.array(src_img.pixels[:], dtype=np.float32).reshape((h_full, w_full, 4))
    bpy.data.images.remove(src_img)

    # Apply emblem polygon alpha mask
    p_full[..., 3] = np.clip(smooth_mask, 0.0, 1.0)

    # Crop to 1106x520
    pix = p_full[145:665, 135:1241, :].copy()
    h, w, _ = pix.shape

    r, g, b, a = pix[..., 0], pix[..., 1], pix[..., 2], pix[..., 3]
    lum = 0.299 * r + 0.587 * g + 0.114 * b
    max_c = np.maximum(np.maximum(r, g), b)
    min_c = np.minimum(np.minimum(r, g), b)
    chroma = max_c - min_c

    # 1. Emission Map Generation
    is_cyan = (b > 0.45) & (g > 0.35) & (chroma > 0.20)
    is_magenta = (r > 0.40) & (b > 0.25) & (chroma > 0.20)
    is_intense = (lum > 0.88) & (a > 0.5)

    em_mask = np.zeros((h, w), dtype=np.float32)
    em_mask = np.where(is_cyan, np.clip(chroma * 1.6, 0.0, 1.0), em_mask)
    em_mask = np.where(is_magenta, np.clip(chroma * 1.8, 0.0, 1.0), em_mask)
    em_mask = np.where(is_intense, 1.0, em_mask)
    em_mask *= (a > 0.1).astype(np.float32)

    pix_emission = np.zeros((h, w, 4), dtype=np.float32)
    pix_emission[..., 0] = r * em_mask
    pix_emission[..., 1] = g * em_mask
    pix_emission[..., 2] = b * em_mask
    pix_emission[..., 3] = a

    # 2. Metallic Map
    is_chrome = (lum > 0.42) & (chroma < 0.32) & (a > 0.5)
    is_chassis = (lum <= 0.42) & (a > 0.5)

    m_val = np.zeros((h, w), dtype=np.float32)
    m_val = np.where(is_chassis, 0.10, m_val)
    m_val = np.where(is_chrome, 0.96, m_val)
    m_val = np.where(em_mask > 0.5, 0.10, m_val)
    m_val *= a
    pix_metallic = np.zeros((h, w, 4), dtype=np.float32)
    pix_metallic[..., :3] = m_val[..., None]
    pix_metallic[..., 3] = a

    # 3. Roughness Map
    r_val = np.ones((h, w), dtype=np.float32) * 0.42
    r_val = np.where(is_chrome, 0.16, r_val)
    r_val = np.where(em_mask > 0.4, 0.12, r_val)
    r_val = np.where(a < 0.1, 1.0, r_val)
    pix_roughness = np.zeros((h, w, 4), dtype=np.float32)
    pix_roughness[..., :3] = r_val[..., None]
    pix_roughness[..., 3] = a

    # 4. Normal Map (Sobel filter)
    pad_norm = 1
    lum_pad = np.pad(lum, pad_norm, mode='edge')
    gx = (-1 * lum_pad[:-2, :-2] + 1 * lum_pad[:-2, 2:] +
          -2 * lum_pad[1:-1, :-2] + 2 * lum_pad[1:-1, 2:] +
          -1 * lum_pad[2:, :-2] + 1 * lum_pad[2:, 2:])
    gy = (-1 * lum_pad[:-2, :-2] - 2 * lum_pad[:-2, 1:-1] - 1 * lum_pad[:-2, 2:] +
           1 * lum_pad[2:, :-2] + 2 * lum_pad[2:, 1:-1] + 1 * lum_pad[2:, 2:])
    nx = -gx * 2.5
    ny = -gy * 2.5
    nz = np.ones_like(nx)
    length = np.sqrt(nx**2 + ny**2 + nz**2)
    pix_normal = np.zeros((h, w, 4), dtype=np.float32)
    pix_normal[..., 0] = (nx / length) * 0.5 + 0.5
    pix_normal[..., 1] = (ny / length) * 0.5 + 0.5
    pix_normal[..., 2] = (nz / length) * 0.5 + 0.5
    pix_normal[..., 3] = a

    pix_albedo = pix.copy()

    # 5. Remove subtitle below bottom_y and synthesize glowing cyan laser conduit
    bottom_y = np.zeros(w)
    for x in range(w):
        if x < 75:
            bottom_y[x] = 190.0 - (x - 38.0) * ((190.0 - 168.0) / (75.0 - 38.0))
        elif x > 1030:
            bottom_y[x] = 168.0 + (x - 1030.0) * ((190.0 - 168.0) / (1068.0 - 1030.0))
        else:
            bottom_y[x] = 168.0

    for x in range(w):
        by = int(bottom_y[x])
        pix_albedo[:by, x, 3] = 0.0
        pix_emission[:by, x, 3] = 0.0
        pix_metallic[:by, x, 3] = 0.0
        pix_roughness[:by, x, 3] = 0.0
        pix_normal[:by, x, 3] = 0.0
        if 75 <= x <= 1030:
            pix_albedo[by + 1, x, :3] = [0.0, 0.95, 1.0]; pix_albedo[by + 1, x, 3] = 1.0
            pix_emission[by + 1, x, :3] = [0.0, 0.95, 1.0]; pix_emission[by + 1, x, 3] = 1.0
            pix_albedo[by, x, :3] = [0.0, 0.65, 0.90]; pix_albedo[by, x, 3] = 0.85
            pix_emission[by, x, :3] = [0.0, 0.65, 0.90]; pix_emission[by, x, 3] = 0.85
            if by > 0:
                pix_albedo[by - 1, x, :3] = [0.0, 0.35, 0.55]; pix_albedo[by - 1, x, 3] = 0.35
                pix_emission[by - 1, x, :3] = [0.0, 0.35, 0.55]; pix_emission[by - 1, x, 3] = 0.35

    # Crop to final title bounds (1074 x 329)
    min_x, max_x = 16, 1089
    min_y, max_y = 156, 484

    c_alb = pix_albedo[min_y : max_y + 1, min_x : max_x + 1].copy()
    c_emi = pix_emission[min_y : max_y + 1, min_x : max_x + 1].copy()
    c_met = pix_metallic[min_y : max_y + 1, min_x : max_x + 1].copy()
    c_rou = pix_roughness[min_y : max_y + 1, min_x : max_x + 1].copy()
    c_nor = pix_normal[min_y : max_y + 1, min_x : max_x + 1].copy()

    # 6. Apply High Dynamic Range Contrast ("Bright & Dark at the Same Time")
    # Pitch black obsidian plate
    is_plate = (c_alb[..., 3] > 0.5) & (c_met[..., 0] < 0.30) & (c_emi[..., :3].max(axis=-1) < 0.20)
    c_alb[is_plate, :3] = c_alb[is_plate, :3] * 0.08
    c_rou[is_plate, 0] = 0.70

    # Bright polished chrome lettering & crest
    is_letters = (c_met[..., 0] >= 0.30)
    c_alb[is_letters, :3] = np.clip(c_alb[is_letters, :3] * 1.15, 0.0, 1.0)
    c_rou[is_letters, 0] = np.clip(c_rou[is_letters, 0] * 0.75, 0.05, 0.50)

    # Vibrant neon outlines
    c_emi[..., :3] = np.clip(c_emi[..., :3] * 1.25, 0.0, 1.0)

    # 7. Update Shader Textures & Render
    tex_alb.image = create_or_update_image("T_Alb", c_alb)
    tex_emi.image = create_or_update_image("T_Emi", c_emi)
    tex_met.image = create_or_update_image("T_Met", c_met, is_non_color=True)
    tex_rou.image = create_or_update_image("T_Rou", c_rou, is_non_color=True)
    tex_nor.image = create_or_update_image("T_Nor", c_nor, is_non_color=True)

    out_canonical = os.path.join(img_dir, f"axiom_force_title_2d_{lang}.png")
    scene.render.filepath = out_canonical
    bpy.ops.render.render(write_still=True)
    print(f"Rendered: {out_canonical}")

    # Also create alias copy if different
    if alias != lang:
        out_alias = os.path.join(img_dir, f"axiom_force_title_2d_{alias}.png")
        shutil.copyfile(out_canonical, out_alias)
        print(f"Created alias: {out_alias}")

print("\nAll 16 Localized Titles Rendered Successfully!")
