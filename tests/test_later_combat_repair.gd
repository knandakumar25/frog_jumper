class_name TestLaterCombatRepair
extends GdUnitTestSuite
## Real physics queries and signature actions supplement the older ledger-injection diagnostics.

const BOSSES := ["IgnisColossus", "AeroCenturion", "SpireSentinel", "NexusOverseer", "BastionPraetor", "VoidCommander"]
const SHOTS := ["FoundrySlagBall2D", "AeroMissile2D", "SpirePlasmaBolt2D", "NexusPulseBolt2D", "PraetorPlasmaBolt2D", "VoidPlasmaSlash2D"]


func _combat_pair(boss_name: String) -> Dictionary:
	## An isolated space freezes movement, but retains the actual operative attack area and boss hurtbox.
	var holder := Node2D.new()
	add_child(holder)
	var player: Node2D = load("res://scenes/gameplay/palace_operative.tscn").instantiate()
	holder.add_child(player)
	player.call("ApplyOperativeProfile", "Cyber Shogun")
	player.call("ResetAtCheckpoint", Vector2(100, 100))
	player.set_physics_process(false)
	var boss: Node2D = load("res://scripts/actors/%sBoss2D.cs" % boss_name).new()
	boss.set("Target", player)
	boss.position = Vector2(170, 100)
	holder.add_child(boss)
	boss.set_physics_process(false)
	await get_tree().physics_frame
	await get_tree().physics_frame
	return {"holder": holder, "player": player, "boss": boss}


func test_actual_melee_contacts_every_later_boss_hurtbox() -> void:
	## No direct boss damage or milestone injection is used as evidence for contact.
	for name in BOSSES:
		var pair := await _combat_pair(name)
		var before: float = pair.boss.get("CurrentHealth")
		assert_int(pair.boss.collision_layer).is_equal(2)
		pair.player.call("PerformPrimaryAttack")
		assert_float(pair.boss.get("CurrentHealth")).is_less(before)
		pair.holder.free()
		await get_tree().process_frame


func test_actual_parry_preserves_projectile_and_returns_to_owning_boss() -> void:
	## A real hostile ray reaches Kaito during his parry window, then the same projectile reaches its boss.
	for index in BOSSES.size():
		var pair := await _combat_pair(BOSSES[index])
		pair.boss.position = Vector2(390, 100)
		var before: float = pair.boss.get("CurrentHealth")
		var player_before: float = pair.player.get("CurrentHealth") + pair.player.get("CurrentShield")
		var reflected := []
		pair.player.connect("ParryDeflected", func(source): reflected.append(source.get("Reflected")))
		var projectile: Node2D = load("res://scripts/actors/%s.cs" % SHOTS[index]).new()
		projectile.set("OwningBoss", pair.boss)
		projectile.set("Direction", Vector2.LEFT)
		projectile.position = Vector2(160, 100)
		pair.holder.add_child(projectile)
		pair.player.call("TriggerSignatureAbility")
		for frame in 100:
			await get_tree().physics_frame
			if pair.boss.get("CurrentHealth") < before:
				break
		assert_array(reflected).is_equal([true])
		assert_float(pair.boss.get("CurrentHealth")).is_less(before)
		assert_float(pair.player.get("CurrentHealth") + pair.player.get("CurrentShield")).is_equal(player_before)
		pair.holder.free()
		await get_tree().process_frame


func _mission(number: int) -> Node:
	## Diagnostic scene setup only; gameplay evidence is asserted through real actions below.
	var bridge = load("res://scripts/managers/LevelCatalogBridge.cs").new()
	bridge.PrepareMission(number)
	bridge = null
	var mission: Node = load("res://scenes/gameplay/level%d.tscn" % number).instantiate()
	add_child(mission)
	return mission


func test_defender_clearance_cannot_automatically_award_an_ability() -> void:
	## Initial combat events are diagnostic setup; neither elapsed time nor an unrelated signature opens the coolant gate.
	var mission := _mission(12)
	mission.call("CompleteCombatEvent", "encounter:ingress1")
	mission.call("CompleteCombatEvent", "encounter:ingress2")
	mission.call("CommitObjective", "ingress_secured")
	await get_tree().physics_frame
	assert_bool(mission.call("HasActionEvidence", "siphon_coolant_doused")).is_false()
	var player: Node2D = mission.get("Player")
	player.call("ResetAtCheckpoint", Vector2(4500, 554))
	player.call("ApplyOperativeProfile", "Cyber Shogun")
	player.call("TriggerSignatureAbility")
	assert_bool(mission.get_node("Actors/PowerSiphonCoolantGate").get("Cleared")).is_false()
	assert_bool(mission.call("HasActionEvidence", "siphon_coolant_doused")).is_false()
	# Recovery clears abandoned ability windows, then Oceanus performs the actual action.
	player.call("ResetAtCheckpoint", Vector2(4500, 554))
	player.call("ApplyOperativeProfile", "Oceanus")
	player.call("TriggerSignatureAbility")
	assert_bool(mission.get_node("Actors/PowerSiphonCoolantGate").get("Cleared")).is_true()
	assert_bool(mission.call("HasActionEvidence", "siphon_coolant_doused")).is_true()
	mission.free()
	await get_tree().process_frame
	collect_orphan_node_details()


func test_phase_boundary_requires_spatial_crossing() -> void:
	## Diagnostic ledger setup reaches the laser stage; Mirage's actual blink supplies the spatial evidence.
	var mission := _mission(12)
	for id in ["ingress1", "ingress2"]:
		mission.call("CompleteCombatEvent", "encounter:" + id)
	for id in ["ingress_secured", "siphon_coolant_doused", "siphon_clamp_shattered"]:
		mission.call("CommitObjective", id)
	for id in ["siphon1", "siphon2", "siphon3"]:
		mission.call("CompleteCombatEvent", "encounter:" + id)
	mission.call("CommitObjective", "siphon_grid_neutralized")
	var player: Node2D = mission.get("Player")
	player.call("ResetAtCheckpoint", Vector2(11690, 554))
	player.call("ApplyOperativeProfile", "Mirage Blade")
	await get_tree().physics_frame
	await get_tree().physics_frame
	player.call("TriggerSignatureAbility")
	await get_tree().physics_frame
	await get_tree().physics_frame
	assert_bool(mission.call("HasActionEvidence", "laser_curtain_phased")).is_true()
	mission.free()


func test_code_built_lasers_have_real_beams_and_collision() -> void:
	## A code-created field must not silently become a harmless empty Node2D.
	var mission := _mission(12)
	var laser: Node = mission.get_node("Actors/VoidBarrierLaserGrid")
	assert_that(laser.get_node("HazardArea/CollisionShape2D").shape).is_not_null()
	assert_int(laser.get_node("BeamLine").points.size()).is_equal(2)
	assert_bool(laser.get("AlwaysActive")).is_true()
	mission.free()


func test_surya_must_walk_the_complete_heat_footprint_with_protection() -> void:
	## Prior stages are diagnostic setup; the evidence under test comes from actual rightward physics movement.
	var mission := _mission(12)
	_diagnostic_arena_setup(mission)
	var player: Node2D = mission.get("Player")
	player.call("ResetAtCheckpoint", Vector2(14590, 554))
	player.call("ApplyOperativeProfile", "Surya Prime")
	for frame in 3:
		await get_tree().physics_frame
	player.call("TriggerSignatureAbility")
	assert_bool(mission.call("HasActionEvidence", "cosmic_vent_absorbed")).is_false()
	Input.action_press("right")
	for frame in 82:
		await get_tree().physics_frame
	Input.action_release("right")
	assert_float(player.position.x).is_greater(14984.0)
	assert_bool(mission.call("HasActionEvidence", "cosmic_vent_absorbed")).is_true()
	mission.free()
	await get_tree().process_frame


func test_gene_pod_accepts_the_shipped_keyboard_and_controller_attack_binding_once() -> void:
	## Diagnostic defender setup is separate from the real context action, proximity and one-shot checks.
	for controller in [false, true]:
		var mission := _mission(6)
		var player: Node2D = mission.get("Player")
		player.call("ResetAtCheckpoint", Vector2(4900, 554))
		var pod: Node = mission.get_node("Actors/Pod1")
		var before: int = mission.get_node("GameManager").get("Points")
		var action: InputEvent
		for binding in InputMap.action_get_events("attack"):
			if (controller and binding is InputEventJoypadButton) or (not controller and binding is InputEventKey):
				action = binding.duplicate()
				action.pressed = true
				break
		assert_that(action).is_not_null()
		pod.call("_Input", action)
		assert_int(mission.get_node("GameManager").get("Points")).is_equal(before)
		for id in ["sump1", "sump2"]:
			mission.call("CompleteCombatEvent", "encounter:" + id)
		mission.call("CommitObjective", "sump_breach")
		mission.call("CompleteCombatEvent", "encounter:incubate1")
		before = mission.get_node("GameManager").get("Points")
		pod.call("_Input", action)
		assert_int(mission.get_node("GameManager").get("Points")).is_equal(before + 1)
		pod.call("_Input", action)
		assert_int(mission.get_node("GameManager").get("Points")).is_equal(before + 1)
		mission.free()


func _diagnostic_arena_setup(mission: Node) -> void:
	## Seeds only pre-arena ledger state to isolate phase/support/recovery behavior; this is not a traversal claim.
	var stages := ["ingress_secured", "siphon_coolant_doused", "siphon_clamp_shattered", "siphon_grid_neutralized", "laser_curtain_phased", "cosmic_vent_absorbed", "barrier_corridor_cleared", "harmonic_lock_resonated", "cathedral_vault_scaled"]
	var formations := ["ingress1", "ingress2", "siphon1", "siphon2", "siphon3", "barrier1", "barrier2", "barrier3", "vault1", "vault2"]
	for stage in stages:
		for formation in formations:
			mission.call("CompleteCombatEvent", "encounter:" + formation)
		assert_bool(mission.call("CommitObjective", stage)).is_true()


func _clear_support(mission: Node, id: String) -> void:
	## Uses real defender damage and death callbacks, without injecting a formation-completion ledger event.
	var encounter: Node = mission.get_node("Actors/Encounter_" + id)
	assert_bool(encounter.get("Active")).is_true()
	for defender in encounter.get_children():
		if defender.has_method("TakeDamage"):
			defender.call("TakeDamage", 10000.0)
	await get_tree().physics_frame
	await get_tree().physics_frame
	assert_bool(encounter.get("Completed")).is_true()


func test_phase_hold_sequential_support_checkpoint_retry_and_premature_terminal() -> void:
	## Actual support death callbacks and checkpoint recovery exercise both phase ordering and durable defeat.
	var mission := _mission(12)
	_diagnostic_arena_setup(mission)
	var player: Node2D = mission.get("Player")
	player.call("ResetAtCheckpoint", Vector2(21000, 554))
	for frame in 4:
		await get_tree().physics_frame
	var boss: Node = mission.get_node("Actors/VoidCommanderBoss")
	boss.call("TakeDamage", 10000.0)
	assert_float(boss.get("CurrentHealth")).is_equal(525.0)
	assert_int(boss.get("CurrentPhase")).is_equal(1)
	assert_bool(boss.get("PhaseBlocked")).is_true()
	assert_bool(mission.get_node("Actors/Encounter_boss2").get("Active")).is_false()
	await _clear_support(mission, "boss1")
	for frame in 4:
		await get_tree().physics_frame
	assert_int(boss.get("CurrentPhase")).is_equal(2)
	assert_bool(mission.get_node("Actors/Encounter_boss2").get("Active")).is_true()
	var earned: int = mission.get_node("GameManager").get("Points")
	player.call("ResetAtCheckpoint", Vector2(21000, 554))
	player.call("TakeDamage", 10000.0, Vector2.ZERO)
	await get_tree().create_timer(1.1).timeout
	for frame in 4:
		await get_tree().physics_frame
	boss = mission.get_node("Actors/VoidCommanderBoss")
	assert_float(boss.get("CurrentHealth")).is_equal(750.0)
	assert_int(boss.get("CurrentPhase")).is_equal(1)
	assert_bool(mission.call("HasCombatEvent", "void_commander_phase:1")).is_false()
	await _clear_support(mission, "boss1")
	boss.call("TakeDamage", 10000.0)
	for frame in 4:
		await get_tree().physics_frame
	assert_int(mission.get_node("GameManager").get("Points")).is_equal(earned)
	await _clear_support(mission, "boss2")
	boss.call("TakeDamage", 10000.0)
	for frame in 4:
		await get_tree().physics_frame
	await _clear_support(mission, "boss3")
	boss.call("TakeDamage", 10000.0)
	assert_bool(mission.call("HasCombatEvent", "void_commander_phase:3")).is_true()
	player.call("ResetAtCheckpoint", Vector2(21000, 554))
	player.call("TakeDamage", 10000.0, Vector2.ZERO)
	await get_tree().create_timer(1.1).timeout
	for frame in 4:
		await get_tree().physics_frame
	var faded_boss: Node = mission.get_node_or_null("Actors/VoidCommanderBoss")
	if faded_boss != null:
		# A retained death tween is harmless; recovery must never replace it with a living boss.
		assert_bool(faded_boss.get("IsDying")).is_true()
		assert_float(faded_boss.get("CurrentHealth")).is_equal(0.0)
	player.call("ResetAtCheckpoint", Vector2(27200, 554))
	for frame in 3:
		await get_tree().physics_frame
	var terminal: Node = mission.get_node("Actors/CoreContainmentTerminal")
	assert_bool(terminal.get("Collected")).is_false()
	assert_bool(terminal.visible).is_true()
	player.call("ResetAtCheckpoint", Vector2(26600, 554))
	for frame in 3:
		await get_tree().physics_frame
	await _clear_support(mission, "citadel_remnant1")
	player.call("ResetAtCheckpoint", Vector2(27200, 554))
	for frame in 3:
		await get_tree().physics_frame
	assert_bool(terminal.get("Collected")).is_true()
	mission.free()
