"""
Build Cyber Shogun (Kaito Kurogane) completely from scratch.
Zero local character references or existing character models.
Sourced strictly from:
1. CC0 Humanizer/MakeHuman base mesh & Asian male morph targets (Godot Asset Library ID 3699)
2. Procedural Japanese Cyber-Samurai Armor, Kabuto & Katana (bmesh)
3. Procedural Humanoid Biped Armature and Hero Animations (Standing, Idle, Running, Blade_Dash, Parry_Stance, etc.)
"""

import bpy
import bmesh
import math
import os
import shutil
from mathutils import Vector, Matrix, Quaternion, Euler

HUMANIZER_DIR = r"C:\Users\karthiknk21\.gemini\antigravity-cli\brain\51c70920-0a37-4082-8177-2c677a76cad3\scratch\humanizer"
SCRATCH_DIR = r"C:\Users\karthiknk21\.gemini\antigravity-cli\brain\51c70920-0a37-4082-8177-2c677a76cad3\scratch\shogun_redesign"
PROJECT_ROOT = r"D:\Projects\frog_jumper"

OUT_GLB = os.path.join(PROJECT_ROOT, "assets", "models", "hq", "cyber_shogun.glb")
OUT_BLEND = os.path.join(PROJECT_ROOT, "assets", "models", "hq", "cyber_shogun.blend")
OUT_WEB_PUBLIC = os.path.join(PROJECT_ROOT, "website", "public", "models", "shogun.glb")
OUT_WEB_DIST = os.path.join(PROJECT_ROOT, "website", "dist", "models", "shogun.glb")

ARTIFACT_DIR = r"C:\Users\karthiknk21\.gemini\antigravity-cli\brain\51c70920-0a37-4082-8177-2c677a76cad3"
RENDER_PORTRAIT = os.path.join(ARTIFACT_DIR, "cyber_shogun_portrait.png")
RENDER_FULLBODY = os.path.join(ARTIFACT_DIR, "cyber_shogun_fullbody.png")


def load_target_deltas(target_path):
    deltas = {}
    if not os.path.exists(target_path):
        return deltas
    with open(target_path, 'r', encoding='utf-8') as f:
        for line in f:
            line = line.strip()
            if not line or line.startswith('#'):
                continue
            parts = line.split()
            if len(parts) == 4:
                deltas[int(parts[0])] = Vector((float(parts[1]), float(parts[2]), float(parts[3])))
    return deltas


def build_asian_male_body():
    base_obj_path = os.path.join(HUMANIZER_DIR, "base.obj")
    d_young = load_target_deltas(os.path.join(HUMANIZER_DIR, "asian-male-young.target"))
    d_old = load_target_deltas(os.path.join(HUMANIZER_DIR, "asian-male-old.target"))
    d_muscle = load_target_deltas(os.path.join(HUMANIZER_DIR, "universal-male-young-maxmuscle-averageweight.target"))

    raw_verts = []
    raw_uvs = []
    face_v_indices = []
    face_vt_indices = []

    with open(base_obj_path, 'r', encoding='utf-8') as f:
        for line in f:
            if line.startswith('v '):
                raw_verts.append(Vector([float(x) for x in line.strip().split()[1:4]]))
            elif line.startswith('vt '):
                raw_uvs.append([float(x) for x in line.strip().split()[1:3]])
            elif line.startswith('f '):
                parts = line.strip().split()[1:]
                v_idx = [int(p.split('/')[0]) - 1 for p in parts]
                vt_idx = [int(p.split('/')[1]) - 1 if len(p.split('/')) > 1 and p.split('/')[1] else -1 for p in parts]
                face_v_indices.append(v_idx)
                face_vt_indices.append(vt_idx)

    # 65% young Asian male + 35% mature Asian male + 75% athletic muscle target
    for idx in range(len(raw_verts)):
        dv = Vector((0.0, 0.0, 0.0))
        if idx in d_young:
            dv += d_young[idx] * 0.65
        if idx in d_old:
            dv += d_old[idx] * 0.35
        if idx in d_muscle:
            dv += d_muscle[idx] * 0.75
        raw_verts[idx] += dv

    body_vert_set = set(range(13380)) | set(range(14598, 14742))
    body_ys = [raw_verts[i].y for i in range(13380)]
    min_y = min(body_ys)
    max_y = max(body_ys)
    total_h = max_y - min_y
    target_h = 1.88
    scale = target_h / total_h

    # Upright coordinates (+Z Up, -Y Forward)
    v_map = {}
    transformed_verts = []
    for old_idx in sorted(list(body_vert_set)):
        v_map[old_idx] = len(transformed_verts)
        ov = raw_verts[old_idx]
        nx = ov.x * scale
        ny = -ov.z * scale
        nz = (ov.y - min_y) * scale
        transformed_verts.append((nx, ny, nz))

    mesh = bpy.data.meshes.new("CyberShogun_BodyMesh")
    obj = bpy.data.objects.new("CyberShogun_Body", mesh)
    bpy.context.collection.objects.link(obj)

    bm = bmesh.new()
    bm_verts = [bm.verts.new(v) for v in transformed_verts]
    bm.verts.ensure_lookup_table()

    uv_layer = bm.loops.layers.uv.new("UVMap")

    for f_idx, v_indices in enumerate(face_v_indices):
        if all(idx in v_map for idx in v_indices):
            try:
                face_bm_verts = [bm_verts[v_map[idx]] for idx in v_indices]
                face = bm.faces.new(face_bm_verts)
                vt_idx = face_vt_indices[f_idx]
                for loop_idx, loop in enumerate(face.loops):
                    t_idx = vt_idx[loop_idx]
                    if t_idx >= 0 and t_idx < len(raw_uvs):
                        loop[uv_layer].uv = (raw_uvs[t_idx][0], raw_uvs[t_idx][1])
            except ValueError:
                pass

    bm.to_mesh(mesh)
    bm.free()
    mesh.update()

    return obj, scale, min_y


def create_cylinder_mesh(bm, radius_bottom, radius_top, height, segments=16, offset=Vector((0,0,0)), rot=Euler((0,0,0)), mat_idx=0):
    mat = Matrix.Translation(offset) @ rot.to_matrix().to_4x4()
    verts_bottom = []
    verts_top = []
    for i in range(segments):
        angle = 2.0 * math.pi * i / segments
        vb = mat @ Vector((radius_bottom * math.cos(angle), radius_bottom * math.sin(angle), -height * 0.5))
        vt = mat @ Vector((radius_top * math.cos(angle), radius_top * math.sin(angle), height * 0.5))
        verts_bottom.append(bm.verts.new(vb))
        verts_top.append(bm.verts.new(vt))
    bm.verts.ensure_lookup_table()

    for i in range(segments):
        i_next = (i + 1) % segments
        f = bm.faces.new([verts_bottom[i], verts_bottom[i_next], verts_top[i_next], verts_top[i]])
        f.material_index = mat_idx
    f_b = bm.faces.new(verts_bottom)
    f_b.material_index = mat_idx
    f_t = bm.faces.new(reversed(verts_top))
    f_t.material_index = mat_idx


def create_curved_plate(bm, center, width, height, thickness, curvature=0.03, segments_x=4, segments_z=4, rot=Euler((0,0,0)), mat_idx=0):
    mat = Matrix.Translation(center) @ rot.to_matrix().to_4x4()
    front_verts = []
    back_verts = []

    for r in range(segments_z + 1):
        fz = (r / segments_z - 0.5) * height
        row_f = []
        row_b = []
        for c in range(segments_x + 1):
            fx = (c / segments_x - 0.5) * width
            curve_y = -math.cos((c / segments_x - 0.5) * math.pi) * curvature
            vf = mat @ Vector((fx, curve_y - thickness * 0.5, fz))
            vb = mat @ Vector((fx, curve_y + thickness * 0.5, fz))
            row_f.append(bm.verts.new(vf))
            row_b.append(bm.verts.new(vb))
        front_verts.append(row_f)
        back_verts.append(row_b)

    bm.verts.ensure_lookup_table()
    for r in range(segments_z):
        for c in range(segments_x):
            f1 = bm.faces.new([front_verts[r][c], front_verts[r][c+1], front_verts[r+1][c+1], front_verts[r+1][c]])
            f1.material_index = mat_idx
            f2 = bm.faces.new([back_verts[r][c], back_verts[r+1][c], back_verts[r+1][c+1], back_verts[r][c+1]])
            f2.material_index = mat_idx

    for c in range(segments_x):
        f = bm.faces.new([front_verts[0][c], back_verts[0][c], back_verts[0][c+1], front_verts[0][c+1]])
        f.material_index = mat_idx
        f = bm.faces.new([front_verts[segments_z][c], front_verts[segments_z][c+1], back_verts[segments_z][c+1], back_verts[segments_z][c]])
        f.material_index = mat_idx
    for r in range(segments_z):
        f = bm.faces.new([front_verts[r][0], front_verts[r+1][0], back_verts[r+1][0], back_verts[r][0]])
        f.material_index = mat_idx
        f = bm.faces.new([front_verts[r][segments_x], back_verts[r][segments_x], back_verts[r+1][segments_x], front_verts[r+1][segments_x]])
        f.material_index = mat_idx


def build_cyber_armor_and_katana(bm):
    # Material indices:
    # 0: ShogunFace_Mat (Body)
    # 1: ShogunArmor_Mat (Crimson lacquer & obsidian plates)
    # 2: ShogunGold_Mat (Brass gold crest & fittings)
    # 3: ShogunBlade_Mat (Steel blade & neon plasma edge)

    # 1. HACHIMAKI & KABUTO CREST (Head)
    create_curved_plate(bm, center=Vector((0.0, -0.096, 1.745)), width=0.17, height=0.038, thickness=0.012, curvature=0.035, segments_x=6, mat_idx=1)
    # Gold Crest (Maedate)
    create_curved_plate(bm, center=Vector((0.0, -0.108, 1.765)), width=0.048, height=0.055, thickness=0.008, curvature=0.005, mat_idx=2)
    # Side Audio / Fukikaeshi
    create_curved_plate(bm, center=Vector((-0.098, -0.015, 1.735)), width=0.032, height=0.058, thickness=0.018, rot=Euler((0, 0, math.radians(25))), mat_idx=1)
    create_curved_plate(bm, center=Vector((0.098, -0.015, 1.735)), width=0.032, height=0.058, thickness=0.018, rot=Euler((0, 0, math.radians(-25))), mat_idx=1)
    # Gold rims on audio modules
    create_curved_plate(bm, center=Vector((-0.108, -0.015, 1.735)), width=0.012, height=0.045, thickness=0.008, mat_idx=2)
    create_curved_plate(bm, center=Vector((0.108, -0.015, 1.735)), width=0.012, height=0.045, thickness=0.008, mat_idx=2)
    # Rear Shikoro neck guards
    for i in range(3):
        z_pos = 1.66 - i * 0.032
        y_pos = 0.082 + i * 0.022
        w = 0.16 + i * 0.025
        create_curved_plate(bm, center=Vector((0.0, y_pos, z_pos)), width=w, height=0.032, thickness=0.009, curvature=-0.045, segments_x=6, mat_idx=1)

    # Chonmage Topknot & Gold Ring
    create_cylinder_mesh(bm, radius_bottom=0.022, radius_top=0.016, height=0.075, segments=12, offset=Vector((0.0, 0.035, 1.885)), rot=Euler((math.radians(20), 0, 0)), mat_idx=1)
    create_cylinder_mesh(bm, radius_bottom=0.025, radius_top=0.025, height=0.015, segments=12, offset=Vector((0.0, 0.028, 1.865)), rot=Euler((math.radians(20), 0, 0)), mat_idx=2)

    # 2. DO (SAMURAI CUIRASS)
    # Neck Gorget
    create_curved_plate(bm, center=Vector((0.0, -0.065, 1.515)), width=0.18, height=0.045, thickness=0.014, curvature=0.042, segments_x=6, mat_idx=1)
    # Chest Breastplate - 4 overlapping plates
    for i in range(4):
        z_pos = 1.43 - i * 0.072
        y_pos = -0.115 + i * 0.012
        w = 0.32 - i * 0.022
        create_curved_plate(bm, center=Vector((0.0, y_pos, z_pos)), width=w, height=0.080, thickness=0.018, curvature=0.055, segments_x=8, mat_idx=1)
    # Back Armor
    for i in range(4):
        z_pos = 1.43 - i * 0.072
        y_pos = 0.108 - i * 0.010
        w = 0.30 - i * 0.020
        create_curved_plate(bm, center=Vector((0.0, y_pos, z_pos)), width=w, height=0.080, thickness=0.018, curvature=-0.050, segments_x=8, mat_idx=1)
    # Center Cyber Energy Core
    create_cylinder_mesh(bm, radius_bottom=0.038, radius_top=0.042, height=0.018, segments=16, offset=Vector((0.0, -0.128, 1.34)), rot=Euler((math.radians(90), 0, 0)), mat_idx=2)

    # 3. SODE (SHOULDER PAULDRONS)
    for side in [-1, 1]:
        for tier in range(4):
            z_pos = 1.46 - tier * 0.055
            x_pos = side * (0.235 + tier * 0.015)
            y_pos = -0.005 + tier * 0.005
            roll = side * math.radians(-18 + tier * 2)
            create_curved_plate(bm, center=Vector((x_pos, y_pos, z_pos)), width=0.135, height=0.062, thickness=0.012, curvature=0.025, rot=Euler((0, roll, 0)), mat_idx=1)
            # Gold rivets along shoulder plates
            create_cylinder_mesh(bm, radius_bottom=0.006, radius_top=0.006, height=0.008, segments=8, offset=Vector((x_pos, y_pos - 0.012, z_pos)), mat_idx=2)

    # 4. KOTE (FOREARM GAUNTLETS)
    # Oriented along the arm A-pose vector:
    # Left: elbow at (0.346, -0.015, 1.317) -> hand at (0.479, -0.198, 1.200)
    # Right: elbow at (-0.346, -0.015, 1.317) -> hand at (-0.479, -0.198, 1.200)
    for side in [-1, 1]:
        p_elbow = Vector((side * 0.346, -0.015, 1.317))
        p_hand = Vector((side * 0.479, -0.198, 1.200))
        fa_vec = (p_hand - p_elbow).normalized()
        fa_mid = (p_elbow + p_hand) * 0.5
        
        # Gauntlet guard
        create_curved_plate(bm, center=fa_mid + Vector((0, -0.03, 0)), width=0.12, height=0.06, thickness=0.015, mat_idx=1)
        # Gold wrist cuff
        create_cylinder_mesh(bm, radius_bottom=0.038, radius_top=0.038, height=0.030, segments=12, offset=p_hand + Vector((0, 0.04, 0.02)), mat_idx=2)

    # 5. OBI (BELT) & KUSAZURI (TASSETS)
    create_cylinder_mesh(bm, radius_bottom=0.175, radius_top=0.170, height=0.065, segments=24, offset=Vector((0.0, 0.0, 1.05)), mat_idx=1)
    create_curved_plate(bm, center=Vector((0.0, -0.178, 1.05)), width=0.078, height=0.065, thickness=0.016, curvature=0.008, mat_idx=2)

    tasset_angles = [
        (-math.radians(35), Vector((-0.09, -0.14, 0.92))),
        (math.radians(35), Vector((0.09, -0.14, 0.92))),
        (-math.radians(85), Vector((-0.17, -0.02, 0.92))),
        (math.radians(85), Vector((0.17, -0.02, 0.92))),
        (-math.radians(145), Vector((-0.10, 0.13, 0.92))),
        (math.radians(145), Vector((0.10, 0.13, 0.92))),
    ]
    for yaw, pos in tasset_angles:
        for tier in range(3):
            t_pos = pos + Vector((math.sin(yaw) * tier * 0.018, -math.cos(yaw) * tier * 0.018, -tier * 0.060))
            create_curved_plate(bm, center=t_pos, width=0.105, height=0.068, thickness=0.010, curvature=0.020, rot=Euler((math.radians(10), 0, yaw)), mat_idx=1)

    # 6. SUNEATE (GREAVES) & CYBER-TABI BOOTS
    for side in [-1, 1]:
        lx = side * 0.125
        # Knee guard
        create_curved_plate(bm, center=Vector((lx, -0.102, 0.54)), width=0.088, height=0.065, thickness=0.014, curvature=0.030, mat_idx=1)
        # Shin plate
        create_curved_plate(bm, center=Vector((lx, -0.092, 0.32)), width=0.085, height=0.28, thickness=0.014, curvature=0.032, segments_z=4, mat_idx=1)
        # Gold knee crest
        create_curved_plate(bm, center=Vector((lx, -0.112, 0.54)), width=0.035, height=0.035, thickness=0.006, mat_idx=2)
        # Cyber-Tabi boot
        create_curved_plate(bm, center=Vector((lx, -0.065, 0.05)), width=0.092, height=0.075, thickness=0.055, curvature=0.015, mat_idx=1)
        # Sole
        create_curved_plate(bm, center=Vector((lx, -0.045, 0.012)), width=0.096, height=0.18, thickness=0.022, curvature=0.005, rot=Euler((math.radians(90), 0, 0)), mat_idx=1)

    # 7. MASTERWORK KATANA & SAYA (SCABBARD)
    hip_mount = Vector((-0.20, -0.02, 0.98))
    k_rot = Euler((math.radians(18), math.radians(22), math.radians(-15)))
    k_mat = Matrix.Translation(hip_mount) @ k_rot.to_matrix().to_4x4()

    saya_segments = 14
    for i in range(saya_segments):
        t0 = i / float(saya_segments)
        t1 = (i + 1) / float(saya_segments)
        curve0 = -0.035 * (t0 ** 1.3)
        curve1 = -0.035 * (t1 ** 1.3)
        z0 = -t0 * 0.82
        z1 = -t1 * 0.82
        w0, h0 = 0.016, 0.026
        w1, h1 = 0.015, 0.024
        
        v00 = k_mat @ Vector((-w0, curve0 - h0*0.5, z0))
        v01 = k_mat @ Vector(( w0, curve0 - h0*0.5, z0))
        v02 = k_mat @ Vector(( w0, curve0 + h0*0.5, z0))
        v03 = k_mat @ Vector((-w0, curve0 + h0*0.5, z0))
        
        v10 = k_mat @ Vector((-w1, curve1 - h1*0.5, z1))
        v11 = k_mat @ Vector(( w1, curve1 - h1*0.5, z1))
        v12 = k_mat @ Vector(( w1, curve1 + h1*0.5, z1))
        v13 = k_mat @ Vector((-w1, curve1 + h1*0.5, z1))
        
        bm00 = bm.verts.new(v00); bm01 = bm.verts.new(v01); bm02 = bm.verts.new(v02); bm03 = bm.verts.new(v03)
        bm10 = bm.verts.new(v10); bm11 = bm.verts.new(v11); bm12 = bm.verts.new(v12); bm13 = bm.verts.new(v13)
        
        for quad in [[bm00, bm01, bm11, bm10], [bm01, bm02, bm12, bm11], [bm02, bm03, bm13, bm12], [bm03, bm00, bm10, bm13]]:
            f = bm.faces.new(quad)
            f.material_index = 1 # Saya obsidian/crimson

    # Kojiri tip cap
    kojiri_pt = k_mat @ Vector((0.0, -0.035, -0.83))
    create_cylinder_mesh(bm, radius_bottom=0.014, radius_top=0.016, height=0.035, segments=10, offset=kojiri_pt, mat_idx=2)

    # Tsuba handguard
    tsuba_pt = k_mat @ Vector((0.0, 0.0, 0.012))
    create_cylinder_mesh(bm, radius_bottom=0.045, radius_top=0.045, height=0.008, segments=16, offset=tsuba_pt, rot=k_rot, mat_idx=2)

    # Tsuka handle
    tsuka_start = k_mat @ Vector((0.0, 0.0, 0.02))
    tsuka_end = k_mat @ Vector((0.0, 0.0, 0.28))
    create_cylinder_mesh(bm, radius_bottom=0.016, radius_top=0.014, height=0.26, segments=12, offset=(tsuka_start + tsuka_end)*0.5, rot=k_rot, mat_idx=1)

    # Kashira pommel
    kashira_pt = k_mat @ Vector((0.0, 0.0, 0.285))
    create_cylinder_mesh(bm, radius_bottom=0.015, radius_top=0.012, height=0.016, segments=12, offset=kashira_pt, rot=k_rot, mat_idx=2)

    # Habaki collar
    habaki_pt = k_mat @ Vector((0.0, 0.0, -0.012))
    create_cylinder_mesh(bm, radius_bottom=0.018, radius_top=0.018, height=0.022, segments=12, offset=habaki_pt, rot=k_rot, mat_idx=2)


def setup_materials(obj):
    # Material 0: ShogunFace_Mat
    mat_face = bpy.data.materials.new(name="ShogunFace_Mat")
    mat_face.use_nodes = True
    nodes = mat_face.node_tree.nodes
    links = mat_face.node_tree.links
    nodes.clear()
    out0 = nodes.new('ShaderNodeOutputMaterial')
    bsdf0 = nodes.new('ShaderNodeBsdfPrincipled')
    bsdf0.inputs['Metallic'].default_value = 0.02
    bsdf0.inputs['Roughness'].default_value = 0.55
    links.new(bsdf0.outputs['BSDF'], out0.inputs['Surface'])

    face_tex_path = os.path.join(SCRATCH_DIR, "shogun_face_diffuse.png")
    if os.path.exists(face_tex_path):
        img = bpy.data.images.load(face_tex_path)
        tex_node = nodes.new('ShaderNodeTexImage')
        tex_node.image = img
        links.new(tex_node.outputs['Color'], bsdf0.inputs['Base Color'])
    else:
        bsdf0.inputs['Base Color'].default_value = (0.78, 0.62, 0.50, 1.0)
    obj.data.materials.append(mat_face)

    # Material 1: ShogunArmor_Mat
    mat_armor = bpy.data.materials.new(name="ShogunArmor_Mat")
    mat_armor.use_nodes = True
    nodes1 = mat_armor.node_tree.nodes
    links1 = mat_armor.node_tree.links
    nodes1.clear()
    out1 = nodes1.new('ShaderNodeOutputMaterial')
    bsdf1 = nodes1.new('ShaderNodeBsdfPrincipled')
    # Deep Japanese Crimson Samurai Lacquer
    bsdf1.inputs['Base Color'].default_value = (0.48, 0.065, 0.092, 1.0)
    bsdf1.inputs['Metallic'].default_value = 0.78
    bsdf1.inputs['Roughness'].default_value = 0.28
    links1.new(bsdf1.outputs['BSDF'], out1.inputs['Surface'])
    obj.data.materials.append(mat_armor)

    # Material 2: ShogunGold_Mat
    mat_gold = bpy.data.materials.new(name="ShogunGold_Mat")
    mat_gold.use_nodes = True
    nodes2 = mat_gold.node_tree.nodes
    links2 = mat_gold.node_tree.links
    nodes2.clear()
    out2 = nodes2.new('ShaderNodeOutputMaterial')
    bsdf2 = nodes2.new('ShaderNodeBsdfPrincipled')
    # Polished Brass Gold
    bsdf2.inputs['Base Color'].default_value = (0.92, 0.74, 0.08, 1.0)
    bsdf2.inputs['Metallic'].default_value = 0.95
    bsdf2.inputs['Roughness'].default_value = 0.20
    links2.new(bsdf2.outputs['BSDF'], out2.inputs['Surface'])
    obj.data.materials.append(mat_gold)

    # Material 3: ShogunBlade_Mat
    mat_blade = bpy.data.materials.new(name="ShogunBlade_Mat")
    mat_blade.use_nodes = True
    nodes3 = mat_blade.node_tree.nodes
    links3 = mat_blade.node_tree.links
    nodes3.clear()
    out3 = nodes3.new('ShaderNodeOutputMaterial')
    bsdf3 = nodes3.new('ShaderNodeBsdfPrincipled')
    # Polished Steel with Cyan Plasma Glow
    bsdf3.inputs['Base Color'].default_value = (0.90, 0.92, 0.95, 1.0)
    bsdf3.inputs['Metallic'].default_value = 0.98
    bsdf3.inputs['Roughness'].default_value = 0.12
    bsdf3.inputs['Emission Color'].default_value = (0.0, 0.85, 1.0, 1.0)
    bsdf3.inputs['Emission Strength'].default_value = 1.5
    links3.new(bsdf3.outputs['BSDF'], out3.inputs['Surface'])
    obj.data.materials.append(mat_blade)


def build_armature():
    arm_data = bpy.data.armatures.new('Armature')
    arm_obj = bpy.data.objects.new('Armature', arm_data)
    bpy.context.collection.objects.link(arm_obj)
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode='EDIT')

    def make_bone(name, head, tail, parent=None):
        b = arm_data.edit_bones.new(name)
        b.head = head
        b.tail = tail
        if parent:
            b.parent = parent
        return b

    hips = make_bone('Hips', Vector((0, 0, 1.00)), Vector((0, 0, 1.15)))
    spine = make_bone('Spine', Vector((0, 0, 1.15)), Vector((0, 0, 1.35)), hips)
    spine01 = make_bone('Spine01', Vector((0, 0, 1.35)), Vector((0, 0, 1.58)), spine)
    neck = make_bone('neck', Vector((0, -0.01, 1.58)), Vector((0, -0.01, 1.70)), spine01)
    head = make_bone('Head', Vector((0, -0.02, 1.70)), Vector((0, -0.02, 1.88)), neck)

    l_sh = make_bone('LeftShoulder', Vector((0.05, 0, 1.52)), Vector((0.18, -0.02, 1.51)), spine01)
    l_arm = make_bone('LeftArm', Vector((0.18, -0.02, 1.51)), Vector((0.35, -0.015, 1.32)), l_sh)
    l_fa = make_bone('LeftForeArm', Vector((0.35, -0.015, 1.32)), Vector((0.48, -0.20, 1.20)), l_arm)
    l_hand = make_bone('LeftHand', Vector((0.48, -0.20, 1.20)), Vector((0.52, -0.30, 1.12)), l_fa)

    r_sh = make_bone('RightShoulder', Vector((-0.05, 0, 1.52)), Vector((-0.18, -0.02, 1.51)), spine01)
    r_arm = make_bone('RightArm', Vector((-0.18, -0.02, 1.51)), Vector((-0.35, -0.015, 1.32)), r_sh)
    r_fa = make_bone('RightForeArm', Vector((-0.35, -0.015, 1.32)), Vector((-0.48, -0.20, 1.20)), r_arm)
    r_hand = make_bone('RightHand', Vector((-0.48, -0.20, 1.20)), Vector((-0.52, -0.30, 1.12)), r_fa)

    l_uleg = make_bone('LeftUpLeg', Vector((0.12, -0.02, 0.98)), Vector((0.175, -0.04, 0.50)), hips)
    l_leg = make_bone('LeftLeg', Vector((0.175, -0.04, 0.50)), Vector((0.244, -0.003, 0.08)), l_uleg)
    l_foot = make_bone('LeftFoot', Vector((0.244, -0.003, 0.08)), Vector((0.244, -0.18, 0.01)), l_leg)

    r_uleg = make_bone('RightUpLeg', Vector((-0.12, -0.02, 0.98)), Vector((-0.175, -0.04, 0.50)), hips)
    r_leg = make_bone('RightLeg', Vector((-0.175, -0.04, 0.50)), Vector((-0.244, -0.003, 0.08)), r_uleg)
    r_foot = make_bone('RightFoot', Vector((-0.244, -0.003, 0.08)), Vector((-0.244, -0.18, 0.01)), r_leg)

    bpy.ops.object.mode_set(mode='OBJECT')
    return arm_obj


def create_action(arm, name, duration_frames):
    action = bpy.data.actions.new(name)
    if not arm.animation_data:
        arm.animation_data_create()
    arm.animation_data.action = action
    track = arm.animation_data.nla_tracks.new()
    track.name = name
    strip = track.strips.new(name, 1, action)
    strip.action_frame_end = duration_frames
    if hasattr(strip, "action_slot") and hasattr(action, "slots") and len(action.slots) > 0:
        strip.action_slot = action.slots[0]
    return action


def key_bone(arm, bone_name, frame, loc=None, rot_euler=None):
    b = arm.pose.bones.get(bone_name)
    if not b:
        return
    b.rotation_mode = 'QUATERNION'
    if loc is not None:
        b.location = loc
        b.keyframe_insert('location', frame=frame, group=bone_name)
    if rot_euler is not None:
        q = rot_euler.to_quaternion()
        b.rotation_quaternion = q
        b.keyframe_insert('rotation_quaternion', frame=frame, group=bone_name)


def author_animations(arm):
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='POSE')

    # 1. Standing / Idle (24-frame heroic breathing cycle)
    for act_name in ['Standing', 'Idle']:
        create_action(arm, act_name, 24)
        for f, breath, bob in [(1, 0.0, 0.0), (12, 0.04, 0.015), (24, 0.0, 0.0)]:
            key_bone(arm, 'Hips', f, loc=Vector((0, 0, bob)), rot_euler=Euler((0, 0, 0)))
            key_bone(arm, 'Spine01', f, rot_euler=Euler((math.radians(breath * 10), 0, 0)))
            key_bone(arm, 'Head', f, rot_euler=Euler((math.radians(-breath * 5), 0, 0)))
            key_bone(arm, 'LeftArm', f, rot_euler=Euler((math.radians(5), 0, math.radians(-10))))
            key_bone(arm, 'RightArm', f, rot_euler=Euler((math.radians(5), 0, math.radians(10))))

    # 2. Running (24-frame cyclic sprint)
    create_action(arm, 'Running', 24)
    run_keys = [
        (1,   math.radians(35),  -math.radians(30), -math.radians(30),  math.radians(30), 0.02),
        (7,   math.radians(10),  -math.radians(5),  -math.radians(10),  math.radians(10), 0.05),
        (13, -math.radians(30),   math.radians(35),  math.radians(30), -math.radians(30), 0.02),
        (19, -math.radians(10),   math.radians(10),  math.radians(10), -math.radians(10), 0.05),
        (24,  math.radians(35),  -math.radians(30), -math.radians(30),  math.radians(30), 0.02)
    ]
    for f, l_leg_r, r_leg_r, l_arm_r, r_arm_r, bob in run_keys:
        key_bone(arm, 'Hips', f, loc=Vector((0, 0, bob)), rot_euler=Euler((math.radians(12), 0, 0)))
        key_bone(arm, 'LeftUpLeg', f, rot_euler=Euler((l_leg_r, 0, 0)))
        key_bone(arm, 'RightUpLeg', f, rot_euler=Euler((r_leg_r, 0, 0)))
        key_bone(arm, 'LeftArm', f, rot_euler=Euler((l_arm_r, 0, math.radians(-15))))
        key_bone(arm, 'RightArm', f, rot_euler=Euler((r_arm_r, 0, math.radians(15))))

    # 3. Walking (24-frame cycle)
    create_action(arm, 'Walking', 24)
    walk_keys = [
        (1,   math.radians(20),  -math.radians(18), -math.radians(18),  math.radians(20), 0.01),
        (13, -math.radians(18),   math.radians(20),  math.radians(20), -math.radians(18), 0.01),
        (24,  math.radians(20),  -math.radians(18), -math.radians(18),  math.radians(20), 0.01)
    ]
    for f, l_leg_r, r_leg_r, l_arm_r, r_arm_r, bob in walk_keys:
        key_bone(arm, 'Hips', f, loc=Vector((0, 0, bob)), rot_euler=Euler((math.radians(4), 0, 0)))
        key_bone(arm, 'LeftUpLeg', f, rot_euler=Euler((l_leg_r, 0, 0)))
        key_bone(arm, 'RightUpLeg', f, rot_euler=Euler((r_leg_r, 0, 0)))
        key_bone(arm, 'LeftArm', f, rot_euler=Euler((l_arm_r, 0, math.radians(-10))))
        key_bone(arm, 'RightArm', f, rot_euler=Euler((r_arm_r, 0, math.radians(10))))

    # 4. Blade_Dash (Ability 0 - 18 frames)
    create_action(arm, 'Blade_Dash', 18)
    key_bone(arm, 'Hips', 1, loc=Vector((0, 0, 0)), rot_euler=Euler((0, 0, 0)))
    key_bone(arm, 'Hips', 6, loc=Vector((0, -0.15, -0.10)), rot_euler=Euler((math.radians(25), 0, math.radians(-15))))
    key_bone(arm, 'Hips', 12, loc=Vector((0, -0.45, -0.05)), rot_euler=Euler((math.radians(35), 0, math.radians(25))))
    key_bone(arm, 'Hips', 18, loc=Vector((0, 0, 0)), rot_euler=Euler((0, 0, 0)))
    key_bone(arm, 'RightArm', 1, rot_euler=Euler((0, 0, 0)))
    key_bone(arm, 'RightArm', 6, rot_euler=Euler((math.radians(-40), math.radians(-20), math.radians(45))))
    key_bone(arm, 'RightArm', 12, rot_euler=Euler((math.radians(70), math.radians(45), math.radians(-60)))) # dynamic slash!
    key_bone(arm, 'RightArm', 18, rot_euler=Euler((0, 0, 0)))

    # 5. Parry_Stance (Ability 1 - 20 frames)
    create_action(arm, 'Parry_Stance', 20)
    key_bone(arm, 'Hips', 1, loc=Vector((0, 0, 0)), rot_euler=Euler((0, 0, 0)))
    key_bone(arm, 'Hips', 10, loc=Vector((0, 0.05, -0.08)), rot_euler=Euler((math.radians(-10), 0, math.radians(15))))
    key_bone(arm, 'Hips', 20, loc=Vector((0, 0, 0)), rot_euler=Euler((0, 0, 0)))
    key_bone(arm, 'LeftArm', 10, rot_euler=Euler((math.radians(45), math.radians(20), math.radians(30))))
    key_bone(arm, 'RightArm', 10, rot_euler=Euler((math.radians(55), math.radians(-30), math.radians(-40))))

    # 6. Dash (Ability 2 - 16 frames)
    create_action(arm, 'Dash', 16)
    key_bone(arm, 'Hips', 1, loc=Vector((0, 0, 0)), rot_euler=Euler((0, 0, 0)))
    key_bone(arm, 'Hips', 8, loc=Vector((0, -0.30, -0.08)), rot_euler=Euler((math.radians(30), 0, 0)))
    key_bone(arm, 'Hips', 16, loc=Vector((0, 0, 0)), rot_euler=Euler((0, 0, 0)))

    # 7. Ground_Slam (Ability 3 - 24 frames)
    create_action(arm, 'Ground_Slam', 24)
    key_bone(arm, 'Hips', 1, loc=Vector((0, 0, 0)), rot_euler=Euler((0, 0, 0)))
    key_bone(arm, 'Hips', 8, loc=Vector((0, 0, 0.40)), rot_euler=Euler((math.radians(-15), 0, 0)))
    key_bone(arm, 'Hips', 16, loc=Vector((0, -0.10, -0.25)), rot_euler=Euler((math.radians(45), 0, 0)))
    key_bone(arm, 'Hips', 24, loc=Vector((0, 0, 0)), rot_euler=Euler((0, 0, 0)))

    # 8. Thrust_Slash (20 frames)
    create_action(arm, 'Thrust_Slash', 20)
    key_bone(arm, 'RightArm', 1, rot_euler=Euler((0, 0, 0)))
    key_bone(arm, 'RightArm', 8, rot_euler=Euler((math.radians(85), 0, math.radians(10)))) # Thrust
    key_bone(arm, 'RightArm', 14, rot_euler=Euler((math.radians(30), math.radians(60), math.radians(-50)))) # Slash
    key_bone(arm, 'RightArm', 20, rot_euler=Euler((0, 0, 0)))

    # 9. Hit (12 frames)
    create_action(arm, 'Hit', 12)
    key_bone(arm, 'Hips', 1, loc=Vector((0, 0, 0)), rot_euler=Euler((0, 0, 0)))
    key_bone(arm, 'Hips', 5, loc=Vector((0, 0.08, 0)), rot_euler=Euler((math.radians(-18), 0, 0)))
    key_bone(arm, 'Hips', 12, loc=Vector((0, 0, 0)), rot_euler=Euler((0, 0, 0)))

    # 10. Death (30 frames)
    create_action(arm, 'Death', 30)
    key_bone(arm, 'Hips', 1, loc=Vector((0, 0, 0)), rot_euler=Euler((0, 0, 0)))
    key_bone(arm, 'Hips', 15, loc=Vector((0, 0.15, -0.40)), rot_euler=Euler((math.radians(-45), 0, 0)))
    for track in arm.animation_data.nla_tracks:
        for strip in track.strips:
            if strip.action and hasattr(strip.action, 'slots') and len(strip.action.slots) > 0:
                strip.action_slot = strip.action.slots[0]

    bpy.ops.object.mode_set(mode='OBJECT')


def render_previews(arm, mesh_obj):
    # Ensure standing pose is active for preview render
    stand_act = bpy.data.actions.get('Standing')
    if stand_act and arm.animation_data:
        arm.animation_data.action = stand_act

    cam_data = bpy.data.cameras.new('RenderCam')
    cam_obj = bpy.data.objects.new('RenderCam', cam_data)
    bpy.context.collection.objects.link(cam_obj)
    bpy.context.scene.camera = cam_obj

    # Key light
    key_l = bpy.data.lights.new('KeyLight', type='SUN')
    key_l.energy = 4.2
    key_obj = bpy.data.objects.new('KeyLight', key_l)
    bpy.context.collection.objects.link(key_obj)
    key_obj.rotation_euler = (math.radians(48), math.radians(24), math.radians(-18))

    # Rim light
    rim_l = bpy.data.lights.new('RimLight', type='SUN')
    rim_l.energy = 2.8
    rim_obj = bpy.data.objects.new('RimLight', rim_l)
    bpy.context.collection.objects.link(rim_obj)
    rim_obj.rotation_euler = (math.radians(-42), math.radians(150), 0)

    # Ambient fill
    fill_l = bpy.data.lights.new('FillLight', type='SUN')
    fill_l.energy = 1.2
    fill_obj = bpy.data.objects.new('FillLight', fill_l)
    bpy.context.collection.objects.link(fill_obj)
    fill_obj.rotation_euler = (math.radians(20), math.radians(-60), 0)

    # 1. Render Portrait
    cam_obj.location = (0.0, -0.75, 1.68)
    cam_obj.rotation_euler = (math.radians(88), 0, 0)
    bpy.context.scene.render.resolution_x = 768
    bpy.context.scene.render.resolution_y = 768
    bpy.context.scene.render.filepath = RENDER_PORTRAIT
    bpy.ops.render.render(write_still=True)
    print("Rendered portrait:", RENDER_PORTRAIT)

    # 2. Render Full Body
    cam_obj.location = (0.0, -2.65, 0.98)
    cam_obj.rotation_euler = (math.radians(90), 0, 0)
    bpy.context.scene.render.resolution_x = 768
    bpy.context.scene.render.resolution_y = 1024
    bpy.context.scene.render.filepath = RENDER_FULLBODY
    bpy.ops.render.render(write_still=True)
    print("Rendered full body:", RENDER_FULLBODY)


def dist_to_segment(p, a, b):
    ab = b - a
    l2 = ab.length_squared
    if l2 < 1e-6:
        return (p - a).length
    t = max(0.0, min(1.0, (p - a).dot(ab) / l2))
    proj = a + ab * t
    return (p - proj).length


def skin_mesh_to_armature(mesh_obj, arm_obj):
    mesh_obj.parent = arm_obj
    arm_mod = mesh_obj.modifiers.new(name="Armature", type='ARMATURE')
    arm_mod.object = arm_obj

    vgs = {b.name: mesh_obj.vertex_groups.new(name=b.name) for b in arm_obj.data.bones}
    bone_segs = {b.name: (arm_obj.matrix_world @ b.head_local, arm_obj.matrix_world @ b.tail_local) for b in arm_obj.data.bones}

    for v in mesh_obj.data.vertices:
        co = mesh_obj.matrix_world @ v.co
        dists = [(b_name, dist_to_segment(co, h, t)) for b_name, (h, t) in bone_segs.items()]
        dists.sort(key=lambda x: x[1])
        top_bones = dists[:3]
        weights = [(b_name, 1.0 / (max(0.02, d) ** 2)) for b_name, d in top_bones]
        total_w = sum(w for _, w in weights)
        for b_name, w in weights:
            vgs[b_name].add([v.index], w / total_w, 'REPLACE')
    print("Skinned", len(mesh_obj.data.vertices), "vertices across", len(vgs), "bones.")


def main():
    print("=================================================================")
    print("BUILD CYBER SHOGUN - REDESIGN FROM SCRATCH (ZERO LOCAL REFS)")
    print("=================================================================")
    bpy.ops.wm.read_factory_settings(use_empty=True)

    # 1. Build human body (Asian male morphs applied, clean quads)
    body_obj, scale, min_y = build_asian_male_body()

    # 2. Add procedural cyber-samurai armor & katana directly into the body mesh
    bm = bmesh.new()
    bm.from_mesh(body_obj.data)
    build_cyber_armor_and_katana(bm)
    bm.to_mesh(body_obj.data)
    bm.free()
    body_obj.data.update()

    # 3. Setup PBR Materials
    setup_materials(body_obj)

    # 4. Smooth shading
    bpy.context.view_layer.objects.active = body_obj
    bpy.ops.object.shade_smooth()

    # 5. Build Armature
    arm_obj = build_armature()

    # 6. Parent and Skin with deterministic smooth proximity weights
    skin_mesh_to_armature(body_obj, arm_obj)

    # 7. Author animations
    author_animations(arm_obj)

    # 8. Render preview renders
    render_previews(arm_obj, body_obj)

    # 9. Save .blend file
    os.makedirs(os.path.dirname(OUT_BLEND), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)
    print("Saved Blend file:", OUT_BLEND)

    # 10. Export GLB
    os.makedirs(os.path.dirname(OUT_GLB), exist_ok=True)
    bpy.ops.export_scene.gltf(
        filepath=OUT_GLB,
        export_format='GLB',
        use_selection=False,
        export_apply=False,
        export_animations=True,
        export_nla_strips=True
    )
    print("Exported GLB:", OUT_GLB)

    # 11. Copy to web public
    if os.path.exists(os.path.dirname(OUT_WEB_PUBLIC)):
        shutil.copyfile(OUT_GLB, OUT_WEB_PUBLIC)
        print("Copied to Web Public:", OUT_WEB_PUBLIC)
    if os.path.exists(os.path.dirname(OUT_WEB_DIST)):
        shutil.copyfile(OUT_GLB, OUT_WEB_DIST)
        print("Copied to Web Dist:", OUT_WEB_DIST)

    print("=================================================================")
    print("BUILD COMPLETE SUCCESSFULLY!")
    print("=================================================================")

if __name__ == "__main__":
    main()
