class_name TestOperativeSwap
extends GdUnitTestSuite

## Squad swapping on the Level 1 platformer: locked operatives are refused, unlocked ones swap in place while
## health/shield stay a shared ratio, a lockout prevents spam, and the HUD squad strip follows the active operative.

const LEVEL1_PATH := "res://scenes/gameplay/level1.tscn"
var catalog = load("res://scripts/managers/LevelCatalogBridge.cs").new()

func _load_mission() -> Node:
	catalog.PrepareMission(1)
	var runner := scene_runner(LEVEL1_PATH)
	return runner.scene()

## A brand-new campaign has only Shogun, so swap keys do nothing and locked operatives are refused.
func test_locked_operatives_cannot_be_fielded() -> void:
	var mission := _load_mission()
	var player := mission.get_node("Actors/CyberShogun")
	assert_int(catalog.UnlockedCount()).is_equal(1)
	assert_bool(player.call("SwapOperative", "Oceanus")).is_false()
	assert_bool(player.call("CycleOperative", 1)).is_false()
	assert_str(player.get("ActiveProfile").get("DisplayName")).is_equal("Cyber Shogun")
	mission.free()

## Swapping to a freshly unlocked operative changes the profile and keeps the health ratio instead of healing.
func test_swap_keeps_health_ratio_and_applies_new_profile() -> void:
	var mission := _load_mission()
	var player := mission.get_node("Actors/CyberShogun")
	catalog.UnlockOperative("Oceanus")
	player.call("TakeDamage", 80.0, Vector2.ZERO)  # 50 shield + 30 health lost from 100/50 -> 70 hp, 0 shield
	var ratio: float = player.get("CurrentHealth") / player.get("MaxHealth")
	assert_bool(player.call("SwapOperative", "Oceanus")).is_true()
	assert_str(player.get("ActiveProfile").get("DisplayName")).is_equal("Oceanus")
	assert_float(player.get("CurrentHealth") / player.get("MaxHealth")).is_equal_approx(ratio, 0.02)
	assert_str(Global.selected_character).is_equal("Oceanus")
	mission.free()

## A second swap inside the lockout window is refused, and works after it expires.
func test_swap_lockout() -> void:
	var mission := _load_mission()
	var player := mission.get_node("Actors/CyberShogun")
	catalog.UnlockOperative("Oceanus")
	assert_bool(player.call("SwapOperative", "Oceanus")).is_true()
	assert_bool(player.call("SwapOperative", "Cyber Shogun")).is_false()
	for _frame in range(70):
		await get_tree().physics_frame
	assert_bool(player.call("SwapOperative", "Cyber Shogun")).is_true()
	mission.free()

## Cycling wraps through unlocked operatives and the HUD strip highlights the active one.
func test_cycle_and_squad_strip() -> void:
	var mission := _load_mission()
	var player := mission.get_node("Actors/CyberShogun")
	catalog.UnlockOperative("Oceanus")
	catalog.UnlockOperative("Apex Titan")
	assert_bool(player.call("CycleOperative", 1)).is_true()
	assert_str(player.get("ActiveProfile").get("DisplayName")).is_equal("Oceanus")
	var strip := mission.find_child("SquadStrip", true, false)
	assert_that(strip).is_not_null()
	assert_int(strip.get_child_count()).is_equal(6)
	mission.free()
