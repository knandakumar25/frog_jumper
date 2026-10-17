"""
Build Cyber Shogun (Kaito Kurogane) - High-Fidelity Human Samurai Operative.
100% Human Samurai (no robot helmet, no visor, no existing roster color/face reuse):
- Restores the 46k-vertex high-detail samurai armor mesh from clean source.
- Completely removes the robotic helmet AND the side shikoro flaps weighted to RightShoulder.
- Uses Smart UV Project on the sculpted human head mesh BEFORE rasterizing the custom 1024x1024
  samurai face texture so every face polygon has its own unique, non-overlapping UV island!
- Sculpts & bakes a custom 1024x1024 human samurai head texture (using bpy.data.images.new() so packed_file never leaks old textures):
  - Warm East Asian skin tone with sculpted cheekbone/jaw shading
  - Dark brown human irises, black pupils, natural sclera, thick dark samurai eyebrows
  - Sculpted & painted samurai mustache and chin beard/goatee + battle scar across left cheek
  - Crimson & gold Hachimaki headband + swept-back jet-black hair and Chonmage topknot
  - Dead-center alignment inside the samurai collar opening (X=-4.2, Y=-7.2, Z=-3.8 offset in REST pose) with an obsidian inner neck seal so there is zero gap at the collar from any angle.
- Smoothly remaps the armor texture into Deep Crimson Samurai Lacquer, Obsidian Black steel, Brass-Gold trim, and Cyan plasma katana edges using continuous color blending (no hard thresholds).
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

                # Preserve original fine shading & panel seams by tinting smoothly!
                # 1. Check if this pixel is a glowing cyan energy line / plasma blade edge
                cyan_w = smoothstep(0.06, 0.15, b - r) * smoothstep(0.32, 0.48, g)
                # 2. Check if this pixel is an original brass/gold ornament (tsuba, knee clasps)
                gold_orn_w = smoothstep(0.05, 0.12, r - b) * smoothstep(0.20, 0.35, lum)

                # Base armor ramp: dark obsidian charcoal for under-suit/obi (lum < 0.24),
                # rich deep crimson lacquer for samurai armor plates (lum >= 0.24),
                # with warm gold/crimson highlights on bright bevels (lum > 0.62)
                plate_w = smoothstep(0.20, 0.30, lum)
                bevel_w = smoothstep(0.58, 0.78, lum)

                # Obsidian charcoal
                obs_r = lum * 0.48 + 0.02
                obs_g = lum * 0.48 + 0.02
                obs_b = lum * 0.54 + 0.03

                # Deep Samurai Crimson Lacquer (smoothly proportional to lum so zero blotchiness!)
                crim_r = 0.14 + lum * 0.78
                crim_g = 0.03 + lum * 0.14
                crim_b = 0.04 + lum * 0.16

                # Warm Brass-Gold bevel highlight
                gold_r = min(0.95, 0.25 + lum * 0.85)
                gold_g = min(0.78, 0.16 + lum * 0.62)
                gold_b = max(0.08, 0.05 + lum * 0.22)

                nr = obs_r * (1.0 - plate_w) + crim_r * plate_w
                ng = obs_g * (1.0 - plate_w) + crim_g * plate_w
                nb = obs_b * (1.0 - plate_w) + crim_b * plate_w

                # Blend bright bevels and gold ornaments
                gw = max(bevel_w * 0.65, gold_orn_w)
                nr = nr * (1.0 - gw) + gold_r * gw
                ng = ng * (1.0 - gw) + gold_g * gw
                nb = nb * (1.0 - gw) + gold_b * gw

                # Blend cyan plasma accents
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
    1. Delete all non-head vertices AND any collar polygons (gold/navy suit collar) from su_mesh.
    2. Sample each vertex's original shading luminance from Surya's texture BEFORE re-unwrapping.
    3. Reshape facial proportions into Kaito Kurogane (leaner cheekbones, stronger samurai jaw, swept-back hair).
    4. Run Smart UV Project on the head mesh so there is ZERO UV overlap between hair/neck and nose/mouth!
    5. Rasterize a crisp 1024x1024 human samurai texture onto the non-overlapping UVs using 3D canonical coordinates
       + original vertex shading luminance.
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
    uv_data = su_mesh.data.uv_layers.active.data

    su_vg_idx = {vg.name: vg.index for vg in su_mesh.vertex_groups}
    su_head_vgs = {su_vg_idx[n] for n in ['Head', 'head', 'neck', 'Neck'] if n in su_vg_idx}

    # Step 1: Identify and delete all non-head vertices AND any collar polygons
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
        # Only check for suit collar polygons below the chin (Z < 148.5) and not on the front face (Y > -7.5 or Z < 145.5)
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

    # Step 2: Sample original per-vertex luminance & RGB BEFORE we re-unwrap UVs!
    uv_data = su_mesh.data.uv_layers.active.data
    vert_orig_lum = {v.index: 0.55 for v in su_mesh.data.vertices}
    vert_orig_rgb = {v.index: (0.55, 0.55, 0.55) for v in su_mesh.data.vertices}
    for poly in su_mesh.data.polygons:
        for vi, li in zip(poly.vertices, poly.loop_indices):
            uv = uv_data[li].uv
            ix = min(w - 1, max(0, int(uv.x * w)))
            iy = min(h - 1, max(0, int(uv.y * h)))
            idx = (iy * w + ix) * 4
            r, g, b = src_px[idx], src_px[idx + 1], src_px[idx + 2]
            lum = 0.299 * r + 0.587 * g + 0.114 * b
            vert_orig_lum[vi] = lum
            vert_orig_rgb[vi] = (r, g, b)

    # Step 3: Reshape facial proportions in canonical head space (centered around X=0, Y=0, Z=155)
    v_canon = {}
    for v in su_mesh.data.vertices:
        cx, cy, cz = v.co.x, v.co.y, v.co.z
        # Narrower temples/cheekbones, firmer jaw angle
        if 145.0 <= cz <= 158.0:
            cx *= 0.93
        # Stronger samurai chin
        if 144.5 <= cz <= 147.0 and abs(cx) < 2.8 and cy < -6.5:
            cy -= 0.45
            cz -= 0.25
        # Flatten the front hair curl into a sleek swept-back warrior hairline
        if cz > 157.0 and cy < -5.0 and abs(cx) < 5.0:
            # Pull the hanging forehead lock back into the hairline so the forehead/headband is clean!
            cy = max(cy, -8.8 + (160.0 - min(160.0, cz)) * 0.25)
        if cz > 161.0:
            cz = 161.0 + (cz - 161.0) * 0.82

        v.co.x, v.co.y, v.co.z = cx, cy, cz
        v_canon[v.index] = Vector((cx, cy, cz))

    # Step 4: Run Smart UV Project on su_mesh so EVERY polygon has a unique, non-overlapping UV!
    bpy.context.view_layer.objects.active = su_mesh
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(66.0), island_margin=0.008)
    bpy.ops.object.mode_set(mode='OBJECT')

    # Scale all head UVs into [0.0, 1.0] x [0.10, 1.0] so the bottom strip Y in [0.0, 0.08] is reserved for Topknot & Neck Seal!
    uv_data = su_mesh.data.uv_layers.active.data
    for loop in uv_data:
        loop.uv.y = 0.10 + loop.uv.y * 0.89

    # Step 5: Rasterize a crisp 1024x1024 human samurai texture onto the non-overlapping UVs!
    tw, th = 1024, 1024
    head_img = bpy.data.images.new("ShogunHumanHead_Tex", width=tw, height=th, alpha=True)
    # Default background fill: warm mid skin tone (prevents black seam bleeding at UV island edges!)
    dst_px = [0.68, 0.50, 0.39, 1.0] * (tw * th)

    def fill_uv_triangle(uv0, uv1, uv2, p0, p1, p2, l0, l1, l2, rgb0, rgb1, rgb2):
        x0, y0 = uv0.x * tw, uv0.y * th
        x1, y1 = uv1.x * tw, uv1.y * th
        x2, y2 = uv2.x * tw, uv2.y * th
        # Expand bounding box by 2px so UV island seams have zero mipmap bleeding
        min_x = max(0, int(min(x0, x1, x2)) - 2)
        max_x = min(tw - 1, int(max(x0, x1, x2)) + 2)
        min_y = max(int(0.09 * th), int(min(y0, y1, y2)) - 2)
        max_y = min(th - 1, int(max(y0, y1, y2)) + 2)
        denom = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2)
        if abs(denom) < 1e-5:
            return
        for py in range(min_y, max_y + 1):
            for px_i in range(min_x, max_x + 1):
                w0 = ((y1 - y2) * (px_i + 0.5 - x2) + (x2 - x1) * (py + 0.5 - y2)) / denom
                w1 = ((y2 - y0) * (px_i + 0.5 - x2) + (x0 - x2) * (py + 0.5 - y2)) / denom
                w2 = 1.0 - w0 - w1
                # Allow a slight 1.5px gutter margin around triangles
                if w0 >= -0.18 and w1 >= -0.18 and w2 >= -0.18:
                    cw0, cw1, cw2 = max(0.0, w0), max(0.0, w1), max(0.0, w2)
                    s_w = cw0 + cw1 + cw2
                    if s_w > 0:
                        cw0, cw1, cw2 = cw0 / s_w, cw1 / s_w, cw2 / s_w
                    pos = p0 * cw0 + p1 * cw1 + p2 * cw2
                    lum = l0 * cw0 + l1 * cw1 + l2 * cw2
                    cx, cy, cz = pos.x, pos.y, pos.z

                    # Base human skin tone modulated by subtle original ambient occlusion/shading
                    shade = 0.85 + 0.30 * max(0.0, min(1.0, (lum - 0.25) / 0.55))
                    cr = min(1.0, 0.72 * shade)
                    cg = min(1.0, 0.53 * shade)
                    cb = min(1.0, 0.42 * shade)

                    # Subtle cheek warmth & nose bridge highlight
                    if cy < -8.0 and 150.5 <= cz <= 154.5 and abs(cx) < 1.2:
                        cr = min(1.0, cr + 0.06)
                        cg = min(1.0, cg + 0.04)
                        cb = min(1.0, cb + 0.03)

                    # 1. Jet-Black Samurai Hair & Sideburns
                    # Crown hair (cz > 159.0), back hair (cy > -2.2 and cz > 147.0), temple sideburns (abs(cx) > 6.6 and cy > -5.5 and cz > 150.5)
                    # Or where original texture was dark hair (lum < 0.24 and cz > 155.5)
                    if cz > 159.0 or (cy > -2.2 and cz > 147.0) or (abs(cx) > 6.6 and cy > -5.5 and cz > 150.5) or (lum < 0.24 and cz > 156.2):
                        cr, cg, cb = 0.05, 0.05, 0.07

                    # 2. Crimson & Gold Samurai Hachimaki Headband (cz in 157.0 .. 159.0)
                    if 157.0 <= cz <= 159.0:
                        if abs(cz - 158.0) < 0.20:
                            cr, cg, cb = 0.82, 0.64, 0.22  # Brass-gold centerline weave
                        elif 157.2 <= cz <= 158.8:
                            cr, cg, cb = 0.64, 0.08, 0.10  # Deep Samurai Crimson cloth
                        else:
                            cr, cg, cb = 0.14, 0.04, 0.05  # Dark border trim

                    # 3. Fierce Samurai Eyebrows (angled upward toward temples)
                    if cy < -6.5 and 1.0 <= abs(cx) <= 4.7:
                        brow_z = 155.30 + (abs(cx) - 1.0) * 0.22
                        if abs(cz - brow_z) < 0.46:
                            cr, cg, cb = 0.05, 0.04, 0.05

                    # 4. Human Eyes (Dark brown irises, black pupils, catchlight, natural sclera)
                    if cy < -6.5 and 153.4 <= cz <= 155.2:
                        for ex_c, ez_c in [(2.55, 154.32), (-3.05, 154.46)]:
                            dx_e = (cx - ex_c) / 1.35
                            dz_e = (cz - ez_c) / 0.52
                            dist_e = math.sqrt(dx_e * dx_e + dz_e * dz_e)
                            if dist_e < 1.05:
                                if dist_e > 0.82 or cz > ez_c + 0.30:
                                    cr, cg, cb = 0.06, 0.04, 0.04  # Eyelash rim
                                else:
                                    iris_d = math.sqrt(((cx - ex_c) / 0.62) ** 2 + ((cz - ez_c) / 0.45) ** 2)
                                    if iris_d < 0.38:
                                        if cx > ex_c + 0.05 and cz > ez_c + 0.05 and iris_d < 0.24:
                                            cr, cg, cb = 0.94, 0.94, 0.96  # Specular catchlight
                                        else:
                                            cr, cg, cb = 0.04, 0.03, 0.03  # Pupil
                                    elif iris_d < 0.88:
                                        cr, cg, cb = 0.24, 0.13, 0.07  # Warm dark brown iris
                                    else:
                                        cr, cg, cb = 0.88, 0.85, 0.82  # Natural human sclera

                    # 5. Samurai Mustache & Chin Goatee Beard
                    if cy < -6.5:
                        arch_z = 150.42 - (abs(cx) ** 1.35) * 0.22
                        if abs(cx) < 2.80 and abs(cz - arch_z) < 0.40:
                            cr, cg, cb = 0.07, 0.06, 0.07  # Mustache
                        elif 144.0 <= cz <= 148.5 and abs(cx) < 2.30:
                            if cz < 146.3 or abs(cx) < 0.72 or abs(cx) > 1.60:
                                cr, cg, cb = 0.07, 0.06, 0.07  # Goatee & soul patch
                        elif 144.5 <= cz <= 149.2 and 2.30 <= abs(cx) <= 5.0 and cy < -4.5:
                            # Subtle jawline stubble
                            cr, cg, cb = cr * 0.76, cg * 0.74, cb * 0.75

                    # 6. Natural Lips (cz in 149.0 .. 149.85, abs(cx) < 1.95)
                    if cy < -8.2 and 149.0 <= cz <= 149.85 and abs(cx) < 1.95:
                        if abs(cz - 149.45) < 0.12:
                            cr, cg, cb = 0.24, 0.12, 0.11
                        else:
                            cr, cg, cb = 0.58, 0.35, 0.32

                    # 7. Battle Scar across left cheek
                    if cy < -6.8 and -3.8 <= cx <= -1.8 and 149.5 <= cz <= 153.5:
                        scar_x = -2.8 + (cz - 151.5) * 0.18
                        if abs(cx - scar_x) < 0.18:
                            cr, cg, cb = min(1.0, cr * 0.85 + 0.12), cg * 0.74, cb * 0.72

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
        rgb0 = vert_orig_rgb[v0]
        for i in range(1, len(loops) - 1):
            vi1 = su_mesh.data.vertices[poly.vertices[i]].index
            vi2 = su_mesh.data.vertices[poly.vertices[i + 1]].index
            uv1 = uv_data[loops[i]].uv
            uv2 = uv_data[loops[i + 1]].uv
            fill_uv_triangle(
                uv0, uv1, uv2,
                p0, v_canon[vi1], v_canon[vi2],
                l0, vert_orig_lum[vi1], vert_orig_lum[vi2],
                rgb0, vert_orig_rgb[vi1], vert_orig_rgb[vi2]
            )

    # Fill the reserved bottom strip Y in [0..64] for the Topknot (jet black) and Crimson Cord
    for ty in range(0, 64):
        for tx in range(0, 512):
            d_idx = (ty * tw + tx) * 4
            dst_px[d_idx] = 0.05
            dst_px[d_idx + 1] = 0.05
            dst_px[d_idx + 2] = 0.07
            dst_px[d_idx + 3] = 1.0
        for tx in range(512, 1024):
            d_idx = (ty * tw + tx) * 4
            dst_px[d_idx] = 0.65
            dst_px[d_idx + 1] = 0.08
            dst_px[d_idx + 2] = 0.10
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

    # Step 6: Translate the sculpted head to the exact center of the Samurai Collar Opening!
    OFFSET = Vector((-4.2, -7.2, -3.8))
    for v in su_mesh.data.vertices:
        v.co += OFFSET

    # Step 7: Add the Obsidian Inner Neck Seal & Samurai Chonmage Topknot with UVs in the reserved bottom strip (Y = 0.03)
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
                loop[uv_layer].uv = (0.25, 0.03)  # Reserved jet-black/obsidian strip

    # 7b. Samurai Chonmage Topknot & Crimson Cord
    cord_rings = []
    for rz, rad in [(159.8, 2.2), (161.2, 1.8), (162.4, 1.9)]:
        ring = []
        for s in range(14):
            ang = 2.0 * math.pi * s / 14
            v = bm.verts.new((-4.2 + math.cos(ang) * rad, -4.2 + math.sin(ang) * rad, rz))
            ring.append(v)
        cord_rings.append(ring)
    for r_i in range(len(cord_rings) - 1):
        r0, r1 = cord_rings[r_i], cord_rings[r_i + 1]
        for s in range(14):
            sn = (s + 1) % 14
            f = bm.faces.new((r0[s], r0[sn], r1[sn], r1[s]))
            f.smooth = True
            for loop in f.loops:
                loop[uv_layer].uv = (0.75, 0.03)  # Reserved crimson strip

    bun_rings = []
    for cy_off, cz_off, rx_r, rz_r in [
        (-3.2, 162.6, 2.1, 1.8),
        (-5.2, 163.6, 2.4, 2.0),
        (-7.8, 164.0, 2.2, 1.7),
        (-9.8, 163.5, 1.5, 1.2),
        (-10.6, 163.0, 0.4, 0.4),
    ]:
        ring = []
        for s in range(14):
            ang = 2.0 * math.pi * s / 14
            v = bm.verts.new((-4.2 + math.cos(ang) * rx_r, cy_off, cz_off + math.sin(ang) * rz_r))
            ring.append(v)
        bun_rings.append(ring)
    for r_i in range(len(bun_rings) - 1):
        r0, r1 = bun_rings[r_i], bun_rings[r_i + 1]
        for s in range(14):
            sn = (s + 1) % 14
            f = bm.faces.new((r0[s], r0[sn], r1[sn], r1[s]))
            f.smooth = True
            for loop in f.loops:
                loop[uv_layer].uv = (0.25, 0.03)  # Reserved jet-black strip

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
            continue  # Keep the katana!
        hw = sum(g.weight for g in v.groups if g.group in head_vgs)
        if (hw > 0.25 and v.co.z > 140.5) or (v.co.z > 142.5 and -23.5 <= v.co.x <= 12.5 and -25.0 <= v.co.y <= 10.0):
            v.select = True

    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.delete(type='VERT')
    bpy.ops.object.mode_set(mode='OBJECT')

    # Apply the new smooth Deep Crimson, Obsidian & Brass-Gold Samurai Armor material
    armor_mat = create_crimson_samurai_armor_material(sh_mesh)
    sh_mesh.data.materials.clear()
    sh_mesh.data.materials.append(armor_mat)

    # 3. Import the human head mesh, remove all collar polygons, reshape into Kaito Kurogane, Smart UV Project, and bake custom texture
    bpy.ops.import_scene.gltf(filepath=SURYA_GLB)
    su_arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE' and o != sh_arm)
    su_mesh = next(o for o in bpy.data.objects if o.type == 'MESH' and o != sh_mesh)
    su_arm.data.pose_position = 'REST'

    for act in list(bpy.data.actions):
        if act.name.startswith("Armature.001") or "Charged_Ground_Slam" in act.name:
            bpy.data.actions.remove(act)

    sculpt_and_bake_samurai_head(su_mesh)

    # Join su_mesh into sh_mesh
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
