class_name TestFoundryForgeMission
extends GdUnitTestSuite
## Mission 7 runtime contracts: deployment, full squad synergy, coolant vents, laser grids, boss phases, and extraction.

const SCENE := "res://scenes/gameplay/level7.tscn"


func _mission() -> Node:
	## Each test instantiates a clean mission outside saved session state.
	var catalog = load("res://scripts/managers/LevelCatalogBridge.cs").new()
	catalog.PrepareMission(7)
	catalog = null
	var mission: Node = load(SCENE).instantiate()
	add_child(mission)
	return mission


func test_mission_initializes_with_all_six_operatives_and_mission_header() -> void:
	## Mission 7 deploys the full 6-operative squad together; title card contains THE FOUNDRY FORGE.
	var mission := _mission()
	var bridge = load("res://scripts/managers/LevelCatalogBridge.cs").new()
	assert_int(bridge.UnlockedCount()).is_equal(6)
	for op_name in ["Cyber Shogun", "Surya Prime", "Oceanus", "Apex Titan", "Aura Phoenicia", "Mirage Blade"]:
		assert_bool(bridge.IsOperativeUnlocked(op_name)).is_true()
	bridge = null

	assert_int(mission.get("FragmentTotal")).is_equal(3)
	var title_card: Node = mission.get_node("MissionHUD/HUDRoot/TitleCard")
	assert_str(title_card.get_node("MissionHeading").text).contains("THE FOUNDRY FORGE")
	mission.free()


func test_hazards_and_objectives_registered_correctly() -> void:
	## Coolant vents, laser grid, core terminal, and exit gantry are present in the actor container.
	var mission := _mission()
	var vent1: Node = mission.get_node("Actors/Vent1")
	assert_that(vent1).is_not_null()
	assert_bool(vent1.get("IsNeutralized")).is_false()
	vent1.call("Neutralize")
	assert_bool(vent1.get("IsNeutralized")).is_true()

	var laser: Node = mission.get_node("Actors/InductionLaserGrid")
	assert_that(laser).is_not_null()

	var terminal: Node = mission.get_node("Actors/CoreTerminal")
	assert_that(terminal).is_not_null()

	var exit_beacon: Node = mission.get_node("Actors/ExtractionGantry")
	assert_that(exit_beacon).is_not_null()
	mission.free()


func test_boss_and_extraction_gating() -> void:
	## Milestone sequence enforces sector gates; extraction is blocked until boss phase 3 core collapse.
	var mission := _mission()
	var exit_beacon: Node = mission.get_node("Actors/ExtractionGantry")
	assert_that(exit_beacon).is_not_null()

	# Initial extraction is blocked
	assert_bool(mission.get("CanExtract")).is_false()

	# Sector 1
	for id in ["ingress1", "ingress2"]:
		mission.call("CompleteCombatEvent", "encounter:" + id)
	mission.call("CommitObjective", "ingress_breach")

	# Sector 2
	mission.call("CompleteCombatEvent", "encounter:coolant1")
	mission.call("CommitObjective", "coolant_douse")
	mission.call("CompleteCombatEvent", "encounter:coolant2")
	mission.call("CommitObjective", "valve_shatter")
	mission.call("CompleteCombatEvent", "encounter:coolant3")
	mission.call("CommitObjective", "turbines_overloaded")

	# Sector 3
	mission.call("CompleteCombatEvent", "encounter:induction1")
	mission.call("CommitObjective", "arc_phase")
	mission.call("CompleteCombatEvent", "encounter:induction2")
	mission.call("CommitObjective", "resonance_disable")
	mission.call("CompleteCombatEvent", "encounter:induction3")
	mission.call("CommitObjective", "conduits_severed")

	# Sector 4
	for id in ["containment1", "containment2"]:
		mission.call("CompleteCombatEvent", "encounter:" + id)
	mission.call("CommitObjective", "radiation_bulwark")
	mission.call("CompleteCombatEvent", "encounter:containment3")
	mission.call("CommitObjective", "core_overlook_breach")

	# Sector 5: Boss phases
	mission.call("CompleteCombatEvent", "encounter:boss1")
	mission.call("CommitObjective", "boss_phase:1")
	mission.call("CompleteCombatEvent", "encounter:boss2")
	mission.call("CommitObjective", "boss_phase:2")
	mission.call("CompleteCombatEvent", "encounter:boss3")
	mission.call("CommitObjective", "boss_phase:3")

	assert_bool(mission.get("CanExtract")).is_true()
	mission.free()
