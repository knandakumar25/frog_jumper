"""Build Axiom Force 3D Command Deck & Cyber City Skyline Assets in Blender 5.2.

Generates:
1. assets/models/menu/command_deck_interior.glb - High-tech Resistance Observation Bridge
   with polished composite floor, panoramic cyber-steel window frame, ceiling rafters,
   peripheral console bays, and subtle observation glass pane.
2. assets/models/menu/cyber_city_skyline.glb - Multi-tiered 3D cyberpunk metropolis skyline
   with stepped corporate spires, emissive window matrices, skybridges, antenna masts,
   and distant urban horizon silhouettes.

Run headless:
  & "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup --python tools/assets/build_command_deck_assets.py
"""
import bpy
import bmesh
import math
from pathlib import Path
from mathutils import Vector, Euler

ROOT = Path(__file__).resolve().parents[2]
MODEL_DIR = ROOT / "assets/models/menu"
MODEL_DIR.mkdir(parents=True, exist_ok=True)

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
        # Command Deck Interior Materials
        'deck_dark':      make_pbr_material('Mat_DeckDarkArmor', (0.015, 0.02, 0.03), metallic=0.75, roughness=0.35),
        'deck_steel':     make_pbr_material('Mat_DeckSteelTrim', (0.12, 0.16, 0.22), metallic=0.85, roughness=0.30),
        'deck_grate':     make_pbr_material('Mat_DeckGrate', (0.06, 0.08, 0.10), metallic=0.90, roughness=0.40),
        'hazard_stripe':  make_pbr_material('Mat_HazardStripe', (0.85, 0.70, 0.08), metallic=0.15, roughness=0.50),
        'glass_pane':     make_pbr_material('Mat_ObservationGlass', (0.01, 0.03, 0.06), metallic=0.05, roughness=0.05, alpha=0.08),
        'console_screen': make_pbr_material('Mat_ConsoleScreen', (0.0, 0.35, 0.45), metallic=0.1, roughness=0.1,
                                            emission_color=(0.0, 0.85, 1.0), emission_strength=4.0),

        # Neon Emissives (Command Deck & Exterior)
        'neon_cyan':      make_pbr_material('Mat_NeonCyan', (0.0, 0.94, 1.0), metallic=0.10, roughness=0.10,
                                            emission_color=(0.0, 0.94, 1.0), emission_strength=5.5),
        'neon_emerald':   make_pbr_material('Mat_NeonEmerald', (0.0, 1.0, 0.45), metallic=0.10, roughness=0.10,
                                            emission_color=(0.0, 1.0, 0.45), emission_strength=5.0),
        'neon_amber':     make_pbr_material('Mat_NeonAmber', (1.0, 0.65, 0.05), metallic=0.10, roughness=0.10,
                                            emission_color=(1.0, 0.65, 0.05), emission_strength=4.8),
        'neon_magenta':   make_pbr_material('Mat_NeonMagenta', (1.0, 0.08, 0.55), metallic=0.10, roughness=0.10,
                                            emission_color=(1.0, 0.08, 0.55), emission_strength=5.2),
        'beacon_red':     make_pbr_material('Mat_BeaconRed', (1.0, 0.05, 0.05), metallic=0.10, roughness=0.10,
                                            emission_color=(1.0, 0.05, 0.05), emission_strength=6.5),

        # Exterior Skyline Materials (Matte dark architectural composite for realistic night contrast)
        'spire_dark':     make_pbr_material('Mat_SpireDark', (0.015, 0.020, 0.030), metallic=0.18, roughness=0.75),
        'spire_metal':    make_pbr_material('Mat_SpireMetal', (0.035, 0.045, 0.060), metallic=0.25, roughness=0.65),
        'spire_trim':     make_pbr_material('Mat_SpireTrim', (0.07, 0.09, 0.13), metallic=0.45, roughness=0.45),
        'window_cyan':    make_pbr_material('Mat_WindowCyan', (0.0, 0.35, 0.55), metallic=0.1, roughness=0.2,
                                            emission_color=(0.0, 0.92, 1.0), emission_strength=4.6),
        'window_amber':   make_pbr_material('Mat_WindowAmber', (0.5, 0.3, 0.05), metallic=0.1, roughness=0.2,
                                            emission_color=(1.0, 0.70, 0.15), emission_strength=4.4),
        'window_emerald': make_pbr_material('Mat_WindowEmerald', (0.05, 0.45, 0.2), metallic=0.1, roughness=0.2,
                                            emission_color=(0.1, 0.98, 0.45), emission_strength=4.6),
        'horizon_fog':    make_pbr_material('Mat_HorizonSpire', (0.010, 0.014, 0.022), metallic=0.15, roughness=0.85),
    })

def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def finish_mesh(obj, name, mat_key):
    obj.name = name
    if mat_key in MATERIALS:
        if obj.data.materials:
            obj.data.materials[0] = MATERIALS[mat_key]
        else:
            obj.data.materials.append(MATERIALS[mat_key])


# ─── 2. Build Command Deck Interior Architecture ─────────────────────────────
def build_command_deck_interior():
    clear_scene()
    init_palette()

    print("--> Modeling Command Deck Interior...")

    # Y = -1.35 is deck floor surface level.
    # Window wall is positioned at Z = -3.8.
    # Camera is at (0, 0.1, 4.5).
    floor_y = -1.35
    window_z = -3.8
    header_y = 2.45

    # 1. Main Deck Floor Platform
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, floor_y - 0.15, 0.5))
    floor_main = bpy.context.object
    floor_main.scale = (8.0, 0.30, 9.0) # spans X: -4 to 4, Z: -4 to 5
    finish_mesh(floor_main, "Deck_Floor_Base", 'deck_dark')

    # Lateral raised walkways (Left and Right)
    for side in (-1, 1):
        wx = side * 3.4
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(wx, floor_y + 0.08, 0.5))
        walkway = bpy.context.object
        walkway.scale = (1.4, 0.16, 9.0)
        finish_mesh(walkway, f"Deck_Walkway_{side}", 'deck_steel')

        # Hazard stripe safety trim along walkway step
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(wx - side * 0.72, floor_y + 0.08, 0.5))
        h_trim = bpy.context.object
        h_trim.scale = (0.06, 0.18, 9.0)
        finish_mesh(h_trim, f"Deck_HazardStrip_{side}", 'hazard_stripe')

    # Center Floor Conduits & Light Runners (Cyan glowing strips leading to window)
    for offset_x in (-1.1, 1.1):
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(offset_x, floor_y + 0.01, 0.5))
        runner = bpy.context.object
        runner.scale = (0.08, 0.03, 8.6)
        finish_mesh(runner, f"Floor_LightRunner_{offset_x}", 'neon_cyan')

    # Transverse accent light strips across floor
    for tz in (-2.8, -0.8, 1.2, 3.2):
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, floor_y + 0.01, tz))
        runner_t = bpy.context.object
        runner_t.scale = (2.1, 0.025, 0.06)
        finish_mesh(runner_t, f"Floor_TransverseStrip_{tz}", 'neon_cyan')

    # Window Perimeter Sill Runner (glows at base of glass)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, floor_y + 0.05, window_z + 0.12))
    sill_light = bpy.context.object
    sill_light.scale = (6.8, 0.04, 0.08)
    finish_mesh(sill_light, "WindowSill_LightRunner", 'neon_emerald')

    # 2. Panoramic Observation Window Frame
    # Bottom heavy sill
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, floor_y + 0.10, window_z))
    sill = bpy.context.object
    sill.scale = (7.6, 0.35, 0.35)
    finish_mesh(sill, "Observation_Sill", 'deck_steel')

    # Top reinforced ceiling header beam
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, header_y, window_z))
    header = bpy.context.object
    header.scale = (7.6, 0.40, 0.45)
    finish_mesh(header, "Observation_Header", 'deck_steel')

    # Overhead linear lighting rail along header
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, header_y - 0.22, window_z + 0.10))
    header_light = bpy.context.object
    header_light.scale = (6.6, 0.05, 0.08)
    finish_mesh(header_light, "Observation_Header_Light", 'neon_cyan')

    # Left & Right Outer Bulkhead Columns (Slanted A-Pillars)
    for side in (-1, 1):
        cx = side * 3.4
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(cx, (floor_y + header_y) / 2.0, window_z))
        pillar = bpy.context.object
        pillar.scale = (0.70, (header_y - floor_y) + 0.4, 0.50)
        pillar.rotation_euler = (0, 0, -side * 0.06) # subtle inward slant
        finish_mesh(pillar, f"Observation_Pillar_{side}", 'deck_steel')

        # Vertical neon accent strip on pillar face
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(cx - side * 0.28, (floor_y + header_y) / 2.0, window_z + 0.26))
        p_light = bpy.context.object
        p_light.scale = (0.05, (header_y - floor_y) * 0.85, 0.03)
        finish_mesh(p_light, f"Pillar_LightStrip_{side}", 'neon_cyan')

    # 3. Tinted Panoramic Observation Glass Pane (Very subtle 8% alpha tint)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, (floor_y + header_y) / 2.0, window_z - 0.02))
    glass = bpy.context.object
    glass.scale = (7.0, (header_y - floor_y), 0.02)
    finish_mesh(glass, "Observation_Glass_Pane", 'glass_pane')

    # 4. Ceiling Canopy Rafters
    for rz in (-2.4, -0.6, 1.2, 3.0):
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, header_y + 0.15, rz))
        rafter = bpy.context.object
        rafter.scale = (7.6, 0.25, 0.32)
        finish_mesh(rafter, f"Ceiling_Rafter_{rz}", 'deck_dark')

        # Recessed ceiling light panel
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, header_y + 0.05, rz))
        c_light = bpy.context.object
        c_light.scale = (4.0, 0.04, 0.12)
        finish_mesh(c_light, f"Ceiling_LightPanel_{rz}", 'neon_cyan')

    # Ceiling main roof slab
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, header_y + 0.35, 0.5))
    roof = bpy.context.object
    roof.scale = (8.0, 0.25, 9.0)
    finish_mesh(roof, "Deck_Roof_Slab", 'deck_dark')

    # 5. Side Wall Bulkhead Bays & Cable Conduits
    for side in (-1, 1):
        wx = side * 4.0
        # Wall panel
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(wx, (floor_y + header_y) / 2.0, 0.5))
        wall = bpy.context.object
        wall.scale = (0.45, (header_y - floor_y) + 0.8, 9.0)
        finish_mesh(wall, f"Deck_SideWall_{side}", 'deck_dark')

        # Vertical power conduits
        for cz in (-1.8, 0.2, 2.2):
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(wx - side * 0.22, (floor_y + header_y) / 2.0, cz))
            conduit = bpy.context.object
            conduit.scale = (0.09, (header_y - floor_y) + 0.5, 0.09)
            finish_mesh(conduit, f"Wall_Conduit_{side}_{cz}", 'deck_steel')

    # 6. Peripheral Tactical Consoles & Datacore Racks (Frame edges)
    for side in (-1, 1):
        cx = side * 3.1
        cz = -2.2 # Near the window corner
        # Console base
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(cx, floor_y + 0.50, cz))
        c_base = bpy.context.object
        c_base.scale = (0.75, 0.85, 0.90)
        c_base.rotation_euler = (0, -side * 0.45, 0)
        finish_mesh(c_base, f"Tactical_Console_Base_{side}", 'deck_steel')

        # Angled holo-screen on console
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(cx - side * 0.08, floor_y + 0.98, cz + 0.08))
        screen = bpy.context.object
        screen.scale = (0.60, 0.38, 0.04)
        screen.rotation_euler = (-0.35, -side * 0.45, 0)
        finish_mesh(screen, f"Tactical_Console_Screen_{side}", 'console_screen')

        # LED status pip array on console rack
        for k in range(4):
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(cx - side * 0.22, floor_y + 0.35 + k * 0.09, cz - 0.25))
            led = bpy.context.object
            led.scale = (0.035, 0.035, 0.035)
            finish_mesh(led, f"ConsoleLed_{side}_{k}", 'neon_emerald' if k % 2 == 0 else 'neon_amber')

        # Server Tower behind the console
        sz = -0.5
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(cx + side * 0.25, floor_y + 1.10, sz))
        server = bpy.context.object
        server.scale = (0.65, 2.0, 0.75)
        finish_mesh(server, f"Server_Tower_{side}", 'deck_dark')

        # Glowing vertical server vent slots
        for v in range(3):
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(cx + side * 0.05, floor_y + 0.60 + v * 0.45, sz))
            vent = bpy.context.object
            vent.scale = (0.05, 0.22, 0.55)
            finish_mesh(vent, f"Server_VentGlow_{side}_{v}", 'neon_cyan' if side < 0 else 'neon_emerald')

    # Export Command Deck Interior with export_yup=False to preserve Godot coordinates (Y=up, Z=depth)
    deck_glb = MODEL_DIR / "command_deck_interior.glb"
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.gltf(
        filepath=str(deck_glb),
        export_format='GLB',
        use_selection=True,
        export_apply=True,
        export_yup=False,
        export_animations=False
    )
    print(f"--> Exported Command Deck Interior to {deck_glb} ({round(deck_glb.stat().st_size / 1024, 1)} KB)")


# ─── 3. Build Cyber City Metropolis Skyline ──────────────────────────────────
def build_cyber_city_skyline():
    clear_scene()
    init_palette()

    print("--> Modeling Cyber City Metropolis Skyline (Panoramic Depth)...")

    # Helper: Create an angular skyscraper with stepped tiers, window matrices, and antenna
    def create_skyscraper(name, loc, base_dims, tiers=4, primary_mat='spire_dark', window_mat='window_cyan', has_antenna=True, emblem_mat=None):
        bx, by, bz = loc
        width, depth, total_h = base_dims
        tier_h = total_h / tiers
        cur_y = by

        for t in range(tiers):
            shrink = 1.0 - (t * 0.15)
            tw = width * shrink
            td = depth * shrink
            th = tier_h * 1.02

            # Main Tier Body Block
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx, cur_y + th / 2.0, bz))
            block = bpy.context.object
            block.scale = (tw, th, td)
            finish_mesh(block, f"{name}_Tier_{t}", primary_mat)

            # Floor cornice trim at top of tier
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx, cur_y + th, bz))
            cornice = bpy.context.object
            cornice.scale = (tw * 1.04, 0.12, td * 1.04)
            finish_mesh(cornice, f"{name}_Cornice_{t}", 'spire_metal')

            # Glowing window matrix on front face (+Z)
            num_cols = max(2, int(tw / 0.75))
            num_rows = max(2, int(th / 1.8))
            win_w = (tw * 0.75) / num_cols * 0.68
            win_h = 0.22

            for r in range(num_rows):
                wy = cur_y + (r + 0.6) * (th / (num_rows + 0.3))
                for c in range(num_cols):
                    # Deterministic window variation for inhabited metropolis feel
                    if (t * 7 + r * 5 + c * 3) % 5 == 0:
                        continue # dark window
                    wx = bx - (tw * 0.75) / 2.0 + (c + 0.5) * ((tw * 0.75) / num_cols)
                    wz = bz + td / 2.0 + 0.03
                    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(wx, wy, wz))
                    win = bpy.context.object
                    win.scale = (win_w, win_h, 0.05)
                    mat_choice = window_mat if (t + r + c) % 4 != 0 else 'window_amber'
                    finish_mesh(win, f"{name}_Win_{t}_{r}_{c}", mat_choice)

            # Side windows facing the center of the camera
            if bx < -1.0: # Tower on left, right (+X) face is visible
                side_x = bx + tw / 2.0 + 0.03
                num_side_cols = max(2, int(td / 0.8))
                for r in range(num_rows):
                    wy = cur_y + (r + 0.6) * (th / (num_rows + 0.3))
                    for sc in range(num_side_cols):
                        if (t * 5 + r * 3 + sc * 7) % 4 == 0:
                            continue
                        sz = bz - (td * 0.75) / 2.0 + (sc + 0.5) * ((td * 0.75) / num_side_cols)
                        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(side_x, wy, sz))
                        win = bpy.context.object
                        win.scale = (0.05, win_h, (td * 0.75) / num_side_cols * 0.68)
                        finish_mesh(win, f"{name}_SideWin_{t}_{r}_{sc}", window_mat)
            elif bx > 1.0: # Tower on right, left (-X) face is visible
                side_x = bx - tw / 2.0 - 0.03
                num_side_cols = max(2, int(td / 0.8))
                for r in range(num_rows):
                    wy = cur_y + (r + 0.6) * (th / (num_rows + 0.3))
                    for sc in range(num_side_cols):
                        if (t * 5 + r * 3 + sc * 7) % 4 == 0:
                            continue
                        sz = bz - (td * 0.75) / 2.0 + (sc + 0.5) * ((td * 0.75) / num_side_cols)
                        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(side_x, wy, sz))
                        win = bpy.context.object
                        win.scale = (0.05, win_h, (td * 0.75) / num_side_cols * 0.68)
                        finish_mesh(win, f"{name}_SideWin_{t}_{r}_{sc}", window_mat)

            # Corner illuminated ribs on lowest tier
            if t == 0:
                for cx in (-1, 1):
                    for cz in (-1, 1):
                        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx + cx * (tw / 2.0), cur_y + th / 2.0, bz + cz * (td / 2.0)))
                        rib = bpy.context.object
                        rib.scale = (0.08, th * 0.98, 0.08)
                        finish_mesh(rib, f"{name}_CornerRib_{cx}_{cz}", 'neon_cyan')

            cur_y += th

        # Rooftop Architectural Crown
        crown_w = width * (1.0 - (tiers - 1) * 0.15) * 0.85
        crown_d = depth * (1.0 - (tiers - 1) * 0.15) * 0.85
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx, cur_y + 0.35, bz))
        crown = bpy.context.object
        crown.scale = (crown_w, 0.70, crown_d)
        finish_mesh(crown, f"{name}_RooftopCrown", 'spire_metal')

        # Rooftop Corporate Billboard / Emissive Sign
        if emblem_mat:
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx, cur_y + 1.10, bz + crown_d / 2.0 + 0.05))
            emblem = bpy.context.object
            emblem.scale = (crown_w * 0.90, 0.65, 0.08)
            finish_mesh(emblem, f"{name}_EmblemBoard", emblem_mat)

        # Rooftop Antenna Spire & Flight Beacon
        if has_antenna:
            ant_h = 3.2
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx, cur_y + 0.70 + ant_h / 2.0, bz))
            ant = bpy.context.object
            ant.scale = (0.10, ant_h, 0.10)
            finish_mesh(ant, f"{name}_AntennaMast", 'spire_metal')

            # Flashing red aircraft warning beacon
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx, cur_y + 0.70 + ant_h + 0.15, bz))
            beacon = bpy.context.object
            beacon.scale = (0.28, 0.28, 0.28)
            finish_mesh(beacon, f"{name}_WarningBeacon", 'beacon_red')

    # ── Midground Tier: Z = -24.0 to -38.0 (Majestic Megastructures framed outside the observation bay) ──
    # Building 1: Apex Corporate Needle (Far Left, Z = -25.0, X = -5.4)
    create_skyscraper(
        name="Spire_Corp_Monolith",
        loc=(-5.4, -14.5, -25.0),
        base_dims=(3.4, 3.4, 15.0),
        tiers=5,
        primary_mat='spire_dark',
        window_mat='window_cyan',
        has_antenna=True,
        emblem_mat='neon_cyan'
    )

    # Building 2: Bio-Gen Cyber Spire (Center-Left, Z = -29.0, X = -3.5)
    create_skyscraper(
        name="Spire_BioGen_Tower",
        loc=(-3.5, -14.8, -29.0),
        base_dims=(3.2, 3.2, 15.0),
        tiers=5,
        primary_mat='spire_metal',
        window_mat='window_emerald',
        has_antenna=True,
        emblem_mat='neon_emerald'
    )

    # Building 3: Deep Center Hyper-Needle (Central Vista, Z = -38.0, X = -0.2)
    # Framed in the gap between AXIOM-7 and the dialogue bubble!
    create_skyscraper(
        name="Spire_HyperNeedle",
        loc=(-0.2, -16.5, -38.0),
        base_dims=(3.8, 3.8, 17.5),
        tiers=6,
        primary_mat='spire_dark',
        window_mat='window_cyan',
        has_antenna=True,
        emblem_mat='neon_magenta'
    )

    # Building 4 & 5: Twin Arcology Spire A & B with Skybridges (Right Midground, Z = -26.0)
    create_skyscraper(
        name="Spire_Arcology_A",
        loc=(3.8, -14.8, -26.0),
        base_dims=(2.8, 2.8, 14.5),
        tiers=4,
        primary_mat='spire_dark',
        window_mat='window_amber',
        has_antenna=True
    )
    create_skyscraper(
        name="Spire_Arcology_B",
        loc=(7.4, -14.5, -26.0),
        base_dims=(2.8, 2.8, 14.8),
        tiers=4,
        primary_mat='spire_dark',
        window_mat='window_cyan',
        has_antenna=True
    )
    # Luminous Skybridges connecting Arcology A and B (Elevated so clearly visible above sill)
    for bridge_y in (-2.2, -0.8):
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(5.6, bridge_y, -26.0))
        sbridge = bpy.context.object
        sbridge.scale = (3.6, 0.45, 0.65)
        finish_mesh(sbridge, f"Arcology_Skybridge_{round(bridge_y, 1)}", 'spire_metal')
        # Skybridge neon walkway light strip
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(5.6, bridge_y - 0.16, -26.0 + 0.34))
        finish_mesh(bpy.context.object, f"Skybridge_Light_{round(bridge_y, 1)}", 'neon_cyan')

    # Building 6: Databank Citadel Block (Far Right, Z = -32.0, X = +11.2)
    create_skyscraper(
        name="Spire_DataCitadel",
        loc=(11.2, -16.0, -32.0),
        base_dims=(4.2, 4.2, 13.5),
        tiers=4,
        primary_mat='spire_metal',
        window_mat='window_amber',
        has_antenna=False
    )

    # ── Distant Megastructure Horizon Wall (Z = -48.0 to -62.0) ──
    print("--> Generating Distant Horizon Silhouettes...")
    # Low-lying distant cityscape: base Y = -18.0, height hh = 12.0 to 15.0 -> top Y = -6.0 to -3.0
    horizon_configs = [
        (-22.0, 14.0, 3.8), (-17.0, 12.5, 3.2), (-12.0, 15.0, 4.2), (-7.5, 13.0, 3.5),
        (-3.0, 14.5, 3.6),  (2.5, 13.5, 3.4),   (7.0, 15.2, 4.0),   (12.0, 12.8, 3.6),
        (17.5, 14.0, 3.8),  (22.5, 13.2, 3.5)
    ]
    for i, (hx, hh, hw) in enumerate(horizon_configs):
        hz = -50.0 - (i % 3) * 4.0
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(hx, -18.0 + hh / 2.0, hz))
        h_block = bpy.context.object
        h_block.scale = (hw, hh, hw * 0.8)
        finish_mesh(h_block, f"Horizon_Megastructure_{i}", 'horizon_fog')

        # Distant glowing window matrices (discrete small window dots)
        num_dwin = max(2, int(hh / 3.0))
        for dw in range(num_dwin):
            dwy = -18.0 + (dw + 0.7) * (hh / (num_dwin + 0.3))
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(hx, dwy, hz + hw * 0.41))
            w_dot = bpy.context.object
            w_dot.scale = (hw * 0.65, 0.20, 0.05)
            finish_mesh(w_dot, f"Horizon_Windows_{i}_{dw}", 'window_cyan' if (i + dw) % 2 == 0 else 'window_amber')

        # Rooftop warning beacon
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(hx, -18.0 + hh + 0.30, hz))
        h_beacon = bpy.context.object
        h_beacon.scale = (0.35, 0.35, 0.35)
        finish_mesh(h_beacon, f"Horizon_Beacon_{i}", 'beacon_red')

    # ── Mid-Air Sky Transit Traffic (Sky lanes with glowing light trails) ──
    print("--> Adding Mid-Air Hover Traffic...")
    traffic_lanes = [
        # (X, Y, Z, length, heading, color_key)
        (-4.5, 0.4, -22.0, 2.2, 1, 'neon_cyan'),
        (2.5, -0.6, -20.5, 2.5, -1, 'neon_amber'),
        (-1.0, 1.2, -28.0, 2.8, 1, 'neon_cyan'),
        (5.5, 0.8, -24.0, 2.4, -1, 'neon_magenta'),
        (0.5, 1.8, -32.0, 3.0, 1, 'neon_cyan'),
    ]
    for idx, (tx, ty, tz, tlen, heading, col_key) in enumerate(traffic_lanes):
        # Hover speeder fuselage
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(tx, ty, tz))
        speeder = bpy.context.object
        speeder.scale = (0.45, 0.20, tlen)
        finish_mesh(speeder, f"Hover_Speeder_{idx}", 'spire_metal')

        # Forward headlight beam
        fwd_z = tz - heading * (tlen / 2.0 + 0.35)
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(tx, ty, fwd_z))
        hlight = bpy.context.object
        hlight.scale = (0.35, 0.10, 0.60)
        finish_mesh(hlight, f"Speeder_Headlight_{idx}", col_key)

        # Rear taillight trail
        trail_z = tz + heading * (tlen / 2.0 + 0.55)
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(tx, ty, trail_z))
        trail = bpy.context.object
        trail.scale = (0.30, 0.08, 1.10)
        finish_mesh(trail, f"Speeder_Trail_{idx}", 'beacon_red' if heading > 0 else 'neon_amber')

    # Export Cyber City Metropolis Skyline with export_yup=False to preserve Godot coordinates (Y=up, Z=depth)
    skyline_glb = MODEL_DIR / "cyber_city_skyline.glb"
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.gltf(
        filepath=str(skyline_glb),
        export_format='GLB',
        use_selection=True,
        export_apply=True,
        export_yup=False,
        export_animations=False
    )
    print(f"--> Exported Cyber City Skyline to {skyline_glb} ({round(skyline_glb.stat().st_size / 1024, 1)} KB)")


if __name__ == "__main__":
    print("==========================================================")
    print("   AXIOM FORCE — Building 3D Command Deck & City Skyline  ")
    print("==========================================================")
    build_command_deck_interior()
    build_cyber_city_skyline()
    print("==========================================================")
    print("   ALL 3D BACKGROUND ASSETS GENERATED SUCCESSFULLY!       ")
    print("==========================================================")
