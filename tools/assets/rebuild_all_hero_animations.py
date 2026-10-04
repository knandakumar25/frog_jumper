import bpy
import os

root_dir = r"D:\Projects\frog_jumper"
hq_dir = os.path.join(root_dir, "assets", "models", "hq")

def clean_armature_and_strays(arm, mesh_name="char1"):
    """Completely purges custom bone shapes and stray objects/meshes (e.g. Icospheres)."""
    if arm and hasattr(arm, "pose"):
        for b in arm.pose.bones:
            b.custom_shape = None

    target_mesh = None
    for o in list(bpy.data.objects):
        if o.type == 'MESH':
            if o.name == mesh_name or o.name.startswith(mesh_name) or o.name.startswith("char"):
                target_mesh = o
            else:
                print(f"  [-] Removing stray object: {o.name}")
                bpy.data.objects.remove(o, do_unlink=True)

    for m in list(bpy.data.meshes):
        if target_mesh and m != target_mesh.data:
            print(f"  [-] Removing stray mesh: {m.name}")
            bpy.data.meshes.remove(m, do_unlink=True)

    bpy.ops.object.select_all(action='DESELECT')
    arm.select_set(True)
    if target_mesh:
        target_mesh.select_set(True)
        bpy.context.view_layer.objects.active = arm

    return target_mesh

def get_base_hips_loc(arm):
    idle = bpy.data.actions.get("Standing") or bpy.data.actions.get("Idle") or bpy.data.actions.get("Idle_02")
    base_loc = [0.0, 0.0, 0.0]
    if idle and hasattr(idle, "layers"):
        for layer in idle.layers:
            for strip in layer.strips:
                for cb in strip.channelbags:
                    for fc in cb.fcurves:
                        if "Hips" in fc.data_path and "location" in fc.data_path:
                            if fc.array_index in [0, 1, 2] and len(fc.keyframe_points) > 0:
                                base_loc[fc.array_index] = fc.keyframe_points[0].co[1]
    return base_loc

def retarget_hips_location(action, base_hips, keep_vertical_jump=False):
    """Grounds Hips horizontal movement to base_hips while keeping native rotations untouched."""
    if not hasattr(action, "layers"):
        return
    for layer in action.layers:
        for strip in layer.strips:
            for cb in strip.channelbags:
                for fc in cb.fcurves:
                    if "Hips" in fc.data_path and "location" in fc.data_path:
                        idx = fc.array_index
                        if idx in [0, 1]:  # Horizontal X, Y locked to base
                            for kp in fc.keyframe_points:
                                kp.co[1] = base_hips[idx]
                        elif idx == 2:  # Vertical Z
                            if keep_vertical_jump:
                                if len(fc.keyframe_points) > 0:
                                    src_0 = fc.keyframe_points[0].co[1]
                                    for kp in fc.keyframe_points:
                                        delta = kp.co[1] - src_0
                                        kp.co[1] = base_hips[2] + delta
                            else:
                                for kp in fc.keyframe_points:
                                    kp.co[1] = base_hips[2]

def slice_action(src_action, new_name, start_f, end_f, base_hips=None, keep_vertical_jump=False):
    """Creates a sub-slice of a native action preserving exact bone rotations."""
    new_act = src_action.copy()
    new_act.name = new_name
    if hasattr(new_act, "layers"):
        for layer in new_act.layers:
            for strip in layer.strips:
                for cb in strip.channelbags:
                    for fc in cb.fcurves:
                        kps = [(kp.co[0] - start_f, kp.co[1]) for kp in fc.keyframe_points if start_f <= kp.co[0] <= end_f]
                        while len(fc.keyframe_points) > 0:
                            fc.keyframe_points.remove(fc.keyframe_points[0])
                        for t, v in kps:
                            fc.keyframe_points.insert(frame=t, value=v)
    if base_hips:
        retarget_hips_location(new_act, base_hips, keep_vertical_jump=keep_vertical_jump)
    return new_act

def ensure_nla_track(arm, track_name, action):
    if not arm.animation_data:
        arm.animation_data_create()
    track = arm.animation_data.nla_tracks.get(track_name)
    if not track:
        track = arm.animation_data.nla_tracks.new()
        track.name = track_name
    for s in list(track.strips):
        track.strips.remove(s)
    start_frame = int(action.frame_range[0])
    strip = track.strips.new(track_name, start_frame, action)
    strip.action = action
    return track

def process_oceanus():
    model_path = os.path.join(hq_dir, "oceanus.glb")
    print(f"\n=== Rebuilding Oceanus ({model_path}) ===")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=model_path)
    arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    mesh_obj = clean_armature_and_strays(arm, "char1")
    base_hips = get_base_hips_loc(arm)
    print(f"  Base Hips: {base_hips}")

    cast_act = bpy.data.actions.get("Armature|mage_soell_cast_4|baselayer") or bpy.data.actions.get("mage_soell_cast_4")
    run_act = bpy.data.actions.get("Armature|running|baselayer") or bpy.data.actions.get("Running")
    jump_act = bpy.data.actions.get("Double_Jump") or bpy.data.actions.get("Jumping")

    if cast_act:
        # 1. Primary: Torrent_Blast (full native Polynesian spell cast)
        torrent_act = cast_act.copy()
        torrent_act.name = "Torrent_Blast"
        retarget_hips_location(torrent_act, base_hips, keep_vertical_jump=False)
        ensure_nla_track(arm, "Torrent_Blast", torrent_act)
        print("  [+] Configured native 'Torrent_Blast' (0..54)")

        # 2. Secondary: Vortex_Cast (swirling channeling phase 0..28)
        vortex_act = slice_action(cast_act, "Vortex_Cast", 0, 28, base_hips=base_hips, keep_vertical_jump=False)
        ensure_nla_track(arm, "Vortex_Cast", vortex_act)
        print("  [+] Configured native 'Vortex_Cast' (0..28)")

    if jump_act:
        # 3. Slam: Ground_Slam (native Polynesian leap & strike)
        slam_act = jump_act.copy()
        slam_act.name = "Ground_Slam"
        retarget_hips_location(slam_act, base_hips, keep_vertical_jump=True)
        ensure_nla_track(arm, "Ground_Slam", slam_act)
        print("  [+] Configured native 'Ground_Slam'")

    if run_act:
        # 4. Mobility: Dash (in-place run)
        dash_act = run_act.copy()
        dash_act.name = "Dash"
        retarget_hips_location(dash_act, base_hips, keep_vertical_jump=False)
        ensure_nla_track(arm, "Dash", dash_act)
        print("  [+] Configured native 'Dash'")

    clean_armature_and_strays(arm, "char1")
    bpy.ops.export_scene.gltf(
        filepath=model_path,
        export_format='GLB',
        use_selection=True,
        export_animations=True,
        export_all_influences=True,
        export_skins=True,
        export_cameras=False,
        export_lights=False
    )
    print("Successfully exported oceanus.glb!")

def process_surya():
    model_path = os.path.join(hq_dir, "surya_prime.glb")
    print(f"\n=== Rebuilding Surya Prime ({model_path}) ===")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=model_path)
    arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    mesh_obj = clean_armature_and_strays(arm, "char1")
    base_hips = get_base_hips_loc(arm)
    print(f"  Base Hips: {base_hips}")

    slam_src = bpy.data.actions.get("Armature|Charged_Ground_Slam|baselayer") or bpy.data.actions.get("Ground_Slam")
    run_act = bpy.data.actions.get("Armature|running|baselayer") or bpy.data.actions.get("Running")

    if slam_src:
        # 1. Primary: Shield_Bash (forward bulwark brace & charge 0..28)
        bash_act = slice_action(slam_src, "Shield_Bash", 0, 28, base_hips=base_hips, keep_vertical_jump=False)
        ensure_nla_track(arm, "Shield_Bash", bash_act)
        print("  [+] Configured native 'Shield_Bash' (0..28)")

        # 2. Secondary: Shield_Guard (defensive hardlight brace 0..22)
        guard_act = slice_action(slam_src, "Shield_Guard", 0, 22, base_hips=base_hips, keep_vertical_jump=False)
        ensure_nla_track(arm, "Shield_Guard", guard_act)
        print("  [+] Configured native 'Shield_Guard' (0..22)")

        # 3. Slam: Ground_Slam (full authentic 2-handed charged slam 0..72)
        slam_act = slam_src.copy()
        slam_act.name = "Ground_Slam"
        retarget_hips_location(slam_act, base_hips, keep_vertical_jump=True)
        ensure_nla_track(arm, "Ground_Slam", slam_act)
        print("  [+] Configured native 'Ground_Slam' (0..72)")

    if run_act:
        # 4. Mobility: Dash (in-place run)
        dash_act = run_act.copy()
        dash_act.name = "Dash"
        retarget_hips_location(dash_act, base_hips, keep_vertical_jump=False)
        ensure_nla_track(arm, "Dash", dash_act)
        print("  [+] Configured native 'Dash'")

    clean_armature_and_strays(arm, "char1")
    bpy.ops.export_scene.gltf(
        filepath=model_path,
        export_format='GLB',
        use_selection=True,
        export_animations=True,
        export_all_influences=True,
        export_skins=True,
        export_cameras=False,
        export_lights=False
    )
    print("Successfully exported surya_prime.glb!")

def process_apex():
    model_path = os.path.join(hq_dir, "apex_titan.glb")
    print(f"\n=== Rebuilding Apex Titan ({model_path}) ===")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=model_path)
    arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    mesh_obj = clean_armature_and_strays(arm, "char1")
    base_hips = get_base_hips_loc(arm)
    print(f"  Base Hips: {base_hips}")

    hammer_src = bpy.data.actions.get("Armature|Heavy_Hammer_Swing|baselayer") or bpy.data.actions.get("Tectonic_Slam")
    run_act = bpy.data.actions.get("Armature|running|baselayer") or bpy.data.actions.get("Running")

    if hammer_src:
        # 1. Primary: Tectonic_Slam (massive overhead hammer slam down 0..44)
        tectonic_act = hammer_src.copy()
        tectonic_act.name = "Tectonic_Slam"
        retarget_hips_location(tectonic_act, base_hips, keep_vertical_jump=False)
        ensure_nla_track(arm, "Tectonic_Slam", tectonic_act)
        print("  [+] Configured native 'Tectonic_Slam' (0..44)")

        # 2. Secondary: Magnetic_Pull (raising power arm overhead channeling 0..24)
        pull_act = slice_action(hammer_src, "Magnetic_Pull", 0, 24, base_hips=base_hips, keep_vertical_jump=False)
        ensure_nla_track(arm, "Magnetic_Pull", pull_act)
        print("  [+] Configured native 'Magnetic_Pull' (0..24)")

        # 3. Slam: Ground_Slam (overhead ground pound)
        slam_act = hammer_src.copy()
        slam_act.name = "Ground_Slam"
        retarget_hips_location(slam_act, base_hips, keep_vertical_jump=True)
        ensure_nla_track(arm, "Ground_Slam", slam_act)
        print("  [+] Configured native 'Ground_Slam'")

    if run_act:
        # 4. Mobility: Dash (in-place run)
        dash_act = run_act.copy()
        dash_act.name = "Dash"
        retarget_hips_location(dash_act, base_hips, keep_vertical_jump=False)
        ensure_nla_track(arm, "Dash", dash_act)
        print("  [+] Configured native 'Dash'")

    clean_armature_and_strays(arm, "char1")
    bpy.ops.export_scene.gltf(
        filepath=model_path,
        export_format='GLB',
        use_selection=True,
        export_animations=True,
        export_all_influences=True,
        export_skins=True,
        export_cameras=False,
        export_lights=False
    )
    print("Successfully exported apex_titan.glb!")

def process_phoenicia():
    model_path = os.path.join(hq_dir, "phoenicia.glb")
    print(f"\n=== Rebuilding Aura Phoenicia ({model_path}) ===")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=model_path)
    arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    mesh_obj = clean_armature_and_strays(arm, "char1")
    base_hips = get_base_hips_loc(arm)
    print(f"  Base Hips: {base_hips}")

    beam_act = bpy.data.actions.get("Beam_Cast") or bpy.data.actions.get("Primary_Ability")
    burst_act = bpy.data.actions.get("Supernova_Burst") or bpy.data.actions.get("Secondary_Ability")
    jump_act = bpy.data.actions.get("Double_Jump")
    run_act = bpy.data.actions.get("Armature|running|baselayer") or bpy.data.actions.get("Running")

    if beam_act:
        # 1. Primary: Harmonic_Pulse (native forward acoustic beam cast 0..20)
        pulse_act = beam_act.copy()
        pulse_act.name = "Harmonic_Pulse"
        retarget_hips_location(pulse_act, base_hips, keep_vertical_jump=False)
        ensure_nla_track(arm, "Harmonic_Pulse", pulse_act)
        print("  [+] Configured native 'Harmonic_Pulse' from Beam_Cast (0..20)")

    if burst_act:
        # 2. Secondary: Prism_Reverberation (native radiant resonant burst 0..22)
        prism_act = burst_act.copy()
        prism_act.name = "Prism_Reverberation"
        retarget_hips_location(prism_act, base_hips, keep_vertical_jump=False)
        ensure_nla_track(arm, "Prism_Reverberation", prism_act)
        print("  [+] Configured native 'Prism_Reverberation' from Supernova_Burst (0..22)")

    if jump_act:
        # 3. Slam: Ground_Slam (native double jump & drop)
        slam_act = jump_act.copy()
        slam_act.name = "Ground_Slam"
        retarget_hips_location(slam_act, base_hips, keep_vertical_jump=True)
        ensure_nla_track(arm, "Ground_Slam", slam_act)
        print("  [+] Configured native 'Ground_Slam' from Double_Jump")

    if run_act:
        # 4. Mobility: Dash (in-place run)
        dash_act = run_act.copy()
        dash_act.name = "Dash"
        retarget_hips_location(dash_act, base_hips, keep_vertical_jump=False)
        ensure_nla_track(arm, "Dash", dash_act)
        print("  [+] Configured native 'Dash'")

    clean_armature_and_strays(arm, "char1")
    bpy.ops.export_scene.gltf(
        filepath=model_path,
        export_format='GLB',
        use_selection=True,
        export_animations=True,
        export_all_influences=True,
        export_skins=True,
        export_cameras=False,
        export_lights=False
    )
    print("Successfully exported phoenicia.glb!")

def process_shogun():
    model_path = os.path.join(hq_dir, "cyber_shogun.glb")
    print(f"\n=== Cleaning Cyber Shogun ({model_path}) ===")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=model_path)
    arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    mesh_obj = clean_armature_and_strays(arm, "char1")
    base_hips = get_base_hips_loc(arm)
    print(f"  Base Hips: {base_hips}")

    clean_armature_and_strays(arm, "char1")
    bpy.ops.export_scene.gltf(
        filepath=model_path,
        export_format='GLB',
        use_selection=True,
        export_animations=True,
        export_all_influences=True,
        export_skins=True,
        export_cameras=False,
        export_lights=False
    )
    print("Successfully exported cyber_shogun.glb!")

def process_mirage():
    model_path = os.path.join(hq_dir, "mirage_blade.glb")
    print(f"\n=== Cleaning Mirage Blade ({model_path}) ===")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=model_path)
    arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    mesh_obj = clean_armature_and_strays(arm, "char1")
    base_hips = get_base_hips_loc(arm)
    print(f"  Base Hips: {base_hips}")

    clean_armature_and_strays(arm, "char1")
    bpy.ops.export_scene.gltf(
        filepath=model_path,
        export_format='GLB',
        use_selection=True,
        export_animations=True,
        export_all_influences=True,
        export_skins=True,
        export_cameras=False,
        export_lights=False
    )
    print("Successfully exported mirage_blade.glb!")

process_oceanus()
process_surya()
process_apex()
process_phoenicia()
process_shogun()
process_mirage()

print("\nALL HEROES SUCCESSFULLY UPGRADED WITH NATIVE, FLAWLESS ANIMATIONS!")
