"""Build Axiom Force Main Menu ("Cyber Command Center") 3D Assets in Blender 5.2.

Generates 12 procedural hard-surface cyberpunk assets with calibrated PBR materials,
exports standard glTF 2.0 (.glb) files to assets/models/menu/, and renders studio previews.

Run headless:
  blender --background --factory-startup --python tools/assets/build_main_menu_assets.py
"""
import bpy
import bmesh
import json
import math
from pathlib import Path
from mathutils import Vector, Euler

ROOT = Path(__file__).resolve().parents[2]
OUTPUT_DIR = ROOT / "assets/models/menu"
PREVIEW_DIR = ROOT / "assets/models/menu/previews"
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
PREVIEW_DIR.mkdir(parents=True, exist_ok=True)

MATERIALS = {}
RECORDS = []

# ─── 1. PBR Material Setup ───────────────────────────────────────────────────
def make_pbr_material(name, base_color, metallic=0.0, roughness=0.4, emission_color=(0, 0, 0), emission_strength=0.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.diffuse_color = (*base_color, 1.0)
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs['Base Color'].default_value = (*base_color, 1.0)
        bsdf.inputs['Metallic'].default_value = metallic
        bsdf.inputs['Roughness'].default_value = roughness
        if emission_strength > 0:
            if 'Emission Color' in bsdf.inputs:
                bsdf.inputs['Emission Color'].default_value = (*emission_color, 1.0)
                bsdf.inputs['Emission Strength'].default_value = emission_strength
    return mat

def init_palette():
    MATERIALS.clear()
    MATERIALS.update({
        'armor':    make_pbr_material('Mat_ObsidianArmor', (0.024, 0.031, 0.043), metallic=0.82, roughness=0.32),
        'titanium': make_pbr_material('Mat_IndustrialTitanium', (0.170, 0.210, 0.260), metallic=0.88, roughness=0.24),
        'trim':     make_pbr_material('Mat_BrushedAluminum', (0.420, 0.480, 0.550), metallic=0.94, roughness=0.18),
        'carbon':   make_pbr_material('Mat_MatteCarbon', (0.012, 0.016, 0.022), metallic=0.20, roughness=0.70),
        'rubber':   make_pbr_material('Mat_ConduitInsulation', (0.008, 0.012, 0.018), metallic=0.05, roughness=0.85),
        'hazard':   make_pbr_material('Mat_HazardChevron', (0.950, 0.540, 0.012), metallic=0.35, roughness=0.40),
        'cyan':     make_pbr_material('Mat_EmissiveCyan', (0.005, 0.850, 1.000), metallic=0.15, roughness=0.20,
                                      emission_color=(0.005, 0.850, 1.000), emission_strength=4.5),
        'magenta':  make_pbr_material('Mat_EmissiveMagenta', (1.000, 0.010, 0.450), metallic=0.10, roughness=0.20,
                                      emission_color=(1.000, 0.010, 0.450), emission_strength=4.5),
        'gold':     make_pbr_material('Mat_EmissiveGold', (1.000, 0.580, 0.020), metallic=0.15, roughness=0.25,
                                      emission_color=(1.000, 0.580, 0.020), emission_strength=3.8),
        'white':    make_pbr_material('Mat_EmissiveWhite', (0.800, 0.950, 1.000), metallic=0.10, roughness=0.25,
                                      emission_color=(0.800, 0.950, 1.000), emission_strength=3.0),
        'red':      make_pbr_material('Mat_EmissiveRed', (1.000, 0.010, 0.020), metallic=0.15, roughness=0.25,
                                      emission_color=(1.000, 0.010, 0.020), emission_strength=4.0),
    })

def reset_scene(name):
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for block in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for item in list(block):
            block.remove(item, do_unlink=True)
    init_palette()
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1.0
    bpy.context.scene['asset_name'] = name

# ─── 2. Geometry Modeling Helpers ─────────────────────────────────────────────
def finish_mesh(obj, name, mat_key, bevel=0.0):
    obj.name = name
    if mat_key in MATERIALS:
        obj.data.materials.append(MATERIALS[mat_key])
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel > 0.0:
        mod = obj.modifiers.new("EdgeBevel", 'BEVEL')
        mod.width = bevel
        mod.segments = 2
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj

def add_box(name, loc, dims, mat='armor', bevel=0.02, rot=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.dimensions = dims
    return finish_mesh(obj, name, mat, bevel)

def add_cylinder(name, loc, radius, depth, mat='titanium', vertices=16, rot=(0,0,0), bevel=0.015):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    obj = bpy.context.object
    return finish_mesh(obj, name, mat, bevel)

def add_sphere(name, loc, radius, mat='cyan', segments=16, rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=radius, location=loc)
    obj = bpy.context.object
    return finish_mesh(obj, name, mat, bevel=0.0)

def get_scene_bounds():
    points = [o.matrix_world @ Vector(v) for o in bpy.context.scene.objects if o.type == 'MESH' for v in o.bound_box]
    if not points:
        return Vector((0,0,0)), Vector((1,1,1))
    low = Vector([min(p[i] for p in points) for i in range(3)])
    high = Vector([max(p[i] for p in points) for i in range(3)])
    return low, high

# ─── 3. Asset Generators ──────────────────────────────────────────────────────

# 1. Tactical Holo-Projector Pedestal
def build_holo_projector_pedestal():
    # Octagonal base platform
    add_cylinder("Pedestal_Base", (0, 0, 0.15), radius=1.2, depth=0.3, mat='armor', vertices=8, bevel=0.03)
    # Tier 2 collar
    add_cylinder("Pedestal_Tier2", (0, 0, 0.35), radius=0.95, depth=0.15, mat='titanium', vertices=8, bevel=0.02)
    # Emitter rim ring
    add_cylinder("Emitter_Rim", (0, 0, 0.46), radius=0.75, depth=0.10, mat='trim', vertices=24, bevel=0.015)
    # Recessed core lens
    add_cylinder("Core_Lens", (0, 0, 0.49), radius=0.55, depth=0.05, mat='cyan', vertices=24)
    # Central focus node
    add_sphere("Focus_Node", (0, 0, 0.52), radius=0.16, mat='white')
    # 4 Stabilizer Claws
    for i in range(4):
        ang = i * (math.pi / 2) + (math.pi / 4)
        x = math.cos(ang) * 0.98
        y = math.sin(ang) * 0.98
        add_box(f"Stabilizer_{i}", (x, y, 0.32), (0.16, 0.28, 0.42), mat='titanium', rot=(0, 0, ang), bevel=0.02)
        add_cylinder(f"PylonJoint_{i}", (x, y, 0.48), radius=0.04, depth=0.12, mat='magenta', rot=(0, math.pi/2, ang))
    # 4 Radial Conduit Lines
    for i in range(4):
        ang = i * (math.pi / 2)
        x = math.cos(ang) * 0.55
        y = math.sin(ang) * 0.55
        add_box(f"Conduit_{i}", (x, y, 0.44), (0.05, 0.55, 0.03), mat='cyan', rot=(0, 0, ang))

# 2. Command Terminal Kiosk
def build_command_terminal_kiosk():
    # Heavy weighted floor base
    add_box("Kiosk_Base", (0, 0, 0.08), (1.1, 0.8, 0.16), mat='armor', bevel=0.03)
    # Angled central pedestal spine
    add_box("Kiosk_Spine", (0, -0.05, 0.55), (0.45, 0.35, 0.85), mat='titanium', rot=(math.radians(12), 0, 0), bevel=0.02)
    # Operator control desk tray
    add_box("Desk_Tray", (0, -0.22, 0.95), (0.95, 0.50, 0.08), mat='armor', rot=(math.radians(25), 0, 0), bevel=0.02)
    # Tactile cyber keyboard panel
    add_box("Keyboard_Deck", (0, -0.24, 0.99), (0.75, 0.25, 0.03), mat='trim', rot=(math.radians(25), 0, 0))
    add_box("KeyRow_1", (0, -0.23, 1.01), (0.65, 0.05, 0.02), mat='cyan', rot=(math.radians(25), 0, 0))
    add_box("KeyRow_2", (0, -0.27, 0.98), (0.65, 0.05, 0.02), mat='magenta', rot=(math.radians(25), 0, 0))
    # Dual Holographic Display Screens (Angled inward)
    add_box("Screen_Left", (-0.35, 0.10, 1.35), (0.58, 0.03, 0.42), mat='cyan', rot=(math.radians(-10), math.radians(-15), 0))
    add_box("Screen_Left_Frame", (-0.35, 0.11, 1.35), (0.62, 0.05, 0.46), mat='titanium', rot=(math.radians(-10), math.radians(-15), 0), bevel=0.015)
    add_box("Screen_Right", (0.35, 0.10, 1.35), (0.58, 0.03, 0.42), mat='white', rot=(math.radians(-10), math.radians(15), 0))
    add_box("Screen_Right_Frame", (0.35, 0.11, 1.35), (0.62, 0.05, 0.46), mat='titanium', rot=(math.radians(-10), math.radians(15), 0), bevel=0.015)
    # Side status battery banks
    add_box("Data_Cartridge_L", (-0.50, 0.0, 0.65), (0.12, 0.22, 0.35), mat='magenta', bevel=0.01)
    add_box("Data_Cartridge_R", (0.50, 0.0, 0.65), (0.12, 0.22, 0.35), mat='cyan', bevel=0.01)

# 3. Operative Staging Podium
def build_operative_podium():
    # Outer low-profile disc
    add_cylinder("Podium_OuterDisc", (0, 0, 0.08), radius=1.80, depth=0.16, mat='armor', vertices=32, bevel=0.02)
    # Mid-tier ring
    add_cylinder("Podium_MidDisc", (0, 0, 0.18), radius=1.55, depth=0.12, mat='titanium', vertices=32, bevel=0.02)
    # Center standing platform
    add_cylinder("Podium_CenterDeck", (0, 0, 0.26), radius=1.30, depth=0.08, mat='carbon', vertices=32)
    # Concentric neon inlay rings
    add_cylinder("NeonRing_Outer", (0, 0, 0.28), radius=1.20, depth=0.02, mat='magenta', vertices=32)
    add_cylinder("NeonRing_Inner", (0, 0, 0.29), radius=0.75, depth=0.02, mat='cyan', vertices=32)
    # Center operative anchor dot
    add_cylinder("Anchor_Dot", (0, 0, 0.30), radius=0.25, depth=0.02, mat='white', vertices=16)
    # 4 Magnetic locking clamps around perimeter
    for i in range(4):
        ang = i * (math.pi / 2)
        x = math.cos(ang) * 1.45
        y = math.sin(ang) * 1.45
        add_box(f"LockClamp_{i}", (x, y, 0.28), (0.22, 0.15, 0.14), mat='hazard', rot=(0, 0, ang), bevel=0.01)

# 4. DataCore Server Tower
def build_datacore_server_tower():
    # Plinth base
    add_box("Server_Base", (0, 0, 0.25), (1.4, 1.4, 0.5), mat='armor', bevel=0.04)
    # Main Tower Body
    add_box("Server_Column", (0, 0, 2.35), (1.1, 1.1, 3.8), mat='titanium', bevel=0.03)
    # Recessed Front Acrylic Panel
    add_box("Front_Bay", (0, -0.56, 2.35), (0.85, 0.05, 3.2), mat='carbon')
    # LED Matrix Rows (Amber, Cyan, Magenta)
    mat_cycle = ['cyan', 'gold', 'magenta', 'cyan', 'gold']
    for i in range(9):
        z = 1.15 + i * 0.28
        mat = mat_cycle[i % len(mat_cycle)]
        add_box(f"LedRow_{i}", (0, -0.58, z), (0.70, 0.03, 0.06), mat=mat)
    # Lateral cooling heatsink fins
    for s in (-0.56, 0.56):
        for j in range(8):
            z = 1.20 + j * 0.32
            add_box(f"Fin_{s}_{j}", (s, 0, z), (0.05, 0.85, 0.04), mat='trim')
    # Armored top power cap
    add_box("Top_Cap", (0, 0, 4.40), (1.3, 1.3, 0.3), mat='armor', bevel=0.03)
    add_sphere("Top_Beacon", (0, 0, 4.60), radius=0.12, mat='cyan')

# 5. Shield Pylon Column
def build_shield_pylon_column():
    # Heavy octagonal bolt-down collar
    add_cylinder("Pylon_Collar", (0, 0, 0.20), radius=0.55, depth=0.40, mat='armor', vertices=8, bevel=0.03)
    # Main vertical octagonal pillar
    add_cylinder("Pylon_Shaft", (0, 0, 2.10), radius=0.32, depth=3.50, mat='titanium', vertices=8, bevel=0.02)
    # Vertical recessed neon light strips along 4 cardinal sides
    for i in range(4):
        ang = i * (math.pi / 2)
        x = math.cos(ang) * 0.33
        y = math.sin(ang) * 0.33
        add_box(f"NeonSpine_{i}", (x, y, 2.10), (0.04, 0.04, 3.20), mat='cyan', rot=(0, 0, ang))
    # Emitter head capital
    add_cylinder("Head_Capital", (0, 0, 3.90), radius=0.48, depth=0.30, mat='trim', vertices=8, bevel=0.02)
    # Central radiant emitter core
    add_sphere("Emitter_Core", (0, 0, 4.15), radius=0.22, mat='cyan')
    # 4 Protective claw prongs
    for i in range(4):
        ang = i * (math.pi / 2) + (math.pi / 4)
        x = math.cos(ang) * 0.36
        y = math.sin(ang) * 0.36
        add_box(f"Claw_{i}", (x, y, 4.15), (0.08, 0.12, 0.45), mat='titanium', rot=(0, 0, ang), bevel=0.015)

# 6. Perimeter Blast Barrier
def build_perimeter_blast_barrier():
    # Heavy footprint base
    add_box("Barrier_Base", (0, 0, 0.15), (3.2, 0.85, 0.30), mat='armor', bevel=0.03)
    # Angled defensive sloped breastplate (Front)
    add_box("Sloped_Armor_Front", (0, -0.15, 0.65), (3.0, 0.40, 0.85), mat='titanium', rot=(math.radians(16), 0, 0), bevel=0.03)
    # Rear reinforcement spine
    add_box("Spine_Rear", (0, 0.20, 0.65), (2.8, 0.30, 0.80), mat='armor', bevel=0.02)
    # Dual horizontal neon accent indicators
    add_box("Neon_Trim_Upper", (0, -0.32, 0.95), (2.6, 0.05, 0.04), mat='cyan')
    add_box("Neon_Trim_Lower", (0, -0.22, 0.38), (2.6, 0.05, 0.04), mat='magenta')
    # End bulkhead caps with hazard chevrons
    for s in (-1.55, 1.55):
        add_box(f"Bulkhead_{s}", (s, 0, 0.65), (0.20, 0.80, 0.95), mat='hazard', bevel=0.02)
        add_cylinder(f"Coupling_{s}", (s + (0.12 if s > 0 else -0.12), 0, 0.65), radius=0.12, depth=0.15, mat='trim', rot=(0, math.pi/2, 0))

# 7. Automated Plasma Sentry Turret
def build_plasma_defense_turret():
    # Rotating circular swivel pedestal
    add_cylinder("Turret_Base", (0, 0, 0.25), radius=0.85, depth=0.50, mat='armor', vertices=24, bevel=0.03)
    add_cylinder("Swivel_Ring", (0, 0, 0.55), radius=0.70, depth=0.15, mat='trim', vertices=24)
    # Turret housing body
    add_box("Turret_Pod", (0, 0, 0.95), (0.90, 1.10, 0.65), mat='titanium', bevel=0.04)
    # Central targeting sensor eye
    add_cylinder("Targeting_Eye", (0, -0.58, 0.95), radius=0.16, depth=0.10, mat='red', vertices=16, rot=(math.pi/2, 0, 0))
    # Dual plasma accelerator barrels
    for s in (-0.30, 0.30):
        # Barrel sleeve
        add_cylinder(f"Barrel_Sleeve_{s}", (s, -0.75, 0.95), radius=0.12, depth=0.70, mat='armor', vertices=16, rot=(math.pi/2, 0, 0), bevel=0.01)
        # Inner plasma coil (glowing)
        add_cylinder(f"Barrel_Coil_{s}", (s, -1.05, 0.95), radius=0.07, depth=0.20, mat='cyan', vertices=16, rot=(math.pi/2, 0, 0))
        # Muzzle brake
        add_cylinder(f"Muzzle_{s}", (s, -1.18, 0.95), radius=0.10, depth=0.10, mat='trim', vertices=16, rot=(math.pi/2, 0, 0))
    # Ammo feed power conduit
    add_box("Ammo_Pack", (0, 0.58, 0.95), (0.75, 0.35, 0.45), mat='carbon', bevel=0.02)
    add_box("Ammo_Status", (0, 0.77, 0.95), (0.45, 0.04, 0.12), mat='gold')

# 8. Command Deck Modular Floor Tile
def build_command_deck_platform():
    # Main slab
    add_box("Deck_Slab", (0, 0, 0.20), (6.0, 6.0, 0.40), mat='armor', bevel=0.04)
    # 4 Raised diamond non-slip tread plates
    for x in (-1.45, 1.45):
        for y in (-1.45, 1.45):
            add_box(f"TreadPlate_{x}_{y}", (x, y, 0.42), (2.60, 2.60, 0.05), mat='titanium', bevel=0.02)
            add_box(f"PlateGrip_{x}_{y}", (x, y, 0.45), (2.20, 2.20, 0.02), mat='carbon')
    # Recessed glowing cross-trunk conduit trench
    add_box("Conduit_Trench_X", (0, 0, 0.42), (5.80, 0.25, 0.03), mat='cyan')
    add_box("Conduit_Trench_Y", (0, 0, 0.42), (0.25, 5.80, 0.03), mat='magenta')
    # 4 Corner flush landing beacon orbs
    for cx in (-2.75, 2.75):
        for cy in (-2.75, 2.75):
            add_sphere(f"Beacon_{cx}_{cy}", (cx, cy, 0.44), radius=0.08, mat='gold')

# 9. Patrol Surveillance Drone
def build_patrol_surveillance_drone():
    # Central fuselage pod
    add_box("Drone_Fuselage", (0, 0, 0.30), (0.70, 1.20, 0.28), mat='armor', bevel=0.04)
    # Forward observation cockpit / sensor visor
    add_cylinder("Sensor_Visor", (0, -0.65, 0.30), radius=0.20, depth=0.18, mat='cyan', vertices=16, rot=(math.pi/2, 0, 0))
    # 4 Outrigger nacelles with glowing anti-grav thrusters
    nacelle_coords = [(-0.65, -0.45), (0.65, -0.45), (-0.75, 0.45), (0.75, 0.45)]
    for i, (nx, ny) in enumerate(nacelle_coords):
        # Outrigger strut
        add_box(f"Strut_{i}", (nx * 0.5, ny, 0.30), (0.45, 0.12, 0.06), mat='titanium', bevel=0.01)
        # Thruster duct ring
        add_cylinder(f"ThrusterRing_{i}", (nx, ny, 0.30), radius=0.24, depth=0.22, mat='trim', vertices=16, bevel=0.015)
        # Emissive thrust plasma disc
        add_cylinder(f"PlasmaDisc_{i}", (nx, ny, 0.22), radius=0.18, depth=0.06, mat='cyan', vertices=16)
    # Twin vertical tail stabilizer fins
    for s in (-0.28, 0.28):
        add_box(f"Fin_{s}", (s, 0.55, 0.55), (0.04, 0.35, 0.35), mat='titanium', bevel=0.01)
        add_sphere(f"NavBeacon_{s}", (s, 0.65, 0.72), radius=0.03, mat='red' if s < 0 else 'cyan')

# 10. Tactical Comm Array
def build_tactical_comm_array():
    # Heavy triangular roof truss mount
    add_cylinder("Mount_Plinth", (0, 0, 0.20), radius=1.10, depth=0.40, mat='armor', vertices=6, bevel=0.03)
    # Swivel gantry mast
    add_cylinder("Mast_Shaft", (0, 0, 1.20), radius=0.22, depth=1.60, mat='titanium', vertices=12, bevel=0.015)
    # Parabolic radar dish (angled 35 degrees)
    dish_rot = (math.radians(-35), 0, 0)
    add_cylinder("Parabolic_Dish", (0, -0.30, 2.30), radius=1.25, depth=0.22, mat='titanium', vertices=24, rot=dish_rot, bevel=0.02)
    add_cylinder("Dish_Interior", (0, -0.32, 2.30), radius=1.12, depth=0.16, mat='armor', vertices=24, rot=dish_rot)
    # Focal feed horn and support tripod
    add_cylinder("Feed_Probe", (0, -0.65, 2.50), radius=0.06, depth=0.55, mat='trim', vertices=8, rot=dish_rot)
    add_sphere("Signal_Tip", (0, -0.88, 2.65), radius=0.08, mat='cyan')
    # High-gain omni antenna rod
    add_cylinder("Omni_Antenna", (0.45, 0.45, 2.80), radius=0.03, depth=2.40, mat='trim', vertices=8)
    add_sphere("Antenna_Beacon", (0.45, 0.45, 4.02), radius=0.06, mat='white')

# 11. Resistance Crest 3D Emblem
def build_resistance_crest_emblem():
    # Outer beveled shield contour
    add_box("Shield_Backing", (0, 0, 0.10), (2.80, 2.80, 0.20), mat='armor', bevel=0.04)
    # Layer 1 Titanium Wing Frames (Left & Right)
    for s in (-0.75, 0.75):
        add_box(f"Wing_L1_{s}", (s, 0.20, 0.22), (1.10, 0.45, 0.12), mat='titanium', rot=(0, 0, math.radians(-25 if s > 0 else 25)), bevel=0.02)
        add_box(f"Wing_L2_{s}", (s * 1.15, -0.30, 0.24), (0.85, 0.35, 0.12), mat='trim', rot=(0, 0, math.radians(-40 if s > 0 else 40)), bevel=0.02)
        # Glowing feather tips
        add_box(f"FeatherTip_{s}", (s * 1.30, -0.55, 0.26), (0.50, 0.15, 0.08), mat='cyan', rot=(0, 0, math.radians(-50 if s > 0 else 50)))
    # Central Resistance Arrow / Chevron Core
    add_box("Core_Chevron_Left", (-0.25, -0.10, 0.30), (0.16, 0.90, 0.12), mat='white', rot=(0, 0, math.radians(-28)), bevel=0.01)
    add_box("Core_Chevron_Right", (0.25, -0.10, 0.30), (0.16, 0.90, 0.12), mat='white', rot=(0, 0, math.radians(28)), bevel=0.01)
    add_box("Core_Spine", (0, 0.25, 0.32), (0.18, 0.60, 0.12), mat='cyan', bevel=0.01)
    add_sphere("Core_Crystal", (0, -0.65, 0.34), radius=0.15, mat='magenta')

# 12. Holographic Focus Rings
def build_holo_focus_rings():
    # Inner ring
    add_cylinder("Ring_Inner", (0, 0, 0.05), radius=0.60, depth=0.03, mat='cyan', vertices=32)
    # 4 Inner ticks
    for i in range(4):
        ang = i * (math.pi / 2)
        x = math.cos(ang) * 0.60
        y = math.sin(ang) * 0.60
        add_box(f"InnerTick_{i}", (x, y, 0.05), (0.04, 0.12, 0.04), mat='white', rot=(0, 0, ang))
    # Middle ring (segmented)
    for i in range(6):
        ang = i * (math.pi / 3) + 0.15
        x = math.cos(ang) * 0.95
        y = math.sin(ang) * 0.95
        add_box(f"MidSegment_{i}", (x, y, 0.10), (0.04, 0.45, 0.03), mat='magenta', rot=(0, 0, ang))
    # Outer ring
    add_cylinder("Ring_Outer", (0, 0, 0.15), radius=1.25, depth=0.03, mat='cyan', vertices=48)
    for i in range(8):
        ang = i * (math.pi / 4)
        x = math.cos(ang) * 1.25
        y = math.sin(ang) * 1.25
        add_box(f"OuterMarker_{i}", (x, y, 0.15), (0.06, 0.14, 0.04), mat='gold', rot=(0, 0, ang))

# ─── 4. Export & Preview Rendering Pipeline ────────────────────────────────────

def render_preview(name, output_png):
    low, high = get_scene_bounds()
    center = (high + low) / 2
    span = max(high - low)
    span = max(span, 1.0)

    preview_objs = []
    try:
        # Dark studio ground plane
        bpy.ops.mesh.primitive_plane_add(size=span * 20, location=(center.x, center.y, low.z - 0.01))
        ground = bpy.context.object
        ground.name = "Preview_Ground"
        mat_ground = make_pbr_material("Mat_StudioGround", (0.015, 0.020, 0.028), metallic=0.9, roughness=0.3)
        ground.data.materials.append(mat_ground)
        preview_objs.append(ground)

        # 3-Point Cyber Studio Lighting Rig
        # Key light (Cool Cyan tint)
        key_light = bpy.data.lights.new("Studio_Key", 'AREA')
        key_light.energy = 180.0 * (span ** 1.5)
        key_light.size = span * 1.5
        key_light.color = (0.75, 0.90, 1.0)
        key_obj = bpy.data.objects.new("Studio_Key", key_light)
        bpy.context.collection.objects.link(key_obj)
        key_obj.location = center + Vector((span * 1.5, -span * 1.8, span * 1.4))
        key_obj.rotation_euler = (center - key_obj.location).to_track_quat('-Z', 'Y').to_euler()
        preview_objs.append(key_obj)

        # Fill / Rim light (Cyber Magenta tint)
        fill_light = bpy.data.lights.new("Studio_Fill", 'AREA')
        fill_light.energy = 120.0 * (span ** 1.5)
        fill_light.size = span * 1.2
        fill_light.color = (1.0, 0.20, 0.60)
        fill_obj = bpy.data.objects.new("Studio_Fill", fill_light)
        bpy.context.collection.objects.link(fill_obj)
        fill_obj.location = center + Vector((-span * 1.8, span * 1.2, span * 1.0))
        fill_obj.rotation_euler = (center - fill_obj.location).to_track_quat('-Z', 'Y').to_euler()
        preview_objs.append(fill_obj)

        # Camera setup (Isometric 35-degree perspective)
        cam_data = bpy.data.cameras.new("Preview_Camera")
        cam_data.lens = 50
        cam_obj = bpy.data.objects.new("Preview_Camera", cam_data)
        bpy.context.collection.objects.link(cam_obj)
        bpy.context.scene.camera = cam_obj
        preview_objs.append(cam_obj)

        dist = span * 2.2
        cam_obj.location = center + Vector((dist * 0.75, -dist * 0.85, dist * 0.55))
        cam_obj.rotation_euler = (center - cam_obj.location).to_track_quat('-Z', 'Y').to_euler()

        # Render settings
        scene = bpy.context.scene
        scene.render.resolution_x = 768
        scene.render.resolution_y = 768
        scene.render.filepath = str(output_png)
        scene.render.image_settings.file_format = 'PNG'
        scene.render.image_settings.color_mode = 'RGBA'
        scene.render.engine = 'BLENDER_EEVEE'

        bpy.ops.render.render(write_still=True)
        print(f"PREVIEW_RENDERED: {output_png.name}", flush=True)
    finally:
        for obj in preview_objs:
            if obj and obj.name in bpy.data.objects:
                bpy.data.objects.remove(obj, do_unlink=True)

def export_asset(name, purpose, builder_fn):
    print(f"\n--- Building Asset: {name} ---", flush=True)
    reset_scene(name)
    builder_fn()
    bpy.context.view_layer.update()

    low, high = get_scene_bounds()
    parent = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(parent)
    parent['units'] = 'meters'
    parent['origin'] = 'bottom_center'
    parent['purpose'] = purpose

    asset_objs = [parent]
    # Align origin Z to ground floor (bottom center)
    for obj in list(bpy.context.scene.objects):
        if obj != parent and obj.type == 'MESH':
            obj.location.z -= low[2]
            obj.parent = parent
            asset_objs.append(obj)

    bpy.context.view_layer.update()

    # 1. Export GLB FIRST (clean asset hierarchy only)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in asset_objs:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = parent

    glb_path = OUTPUT_DIR / f"{name}.glb"
    bpy.ops.export_scene.gltf(
        filepath=str(glb_path),
        export_format='GLB',
        use_selection=True,
        export_apply=True,
        export_extras=True,
        export_yup=True,
        export_animations=False
    )

    size_kb = round(glb_path.stat().st_size / 1024, 1)
    low_final, high_final = get_scene_bounds()
    dims = [round(high_final[i] - low_final[i], 3) for i in range(3)]

    # 2. Render Preview Image
    preview_path = PREVIEW_DIR / f"{name}.png"
    try:
        render_preview(name, preview_path)
    except Exception as e:
        print(f"Warning: preview render failed for {name}: {e}", flush=True)

    RECORDS.append({
        'name': name,
        'file': glb_path.relative_to(ROOT).as_posix(),
        'preview': preview_path.relative_to(ROOT).as_posix(),
        'purpose': purpose,
        'dimensions_m': dims,
        'size_kb': size_kb,
    })
    print(f"GLB_EXPORTED: {glb_path.name} ({size_kb} KB) | Dims: {dims}", flush=True)

# ─── 5. Main Execution ────────────────────────────────────────────────────────
ASSETS_TO_BUILD = [
    ("holo_projector_pedestal", "Octagonal multi-tier holo emitter platform with stabilizer struts", build_holo_projector_pedestal),
    ("command_terminal_kiosk", "Ergonomic operator console with dual angled holo visors and cyber keyboard", build_command_terminal_kiosk),
    ("operative_podium", "Circular character staging platform with concentric neon ring inlays", build_operative_podium),
    ("datacore_server_tower", "Vertical mainframe column with LED status telemetry and heatsinks", build_datacore_server_tower),
    ("shield_pylon_column", "Octagonal perimeter defense pylon with radiant emitter core and light spines", build_shield_pylon_column),
    ("perimeter_blast_barrier", "Heavy modular armored barricade with hazard chevrons and neon trims", build_perimeter_blast_barrier),
    ("plasma_defense_turret", "Dual-barrel automated plasma defense turret with central targeting optic", build_plasma_defense_turret),
    ("command_deck_platform", "Modular 6x6m plaza deck tile with recessed emissive power channels", build_command_deck_platform),
    ("patrol_surveillance_drone", "Anti-gravity patrol drone with 4 ducted thrusters and forward sensor visor", build_patrol_surveillance_drone),
    ("tactical_comm_array", "Rooftop parabolic comm dish with dual antenna and signal beacons", build_tactical_comm_array),
    ("resistance_crest_emblem", "High-detail 3D geometric resistance shield crest with glowing wings", build_resistance_crest_emblem),
    ("holo_focus_rings", "Concentric floating segmented telemetry rings for holographic projections", build_holo_focus_rings),
]

if __name__ == "__main__":
    print("=====================================================")
    print("   Axiom Force Main Menu Asset Generator (Blender 5.2)")
    print("=====================================================")
    for name, purpose, fn in ASSETS_TO_BUILD:
        export_asset(name, purpose, fn)

    manifest_file = OUTPUT_DIR / "menu_assets_manifest.json"
    manifest_file.write_text(json.dumps({
        'generator': 'tools/assets/build_main_menu_assets.py',
        'blender_version': bpy.app.version_string,
        'count': len(RECORDS),
        'assets': RECORDS
    }, indent=2) + "\n", encoding='utf-8')

    print("\n=====================================================")
    print(f"   SUCCESS: Generated {len(RECORDS)} Main Menu Cyber Assets!")
    print(f"   Manifest: {manifest_file}")
    print("=====================================================")
