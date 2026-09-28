"""Build Cyber Shogun High-Altitude Fly-In VFX Assets in Blender 5.2.

Generates:
1. landing_shockwave.glb - Concentric multi-tiered energy shockwave disc for podium touchdown.
2. thruster_plume.glb - Aerodynamic dual-layered cyan plasma thruster cone with stabilizer nozzle.

Run headless:
  & "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup --python tools/assets/build_flyin_vfx_assets.py
"""
import bpy
import math
from pathlib import Path
from mathutils import Vector, Euler

ROOT = Path(__file__).resolve().parents[2]
OUTPUT_DIR = ROOT / "assets/models/menu"
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

MATERIALS = {}

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
        'titanium': make_pbr_material('Mat_VfxTitanium', (0.15, 0.19, 0.24), metallic=0.9, roughness=0.2),
        'cyan':     make_pbr_material('Mat_VfxCyan', (0.005, 0.85, 1.0), metallic=0.1, roughness=0.1,
                                      emission_color=(0.005, 0.85, 1.0), emission_strength=5.5),
        'white':    make_pbr_material('Mat_VfxCoreWhite', (0.85, 0.98, 1.0), metallic=0.1, roughness=0.1,
                                      emission_color=(0.85, 0.98, 1.0), emission_strength=7.0),
        'plasma':   make_pbr_material('Mat_VfxPlasma', (0.0, 0.6, 0.95), metallic=0.05, roughness=0.15,
                                      emission_color=(0.0, 0.6, 0.95), emission_strength=4.0),
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
    return obj

def add_cylinder(name, loc, radius, depth, mat='cyan', vertices=24, rot=(0,0,0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    obj = bpy.context.object
    return finish_mesh(obj, name, mat)

def add_cone(name, loc, radius1, radius2, depth, mat='cyan', vertices=16, rot=(0,0,0)):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius1, radius2=radius2, depth=depth, location=loc, rotation=rot)
    obj = bpy.context.object
    return finish_mesh(obj, name, mat)

def add_box(name, loc, dims, mat='cyan', rot=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.dimensions = dims
    return finish_mesh(obj, name, mat)

def build_landing_shockwave():
    reset_scene()
    
    # Outer hexagonal shockwave boundary ring
    add_cylinder("Shockwave_OuterRim", (0, 0, 0.02), radius=1.35, depth=0.02, mat='cyan', vertices=8)
    
    # Inner circular blast wave
    add_cylinder("Shockwave_InnerRing", (0, 0, 0.03), radius=1.05, depth=0.03, mat='white', vertices=24)
    
    # Secondary expansion ripple
    add_cylinder("Shockwave_MiddleRing", (0, 0, 0.025), radius=0.75, depth=0.02, mat='plasma', vertices=24)
    
    # Core impact burst plate
    add_cylinder("Shockwave_CoreDisc", (0, 0, 0.04), radius=0.45, depth=0.04, mat='white', vertices=16)

    # 8 Radial energy spikes
    for i in range(8):
        ang = i * (math.pi / 4)
        x = math.cos(ang) * 0.95
        y = math.sin(ang) * 0.95
        add_box(f"Shockwave_Spike_{i}", (x, y, 0.03), (0.06, 0.55, 0.02), mat='white', rot=(0, 0, ang))

    # Export GLB
    export_asset("landing_shockwave")

def build_thruster_plume():
    reset_scene()
    
    # Thruster mounting ring / nozzle
    add_cylinder("Nozzle_Ring", (0, 0, 0.0), radius=0.09, depth=0.05, mat='titanium', vertices=16)
    add_cylinder("Nozzle_Emitter", (0, 0, -0.02), radius=0.07, depth=0.03, mat='white', vertices=16)
    
    # Main plasma exhaust flame (cone pointing downwards)
    add_cone("Flame_OuterPlasma", (0, 0, -0.32), radius1=0.11, radius2=0.01, depth=0.6, mat='cyan', vertices=12, rot=(math.pi, 0, 0))
    
    # High-intensity inner core flame
    add_cone("Flame_InnerCore", (0, 0, -0.22), radius1=0.06, radius2=0.005, depth=0.4, mat='white', vertices=8, rot=(math.pi, 0, 0))

    # Export GLB
    export_asset("thruster_plume")

def export_asset(name):
    bpy.context.view_layer.update()
    parent = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(parent)
    
    asset_objs = [parent]
    for obj in list(bpy.context.scene.objects):
        if obj != parent and obj.type == 'MESH':
            obj.parent = parent
            asset_objs.append(obj)

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
        export_yup=True,
        export_animations=False
    )
    print(f"Successfully generated {glb_path} ({round(glb_path.stat().st_size / 1024, 1)} KB)")

if __name__ == "__main__":
    print("Building Fly-In VFX Assets in Blender 5.2...")
    build_landing_shockwave()
    build_thruster_plume()
    print("All VFX assets built successfully!")
