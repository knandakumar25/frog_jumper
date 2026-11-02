class_name TestMissionReadability
extends GdUnitTestSuite
## Regression coverage for contextual teaching, objective defense and bypass-safe encounter retirement.

func _mission(level: int) -> Node:
	## Create a fresh scene-local run and use the shipped Skip path for independently tested arrival media.
	var m := scene_runner("res://scenes/gameplay/level%d.tscn" % level).scene()
	if level == 2: m.get_node("ConcreteWildfireIntro").call("Finish")
	if level == 3: m.get_node("NeonRustIntro").call("Finish")
	return m

func _defeat(m: Node, id: String) -> void:
	## Exercise actual finite member damage, completion and reward callbacks.
	var e := m.get_node("Actors/Encounter_" + id)
	e.call("Activate")
	for enemy in e.get_children():
		if not enemy.is_queued_for_deletion() and enemy.has_method("TakeDamage"):
			enemy.call("TakeDamage", 1000.0)

func test_lantern_instruction_survives_lesson_use_and_requires_deflection() -> void:
	## An ability press can finish the optional lesson but contextual teaching persists until a successful parry.
	var m := _mission(1)
	var p := m.get_node("Actors/CyberShogun")
	var hint := m.get_node("MissionHUD/HUDRoot/LanternInstruction") as Label
	p.position = Vector2(2100, 450)
	m.get_node("Actors/ParryLesson").call("Trigger")
	assert_str(m.get("PromptText")).contains("press Q")
	p.call("TriggerSignatureAbility")
	m.call("_Process", 0.1)
	assert_bool(hint.visible).is_true()
	assert_str(hint.text).contains("as a sentry shot lands")
	var sentry := m.get_node("Actors/PurgeSentry02")
	p.call("TakeDamageFrom", 18.0, Vector2.ZERO, sentry)
	m.call("_Process", 0.1)
	assert_str(hint.text).contains("DEFLECTION CONFIRMED")
	m.call("_Process", 3.1)
	assert_bool(hint.visible).is_false()
	p.position.x = 3200
	m.call("_Process", 0.1)
	assert_bool(hint.visible).is_false()
	assert_bool(m.get_node("PalaceGeometry/ArchiveRoofSign").visible).is_true()
	assert_bool(m.get_node("PalaceGeometry/ArchiveDescentSign").visible).is_true()
	m.free()

func test_beacon_state_binding_and_recovery_guidance() -> void:
	## Live charges, exposed wording and the sealed blade remain correct across recovery without a new tutorial gate.
	var m := _mission(1)
	var p := m.get_node("Actors/CyberShogun")
	var beacon := m.get_node("Actors/DemolitionBeacon")
	var blade := m.get_node("Actors/DataBlade")
	p.position = Vector2(7150, -46)
	m.call("_PhysicsProcess", 0.016)
	assert_str(m.get("CurrentObjective")).contains("2 CHARGES")
	assert_str(m.get("CurrentObjective")).not_contains("Destroy")
	assert_bool(blade.get("Sealed")).is_true()
	var keyboard := InputEventKey.new()
	keyboard.physical_keycode = KEY_Q
	m.call("_Input", keyboard)
	m.call("_PhysicsProcess", 0.016)
	assert_str(m.get("CurrentObjective")).contains("PARRY: Q")
	var pad := InputEventJoypadButton.new()
	pad.button_index = JOY_BUTTON_Y
	m.call("_Input", pad)
	m.call("_PhysicsProcess", 0.016)
	assert_str(m.get("CurrentObjective")).contains("PARRY: Y")
	var touch := InputEventScreenTouch.new()
	m.call("_Input", touch)
	m.call("_PhysicsProcess", 0.016)
	assert_str(m.get("CurrentObjective")).contains("PARRY: Ability")
	p.position = Vector2(8300, -106)
	p.call("TriggerSignatureAbility")
	p.call("TakeDamageFrom", 18.0, Vector2.ZERO, m.get_node("Actors/PurgeSentry09"))
	assert_int(beacon.get("ShieldCharges")).is_equal(1)
	p.position.y = 3000
	m.call("_PhysicsProcess", 0.016)
	p.position = Vector2(7150, -46)
	m.call("_PhysicsProcess", 0.016)
	assert_str(m.get("CurrentObjective")).contains("1 CHARGES")
	beacon.call("HitShield")
	m.call("_PhysicsProcess", 0.016)
	assert_str(m.get("CurrentObjective")).contains("Shield down")
	m.call("_Process", 0.016)
	assert_str(blade.get_node("BladeStatus").text).contains("STRIKE THE EXPOSED BEACON")
	beacon.call("TakeDamage", 1000.0)
	assert_bool(blade.get("Sealed")).is_false()
	m.call("_Process", 0.016)
	assert_str(blade.get_node("BladeStatus").text).contains("ACCESSIBLE")
	m.free()

func test_rescue_status_orders_defenders_fire_movement_and_arrival() -> void:
	## Dousing/defense cannot count a rescue early; progress advances only at the authored shelter endpoint.
	var m := _mission(2)
	var group := m.get_node("Actors/Rescue_residential")
	assert_str(group.get("StatusText")).contains("3 LOCAL DEFENDERS")
	_defeat(m, "residential-route")
	assert_str(group.get("StatusText")).contains("OCEANUS")
	var fire := m.get_node("Actors/ResidentialRouteFire")
	fire.call("ReceiveAbility", "douse", fire.position + Vector2(-180,-50), 1.0)
	group.call("_PhysicsProcess", 0.1)
	assert_str(group.get("StatusText")).contains("CIVILIANS MOVING")
	assert_int(m.get("CivilianGroupsEvacuated")).is_equal(0)
	m.get_node("Actors/CyberShogun").position.y = 3000
	m.call("_PhysicsProcess", 0.016)
	assert_str(group.get("StatusText")).contains("CIVILIANS MOVING")
	assert_bool(fire.get("PermanentlySafe")).is_true()
	group.call("_PhysicsProcess", 5.0)
	assert_bool(group.get("Evacuated")).is_false()
	group.call("_PhysicsProcess", 1.0)
	assert_str(group.get("StatusText")).contains("SHELTER REACHED")
	assert_int(m.get("CivilianGroupsEvacuated")).is_equal(1)
	group.call("_PhysicsProcess", 10.0)
	assert_int(m.get("CivilianGroupsEvacuated")).is_equal(1)
	assert_bool(m.call("HasCombatEvent", "encounter:road-parry")).is_false()
	m.free()

func test_final_rescue_highlights_barrier_then_battery_and_spire_names_its_defenders() -> void:
	## Each next blocking condition is readable; unrelated formations never appear in the local safety requirement.
	var m := _mission(2)
	var group := m.get_node("Actors/Rescue_extraction")
	_defeat(m, "extraction-guard")
	assert_str(group.get("StatusText")).contains("APEX TITAN")
	var wall := m.get_node("Actors/MembraneRubble")
	wall.call("ReceiveAbility", "slam", wall.position + Vector2(-120,-50), 1.0)
	assert_str(group.get("StatusText")).contains("BATTERY CORE")
	m.call("_PhysicsProcess", 0.016)
	assert_str(m.get_node("Actors/BiometricSpire1/DefenseStatus").text).contains("spire-street")
	assert_str(m.get_node("Actors/BiometricSpire1/DefenseStatus").text).contains("4 LOCAL DEFENDERS")
	m.free()

func test_recruited_abilities_secure_real_rescue_fire_and_titan_barrier() -> void:
	## Production signature abilities reach actual gates; the fire still needs its own defenders and recovery keeps route safety.
	var m := _mission(2)
	await get_tree().process_frame
	var p := m.get_node("Actors/CyberShogun")
	m.get_node("Actors/OceanusJoins").call("Trigger")
	p.call("SwapOperative", "Oceanus")
	var fire := m.get_node("Actors/ResidentialRouteFire")
	p.call("ResetAtCheckpoint", fire.position + Vector2(-180,-46))
	p.call("TriggerSignatureAbility")
	assert_bool(fire.get("Cleared")).is_true()
	var group := m.get_node("Actors/Rescue_residential")
	group.call("_PhysicsProcess", 0.1)
	assert_bool(group.get("RouteSecured")).is_false()
	_defeat(m, "residential-route")
	group.call("_PhysicsProcess", 0.1)
	assert_bool(group.get("RouteSecured")).is_true()
	p.position.y = 3000
	m.call("_PhysicsProcess", 0.016)
	assert_bool(fire.get("PermanentlySafe")).is_true()
	m.get_node("Actors/TitanJoins").call("Trigger")
	# Wait on simulation state: under heavy rendering, a wall-clock timer can outpace capped physics ticks.
	for frame in range(120):
		if float(p.get("SwapCooldownTimer")) <= 0.0: break
		await get_tree().physics_frame
	assert_float(p.get("SwapCooldownTimer")).is_equal(0.0)
	assert_bool(p.call("SwapOperative", "Apex Titan")).is_true()
	var wall := m.get_node("Actors/TitanPracticeRubble")
	p.call("ResetAtCheckpoint", wall.position + Vector2(-180,-46))
	# Cooldown behavior is independently covered; isolate the newly recruited operative's target acceptance here.
	p.set("AbilityCooldownTimer", 0.0)
	p.call("TriggerSignatureAbility")
	assert_bool(wall.get("Cleared")).is_true()
	m.free()

func test_retirement_releases_budget_and_reactivation_cannot_farm_rewards() -> void:
	## Both opted-in missions retire without completion and allow fresh nearby groups, retaining per-slot reward ownership.
	for level in [2, 3]:
		var m := _mission(level)
		await get_tree().process_frame
		var p := m.get_node("Actors/CyberShogun")
		var id := "road-parry" if level == 2 else "s1-1"
		var e := m.get_node("Actors/Encounter_" + id)
		p.position = Vector2(800, 554)
		e.call("Activate")
		e.get_child(0).call("TakeDamage", 1000.0)
		var score: int = m.get_node("GameManager").get("Points")
		p.position.x = 8000
		e.call("_PhysicsProcess", 0.016)
		assert_bool(e.get("Active")).is_false()
		assert_bool(e.get("Completed")).is_false()
		assert_int(m.get_node("GameManager").get("Points")).is_equal(score)
		assert_bool(m.call("CanActivateEncounter", 4)).is_true()
		var next := m.get_node("Actors/Encounter_residential-entry" if level == 2 else "Actors/Encounter_s2-1")
		next.call("Activate")
		assert_bool(next.get("Active")).is_true()
		await get_tree().process_frame
		p.position = Vector2(800, 554)
		next.call("RetireIfRemote")
		e.call("_PhysicsProcess", 0.016)
		assert_bool(e.get("Active")).is_true()
		var living := e.get_children().filter(func(n: Node) -> bool: return not n.is_queued_for_deletion())
		living[0].call("TakeDamage", 1000.0)
		assert_int(m.get_node("GameManager").get("Points")).is_equal(score)
		m.free()

func test_neon_optional_seals_absent_and_all_checkpoints_use_objective_milestones() -> void:
	## Milestones unlock each shrine even when all thirty patrol ledgers remain incomplete.
	var m := _mission(3)
	await get_tree().process_frame
	var p := m.get_node("Actors/CyberShogun")
	assert_bool(m.has_node("PalaceGeometry/CombatSeal_s1-1")).is_false()
	assert_bool(m.has_node("PalaceGeometry/HarmonicAccessInterlock")).is_false()
	assert_bool(m.has_node("PalaceGeometry/CombatSeal_s7-1")).is_true()
	assert_bool(m.has_node("PalaceGeometry/PracticeInterlock")).is_true()
	var shrines := ["ShrineFreight", "ShrineRunoff", "ShrineHarmonic", "ShrineWest", "ShrineTower", "ShrineSummit", "ShrineEscape"]
	var events := ["barrier:insertion", "shipment:freight", "shipment:runoff", "lesson:aura", "uplink:west", "uplink:east", "lesson:mirage"]
	for name in shrines:
		var shrine := m.get_node("Actors/" + name)
		p.position = shrine.position + Vector2(0,-46)
		m.call("_PhysicsProcess", 0.016)
		assert_bool(shrine.get("Activated")).is_false()
	# In route order, accept exact prerequisites and test each new milestone immediately.
	var route := ["barrier:insertion", "encounter:s2-4", "shipment:freight", "encounter:s3-3", "shipment:runoff", "recruit:aura", "lesson:aura", "encounter:s5-4", "shipment:harmonic", "uplink:west", "encounter:s6-5", "uplink:east", "encounter:s7-1", "encounter:s7-2", "coupler:west", "encounter:s7-3", "coupler:east", "encounter:s7-4", "encounter:s7-5", "relay:central", "recruit:mirage", "lesson:mirage"]
	for id in route:
		m.call("CompleteCombatEvent", id)
		if id in events:
			var shrine := m.get_node("Actors/" + shrines[events.find(id)])
			p.position = shrine.position + Vector2(0,-46)
			m.call("_PhysicsProcess", 0.016)
			assert_bool(shrine.get("Activated")).is_true()
	assert_bool(m.get("ExtractionReady")).is_true()
	assert_bool(m.call("HasCombatEvent", "encounter:s8-4")).is_false()
	m.call("_PhysicsProcess", 0.016)
	assert_str(m.get("CurrentObjective")).contains("Patrol combat is optional")
	m.free()
