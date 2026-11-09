extends GdUnitTestSuite
## Covers the temporary CC0 bike and enemy art and combat sounds wired through StandInAssets.

const SOUNDS := [
	"res://assets/stand_in/kenney_scifi_sounds/spaceEngineLow_000.ogg",
	"res://assets/stand_in/kenney_scifi_sounds/laserLarge_000.ogg",
	"res://assets/stand_in/kenney_scifi_sounds/laserRetro_000.ogg",
	"res://assets/stand_in/kenney_scifi_sounds/impactMetal_000.ogg",
	"res://assets/stand_in/kenney_scifi_sounds/explosionCrunch_000.ogg",
]


func test_every_stand_in_file_imports() -> void:
	## Fails when a referenced file is missing or Godot could not import it.
	for path: String in SOUNDS:
		assert_object(load(path)).is_not_null()
	assert_object(load("res://assets/stand_in/kenney_racing/motorcycle_blue.png")).is_not_null()
	assert_object(load("res://assets/stand_in/foozle_mecha_boss/drone_idle.png")).is_not_null()


func test_every_robot_sheet_imports() -> void:
	## All three animations must exist in all eight directions, or a drone could keep a stale sheet.
	for animation: String in ["IdleAim", "StandingShoot", "WalkingShoot"]:
		for direction in range(1, 9):
			assert_object(load("res://assets/stand_in/hormelz_robot/%s_dir%d.png" % [animation, direction])).is_not_null()


func test_robot_direction_matches_the_art() -> void:
	## The sheets were matched by eye: dir8 south, dir2 west, dir4 north, dir6 east, with +y pointing south.
	var hostile: Node2D = auto_free(preload("res://scripts/world/WorldHostile.cs").new())
	add_child(hostile)
	assert_int(hostile.call("RobotDirection", 0.0, 1.0)).is_equal(8)
	assert_int(hostile.call("RobotDirection", -1.0, 0.0)).is_equal(2)
	assert_int(hostile.call("RobotDirection", 0.0, -1.0)).is_equal(4)
	assert_int(hostile.call("RobotDirection", 1.0, 0.0)).is_equal(6)
	assert_object(hostile.get_node_or_null("StandInBody")).is_not_null()


func test_sentry_shows_drone_sprite() -> void:
	## The sprite draws behind the sentry so the beam and health bar stay readable.
	var sentry: CharacterBody2D = auto_free(preload("res://scripts/actors/PalaceSentry.cs").new())
	add_child(sentry)
	var body: Sprite2D = sentry.get_node("StandInBody")
	assert_bool(body.show_behind_parent).is_true()
	assert_int(body.hframes).is_equal(8)


func test_bike_uses_art_and_faces_travel_direction() -> void:
	## With art present the fallback shapes are absent and Facing 0 (south) turns the art 180 degrees.
	var player: CharacterBody2D = auto_free(preload("res://scripts/world/ExplorationPlayer.cs").new())
	add_child(player)
	var bike: Sprite2D = player.get_node("BikeMountSprite")
	assert_object(bike.texture).is_not_null()
	assert_object(bike.get_node_or_null("BikeFrame")).is_null()
	assert_bool(player.call("ToggleBikeAt", 44000.0, 44000.0, 0)).is_true()
	assert_float(bike.rotation).is_equal_approx(PI, 0.001)
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
	assert_int(added.size()).is_equal(1)
	for sound: Node in added:
		sound.free()
	await get_tree().process_frame
