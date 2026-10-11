"""Recreate CYBER SHOGUN from scratch -- original procedural model + animation.

Run: blender --background --factory-startup --python tools/assets/build_cyber_shogun_from_scratch.py
Optional: -- --overwrite (the file must be absent or that flag set)

This is a fresh authorship pass, not a restoration: the old Meshy-built
cyber_shogun.glb (its root-motion clips, its 55-degree torso oscillation, its
backward-slumped rest posture, its 50-plus duplicate actions) has been deleted,
and this script builds a brand-new character in its place.

Design brief (the project's own identity for this operative, honored but
re-drawn from zero): a cybernetic samurai, pure-martial, katana and parry,
zero particle VFX. Angular plated armor, glowing crest and obi core, a half
cape, a sheathed katana that the right hand draws.

Constraints encoded by the surrounding game that this rebuild must satisfy:
  - lives at assets/models/hq/cyber_shogun.glb so Player3D's CharacterRegistry
    and CharacterSelection resolve it by path;
  - the clip names are the animation contract (Player3D.PlayAbilityAnimation
    maps 'standing'/'walking'/'running'/etc. by name, and the delivery
    validator requires the standard set) -- all authored here, IN PLACE and
    upright by construction, with no root motion and no exaggerated pitches;
  - mesh objects must NOT be named Cube/Sphere/Plane/Icosphere: those names
    are purged by CharacterSelection.CleanDuplicateMeshes;
  - materials: PBR_ physical, EMISSIVE_ emissive (the validator wants at
    least one emissive with a real factor);
  - hq's model_count == 50 test gate: this pass restores exactly the one file
    that was removed, so the count lands back at 50.
"""
import bpy, sys, math, json, re
import bmesh
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'assets' / 'models' / 'hq'
PREVIEW = ROOT / '.validation' / 'shogun-rebuild'
ARGS = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
OVERWRITE = '--overwrite' in ARGS
ASSET = 'cyber_shogun'
HEIGHT = 1.85
PARTS = []
MAT = {}
RIG = None
BONES = {}

OUTPUT.mkdir(parents=True, exist_ok=True)
PREVIEW.mkdir(parents=True, exist_ok=True)


def material(name, color, metallic=0.0, roughness=.45, emission=0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    p = mat.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*color, 1)
    p.inputs['Metallic'].default_value = metallic
    p.inputs['Roughness'].default_value = roughness
    if emission:
        p.inputs['Emission Color'].default_value = (*color, 1)
        p.inputs['Emission Strength'].default_value = emission
    mat.diffuse_color = (*color, 1)
    mat['zone'] = 'independently_pulsable_emission' if emission else 'physical_surface'
    return mat


def begin():
    global PARTS, MAT, RIG, BONES
    PARTS = []; BONES = {}; RIG = None
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for act in list(bpy.data.actions):
        bpy.data.actions.remove(act)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat, do_unlink=True)
    MAT = {
        # material doctrine: the lacquer/armor pieces are DIELECTRIC (metal 0)
        # -- dark METALS mirror the scene light and render as a grey-blue
        # vinyl sheen, which is a robot cue; only the trim and gilt keep
        # metallic, because those are actual steel or brass.
        'plate': material('PBR_Obsidian_Plate', (.030, .042, .055), 0, .38),
        'carbon': material('PBR_Carbon_Weave', (.014, .020, .027), 0, .68),
        'trim': material('PBR_Titanium_Trim', (.26, .30, .34), .90, .38),
        'iron': material('PBR_Iron_Hardware', (.13, .15, .18), 0, .72),
        'gilt': material('PBR_Brass_Gilt', (.42, .30, .11), .95, .35),
        'fabric': material('PBR_Fabric_Indigo', (.13, .14, .28), .05, .85),
        'skin': material('PBR_Skin_Tone', (.70, .44, .32), 0, .72),
        'hair': material('PBR_Hair_Black', (.025, .028, .045), .1, .5),
        'crest': material('EMISSIVE_Cyan_Crest', (.008, .80, 1.0), .1, .25, 6.0),
        'core': material('EMISSIVE_Cyan_Core', (.0, .90, 1.0), .1, .22, 8.0),
        'accent': material('EMISSIVE_Magenta_Trim', (1.0, .05, .30), .1, .25, 5.0),
        'visor': material('EMISSIVE_White_Visor', (.90, .95, 1.0), .2, .25, 5.0),
        'eyes': material('EMISSIVE_Cyan_Eyes', (.2, .85, 1.0), .1, .3, 2.0),
    }


def finish_obj(obj, name, mat, bone='Hips', bevel=0, smooth=False):
    obj.name = name
    obj.data.name = name + '_Data'  # mesh-data names too: Godot's importer keys off names
    obj.data.materials.append(MAT[mat])
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('Manufactured_edge_bevel', 'BEVEL')
        mod.width = bevel; mod.segments = 2
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    if smooth:
        # set the flag directly: bpy.ops.object.shade_smooth() acts on the
        # ACTIVE object, and lofts/shells created via bpy.data.objects.new()
        # never become active -- their smooth flags would silently not apply
        for face in obj.data.polygons:
            face.use_smooth = True
    else:
        for face in obj.data.polygons:
            face.use_smooth = False
    obj['role'] = name
    PARTS.append((obj, bone))
    return obj


def box(name, loc, scale, mat='plate', bone='Hips', bevel=.02, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.dimensions = scale
    return finish_obj(obj, name, mat, bone, bevel)


def cyl(name, a, b, r, mat='iron', bone='Hips', vertices=12, r2=None):
    a, b = Vector(a), Vector(b)
    d = b - a
    if r2 is None:
        bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=r, depth=d.length, location=(a + b) / 2)
    else:
        bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=r, radius2=r2, depth=d.length, location=(a + b) / 2)
    obj = bpy.context.object
    obj.rotation_euler = d.to_track_quat('Z', 'Y').to_euler()
    return finish_obj(obj, name, mat, bone, min(r * .14, .012))


def orb(name, loc, scale, mat='iron', bone='Hips', smooth=False, segments=16, rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=1, location=loc)
    obj = bpy.context.object
    obj.scale = scale
    return finish_obj(obj, name, mat, bone, smooth=smooth)


def shell(name, loc, scale, mat, bone, keep='front', smooth=True, segments=24, rings=12, zband=None, rot=None):
    """An armour shell from a half-sphere: keep='front' (y<0, the face side),
    'back' (y>0), 'outer-x+', 'outer-x-' or 'lower' (z<0). zband=(lo,hi) keeps
    only a vertical slice of the unit sphere (lamellar bands). A curved plate
    shape, never a full ball."""
    rules = {'front': lambda c: c.y < -.05,
             'back': lambda c: c.y > .05,
             'lower': lambda c: c.z < 0.0,
             'outer-x+': lambda c: c.x > .05,
             'outer-x-': lambda c: c.x < -.05}
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=1, location=loc)
    obj = bpy.context.object
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    rule = rules[keep]
    if zband:
        base_rule = rule

        def rule(c):
            return base_rule(c) and zband[0] < c.z < zband[1]
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if not rule(f.calc_center_median())],
                     context='FACES_ONLY')
    bm.to_mesh(obj.data)
    bm.free()
    if rot:
        obj.rotation_euler = rot
    obj.scale = scale
    return finish_obj(obj, name, mat, bone, smooth=smooth)


def loft(name, base, z0, z1, profile, mat='plate', bone='Chest', segments=16, smooth=True, rings_n=9):
    """Sweep an elliptical cross-section up the body from a center offset as a
    bmesh sampled at rings_n heights: profile(t) gives (half_width_x,
    half_depth_y) at height fraction t=0..1. Real intermediate rings mean the
    taper and the curvature shade properly -- a 2-ring tube smooth-shaded
    would just glob into an egg."""
    bm = bmesh.new()
    prev = None
    for i in range(rings_n):
        t = i / (rings_n - 1)
        z = z0 + (z1 - z0) * t
        w, d = profile(t)
        vs = [bm.verts.new((base[0] + w * math.cos(2 * math.pi * j / segments),
                            base[1] + d * math.sin(2 * math.pi * j / segments), z))
              for j in range(segments)]
        if prev is not None:
            for j in range(segments):
                bm.faces.new((prev[j], prev[(j + 1) % segments], vs[(j + 1) % segments], vs[j]))
        prev = vs
    bm.faces.new(prev[::-1])  # top cap
    bm.normal_update()
    mesh = bpy.data.meshes.new(name + '_Data')
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return finish_obj(obj, name, mat, bone, smooth=smooth)


def label(text_, loc, size=.1, mat='trim', bone='Hips', rot=(math.pi / 2, 0, 0)):
    bpy.ops.object.text_add(location=loc, rotation=rot)
    obj = bpy.context.object
    obj.data.body = text_
    obj.data.size = size
    obj.data.align_x = 'CENTER'
    obj.data.extrude = .0008
    bpy.ops.object.convert(target='MESH')
    return finish_obj(bpy.context.object, 'Marking_' + text_.replace(' ', '_'), mat, bone)


# ----------------------------------------------------------------------------
# the rig: 21 bones, L/R-symmetric naming, hips vertical by construction
# ----------------------------------------------------------------------------
BONE_TABLE = [
    # name, head, tail, parent
    ('Hips',        (0, 0, 0),        (0, 0, 0.98),  None),
    ('Spine',       (0, 0, 0.98),     (0, 0, 1.16),  'Hips'),
    ('Chest',       (0, 0, 1.16),     (0, 0, 1.46),  'Spine'),
    ('Neck',        (0, 0, 1.46),     (0, 0, 1.53),  'Chest'),
    ('Head',        (0, 0, 1.53),     (0, 0, 1.63),  'Neck'),
]


def build_rig():
    global RIG, BONES
    bpy.context.view_layer.update()
    parts_alive = []
    for obj, bone in PARTS:
        try:
            _ = obj.type
            parts_alive.append(obj)
        except ReferenceError:
            print('DEAD_PART dropped at grounding', flush=True)
    floor = min((obj.matrix_world @ Vector(c)).z for obj in parts_alive for c in obj.bound_box)
    if abs(floor) > 1e-6:
        for obj, bone in PARTS:
            obj.location.z -= floor
    arm = bpy.data.armatures.new(ASSET + '_Skeleton')
    RIG = bpy.data.objects.new(ASSET + '_Rig', arm)
    bpy.context.collection.objects.link(RIG)
    bpy.context.view_layer.objects.active = RIG
    RIG.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    for name, head, tail, parent in BONE_TABLE:
        b = arm.edit_bones.new(name)
        b.head = head; b.tail = tail
        if parent:
            b.parent = arm.edit_bones[parent]
        BONES[name] = (head, tail, parent)
    for side in ('L', 'R'):
        s = 1 if side == 'L' else -1
        specs = [
            ('Clavicle.' + side, (s * .10, 0, 1.44), (s * .24, 0, 1.46), 'Chest'),
            ('UpperArm.' + side, (s * .24, 0, 1.44), (s * .235, -.01, 1.14), 'Clavicle.' + side),
            ('ForeArm.' + side,  (s * .235, -.01, 1.14), (s * .23, 0, .90), 'UpperArm.' + side),
            ('Hand.' + side,     (s * .23, 0, .90),  (s * .23, -.01, .82), 'ForeArm.' + side),
            ('UpLeg.' + side,    (s * .11, 0, .92),  (s * .11, -.01, .54), 'Hips'),
            ('Leg.' + side,      (s * .11, -.01, .54), (s * .115, 0, .10), 'UpLeg.' + side),
            ('Foot.' + side,     (s * .115, 0, .10), (s * .115, -.13, .02), 'Leg.' + side),
        ]
        for name, head, tail, parent in specs:
            b = arm.edit_bones.new(name)
            b.head = head; b.tail = tail
            b.parent = arm.edit_bones[parent]
            BONES[name] = (head, tail, parent)
    # align every bone's roll to the global axes, so each pose bone's local
    # x = world x (sagittal swings), local z = world z (yaws) regardless of
    # which way the bone itself points. Without this, a pitch written into a
    # vertical bone's euler-Y would twist instead of lean.
    bpy.ops.object.mode_set(mode='EDIT')  # (already in edit; selects active only)
    for b in arm.edit_bones:
        b.select = True
    bpy.context.view_layer.objects.active = RIG
    bpy.ops.armature.calculate_roll(type='GLOBAL_POS_Y')
    bpy.ops.object.mode_set(mode='OBJECT')
    RIG['units'] = 'meters'
    RIG['origin'] = 'feet/bottom-center'
    RIG['forward'] = '-Y in Blender; +Z in exported Godot glTF convention'
    return RIG


def attach_to_rig():
    """Consolidate the parts by material into skinned meshes on the rig."""
    grouped = {}
    for obj, bone in PARTS:
        vg = obj.vertex_groups.new(name=bone)
        vg.add(list(range(len(obj.data.vertices))), 1, 'REPLACE')
        grouped.setdefault(obj.data.materials[0].name, []).append(obj)
    for material_name, objects in sorted(grouped.items()):
        bpy.ops.object.select_all(action='DESELECT')
        for obj in objects:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = objects[0]
        bpy.ops.object.join()
        obj = bpy.context.object
        obj.name = ASSET + '__' + material_name
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        mod = obj.modifiers.new('Rigid skinning', 'ARMATURE')
        mod.object = RIG
        obj.parent = RIG


# ----------------------------------------------------------------------------
# the model: angular plated samurai armor
# ----------------------------------------------------------------------------
def torso_profile(t):
    """Waist -> chest: narrow at the waist, swells through the ribs, rounds
    off at the shoulders. Piecewise control points, linear between."""
    pts = [(0.0, .150, .112), (0.28, .152, .122), (0.55, .180, .128),
           (0.82, .222, .142), (1.0, .192, .126)]
    for i in range(len(pts) - 1):
        t0, w0, d0 = pts[i]
        t1, w1, d1 = pts[i + 1]
        if t0 <= t <= t1:
            f = (t - t0) / (t1 - t0)
            return w0 + (w1 - w0) * f, d0 + (d1 - d0) * f
    return pts[-1][1], pts[-1][2]


def shin_profile(t):
    """Calf bulge low on the shin, tapering to the ankle."""
    bulge = math.exp(-((t - .35) / .34) ** 2)
    return .042 + .018 * bulge, .040 + .014 * bulge


def face_profile(t):
    """Jaw -> crown: narrow at the chin, swelling through the cheeks."""
    pts = [(0.0, .058, .075), (0.3, .072, .084), (0.62, .082, .088),
           (0.85, .088, .088), (1.0, .092, .086)]
    for i in range(len(pts) - 1):
        t0, w0, d0 = pts[i]
        t1, w1, d1 = pts[i + 1]
        if t0 <= t <= t1:
            f = (t - t0) / (t1 - t0)
            return w0 + (w1 - w0) * f, d0 + (d1 - d0) * f
    return pts[-1][1], pts[-1][2]


def build_model():
    """Organic reading, armored operative: a lofted torso with a real waist
    taper, tapered limb cylinders, sphere joints, rounded plate shells. The
    armour stays hard-surface exactly where the design wants it (kabuto,
    menpo, obi, katana); everything the body's living shape drives is smooth
    and continuous."""
    # --- the living form under the armour ---------------------------------
    loft('Torso_form', (0, 0), .99, 1.46, torso_profile, 'carbon', 'Chest', 24)
    orb('Pelvis_form', (0, 0, .93), (.185, .15, .125), 'carbon', 'Hips', smooth=True)
    cyl('Neck_form', (0, 0, 1.40), (0, 0, 1.545), .052, 'skin', 'Neck', 10)
    orb('Skull_form', (0, -.005, 1.60), (.075, .085, .09), 'skin', 'Head', smooth=True)

    # --- arms: tapered tubes with nested elbows and mitt hands ------------
    # the robot read here came from ball joints showing at the elbow and
    # sphere "fists": the tubes now overlap the (hiding) elbow, taper to the
    # wrist, and the hand is a shaped mitt with a thumb nub
    for side in ('L', 'R'):
        s = 1 if side == 'L' else -1
        b = side
        cyl('Upper_arm_' + b, (s * .24, 0, 1.42), (s * .235, -.015, 1.145), .052, 'carbon', 'UpperArm.' + b, 12, r2=.040)
        orb('Elbow_' + b, (s * .233, -.025, 1.14), (.038, .038, .034), 'carbon', 'ForeArm.' + b, smooth=True)
        # the forearm hangs a touch forward and outward of the torso plane: a
        # natural standing carriage instead of a mannequin's straight drop
        cyl('Forearm_' + b, (s * .233, -.02, 1.14), (s * .248, -.075, .885), .044, 'carbon', 'ForeArm.' + b, 12, r2=.030)
        cyl('Hand_mitt_' + b, (s * .246, -.078, .885), (s * .24, -.05, .80), .03, 'carbon', 'Hand.' + b, 8, r2=.021)
        orb('Thumb_' + b, (s * .243, -.092, .845), (.013, .013, .018), 'carbon', 'Hand.' + b, smooth=True)

    # --- legs: tapered thighs, calf-profile shins, boots at a stance ------
    for side in ('L', 'R'):
        s = 1 if side == 'L' else -1
        b = side
        cyl('Thigh_' + b, (s * .11, 0, .93), (s * .11, -.01, .53), .082, 'carbon', 'UpLeg.' + b, 12, r2=.058)
        loft('Shin_' + b, (s * .115, 0), .09, .53, shin_profile, 'carbon', 'Leg.' + b, 16)
        orb('Knee_' + b, (s * .112, -.05, .53), (.058, .052, .054), 'carbon', 'Leg.' + b, smooth=True)

    # --- armour: curved shells over the form, never balloons ---------------
    for side in ('L', 'R'):
        s = 1 if side == 'L' else -1
        b = side
        # the sode: three stacked lamellar bands over the shoulder; band
        # z-ranges overlap (gaps read as floating plates) and each band is
        # tilted to hug the upper arm instead of jutting past it -- the old
        # footprint put the outer edge 12 cm beyond the arm: fridge shoulders
        shell('Sode_top_' + b, (s * .225, -.005, 1.435), (.085, .11, .15), 'plate', 'Clavicle.' + b,
              keep='outer-x+' if s > 0 else 'outer-x-', zband=(.12, 1.01), segments=16, rings=8, rot=(0, s * -.25, 0))
        shell('Sode_mid_' + b, (s * .228, 0, 1.375), (.085, .10, .15), 'plate', 'UpperArm.' + b,
              keep='outer-x+' if s > 0 else 'outer-x-', zband=(-.55, .18), segments=16, rings=8, rot=(0, s * -.22, 0))
        shell('Sode_low_' + b, (s * .23, .005, 1.315), (.088, .10, .15), 'plate', 'UpperArm.' + b,
              keep='outer-x+' if s > 0 else 'outer-x-', zband=(-1.05, -.5), segments=16, rings=8, rot=(0, s * -.18, 0))
        box('Pauldron_fin_' + b, (s * .30, .02, 1.49), (.022, .15, .04), 'gilt', 'Clavicle.' + b, .003)
        shell('Thigh_guard_' + b, (s * .11, -.028, .70), (.06, .062, .145), 'plate', 'UpLeg.' + b, keep='front')
        shell('Knee_cop_' + b, (s * .112, -.055, .528), (.06, .062, .056), 'gilt', 'Leg.' + b, keep='lower', segments=16, rings=8)
        # boots: flat sole so they ground cleanly; the stance sits a touch
        # wider than the thigh line so the rest pose reads as standing, not
        # mannequin. Lacquered plate, not the hardware grey -- pale boots
        # glowed under the podium key light like sneakers.
        box('Boot_sole_' + b, (s * .125, -.065, .026), (.10, .21, .052), 'plate', 'Foot.' + b, .01)
        shell('Sabaton_toe_' + b, (s * .125, -.09, .05), (.05, .1, .048), 'plate', 'Foot.' + b, keep='lower', segments=16, rings=8)
        box('Boot_heel_' + b, (s * .125, .05, .05), (.08, .08, .10), 'plate', 'Foot.' + b, .012)
    shell('Breastplate', (0, -.015, 1.315), (.17, .12, .15), 'plate', 'Chest', keep='front')
    box('Chest_ridge', (0, -.132, 1.33), (.04, .032, .24), 'gilt', 'Chest', .004)
    shell('Backplate', (0, .02, 1.30), (.155, .11, .14), 'plate', 'Chest', keep='back')
    box('Shoulder_yoke', (0, .01, 1.452), (.42, .13, .05), 'plate', 'Chest', .02)

    # --- obi belt + glowing core -------------------------------------------
    box('Obi_belt', (0, -.005, 1.02), (.30, .21, .09), 'carbon', 'Hips', .02)
    box('Obi_plate', (0, -.115, 1.02), (.15, .02, .10), 'gilt', 'Hips', .008)
    cyl('Energy_core', (0, -.135, 1.02), (0, -.162, 1.02), .042, 'core', 'Hips', 6)

    # --- fauld skirt: smooth flared plate cone over a fabric tabard --------
    cyl('Fauld_skirt', (0, 0, 1.00), (0, 0, .845), .19, 'plate', 'Hips', 20, r2=.232)
    cyl('Fauld_hem_ring', (0, 0, .845), (0, 0, .852), .235, 'accent', 'Hips', 24)
    cyl('Tabard_skirt', (0, 0, .99), (0, 0, .83), .158, 'fabric', 'Hips', 20, r2=.148)

    # --- half cape from the shoulders, hanging behind the backplate --------
    box('Cape_panel', (0, .195, 1.11), (.30, .018, .56), 'fabric', 'Chest', .006, rot=(-.06, 0, 0))
    box('Cape_hem', (0, .222, .826), (.31, .02, .025), 'fabric', 'Spine', .004, rot=(-.06, 0, 0))
    box('Cape_clasp_L', (.10, .10, 1.44), (.05, .075, .04), 'gilt', 'Chest', .01)

    # --- the open face: skin loft with a jaw taper (the same loft trick the
    # torso uses), dark stylized eyes + brows, a nose, and the menpo pushed
    # down to a steel chin cup. Face = flesh; armour = bowl, brim, wings.
    loft('Face_mask', (0, -.012), 1.465, 1.615, face_profile, 'skin', 'Head', 20)
    orb('Skull_under_bowl', (0, -.005, 1.585), (.072, .082, .07), 'skin', 'Head', smooth=True)
    for side in ('L', 'R'):
        s = 1 if side == 'L' else -1
        box('Eye_' + side, (s * .031, -.099, 1.547), (.022, .011, .01), 'eyes', 'Head', .002)
        box('Brow_' + side, (s * .032, -.095, 1.565), (.032, .01, .006), 'hair', 'Head', .002)
    box('Nose_ridge', (0, -.104, 1.53), (.018, .016, .024), 'skin', 'Head', .004)
    shell('Menpo_chin_cup', (0, -.062, 1.482), (.066, .033, .05), 'plate', 'Head', keep='front', segments=16, rings=8)
    box('Mouth_line', (0, -.0985, 1.528), (.03, .008, .004), 'hair', 'Head', .001)
    cyl('Helmet_bowl', (0, -.015, 1.617), (0, -.015, 1.715), .115, 'plate', 'Head', 14, r2=.082)
    box('Helmet_brim', (0, -.095, 1.612), (.165, .075, .016), 'plate', 'Head', .005, rot=(-.35, 0, 0))
    box('Hair_nape', (0, .048, 1.515), (.085, .055, .09), 'hair', 'Head', .02)
    orb('Chikara_bun', (0, .095, 1.705), (.038, .042, .048), 'hair', 'Head', smooth=True)
    box('Shikoro_left', (-.10, .075, 1.60), (.10, .02, .14), 'plate', 'Head', .004, rot=(-.5, 0, 0))
    box('Shikoro_right', (.10, .075, 1.60), (.10, .02, .14), 'plate', 'Head', .004, rot=(-.5, 0, 0))
    box('Shikoro_center', (0, .085, 1.59), (.09, .02, .12), 'plate', 'Head', .004, rot=(-.55, 0, 0))
    box('Fukigaeshi_left', (-.105, .065, 1.70), (.055, .05, .075), 'gilt', 'Head', .005, rot=(0, .5, 0))
    box('Fukigaeshi_right', (.105, .065, 1.70), (.055, .05, .075), 'gilt', 'Head', .005, rot=(0, -.5, 0))
    box('Crest_fin', (0, .055, 1.78), (.018, .05, .10), 'crest', 'Head', .003)
    box('Crest_fin_upper', (0, .06, 1.845), (.014, .04, .05), 'crest', 'Head', .002)
    box('Crest_base', (0, .05, 1.718), (.03, .06, .04), 'gilt', 'Head', .004)
    cyl('Helmet_antenna', (.10, .02, 1.60), (.14, .06, 1.76), .007, 'trim', 'Head', 6)

    # --- the katana, worn OUTSIDE the fauld skirt at the left hip ----------
    cyl('Katana_scabbard', (0.24, -0.03, 0.90), (0.255, 0.20, 1.12), .021, 'carbon', 'Hips', 10)
    cyl('Scabbard_mouth', (0.24, -0.03, 0.90), (0.242, 0.01, 0.945), .024, 'iron', 'Hips', 10)
    cyl('Katana_hilt', (0.24, -0.03, 0.90), (0.20, -0.235, 0.815), .013, 'iron', 'Hips', 8)
    for i in range(4):  # tsuka-ito wrap diamonds
        f = i / 3.0
        box('Hilt_wrap_%d' % i, (0.24 - .04 * f, -0.03 - .205 * f, 0.90 - .085 * f), (.03, .012, .02), 'accent', 'Hips', .002)
    box('Katana_guard', (0.24, -0.048, 0.888), (.05, .022, .05), 'gilt', 'Hips', .003)
    box('Scabbard_tip_trim', (0.255, 0.195, 1.115), (.05, .02, .03), 'gilt', 'Hips', .003)

    # --- the operative's own designation, low on the tabard ----------------
    label('SHOGUN', (0, -.165, .96), .05, 'gilt', 'Hips')


# ----------------------------------------------------------------------------
# animation: every clip authored in place; pitch magnitudes hand-picked small
# ----------------------------------------------------------------------------
def clip(name, length, pose_fn):
    bpy.context.view_layer.objects.active = RIG
    RIG.animation_data_create()
    RIG.animation_data.action = None
    act = bpy.data.actions.new(name)
    RIG.animation_data.action = act
    frames = list(range(1, length + 1))
    for f in frames:
        t = (f - 1) / (length - 1)
        for pb in RIG.pose.bones:
            pb.rotation_mode = 'QUATERNION'
            pb.rotation_quaternion = (1, 0, 0, 0)
            pb.location = (0, 0, 0)
            pb.scale = (1, 1, 1)
        pose_fn(t, RIG.pose.bones)
        for pb in RIG.pose.bones:
            pb.keyframe_insert('location', frame=f, group=pb.name)
            pb.keyframe_insert('rotation_quaternion', frame=f, group=pb.name)
            pb.keyframe_insert('scale', frame=f, group=pb.name)
    RIG.animation_data.action = None
    track = RIG.animation_data.nla_tracks.new()
    track.name = name
    strip = track.strips.new(name, 1, act)
    strip.name = name
    strip.extrapolation = 'NOTHING'
    return name


def q(pb, bone, pitch_deg=0.0, yaw_deg=0.0, roll_deg=0.0):
    """Pose a bone. pitch = the sagittal swing; +pitch means FORWARD (toward
    the face) for EVERY bone -- for the torso this is applied as a negative
    euler-X (Godot-verified 2026-10-11: on the reimported skeleton, +euler-X
    on the torso chain pitches the head BACKWARD, i.e. Death@+65 measured a
    47 deg BACKWARD collapse; limbs take +euler-X = backward directly).
    yaw = sidesway/lean about the vertical; roll = twist about the bone axis.
    Child bones compose with their parent's animated frame (elbows/knees bend
    relatively, as they should). Degrees in.
    """
    from mathutils import Euler
    if bone in ('Hips', 'Spine', 'Chest', 'Neck', 'Head'):
        pitch_deg = -pitch_deg
    e = Euler((math.radians(pitch_deg), math.radians(roll_deg), math.radians(yaw_deg))).to_quaternion()
    pb[bone].rotation_mode = 'QUATERNION'
    pb[bone].rotation_quaternion = e


def translate(pb, bone, dx, dy, dz):
    pb[bone].location = (pb[bone].location.x + dx, pb[bone].location.y + dy, pb[bone].location.z + dz)


def build_clips():
    """Sign convention (q() normalizes): pitch + = torso tips toward the face,
    arm/leg forward swings are - (they start pointing down)."""
    clips = []

    def idle(t, p):
        ph = t * 2 * math.pi
        q(p, 'Chest', -2.0 * math.sin(ph / 2))
        q(p, 'Head', 3.0 * math.sin(ph), 6.0 * math.sin(ph / 2))
        q(p, 'UpperArm.L', 3.0 * math.sin(ph / 2))
        q(p, 'UpperArm.R', -2.0 * math.sin(ph / 2))
        translate(p, 'Chest', 0, 0, .008 * math.sin(ph / 2))

    def standing(t, p):
        ph = t * 2 * math.pi
        q(p, 'Chest', 1.5)  # chest-out heroic rest
        q(p, 'Head', 2.0 * math.sin(ph))
        q(p, 'UpperArm.L', 2.0 * math.sin(ph))

    def idle02(t, p):
        ph = t * 2 * math.pi
        q(p, 'Chest', 2.5, 4.0 * math.sin(ph / 2))
        q(p, 'UpperArm.R', 0.0, 25.0)   # reach across to the katana hilt (left
        q(p, 'ForeArm.R', -45.0)        # hip, world +X) and rest the hand there
        q(p, 'Hand.R', -8.0)
        q(p, 'Head', 2.0 * math.sin(ph), 10.0 * math.sin(ph / 2))
        translate(p, 'Chest', 0, 0, .005 * math.sin(ph / 2))

    def walkcycle(t, p, amp=24.0, arm=16.0, bob=.025, lean=2.0):
        ph = 2 * math.pi * t
        q(p, 'Hips', lean, 3.0 * math.sin(ph / 2))
        translate(p, 'Hips', 0, 0, bob * math.sin(2 * ph) - bob * .55)
        q(p, 'UpLeg.L', amp * math.sin(ph))
        q(p, 'UpLeg.R', amp * math.sin(ph + math.pi))
        q(p, 'Leg.L', 14.0 * max(0.0, math.sin(ph + math.pi / 2)))
        q(p, 'Leg.R', 14.0 * max(0.0, math.sin(ph - math.pi / 2)))
        q(p, 'Foot.L', -6.0 * math.sin(ph))
        q(p, 'Foot.R', -6.0 * math.sin(ph + math.pi))
        q(p, 'UpperArm.L', arm * math.sin(ph))
        q(p, 'UpperArm.R', arm * math.sin(ph + math.pi) + 10.0)  # sword arm rides slightly back
        q(p, 'ForeArm.L', -18.0)
        q(p, 'ForeArm.R', -30.0)
        q(p, 'Chest', lean, 5.0 * math.sin(ph))
        q(p, 'Head', -2.0, -3.0 * math.sin(ph))  # eyes stay level

    def runcycle(t, p):
        walkcycle(t, p, amp=38.0, arm=34.0, bob=.04, lean=8.0)  # lean INTO the stride

    def jumping(t, p):
        if t < .35:  # crouch: torso folds forward, heels tuck back
            e = t / .35
            q(p, 'Hips', 14.0 * e)
            translate(p, 'Hips', 0, 0, -.13 * e)
            q(p, 'UpLeg.L', -22.0 * e); q(p, 'UpLeg.R', -22.0 * e)
            q(p, 'Leg.L', 32.0 * e); q(p, 'Leg.R', 32.0 * e)
        elif t < .7:  # launch + hang: front leg tucks, back leg trails
            e = (t - .35) / .35
            q(p, 'Hips', 14.0 * (1 - e) - 4.0 * e)
            translate(p, 'Hips', 0, 0, -.13 * (1 - e) + .16 * math.sin(math.pi * e))
            q(p, 'UpLeg.L', -22.0 * (1 - e) - 30.0 * e)
            q(p, 'UpLeg.R', -22.0 * (1 - e))
            q(p, 'Leg.L', 32.0 * (1 - e) + 20.0 * e); q(p, 'Leg.R', 15.0 * e)
            q(p, 'UpperArm.L', -30.0 * e); q(p, 'UpperArm.R', -30.0 * e)
        else:  # land: absorb through the knees
            e = (t - .7) / .3
            translate(p, 'Hips', 0, 0, .16 * math.cos(math.pi / 2 * e) - .08 * math.sin(math.pi * e))
            q(p, 'Hips', -4.0 * (1 - e) - 8.0 * math.sin(math.pi * e))
            q(p, 'UpLeg.L', -30.0 * (1 - e) - 10.0 * math.sin(math.pi * e))
            q(p, 'UpLeg.R', -12.0 * math.sin(math.pi * e))
            q(p, 'Leg.L', 30.0 * e + 15.0 * math.sin(math.pi * e))
            q(p, 'Leg.R', 12.0 * math.sin(math.pi * e))
            q(p, 'UpperArm.L', -30.0 * (1 - e))
            q(p, 'UpperArm.R', -20.0 * math.sin(math.pi * e))

    clips += [clip('Idle', 61, idle), clip('Standing', 31, standing), clip('Idle_02', 61, idle02)]
    clips += [clip('Walking', 32, lambda t, p: walkcycle(t, p))]
    clips += [clip('Running', 24, runcycle), clip('Jumping', 25, jumping)]

    def double_jump(t, p):
        # a tuck-kick at apex; Player3D plays it on the mid-air second jump
        e = math.sin(math.pi * t)
        translate(p, 'Hips', 0, 0, .05 * e)
        q(p, 'UpLeg.L', -40.0 * e)
        q(p, 'UpLeg.R', 30.0 * e)
        q(p, 'Leg.L', 45.0 * e)
        q(p, 'Leg.R', 10.0 * e)
        q(p, 'UpperArm.L', 30.0 * e)
        q(p, 'UpperArm.R', 25.0 * e)
        q(p, 'Chest', -6.0 * e)

    def death(t, p):
        e = t * t * (3 - 2 * t)  # a slow-in stagger then a forward collapse
        q(p, 'Hips', 65.0 * e, 8.0 * e)
        translate(p, 'Hips', 0, 0, -.22 * e)
        q(p, 'Chest', 20.0 * e)
        q(p, 'Head', 10.0 * e, 10.0 * e)
        q(p, 'UpperArm.L', -18.0 * e, 0, 30.0 * e)
        q(p, 'UpperArm.R', 25.0 * e, 0, -25.0 * e)
        q(p, 'UpLeg.L', -18.0 * e)
        q(p, 'Leg.R', 40.0 * e)

    def hit(t, p):
        e = math.sin(math.pi * t)
        q(p, 'Hips', -10.0 * e, -6.0 * e)  # driven back
        q(p, 'Chest', -12.0 * e)
        q(p, 'Head', -8.0 * e)
        translate(p, 'Hips', 0, -.04 * e, 0)
        q(p, 'UpperArm.L', -12.0 * e)
        q(p, 'ForeArm.L', 20.0 * e)

    def slash(t, p):
        # katana: windup back-high -> horizontal cut -> settle
        if t < .3:
            e = t / .3
            q(p, 'UpperArm.R', 40.0 * e, -60.0 * e)
            q(p, 'ForeArm.R', -70.0 * e)
            q(p, 'Hips', -4.0 * e, -22.0 * e)
        elif t < .65:
            e = (t - .3) / .35
            q(p, 'UpperArm.R', 40.0 - 65.0 * e, -60.0 + 105.0 * e)
            q(p, 'ForeArm.R', -70.0 + 60.0 * e)
            q(p, 'Hips', -4.0 + 14.0 * e, -22.0 + 52.0 * e)
        else:
            e = (t - .65) / .35
            q(p, 'UpperArm.R', -25.0 + 20.0 * e, 45.0 - 40.0 * e)
            q(p, 'ForeArm.R', -10.0 + 8.0 * e)
            q(p, 'Hips', 10.0 - 12.0 * e, 30.0 - 28.0 * e)

    def thrust(t, p):
        if t < .35:  # pull back, blade cocked alongside
            e = t / .35
            q(p, 'UpperArm.R', 15.0 * e, -30.0 * e)
            q(p, 'ForeArm.R', -95.0 * e)
            q(p, 'Chest', -3.0 * e, -14.0 * e)
        elif t < .6:  # the stab
            e = (t - .35) / .25
            q(p, 'UpperArm.R', 15.0 - 60.0 * e, -30.0 + 35.0 * e)
            q(p, 'ForeArm.R', -95.0 + 90.0 * e)
            q(p, 'Chest', -3.0 + 14.0 * e, -14.0 + 28.0 * e)
        else:
            e = (t - .6) / .4
            q(p, 'UpperArm.R', -45.0 + 40.0 * e)
            q(p, 'ForeArm.R', -5.0)
            q(p, 'Chest', 11.0 - 11.0 * e, 14.0 - 14.0 * e)

    def karate(t, p):
        if t < .4:  # cock the fist: elbow back, fist folded up at the chest
            e = t / .4
            q(p, 'UpperArm.R', 15.0 * e, 30.0 * e)
            q(p, 'ForeArm.R', -95.0 * e)
        elif t < .65:  # the punch: the whole arm unfolds forward
            e = (t - .4) / .25
            q(p, 'UpperArm.R', 15.0 - 85.0 * e, 30.0 - 30.0 * e)
            q(p, 'ForeArm.R', -95.0 + 88.0 * e)
            q(p, 'Chest', -2.0 + 8.0 * e, -10.0 + 24.0 * e)
        else:
            e = (t - .65) / .35
            q(p, 'UpperArm.R', -70.0 + 70.0 * e)
            q(p, 'ForeArm.R', -7.0 + 7.0 * e)
            q(p, 'Chest', 6.0 - 6.0 * e, 14.0 - 14.0 * e)

    def slam(t, p):
        if t < .4:  # both hands lift overhead
            e = t / .4
            q(p, 'UpperArm.R', 135.0 * e)
            q(p, 'ForeArm.R', 20.0 * e)
            q(p, 'UpperArm.L', 135.0 * e)
            q(p, 'Hips', -12.0 * e)
            q(p, 'Chest', -10.0 * e)
        elif t < .62:  # the chop + crouch
            e = (t - .4) / .22
            q(p, 'UpperArm.R', 135.0 - 155.0 * e)
            q(p, 'UpperArm.L', 135.0 - 155.0 * e)
            q(p, 'ForeArm.R', 20.0 + 10.0 * e)
            q(p, 'Hips', -12.0 + 26.0 * e)
            q(p, 'Chest', -10.0 + 24.0 * e)
            translate(p, 'Hips', 0, 0, -.06 * e)
        else:
            e = (t - .62) / .38
            q(p, 'UpperArm.R', -20.0 + 20.0 * e)
            q(p, 'UpperArm.L', -20.0 + 20.0 * e)
            q(p, 'Hips', 14.0 - 14.0 * e)
            q(p, 'Chest', 14.0 - 14.0 * e)
            translate(p, 'Hips', 0, 0, -.06 * (1 - e))

    def dash(t, p):
        e = math.sin(math.pi * t)  # wind up then release forward
        q(p, 'Hips', 16.0 * e)
        translate(p, 'Hips', 0, 0, -.09 * e)
        q(p, 'UpLeg.L', -46.0 * e)
        q(p, 'UpLeg.R', 24.0 * e)
        q(p, 'Leg.R', 48.0 * e)
        q(p, 'UpperArm.R', 50.0 * (1 - e) - 60.0 * e, -30.0 * (1 - e))
        q(p, 'ForeArm.R', -60.0 * (1 - e) + 55.0 * e)
        q(p, 'UpperArm.L', 20.0 * e)
        q(p, 'Chest', 14.0 * e)

    def parry(t, p):
        e = math.sin(2 * math.pi * t)
        q(p, 'UpperArm.R', -35.0)
        q(p, 'ForeArm.R', -75.0)  # blade raised across the guard
        q(p, 'Hand.R', -15.0)
        q(p, 'Hips', 2.0 + 3.0 * e, 8.0 + 2.0 * e)
        q(p, 'Chest', 3.0)
        q(p, 'Head', 3.0 + 2.0 * e, 6.0 + 2.0 * e)

    clips.append(clip('Double_Jump', 25, double_jump))
    clips += [clip('Death', 41, death), clip('Hit', 15, hit)]
    clips += [clip('Attack', 29, slash), clip('Thrust_Slash', 33, thrust)]
    clips += [clip('Karate_Strike', 23, karate), clip('Ground_Slam', 37, slam)]
    clips += [clip('Dash', 21, dash), clip('Parry_Stance', 31, parry)]
    return clips


# ----------------------------------------------------------------------------
# export + manifest + previews
# ----------------------------------------------------------------------------
def export_and_manifest(clips):
    target = OUTPUT / (ASSET + '.glb')
    if target.exists() and not OVERWRITE:
        raise RuntimeError('Refusing to overwrite existing asset: %s' % target)
    for pb in RIG.pose.bones:
        pb.location = (0, 0, 0)
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.scale = (1, 1, 1)
    bpy.context.scene.render.fps = 24
    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 61
    bpy.ops.object.select_all(action='DESELECT')
    RIG.select_set(True)
    for obj in RIG.children:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = RIG
    # same flags as ground_hero_locomotion.hero_export so the export pipeline
    # treats this GLB exactly like the other heroes' (ACTIONS mode ships one
    # glTF animation per action, all sampled)
    bpy.ops.export_scene.gltf(
        filepath=str(target), export_format='GLB', use_selection=True,
        export_animations=True, export_skins=True, export_all_influences=True,
        export_yup=True, export_cameras=False, export_lights=False)

    meshes = [o for o in RIG.children if o.type == 'MESH']
    tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in meshes)
    bpy.context.view_layer.update()
    pts = [o.matrix_world @ Vector(c) for o in meshes for c in o.bound_box]
    lo = [min(p[i] for p in pts) for i in range(3)]
    hi = [max(p[i] for p in pts) for i in range(3)]
    record = {
        'name': ASSET,
        'path': str(target.relative_to(ROOT)).replace('\\', '/'),
        'bytes': target.stat().st_size,
        'triangles': tris,
        'bbox_m': [round(hi[i] - lo[i], 4) for i in range(3)],
        'rest_bounds_blender_xyz_m': [lo, hi],
        'origin': 'feet/bottom-center',
        'units': 'meters',
        'materials': [m.name for m in {slot.material for obj in meshes for slot in obj.material_slots}],
        'animation_clips': clips,
        'design': 'cybernetic samurai authored from scratch, organic pass: lofted tapered torso/legs/mask, sphere joints, lamellar sode stacks, lacquered plates with brass gilt, indigo fabric, glowing visor/obi/crest',
        'zero_particle_vfx': True,
        'preview': str((PREVIEW / (ASSET + '.png')).relative_to(ROOT)).replace('\\', '/'),
    }
    (ROOT / 'assets/models/hq/cyber_shogun_rebuild_manifest.json').write_text(
        json.dumps({'generator': 'tools/assets/build_cyber_shogun_from_scratch.py',
                    'style': 'Neon Pulse / original from-scratch rebuild',
                    'blender_version': bpy.app.version_string,
                    'format': 'glTF 2.0 GLB', 'assets': [record]}, indent=2), encoding='utf-8')
    print('SHOGUN_REBUILT', tris, 'tris', target.stat().st_size, 'bytes', flush=True)


def render_preview():
    scene = bpy.context.scene
    # mute every NLA track: at this frame several 61-frame idles stack and
    # blend, and an evaluated blend pose would be rendered as "the rest pose"
    for track in RIG.animation_data.nla_tracks:
        track.mute = True
    RIG.animation_data.use_nla = False
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 24
    if scene.world is None:
        scene.world = bpy.data.worlds.new('QAWorld')
        scene.world.use_nodes = True
    scene.world.color = (.05, .06, .08)
    bpy.ops.mesh.primitive_plane_add(size=200, location=(0, 0, 0))
    ground = bpy.context.object
    ground.name = 'QAGround_RebuildOnly'
    ground.data.materials.append(MAT['carbon'])
    for loc, power, color, size in [((1.2, -1.0, 2.4), 700, (0.9, 0.95, 1.0), 1.6),
                                    ((-1.6, 1.2, 2.0), 900, (0.1, 0.6, 1.0), 1.8),
                                    ((-1.0, -1.6, 0.8), 400, (1.0, 0.5, 0.3), 2.4)]:
        bpy.ops.object.light_add(type='AREA', location=loc)
        l = bpy.context.object
        l.name = 'QALight_RebuildOnly'
        l.data.energy = power
        l.data.color = color
        l.data.shape = 'DISK'
        l.data.size = size
        l.rotation_euler = (Vector((0, 0, 0.95)) - l.location).to_track_quat('-Z', 'Y').to_euler()
    scene.view_settings.view_transform = 'Standard'  # keep the neon emissive punch
    scene.render.image_settings.file_format = 'PNG'
    scene.render.resolution_x = 560
    scene.render.resolution_y = 760
    for tag, loc, scale in [('front', (0, -3.4, 1.0), 2.4),
                            ('side', (3.4, -0.4, 1.0), 2.4),
                            ('threeq', (2.6, -2.2, 1.35), 3.0)]:
        bpy.ops.object.camera_add(location=loc)
        cam = bpy.context.object
        cam.name = 'QACam_' + tag
        cam.rotation_euler = (Vector((0, 0, 0.95)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
        cam.data.type = 'ORTHO'
        cam.data.ortho_scale = scale
        scene.camera = cam
        scene.render.filepath = str(PREVIEW / ('%s_%s.png' % (ASSET, tag)))
        bpy.ops.render.render(write_still=True)


def main():
    begin()
    build_model()          # geometry only; parts wait for the rig
    build_rig()            # creates RIG, grounds the parts, aligns bone rolls
    attach_to_rig()        # skin the parts onto the rig
    clips = build_clips()
    export_and_manifest(clips)
    render_preview()
    print('REBUILD_COMPLETE ' + ASSET, flush=True)


main()