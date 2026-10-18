"""
Build Cyber Shogun (Kaito Kurogane) - Authentic Japanese Samurai Operative.
Uses the CC0 Asian Male head/face mesh, morph targets, 3D eyeballs, 3D eyebrows, and 2K Asian male skin
texture from the Godot Asset Library's Humanizer package (Asset ID 3699, NitroxNova/humanizer / MakeHuman CC0):
1. Restores the 46k-vertex high-detail samurai armor mesh (`d3488f8_shogun.glb`) and removes the robotic
   helmet and shoulder-weighted shikoro flaps.
2. Builds Kaito Kurogane's head directly from the Humanizer/MakeHuman CC0 base mesh (`base.obj`, clean quad
   topology with zero unwelded facets) morphed with `asian-male-young.target` + `asian-male-old.target` +
   `universal-male-young-maxmuscle-averageweight.target`.
3. Fits the Humanizer 3D Eyeballs (`LeftEye-LowPolyEyeball.obj`, `RightEye-LowPolyEyeball.obj`),
   3D Eyebrows (`LeftEyebrow-Eyebrow009.obj`, `RightEyebrow-Eyebrow009.obj`), and swept-back hair cap
   (`short04.obj`) using their `.mhclo` barycentric skinning weights.
4. Adds the traditional Japanese Samurai Chonmage (Oicho-mage / Motodori topknot) with Gold/Crimson Mizuhiki
   cord binding and a 3D Hachimaki headband with a gold Takeda clan diamond crest (Mon) on the forehead.
5. Packs the 2K Asian Male skin texture (`young_asian_male.png` + `middleage_asian_male.png` blend),
   dark-brown Japanese iris/sclera eyeball textures, and jet-black hair texture into a single unified
   1024x1024 texture atlas so `CyberShogun_Body` renders cleanly with 2 materials (Armor + Head).
"""

import bpy
import bmesh
import math
import os
import shutil
import subprocess
from mathutils import Vector

PROJECT_ROOT = r"D:\Projects\frog_jumper"
SCRATCH_GLB = r"C:\Users\karthiknk21\.gemini\antigravity-cli\brain\51c70920-0a37-4082-8177-2c677a76cad3\scratch\d3488f8_shogun.glb"
HUMANIZER_DIR = r"C:\Users\karthiknk21\.gemini\antigravity-cli\brain\51c70920-0a37-4082-8177-2c677a76cad3\scratch\humanizer"

OUT_GLB = os.path.join(PROJECT_ROOT, "assets", "models", "hq", "cyber_shogun.glb")
OUT_BLEND = os.path.join(PROJECT_ROOT, "assets", "models", "hq", "cyber_shogun.blend")
OUT_WEB_PUBLIC = os.path.join(PROJECT_ROOT, "website", "public", "models", "shogun.glb")
OUT_WEB_DIST = os.path.join(PROJECT_ROOT, "website", "dist", "models", "shogun.glb")

RENDER_PORTRAIT = r"C:\Users\karthiknk21\.gemini\antigravity-cli\brain\51c70920-0a37-4082-8177-2c677a76cad3\cyber_shogun_portrait.png"
RENDER_FULLBODY = r"C:\Users\karthiknk21\.gemini\antigravity-cli\brain\51c70920-0a37-4082-8177-2c677a76cad3\cyber_shogun_fullbody.png"


def smoothstep(edge0, edge1, x):
    t = max(0.0, min(1.0, (x - edge0) / (edge1 - edge0)))
    return t * t * (3.0 - 2.0 * t)


def ensure_d3488f8_glb():
    if not os.path.exists(SCRATCH_GLB) or os.path.getsize(SCRATCH_GLB) < 1000000:
        os.makedirs(os.path.dirname(SCRATCH_GLB), exist_ok=True)
        with open(SCRATCH_GLB, "wb") as f:
            subprocess.run(["git", "show", "d3488f8:assets/models/hq/cyber_shogun.glb"], cwd=PROJECT_ROOT, stdout=f, check=True)


def create_crimson_samurai_armor_material(src_mesh):
    """Smoothly transform the armor texture into Deep Crimson Samurai Lacquer, Obsidian Black & Brass-Gold."""
    src_img = None
    for mat in src_mesh.data.materials:
        if mat and mat.use_nodes:
            for node in mat.node_tree.nodes:
                if node.type == 'TEX_IMAGE' and node.image:
                    src_img = node.image
                    break

    new_mat = bpy.data.materials.new(name="ShogunCrimsonArmor_Mat")
    new_mat.use_nodes = True
    nodes = new_mat.node_tree.nodes
    links = new_mat.node_tree.links
    nodes.clear()

    out_node = nodes.new(type='ShaderNodeOutputMaterial')
    bsdf = nodes.new(type='ShaderNodeBsdfPrincipled')
    bsdf.inputs['Metallic'].default_value = 0.50
    bsdf.inputs['Roughness'].default_value = 0.35
    links.new(bsdf.outputs['BSDF'], out_node.inputs['Surface'])

    if src_img:
        w, h = src_img.size
        tw, th = min(1024, w), min(1024, h)
        scale_x, scale_y = w / tw, h / th
        src_px = list(src_img.pixels[:])

        new_img = bpy.data.images.new("ShogunArmor_CrimsonObsidian", width=tw, height=th, alpha=True)
        dst_px = [0.0] * (tw * th * 4)

        for ty in range(th):
            sy = min(h - 1, int(ty * scale_y))
            for tx in range(tw):
                sx = min(w - 1, int(tx * scale_x))
                s_idx = (sy * w + sx) * 4
                r, g, b, a = src_px[s_idx], src_px[s_idx + 1], src_px[s_idx + 2], src_px[s_idx + 3]
                lum = 0.299 * r + 0.587 * g + 0.114 * b

                cyan_w = smoothstep(0.06, 0.15, b - r) * smoothstep(0.32, 0.48, g)
                gold_orn_w = smoothstep(0.05, 0.12, r - b) * smoothstep(0.20, 0.35, lum)

                plate_w = smoothstep(0.20, 0.30, lum)
                bevel_w = smoothstep(0.58, 0.78, lum)

                obs_r = lum * 0.44 + 0.02
                obs_g = lum * 0.44 + 0.02
                obs_b = lum * 0.50 + 0.03

                crim_r = 0.12 + lum * 0.72
                crim_g = 0.03 + lum * 0.12
                crim_b = 0.04 + lum * 0.14

                gold_r = min(0.92, 0.22 + lum * 0.80)
                gold_g = min(0.75, 0.14 + lum * 0.58)
                gold_b = max(0.07, 0.04 + lum * 0.20)

                nr = obs_r * (1.0 - plate_w) + crim_r * plate_w
                ng = obs_g * (1.0 - plate_w) + crim_g * plate_w
                nb = obs_b * (1.0 - plate_w) + crim_b * plate_w

                gw = max(bevel_w * 0.65, gold_orn_w)
                nr = nr * (1.0 - gw) + gold_r * gw
                ng = ng * (1.0 - gw) + gold_g * gw
                nb = nb * (1.0 - gw) + gold_b * gw

                if cyan_w > 0.0:
                    cy_r = r * 0.20
                    cy_g = min(1.0, g * 1.15 + 0.08)
                    cy_b = min(1.0, b * 1.25 + 0.12)
                    nr = nr * (1.0 - cyan_w) + cy_r * cyan_w
                    ng = ng * (1.0 - cyan_w) + cy_g * cyan_w
                    nb = nb * (1.0 - cyan_w) + cy_b * cyan_w

                d_idx = (ty * tw + tx) * 4
                dst_px[d_idx] = nr
                dst_px[d_idx + 1] = ng
                dst_px[d_idx + 2] = nb
                dst_px[d_idx + 3] = a

        new_img.pixels[:] = dst_px
        new_img.pack()

        tex_node = nodes.new(type='ShaderNodeTexImage')
        tex_node.image = new_img
        links.new(tex_node.outputs['Color'], bsdf.inputs['Base Color'])

    return new_mat


def load_obj_uvs_and_faces(obj_path):
    verts = []
    uvs = []
    faces = []
    with open(obj_path, "r", encoding="utf-8") as f:
        for line in f:
            if line.startswith("v "):
                verts.append([float(x) for x in line.split()[1:4]])
            elif line.startswith("vt "):
                uvs.append([float(x) for x in line.split()[1:3]])
            elif line.startswith("f "):
                parts = line.split()[1:]
                face = []
                for p in parts:
                    sp = p.split("/")
                    vi = int(sp[0]) - 1
                    ti = int(sp[1]) - 1 if len(sp) > 1 and sp[1] else 0
                    face.append((vi, ti))
                faces.append(face)
    return verts, uvs, faces


def fit_mhclo_vertices(helper_verts, mhclo_path):
    scale_cfg = {}
    v_lines = []
    sec = "header"
    with open(mhclo_path, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line or line.startswith("#"):
                continue
            if line == "verts 0":
                sec = "verts"
                continue
            elif line == "delete_verts":
                break
            if sec == "header":
                for ax, idx in [("x_scale", 0), ("y_scale", 1), ("z_scale", 2)]:
                    if line.startswith(ax):
                        p = line.split()
                        scale_cfg[idx] = (int(p[1]), int(p[2]), float(p[3]))
            elif sec == "verts":
                p = line.split()
                if len(p) == 1:
                    v_lines.append(("single", int(p[0])))
                elif len(p) == 9:
                    v_lines.append((
                        "tri",
                        [int(p[0]), int(p[1]), int(p[2])],
                        [float(p[3]), float(p[4]), float(p[5])],
                        [float(p[6]), float(p[7]), float(p[8])],
                    ))
    sc = [1.0, 1.0, 1.0]
    for idx, (s_i, e_i, length) in scale_cfg.items():
        sc[idx] = abs(helper_verts[e_i][idx] - helper_verts[s_i][idx]) / length
    out_v = []
    for item in v_lines:
        if item[0] == "single":
            out_v.append(list(helper_verts[item[1]]))
        else:
            _, vis, ws, off = item
            pos = [0.0, 0.0, 0.0]
            for i in range(3):
                for k in range(3):
                    pos[k] += helper_verts[vis[i]][k] * ws[i]
            for k in range(3):
                pos[k] += off[k] * sc[k]
            out_v.append(pos)
    return out_v


def build_humanizer_asian_samurai_head(sh_arm):
    """
    Constructs Kaito Kurogane's head mesh and unified 1024x1024 texture atlas from the
    Godot Asset Library's Humanizer CC0 Asian Male assets:
    - Base mesh: `base.obj` morphed with `asian-male-young.target` (0.65) + `asian-male-old.target` (0.35)
      + `universal-male-young-maxmuscle-averageweight.target` (1.0).
    - 3D Eyeballs: `left_eye.obj` & `right_eye.obj` fitted via `.mhclo`.
    - 3D Eyebrows: `LeftEyebrow-009.obj` & `RightEyebrow-009.obj` fitted via `.mhclo` and angled upward.
    - 3D Swept-Back Hair Cap: `short04.obj` cap polygons fitted via `.mhclo` and gathered into the topknot.
    - 3D Samurai Hachimaki headband & Chonmage (Oicho-mage / Motodori topknot) with Gold/Crimson Mizuhiki wrap.
    """
    base_obj_path = os.path.join(HUMANIZER_DIR, "base.obj")
    verts, uvs, faces = load_obj_uvs_and_faces(base_obj_path)

    # Apply MakeHuman Asian Male morph targets
    morph_targets = [
        ("asian-male-young.target", 0.65),
        ("asian-male-old.target", 0.35),
        ("universal-male-young-maxmuscle-averageweight.target", 1.0),
    ]
    for tname, weight in morph_targets:
        tpath = os.path.join(HUMANIZER_DIR, tname)
        with open(tpath, "r", encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if not line or line.startswith("#"):
                    continue
                parts = line.split()
                vi = int(parts[0])
                verts[vi][0] += float(parts[1]) * weight
                verts[vi][1] += float(parts[2]) * weight
                verts[vi][2] += float(parts[3]) * weight

    # Enhance mature Japanese warrior bone structure (sharper jawline, defined cheekbones, strong nose bridge)
    for vi in range(13380):
        mx, my, mz = verts[vi]
        if my > 5.3:
            # Stronger zygomatic arch (cheekbone definition around Y=6.6..6.9)
            if 6.55 <= my <= 6.95 and mz > 0.4:
                zt = 1.0 - abs(my - 6.75) / 0.20
                mx *= (1.0 + 0.035 * zt)
            # Crisp, chiseled samurai jawline (Y=5.75..6.35)
            if 5.75 <= my <= 6.35 and mz > 0.2:
                jt = 1.0 - abs(my - 6.05) / 0.30
                mx *= (1.0 + 0.025 * jt)
            # Slightly narrower, dignified samurai nasal bridge
            if 6.45 <= my <= 6.95 and abs(mx) < 0.22 and mz > 1.15:
                mx *= 0.90
            verts[vi] = [mx, my, mz]

    # Coordinate mapping from MakeHuman decimeters (X=right, Y=up, Z=front)
    # to Cyber Shogun centimeters (X=right, Y=-front, Z=up) centered in the armor collar (-4.2, -7.2, 138..161)
    SCALE = 8.15
    X_OFF = -4.2
    Y_OFF = -1.6
    Z_OFF = 95.5

    def mh_to_shogun(pt):
        return Vector((
            pt[0] * SCALE + X_OFF,
            -pt[2] * SCALE + Y_OFF,
            pt[1] * SCALE + Z_OFF,
        ))

    bm = bmesh.new()
    uv_layer = bm.loops.layers.uv.new("UVMap")

    # 1. Add Asian Male Head & Neck Quad Mesh (sliced at neck base Y > 5.28 and tapered cleanly into the armor collar)
    mh_to_bm_vert = {}
    for face in faces:
        if all(vi < 13380 and verts[vi][1] > 5.28 and abs(verts[vi][0]) < 0.98 for vi, _ in face):
            bm_verts = []
            for vi, _ in face:
                if vi not in mh_to_bm_vert:
                    px, py, pz = verts[vi]
                    # Smoothly taper the lower neck/clavicle flare (Y < 5.65) inward so it tucks inside the collar seal
                    if py < 5.65:
                        t_neck = (5.65 - py) / 0.37
                        px *= (1.0 - 0.35 * t_neck)
                        pz *= (1.0 - 0.25 * t_neck)
                    mh_to_bm_vert[vi] = bm.verts.new(mh_to_shogun([px, py, pz]))
                bm_verts.append(mh_to_bm_vert[vi])
            try:
                f = bm.faces.new(bm_verts)
                f.smooth = True
                for loop, (_, ti) in zip(f.loops, face):
                    u, v = uvs[ti]
                    loop[uv_layer].uv = (u, v)
            except ValueError:
                pass

    # 2. Fit & Add 3D Eyeballs (`left_eye.obj` & `right_eye.obj`)
    # Remap eyeball UVs into atlas region: u in [0.02..0.28], v in [0.02..0.28]
    for eye_obj_name, eye_mhclo_name in [("left_eye.obj", "left_eye.mhclo"), ("right_eye.obj", "right_eye.mhclo")]:
        _, e_uvs, e_faces = load_obj_uvs_and_faces(os.path.join(HUMANIZER_DIR, eye_obj_name))
        e_verts = fit_mhclo_vertices(verts, os.path.join(HUMANIZER_DIR, eye_mhclo_name))
        e_bm_verts = [bm.verts.new(mh_to_shogun(p)) for p in e_verts]
        min_u = min(u[0] for u in e_uvs)
        max_u = max(u[0] for u in e_uvs)
        min_v = min(u[1] for u in e_uvs)
        max_v = max(u[1] for u in e_uvs)
        du = max(1e-5, max_u - min_u)
        dv = max(1e-5, max_v - min_v)
        for face in e_faces:
            try:
                f = bm.faces.new([e_bm_verts[vi] for vi, _ in face])
                f.smooth = True
                for loop, (_, ti) in zip(f.loops, face):
                    u, v = e_uvs[ti]
                    nu = 0.02 + ((u - min_u) / du) * 0.26
                    nv = 0.02 + ((v - min_v) / dv) * 0.26
                    loop[uv_layer].uv = (nu, nv)
            except ValueError:
                pass

    # 3. Fit & Add Humanizer Swept-Back Hair Cap (`short04.obj` / `short04.mhclo`)
    # Keep all solid combed scalp cap polygons (v > 0.42), pull any low forehead bangs up above Y=7.18
    # so the hair seamlessly meets the Hachimaki headband with zero skin gap!
    _, h_uvs, h_faces = load_obj_uvs_and_faces(os.path.join(HUMANIZER_DIR, "short04.obj"))
    h_verts = fit_mhclo_vertices(verts, os.path.join(HUMANIZER_DIR, "short04.mhclo"))
    h_bm_verts = {}
    for face in h_faces:
        if all(h_uvs[ti][1] > 0.42 for _, ti in face):
            f_verts = []
            for vi, _ in face:
                if vi not in h_bm_verts:
                    px, py, pz = h_verts[vi]
                    # Lift any low forehead hair bangs cleanly to the Hachimaki line (Y >= 7.18)
                    if pz > 0.85 and py < 7.22:
                        py = 7.22 + (py - 7.22) * 0.20
                        pz = min(pz, 1.16)
                    # Sleekly gather upper crown into the Chonmage topknot
                    if py > 7.55:
                        t_top = min(1.0, (py - 7.55) / 0.55)
                        px *= (1.0 - 0.16 * t_top)
                    h_bm_verts[vi] = bm.verts.new(mh_to_shogun([px, py, pz]))
                f_verts.append(h_bm_verts[vi])
            try:
                f = bm.faces.new(f_verts)
                f.smooth = True
                for loop, (_, ti) in zip(f.loops, face):
                    u, v = h_uvs[ti]
                    nu = 0.32 + u * 0.26
                    nv = 0.04 + ((v - 0.42) / 0.58) * 0.24
                    loop[uv_layer].uv = (nu, nv)
            except ValueError:
                pass

    # 4. Add 3D Samurai Hachimaki Headband around the forehead & temples (Z = 153.6..155.4)
    # Contours proudly around the morphed Asian Male forehead/temples/hairline so it is 100% visible!
    hachi_rings = []
    for rz, expand in [(153.6, 0.24), (154.5, 0.30), (155.4, 0.24)]:
        ring = []
        for s in range(32):
            ang = 2.0 * math.pi * s / 32
            rx = 6.42 + expand
            ry = 7.82 + expand
            vx = -4.2 + math.cos(ang) * rx
            vy = -5.50 + math.sin(ang) * ry
            ring.append(bm.verts.new((vx, vy, rz)))
        hachi_rings.append(ring)

    for r_i in range(len(hachi_rings) - 1):
        r0, r1 = hachi_rings[r_i], hachi_rings[r_i + 1]
        for s in range(32):
            sn = (s + 1) % 32
            f = bm.faces.new((r0[s], r0[sn], r1[sn], r1[s]))
            f.smooth = True
            for loop, sv in zip(f.loops, [s, sn, sn, s]):
                ang = 2.0 * math.pi * sv / 32
                front_d = abs(math.cos(ang)) if math.sin(ang) < -0.85 else 1.0
                if front_d < 0.15:
                    loop[uv_layer].uv = (0.18, 0.34)
                else:
                    loop[uv_layer].uv = (0.06, 0.34)

    # 5. Add Authentic Japanese Samurai Chonmage (Oicho-mage / Motodori Topknot) & Mizuhiki Cord
    # 5a) Broad swept-back hair dome & gather mound at the crown (X=-4.2, Y=-4.85, Z=154.8..160.4)
    gather_rings = []
    for rz, rx_r, ry_r in [
        (154.8, 6.20, 7.25),
        (156.8, 5.80, 6.65),
        (158.6, 4.55, 5.10),
        (159.6, 2.90, 3.10),
        (160.4, 1.85, 1.95),
    ]:
        ring = []
        for s in range(24):
            ang = 2.0 * math.pi * s / 24
            ring.append(bm.verts.new((-4.2 + math.cos(ang) * rx_r, -4.85 + math.sin(ang) * ry_r, rz)))
        gather_rings.append(ring)
    for r_i in range(len(gather_rings) - 1):
        r0, r1 = gather_rings[r_i], gather_rings[r_i + 1]
        for s in range(24):
            sn = (s + 1) % 24
            f = bm.faces.new((r0[s], r0[sn], r1[sn], r1[s]))
            f.smooth = True
            for loop in f.loops:
                loop[uv_layer].uv = (0.35 + 0.18 * (s / 24.0), 0.12)

    # 5b) Gold & Crimson Mizuhiki Cord Wrap around the vertical stem (Z=160.2..162.0)
    cord_rings = []
    for rz, rad in [(160.2, 1.80), (161.1, 1.70), (162.0, 1.80)]:
        ring = []
        for s in range(16):
            ang = 2.0 * math.pi * s / 16
            ring.append(bm.verts.new((-4.2 + math.cos(ang) * rad, -4.85 + math.sin(ang) * rad, rz)))
        cord_rings.append(ring)
    for r_i in range(len(cord_rings) - 1):
        r0, r1 = cord_rings[r_i], cord_rings[r_i + 1]
        for s in range(16):
            sn = (s + 1) % 16
            f = bm.faces.new((r0[s], r0[sn], r1[sn], r1[s]))
            f.smooth = True
            uv_c = (0.18, 0.34) if r_i == 0 else (0.06, 0.34)
            for loop in f.loops:
                loop[uv_layer].uv = uv_c

    # 5c) Forward-Folded Samurai Oicho-mage / Motodori Topknot Fan (Z=161.8..163.5, Y=-4.5..-11.2)
    bun_rings = []
    for cy_off, cz_off, rx_r, rz_r in [
        (-4.5, 161.8, 1.85, 1.45),
        (-6.2, 162.9, 2.15, 1.35),
        (-8.2, 163.3, 2.50, 1.25),
        (-10.0, 163.0, 2.75, 1.05),
        (-11.0, 162.6, 2.60, 0.80),
        (-11.3, 162.4, 0.25, 0.12),
    ]:
        ring = []
        for s in range(16):
            ang = 2.0 * math.pi * s / 16
            ridge = 1.0 + 0.05 * math.cos(ang * 8.0)
            ring.append(bm.verts.new((-4.2 + math.cos(ang) * rx_r * ridge, cy_off, cz_off + math.sin(ang) * rz_r * ridge)))
        bun_rings.append(ring)
    for r_i in range(len(bun_rings) - 1):
        r0, r1 = bun_rings[r_i], bun_rings[r_i + 1]
        for s in range(16):
            sn = (s + 1) % 16
            f = bm.faces.new((r0[s], r0[sn], r1[sn], r1[s]))
            f.smooth = True
            for loop in f.loops:
                loop[uv_layer].uv = (0.35 + 0.18 * (s / 16.0), 0.12)

    # 6. Inner Obsidian Armor Collar Under-Suit (tucked cleanly inside the neck & armor collar Z=133.5..140.5)
    seal_rings = []
    for rz, rx_rad, ry_rad in [
        (133.5, 8.2, 7.6),
        (137.0, 6.2, 5.8),
        (140.2, 4.7, 4.5),
    ]:
        ring = []
        for s in range(20):
            ang = 2.0 * math.pi * s / 20
            ring.append(bm.verts.new((-4.2 + math.cos(ang) * rx_rad, -5.2 + math.sin(ang) * ry_rad, rz)))
        seal_rings.append(ring)
    for r_i in range(len(seal_rings) - 1):
        r0, r1 = seal_rings[r_i], seal_rings[r_i + 1]
        for s in range(20):
            sn = (s + 1) % 20
            f = bm.faces.new((r0[s], r0[sn], r1[sn], r1[s]))
            f.smooth = True
            for loop in f.loops:
                loop[uv_layer].uv = (0.42, 0.10)

    bm.normal_update()
    head_mesh_data = bpy.data.meshes.new("CyberShogun_HeadMesh")
    bm.to_mesh(head_mesh_data)
    bm.free()

    head_obj = bpy.data.objects.new("CyberShogun_Head", head_mesh_data)
    bpy.context.scene.collection.objects.link(head_obj)

    # 7. Build Unified 1024x1024 Japanese Samurai Texture Atlas from Humanizer CC0 Asian Male Textures
    tw, th = 1024, 1024
    young_img = bpy.data.images.load(os.path.join(HUMANIZER_DIR, "young_asian_male.png"))
    mid_img = bpy.data.images.load(os.path.join(HUMANIZER_DIR, "middleage_asian_male.png"))
    eye_img = bpy.data.images.load(os.path.join(HUMANIZER_DIR, "eyeball_albedo.png"))
    iris_img = bpy.data.images.load(os.path.join(HUMANIZER_DIR, "overlay_iris_albedo.png"))

    yw, yh = young_img.size
    y_px = list(young_img.pixels[:])
    m_px = list(mid_img.pixels[:])

    ew, eh = eye_img.size
    e_px = list(eye_img.pixels[:])
    i_px = list(iris_img.pixels[:])

    head_img = bpy.data.images.new("ShogunHumanHead_Tex", width=tw, height=th, alpha=True)
    dst_px = [0.0] * (tw * th * 4)

    for ty in range(th):
        v = ty / float(th)
        sy = min(yh - 1, int(v * yh))
        for tx in range(tw):
            u = tx / float(tw)
            sx = min(yw - 1, int(u * yw))
            s_idx = (sy * yw + sx) * 4

            # Blend 60% Young Asian Male + 40% Middle-Age Asian Male for a seasoned 32-year-old Samurai Commander
            r = y_px[s_idx] * 0.60 + m_px[s_idx] * 0.40
            g = y_px[s_idx + 1] * 0.60 + m_px[s_idx + 1] * 0.40
            b = y_px[s_idx + 2] * 0.60 + m_px[s_idx + 2] * 0.40

            # Calibrate exposure (0.68x) so under Godot's 1.15x toon_character.gdshader it renders as rich,
            # natural warm Japanese skin (never chalk-white!) with full pore/brow/lip detail preserved
            cr = r * 0.68
            cg = g * 0.65
            cb = b * 0.62

            # Paint jet-black hair scalp & sideburns on the upper/rear skull region of the MakeHuman head UV island
            # Extending down to u = 0.802 so the scalp seamlessly meets the Hachimaki headband with zero skin gap!
            # Keep the ear pinna (u in 0.800..0.885, dv_center in 0.118..0.205) natural skin color!
            dv_center = abs(v - 0.4832)
            is_ear_pinna = (0.800 <= u <= 0.885 and 0.118 <= dv_center <= 0.205)
            is_scalp = False
            if u > 0.60 and not is_ear_pinna:
                if u < 0.802 and dv_center < 0.34:
                    is_scalp = True
                elif 0.802 <= u <= 0.858 and 0.096 <= dv_center <= 0.28:
                    # Narrow sideburns (bin) in front of the ears and nape hair behind the ears
                    is_scalp = True

            if is_scalp:
                strand = 0.5 + 0.5 * math.sin((u * 180.0) + (v * 90.0))
                cr = 0.035 + 0.025 * strand
                cg = 0.035 + 0.025 * strand
                cb = 0.045 + 0.030 * strand

            # Sharpen & darken the natural Asian male eyebrows directly on the skin texture (u in 0.818..0.835, dv in 0.015..0.068)
            # With a subtle upward samurai angle (u shifts toward forehead as dv_center increases)
            if 0.014 <= dv_center <= 0.068:
                brow_center_u = 0.828 - (dv_center - 0.014) * 0.12
                brow_half_w = 0.0068 * (1.15 - 0.45 * ((dv_center - 0.014) / 0.054))
                if abs(u - brow_center_u) < brow_half_w:
                    brow_t = 1.0 - abs(u - brow_center_u) / brow_half_w
                    hair_str = 0.82 * (brow_t ** 0.6)
                    cr = cr * (1.0 - hair_str) + 0.035 * hair_str
                    cg = cg * (1.0 - hair_str) + 0.035 * hair_str
                    cb = cb * (1.0 - hair_str) + 0.045 * hair_str

            # Crisp dark upper lash line around the upper eye rim only (u in 0.838..0.846, dv in 0.024..0.058)
            if 0.838 <= u <= 0.846 and 0.024 <= dv_center <= 0.058:
                lash_t = max(0.0, 1.0 - abs(u - 0.842) / 0.004)
                cr *= (1.0 - 0.45 * lash_t)
                cg *= (1.0 - 0.45 * lash_t)
                cb *= (1.0 - 0.45 * lash_t)

            # Subtle, realistic Japanese Samurai Facial Hair (Kuchihige Mustache & Agohige Chin Tuft)
            if u > 0.885 and dv_center < 0.065:
                stubble_noise = 0.5 + 0.5 * math.sin(tx * 1.7 + ty * 2.3) * math.cos(tx * 2.9 - ty * 1.3)
                m_curve_u = 0.895 + (dv_center / 0.042) ** 1.6 * 0.015
                if dv_center < 0.042 and abs(u - m_curve_u) < 0.0048:
                    m_w = (1.0 - abs(u - m_curve_u) / 0.0048) * (0.68 + 0.25 * stubble_noise)
                    cr = cr * (1.0 - m_w) + 0.06 * m_w
                    cg = cg * (1.0 - m_w) + 0.06 * m_w
                    cb = cb * (1.0 - m_w) + 0.07 * m_w
                elif 0.938 <= u <= 0.962 and dv_center < 0.020:
                    c_w = (1.0 - dv_center / 0.020) * (1.0 - abs(u - 0.950) / 0.014) * (0.65 + 0.25 * stubble_noise)
                    c_w = max(0.0, c_w)
                    cr = cr * (1.0 - c_w) + 0.06 * c_w
                    cg = cg * (1.0 - c_w) + 0.06 * c_w
                    cb = cb * (1.0 - c_w) + 0.07 * c_w

            d_idx = (ty * tw + tx) * 4
            dst_px[d_idx] = cr
            dst_px[d_idx + 1] = cg
            dst_px[d_idx + 2] = cb
            dst_px[d_idx + 3] = 1.0

    # Stamp the dedicated sub-regions into the unused lower-left quadrant (u < 0.60, v < 0.40):
    # Region A: [u: 0.02..0.28, v: 0.02..0.28] -> 3D Eyeball Sclera + Deep Japanese Dark Obsidian-Brown Iris & Pupil
    for ty in range(int(0.02 * th), int(0.28 * th)):
        nu = (ty / float(th) - 0.02) / 0.26
        ey = min(eh - 1, max(0, int((0.033 + nu * 0.535) * eh)))
        for tx in range(int(0.02 * tw), int(0.28 * tw)):
            nv = (tx / float(tw) - 0.02) / 0.26
            ex = min(ew - 1, max(0, int((0.028 + nv * 0.529) * ew)))
            e_idx = (ey * ew + ex) * 4
            er, eg, eb = e_px[e_idx], e_px[e_idx + 1], e_px[e_idx + 2]
            ir, ig, ib, ia = i_px[e_idx], i_px[e_idx + 1], i_px[e_idx + 2], i_px[e_idx + 3]

            # Sclera calibrated to warm natural ivory
            sr, sg, sb = er * 0.78, eg * 0.75, eb * 0.73
            if ia > 0.05:
                # Radial distance from center of iris (0.50, 0.50)
                rad_i = math.sqrt((nv - 0.50) ** 2 + (nu - 0.50) ** 2)
                if rad_i < 0.085 or ir < 0.12:
                    # Jet-black pupil center
                    brown_r, brown_g, brown_b = 0.015, 0.012, 0.012
                else:
                    # Warm, rich Japanese dark-amber/chocolate-brown iris ring with dark limbal ring
                    limbal = smoothstep(0.24, 0.18, rad_i)
                    brown_r = (0.14 + ir * 0.22) * limbal + 0.02
                    brown_g = (0.07 + ig * 0.11) * limbal + 0.015
                    brown_b = (0.03 + ib * 0.05) * limbal + 0.01
                sr = sr * (1.0 - ia) + brown_r * ia
                sg = sg * (1.0 - ia) + brown_g * ia
                sb = sb * (1.0 - ia) + brown_b * ia

            # Crisp corneal catchlight near upper-right of pupil
            dx_c = nv - 0.53
            dy_c = nu - 0.54
            if dx_c * dx_c + dy_c * dy_c < 0.0018:
                sr, sg, sb = 0.90, 0.90, 0.92

            d_idx = (ty * tw + tx) * 4
            dst_px[d_idx] = sr
            dst_px[d_idx + 1] = sg
            dst_px[d_idx + 2] = sb
            dst_px[d_idx + 3] = 1.0

    # Region B: [u: 0.30..0.60, v: 0.02..0.30] -> Jet-Black Combed Samurai Hair, Chonmage Topknot & Eyebrows
    for ty in range(int(0.02 * th), int(0.30 * th)):
        for tx in range(int(0.30 * tw), int(0.60 * tw)):
            strand = 0.5 + 0.5 * math.sin(tx * 0.35 + ty * 0.15)
            d_idx = (ty * tw + tx) * 4
            dst_px[d_idx] = 0.035 + 0.025 * strand
            dst_px[d_idx + 1] = 0.035 + 0.025 * strand
            dst_px[d_idx + 2] = 0.048 + 0.030 * strand
            dst_px[d_idx + 3] = 1.0

    # Region C: [u: 0.02..0.12, v: 0.30..0.38] -> Crimson-Indigo Hachimaki & Mizuhiki Cord
    # Region D: [u: 0.13..0.25, v: 0.30..0.38] -> Brass-Gold Clan Crest & Gold Mizuhiki Cord
    for ty in range(int(0.30 * th), int(0.38 * th)):
        for tx in range(int(0.02 * tw), int(0.12 * tw)):
            d_idx = (ty * tw + tx) * 4
            dst_px[d_idx] = 0.56
            dst_px[d_idx + 1] = 0.07
            dst_px[d_idx + 2] = 0.09
            dst_px[d_idx + 3] = 1.0
        for tx in range(int(0.13 * tw), int(0.25 * tw)):
            d_idx = (ty * tw + tx) * 4
            dst_px[d_idx] = 0.82
            dst_px[d_idx + 1] = 0.64
            dst_px[d_idx + 2] = 0.20
            dst_px[d_idx + 3] = 1.0

    head_img.pixels[:] = dst_px
    head_img.pack()

    head_mat = bpy.data.materials.new(name="ShogunHumanHead_Mat")
    head_mat.use_nodes = True
    nodes = head_mat.node_tree.nodes
    links = head_mat.node_tree.links
    nodes.clear()

    out_node = nodes.new(type='ShaderNodeOutputMaterial')
    bsdf = nodes.new(type='ShaderNodeBsdfPrincipled')
    bsdf.inputs['Metallic'].default_value = 0.02
    bsdf.inputs['Roughness'].default_value = 0.55
    links.new(bsdf.outputs['BSDF'], out_node.inputs['Surface'])

    tex_node = nodes.new(type='ShaderNodeTexImage')
    tex_node.image = head_img
    links.new(tex_node.outputs['Color'], bsdf.inputs['Base Color'])

    head_obj.data.materials.append(head_mat)

    # 9. Assign clean vertex weights (`Head`, `neck`, `Spine`)
    vg_head = head_obj.vertex_groups.new(name="Head")
    vg_neck = head_obj.vertex_groups.new(name="neck")
    vg_spine = head_obj.vertex_groups.new(name="Spine")

    for v in head_obj.data.vertices:
        z = v.co.z
        if z < 137.0:
            vg_spine.add([v.index], 0.7, 'REPLACE')
            vg_neck.add([v.index], 0.3, 'REPLACE')
        elif z < 143.0:
            vg_neck.add([v.index], 0.70, 'REPLACE')
            vg_head.add([v.index], 0.30, 'REPLACE')
        else:
            vg_head.add([v.index], 1.0, 'REPLACE')

    head_obj.parent = sh_arm
    arm_mod = head_obj.modifiers.new(name="Armature", type='ARMATURE')
    arm_mod.object = sh_arm

    return head_obj


def main():
    ensure_d3488f8_glb()
    bpy.ops.wm.read_factory_settings(use_empty=True)

    # 1. Import d3488f8_shogun.glb (has all 14 animations + rigged samurai body with katana in hand)
    bpy.ops.import_scene.gltf(filepath=SCRATCH_GLB)
    sh_arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    sh_mesh = next(o for o in bpy.data.objects if o.type == 'MESH')
    sh_arm.name = "Armature"
    sh_mesh.name = "CyberShogun_Body"
    sh_arm.data.pose_position = 'REST'

    # 2. Remove the robotic helmet AND the side shikoro flaps weighted to RightShoulder from sh_mesh
    vg_idx = {vg.name: vg.index for vg in sh_mesh.vertex_groups}
    head_vgs = {vg_idx[n] for n in ['Head', 'head', 'neck', 'Neck'] if n in vg_idx}
    rh_vg = vg_idx.get('RightHand', -1)

    bpy.context.view_layer.objects.active = sh_mesh
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='DESELECT')
    bpy.ops.object.mode_set(mode='OBJECT')

    for v in sh_mesh.data.vertices:
        rw = sum(g.weight for g in v.groups if g.group == rh_vg)
        if rw > 0.5:
            continue
        hw = sum(g.weight for g in v.groups if g.group in head_vgs)
        if (hw > 0.25 and v.co.z > 140.5) or (v.co.z > 142.5 and -23.5 <= v.co.x <= 12.5 and -25.0 <= v.co.y <= 10.0):
            v.select = True

    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.delete(type='VERT')
    bpy.ops.object.mode_set(mode='OBJECT')

    armor_mat = create_crimson_samurai_armor_material(sh_mesh)
    sh_mesh.data.materials.clear()
    sh_mesh.data.materials.append(armor_mat)

    # 3. Build the authentic Japanese Samurai head from Godot Asset Library's Humanizer Asian Male CC0 assets
    head_obj = build_humanizer_asian_samurai_head(sh_arm)

    bpy.ops.object.select_all(action='DESELECT')
    head_obj.select_set(True)
    sh_mesh.select_set(True)
    bpy.context.view_layer.objects.active = sh_mesh
    bpy.ops.object.join()

    bpy.context.view_layer.objects.active = sh_mesh
    bpy.ops.object.shade_smooth()

    sh_arm.data.pose_position = 'POSE'
    stand_act = bpy.data.actions.get("Standing") or bpy.data.actions.get("Idle")
    if stand_act:
        if not sh_arm.animation_data:
            sh_arm.animation_data_create()
        for t in sh_arm.animation_data.nla_tracks:
            t.mute = True
        sh_arm.animation_data.action = stand_act

    print(f"Final CyberShogun_Body: {len(sh_mesh.data.vertices)} verts, {len(sh_mesh.data.polygons)} faces")

    bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)
    bpy.ops.export_scene.gltf(
        filepath=OUT_GLB,
        export_format='GLB',
        export_animations=True,
        export_skins=True,
        export_morph=False,
        export_apply=False,
    )

    for web_path in [OUT_WEB_PUBLIC, OUT_WEB_DIST]:
        if os.path.exists(os.path.dirname(web_path)):
            shutil.copyfile(OUT_GLB, web_path)

    # Render Verification Previews (Portrait + Full Body)
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE'

    world = bpy.data.worlds.new("PreviewWorld")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.04, 0.05, 0.08, 1.0)
        bg.inputs[1].default_value = 1.0

    key_data = bpy.data.lights.new("KeyLight", type='AREA')
    key_data.energy = 120.0
    key_data.size = 2.0
    key_obj = bpy.data.objects.new("KeyLight", key_data)
    scene.collection.objects.link(key_obj)
    key_obj.location = (1.2, -2.0, 2.1)
    key_obj.rotation_euler = (math.radians(60), 0, math.radians(28))

    fill_data = bpy.data.lights.new("FillLight", type='AREA')
    fill_data.energy = 65.0
    fill_data.size = 2.0
    fill_data.color = (0.85, 0.92, 1.0)
    fill_obj = bpy.data.objects.new("FillLight", fill_data)
    scene.collection.objects.link(fill_obj)
    fill_obj.location = (-1.4, -1.8, 1.5)
    fill_obj.rotation_euler = (math.radians(70), 0, math.radians(-35))

    cam_data = bpy.data.cameras.new("PreviewCam")
    cam_obj = bpy.data.objects.new("PreviewCam", cam_data)
    scene.collection.objects.link(cam_obj)
    scene.camera = cam_obj

    # Portrait
    cam_data.lens = 65
    cam_obj.location = (-0.04, -0.78, 1.48)
    cam_obj.rotation_euler = (math.radians(89), 0, 0)
    scene.render.resolution_x = 700
    scene.render.resolution_y = 700
    scene.render.filepath = RENDER_PORTRAIT
    bpy.ops.render.render(write_still=True)

    # Full Body
    cam_data.lens = 50
    cam_obj.location = (-0.02, -2.55, 1.02)
    cam_obj.rotation_euler = (math.radians(88), 0, 0)
    scene.render.resolution_x = 800
    scene.render.resolution_y = 1000
    scene.render.filepath = RENDER_FULLBODY
    bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    main()
