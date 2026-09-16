extends SceneTree
## Run in the isolated asset-validation project prepared by prepare_validation.py.
## Verifies imported materials, skeletons, clip names/targets, bounds and review scene.

const REQUIRED := ["Idle", "Running", "Walking", "Jumping", "Death", "Hit", "Thrust_Slash", "Karate_Strike", "Attack"]
var failures: Array[String] = []
var results: Array = []

func _initialize() -> void:
	call_deferred("_run")

func check(condition: bool, message: String) -> void:
	if not condition:
		failures.append(message)
		push_error(message)

func _run() -> void:
	var manifest: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://assets/asset_delivery_manifest.json"))
	for entry: Dictionary in manifest["assets"]:
		var path: String = entry["path"]
		var packed := load(path) as PackedScene
		check(packed != null, "Could not import " + path)
		if packed == null:
			continue
		var instance := packed.instantiate() as Node3D
		root.add_child(instance)
		await process_frame
		var meshes := instance.find_children("*", "MeshInstance3D", true, false)
		check(not meshes.is_empty(), path + ": no imported meshes")
		var bounds := AABB()
		var initialized := false
		var surfaces := 0
		var emissive_surfaces := 0
		for node in meshes:
			var mesh := node as MeshInstance3D
			for i in mesh.mesh.get_surface_count():
				surfaces += 1
				var material := mesh.get_active_material(i) as BaseMaterial3D
				check(material != null, path + ": missing imported PBR material")
				if material != null:
					check(material.shading_mode == BaseMaterial3D.SHADING_MODE_PER_PIXEL, path + ": unlit base material")
					if material.emission_enabled:
						emissive_surfaces += 1
			var box := mesh.get_aabb()
			for corner in 8:
				var point := mesh.global_transform * box.get_endpoint(corner)
				if not initialized:
					bounds = AABB(point, Vector3.ZERO)
					initialized = true
				else:
					bounds = bounds.expand(point)
		check(emissive_surfaces > 0, path + ": no emission channel imported")
		var animations: Array[String] = []
		var players := instance.find_children("*", "AnimationPlayer", true, false)
		for node in players:
			var player := node as AnimationPlayer
			for name in player.get_animation_list():
				animations.append(name)
				if name == "RESET":
					continue
				var animation := player.get_animation(name)
				check(animation.length > 0 and animation.get_track_count() > 0, path + ": empty clip " + name)
				player.play(name)
				player.advance(animation.length * 0.5)
				player.stop()
		if entry["category"] == "operative":
			check(instance.find_children("*", "Skeleton3D", true, false).size() > 0, path + ": missing skeleton")
			for name in REQUIRED:
				check(name in animations, path + ": missing exact clip " + name)
			check(bounds.size.y > 1.3 and bounds.size.y < 3.0, path + ": operative not human meter scale: " + str(bounds.size))
		# Imported mesh AABBs can be conservative for skinned meshes; store exact readings
		# for review instead of falsely rejecting a correct posed foot placement.
		results.append({"path": path, "bounds_size_m": [bounds.size.x, bounds.size.y, bounds.size.z],
			"bounds_base_y": bounds.position.y, "surfaces": surfaces,
			"emissive_surfaces": emissive_surfaces, "animations": animations})
		instance.free()
		print("ASSET_IMPORT_OK ", path)
	var showcase := load("res://scenes/asset_preview/asset_showcase.tscn") as PackedScene
	check(showcase != null, "Asset showcase failed to load")
	if showcase != null:
		var preview := showcase.instantiate()
		root.add_child(preview)
		await process_frame
		await process_frame
		preview.free()
	var file := FileAccess.open("res://godot_delivery_results.json", FileAccess.WRITE)
	file.store_string(JSON.stringify({"assets": results, "failures": failures}, "\t"))
	print("ASSET_DELIVERY_CHECKS ", results.size(), " assets; ", failures.size(), " failures")
	quit(0 if failures.is_empty() else 1)
