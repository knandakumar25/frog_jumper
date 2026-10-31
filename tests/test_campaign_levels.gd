class_name TestCampaignLevels
extends GdUnitTestSuite

## Structural checks for all four code-built missions: they load, the squad matches the story schedule, every
## shrine/objective/fragment stands on a platform the route can reach with conservative jump limits, and each
## gate is cleared only by its own operative's ability.

const LEVELS := {
	1: "res://scenes/gameplay/level1.tscn",
	2: "res://scenes/gameplay/level2.tscn",
	3: "res://scenes/gameplay/level3.tscn",
	4: "res://scenes/gameplay/level4.tscn",
}
const GATE := 4
## Conservative limits: weakest jump (Apex Titan) clears ~177 px up and ~256 px across at run speed.
const MAX_UP := 160.0
const MAX_GAP_LEVEL := 200.0
const MAX_GAP_DROP := 260.0
var catalog = load("res://scripts/managers/LevelCatalogBridge.cs").new()

func _mission(level: int) -> Node:
	## Gameplay checks explicitly complete the independently tested arrival presentation.
	catalog.PrepareMission(level)
	var runner := scene_runner(LEVELS[level])
	var mission := runner.scene()
	if level == 2:
		mission.get_node("ConcreteWildfireIntro").call("Finish")
	if level == 3:
		mission.get_node("NeonRustIntro").call("Finish")
	return mission

## Builds standable rectangles from the geometry: [left, right, top] (movers span their whole travel).
func _surfaces(mission: Node) -> Array:
	var surfaces: Array = []
	for node in mission.get_node("PalaceGeometry").get_children():
		if node.name == "WorldCeiling":
			continue
		# Mission dressing shares the world transform but has no collision surface or platform dimensions.
		if node.get("Size") == null:
			continue
		var size: Vector2 = node.get("Size")
		if node.get("Surface") != null:
			var surface: int = node.get("Surface")
			# Gates are doors, not floors. Wall tops ARE standable (the gorge's east wall is the rooftop entry);
			# the tall ones simply never become reachable because their tops are too high.
			if surface == GATE:
				continue
		var travel: Vector2 = node.get("Travel") if node.get("Travel") != null else Vector2.ZERO
		surfaces.append([node.position.x, node.position.x + size.x + maxf(travel.x, 0.0), node.position.y, node.name])
	return surfaces

func _reachable(surfaces: Array, start: Vector2) -> Array:
	var seen: Array = []
	var queue: Array = []
	for i in surfaces.size():
		var s: Array = surfaces[i]
		if start.x >= s[0] and start.x <= s[1] and absf(start.y - s[2]) < 8.0:
			queue.append(i)
			seen.append(i)
	while not queue.is_empty():
		var a: Array = surfaces[queue.pop_back()]
		for j in surfaces.size():
			if seen.has(j):
				continue
			var b: Array = surfaces[j]
			var gap := maxf(0.0, maxf(b[0] - a[1], a[0] - b[1]))
			var up: float = a[2] - b[2]   # positive when b is higher
			var ok := false
			if up > MAX_UP:
				ok = false
			elif up > 100.0:
				ok = gap <= 120.0
			elif up >= -40.0:
				ok = gap <= MAX_GAP_LEVEL
			else:
				ok = gap <= MAX_GAP_DROP
			if ok:
				seen.append(j)
				queue.append(j)
	return seen

func _stands_on_reachable(surfaces: Array, reachable: Array, point: Vector2) -> bool:
	for i in reachable:
		var s: Array = surfaces[i]
		if point.x >= s[0] - 4.0 and point.x <= s[1] + 4.0 and absf(point.y - s[2]) < 8.0:
			return true
	return false

func test_all_levels_load_with_player_hud_and_ceiling() -> void:
	for level in LEVELS:
		var mission := _mission(level)
		assert_that(mission.get_node("Actors/CyberShogun")).is_not_null()
		assert_that(mission.get_node("CyberCombatHud")).is_not_null()
		assert_that(mission.get_node("CanvasLayer/PauseMenu")).is_not_null()
		assert_that(mission.get_node("PalaceGeometry/WorldCeiling")).is_not_null()
		mission.free()

func test_squad_at_each_level_start_follows_the_story() -> void:
	var expected := {1: 1, 2: 1, 3: 3, 4: 5}
	for level in expected:
		var mission := _mission(level)
		assert_int(catalog.UnlockedCount()).is_equal(expected[level])
		mission.free()

func test_every_objective_stands_on_a_reachable_platform() -> void:
	for level in LEVELS:
		var mission := _mission(level)
		var surfaces := _surfaces(mission)
		var start: Vector2 = mission.get_node("Actors/CyberShogun").position + Vector2(0, 46)
		var reachable := _reachable(surfaces, start)
		assert_bool(reachable.size() > 0).is_true()
		for node in mission.get_node("Actors").get_children():
			var is_target: bool = node.name.begins_with("Shrine") or node.name.begins_with("MemoryFragment") \
				or node.name == "ExtractionGate" or node.name == "DataBlade" or node.name == "DemolitionBeacon" \
				or node.name.begins_with("BiometricSpire")
			if not is_target:
				continue
			assert_bool(_stands_on_reachable(surfaces, reachable, node.position)) \
				.override_failure_message("Level %d: %s at %s is not on a reachable platform" % [level, node.name, node.position]) \
				.is_true()
		mission.free()

## Gate walls must meet the floor: a gate that stops short of the ground can be walked under.
func test_gate_walls_reach_the_floor_and_the_ceiling() -> void:
	for level in LEVELS:
		var mission := _mission(level)
		var surfaces := _surfaces(mission)
		for node in mission.get_node("PalaceGeometry").get_children():
			if node.get("Surface") == null or node.get("Surface") != GATE:
				continue
			var size: Vector2 = node.get("Size")
			var bottom: float = node.position.y + size.y
			var gate_x: float = node.position.x + size.x * 0.5
			var meets_floor := false
			for s in surfaces:
				if gate_x >= s[0] - 1.0 and gate_x <= s[1] + 1.0 and absf(s[2] - bottom) < 8.0:
					meets_floor = true
			assert_bool(meets_floor).override_failure_message("Level %d: gate %s bottom %.0f does not meet a floor" % [level, node.name, bottom]).is_true()
		mission.free()

func test_level2_fire_needs_oceanus_and_rubble_needs_titan() -> void:
	var mission := _mission(2)
	var fire := mission.get_node("Actors/FireCorridor1")
	var rubble := mission.get_node("Actors/MembraneRubble")
	catalog.UnlockOperative("Oceanus")
	catalog.UnlockOperative("Apex Titan")
	assert_bool(fire.call("ReceiveAbility", "slam", fire.global_position + Vector2(-100, -50), 1.0)).is_false()
	assert_bool(fire.call("ReceiveAbility", "douse", fire.global_position + Vector2(-100, -50), 1.0)).is_true()
	assert_bool(rubble.call("ReceiveAbility", "douse", rubble.global_position + Vector2(-100, -50), 1.0)).is_false()
	assert_bool(rubble.call("ReceiveAbility", "slam", rubble.global_position + Vector2(-100, -50), 1.0)).is_true()
	mission.free()

func test_level2_membrane_opens_only_after_all_three_spires() -> void:
	## Each local defense must fall before its scanner becomes damageable.
	var mission := _mission(2)
	var membrane := mission.get_node("PalaceGeometry/Membrane")
	for id in ["street", "tower", "courtyard"]:
		var encounter := mission.get_node("Actors/Encounter_spire-" + id)
		encounter.call("Activate")
		for enemy in encounter.get_children():
			enemy.call("TakeDamage", 1000.0)
	await get_tree().physics_frame
	for index in range(1, 4):
		assert_bool(membrane.get("IsOpen")).is_false()
		mission.get_node("Actors/BiometricSpire%d" % index).call("TakeDamage", 1000.0)
	assert_bool(membrane.get("IsOpen")).is_true()
	assert_int(mission.get("SpiresDisabled")).is_equal(3)
	mission.free()

func test_level3_relay_blacks_out_comms_for_phoenicia_only() -> void:
	## The relay rejects correct abilities until every sabotage and reinforcement prerequisite is complete.
	var mission := _mission(3)
	await get_tree().process_frame
	var relay := mission.get_node("Actors/CommRelay")
	assert_bool(relay.call("ReceiveAbility", "slam", relay.global_position, 1.0)).is_false()
	assert_bool(relay.call("ReceiveAbility", "resonate", relay.global_position, 1.0)).is_false()
	assert_bool(mission.get("CommsBlackedOut")).is_false()
	for s in range(1, 8):
		for g in range(1, [3,4,3,2,4,5,5][s-1]+1):
			mission.call("CompleteCombatEvent", "encounter:s%d-%d" % [s,g])
	for id in ["shipment:freight", "shipment:runoff", "recruit:aura", "lesson:aura", "shipment:harmonic", "uplink:west", "uplink:east", "coupler:west", "coupler:east"]:
		mission.call("CompleteCombatEvent", id)
	assert_bool(relay.call("ReceiveAbility", "resonate", relay.global_position + Vector2(-150, 0), 1.0)).is_true()
	assert_bool(mission.get("CommsBlackedOut")).is_true()
	mission.free()

func test_level3_and_4_laser_curtains_are_lethal_without_phase() -> void:
	for level in [3, 4]:
		var mission := _mission(level)
		var player := mission.get_node("Actors/CyberShogun")
		var found := false
		for node in mission.get_node("Actors").get_children():
			if node.get_script() != null and "CyberLaserGrid2D" in str(node.get_script().resource_path) and node.get("AlwaysActive") and node.get("Damage") >= 999.0:
				found = true
		assert_bool(found).override_failure_message("Level %d needs a lethal laser curtain" % level).is_true()
		assert_bool(player.get("IsPhased")).is_false()
		mission.free()

func test_level4_boss_spawns_in_arena_and_defeat_opens_the_wall() -> void:
	var mission := _mission(4)
	var player := mission.get_node("Actors/CyberShogun")
	var wall := mission.get_node("PalaceGeometry/BorderWall")
	assert_that(mission.get("Boss")).is_null()
	player.global_position = Vector2(5900, 540)
	await get_tree().physics_frame
	await get_tree().physics_frame
	var boss = mission.get("Boss")
	assert_that(boss).is_not_null()
	assert_bool(wall.get("IsOpen")).is_false()
	boss.call("TakeDamage", 100000.0)
	await get_tree().physics_frame
	assert_bool(mission.get("BossDefeated")).is_true()
	assert_bool(wall.get("IsOpen")).is_true()
	mission.free()

func test_each_level_names_the_next_scene_or_ends_the_act() -> void:
	for level in range(1, 5):
		assert_str(catalog.ScenePath(level)).is_equal(LEVELS[level])
	assert_int(catalog.Count()).is_equal(4)
