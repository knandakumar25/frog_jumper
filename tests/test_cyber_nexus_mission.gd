class_name TestCyberNexusMission
extends GdUnitTestSuite
## Mission 10 runtime contracts: mainframe core infiltration, all 6 operatives, coolant vents, laser grids, Nexus Overseer boss phases, quantum nodes override, and express ascent lift extraction.

const SCENE := "res://scenes/gameplay/level10.tscn"


func _mission() -> Node:
	## Each test instantiates a clean mission outside saved session state.
	var catalog = load("res://scripts/managers/LevelCatalogBridge.cs").new()
	catalog.PrepareMission(10)
	catalog = null
	var mission: Node = load(SCENE).instantiate()
	add_child(mission)
	return mission


func test_mission_initializes_with_all_six_operatives_and_mission_header() -> void:
	## Mission 10 deploys the full 6-operative squad together; title card contains THE CYBER-NEXUS.
	var mission := _mission()
	var bridge = load("res://scripts/managers/LevelCatalogBridge.cs").new()
	assert_int(bridge.UnlockedCount()).is_equal(6)
	for op_name in ["Cyber Shogun", "Surya Prime", "Oceanus", "Apex Titan", "Aura Phoenicia", "Mirage Blade"]:
		assert_bool(bridge.IsOperativeUnlocked(op_name)).is_true()
	bridge = null

	assert_int(mission.get("FragmentTotal")).is_equal(3)
	var title_card: Node = mission.get_node("MissionHUD/HUDRoot/TitleCard")
	assert_str(title_card.get_node("MissionHeading").text).contains("THE CYBER-NEXUS")
	mission.free()


func test_hazards_and_objectives_registered_correctly() -> void:
	## Cryogenic coolant vents, optical firewall laser grid, 3 quantum node terminals, and ascent trigger are present.
	var mission := _mission()
	var vent1: Node = mission.get_node("Actors/CryoVent1")
	assert_that(vent1).is_not_null()
	assert_bool(vent1.get("IsNeutralized")).is_false()
	vent1.call("Neutralize")
	assert_bool(vent1.get("IsNeutralized")).is_true()

	var laser: Node = mission.get_node("Actors/OpticalFirewallLaser")
	assert_that(laser).is_not_null()

	var node_alpha: Node = mission.get_node("Actors/NodeAlpha")
	assert_that(node_alpha).is_not_null()

	var node_beta: Node = mission.get_node("Actors/NodeBeta")
	assert_that(node_beta).is_not_null()

	var node_gamma: Node = mission.get_node("Actors/NodeGamma")
	assert_that(node_gamma).is_not_null()

	var exit_beacon: Node = mission.get_node("Actors/ApexAscentTrigger")
	assert_that(exit_beacon).is_not_null()
	mission.free()


func test_boss_and_extraction_gating() -> void:
	## Milestone sequence enforces sector gates; extraction is blocked until Nexus Overseer defeat and quantum node override.
	var mission := _mission()
	var exit_beacon: Node = mission.get_node("Actors/ApexAscentTrigger")
	assert_that(exit_beacon).is_not_null()

	# Initial extraction is blocked
	assert_bool(mission.get("CanExtract")).is_false()

	# Sector 1
	for id in ["ingress1", "ingress2"]:
		mission.call("CompleteCombatEvent", "encounter:" + id)
	mission.call("CommitObjective", "ingress_confirmed")

	# Sector 2
	mission.call("CompleteCombatEvent", "encounter:coolant1")
	mission.call("CommitObjective", "helium_manifold_doused")
	mission.call("CompleteCombatEvent", "encounter:coolant2")
	mission.call("CommitObjective", "server_bracket_shattered")
	mission.call("CompleteCombatEvent", "encounter:coolant3")
	mission.call("CommitObjective", "cooling_vaults_secured")

	# Sector 3
	mission.call("CompleteCombatEvent", "encounter:data1")
	mission.call("CommitObjective", "laser_firewall_phased")
	mission.call("CompleteCombatEvent", "encounter:data2")
	mission.call("CommitObjective", "biometric_firewall_absorbed")
	mission.call("CompleteCombatEvent", "encounter:data3")
	mission.call("CommitObjective", "optical_conduits_secured")

	# Sector 4
	for id in ["neural1", "neural2"]:
		mission.call("CompleteCombatEvent", "encounter:" + id)
	mission.call("CommitObjective", "cipher_resonance_bypassed")
	mission.call("CompleteCombatEvent", "encounter:neural3")
	mission.call("CommitObjective", "neural_substation_breached")

	# Sector 5: Boss phases
	mission.call("CompleteCombatEvent", "encounter:core1")
	mission.call("CommitObjective", "nexus_overseer_phase:1")
	mission.call("CompleteCombatEvent", "encounter:core2")
	mission.call("CommitObjective", "nexus_overseer_phase:2")
	mission.call("CompleteCombatEvent", "encounter:core3")
	mission.call("CommitObjective", "nexus_overseer_phase:3")

	# Sector 6: Quantum Nodes Override
	mission.call("CommitObjective", "quantum_nodes_overridden")

	assert_bool(mission.get("CanExtract")).is_true()
	mission.free()
