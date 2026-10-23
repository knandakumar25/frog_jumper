class_name TestSignatureAbilities
extends GdUnitTestSuite

## Each operative's signature ability clears exactly its own gate (and only that one), and the timed
## defensive windows (parry, phase, bulwark) change what damage is taken.

const OPERATIVE := preload("res://scenes/gameplay/palace_operative.tscn")
var catalog = load("res://scripts/managers/LevelCatalogBridge.cs").new()

## Builds a world holding one operative, with the full squad unlocked, swapped to `operative`.
func _spawn(operative: String) -> Node2D:
	catalog.PrepareMission(1)
	for name in ["Oceanus", "Apex Titan", "Aura Phoenicia", "Mirage Blade", "Surya Prime"]:
		catalog.UnlockOperative(name)
	var player: Node2D = auto_free(OPERATIVE.instantiate())
	add_child(player)
	await get_tree().process_frame
	if operative != "Cyber Shogun":
		assert_bool(player.call("SwapOperative", operative)).is_true()
	return player

func _gate(kind: int, at: Vector2) -> Node2D:
	var gate: Node2D = auto_free(load("res://scripts/gameplay/AbilityGate.cs").new())
	gate.set("Kind", kind)
	gate.position = at
	add_child(gate)
	return gate

## GateKind enum: 0 FireVent, 1 RubbleWall, 2 SonicLock, 3 HeatVent.
func test_each_operative_reports_its_own_ability_tag() -> void:
	var expected := {"Cyber Shogun": "parry", "Oceanus": "douse", "Apex Titan": "slam",
		"Aura Phoenicia": "resonate", "Mirage Blade": "phase", "Surya Prime": "bulwark"}
	for operative in expected:
		var player := await _spawn(operative)
		assert_str(player.get("AbilityTag")).is_equal(expected[operative])

func test_titan_slam_breaks_rubble_but_shogun_cannot() -> void:
	var shogun := await _spawn("Cyber Shogun")
	var rubble := _gate(1, shogun.global_position + Vector2(120, 40))
	shogun.call("TriggerSignatureAbility")
	assert_bool(rubble.get("Cleared")).is_false()
	var titan := await _spawn("Apex Titan")
	titan.global_position = shogun.global_position
	titan.call("TriggerSignatureAbility")
	assert_bool(rubble.get("Cleared")).is_true()

func test_slam_out_of_range_does_nothing() -> void:
	var titan := await _spawn("Apex Titan")
	var rubble := _gate(1, titan.global_position + Vector2(900, 0))
	titan.call("TriggerSignatureAbility")
	assert_bool(rubble.get("Cleared")).is_false()

func test_oceanus_douses_fire_only_when_facing_it() -> void:
	var oceanus := await _spawn("Oceanus")
	oceanus.get_node("Sprite2D").flip_h = true   # facing left, fire is to the right
	var fire := _gate(0, oceanus.global_position + Vector2(300, 40))
	oceanus.call("TriggerSignatureAbility")
	assert_bool(fire.get("Cleared")).is_false()
	var facing := await _spawn("Oceanus")
	facing.global_position = oceanus.global_position
	facing.get_node("Sprite2D").flip_h = false
	facing.call("TriggerSignatureAbility")
	assert_bool(fire.get("Cleared")).is_true()

func test_phoenicia_pulse_opens_sonic_lock() -> void:
	var aura := await _spawn("Aura Phoenicia")
	var door := _gate(2, aura.global_position + Vector2(-200, 40))
	aura.call("TriggerSignatureAbility")
	assert_bool(door.get("Cleared")).is_true()

func test_other_abilities_do_not_open_sonic_lock() -> void:
	for operative in ["Cyber Shogun", "Oceanus", "Apex Titan", "Mirage Blade", "Surya Prime"]:
		var player := await _spawn(operative)
		var door := _gate(2, player.global_position + Vector2(100, 40))
		player.call("TriggerSignatureAbility")
		assert_bool(door.get("Cleared")).is_false()

func test_mirage_phase_ignores_hazards_but_not_ranged_fire() -> void:
	var mirage := await _spawn("Mirage Blade")
	var source: Node2D = auto_free(Node2D.new())
	mirage.call("TriggerSignatureAbility")
	assert_bool(mirage.get("IsPhased")).is_true()
	var before: float = mirage.get("CurrentHealth") + mirage.get("CurrentShield")
	mirage.call("TakeDamage", 40.0, Vector2.ZERO)   # hazard: no source
	assert_float(mirage.get("CurrentHealth") + mirage.get("CurrentShield")).is_equal(before)
	mirage.call("TakeDamageFrom", 40.0, Vector2.ZERO, source)   # ranged: still hurts
	assert_float(mirage.get("CurrentHealth") + mirage.get("CurrentShield")).is_less(before)

func test_surya_bulwark_absorbs_hazards_and_ranged_fire() -> void:
	var surya := await _spawn("Surya Prime")
	var source: Node2D = auto_free(Node2D.new())
	surya.call("TriggerSignatureAbility")
	assert_bool(surya.get("IsBulwarked")).is_true()
	var before: float = surya.get("CurrentHealth") + surya.get("CurrentShield")
	surya.call("TakeDamage", 60.0, Vector2.ZERO)
	surya.call("TakeDamageFrom", 60.0, Vector2.ZERO, source)
	assert_float(surya.get("CurrentHealth") + surya.get("CurrentShield")).is_equal(before)

func test_without_a_window_hazards_hurt() -> void:
	var surya := await _spawn("Surya Prime")
	var before: float = surya.get("CurrentHealth") + surya.get("CurrentShield")
	surya.call("TakeDamage", 30.0, Vector2.ZERO)
	assert_float(surya.get("CurrentHealth") + surya.get("CurrentShield")).is_less(before)

func test_shogun_parry_reflects_ranged_attack() -> void:
	var shogun := await _spawn("Cyber Shogun")
	var attacker: Node2D = auto_free(load("res://scripts/gameplay/PalaceObjective.cs").new())
	attacker.set("Kind", "Beacon")
	add_child(attacker)
	var deflected := [0]
	shogun.connect("ParryDeflected", func(_source): deflected[0] += 1)
	shogun.call("TriggerSignatureAbility")
	assert_bool(shogun.get("IsParrying")).is_true()
	var before: float = shogun.get("CurrentHealth") + shogun.get("CurrentShield")
	shogun.call("TakeDamageFrom", 40.0, Vector2.ZERO, attacker)
	assert_float(shogun.get("CurrentHealth") + shogun.get("CurrentShield")).is_equal(before)
	assert_int(deflected[0]).is_equal(1)
	assert_float(attacker.get("CurrentHealth")).is_less(210.0)

func test_ability_cooldown_blocks_immediate_reuse() -> void:
	var titan := await _spawn("Apex Titan")
	var uses := [0]
	titan.connect("AbilityUsed", func(_tag): uses[0] += 1)
	titan.call("TriggerSignatureAbility")
	titan.call("TriggerSignatureAbility")
	assert_int(uses[0]).is_equal(1)
	assert_float(titan.get("AbilityCooldownTimer")).is_greater(0.0)
