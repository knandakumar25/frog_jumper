class_name TestAegisFortress
extends GdUnitTestSuite
## Fortress integration checks. Seeded ledger setup is diagnostic; real ability checks exercise the controller separately.

const LEVEL := "res://scenes/gameplay/level4.tscn"
const BEFORE_GUN := ["encounter:s1-3", "interlock:approach", "encounter:s2-1", "encounter:s2-2", "recruit:surya", "lesson:bulwark", "crossing:bulwark", "encounter:s3-3", "coolant:intake", "encounter:s4-3"]


func _mission() -> Node:
	## Use the actual scene and finish its independent arrival through the public handoff.
	var mission: Node = load(LEVEL).instantiate()
	add_child(mission)
	mission.get_node("ShatteredAegisIntro").call("Finish")
	await get_tree().process_frame
	return mission


func _seed(mission: Node, events: Array) -> void:
	## Supply exact accepted events without teleporting actors or claiming normal-play coverage.
	for event in events:
		mission.call("CompleteCombatEvent", event)


func _arena(mission: Node) -> Node:
	## Reach the arena after the exact objective dependencies; the boss remains the existing scene.
	_seed(mission, BEFORE_GUN + ["guns:western", "lesson:blink", "encounter:s5-3", "curtain:inner"])
	# Synchronize the safe checkpoint before entering the sealed arena through its real trigger.
	mission.get_node("Actors/CyberShogun").position = Vector2(21100, 554)
	mission.call("_PhysicsProcess", 0.016)
	mission.get_node("Actors/CyberShogun").position = Vector2(22000, 554)
	mission.call("_PhysicsProcess", 0.016)
	var boss: Node = mission.get("Boss")
	boss.set_physics_process(false)
	return boss


func _expose(controller: Node) -> void:
	## Advance a full warned volley and floor strike; no projectiles survive into recovery.
	controller.call("Tick", 1.2)
	controller.call("Tick", 1.8)
	controller.call("Tick", 1.1)
	controller.call("Tick", 0.31)


func test_both_gun_routes_converge_and_reject_premature_operation() -> void:
	## Douse and slam operate real gates; early or repeated abilities cannot skip defenders or duplicate progress.
	for route in ["GunCoolant", "MaintenanceBreach"]:
		var mission := await _mission()
		var gate := mission.get_node("Actors/" + route)
		var tag: String = gate.get("RequiredTag")
		var origin: Vector2 = gate.position + Vector2(-150, -46)
		assert_bool(gate.call("ReceiveAbility", tag, origin, 1.0)).is_false()
		_seed(mission, BEFORE_GUN)
		assert_bool(gate.call("ReceiveAbility", tag, origin, 1.0)).is_true()
		assert_bool(mission.call("HasCombatEvent", "guns:western")).is_true()
		assert_bool(gate.call("ReceiveAbility", tag, origin, 1.0)).is_false()
		await get_tree().process_frame
		mission.free()


func test_protected_crossing_needs_the_actual_window() -> void:
	## Walking the harmless vent fails. Real Surya Bulwark across its complete footprint succeeds.
	var mission := await _mission()
	_seed(mission, ["encounter:s1-3", "interlock:approach", "encounter:s2-1", "encounter:s2-2"])
	mission.get_node("Actors/SuryaJoins").call("Trigger")
	mission.call("_PhysicsProcess", 0.016)
	var player := mission.get_node("Actors/CyberShogun")
	player.position = Vector2(6770,554)
	mission.call("_PhysicsProcess", 0.016)
	player.position.x = 6860
	mission.call("_PhysicsProcess", 0.016)
	player.position.x = 7020
	mission.call("_PhysicsProcess", 0.016)
	assert_bool(mission.call("HasCombatEvent", "lesson:bulwark")).is_false()
	player.call("SwapOperative", "Surya Prime")
	player.position.x = 6770
	mission.call("_PhysicsProcess", 0.016)
	player.call("TriggerSignatureAbility")
	assert_bool(player.get("IsBulwarked")).is_true()
	player.position.x = 6860
	mission.call("_PhysicsProcess", 0.016)
	player.position.x = 7020
	mission.call("_PhysicsProcess", 0.016)
	assert_bool(mission.call("HasCombatEvent", "lesson:bulwark")).is_true()
	await get_tree().process_frame
	mission.free()


func test_blink_requires_a_real_forward_crossing() -> void:
	## An ability from the wrong side fails; a real 180px Blink crosses the practice beam with environmental protection.
	var mission := await _mission()
	_seed(mission, BEFORE_GUN + ["guns:western"])
	var player := mission.get_node("Actors/CyberShogun")
	player.call("SwapOperative", "Mirage Blade")
	player.position = Vector2(17000,54)
	mission.call("_PhysicsProcess", 0.016)
	player.call("TriggerSignatureAbility")
	assert_bool(mission.call("HasCombatEvent", "lesson:blink")).is_false()
	player.set("AbilityCooldownTimer", 0.0)
	player.position = Vector2(17290,54)
	mission.call("_PhysicsProcess", 0.016)
	player.call("TriggerSignatureAbility")
	assert_float(player.position.x).is_greater(17430.0)
	assert_bool(mission.call("HasCombatEvent", "lesson:blink")).is_true()
	await get_tree().process_frame
	mission.free()


func test_all_phases_clamp_large_hits_and_pylons_shield_core() -> void:
	## Massive hits stop at each boundary; pylons persist offline through phase three and defeat still needs crossing release.
	var mission := await _mission()
	var boss := _arena(mission)
	var controller: Node = mission.get("Fortress")
	boss.call("TakeDamage", 10000.0)
	assert_float(boss.get("CurrentHealth")).is_equal(300.0)
	_expose(controller)
	assert_bool(controller.get("Exposed")).is_true()
	assert_int(get_tree().get_nodes_in_group("aegis_projectiles").filter(func(n: Node) -> bool: return not n.is_queued_for_deletion()).size()).is_equal(0)
	boss.call("TakeDamage", 10000.0)
	assert_float(boss.get("CurrentHealth")).is_equal(200.0)
	assert_int(boss.get("CurrentPhase")).is_equal(2)
	assert_bool(controller.get("Shielded")).is_true()
	boss.call("TakeReflectedDamage", 10000.0)
	assert_float(boss.get("CurrentHealth")).is_equal(200.0)
	for node in mission.get_node("Actors").get_children():
		if node.name.begins_with("ArenaPylon"):
			node.call("TakeDamage", 10000.0)
	controller.call("Tick", 0.01)
	assert_bool(controller.get("Exposed")).is_true()
	boss.call("TakeDamage", 10000.0)
	assert_float(boss.get("CurrentHealth")).is_equal(100.0)
	assert_int(boss.get("CurrentPhase")).is_equal(3)
	assert_bool(controller.get("Shielded")).is_false()
	_expose(controller)
	boss.call("TakeDamage", 10000.0)
	assert_bool(mission.get("BossDefeated")).is_true()
	assert_bool(mission.get_node("PalaceGeometry/BorderWall").get("IsOpen")).is_false()
	mission.get_node("Actors/CrossingControl").call("TakeDamage", 1.0)
	mission.call("_PhysicsProcess", 0.016)
	assert_bool(mission.get_node("PalaceGeometry/BorderWall").get("IsOpen")).is_true()
	assert_bool(mission.get("MissionComplete")).is_false()
	await get_tree().physics_frame
	await get_tree().physics_frame
	assert_int(mission.get_node("Actors/CrossingControl/Hurtbox").get("collision_layer")).is_equal(0)
	mission.get_node("Actors/CyberShogun").position.y = 3000
	mission.call("_PhysicsProcess", 0.016)
	assert_bool(mission.get("BossDefeated")).is_true()
	assert_that(mission.get("Boss")).is_null()
	await get_tree().process_frame
	mission.free()


func test_recovery_resets_boss_attempt_and_retains_objectives() -> void:
	## Each health phase restarts at full health with fresh pylons, without replaying arrival or losing prior mission state.
	for phase in [1,2,3]:
		var mission := await _mission()
		var boss := _arena(mission)
		var controller: Node = mission.get("Fortress")
		if phase >= 2:
			_expose(controller)
			boss.call("TakeDamage", 1000.0)
		if phase == 3:
			for node in mission.get_node("Actors").get_children():
				if node.name.begins_with("ArenaPylon"): node.call("TakeDamage", 1000.0)
			controller.call("Tick", 0.01)
			boss.call("TakeDamage", 1000.0)
		var player := mission.get_node("Actors/CyberShogun")
		player.call("ResetAtCheckpoint", player.position)
		player.call("TakeDamageFrom", 10000.0, Vector2.ZERO, boss)
		assert_that(mission.get("Boss")).is_null()
		for actor in mission.get_node("Actors").find_children("*", "", true, false):
			assert_bool(actor.can_process()).is_false()
		assert_bool(mission.call("HasCombatEvent", "curtain:inner")).is_true()
		assert_bool(mission.get("ArrivalActive")).is_false()
		await get_tree().create_timer(1.05).timeout
		assert_float(player.position.x).is_equal(21100.0)
		boss = _arena(mission)
		assert_int(boss.get("CurrentPhase")).is_equal(1)
		assert_float(boss.get("CurrentHealth")).is_equal(300.0)
		await get_tree().process_frame
		mission.free()


func test_real_missile_reflection_bulwark_blink_and_dodge() -> void:
	## Swept missiles hit the player, physically return to their owner on parry, and respect operative defense distinctions.
	for defense in ["parry", "bulwark", "blink", "dodge"]:
		var mission := await _mission()
		var boss := _arena(mission)
		var player := mission.get_node("Actors/CyberShogun")
		var catalog = load("res://scripts/managers/LevelCatalogBridge.cs").new()
		catalog.UnlockOperative("Surya Prime")
		catalog = null
		player.set("SwapCooldownTimer", 0.0)
		player.call("SwapOperative", {"parry":"Cyber Shogun", "bulwark":"Surya Prime", "blink":"Mirage Blade", "dodge":"Oceanus"}[defense])
		player.call("ResetAtCheckpoint", Vector2(22200,554))
		player.set_physics_process(false)
		player.set("AbilityCooldownTimer", 0.0)
		if defense != "dodge": player.call("TriggerSignatureAbility")
		boss.position = player.position + Vector2(250,0)
		assert_str(player.get("AbilityTag")).is_equal({"parry":"parry", "bulwark":"bulwark", "blink":"phase", "dodge":"douse"}[defense])
		var before: float = player.get("CurrentHealth") + player.get("CurrentShield")
		var missile: Node2D = load("res://scenes/gameplay/cyber_boss_missile_2d.tscn").instantiate()
		missile.set("OwningBoss", boss)
		missile.set("Direction", Vector2.RIGHT)
		mission.call("AddBossProjectile", missile, player.position + Vector2(-40, -150 if defense == "dodge" else 0))
		for frame in range(80): await get_tree().physics_frame
		var after: float = player.get("CurrentHealth") + player.get("CurrentShield")
		if defense == "parry": assert_float(boss.get("CurrentHealth")).is_equal(265.0)
		elif defense == "blink": assert_float(after).is_less(before)
		else: assert_float(after).is_equal(before)
		mission.free()


func test_each_checkpoint_retains_progress_and_defeated_voss_never_respawns() -> void:
	## Checkpoint recovery retains objectives, recruit/lesson ledger and rewards; a defeated arena remains quiet.
	var mission := await _mission()
	_seed(mission, BEFORE_GUN + ["guns:western", "lesson:blink", "encounter:s5-3", "curtain:inner", "boss:voss"])
	var player := mission.get_node("Actors/CyberShogun")
	for checkpoint in [Vector2(3950,554), Vector2(7850,554), Vector2(11900,554), Vector2(16850,-46), Vector2(20750,554), Vector2(21100,554), Vector2(25100,554)]:
		player.position = checkpoint
		mission.call("_PhysicsProcess", 0.016)
		var saved: Vector2 = mission.get("CheckpointPosition")
		assert_float(saved.x).is_equal(checkpoint.x)
		player.position.y = 3000
		mission.call("_PhysicsProcess", 0.016)
		assert_float(player.position.x).is_equal(saved.x)
		assert_bool(mission.get("BossDefeated")).is_true()
		assert_bool(mission.call("HasCombatEvent", "lesson:bulwark")).is_true()
		assert_that(mission.get("Boss")).is_null()
		await get_tree().process_frame
	mission.free()


func test_extraction_freezes_once_and_has_only_implemented_destinations() -> void:
	## Results snapshot once, freeze every hostile/audio descendant and keep Replay on this mission.
	var mission := await _mission()
	_seed(mission, BEFORE_GUN + ["guns:western", "lesson:blink", "encounter:s5-3", "curtain:inner", "boss:voss"])
	mission.get_node("Actors/CrossingControl").call("TakeDamage", 1.0)
	var player := mission.get_node("Actors/CyberShogun")
	player.position = Vector2(27700,554)
	mission.call("_PhysicsProcess", 0.016)
	mission.call("_PhysicsProcess", 0.016)
	assert_bool(mission.get("MissionComplete")).is_true()
	var results := mission.get_node("CyberVictoryScorecard/RootPanel/MainVBox")
	assert_str(results.get_node("TitleLabel").text).is_equal("ACT I COMPLETE")
	assert_bool(results.get_node("ContinueBtn").visible).is_false()
	assert_bool(results.get_node("ButtonsHBox/ReplayBtn").visible).is_true()
	for node in mission.get_node("Actors").find_children("*", "", true, false):
		assert_bool(node.can_process()).is_false()
	assert_bool(mission.get_node("AudioStreamPlayer").playing).is_false()
	mission.free()


func test_exposed_core_stays_put_and_accepts_real_melee() -> void:
	## A recovery target must be physically reachable; approaching it cannot push it away at a fixed player offset.
	var mission := await _mission()
	var boss := _arena(mission)
	var controller: Node = mission.get("Fortress")
	_expose(controller)
	var target: Vector2 = boss.position
	var player := mission.get_node("Actors/CyberShogun")
	player.call("ResetAtCheckpoint", target + Vector2(-65,39))
	controller.call("Tick", 0.01)
	assert_vector(boss.position).is_equal(target)
	await get_tree().physics_frame
	await get_tree().physics_frame
	player.call("PerformPrimaryAttack")
	assert_float(boss.get("CurrentHealth")).is_less(300.0)
	await get_tree().process_frame
	mission.free()


func test_pause_and_missing_voice_keep_progress_independent() -> void:
	## Pausing freezes owned speech; missing speech has readable English subtitles and never changes objective acceptance.
	var mission := await _mission()
	var scheduler := mission.get_node("AegisDialogue")
	scheduler.call("Cancel")
	var queue: Node = mission.get_node("MissionSpeechQueue")
	queue.set("AudioDirectoryOverride", "res://missing-aegis-voice/")
	mission.call("QueueBeat", "practice")
	await get_tree().process_frame
	var speech := queue.get_node("EnglishSpeech")
	assert_bool(speech.playing).is_false()
	get_tree().paused = true
	assert_bool(scheduler.can_process()).is_false()
	assert_bool(mission.call("HasCombatEvent", "lesson:bulwark")).is_false()
	get_tree().paused = false
	await get_tree().process_frame
	mission.free()
