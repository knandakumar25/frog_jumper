import bpy
import bmesh
from pathlib import Path
from mathutils import Vector, Euler
import math

def apply_sword_fix_shogun(src_path, dst_path):
    print("=" * 70)
    print("APPLYING SWORD FIX: CYBER SHOGUN")
    print("=" * 70)
    
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(src_path))
    
    char1 = bpy.data.objects.get("char1")
    vgroups = {vg.name: vg.index for vg in char1.vertex_groups}
    rhand_idx = vgroups.get("RightHand")
    rfore_idx = vgroups.get("RightForeArm")
    
    # Identify the 1296 sword vertices
    sword_indices = []
    for idx, v in enumerate(char1.data.vertices):
        w_hand = 0.0
        w_fore = 0.0
        for g in v.groups:
            if g.group == rhand_idx: w_hand = g.weight
            elif g.group == rfore_idx: w_fore = g.weight
        if abs(w_hand - 0.5) < 0.01 and abs(w_fore - 0.5) < 0.01:
            sword_indices.append(idx)
            
    print(f"Cyber Shogun: found {len(sword_indices)} sword vertices")
    
    # Grip center in mesh space
    grip = Vector((-20.0, -33.0, 97.0))
    # Rotation angle from shogun_fine_2: (-35, -45, 35)
    rot_mat = Euler((math.radians(-35), math.radians(-45), math.radians(35)), 'XYZ').to_matrix()
    
    for idx in sword_indices:
        v = char1.data.vertices[idx]
        rel = v.co - grip
        v.co = grip + (rot_mat @ rel)
        
    # Assign 100% to RightHand, 0% to RightForeArm
    for idx in sword_indices:
        char1.vertex_groups[rhand_idx].add([idx], 1.0, 'REPLACE')
        if rfore_idx is not None:
            char1.vertex_groups[rfore_idx].remove([idx])
            
    char1.data.update()
    
    # Also apply the exact same transformation to duplicate meshes (char1.001 ... char1.042)
    # so that all mesh instances in the file stay consistent!
    for obj in bpy.data.objects:
        if obj.name.startswith("char1.") and obj.type == 'MESH':
            print(f"Updating duplicate mesh: {obj.name}")
            o_vg = {vg.name: vg.index for vg in obj.vertex_groups}
            o_rhand = o_vg.get("RightHand")
            o_rfore = o_vg.get("RightForeArm")
            for idx in sword_indices:
                if idx < len(obj.data.vertices):
                    v = obj.data.vertices[idx]
                    rel = v.co - grip
                    v.co = grip + (rot_mat @ rel)
                    if o_rhand is not None:
                        obj.vertex_groups[o_rhand].add([idx], 1.0, 'REPLACE')
                    if o_rfore is not None:
                        obj.vertex_groups[o_rfore].remove([idx])
            obj.data.update()
            
    # Export to GLB
    print(f"Exporting to {dst_path}...")
    bpy.ops.export_scene.gltf(
        filepath=str(dst_path),
        export_format='GLB',
        export_animations=True,
        export_all_influences=True,
        export_skins=True
    )
    print("Cyber Shogun export complete!")

def apply_sword_fix_mirage(src_path, dst_path):
    print("=" * 70)
    print("APPLYING SWORD FIX: MIRAGE BLADE")
    print("=" * 70)
    
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(src_path))
    
    char1 = bpy.data.objects.get("char1")
    vgroups = {vg.index: vg.name for vg in char1.vertex_groups}
    rhand_idx = next(vg.index for vg in char1.vertex_groups if vg.name == "RightHand")
    lhand_idx = next(vg.index for vg in char1.vertex_groups if vg.name == "LeftHand")
    rupleg_idx = next((vg.index for vg in char1.vertex_groups if vg.name == "RightUpLeg"), None)
    lupleg_idx = next((vg.index for vg in char1.vertex_groups if vg.name == "LeftUpLeg"), None)
    
    bm = bmesh.new()
    bm.from_mesh(char1.data)
    
    visited = set()
    islands = []
    for vert in bm.verts:
        if vert.index in visited:
            continue
        island = []
        q = [vert]
        visited.add(vert.index)
        while q:
            curr = q.pop()
            island.append(curr)
            for edge in curr.link_edges:
                other = edge.other_vert(curr)
                if other.index not in visited:
                    visited.add(other.index)
                    q.append(other)
        islands.append(island)
        
    r_dagger_islands = []
    l_dagger_islands = []
    
    for island in islands:
        coords = [v.co for v in island]
        center = sum(coords, Vector((0,0,0))) / len(coords)
        w_rhand = 0.0
        w_lhand = 0.0
        for vert in island:
            orig_v = char1.data.vertices[vert.index]
            for g in orig_v.groups:
                bname = vgroups.get(g.group)
                if bname == "RightHand": w_rhand += g.weight
                elif bname == "LeftHand": w_lhand += g.weight
        w_rhand /= len(island)
        w_lhand /= len(island)
        
        if center.y < -15.0 and center.z < 115.0:
            if w_rhand > 0.05 and center.x < -10.0:
                r_dagger_islands.append(island)
            elif w_lhand > 0.05 and center.x > 10.0:
                l_dagger_islands.append(island)
                
    r_verts = [v for isl in r_dagger_islands for v in isl]
    l_verts = [v for isl in l_dagger_islands for v in isl]
    
    r_grip = Vector((-55.0, -18.7, 101.0))
    l_grip = Vector((55.0, -18.7, 101.0))
    
    # Rotations from mirage_grid_g2:
    # Right: (0, 50, -50), Left: (-45, 20, 110)
    r_rot = Euler((math.radians(0), math.radians(50), math.radians(-50)), 'XYZ').to_matrix()
    l_rot = Euler((math.radians(-45), math.radians(20), math.radians(110)), 'XYZ').to_matrix()
    
    for v in r_verts:
        rel = v.co - r_grip
        v.co = r_grip + (r_rot @ rel)
        
    for v in l_verts:
        rel = v.co - l_grip
        v.co = l_grip + (l_rot @ rel)
        
    r_vert_indices = set(v.index for v in r_verts)
    l_vert_indices = set(v.index for v in l_verts)
    
    bm.to_mesh(char1.data)
    bm.free()
    
    for idx in r_vert_indices:
        char1.vertex_groups[rhand_idx].add([idx], 1.0, 'REPLACE')
        if rupleg_idx is not None:
            char1.vertex_groups[rupleg_idx].remove([idx])
            
    for idx in l_vert_indices:
        char1.vertex_groups[lhand_idx].add([idx], 1.0, 'REPLACE')
        if lupleg_idx is not None:
            char1.vertex_groups[lupleg_idx].remove([idx])
            
    char1.data.update()
    
    # Also update duplicates char1.001 ... char1.028
    for obj in bpy.data.objects:
        if obj.name.startswith("char1.") and obj.type == 'MESH':
            print(f"Updating duplicate mesh: {obj.name}")
            o_vg = {vg.name: vg.index for vg in obj.vertex_groups}
            o_rhand = o_vg.get("RightHand")
            o_lhand = o_vg.get("LeftHand")
            o_rupleg = o_vg.get("RightUpLeg")
            o_lupleg = o_vg.get("LeftUpLeg")
            for idx in r_vert_indices:
                if idx < len(obj.data.vertices):
                    v = obj.data.vertices[idx]
                    rel = v.co - r_grip
                    v.co = r_grip + (r_rot @ rel)
                    if o_rhand is not None: obj.vertex_groups[o_rhand].add([idx], 1.0, 'REPLACE')
                    if o_rupleg is not None: obj.vertex_groups[o_rupleg].remove([idx])
            for idx in l_vert_indices:
                if idx < len(obj.data.vertices):
                    v = obj.data.vertices[idx]
                    rel = v.co - l_grip
                    v.co = l_grip + (l_rot @ rel)
                    if o_lhand is not None: obj.vertex_groups[o_lhand].add([idx], 1.0, 'REPLACE')
                    if o_lupleg is not None: obj.vertex_groups[o_lupleg].remove([idx])
            obj.data.update()
            
    print(f"Exporting to {dst_path}...")
    bpy.ops.export_scene.gltf(
        filepath=str(dst_path),
        export_format='GLB',
        export_animations=True,
        export_all_influences=True,
        export_skins=True
    )
    print("Mirage Blade export complete!")

if __name__ == "__main__":
    root = Path("D:/Projects/frog_jumper")
    out_dir = Path("C:/Users/karthiknk21/.gemini/antigravity-cli/brain/7835afa7-94f8-4db5-9713-7de77eee6da6")
    apply_sword_fix_shogun(root / "assets/models/hq/cyber_shogun.glb", out_dir / "cyber_shogun_fixed.glb")
    apply_sword_fix_mirage(root / "assets/models/hq/mirage_blade.glb", out_dir / "mirage_blade_fixed.glb")
