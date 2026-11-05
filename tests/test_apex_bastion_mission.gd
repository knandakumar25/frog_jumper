class_name TestApexBastionMission
extends GdUnitTestSuite
## Mission 11 runtime contracts: stratospheric summit fortress infiltration, all 6 operatives, coolant vents, laser grids, Bastion Praetor boss phases, citadel blast doors breach, and throne room ascent extraction.

const SCENE := "res://scenes/gameplay/level11.tscn"


func _mission() -> Node:
	## Each test instantiates a clean mission outside saved session state.
	var catalog = load("res://scripts/managers/LevelCatalogBridge.cs").new()
	catalog.PrepareMission(11)
	catalog = null
	var mission: Node = load(SCENE).instantiate()
	add_child(mission)
	return mission


func test_mission_initializes_with_all_six_operatives_and_mission_header() -> void:
	## Mission 11 deploys the full 6-operative squad together; title card contains THE APEX BASTION.
	var mission := _mission()
	var bridge = load("res://scripts/managers/LevelCatalogBridge.cs").new()
	assert_int(bridge.UnlockedCount()).is_equal(6)
	for op_name in ["Cyber Shogun", "Surya Prime", "Oceanus", "Apex Titan", "Aura Phoenicia", "Mirage Blade"]:
		assert_bool(bridge.IsOperativeUnlocked(op_name)).is_true()
	bridge = null

	assert_int(mission.get("FragmentTotal")).is_equal(3)
	var title_card: Node = mission.get_node("MissionHUD/HUDRoot/TitleCard")
	assert_str(title_card.get_node("MissionHeading").text).contains("THE APEX BASTION")
	mission.free()


func test_hazards_and_objectives_registered_correctly() -> void:
	## Cryogenic coolant vents, redoubt laser grid, breach terminal, and throne room ascent trigger are present.
	var mission := _mission()
	var vent1: Node = mission.get_node("Actors/FlakCoolantVent1")
	assert_that(vent1).is_not_null()
	assert_bool(vent1.get("IsNeutralized")).is_false()
	vent1.call("Neutralize")
	assert_bool(vent1.get("IsNeutralized")).is_true()

	var laser: Node = mission.get_node("Actors/RedoubtLaserGrid")
	assert_that(laser).is_not_null()

	var breach_terminal: Node = mission.get_node("Actors/CitadelBreachTerminal")
	assert_that(breach_terminal).is_not_null()

	var exit_beacon: Node = mission.get_node("Actors/ThroneRoomAscentTrigger")
	assert_that(exit_beacon).is_not_null()
	mission.free()


func test_boss_and_extraction_gating() -> void:
	## Milestone sequence enforces sector gates; extraction is blocked until Bastion Praetor defeat and blast doors breach.
	var mission := _mission()
	var exit_beacon: Node = mission.get_node("Actors/ThroneRoomAscentTrigger")
	assert_that(exit_beacon).is_not_null()

	# Initial extraction is blocked
	assert_bool(mission.get("CanExtract")).is_false()

	# Sector 1
	for id in ["summit1", "summit2"]:
		mission.call("CompleteCombatEvent", "encounter:" + id)
	mission.call("CommitObjective", "staging_secured")

	# Sector 2
	mission.call("CompleteCombatEvent", "encounter:flak1")
	mission.call("CommitObjective", "flak_coolant_doused")
	mission.call("CompleteCombatEvent", "encounter:flak2")
	mission.call("CommitObjective", "titan_clamp_shattered")
	mission.call("CompleteCombatEvent", "encounter:flak3")
	mission.call("CommitObjective", "flak_battery_neutralized")

	# Sector 3
	mission.call("CompleteCombatEvent", "encounter:redoubt1")
	mission.call("CommitObjective", "laser_curtain_phased")
	mission.call("CompleteCombatEvent", "encounter:redoubt2")
	mission.call("CommitObjective", "thermal_vent_absorbed")
	mission.call("CompleteCombatEvent", "encounter:redoubt3")
	mission.call("CommitObjective", "lower_redoubt_cleared")

	# Sector 4
	mission.call("CompleteCombatEvent", "encounter:praetorian1")
	mission.call("CommitObjective", "acoustic_lock_resonated")
	mission.call("CompleteCombatEvent", "encounter:praetorian2")
	mission.call("CommitObjective", "elevator_shaft_scaled")

	# Sector 5: Boss phases
	mission.call("CompleteCombatEvent", "encounter:arena1")
	mission.call("CommitObjective", "bastion_praetor_phase:1")
	mission.call("CompleteCombatEvent", "encounter:arena2")
	mission.call("CommitObjective", "bastion_praetor_phase:2")
	mission.call("CompleteCombatEvent", "encounter:arena3")
	mission.call("CommitObjective", "bastion_praetor_phase:3")

	# Sector 6: Blast doors breach
	mission.call("CompleteCombatEvent", "encounter:breach1")
	mission.call("CommitObjective", "citadel_blast_doors_breached")

	assert_bool(mission.get("CanExtract")).is_true()
	mission.free()
