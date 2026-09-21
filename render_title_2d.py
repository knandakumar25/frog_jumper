import bpy
import math
import os
import numpy as np

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

# ─── 1. Load Source PBR Texture Maps ───────────────────────────────────────────
img_dir = os.path.abspath("assets/images")
albedo_src = bpy.data.images.load(os.path.join(img_dir, "axiom_force_logo_clean.png"))
emission_src = bpy.data.images.load(os.path.join(img_dir, "axiom_force_emission.png"))
metallic_src = bpy.data.images.load(os.path.join(img_dir, "axiom_force_metallic.png"))
roughness_src = bpy.data.images.load(os.path.join(img_dir, "axiom_force_roughness.png"))
normal_src = bpy.data.images.load(os.path.join(img_dir, "axiom_force_normal.png"))

w, h = albedo_src.size  # 1106 x 520
pix_albedo = np.array(albedo_src.pixels[:], dtype=np.float32).reshape((h, w, 4))
pix_emission = np.array(emission_src.pixels[:], dtype=np.float32).reshape((h, w, 4))
pix_metallic = np.array(metallic_src.pixels[:], dtype=np.float32).reshape((h, w, 4))
pix_roughness = np.array(roughness_src.pixels[:], dtype=np.float32).reshape((h, w, 4))
pix_normal = np.array(normal_src.pixels[:], dtype=np.float32).reshape((h, w, 4))

# ─── 2. Crop & Isolate Axiom Force (Remove Reclaim Earth) ─────────────────────
bottom_y = np.zeros(w)
for x in range(w):
    if x < 75:
        bottom_y[x] = 190.0 - (x - 38.0) * ((190.0 - 168.0) / (75.0 - 38.0))
    elif x > 1030:
        bottom_y[x] = 168.0 + (x - 1030.0) * ((190.0 - 168.0) / (1068.0 - 1030.0))
    else:
        bottom_y[x] = 168.0

# Mask out Reclaim Earth and enhance bottom neon laser conduit
for x in range(w):
    by = int(bottom_y[x])
    
    # Zero out everything below the bottom chassis line
    pix_albedo[:by, x, 3] = 0.0
    pix_emission[:by, x, 3] = 0.0
    pix_metallic[:by, x, 3] = 0.0
    pix_roughness[:by, x, 3] = 0.0
    pix_normal[:by, x, 3] = 0.0

    # For the center span (75 to 1030), render a clean glowing deep cyan laser line
    if 75 <= x <= 1030:
        # Core row (by + 1)
        pix_albedo[by + 1, x, :3] = [0.04, 0.35, 0.50]
        pix_albedo[by + 1, x, 3] = 1.0
        pix_emission[by + 1, x, :3] = [0.02, 0.30, 0.45]
        pix_emission[by + 1, x, 3] = 1.0

        # Rim row (by)
        pix_albedo[by, x, :3] = [0.0, 0.25, 0.38]
        pix_albedo[by, x, 3] = 0.85
        pix_emission[by, x, :3] = [0.0, 0.25, 0.38]
        pix_emission[by, x, 3] = 0.85

        # Anti-aliased outer edge (by - 1)
        if by > 0:
            pix_albedo[by - 1, x, :3] = [0.0, 0.15, 0.25]
            pix_albedo[by - 1, x, 3] = 0.35
            pix_emission[by - 1, x, :3] = [0.0, 0.15, 0.25]
            pix_emission[by - 1, x, 3] = 0.35

# Tightly crop to Axiom Force bounds with clean padding
min_x, max_x = 16, 1089
min_y, max_y = 156, 484
crop_w = max_x - min_x + 1  # 1074
crop_h = max_y - min_y + 1  # 329

crop_albedo = pix_albedo[min_y : max_y + 1, min_x : max_x + 1].copy()
crop_emission = pix_emission[min_y : max_y + 1, min_x : max_x + 1].copy()
crop_metallic = pix_metallic[min_y : max_y + 1, min_x : max_x + 1]
crop_roughness = pix_roughness[min_y : max_y + 1, min_x : max_x + 1]
crop_normal = pix_normal[min_y : max_y + 1, min_x : max_x + 1]

# Apply steep dark stealth tone curve: gamma 1.7 + multiplier 0.42
# Converts bright white/chrome letters to dark gunmetal steel and chassis plate to deep obsidian
crop_albedo[..., :3] = (np.clip(crop_albedo[..., :3], 0.0, 1.0) ** 1.7) * 0.42
crop_emission[..., :3] = (np.clip(crop_emission[..., :3], 0.0, 1.0) ** 1.3) * 0.45

def create_bpy_image(name, data_rgba, is_non_color=False):
    img = bpy.data.images.new(name, width=crop_w, height=crop_h, alpha=True)
    img.pixels = data_rgba.flatten().tolist()
    if is_non_color:
        img.colorspace_settings.name = 'Non-Color'
    return img

img_albedo = create_bpy_image("Title_Albedo", crop_albedo)
img_emission = create_bpy_image("Title_Emission", crop_emission)
img_metallic = create_bpy_image("Title_Metallic", crop_metallic, is_non_color=True)
img_roughness = create_bpy_image("Title_Roughness", crop_roughness, is_non_color=True)
img_normal = create_bpy_image("Title_Normal", crop_normal, is_non_color=True)

# ─── 3. Create PBR Material ───────────────────────────────────────────────────
mat_logo = bpy.data.materials.new("Mat_LogoPBR")
mat_logo.use_nodes = True
nodes = mat_logo.node_tree.nodes
links = mat_logo.node_tree.links
nodes.clear()

node_output = nodes.new(type='ShaderNodeOutputMaterial')
node_bsdf = nodes.new(type='ShaderNodeBsdfPrincipled')

# Albedo & Alpha
tex_albedo = nodes.new(type='ShaderNodeTexImage')
tex_albedo.image = img_albedo
links.new(tex_albedo.outputs['Color'], node_bsdf.inputs['Base Color'])
links.new(tex_albedo.outputs['Alpha'], node_bsdf.inputs['Alpha'])

# Metallic
tex_metallic = nodes.new(type='ShaderNodeTexImage')
tex_metallic.image = img_metallic
links.new(tex_metallic.outputs['Color'], node_bsdf.inputs['Metallic'])

# Roughness
tex_roughness = nodes.new(type='ShaderNodeTexImage')
tex_roughness.image = img_roughness
links.new(tex_roughness.outputs['Color'], node_bsdf.inputs['Roughness'])

# Normal Map
tex_normal = nodes.new(type='ShaderNodeTexImage')
tex_normal.image = img_normal
node_norm_map = nodes.new(type='ShaderNodeNormalMap')
node_norm_map.inputs['Strength'].default_value = 2.4
links.new(tex_normal.outputs['Color'], node_norm_map.inputs['Color'])
links.new(node_norm_map.outputs['Normal'], node_bsdf.inputs['Normal'])

# Emission (rich, deep cybernetic glow without overblown glare)
tex_emission = nodes.new(type='ShaderNodeTexImage')
tex_emission.image = img_emission
links.new(tex_emission.outputs['Color'], node_bsdf.inputs['Emission Color'])
node_bsdf.inputs['Emission Strength'].default_value = 1.0

links.new(node_bsdf.outputs['BSDF'], node_output.inputs['Surface'])

if hasattr(mat_logo, 'blend_method'):
    mat_logo.blend_method = 'BLEND'
if hasattr(mat_logo, 'shadow_method'):
    mat_logo.shadow_method = 'NONE'

# ─── 4. Create Plane Mesh with Exact Aspect Ratio ─────────────────────────────
aspect = crop_w / crop_h  # ~3.264
plane_w = 4.0
plane_h = plane_w / aspect

bpy.ops.mesh.primitive_plane_add(size=1.0, enter_editmode=False, location=(0, 0, 0))
plane_obj = bpy.context.active_object
plane_obj.name = "TitlePlane"
plane_obj.scale = (plane_w, plane_h, 1.0)
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
plane_obj.data.materials.append(mat_logo)

mesh = plane_obj.data
if not mesh.uv_layers:
    mesh.uv_layers.new(name="UVMap")

# ─── 5. Darker Cybernetic Lighting with Balanced Highlights ───────────────────
# Softened key sun light to preserve the dark stealth gunmetal tones
light_key = bpy.data.lights.new(name="LightKey", type='SUN')
light_key.energy = 0.6
light_key.color = (0.75, 0.82, 0.90)
obj_key = bpy.data.objects.new(name="LightKeyObj", object_data=light_key)
obj_key.rotation_euler = (math.radians(35), math.radians(-25), 0)
bpy.context.collection.objects.link(obj_key)

light_cyan = bpy.data.lights.new(name="LightCyan", type='SUN')
light_cyan.energy = 0.4
light_cyan.color = (0.0, 0.45, 0.65)
obj_cyan = bpy.data.objects.new(name="LightCyanObj", object_data=light_cyan)
obj_cyan.rotation_euler = (math.radians(-35), math.radians(40), 0)
bpy.context.collection.objects.link(obj_cyan)

light_mag = bpy.data.lights.new(name="LightMag", type='SUN')
light_mag.energy = 0.35
light_mag.color = (0.55, 0.0, 0.30)
obj_mag = bpy.data.objects.new(name="LightMagObj", object_data=light_mag)
obj_mag.rotation_euler = (math.radians(25), math.radians(50), 0)
bpy.context.collection.objects.link(obj_mag)

# Subtle point light for bevel definition without glare
light_point = bpy.data.lights.new(name="LightGlint", type='POINT')
light_point.energy = 5.0
light_point.color = (0.60, 0.75, 0.88)
obj_point = bpy.data.objects.new(name="LightGlintObj", object_data=light_point)
obj_point.location = (0.1, -0.05, 1.4)
bpy.context.collection.objects.link(obj_point)

# ─── 6. Camera & Render Settings ───────────────────────────────────────────────
cam_data = bpy.data.cameras.new("RenderCam")
cam_data.type = 'ORTHO'
cam_data.ortho_scale = plane_w
cam_data.clip_start = 0.1
cam_data.clip_end = 20.0

cam_obj = bpy.data.objects.new("Camera", cam_data)
cam_obj.location = (0.0, 0.0, 4.0)
cam_obj.rotation_euler = (0, 0, 0)
bpy.context.collection.objects.link(cam_obj)
bpy.context.scene.camera = cam_obj

scene = bpy.context.scene
scene.render.resolution_x = 1920
scene.render.resolution_y = int(1920 / aspect)  # ~588
scene.render.resolution_percentage = 100
scene.render.film_transparent = True
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
scene.render.image_settings.color_depth = '8'

output_file = os.path.join(img_dir, "axiom_force_title_2d.png")
scene.render.filepath = output_file

print(f"Rendering Dark Gunmetal 2D Axiom Force Title ({scene.render.resolution_x}x{scene.render.resolution_y}) to {output_file}...")
bpy.ops.render.render(write_still=True)
print("Dark Gunmetal Axiom Force 2D Title Render Complete!")
