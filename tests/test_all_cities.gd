class_name TestAllCities
extends GdUnitTestSuite
## Flies to every city in unsaved preview mode and builds every side-scrolling operation.

const CITIES := ["kyoto", "mumbai", "honolulu", "dubai", "luanda", "beirut"]
const SCENES := [
	"res://scenes/gameplay/mumbai_viaduct_grid.tscn",
	"res://scenes/gameplay/honolulu_pumping_station.tscn",
	"res://scenes/gameplay/dubai_salt_plant.tscn",
	"res://scenes/gameplay/luanda_power_station.tscn",
	"res://scenes/gameplay/beirut_broadcast_tower.tscn",
]


func _wait_ready(world: Node) -> void:
	## Waits until all nine chunks around the player exist and movement is enabled.
	for frame in 240:
		await get_tree().process_frame
		if int(world.get("ResidentChunks")) == 9 and world.get_node("ExplorationPlayer").get("MovementEnabled"):
			return


func test_every_city_loads_and_streams() -> void:
	var sessions: Node = get_node("/root/SessionManager")
	var global: Node = get_node("/root/Global")
	var previous_session: int = sessions.get("CurrentSessionId")
	var previous_progress: int = global.get("unlocked_levels")
	sessions.set("CurrentSessionId", -1)
	global.set("unlocked_levels", 5)
	var world: Node = load("res://scenes/world/neo_kyoto.tscn").instantiate()
	add_child(world)
	await _wait_ready(world)
	for city in CITIES:
		assert_bool(world.call("PreviewTravel", city)).is_true()
		await _wait_ready(world)
		var state: Dictionary = world.call("InspectionState")
		assert_str(state["city"]).is_equal(city)
		assert_int(int(state["resident"])).is_equal(9)
		assert_bool(world.get_node("ExplorationPlayer").get("MovementEnabled")).is_true()
	world.free()
	await get_tree().process_frame
	sessions.set("CurrentSessionId", previous_session)
	global.set("unlocked_levels", previous_progress)
	load("res://scripts/managers/LevelCatalogBridge.cs").new().call("PrepareMission", previous_progress)


func test_destroying_each_beacon_opens_the_way_to_the_results_panel() -> void:
	var sessions: Node = get_node("/root/SessionManager")
	var previous_session: int = sessions.get("CurrentSessionId")
	sessions.set("CurrentSessionId", -1)
	for path in SCENES:
		var mission: Node = load(path).instantiate()
		add_child(mission)
		await get_tree().process_frame
		var beacon: Node2D = mission.find_child("OperationBeacon", true, false)
		beacon.call("TakeDamage", 9999.0)
		assert_bool(mission.get("BeaconDestroyed")).is_true()
		var exit_gate: Node2D = mission.find_child("ExtractionGate", true, false)
		var player: Node2D = null
		for body in mission.find_children("*", "CharacterBody2D", true, false):
			if body.get("ActiveProfile") != null:
				player = body
		player.global_position = exit_gate.global_position + Vector2(-30, -60)
		for frame in 12:
			await get_tree().physics_frame
		assert_object(mission.find_child("ResultsPanel", true, false)).is_not_null()
		mission.free()
		await get_tree().process_frame
	sessions.set("CurrentSessionId", previous_session)


## City art awaits SpriteCook regeneration (docs/ART_REGENERATION.md). A city must have either
## its full set of eight modules and eight landmarks or none at all, never a half-regenerated mix.
func test_every_city_has_all_or_none_of_its_art() -> void:
	for city in CITIES:
		var present := 0
		for index in 8:
			if ResourceLoader.exists("res://assets/world/%s/module_%d.png" % [city, index]):
				present += 1
			if ResourceLoader.exists("res://assets/world/%s/landmark_%d.png" % [city, index]):
				present += 1
		assert_bool(present == 0 or present == 16).override_failure_message(
			"%s has %d of 16 art files" % [city, present]).is_true()


func test_every_operation_builds_with_beacon_gate_and_exit() -> void:
	var global: Node = get_node("/root/Global")
	var before: int = global.get("unlocked_levels")
	for path in SCENES:
		var mission: Node = load(path).instantiate()
		add_child(mission)
		for frame in 3:
			await get_tree().process_frame
		assert_object(mission.find_child("OperationBeacon", true, false)).is_not_null()
		assert_object(mission.find_child("ExtractionGate", true, false)).is_not_null()
		assert_object(mission.find_child("OperationGate", true, false)).is_not_null()
		assert_bool(mission.get("BeaconDestroyed")).is_false()
		assert_int(global.get("unlocked_levels")).is_equal(before)
		mission.free()
		await get_tree().process_frame
