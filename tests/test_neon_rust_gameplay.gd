class_name TestNeonRustGameplay
extends GdUnitTestSuite
## Scene integration checks use production callbacks and free every instantiated mission.

func _mission() -> Node:
	## Finish the independently tested arrival before interacting with objectives.
	var m := scene_runner("res://scenes/gameplay/level3.tscn").scene()
	m.get_node("NeonRustIntro").call("Finish")
	return m

func _defeat(m: Node, id: String) -> void:
	## Actual damage callbacks validate finite completion and member reward ownership.
	var encounter := m.get_node("Actors/Encounter_" + id)
	encounter.call("Activate")
	for enemy in encounter.get_children():
		if enemy.has_method("TakeDamage"): enemy.call("TakeDamage", 1000.0)

func _seed_to_relay(m: Node) -> void:
	## Seed prerequisites to isolate relay behavior; full live route verification covers physical objective actions.
	for s in range(1, 8):
		for g in range(1, [3,4,3,2,4,5,5][s-1]+1):
			m.call("CompleteCombatEvent", "encounter:s%d-%d" % [s,g])
	for id in ["barrier:insertion", "shipment:freight", "shipment:runoff", "recruit:aura", "lesson:aura", "shipment:harmonic", "uplink:west", "uplink:east", "coupler:west", "coupler:east"]:
		m.call("CompleteCombatEvent", id)

func test_defended_targets_reject_wrong_and_premature_abilities() -> void:
	## No correct ability can bypass live defenders; arrival rejects direct external ability offers too.
	var m := _mission()
	var shipment := m.get_node("Actors/FreightShipment")
	assert_bool(shipment.call("ReceiveAbility", "slam", shipment.position, 1.0)).is_false()
	await get_tree().process_frame
	assert_bool(shipment.call("ReceiveAbility", "douse", shipment.position, 1.0)).is_false()
	assert_bool(shipment.call("ReceiveAbility", "slam", shipment.position, 1.0)).is_false()
	for g in range(1,5): _defeat(m, "s2-%d" % g)
	assert_bool(shipment.call("ReceiveAbility", "slam", shipment.position + Vector2(-160,-46), 1.0)).is_true()
	assert_bool(shipment.call("ReceiveAbility", "slam", shipment.position, 1.0)).is_false()
	assert_bool(m.call("HasCombatEvent", "shipment:freight")).is_true()
	m.free()

func test_cap_finite_completion_and_duplicate_rewards() -> void:
	## Three living actors prevent a four-member group from spawning; an incomplete retry cannot farm the first slot.
	var m := _mission()
	await get_tree().process_frame
	var first := m.get_node("Actors/Encounter_s1-1")
	var second := m.get_node("Actors/Encounter_s1-2")
	first.call("Activate")
	second.call("Activate")
	assert_bool(second.get("Active")).is_false()
	first.get_child(0).call("TakeDamage", 1000.0)
	var score: int = m.get_node("GameManager").get("Points")
	first.call("ResetIncomplete")
	first.call("Activate")
	first.get_children().filter(func(n): return not n.is_queued_for_deletion())[0].call("TakeDamage", 1000.0)
	assert_int(m.get_node("GameManager").get("Points")).is_equal(score)
	_defeat(m, "s1-1")
	assert_bool(first.get("Completed")).is_true()
	first.call("ResetIncomplete")
	first.call("Activate")
	assert_bool(first.get("Active")).is_false()
	m.free()

func test_recruit_prerequisites() -> void:
	## Aura cannot join early and using Pulse away from the practice lock cannot complete her lesson.
	var m := _mission()
	await get_tree().process_frame
	var aura := m.get_node("Actors/PhoeniciaJoins")
	aura.call("Trigger")
	assert_bool(aura.get("Triggered")).is_false()
	_seed_to_relay(m)
	aura.call("Trigger")
	var mirage := m.get_node("Actors/MirageJoins")
	mirage.call("Trigger")
	assert_bool(mirage.get("Triggered")).is_false()
	m.free()

func test_recovery_retains_objectives_but_restarts_incomplete_encounters() -> void:
	## Repeat fall recovery after each major objective and interrupt both reinforcement groups without losing rewards.
	var m := _mission()
	await get_tree().process_frame
	var p := m.get_node("Actors/CyberShogun")
	_seed_to_relay(m)
	for id in ["shipment:freight", "shipment:runoff", "lesson:aura", "shipment:harmonic", "uplink:west", "uplink:east", "coupler:west", "coupler:east"]:
		p.position = Vector2(200, 3000)
		m.call("_PhysicsProcess", 0.016)
		assert_bool(m.call("HasCombatEvent", id)).is_true()
		assert_float(p.position.y).is_less(1000.0)
		assert_bool(m.get("GameplaySuspended")).is_false()
	assert_bool(m.get("CommsBlackedOut")).is_false()
	m.free()

func test_death_suspends_hazards_and_clears_rounds() -> void:
	## During the respawn delay every hazard and enemy is disabled; processing restores after recovery.
	var m := _mission()
	await get_tree().process_frame
	_defeat(m, "s1-1")
	var e := m.get_node("Actors/Encounter_s1-2")
	e.call("Activate")
	var source := e.get_child(0)
	m.call("LaunchRound", Vector2(200,400), Vector2.RIGHT, source)
	var p := m.get_node("Actors/CyberShogun")
	p.call("TakeDamage", 10000.0, Vector2.ZERO)
	assert_bool(m.get("GameplaySuspended")).is_true()
	assert_bool(m.get_node("Actors/MiragePracticeBeam").can_process()).is_false()
	await get_tree().create_timer(1.1).timeout
	assert_bool(m.get("GameplaySuspended")).is_false()
	assert_bool(m.get_node("Actors/MiragePracticeBeam").can_process()).is_true()
	assert_int(get_tree().get_nodes_in_group("wildfire_rounds").size()).is_equal(0)
	assert_bool(e.get("Active")).is_false()
	m.free()

func test_practice_beam_and_all_landing_metrics() -> void:
	## Safe practice damage, stationary shrines and 110px route stair maximum are structural invariants.
	var m := _mission()
	assert_float(m.get_node("Actors/MiragePracticeBeam").get("Damage")).is_equal(0.0)
	var shrines := 0
	var encounters := 0
	for n in m.get_node("Actors").get_children():
		if n.name.begins_with("Shrine"): shrines += 1
		if n.name.begins_with("Encounter_"): encounters += 1
	assert_int(shrines).is_equal(7)
	assert_int(encounters).is_equal(30)
	m.free()

func test_aura_lesson_requires_opening_the_practice_lock() -> void:
	## An ability press away from the lock leaves the lesson active; opening its actual gate finishes it.
	var m := _mission()
	await get_tree().process_frame
	for s in [2,3]:
		for g in range(1, (4 if s==2 else 3)+1): m.call("CompleteCombatEvent", "encounter:s%d-%d" % [s,g])
	m.call("CompleteCombatEvent", "shipment:freight")
	m.call("CompleteCombatEvent", "shipment:runoff")
	var aura := m.get_node("Actors/PhoeniciaJoins")
	aura.call("Trigger")
	m.call("_PhysicsProcess", 0.016)
	var p := m.get_node("Actors/CyberShogun")
	p.call("SwapOperative", "Aura Phoenicia")
	p.call("TriggerSignatureAbility")
	assert_bool(aura.get("Completed")).is_false()
	p.call("ResetAtCheckpoint", Vector2(19300,554))
	p.set("AbilityCooldownTimer", 0.0)
	p.call("TriggerSignatureAbility")
	assert_bool(m.get_node("Actors/AuraPracticeLock").get("Cleared")).is_true()
	assert_bool(aura.get("Completed")).is_true()
	m.free()

func test_mirage_lesson_requires_actual_crossing_and_recovery_retains_it() -> void:
	## Using Phase elsewhere and walking across the harmless beam fail; a real Blink crossing succeeds.
	var m := _mission()
	await get_tree().process_frame
	_seed_to_relay(m)
	var relay := m.get_node("Actors/CommRelay")
	relay.call("ReceiveAbility", "resonate", relay.position, 1.0)
	var recruit := m.get_node("Actors/MirageJoins")
	recruit.call("Trigger")
	var p := m.get_node("Actors/CyberShogun")
	p.call("SwapOperative", "Mirage Blade")
	m.call("_PhysicsProcess",0.016)
	p.call("TriggerSignatureAbility")
	assert_bool(recruit.get("Completed")).is_false()
	p.call("ResetAtCheckpoint", Vector2(40390,-546))
	m.call("_PhysicsProcess",0.016)
	p.set("AbilityCooldownTimer",0.0)
	p.call("TriggerSignatureAbility")
	m.call("_PhysicsProcess",0.016)
	assert_bool(m.call("HasCombatEvent","lesson:mirage")).is_true()
	assert_bool(recruit.get("Completed")).is_true()
	p.position=Vector2(200,3000)
	m.call("_PhysicsProcess",0.016)
	assert_bool(m.call("HasCombatEvent","lesson:mirage")).is_true()
	assert_bool(m.get("CommsBlackedOut")).is_true()
	m.free()

func test_reinforcement_recovery_restarts_only_incomplete_group() -> void:
	## First reinforcement completion survives repeated recoveries while a partially defeated second group restarts.
	var m := _mission()
	await get_tree().process_frame
	for s in range(1,8):
		for g in range(1,[3,4,3,2,4,5,3][s-1]+1): m.call("CompleteCombatEvent","encounter:s%d-%d" % [s,g])
	for id in ["shipment:freight","shipment:runoff","recruit:aura","lesson:aura","shipment:harmonic","uplink:west","uplink:east","coupler:west","coupler:east"]:
		m.call("CompleteCombatEvent",id)
	_defeat(m,"s7-4")
	var second := m.get_node("Actors/Encounter_s7-5")
	second.call("Activate")
	second.get_child(0).call("TakeDamage",1000.0)
	var score: int = m.get_node("GameManager").get("Points")
	var p := m.get_node("Actors/CyberShogun")
	for attempt in range(2):
		p.position=Vector2(200,3000)
		m.call("_PhysicsProcess",0.016)
		await get_tree().process_frame
	assert_bool(m.call("HasCombatEvent","encounter:s7-4")).is_true()
	assert_bool(m.call("HasCombatEvent","encounter:s7-5")).is_false()
	second.call("Activate")
	second.get_child(0).call("TakeDamage",1000.0)
	assert_int(m.get_node("GameManager").get("Points")).is_equal(score)
	assert_bool(m.get("CommsBlackedOut")).is_false()
	m.free()

func test_aftermath_is_frozen_and_extraction_requires_escape() -> void:
	## Blackout alone cannot complete the scene; accepted extraction runs one frozen epilogue before the shared results.
	var m := _mission()
	await get_tree().process_frame
	_seed_to_relay(m)
	m.call("CompleteCombatEvent","relay:central")
	var p := m.get_node("Actors/CyberShogun")
	p.position=Vector2(47700,554)
	m.call("_PhysicsProcess",0.016)
	assert_bool(m.get("MissionComplete")).is_false()
	for id in ["recruit:mirage","lesson:mirage","encounter:s8-1","encounter:s8-2","encounter:s8-3","encounter:s8-4"]:
		m.call("CompleteCombatEvent",id)
	m.call("_PhysicsProcess",0.016)
	m.call("_PhysicsProcess",0.016)
	assert_bool(m.get("MissionComplete")).is_true()
	assert_bool(p.can_process()).is_false()
	assert_bool(m.get_node("Actors/MiragePracticeBeam").can_process()).is_false()
	assert_bool(m.get_node("MissionHUD").visible).is_false()
	await get_tree().process_frame
	var aftermath := m.get_node("NeonRustAftermath")
	assert_int(m.find_children("NeonRustAftermath", "", true, false).size()).is_equal(1)
	assert_bool(m.get_node("CyberVictoryScorecard/RootPanel").visible).is_false()
	var score: int = m.get_node("GameManager").get("Points")
	m.get_node("GameManager").set("Points", score + 999)
	p.call("SwapOperative", "Oceanus")
	aftermath.call("Finish")
	aftermath.call("Finish")
	await get_tree().process_frame
	var card := m.get_node("CyberVictoryScorecard/RootPanel")
	assert_bool(card.visible).is_true()
	assert_str(card.get_node("MainVBox/StatsGrid/ScoreLabel").text).is_equal("FINAL SCORE: %d PTS" % score)
	assert_str(card.get_node("MainVBox/StatsGrid/OperativeLabel").text).contains("KAITO")
	assert_str(card.get_node("MainVBox/ContinueBtn").text).is_equal("CONTINUE TO LEVEL 4")
	for button in ["ReplayBtn","LevelSelectBtn","MainMenuBtn"]:
		assert_bool(card.get_node("MainVBox/ButtonsHBox/"+button).visible).is_true()
	m.free()
