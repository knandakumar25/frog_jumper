class_name TestCyberGraveMission
extends GdUnitTestSuite
## Mission 6 runtime contracts: deployment, 6-operative squad, stasis pods, coolant vents, checkpoints, and extraction.

const SCENE := "res://scenes/gameplay/level6.tscn"


func _mission() -> Node:
	## Each test instantiates a clean mission outside saved session state.
	var catalog = load("res://scripts/managers/LevelCatalogBridge.cs").new()
	catalog.PrepareMission(6)
	catalog = null
	var mission: Node = load(SCENE).instantiate()
	add_child(mission)
	return mission


func test_mission_initializes_with_all_six_operatives_and_mission_header() -> void:
	## Mission 6 deploys the full 6-operative squad together; Surya is available.
	var mission := _mission()
	var bridge = load("res://scripts/managers/LevelCatalogBridge.cs").new()
	assert_int(bridge.UnlockedCount()).is_equal(6)
	for op_name in ["Cyber Shogun", "Surya Prime", "Oceanus", "Apex Titan", "Aura Phoenicia", "Mirage Blade"]:
		assert_bool(bridge.IsOperativeUnlocked(op_name)).is_true()
	bridge = null

	assert_int(mission.get("FragmentTotal")).is_equal(3)
	var title_card: Node = mission.get_node("MissionHUD/HUDRoot/TitleCard")
	assert_str(title_card.get_node("MissionHeading").text).contains("CYBER-GRAVE LABS")
	mission.free()


func test_stasis_pods_and_coolant_vents_registered_correctly() -> void:
	## GenePods and CoolantVents are present in the actor container.
	var mission := _mission()
	var pod1: Node = mission.get_node("Actors/Pod1")
	assert_that(pod1).is_not_null()
	assert_bool(pod1.call("TryLiberate")).is_false()

	var vent1: Node = mission.get_node("Actors/Vent1")
	assert_that(vent1).is_not_null()
	assert_bool(vent1.get("IsNeutralized")).is_false()
	vent1.call("Neutralize")
	assert_bool(vent1.get("IsNeutralized")).is_true()
	mission.free()


func test_checkpoints_and_extraction_gating() -> void:
	## Shrines enforce sector prerequisites; extraction is blocked until vault override.
	var mission := _mission()
	var exit_beacon: Node = mission.get_node("Actors/ExtractionLift")
	assert_that(exit_beacon).is_not_null()

	# Initial extraction is blocked
	assert_bool(mission.get("CanExtract")).is_false()

	# Simulate clearing through all sectors to unlock extraction
	for id in ["sump1", "sump2"]:
		mission.call("CompleteCombatEvent", "encounter:" + id)
	mission.call("CommitObjective", "sump_breach")

	for id in ["incubate1", "incubate2", "incubate3"]:
		mission.call("CompleteCombatEvent", "encounter:" + id)
	mission.call("CommitObjective", "pod:1")
	mission.call("CommitObjective", "pod:2")
	mission.call("CommitObjective", "incubation_cleared")

	for id in ["coolant1", "coolant2"]:
		mission.call("CompleteCombatEvent", "encounter:" + id)
	mission.call("CommitObjective", "coolant_override")

	for id in ["resonance1", "resonance2"]:
		mission.call("CompleteCombatEvent", "encounter:" + id)
	mission.call("CommitObjective", "resonance_bypass")

	mission.call("CompleteCombatEvent", "encounter:laser1")
	mission.call("CommitObjective", "grid_phase")

	for id in ["siphon1", "siphon2"]:
		mission.call("CompleteCombatEvent", "encounter:" + id)
	mission.call("CommitObjective", "siphon_contain")

	mission.call("CompleteCombatEvent", "encounter:core1")
	mission.call("CommitObjective", "core_defense:1")

	mission.call("CompleteCombatEvent", "encounter:core2")
	mission.call("CommitObjective", "core_defense:2")

	mission.call("CompleteCombatEvent", "encounter:core3")
	mission.call("CommitObjective", "core_defense:3")

	mission.call("CommitObjective", "vault_override")
	assert_bool(mission.get("CanExtract")).is_true()
	mission.free()
