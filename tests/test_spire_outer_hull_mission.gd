class_name TestSpireOuterHullMission
extends GdUnitTestSuite
## Mission 9 runtime contracts: high-altitude ascent (2,500m), all 6 operatives, coolant vents, laser grids, Spire Sentinel boss phases, and core airlock extraction.

const SCENE := "res://scenes/gameplay/level9.tscn"


func _mission() -> Node:
	## Each test instantiates a clean mission outside saved session state.
	var catalog = load("res://scripts/managers/LevelCatalogBridge.cs").new()
	catalog.PrepareMission(9)
	catalog = null
	var mission: Node = load(SCENE).instantiate()
	add_child(mission)
	return mission


func test_mission_initializes_with_all_six_operatives_and_mission_header() -> void:
	## Mission 9 deploys the full 6-operative squad together; title card contains SPIRE OUTER HULL.
	var mission := _mission()
	var bridge = load("res://scripts/managers/LevelCatalogBridge.cs").new()
	assert_int(bridge.UnlockedCount()).is_equal(6)
	for op_name in ["Cyber Shogun", "Surya Prime", "Oceanus", "Apex Titan", "Aura Phoenicia", "Mirage Blade"]:
		assert_bool(bridge.IsOperativeUnlocked(op_name)).is_true()
	bridge = null

	assert_int(mission.get("FragmentTotal")).is_equal(3)
	var title_card: Node = mission.get_node("MissionHUD/HUDRoot/TitleCard")
	assert_str(title_card.get_node("MissionHeading").text).contains("SPIRE OUTER HULL")
	mission.free()


func test_hazards_and_objectives_registered_correctly() -> void:
	## Cryogenic coolant vents, flak perimeter laser grid, airlock terminal, and airlock trigger are present.
	var mission := _mission()
	var vent1: Node = mission.get_node("Actors/CryoVent1")
	assert_that(vent1).is_not_null()
	assert_bool(vent1.get("IsNeutralized")).is_false()
	vent1.call("Neutralize")
	assert_bool(vent1.get("IsNeutralized")).is_true()

	var laser: Node = mission.get_node("Actors/FlakPerimeterLaser")
	assert_that(laser).is_not_null()

	var terminal: Node = mission.get_node("Actors/AirlockTerminal")
	assert_that(terminal).is_not_null()

	var exit_beacon: Node = mission.get_node("Actors/AirlockBreachTrigger")
	assert_that(exit_beacon).is_not_null()
	mission.free()


func test_boss_and_extraction_gating() -> void:
	## Milestone sequence enforces sector gates; extraction is blocked until Spire Sentinel phase 3 defeat.
	var mission := _mission()
	var exit_beacon: Node = mission.get_node("Actors/AirlockBreachTrigger")
	assert_that(exit_beacon).is_not_null()

	# Initial extraction is blocked
	assert_bool(mission.get("CanExtract")).is_false()

	# Sector 1
	for id in ["gantry1", "gantry2"]:
		mission.call("CompleteCombatEvent", "encounter:" + id)
	mission.call("CommitObjective", "gantry_ingress")

	# Sector 2
	mission.call("CompleteCombatEvent", "encounter:exhaust1")
	mission.call("CommitObjective", "coolant_shaft_douse")
	mission.call("CompleteCombatEvent", "encounter:exhaust2")
	mission.call("CommitObjective", "clamp_brace_shatter")
	mission.call("CompleteCombatEvent", "encounter:exhaust3")
	mission.call("CommitObjective", "exhaust_conduits_cleared")

	# Sector 3
	mission.call("CompleteCombatEvent", "encounter:flak1")
	mission.call("CommitObjective", "laser_grid_phase")
	mission.call("CompleteCombatEvent", "encounter:flak2")
	mission.call("CommitObjective", "flak_barrier_deflect")
	mission.call("CompleteCombatEvent", "encounter:flak3")
	mission.call("CommitObjective", "battery_deck_cleared")

	# Sector 4
	for id in ["shaft1", "shaft2"]:
		mission.call("CompleteCombatEvent", "encounter:" + id)
	mission.call("CommitObjective", "relay_resonance_disable")
	mission.call("CompleteCombatEvent", "encounter:shaft3")
	mission.call("CommitObjective", "elevator_ascent_breach")

	# Sector 5: Boss phases
	mission.call("CompleteCombatEvent", "encounter:summit1")
	mission.call("CommitObjective", "spire_sentinel_phase:1")
	mission.call("CompleteCombatEvent", "encounter:summit2")
	mission.call("CommitObjective", "spire_sentinel_phase:2")
	mission.call("CompleteCombatEvent", "encounter:summit3")
	mission.call("CommitObjective", "spire_sentinel_phase:3")

	assert_bool(mission.get("CanExtract")).is_true()
	mission.free()
