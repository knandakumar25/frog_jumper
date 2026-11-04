class_name TestStratosDocksMission
extends GdUnitTestSuite
## Mission 8 runtime contracts: high-altitude deployment, all 6 operatives, cryo vents, radar laser grids, boss phases, and VTOL extraction.

const SCENE := "res://scenes/gameplay/level8.tscn"


func _mission() -> Node:
	## Each test instantiates a clean mission outside saved session state.
	var catalog = load("res://scripts/managers/LevelCatalogBridge.cs").new()
	catalog.PrepareMission(8)
	catalog = null
	var mission: Node = load(SCENE).instantiate()
	add_child(mission)
	return mission


func test_mission_initializes_with_all_six_operatives_and_mission_header() -> void:
	## Mission 8 deploys the full 6-operative squad together; title card contains STRATOS DOCKS.
	var mission := _mission()
	var bridge = load("res://scripts/managers/LevelCatalogBridge.cs").new()
	assert_int(bridge.UnlockedCount()).is_equal(6)
	for op_name in ["Cyber Shogun", "Surya Prime", "Oceanus", "Apex Titan", "Aura Phoenicia", "Mirage Blade"]:
		assert_bool(bridge.IsOperativeUnlocked(op_name)).is_true()
	bridge = null

	assert_int(mission.get("FragmentTotal")).is_equal(3)
	var title_card: Node = mission.get_node("MissionHUD/HUDRoot/TitleCard")
	assert_str(title_card.get_node("MissionHeading").text).contains("STRATOS DOCKS")
	mission.free()


func test_hazards_and_objectives_registered_correctly() -> void:
	## Cryo vents, flight deck laser grid, VTOL terminal, and boarding ramp exit are present in the actor container.
	var mission := _mission()
	var vent1: Node = mission.get_node("Actors/CryoVent1")
	assert_that(vent1).is_not_null()
	assert_bool(vent1.get("IsNeutralized")).is_false()
	vent1.call("Neutralize")
	assert_bool(vent1.get("IsNeutralized")).is_true()

	var laser: Node = mission.get_node("Actors/FlightDeckLaserGrid")
	assert_that(laser).is_not_null()

	var terminal: Node = mission.get_node("Actors/VtolTerminal")
	assert_that(terminal).is_not_null()

	var exit_beacon: Node = mission.get_node("Actors/VtolBoardingRampTrigger")
	assert_that(exit_beacon).is_not_null()
	mission.free()


func test_boss_and_extraction_gating() -> void:
	## Milestone sequence enforces sector gates; extraction is blocked until Aero-Centurion phase 3 defeat.
	var mission := _mission()
	var exit_beacon: Node = mission.get_node("Actors/VtolBoardingRampTrigger")
	assert_that(exit_beacon).is_not_null()

	# Initial extraction is blocked
	assert_bool(mission.get("CanExtract")).is_false()

	# Sector 1
	for id in ["catwalk1", "catwalk2"]:
		mission.call("CompleteCombatEvent", "encounter:" + id)
	mission.call("CommitObjective", "perimeter_ingress")

	# Sector 2
	mission.call("CompleteCombatEvent", "encounter:fuel1")
	mission.call("CommitObjective", "cryo_fuel_douse")
	mission.call("CompleteCombatEvent", "encounter:fuel2")
	mission.call("CommitObjective", "titan_clamp_shatter")
	mission.call("CompleteCombatEvent", "encounter:fuel3")
	mission.call("CommitObjective", "fuel_conduits_secured")

	# Sector 3
	mission.call("CompleteCombatEvent", "encounter:scanner1")
	mission.call("CommitObjective", "scanner_grid_phase")
	mission.call("CompleteCombatEvent", "encounter:scanner2")
	mission.call("CommitObjective", "radar_resonance_disable")
	mission.call("CompleteCombatEvent", "encounter:scanner3")
	mission.call("CommitObjective", "scanner_array_cleared")

	# Sector 4
	for id in ["gantry1", "gantry2"]:
		mission.call("CompleteCombatEvent", "encounter:" + id)
	mission.call("CommitObjective", "flak_bulwark_breach")
	mission.call("CompleteCombatEvent", "encounter:gantry3")
	mission.call("CommitObjective", "hangar_overlook_breach")

	# Sector 5: Boss phases
	mission.call("CompleteCombatEvent", "encounter:hangar1")
	mission.call("CommitObjective", "aero_guardian_phase:1")
	mission.call("CompleteCombatEvent", "encounter:hangar2")
	mission.call("CommitObjective", "aero_guardian_phase:2")
	mission.call("CompleteCombatEvent", "encounter:hangar3")
	mission.call("CommitObjective", "aero_guardian_phase:3")

	assert_bool(mission.get("CanExtract")).is_true()
	mission.free()
