extends GdUnitTestSuite
## Covers the code paths behind StandInAssets.
##
## The downloaded CC0 packs in assets/stand_in (Kenney, Foozle, Hormelz) were deleted on 2026-11-11
## at the owner's request, together with the Blender art. Each lookup must then return null so the
## game falls back to its code-drawn shapes and stays silent. The tests check those fallbacks
## while the files are absent, and the art when present.

const SOUNDS := [
	"res://assets/stand_in/kenney_scifi_sounds/spaceEngineLow_000.ogg",
	"res://assets/stand_in/kenney_scifi_sounds/laserLarge_000.ogg",
	"res://assets/stand_in/kenney_scifi_sounds/laserRetro_000.ogg",
	"res://assets/stand_in/kenney_scifi_sounds/impactMetal_000.ogg",
	"res://assets/stand_in/kenney_scifi_sounds/explosionCrunch_000.ogg",
]
const BIKE := "res://assets/stand_in/kenney_racing/motorcycle_blue.png"
const DRONE := "res://assets/stand_in/foozle_mecha_boss/drone_idle.png"


func test_present_files_import_and_absent_ones_are_not_referenced() -> void:
	## Any stand-in file that exists must import; a missing one must not be loadable.
	for path: String in SOUNDS + [BIKE, DRONE]:
		if FileAccess.file_exists(path):
			assert_object(load(path)).is_not_null()
		else:
			assert_bool(ResourceLoader.exists(path)).is_false()


func test_robot_direction_matches_the_art() -> void:
	## The sheets were matched by eye: dir8 south, dir2 west, dir4 north, dir6 east, with +y pointing south.
	var hostile: Node2D = auto_free(preload("res://scripts/world/WorldHostile.cs").new())
	add_child(hostile)
	assert_int(hostile.call("RobotDirection", 0.0, 1.0)).is_equal(8)
	assert_int(hostile.call("RobotDirection", -1.0, 0.0)).is_equal(2)
	assert_int(hostile.call("RobotDirection", 0.0, -1.0)).is_equal(4)
	assert_int(hostile.call("RobotDirection", 1.0, 0.0)).is_equal(6)
	# The robot body exists only while the Hormelz sheets do.
	var has_art := ResourceLoader.exists("res://assets/stand_in/hormelz_robot/IdleAim_dir8.png")
	assert_bool(hostile.get_node_or_null("StandInBody") != null).is_equal(has_art)


func test_sentry_shows_drone_sprite() -> void:
	## The sprite draws behind the sentry so the beam and health bar stay readable.
	var sentry: CharacterBody2D = auto_free(preload("res://scripts/actors/PalaceSentry.cs").new())
	add_child(sentry)
	var body: Sprite2D = sentry.get_node_or_null("StandInBody")
	if not ResourceLoader.exists(DRONE):
		assert_object(body).is_null()  # Code-drawn sentry only.
		return
	assert_bool(body.show_behind_parent).is_true()
	assert_int(body.hframes).is_equal(8)


func test_bike_uses_art_and_faces_travel_direction() -> void:
	## With art present the fallback shapes are absent and Facing 0 (south) turns the art 180 degrees.
	var player: CharacterBody2D = auto_free(preload("res://scripts/world/ExplorationPlayer.cs").new())
	add_child(player)
	var bike: Sprite2D = player.get_node("BikeMountSprite")
	if ResourceLoader.exists(BIKE):
		assert_object(bike.texture).is_not_null()
		assert_object(bike.get_node_or_null("BikeFrame")).is_null()
	else:
		# Without the Kenney art the bike is the code-drawn frame.
		assert_object(bike.texture).is_null()
		assert_object(bike.get_node_or_null("BikeFrame")).is_not_null()
	assert_bool(player.call("ToggleBikeAt", 44000.0, 44000.0, 0)).is_true()
	# Only the top-down art turns with Facing; the code-drawn side-view frame never rotates.
	assert_float(bike.rotation).is_equal_approx(PI if ResourceLoader.exists(BIKE) else 0.0, 0.001)
	assert_bool(bike.visible).is_true()
	player.call("ToggleBikeAt", 44000.0, 44000.0, 0)
	assert_bool(bike.visible).is_false()


func test_destroyed_sentry_leaves_explosion_sound_behind() -> void:
	## The explosion goes on the scene root: never on the sentry, which frees itself on death, nor on the
	## parent, whose children encounter code treats as enemies.
	var arena: Node2D = auto_free(Node2D.new())
	add_child(arena)
	var sentry: CharacterBody2D = preload("res://scripts/actors/PalaceSentry.cs").new()
	arena.add_child(sentry)
	var is_sound := func(n: Node) -> bool: return n is AudioStreamPlayer2D
	var before := get_tree().root.get_children().filter(is_sound)
	sentry.call("TakeDamage", 9999.0)
	assert_int(arena.get_children().filter(is_sound).size()).is_equal(0)
	var added := get_tree().root.get_children().filter(is_sound).filter(func(n: Node) -> bool: return not before.has(n))
	# Silent (no player at all) once the Kenney sound is deleted.
	assert_int(added.size()).is_equal(1 if ResourceLoader.exists(SOUNDS[4]) else 0)
	for sound: Node in added:
		sound.free()
	await get_tree().process_frame
