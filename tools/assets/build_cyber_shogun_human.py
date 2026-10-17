"""
Build Cyber Shogun (Kaito Kurogane) - Authentic Japanese Samurai Operative.
100% Japanese Samurai facial structure, hairstyle, and traditional O-Yoroi aesthetic:
- Restores the 46k-vertex high-detail samurai armor mesh from clean source.
- Completely removes the robotic helmet AND the side shikoro flaps weighted to RightShoulder.
- Sculpts authentic Japanese Samurai facial geometry & traditional Chonmage hairstyle:
  1. Completely removes the Western pompadour/superhero curl from the forehead and pulls the crown hair
     smoothly upward and back into a traditional Japanese Samurai tied-back crown.
  2. Reshapes the facial bone structure into authentic Japanese warrior proportions:
     - Defined zygomatic arches (cheekbones) with a clean taper to a firm, dignified jawline
     - Flatter glabella/brow-to-nose transition (removes the deep-set Western brow ridge overhang)
     - Sharp, narrower almond-shaped East Asian eye apertures with an upward outer epicanthic tilt
  3. Sculpts a prominent 3D Samurai Chonmage (Motodori topknot) with traditional white/gold Mizuhiki cord
     binding and a sculpted 3D Hachimaki headband with flowing rear ribbons.
  4. Bakes a rich, natural Japanese olive-tan skin tone (albedo ~0.48, 0.34, 0.25 so it never blows out
     to chalk-white under Godot's 1.15x cel-shader light!), dark obsidian-brown Japanese eyes, sharp
     angled samurai eyebrows, traditional Sengoku-style tapered mustache & chin beard (kuchihige & agohige),
     and a crimson Hachimaki headband with a gold Takeda-diamond clan mon crest at the forehead center.
"""

import bpy
import bmesh
import math
import os
import subprocess
from mathutils import Vector, Matrix

PROJECT_ROOT = r"D:\Projects\frog_jumper"
SCRATCH_GLB = r"C:\Users\karthiknk21\.gemini\antigravity-cli\brain\51c70920-0a37-4082-8177-2c677a76cad3\scratch\d3488f8_shogun.glb"
SURYA_GLB = os.path.join(PROJECT_ROOT, "assets", "models", "hq", "surya_prime.glb")

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


def sculpt_and_bake_samurai_head(su_mesh):
    """
    Sculpt Kaito Kurogane's face & hair into an unmistakable Japanese Samurai warrior:
    - Eliminate the Western pompadour/curl at the front of the head and sweep the hair tightly back
      into a traditional Samurai Chonmage (Motodori topknot) with Mizuhiki cord binding.
    - Reshape the facial bone structure: higher cheekbones, flatter East Asian brow/glabella transition,
      narrower almond-shaped eyes with upward outer tilt, and traditional tapered samurai mustache & chin beard.
    - Bake a warm, natural Japanese skin complexion (calibrated for Godot's 1.15x cel-shader light so it
      never looks chalk-white!).
    """
    src_img = None
    for mat in su_mesh.data.materials:
        if mat and mat.use_nodes:
            for node in mat.node_tree.nodes:
                if node.type == 'TEX_IMAGE' and node.image:
                    src_img = node.image
                    break

    w, h = src_img.size
    src_px = list(src_img.pixels[:])

    su_vg_idx = {vg.name: vg.index for vg in su_mesh.vertex_groups}
    su_head_vgs = {su_vg_idx[n] for n in ['Head', 'head', 'neck', 'Neck'] if n in su_vg_idx}

    bpy.context.view_layer.objects.active = su_mesh
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='DESELECT')
    bpy.ops.object.mode_set(mode='OBJECT')
    uv_data = su_mesh.data.uv_layers.active.data

    collar_vert_indices = set()
    for poly in su_mesh.data.polygons:
        c = poly.center
        hw = sum(sum(g.weight for g in su_mesh.data.vertices[vi].groups if g.group in su_head_vgs) for vi in poly.vertices) / len(poly.vertices)
        if hw <= 0.35 or c.z < 143.2:
            for vi in poly.vertices:
                collar_vert_indices.add(vi)
            continue
        if c.z < 148.5 and (c.y > -7.5 or c.z < 145.2):
            is_collar_poly = False
            for li in poly.loop_indices:
                uv = uv_data[li].uv
                ix = min(w - 1, max(0, int(uv.x * w)))
                iy = min(h - 1, max(0, int(uv.y * h)))
                idx = (iy * w + ix) * 4
                r, g, b = src_px[idx], src_px[idx + 1], src_px[idx + 2]
                if (r > b + 0.08) or (b > r + 0.03 and r < 0.25):
                    is_collar_poly = True
                    break
            if is_collar_poly:
                for vi in poly.vertices:
                    if su_mesh.data.vertices[vi].co.z < 148.2:
                        collar_vert_indices.add(vi)

    for v in su_mesh.data.vertices:
        hw = sum(g.weight for g in v.groups if g.group in su_head_vgs)
        if hw <= 0.35 or v.co.z < 143.2 or v.index in collar_vert_indices:
            v.select = True

    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.delete(type='VERT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.delete_loose(use_verts=True, use_edges=True, use_faces=False)
    bpy.ops.object.mode_set(mode='OBJECT')

    # Sample original per-vertex luminance BEFORE re-unwrapping UVs
    uv_data = su_mesh.data.uv_layers.active.data
    vert_orig_lum = {v.index: 0.55 for v in su_mesh.data.vertices}
    for poly in su_mesh.data.polygons:
        for vi, li in zip(poly.vertices, poly.loop_indices):
            uv = uv_data[li].uv
            ix = min(w - 1, max(0, int(uv.x * w)))
            iy = min(h - 1, max(0, int(uv.y * h)))
            idx = (iy * w + ix) * 4
            r, g, b = src_px[idx], src_px[idx + 1], src_px[idx + 2]
            vert_orig_lum[vi] = 0.299 * r + 0.587 * g + 0.114 * b

    # Step 3: Sculpt Authentic Japanese Samurai Facial Proportions & Swept-Back Hairline
    v_canon = {}
    for v in su_mesh.data.vertices:
        cx, cy, cz = v.co.x, v.co.y, v.co.z

        # A. Eliminate the Western pompadour / front hair curl completely!
        # In the original mesh, the pompadour bulges out to Y = -10.19 at Z = 160..167, overhanging the forehead (Y = -9.0).
        # For a Japanese Samurai with hair pulled tautly back into a Chonmage topknot:
        # Above Z = 158.0, the front surface of the head must slope smoothly BACKWARD toward the crown!
        if cz > 158.0:
            t_crown = min(1.0, (cz - 158.0) / 10.0)
            # Target front limit recedes from Y = -8.6 at Z=158 to Y = -4.2 at Z=168
            front_limit_y = -8.65 + t_crown * 4.5
            if cy < front_limit_y:
                cy = front_limit_y + (cy - front_limit_y) * 0.15
            # Also sleekly taper the side volume of the upper hair toward the topknot gather point (X=0, Y=1.0, Z=165)
            gather_t = smoothstep(159.5, 167.5, cz)
            cx = cx * (1.0 - 0.35 * gather_t)
            # Lower the puffy Western pompadour height slightly so the Chonmage topknot dominates the silhouette
            if cz > 160.0:
                cz = 160.0 + (cz - 160.0) * 0.68

        # B. Authentic Japanese Facial Bone Structure:
        # 1. Defined East Asian cheekbones (zygomatic arch around Z = 151.5..154.0, abs(X) = 3.5..6.8)
        if 151.0 <= cz <= 154.5 and cy < -4.0:
            z_cheek = 1.0 - abs(cz - 152.8) / 1.8
            if z_cheek > 0:
                cx *= (1.0 + 0.04 * z_cheek)
                if 2.2 <= abs(cx) <= 5.5 and cy < -6.5:
                    cy -= 0.35 * z_cheek  # fuller forward malar projection (flatter mid-face plane)

        # 2. Softer East Asian glabella / brow-to-nose transition (less deep-set Western brow cavern)
        if 153.2 <= cz <= 155.5 and abs(cx) < 4.5 and cy < -7.0:
            # Bring the upper eyelid / inner eye plane slightly forward (more negativos Y) for epicanthic fold contour
            cy -= 0.28 * (1.0 - abs(cz - 154.3) / 1.2)

        # 3. Refined Japanese nose bridge (straight, dignified, slightly less bulbous tip)
        if 150.0 <= cz <= 154.0 and abs(cx) < 1.8 and cy < -9.0:
            cx *= 0.85
            if cy < -10.8:
                cy = -10.8 + (cy - (-10.8)) * 0.55

        # 4. Firm, angular Samurai jawline tapering cleanly from gonial angle to chin
        if 144.0 <= cz < 151.0:
            t_jaw = (151.0 - cz) / 7.0
            cx *= (0.94 - 0.05 * t_jaw)

        v.co.x, v.co.y, v.co.z = cx, cy, cz
        v_canon[v.index] = Vector((cx, cy, cz))

    # Step 4: Run Smart UV Project on the sculpted head mesh so every polygon has a unique, non-overlapping UV
    bpy.context.view_layer.objects.active = su_mesh
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(66.0), island_margin=0.008)
    bpy.ops.object.mode_set(mode='OBJECT')

    uv_data = su_mesh.data.uv_layers.active.data
    for loop in uv_data:
        loop.uv.y = 0.12 + loop.uv.y * 0.87

    # Step 5: Bake a 1024x1024 Japanese Samurai Face Texture
    # IMPORTANT: Calibrated for Godot's toon_character.gdshader (which multiplies ALBEDO by 1.15 + spec_glint),
    # so we use a rich, warm Japanese olive-amber skin tone (R ~ 0.50, G ~ 0.36, B ~ 0.26) that looks
    # like natural warm Japanese skin in-game instead of blowing out to white!
    tw, th = 1024, 1024
    head_img = bpy.data.images.new("ShogunHumanHead_Tex", width=tw, height=th, alpha=True)
    dst_px = [0.48, 0.34, 0.25, 1.0] * (tw * th)

    def fill_uv_triangle(uv0, uv1, uv2, p0, p1, p2, l0, l1, l2):
        x0, y0 = uv0.x * tw, uv0.y * th
        x1, y1 = uv1.x * tw, uv1.y * th
        x2, y2 = uv2.x * tw, uv2.y * th
        min_x = max(0, int(min(x0, x1, x2)) - 2)
        max_x = min(tw - 1, int(max(x0, x1, x2)) + 2)
        min_y = max(int(0.11 * th), int(min(y0, y1, y2)) - 2)
        max_y = min(th - 1, int(max(y0, y1, y2)) + 2)
        denom = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2)
        if abs(denom) < 1e-5:
            return
        for py in range(min_y, max_y + 1):
            for px_i in range(min_x, max_x + 1):
                w0 = ((y1 - y2) * (px_i + 0.5 - x2) + (x2 - x1) * (py + 0.5 - y2)) / denom
                w1 = ((y2 - y0) * (px_i + 0.5 - x2) + (x0 - x2) * (py + 0.5 - y2)) / denom
                w2 = 1.0 - w0 - w1
                if w0 >= -0.18 and w1 >= -0.18 and w2 >= -0.18:
                    cw0, cw1, cw2 = max(0.0, w0), max(0.0, w1), max(0.0, w2)
                    s_w = cw0 + cw1 + cw2
                    if s_w > 0:
                        cw0, cw1, cw2 = cw0 / s_w, cw1 / s_w, cw2 / s_w
                    pos = p0 * cw0 + p1 * cw1 + p2 * cw2
                    lum = l0 * cw0 + l1 * cw1 + l2 * cw2
                    cx, cy, cz = pos.x, pos.y, pos.z

                    # Warm Japanese Golden-Tan / Olive Complexion (calibrated for Godot cel-shader)
                    # Use symmetrical 3D anatomical shading so there is no vertical seam down the center of the nose/chin
                    frontness = max(0.0, min(1.0, (-cy - 4.0) / 6.5))
                    height_warmth = max(0.0, min(1.0, (cz - 144.0) / 14.0))
                    shade = 0.88 + 0.14 * frontness + 0.06 * height_warmth
                    # Add natural facial sculpting gradients (cheekbone highlight, jaw/temple contour)
                    if cy < -7.5 and 151.5 <= cz <= 154.5 and 1.8 <= abs(cx) <= 4.8:
                        shade += 0.06  # East Asian malar/cheekbone highlight
                    elif abs(cx) > 5.0 or cz < 146.2:
                        shade -= 0.07  # temple & under-jaw contour

                    cr = min(0.76, 0.64 * shade)
                    cg = min(0.58, 0.47 * shade)
                    cb = min(0.45, 0.35 * shade)

                    # Subtle warm nose bridge & philtrum contour
                    if cy < -8.5 and 150.5 <= cz <= 154.2:
                        if abs(cx) < 0.55:
                            cr, cg, cb = cr + 0.035, cg + 0.025, cb + 0.018
                        elif 0.75 <= abs(cx) <= 1.55:
                            cr, cg, cb = cr * 0.89, cg * 0.87, cb * 0.86

                    # 1. Traditional Japanese Samurai Swept-Back Jet-Black Hair (with subtle combed strand texture)
                    # Hairline starts cleanly above the Hachimaki (cz > 158.6), plus sideburns (bin) at temples
                    is_hair = (
                        cz > 158.6
                        or (cy > -2.5 and cz > 146.8)
                        or (abs(cx) > 6.3 and cy > -5.8 and cz > 150.2)
                        or (abs(cx) > 5.6 and -5.8 <= cy <= -2.0 and 152.5 <= cz <= 158.6)
                    )
                    if is_hair:
                        # Subtle combed strand sheen oriented toward the crown topknot
                        strand = 0.5 + 0.5 * math.sin(cx * 3.8 + cy * 1.5)
                        cr = 0.03 + 0.03 * strand
                        cg = 0.03 + 0.03 * strand
                        cb = 0.04 + 0.04 * strand

                    # 2. Traditional Samurai Hachimaki Headband (cz in 156.8 .. 158.8)
                    # Deep Obsidian-Indigo cloth with Crimson borders and a Gold Takeda-clan 4-diamond Mon crest at center!
                    if 156.8 <= cz <= 158.8:
                        # Check for Gold Clan Mon crest at center of forehead (cy < -7.0, abs(cx) < 1.35)
                        dx_m = abs(cx) / 1.15
                        dz_m = abs(cz - 157.8) / 0.72
                        diamond_d = dx_m + dz_m
                        if cy < -7.0 and (0.32 <= diamond_d <= 0.62 or diamond_d <= 0.18):
                            cr, cg, cb = 0.82, 0.64, 0.20  # Gold Samurai Clan Crest (Mon)
                        elif cz < 157.15 or cz > 158.45:
                            cr, cg, cb = 0.58, 0.08, 0.10  # Crimson border trim
                        else:
                            cr, cg, cb = 0.10, 0.10, 0.14  # Traditional dark indigo/obsidian headband cloth

                    # 3. Authentic Japanese Samurai Eyebrows (Mayu: sharp, straight-angled upward toward temples)
                    if cy < -6.5 and 0.95 <= abs(cx) <= 4.85:
                        brow_z = 155.15 + (abs(cx) - 0.95) * 0.26
                        brow_thick = 0.38 * (1.15 - 0.45 * ((abs(cx) - 0.95) / 3.9))
                        if abs(cz - brow_z) < brow_thick:
                            cr, cg, cb = 0.04, 0.04, 0.05

                    # 4. Authentic East Asian / Japanese Almond-Shaped Eyes with Epicanthic Fold & Upward Outer Tilt
                    if cy < -6.5 and 153.2 <= cz <= 155.2:
                        # Left eye (+X) and Right eye (-X)
                        for sign_x, ex_c, ez_c in [(1.0, 2.55, 154.22), (-1.0, -2.95, 154.32)]:
                            # Outer corner tilts upward by +0.22 cm relative to inner corner!
                            rel_x = (cx - ex_c) * sign_x  # negative = inner canthus (toward nose), positive = outer canthus (toward temple)
                            tilt_z = ez_c + rel_x * 0.14
                            dx_e = (cx - ex_c) / 1.32
                            dz_e = (cz - tilt_z) / 0.36  # Narrower, sharper East Asian almond aperture (0.36 vs 0.52!)
                            dist_e = math.sqrt(dx_e * dx_e + dz_e * dz_e)

                            # Epicanthic fold crease just above the upper eyelid
                            if -1.1 <= dx_e <= 1.05 and 0.36 <= (cz - tilt_z) <= 0.54:
                                cr, cg, cb = cr * 0.72, cg * 0.70, cb * 0.70
                            elif dist_e < 1.05:
                                # Thick dark upper lash line & sharp inner/outer canthus corners
                                if dist_e > 0.78 or (cz - tilt_z) > 0.18:
                                    cr, cg, cb = 0.04, 0.03, 0.04
                                else:
                                    iris_d = math.sqrt(((cx - ex_c) / 0.56) ** 2 + ((cz - tilt_z) / 0.34) ** 2)
                                    if iris_d < 0.38:
                                        if cx > ex_c + 0.05 and cz > tilt_z + 0.04 and iris_d < 0.22:
                                            cr, cg, cb = 0.85, 0.85, 0.88  # Subtle catchlight
                                        else:
                                            cr, cg, cb = 0.03, 0.02, 0.02  # Jet-black pupil
                                    elif iris_d < 0.90:
                                        cr, cg, cb = 0.16, 0.09, 0.05  # Deep Japanese dark-brown iris
                                    else:
                                        cr, cg, cb = 0.74, 0.71, 0.68  # Natural warm sclera (not neon white!)

                    # 5. Traditional Japanese Sengoku Samurai Facial Hair (Kuchihige Mustache & Agohige Chin Tuft)
                    # Unlike a Western blocky box-goatee, a classic Japanese samurai mustache tapers sharply
                    # downward past the corners of the mouth, paired with a clean triangular soul-patch & chin beard
                    # leaving the skin on either side of the lower lip natural!
                    if cy < -6.5:
                        # Tapered samurai mustache (Kuchihige) curving down around mouth corners
                        m_arch_z = 150.38 - (abs(cx) ** 1.55) * 0.26
                        m_thick = max(0.10, 0.34 - abs(cx) * 0.06)
                        if abs(cx) < 2.95 and abs(cz - m_arch_z) < m_thick:
                            cr, cg, cb = 0.05, 0.05, 0.06
                        # Traditional under-lip soul patch (narrow triangle at abs(cx) < 0.55, cz in 147.8..148.9)
                        elif 147.8 <= cz <= 148.9 and abs(cx) < (0.58 - (148.9 - cz) * 0.30):
                            cr, cg, cb = 0.05, 0.05, 0.06
                        # Pointed Samurai chin beard (Agohige) on the lower chin ridge (cz in 144.0..146.6)
                        elif 144.0 <= cz <= 146.6 and abs(cx) < (1.65 - (cz - 144.0) * 0.32):
                            cr, cg, cb = 0.05, 0.05, 0.06
                        # Subtle samurai jawline stubble shadow
                        elif 144.2 <= cz <= 148.5 and 1.6 <= abs(cx) <= 4.8 and cy < -4.5:
                            cr, cg, cb = cr * 0.88, cg * 0.86, cb * 0.86

                    # 6. Natural Lips (firm, stoic expression)
                    if cy < -8.2 and 149.0 <= cz <= 149.85 and abs(cx) < 1.85:
                        if abs(cz - 149.45) < 0.12:
                            cr, cg, cb = 0.18, 0.09, 0.08
                        else:
                            cr, cg, cb = 0.44, 0.27, 0.24

                    # 7. Katana Battle Scar across right cheek
                    if cy < -6.8 and 1.6 <= cx <= 3.4 and 150.0 <= cz <= 153.6:
                        scar_x = 2.5 - (cz - 151.8) * 0.16
                        if abs(cx - scar_x) < 0.15:
                            cr, cg, cb = cr * 0.78 + 0.08, cg * 0.68, cb * 0.65

                    d_idx = (py * tw + px_i) * 4
                    dst_px[d_idx] = cr
                    dst_px[d_idx + 1] = cg
                    dst_px[d_idx + 2] = cb
                    dst_px[d_idx + 3] = 1.0

    for poly in su_mesh.data.polygons:
        loops = poly.loop_indices
        v0 = su_mesh.data.vertices[poly.vertices[0]].index
        uv0 = uv_data[loops[0]].uv
        p0 = v_canon[v0]
        l0 = vert_orig_lum[v0]
        for i in range(1, len(loops) - 1):
            vi1 = su_mesh.data.vertices[poly.vertices[i]].index
            vi2 = su_mesh.data.vertices[poly.vertices[i + 1]].index
            uv1 = uv_data[loops[i]].uv
            uv2 = uv_data[loops[i + 1]].uv
            fill_uv_triangle(
                uv0, uv1, uv2,
                p0, v_canon[vi1], v_canon[vi2],
                l0, vert_orig_lum[vi1], vert_orig_lum[vi2]
            )

    # Fill the reserved bottom strip Y in [0..80] for:
    # - [0..340]: Jet-black Samurai Hair & Neck Seal (with subtle strand variation)
    # - [340..680]: Crimson Hachimaki / Mizuhiki cord
    # - [680..1024]: Gold Mizuhiki Topknot binding cord
    for ty in range(0, 80):
        for tx in range(0, 340):
            d_idx = (ty * tw + tx) * 4
            strand_v = 0.015 * math.sin(tx * 0.25)
            dst_px[d_idx] = 0.035 + strand_v
            dst_px[d_idx + 1] = 0.035 + strand_v
            dst_px[d_idx + 2] = 0.050 + strand_v
            dst_px[d_idx + 3] = 1.0
        for tx in range(340, 680):
            d_idx = (ty * tw + tx) * 4
            dst_px[d_idx] = 0.60
            dst_px[d_idx + 1] = 0.08
            dst_px[d_idx + 2] = 0.10
            dst_px[d_idx + 3] = 1.0
        for tx in range(680, 1024):
            d_idx = (ty * tw + tx) * 4
            dst_px[d_idx] = 0.78
            dst_px[d_idx + 1] = 0.62
            dst_px[d_idx + 2] = 0.22
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
    bsdf.inputs['Roughness'].default_value = 0.58
    links.new(bsdf.outputs['BSDF'], out_node.inputs['Surface'])

    tex_node = nodes.new(type='ShaderNodeTexImage')
    tex_node.image = head_img
    links.new(tex_node.outputs['Color'], bsdf.inputs['Base Color'])

    su_mesh.data.materials.clear()
    su_mesh.data.materials.append(head_mat)

    # Step 6: Translate the sculpted head to the exact center of the Samurai Collar Opening
    OFFSET = Vector((-4.2, -7.2, -3.8))
    for v in su_mesh.data.vertices:
        v.co += OFFSET

    # Clear any split normals from the original glTF so the sculpted face shades smoothly
    bpy.context.view_layer.objects.active = su_mesh
    try:
        bpy.ops.mesh.customdata_custom_splitnormals_clear()
    except Exception:
        pass

    # Step 7: Add 3D Sculpted Samurai Chonmage (Oicho-mage / Motodori Topknot), Mizuhiki Cord, and Neck Seal
    bm = bmesh.new()
    bm.from_mesh(su_mesh.data)
    uv_layer = bm.loops.layers.uv.active

    # 7a. Obsidian Inner Neck Column & Collar Seal
    seal_rings = []
    for rz, rx_rad, ry_rad in [
        (133.0, 8.8, 8.2),
        (136.5, 7.2, 6.8),
        (140.0, 5.8, 5.6),
        (143.5, 5.4, 5.2),
    ]:
        ring = []
        for s in range(20):
            ang = 2.0 * math.pi * s / 20
            vx = -4.2 + math.cos(ang) * rx_rad
            vy = -7.2 + math.sin(ang) * ry_rad
            v = bm.verts.new((vx, vy, rz))
            ring.append(v)
        seal_rings.append(ring)

    for r_i in range(len(seal_rings) - 1):
        r0, r1 = seal_rings[r_i], seal_rings[r_i + 1]
        for s in range(20):
            sn = (s + 1) % 20
            f = bm.faces.new((r0[s], r0[sn], r1[sn], r1[s]))
            f.smooth = True
            for loop in f.loops:
                loop[uv_layer].uv = (0.15, 0.03)  # Jet-black/obsidian strip

    # 7b. Authentic Japanese Samurai Chonmage (Oicho-mage / Motodori Topknot)
    # 1) Hair gather mound at the crown (X=-4.2, Y=-4.8, Z=158.0..160.8)
    gather_rings = []
    for rz, rx_r, ry_r in [
        (158.0, 4.6, 5.0),
        (159.6, 3.0, 3.2),
        (160.8, 1.9, 2.0),
    ]:
        ring = []
        for s in range(16):
            ang = 2.0 * math.pi * s / 16
            v = bm.verts.new((-4.2 + math.cos(ang) * rx_r, -4.8 + math.sin(ang) * ry_r, rz))
            ring.append(v)
        gather_rings.append(ring)
    for r_i in range(len(gather_rings) - 1):
        r0, r1 = gather_rings[r_i], gather_rings[r_i + 1]
        for s in range(16):
            sn = (s + 1) % 16
            f = bm.faces.new((r0[s], r0[sn], r1[sn], r1[s]))
            f.smooth = True
            for loop in f.loops:
                loop[uv_layer].uv = (0.05 + 0.20 * (s / 16.0), 0.03)  # Stranded jet-black hair

    # 2) Gold & Crimson Mizuhiki Cord Wrap around the vertical stem of the Chonmage (Z=160.6..162.6)
    cord_rings = []
    for rz, rad in [(160.6, 1.85), (161.6, 1.75), (162.6, 1.85)]:
        ring = []
        for s in range(16):
            ang = 2.0 * math.pi * s / 16
            v = bm.verts.new((-4.2 + math.cos(ang) * rad, -4.8 + math.sin(ang) * rad, rz))
            ring.append(v)
        cord_rings.append(ring)
    for r_i in range(len(cord_rings) - 1):
        r0, r1 = cord_rings[r_i], cord_rings[r_i + 1]
        for s in range(16):
            sn = (s + 1) % 16
            f = bm.faces.new((r0[s], r0[sn], r1[sn], r1[s]))
            f.smooth = True
            uv_c = (0.85, 0.03) if r_i == 0 else (0.50, 0.03)  # Gold lower wrap, Crimson upper wrap
            for loop in f.loops:
                loop[uv_layer].uv = uv_c

    # 3) Forward-Folded Samurai Oicho-mage / Motodori Topknot Hair Fan (Z=162.4..164.3, Y=-4.8..-11.2)
    # Folds forward over the crown and fans out horizontally into a flat, solid jet-black hair brush!
    bun_rings = []
    for cy_off, cz_off, rx_r, rz_r in [
        (-4.5, 162.5, 1.85, 1.55),
        (-6.2, 163.8, 2.15, 1.45),
        (-8.2, 164.2, 2.55, 1.30),
        (-10.0, 163.9, 2.85, 1.10),
        (-11.0, 163.5, 2.70, 0.85),
        (-11.3, 163.3, 0.25, 0.12),
    ]:
        ring = []
        for s in range(16):
            ang = 2.0 * math.pi * s / 16
            # Subtle hair strand ridges along the topknot bundle
            ridge = 1.0 + 0.05 * math.cos(ang * 8.0)
            v = bm.verts.new((-4.2 + math.cos(ang) * rx_r * ridge, cy_off, cz_off + math.sin(ang) * rz_r * ridge))
            ring.append(v)
        bun_rings.append(ring)
    for r_i in range(len(bun_rings) - 1):
        r0, r1 = bun_rings[r_i], bun_rings[r_i + 1]
        for s in range(16):
            sn = (s + 1) % 16
            f = bm.faces.new((r0[s], r0[sn], r1[sn], r1[s]))
            f.smooth = True
            for loop in f.loops:
                loop[uv_layer].uv = (0.04 + 0.22 * (s / 16.0), 0.03)  # Solid stranded jet-black hair!

    # Smooth all faces on the head mesh so cheeks, brow, nose, and jawline render without low-poly facets
    for f in bm.faces:
        f.smooth = True

    bm.normal_update()
    bm.to_mesh(su_mesh.data)
    bm.free()

    # Step 8: Assign clean vertex weights (`Head`, `neck`, `Spine`)
    su_mesh.vertex_groups.clear()
    vg_head = su_mesh.vertex_groups.new(name="Head")
    vg_neck = su_mesh.vertex_groups.new(name="neck")
    vg_spine = su_mesh.vertex_groups.new(name="Spine")

    for v in su_mesh.data.vertices:
        z = v.co.z
        if z < 136.0:
            vg_spine.add([v.index], 0.7, 'REPLACE')
            vg_neck.add([v.index], 0.3, 'REPLACE')
        elif z < 141.5:
            vg_neck.add([v.index], 0.75, 'REPLACE')
            vg_head.add([v.index], 0.25, 'REPLACE')
        else:
            vg_head.add([v.index], 1.0, 'REPLACE')


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

    # 3. Import the human head mesh, sculpt into Japanese Samurai (Kaito Kurogane), and bake custom texture
    sh_actions = set(bpy.data.actions)
    bpy.ops.import_scene.gltf(filepath=SURYA_GLB)
    su_arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE' and o != sh_arm)
    su_mesh = next(o for o in bpy.data.objects if o.type == 'MESH' and o != sh_mesh)
    su_arm.data.pose_position = 'REST'

    for act in list(bpy.data.actions):
        if act not in sh_actions:
            bpy.data.actions.remove(act)

    sculpt_and_bake_samurai_head(su_mesh)

    su_mesh.parent = sh_arm
    for mod in su_mesh.modifiers:
        if mod.type == 'ARMATURE':
            mod.object = sh_arm

    bpy.ops.object.select_all(action='DESELECT')
    su_mesh.select_set(True)
    sh_mesh.select_set(True)
    bpy.context.view_layer.objects.active = sh_mesh
    bpy.ops.object.join()

    bpy.data.objects.remove(su_arm, do_unlink=True)

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

    import shutil
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
    cam_data.lens = 45
    cam_obj.location = (-0.02, -2.35, 0.92)
    cam_obj.rotation_euler = (math.radians(88), 0, 0)
    scene.render.resolution_x = 700
    scene.render.resolution_y = 1000
    scene.render.filepath = RENDER_FULLBODY
    bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    main()
