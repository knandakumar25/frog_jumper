class_name TestJudgmentDayMission
extends GdUnitTestSuite
## Mission 12 runtime contracts: Citadel Summit & Void Singularity Chamber, all 6 operatives, coolant vents, laser grids, Void Commander boss phases, core containment override, and campaign victory extraction.

const SCENE := "res://scenes/gameplay/level12.tscn"


func _mission() -> Node:
	## Each test instantiates a clean mission outside saved session state.
	var catalog = load("res://scripts/managers/LevelCatalogBridge.cs").new()
	catalog.PrepareMission(12)
	catalog = null
	var mission: Node = load(SCENE).instantiate()
	add_child(mission)
	return mission


func test_mission_initializes_with_all_six_operatives_and_mission_header() -> void:
	## Mission 12 deploys the full 6-operative squad together; title card contains JUDGMENT DAY.
	var mission := _mission()
	var bridge = load("res://scripts/managers/LevelCatalogBridge.cs").new()
	assert_int(bridge.UnlockedCount()).is_equal(6)
	for op_name in ["Cyber Shogun", "Surya Prime", "Oceanus", "Apex Titan", "Aura Phoenicia", "Mirage Blade"]:
		assert_bool(bridge.IsOperativeUnlocked(op_name)).is_true()
	bridge = null

	assert_int(mission.get("FragmentTotal")).is_equal(3)
	var title_card: Node = mission.get_node("MissionHUD/HUDRoot/TitleCard")
	assert_str(title_card.get_node("MissionHeading").text).contains("JUDGMENT DAY")
	mission.free()


func test_hazards_and_objectives_registered_correctly() -> void:
	## Cryogenic coolant vents, void barrier laser grid, core containment terminal, and extraction threshold are present.
	var mission := _mission()
	var vent1: Node = mission.get_node("Actors/SiphonCoolantVent1")
	assert_that(vent1).is_not_null()
	assert_bool(vent1.get("IsNeutralized")).is_false()
	vent1.call("Neutralize")
	assert_bool(vent1.get("IsNeutralized")).is_true()

	var laser: Node = mission.get_node("Actors/VoidBarrierLaserGrid")
	assert_that(laser).is_not_null()

	var core_terminal: Node = mission.get_node("Actors/CoreContainmentTerminal")
	assert_that(core_terminal).is_not_null()

	var exit_beacon: Node = mission.get_node("Actors/ExtractionGate")
	assert_that(exit_beacon).is_not_null()
	mission.free()


func test_boss_and_extraction_gating() -> void:
	## Milestone sequence enforces sector gates; extraction is blocked until Void Commander defeat and core containment.
	var mission := _mission()
	var exit_beacon: Node = mission.get_node("Actors/ExtractionGate")
	assert_that(exit_beacon).is_not_null()

	# Initial extraction is blocked
	assert_bool(mission.get("CanExtract")).is_false()

	# Sector 1
	for id in ["ingress1", "ingress2"]:
		mission.call("CompleteCombatEvent", "encounter:" + id)
	mission.call("CommitObjective", "ingress_secured")

	# Sector 2
	mission.call("CompleteCombatEvent", "encounter:siphon1")
	mission.call("CommitObjective", "siphon_coolant_doused")
	mission.call("CompleteCombatEvent", "encounter:siphon2")
	mission.call("CommitObjective", "siphon_clamp_shattered")
	mission.call("CompleteCombatEvent", "encounter:siphon3")
	mission.call("CommitObjective", "siphon_grid_neutralized")

	# Sector 3
	mission.call("CompleteCombatEvent", "encounter:barrier1")
	mission.call("CommitObjective", "laser_curtain_phased")
	mission.call("CompleteCombatEvent", "encounter:barrier2")
	mission.call("CommitObjective", "cosmic_vent_absorbed")
	mission.call("CompleteCombatEvent", "encounter:barrier3")
	mission.call("CommitObjective", "barrier_corridor_cleared")

	# Sector 4
	mission.call("CompleteCombatEvent", "encounter:vault1")
	mission.call("CommitObjective", "harmonic_lock_resonated")
	mission.call("CompleteCombatEvent", "encounter:vault2")
	mission.call("CommitObjective", "cathedral_vault_scaled")

	# Sector 5: Boss phases
	mission.call("CompleteCombatEvent", "encounter:boss1")
	mission.call("CommitObjective", "void_commander_phase:1")
	mission.call("CompleteCombatEvent", "encounter:boss2")
	mission.call("CommitObjective", "void_commander_phase:2")
	mission.call("CompleteCombatEvent", "encounter:boss3")
	mission.call("CommitObjective", "void_commander_phase:3")

	# Sector 6: Core containment override
	mission.call("CompleteCombatEvent", "encounter:citadel_remnant1")
	mission.call("CommitObjective", "core_containment_initiated")

	assert_bool(mission.get("CanExtract")).is_true()
	mission.free()
