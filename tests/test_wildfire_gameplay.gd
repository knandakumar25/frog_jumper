class_name TestWildfireGameplay
extends GdUnitTestSuite
## Real scene tests for local safety, shield logic, transient cleanup, repeat events and device-aware lessons.


func _mission() -> Node:
	## Fresh solo Shogun route; this suite explicitly skips the independently covered cinematic.
	var catalog = load("res://scripts/managers/LevelCatalogBridge.cs").new()
	catalog.PrepareMission(2)
	var mission := scene_runner("res://scenes/gameplay/level2.tscn").scene()
	mission.get_node("ConcreteWildfireIntro").call("Finish")
	return mission


func _defeat(mission: Node, id: String) -> void:
	## Defeat the actual authored members, exercising their production callbacks and completion ledger.
	var encounter := mission.get_node("Actors/Encounter_" + id)
	encounter.call("Activate")
	for enemy in encounter.get_children():
		if enemy.has_method("TakeDamage"):
			enemy.call("TakeDamage", 1000.0)


func test_encounter_boundaries_and_completed_groups_never_respawn() -> void:
	## No group is globally awake; crossing one local boundary activates just that formation.
	var mission := _mission()
	var encounter := mission.get_node("Actors/Encounter_road-parry")
	assert_bool(encounter.get("Active")).is_false()
	mission.get_node("Actors/CyberShogun").position = Vector2(800, 554)
	await get_tree().physics_frame
	await get_tree().physics_frame
	assert_bool(encounter.get("Active")).is_true()
	assert_bool(mission.get_node("Actors/Encounter_road-dash").get("Active")).is_false()
	_defeat(mission, "road-parry")
	await get_tree().process_frame
	encounter.call("ResetIncomplete")
	encounter.call("Activate")
	assert_bool(encounter.get("Completed")).is_true()
	assert_int(encounter.get_child_count()).is_equal(0)
	mission.free()


func test_rescue_requires_combat_and_fire_then_permanently_secures_route() -> void:
	## Dousing alone cannot release civilians. Once combat is cleared, the route can never reignite beneath them.
	var mission := _mission()
	var fire := mission.get_node("Actors/ResidentialRouteFire")
	var group := mission.get_node("Actors/Rescue_residential")
	fire.call("ReceiveAbility", "douse", fire.position + Vector2(-180, -50), 1.0)
	group.call("_PhysicsProcess", 0.1)
	assert_bool(group.get("RouteSecured")).is_false()
	_defeat(mission, "residential-route")
	group.call("_PhysicsProcess", 0.1)
	assert_bool(group.get("RouteSecured")).is_true()
	assert_bool(fire.get("PermanentlySafe")).is_true()
	fire.call("_PhysicsProcess", 12.0)
	assert_bool(fire.get("Cleared")).is_true()
	group.call("_PhysicsProcess", 6.0)
	group.call("_PhysicsProcess", 6.0)
	assert_bool(group.get("Evacuated")).is_true()
	assert_int(mission.get("CivilianGroupsEvacuated")).is_equal(1)
	mission.free()


func test_traversal_fire_reignites_and_wrong_abilities_fail() -> void:
	## Ordinary challenges retain temporary dousing; route-only permanence cannot clear a burning gate for free.
	var mission := _mission()
	var fire := mission.get_node("Actors/FireCorridor1")
	assert_bool(fire.call("SecureForEvacuation")).is_false()
	assert_bool(fire.call("ReceiveAbility", "slam", fire.position, 1.0)).is_false()
	assert_bool(fire.call("ReceiveAbility", "douse", fire.position + Vector2(-200, -50), 1.0)).is_true()
	fire.call("_PhysicsProcess", 8.1)
	assert_bool(fire.get("Cleared")).is_false()
	mission.free()


func test_battery_shield_relay_duplicates_and_permanent_artillery_stop() -> void:
	## The core ignores all damage until both distinct relays fall; repeated relay signals cannot remove both charges.
	var mission := _mission()
	var battery := mission.get_node("Actors/SiegeBattery")
	var core := battery.get_node("Core")
	core.call("TakeDamage", 1000.0)
	assert_float(core.get("CurrentHealth")).is_equal(210.0)
	var west := battery.get_node("Relay_west")
	west.call("TakeDamage", 1000.0)
	west.emit_signal("Neutralized")
	assert_int(core.get("ShieldCharges")).is_equal(1)
	assert_bool(mission.get_node("Actors/Encounter_battery-reinforcements").get("Active")).is_true()
	battery.get_node("Relay_east").call("TakeDamage", 1000.0)
	assert_bool(core.get("Shielded")).is_false()
	core.call("TakeDamage", 1000.0)
	core.emit_signal("Neutralized")
	assert_bool(battery.get("Destroyed")).is_true()
	assert_bool(mission.get_node("Actors/Artillery").get("Stopped")).is_true()
	assert_bool(mission.get("ExtractionReady")).is_false()
	mission.free()


func test_artillery_locks_target_rejects_safe_zones_and_cancels_before_fall() -> void:
	## Warning remains at its original location when the player moves and cannot survive recovery.
	var mission := _mission()
	var artillery := mission.get_node("Actors/Artillery")
	for point in [Vector2(5400, 554), Vector2(5700, 554), Vector2(9800, 554), Vector2(15600, 554), Vector2(28300, 554)]:
		assert_bool(artillery.call("TryWarn", point)).is_false()
	assert_bool(artillery.call("TryWarn", Vector2(2900, 554))).is_true()
	assert_float(artillery.get("WarningRemaining")).is_greater_equal(2.0)
	var target: Vector2 = artillery.get("LockedTarget")
	var player := mission.get_node("Actors/CyberShogun")
	player.position = Vector2(3000, 554)
	artillery.call("_PhysicsProcess", 0.1)
	assert_vector(artillery.get("LockedTarget")).is_equal(target)
	player.position.y = 3000
	await get_tree().physics_frame
	await get_tree().physics_frame
	assert_float(artillery.get("WarningRemaining")).is_equal(0.0)
	assert_float(player.position.y).is_less(1000.0)
	mission.free()


func test_recovery_restarts_incomplete_group_without_duplicate_rewards() -> void:
	## A partial group resets its surviving actors but its already awarded member cannot be farmed.
	var mission := _mission()
	var encounter := mission.get_node("Actors/Encounter_road-dash")
	encounter.call("Activate")
	encounter.get_child(0).call("TakeDamage", 1000.0)
	var points: int = mission.get_node("GameManager").get("Points")
	mission.get_node("Actors/CyberShogun").position.y = 3000
	await get_tree().physics_frame
	await get_tree().physics_frame
	encounter.call("Activate")
	encounter.get_child(0).call("TakeDamage", 1000.0)
	assert_int(mission.get_node("GameManager").get("Points")).is_equal(points)
	mission.free()


func test_recruits_precede_required_gates_and_device_prompts_follow_input() -> void:
	## Lessons remain reachable by solo Shogun and controller instructions do not ask for a keyboard key.
	var mission := _mission()
	var recruit := mission.get_node("Actors/OceanusJoins")
	assert_float(recruit.position.x).is_less(mission.get_node("Actors/FireCorridor1").position.x)
	assert_float(mission.get_node("Actors/TitanJoins").position.x).is_less(mission.get_node("Actors/TitanPracticeRubble").position.x)
	recruit.call("Trigger")
	var pad := InputEventJoypadButton.new()
	pad.pressed = true
	pad.button_index = JOY_BUTTON_Y
	recruit.call("_Input", pad)
	recruit.call("_PhysicsProcess", 0.01)
	assert_str(mission.get("PromptText")).contains("D-Pad")
	assert_str(mission.get("PromptText")).not_contains("press Q")
	var touch := InputEventScreenTouch.new()
	touch.pressed = true
	recruit.call("_Input", touch)
	recruit.call("_PhysicsProcess", 0.01)
	assert_str(mission.get("PromptText")).contains("Ability")
	mission.free()


func test_extraction_portal_rejects_incomplete_mission() -> void:
	## Walking into the last portal cannot skip rescues, spires, relays or the final barrier.
	var mission := _mission()
	mission.get_node("Actors/CyberShogun").position = Vector2(35100, 554)
	await get_tree().physics_frame
	await get_tree().physics_frame
	assert_bool(mission.get("MissionComplete")).is_false()
	assert_bool(mission.get("ExtractionReady")).is_false()
	mission.free()


func _fall_and_recover(mission: Node) -> void:
	## Exercise the shared production fall path; tests never call a test-only objective-state setter.
	mission.get_node("Actors/CyberShogun").position.y = 3000
	await get_tree().physics_frame
	await get_tree().physics_frame
	assert_float(mission.get_node("Actors/CyberShogun").position.y).is_less(1000.0)


func test_complete_objective_chain_survives_recovery_at_every_milestone() -> void:
	## Replay the mission's actual callbacks, recovering before/after recruits, rescues, spires, relays and core.
	var mission := _mission()
	var player := mission.get_node("Actors/CyberShogun")
	var catalog = load("res://scripts/managers/LevelCatalogBridge.cs").new()
	await _fall_and_recover(mission)
	assert_int(catalog.UnlockedCount()).is_equal(1)
	mission.get_node("Actors/OceanusJoins").call("Trigger")
	await _fall_and_recover(mission)
	assert_bool(catalog.IsOperativeUnlocked("Oceanus")).is_true()
	for id in ["residential", "overpass"]:
		_defeat(mission, id + "-route" if id == "residential" else "overpass-exit")
		var fire := mission.get_node("Actors/ResidentialRouteFire" if id == "residential" else "Actors/OverpassRouteFire")
		fire.call("ReceiveAbility", "douse", fire.position + Vector2(-180, -50), 1.0)
		var group := mission.get_node("Actors/Rescue_" + id)
		group.call("_PhysicsProcess", 6.0)
		await _fall_and_recover(mission)
		assert_bool(group.get("Evacuated")).is_true()
		assert_bool(fire.get("PermanentlySafe")).is_true()
	assert_int(mission.get("CivilianGroupsEvacuated")).is_equal(2)
	player.position = Vector2(16000, 554)
	mission.call("_PhysicsProcess", 0.01)
	await _fall_and_recover(mission)
	assert_float(player.position.x).is_equal_approx(16000.0, 2.0)
	var count := 0
	for id in ["street", "tower", "courtyard"]:
		_defeat(mission, "spire-" + id)
		mission.call("_PhysicsProcess", 0.01)
		var spire := mission.get_node("Actors/BiometricSpire%d" % (count + 1))
		spire.call("TakeDamage", 1000.0)
		spire.emit_signal("Neutralized")
		count += 1
		await _fall_and_recover(mission)
		assert_int(mission.get("SpiresDisabled")).is_equal(count)
	assert_bool(mission.get("MembraneOpen")).is_true()
	mission.get_node("Actors/TitanJoins").call("Trigger")
	await _fall_and_recover(mission)
	assert_bool(catalog.IsOperativeUnlocked("Apex Titan")).is_true()
	for wall in ["TitanPracticeRubble", "DefendedRubble"]:
		var barrier := mission.get_node("Actors/" + wall)
		barrier.call("ReceiveAbility", "slam", barrier.position + Vector2(-120, -50), 1.0)
		await _fall_and_recover(mission)
		assert_bool(barrier.get("Cleared")).is_true()
	var battery := mission.get_node("Actors/SiegeBattery")
	for relay in ["Relay_west", "Relay_east"]:
		battery.get_node(relay).call("TakeDamage", 1000.0)
		await _fall_and_recover(mission)
		assert_bool(battery.get_node(relay).get("Collected")).is_true()
	battery.get_node("Core").call("TakeDamage", 1000.0)
	await _fall_and_recover(mission)
	assert_bool(battery.get("Destroyed")).is_true()
	assert_bool(mission.get_node("Actors/Artillery").get("Stopped")).is_true()
	var wall := mission.get_node("Actors/MembraneRubble")
	wall.call("ReceiveAbility", "slam", wall.position + Vector2(-120, -50), 1.0)
	_defeat(mission, "extraction-guard")
	mission.get_node("Actors/Rescue_extraction").call("_PhysicsProcess", 6.0)
	await _fall_and_recover(mission)
	assert_int(mission.get("CivilianGroupsEvacuated")).is_equal(3)
	assert_bool(mission.get("ExtractionReady")).is_true()
	var manager := mission.get_node("GameManager")
	manager.set("Points", 150)
	player.emit_signal("ComboChanged", 4)
	player.position = Vector2(35100, 554)
	mission.call("_PhysicsProcess", 0.01)
	mission.call("_PhysicsProcess", 0.01)
	assert_bool(mission.get("MissionComplete")).is_true()
	await get_tree().process_frame
	var overlay := mission.get_node("WildfireAftermath")
	overlay.set_process(false)
	assert_int(mission.find_children("WildfireAftermath", "", false, false).size()).is_equal(1)
	assert_bool(player.can_process()).is_false()
	assert_bool(mission.get_node("CanvasLayer/PauseMenu").can_process()).is_false()
	assert_bool(mission.get_node("MissionHUD").visible).is_false()
	assert_bool(mission.get_node("AudioStreamPlayer").playing).is_false()
	var frozen: Vector2 = player.position
	var health: float = player.get("CurrentHealth")
	for _frame in range(5):
		await get_tree().physics_frame
	assert_vector(player.position).is_equal(frozen)
	assert_float(player.get("CurrentHealth")).is_equal(health)
	# External mutations cannot rewrite the immutable extraction snapshot.
	manager.set("Points", 999)
	player.emit_signal("ComboChanged", 20)
	overlay.call("Finish")
	overlay.call("Finish")
	await get_tree().process_frame
	var card := mission.get_node("CyberVictoryScorecard")
	assert_bool(card.get_node("RootPanel").visible).is_true()
	assert_str(card.get_node("RootPanel/MainVBox/StatsGrid/ScoreLabel").text).contains("150 PTS")
	assert_str(card.get_node("RootPanel/MainVBox/StatsGrid/ComboLabel").text).contains("4x STRIKE")
	assert_bool(card.get_node("RootPanel/MainVBox/ContinueBtn").visible).is_true()
	mission.free()


func test_defeat_cancels_projectiles_and_restarts_incomplete_encounters() -> void:
	## The delayed death path cancels all pending damage before its timer, preserving the same scene and recruits.
	var mission := _mission()
	var encounter := mission.get_node("Actors/Encounter_road-parry")
	encounter.call("Activate")
	var gunner := encounter.get_child(0)
	mission.call("LaunchRound", Vector2(1000, 550), Vector2.RIGHT, gunner)
	mission.get_node("Actors/Artillery").call("TryWarn", Vector2(2900, 554))
	mission.get_node("Actors/CyberShogun").call("TakeDamage", 10000.0, Vector2.ZERO)
	assert_bool(mission.get("GameplaySuspended")).is_true()
	assert_float(mission.get_node("Actors/Artillery").get("WarningRemaining")).is_equal(0.0)
	await get_tree().create_timer(1.1).timeout
	assert_bool(mission.get("GameplaySuspended")).is_false()
	assert_float(mission.get_node("Actors/CyberShogun").get("CurrentHealth")).is_greater(0.0)
	assert_int(get_tree().get_nodes_in_group("wildfire_rounds").size()).is_equal(0)
	assert_bool(encounter.get("Active")).is_false()
	assert_bool(mission.get("ArrivalActive")).is_false()
	mission.free()


func test_gunner_round_uses_existing_parry_and_swept_cover_collision() -> void:
	## Real round collision reflects to the gunner through TakeDamageFrom; concrete blocks a second swept shot.
	var mission := _mission()
	var player := mission.get_node("Actors/CyberShogun")
	var encounter := mission.get_node("Actors/Encounter_road-parry")
	encounter.call("Activate")
	var gunner := encounter.get_child(0)
	player.position = Vector2(800, 554)
	player.call("TriggerSignatureAbility")
	var health: float = player.get("CurrentHealth")
	mission.call("LaunchRound", Vector2(790, 554), Vector2.RIGHT, gunner)
	for frame in range(4):
		await get_tree().physics_frame
	assert_float(gunner.get("Health")).is_equal(35.0)
	assert_float(player.get("CurrentHealth")).is_equal(health)
	mission.call("LaunchRound", Vector2(880, 550), Vector2.RIGHT, gunner)
	for frame in range(5):
		await get_tree().physics_frame
	assert_int(get_tree().get_nodes_in_group("wildfire_rounds").size()).is_equal(0)
	mission.free()


func test_new_systems_remain_inactive_until_natural_arrival_handoff() -> void:
	## New components must honor the same freeze contract for natural completion as for Skip.
	var mission := scene_runner("res://scenes/gameplay/level2.tscn").scene()
	var encounter := mission.get_node("Actors/Encounter_road-parry")
	var artillery := mission.get_node("Actors/Artillery")
	var rescue := mission.get_node("Actors/Rescue_residential")
	assert_bool(encounter.can_process()).is_false()
	assert_bool(artillery.can_process()).is_false()
	assert_bool(rescue.can_process()).is_false()
	assert_int(encounter.get_child_count()).is_equal(0)
	mission.get_node("ConcreteWildfireIntro").call("Seek", 60.0)
	mission.get_node("ConcreteWildfireIntro").call("_Process", 0.1)
	assert_bool(mission.get("ArrivalActive")).is_false()
	assert_bool(encounter.can_process()).is_true()
	assert_bool(artillery.can_process()).is_true()
	assert_bool(rescue.can_process()).is_true()
	assert_bool(encounter.get("Active")).is_false()
	assert_bool(rescue.get("RouteSecured")).is_false()
	mission.free()


func test_overpass_checkpoint_requires_shelter_safety() -> void:
	## Sprinting past the shelter cannot activate its checkpoint while its designated guards/fire remain active.
	var mission := _mission()
	var player := mission.get_node("Actors/CyberShogun")
	var shrine := mission.get_node("Actors/ShrineOverpass")
	player.position = Vector2(16000, 554)
	mission.call("_PhysicsProcess", 0.01)
	assert_bool(shrine.get("Activated")).is_false()
	_defeat(mission, "overpass-exit")
	var fire := mission.get_node("Actors/OverpassRouteFire")
	fire.call("ReceiveAbility", "douse", fire.position + Vector2(-180, -50), 1.0)
	mission.get_node("Actors/Rescue_overpass").call("_PhysicsProcess", 0.1)
	mission.call("_PhysicsProcess", 0.01)
	assert_bool(shrine.get("Activated")).is_true()
	mission.free()


func test_gunner_warns_bursts_three_times_then_recovers() -> void:
	## Close-range gunfire has a visible aim window and finite burst, with no hidden continuous fire.
	var mission := _mission()
	var encounter := mission.get_node("Actors/Encounter_road-parry")
	encounter.call("Activate")
	var gunner := encounter.get_child(0)
	mission.get_node("Actors/CyberShogun").position = Vector2(1200, 554)
	await get_tree().physics_frame
	gunner.call("_PhysicsProcess", 1.3)
	assert_str(gunner.get("State")).is_equal("aim")
	assert_int(get_tree().get_nodes_in_group("wildfire_rounds").size()).is_equal(0)
	gunner.call("_PhysicsProcess", 1.2)
	gunner.call("_PhysicsProcess", 0.23)
	gunner.call("_PhysicsProcess", 0.23)
	assert_str(gunner.get("State")).is_equal("recovery")
	assert_int(get_tree().get_nodes_in_group("wildfire_rounds").size()).is_equal(3)
	mission.free()
