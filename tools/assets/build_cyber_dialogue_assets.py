"""Build Axiom Force AXIOM-7 Cyber Holographic Dialogue Terminal Assets in Blender 5.2.

Generates:
1. assets/models/menu/axiom7_speech_terminal.glb - 3D Sci-Fi Holographic Comm Terminal with
   chamfered armor frame, glowing neon light-rails, corner brackets, and directional holo-emitter nozzle.
2. assets/textures/cyber_speech_frame.png - High-tech 9-slice / panel texture with chamfered corners,
   neon cyan glow rails, and corner brackets.
3. assets/textures/axiom7_avatar_icon.png - Holographic AXIOM-7 AI core emblem with optic reticle and telemetry.
4. assets/textures/cyber_speech_pointer.png - Directional energy chevron array pointing left toward AXIOM-7.

Run headless:
  & "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup --python tools/assets/build_cyber_dialogue_assets.py
"""
import bpy
import bmesh
import math
import zlib
import struct
import os
from pathlib import Path
from mathutils import Vector, Euler

ROOT = Path(__file__).resolve().parents[2]
MODEL_DIR = ROOT / "assets/models/menu"
TEXTURE_DIR = ROOT / "assets/textures"
MODEL_DIR.mkdir(parents=True, exist_ok=True)
TEXTURE_DIR.mkdir(parents=True, exist_ok=True)

MATERIALS = {}

# ─── 1. PBR Material Helpers ──────────────────────────────────────────────────
def make_pbr_material(name, base_color, metallic=0.0, roughness=0.4, emission_color=(0, 0, 0), emission_strength=0.0, alpha=1.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.diffuse_color = (*base_color, alpha)
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs['Base Color'].default_value = (*base_color, 1.0)
        bsdf.inputs['Metallic'].default_value = metallic
        bsdf.inputs['Roughness'].default_value = roughness
        if 'Alpha' in bsdf.inputs and alpha < 1.0:
            bsdf.inputs['Alpha'].default_value = alpha
            if hasattr(mat, 'blend_method'):
                mat.blend_method = 'BLEND'
        if emission_strength > 0:
            if 'Emission Color' in bsdf.inputs:
                bsdf.inputs['Emission Color'].default_value = (*emission_color, 1.0)
                bsdf.inputs['Emission Strength'].default_value = emission_strength
    return mat

def init_palette():
    MATERIALS.clear()
    MATERIALS.update({
        'armor':      make_pbr_material('Mat_TerminalArmor', (0.018, 0.024, 0.035), metallic=0.88, roughness=0.25),
        'titanium':   make_pbr_material('Mat_TerminalTitanium', (0.15, 0.19, 0.25), metallic=0.92, roughness=0.20),
        'trim':       make_pbr_material('Mat_TerminalTrim', (0.35, 0.42, 0.50), metallic=0.95, roughness=0.15),
        'cyan':       make_pbr_material('Mat_NeonCyan', (0.0, 0.94, 1.0), metallic=0.10, roughness=0.10,
                                        emission_color=(0.0, 0.94, 1.0), emission_strength=5.5),
        'emerald':    make_pbr_material('Mat_NeonEmerald', (0.0, 1.0, 0.45), metallic=0.10, roughness=0.10,
                                        emission_color=(0.0, 1.0, 0.45), emission_strength=5.0),
        'gold':       make_pbr_material('Mat_NeonGold', (1.0, 0.70, 0.05), metallic=0.15, roughness=0.20,
                                        emission_color=(1.0, 0.70, 0.05), emission_strength=4.0),
        'white':      make_pbr_material('Mat_NeonWhite', (0.85, 0.98, 1.0), metallic=0.10, roughness=0.10,
                                        emission_color=(0.85, 0.98, 1.0), emission_strength=6.0),
        'holo_visor': make_pbr_material('Mat_HoloVisor', (0.02, 0.12, 0.22), metallic=0.10, roughness=0.08,
                                        emission_color=(0.0, 0.60, 0.85), emission_strength=1.5, alpha=0.65),
    })

def reset_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for block in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for item in list(block):
            block.remove(item, do_unlink=True)
    init_palette()

def finish_mesh(obj, name, mat_key):
    obj.name = name
    if mat_key in MATERIALS:
        obj.data.materials.append(MATERIALS[mat_key])
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

# ─── 2. Model 3D Holographic Speech Terminal ──────────────────────────────────
def build_3d_speech_terminal():
    reset_scene()
    print("--> Modeling 3D Holographic Speech Terminal...")

    # Dimensions
    width, height, depth = 3.2, 1.9, 0.08
    chamfer = 0.24

    # 1. Outer Chamfered Armor Bezel
    # Create chamfered octagonal plane/prism
    mesh = bpy.data.meshes.new("TerminalArmor_Mesh")
    bm = bmesh.new()

    hw, hh = width / 2.0, height / 2.0
    hd = depth / 2.0

    # 8-corner outline with cyber chamfers (larger top-right and bottom-left chamfers)
    c_tl, c_tr, c_br, c_bl = 0.12, 0.32, 0.14, 0.32
    verts_2d = [
        (-hw + c_tl, hh),
        (hw - c_tr, hh),
        (hw, hh - c_tr),
        (hw, -hh + c_br),
        (hw - c_br, -hh),
        (-hw + c_bl, -hh),
        (-hw, -hh + c_bl),
        (-hw, hh - c_tl),
    ]

    front_verts = [bm.verts.new((x, y, hd)) for x, y in verts_2d]
    back_verts  = [bm.verts.new((x, y, -hd)) for x, y in verts_2d]
    bm.verts.ensure_lookup_table()

    # Front and back faces
    bm.faces.new(front_verts)
    bm.faces.new(reversed(back_verts))

    # Side bridge faces
    n = len(verts_2d)
    for i in range(n):
        v1, v2 = front_verts[i], front_verts[(i + 1) % n]
        v3, v4 = back_verts[(i + 1) % n], back_verts[i]
        bm.faces.new([v1, v2, v3, v4])

    bm.to_mesh(mesh)
    bm.free()

    obj_armor = bpy.data.objects.new("Terminal_OuterArmor", mesh)
    bpy.context.collection.objects.link(obj_armor)
    finish_mesh(obj_armor, "Terminal_OuterArmor", 'armor')

    # 2. Central Holographic Visor Screen (Recessed slightly, translucent cyan glass)
    s_w, s_h = width - 0.28, height - 0.28
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, 0.01))
    screen = bpy.context.object
    screen.scale = (s_w, s_h, 0.02)
    finish_mesh(screen, "Terminal_HoloVisor", 'holo_visor')

    # 3. Glowing Neon Perimeter Rail (Cyan light-pipe running around screen perimeter)
    bpy.ops.mesh.primitive_torus_add(
        major_radius=1.0, minor_radius=0.015, major_segments=4, minor_segments=8,
        location=(0, 0, 0.045)
    )
    neon_frame = bpy.context.object
    neon_frame.rotation_euler = (0, 0, math.pi / 4.0)
    neon_frame.scale = (s_w * 0.71, s_h * 0.71, 1.0)
    finish_mesh(neon_frame, "Terminal_NeonFrame", 'cyan')

    # 4. Sci-Fi Corner Tech Brackets (4 corners)
    bracket_coords = [
        (-hw + 0.10,  hh - 0.10, 0.045,  1,  1),
        ( hw - 0.18,  hh - 0.18, 0.045, -1,  1),
        ( hw - 0.10, -hh + 0.10, 0.045, -1, -1),
        (-hw + 0.18, -hh + 0.18, 0.045,  1, -1),
    ]
    for i, (bx, by, bz, sx, sy) in enumerate(bracket_coords):
        # L-shaped cyber bracket
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx, by, bz))
        b_obj = bpy.context.object
        b_obj.scale = (0.24, 0.04, 0.03)
        finish_mesh(b_obj, f"CornerBracket_H_{i}", 'emerald' if i % 2 == 0 else 'cyan')

        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx, by, bz))
        b_obj_v = bpy.context.object
        b_obj_v.scale = (0.04, 0.24, 0.03)
        finish_mesh(b_obj_v, f"CornerBracket_V_{i}", 'emerald' if i % 2 == 0 else 'cyan')

        # Hex bolt accent
        bpy.ops.mesh.primitive_cylinder_add(
            vertices=6, radius=0.035, depth=0.04, location=(bx - sx * 0.04, by - sy * 0.04, bz + 0.01)
        )
        finish_mesh(bpy.context.object, f"HexBolt_{i}", 'titanium')

    # 5. Left-Side Holographic Transmission Emitter Array
    # Points left towards AXIOM-7's position in world space
    emitter_x = -hw - 0.08
    for j in range(3):
        ey = 0.25 - j * 0.25
        espan = 0.28 - j * 0.04
        # Angled energy emitter rib
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(emitter_x - j * 0.08, ey, 0.02))
        rib = bpy.context.object
        rib.scale = (0.16, 0.035, 0.04)
        rib.rotation_euler = (0, 0, math.radians(20 if j == 0 else (-20 if j == 2 else 0)))
        finish_mesh(rib, f"TransmissionRib_{j}", 'cyan')

    # Emitter Core Node
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=8, radius=0.07, depth=0.08, location=(emitter_x - 0.22, 0.0, 0.02)
    )
    emitter_core = bpy.context.object
    emitter_core.rotation_euler = (0, math.pi / 2.0, 0)
    finish_mesh(emitter_core, "EmitterCoreNode", 'emerald')

    # Conical energy projection cone pointing toward AXIOM-7 (-X)
    bpy.ops.mesh.primitive_cone_add(
        vertices=16, radius1=0.02, radius2=0.18, depth=0.35, location=(emitter_x - 0.42, 0.0, 0.02)
    )
    beam_cone = bpy.context.object
    beam_cone.rotation_euler = (0, -math.pi / 2.0, 0)
    finish_mesh(beam_cone, "EmitterProjectionBeam", 'cyan')

    # 6. Top Telemetry Sensor Bar & Dual Micro-Antennas
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, hh + 0.04, 0.02))
    badge = bpy.context.object
    badge.scale = (0.90, 0.08, 0.04)
    finish_mesh(badge, "TelemetryBadgePlate", 'titanium')

    # Status LED pips on badge
    for k in range(5):
        px = -0.32 + k * 0.16
        bpy.ops.mesh.primitive_cylinder_add(
            vertices=12, radius=0.02, depth=0.025, location=(px, hh + 0.05, 0.04)
        )
        finish_mesh(bpy.context.object, f"StatusLed_{k}", 'emerald' if k < 4 else 'gold')

    # Dual Micro-Antenna pins
    for side in (-1, 1):
        bpy.ops.mesh.primitive_cylinder_add(
            vertices=8, radius=0.012, depth=0.22, location=(side * 0.48, hh + 0.12, 0.02)
        )
        finish_mesh(bpy.context.object, f"MicroAntenna_{side}", 'trim')
        bpy.ops.mesh.primitive_uv_sphere_add(
            radius=0.025, location=(side * 0.48, hh + 0.23, 0.02)
        )
        finish_mesh(bpy.context.object, f"AntennaTip_{side}", 'cyan')

    # Export to GLB
    glb_path = MODEL_DIR / "axiom7_speech_terminal.glb"
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.gltf(
        filepath=str(glb_path),
        export_format='GLB',
        use_selection=True,
        export_apply=True,
        export_yup=True,
        export_animations=False
    )
    print(f"--> Exported 3D Speech Terminal to {glb_path} ({round(glb_path.stat().st_size / 1024, 1)} KB)")


# ─── 3. Pure Python Standard-Library PNG Texture Generators ───────────────────
def write_png(filepath, width, height, rgba_data):
    """Encodes raw RGBA bytes into a standard PNG file using zlib (no external dependencies)."""
    # Build raw scanlines with filter byte 0
    raw_scanlines = bytearray()
    for y in range(height):
        raw_scanlines.append(0)  # Filter type None
        start = y * width * 4
        end = start + width * 4
        raw_scanlines.extend(rgba_data[start:end])

    compressed = zlib.compress(bytes(raw_scanlines), 9)

    def make_chunk(chunk_type, data):
        length = len(data)
        crc = zlib.crc32(chunk_type + data) & 0xffffffff
        return struct.pack(">I", length) + chunk_type + data + struct.pack(">I", crc)

    ihdr_data = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)
    png_bytes = (
        b"\x89PNG\r\n\x1a\n" +
        make_chunk(b"IHDR", ihdr_data) +
        make_chunk(b"IDAT", compressed) +
        make_chunk(b"IEND", b"")
    )

    with open(filepath, "wb") as f:
        f.write(png_bytes)
    print(f"--> Generated PNG texture: {filepath} ({width}x{height}, {round(len(png_bytes)/1024, 1)} KB)")


def generate_cyber_speech_frame():
    """Generates a high-tech 9-slice cyber speech frame (512x320) with chamfered corners,
    dual neon cyan/emerald perimeter glow rails, micro-grid markings, and corner brackets."""
    W, H = 512, 320
    pixels = bytearray(W * H * 4)

    # Margins and chamfer size
    c_tl = 20  # Top-left chamfer
    c_tr = 44  # Top-right cut chamfer
    c_br = 22  # Bottom-right chamfer
    c_bl = 44  # Bottom-left cut chamfer
    border_thick = 4.0

    def point_in_panel(x, y):
        # Checks if (x,y) is inside the chamfered polygon
        if x < 0 or x >= W or y < 0 or y >= H:
            return False
        # Top-left cut: x + y >= c_tl
        if x + y < c_tl:
            return False
        # Top-right cut: (W - 1 - x) + y >= c_tr
        if (W - 1 - x) + y < c_tr:
            return False
        # Bottom-right cut: (W - 1 - x) + (H - 1 - y) >= c_br
        if (W - 1 - x) + (H - 1 - y) < c_br:
            return False
        # Bottom-left cut: x + (H - 1 - y) >= c_bl
        if x + (H - 1 - y) < c_bl:
            return False
        return True

    def dist_to_boundary(x, y):
        # Distance to outer edges
        d_left = x
        d_right = W - 1 - x
        d_top = y
        d_bottom = H - 1 - y
        d_tl = (x + y - c_tl) * 0.7071
        d_tr = ((W - 1 - x) + y - c_tr) * 0.7071
        d_br = ((W - 1 - x) + (H - 1 - y) - c_br) * 0.7071
        d_bl = (x + (H - 1 - y) - c_bl) * 0.7071
        return min(d_left, d_right, d_top, d_bottom, d_tl, d_tr, d_br, d_bl)

    for y in range(H):
        for x in range(W):
            idx = (y * W + x) * 4
            if not point_in_panel(x, y):
                # Transparent outside
                continue

            d = dist_to_boundary(x, y)

            # Outer glowing border (0 <= d <= 5)
            if d <= border_thick:
                # Cyan neon edge
                edge_intensity = 1.0 - (d / border_thick) * 0.3
                pixels[idx]     = int(0   * edge_intensity)
                pixels[idx + 1] = int(240 * edge_intensity)
                pixels[idx + 2] = int(255 * edge_intensity)
                pixels[idx + 3] = int(240 * edge_intensity)
            elif d <= border_thick + 4:
                # Recessed darker accent ring
                pixels[idx]     = 10
                pixels[idx + 1] = 35
                pixels[idx + 2] = 55
                pixels[idx + 3] = 200
            elif d <= border_thick + 6:
                # Inner thin neon emerald accent rail
                pixels[idx]     = 0
                pixels[idx + 1] = 230
                pixels[idx + 2] = 130
                pixels[idx + 3] = 160
            else:
                # Dark cyber tinted glass interior
                # Subtle vertical gradient and horizontal scanline feel
                base_lum = 12 + int((y / H) * 10)
                grid_lum = 6 if (x % 32 == 0 or y % 32 == 0) else 0
                pixels[idx]     = min(255, 6  + grid_lum)
                pixels[idx + 1] = min(255, 12 + grid_lum + (base_lum // 2))
                pixels[idx + 2] = min(255, 24 + grid_lum + base_lum)
                pixels[idx + 3] = 220  # High opacity glass

            # High-tech corner bracket highlights
            # Top-left bracket
            if (x < 36 and y < 36) and (d <= 7):
                pixels[idx]     = 0
                pixels[idx + 1] = 255
                pixels[idx + 2] = 180
                pixels[idx + 3] = 255
            # Top-right bracket
            if (x > W - 60 and y < 60) and (d <= 7):
                pixels[idx]     = 0
                pixels[idx + 1] = 240
                pixels[idx + 2] = 255
                pixels[idx + 3] = 255
            # Bottom-left bracket
            if (x < 60 and y > H - 60) and (d <= 7):
                pixels[idx]     = 0
                pixels[idx + 1] = 255
                pixels[idx + 2] = 140
                pixels[idx + 3] = 255
            # Bottom-right bracket
            if (x > W - 36 and y > H - 36) and (d <= 7):
                pixels[idx]     = 0
                pixels[idx + 1] = 240
                pixels[idx + 2] = 255
                pixels[idx + 3] = 255

    write_png(TEXTURE_DIR / "cyber_speech_frame.png", W, H, pixels)


def generate_axiom7_avatar_icon():
    """Generates an ultra-crisp holographic AXIOM-7 AI Core Avatar (128x128)."""
    S = 128
    pixels = bytearray(S * S * 4)
    cx, cy = S / 2.0, S / 2.0

    for y in range(S):
        for x in range(S):
            idx = (y * S + x) * 4
            dx = x - cx
            dy = y - cy
            dist = math.sqrt(dx * dx + dy * dy)
            angle = math.atan2(dy, dx)

            # Circular base clipping
            if dist > 60:
                continue

            # Outer targeting reticle ring (dist ~ 54 to 58)
            if 54 <= dist <= 58:
                # Segmented gaps at 4 cardinals
                cardinal_gap = abs(dx) < 6 or abs(dy) < 6
                if not cardinal_gap:
                    pixels[idx]     = 0
                    pixels[idx + 1] = 240
                    pixels[idx + 2] = 255
                    pixels[idx + 3] = 230
                    continue

            # Secondary tech ring (dist ~ 44 to 47)
            if 44 <= dist <= 47:
                # 8-segment tick marks
                ticks = int((angle + math.pi) / (math.pi / 4.0)) % 2 == 0
                if ticks:
                    pixels[idx]     = 0
                    pixels[idx + 1] = 255
                    pixels[idx + 2] = 140
                    pixels[idx + 3] = 200
                    continue

            # Inner Hexagonal Chassis Core (AXIOM-7 eye housing)
            # Distance in hexagonal metric
            hex_dist = max(abs(dx) * 0.866025 + abs(dy) * 0.5, abs(dy))
            if hex_dist <= 28:
                if hex_dist >= 24:
                    # Hex rim border
                    pixels[idx]     = 0
                    pixels[idx + 1] = 255
                    pixels[idx + 2] = 220
                    pixels[idx + 3] = 255
                else:
                    # Dark cyber optic socket
                    pixels[idx]     = 8
                    pixels[idx + 1] = 16
                    pixels[idx + 2] = 32
                    pixels[idx + 3] = 230

            # Glowing Central Optic Pupil (dist <= 14)
            if dist <= 14:
                glow = 1.0 - (dist / 14.0)
                # Intense white-cyan core with cyan aura
                if dist <= 6:
                    pixels[idx]     = 220
                    pixels[idx + 1] = 255
                    pixels[idx + 2] = 255
                    pixels[idx + 3] = 255
                else:
                    pixels[idx]     = int(0   * glow)
                    pixels[idx + 1] = int(240 * glow)
                    pixels[idx + 2] = int(255 * glow)
                    pixels[idx + 3] = int(240 * glow)

    write_png(TEXTURE_DIR / "axiom7_avatar_icon.png", S, S, pixels)


def generate_cyber_speech_pointer():
    """Generates a directional holographic energy pointer / chevron tail (128x128)
    pointing to the left towards AXIOM-7."""
    W, H = 128, 128
    pixels = bytearray(W * H * 4)

    # 3 nested energy chevrons: <<< pointing LEFT
    chevrons = [
        (24, 64, 40, 28, (0, 255, 140)),   # Tip chevron (Emerald)
        (56, 64, 40, 34, (0, 240, 255)),   # Mid chevron (Cyan)
        (88, 64, 40, 40, (0, 180, 255)),   # Base chevron (Deep Cyan)
    ]

    for y in range(H):
        for x in range(W):
            idx = (y * W + x) * 4
            for cx, cy, span_x, span_y, color in chevrons:
                # Equation of < : x - cx = abs(y - cy)
                dy = abs(y - cy)
                if dy <= span_y:
                    target_x = cx + dy * 0.75
                    dx = abs(x - target_x)
                    if dx <= 3.5:
                        fade = 1.0 - (dx / 3.5)
                        alpha = int(255 * fade * (1.0 - dy / span_y * 0.3))
                        pixels[idx]     = color[0]
                        pixels[idx + 1] = color[1]
                        pixels[idx + 2] = color[2]
                        pixels[idx + 3] = max(pixels[idx + 3], alpha)

    write_png(TEXTURE_DIR / "cyber_speech_pointer.png", W, H, pixels)


if __name__ == "__main__":
    print("==========================================================")
    print("   AXIOM FORCE — Building Cyber Dialogue Terminal Assets  ")
    print("==========================================================")
    build_3d_speech_terminal()
    generate_cyber_speech_frame()
    generate_axiom7_avatar_icon()
    generate_cyber_speech_pointer()
    print("==========================================================")
    print("   ALL DIALOGUE ASSETS GENERATED SUCCESSFULLY!            ")
    print("==========================================================")
