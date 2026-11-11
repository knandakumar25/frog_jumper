class_name TestConcreteWildfireIntro
extends GdUnitTestSuite
const ART := preload("res://tests/support/cutscene_art.gd")  # Cutscene art may await SpriteCook regeneration.
## Arrival lock integration and independent overlay fallback/input lifecycle tests.

const OVERLAY := "res://scripts/ui/ConcreteWildfireIntro.cs"


func test_initial_freeze_and_exact_restoration() -> void:
	if not ART.available("res://scripts/ui/WildfireStage.cs", self):
		return  # Playback needs art awaiting SpriteCook regeneration (docs/ART_REGENERATION.md).
	## Fresh launch freezes all descendants before physics; repeated finish restores HUD, audio and solo squad once.
	var mission: Node = scene_runner("res://scenes/gameplay/level2.tscn").scene()
	var overlay := mission.get_node("ConcreteWildfireIntro")
	overlay.set_process(false)
	var player := mission.get_node("Actors/CyberShogun")
	var position: Vector2 = player.position
	var health: float = player.get("CurrentHealth")
	var points: int = mission.get_node("GameManager").get("Points")
	assert_bool(mission.get("ArrivalActive")).is_true()
	assert_bool(mission.get_node("MissionHUD").visible).is_false()
	for node in mission.find_children("*", "", true, false):
		if node == overlay or overlay.is_ancestor_of(node):
			continue
		assert_bool(node.can_process()).override_failure_message(str(node.get_path())).is_false()
	for _frame in range(5):
		await get_tree().physics_frame
	assert_vector(player.position).is_equal(position)
	assert_float(player.get("CurrentHealth")).is_equal(health)
	assert_int(mission.get_node("GameManager").get("Points")).is_equal(points)
	assert_int(mission.get("SpiresDisabled")).is_equal(0)
	assert_int(mission.get("MemoryFragmentsFound")).is_equal(0)
	assert_bool(mission.get("MissionComplete")).is_false()
	overlay.call("Finish")
	overlay.call("Finish")
	assert_bool(mission.get("ArrivalActive")).is_false()
	assert_bool(player.can_process()).is_true()
	assert_bool(mission.get_node("CanvasLayer/PauseMenu").can_process()).is_true()
	assert_bool(mission.get_node("MissionHUD").visible).is_true()
	assert_bool(mission.get_node("CyberCombatHud").visible).is_true()
	# Parent process-mode notifications must not overwrite the music's captured pause flag.
	assert_bool(mission.get_node("AudioStreamPlayer").stream_paused).is_false()
	assert_bool(mission.get_node("AudioStreamPlayer").playing).is_true()
	var catalog = load("res://scripts/managers/LevelCatalogBridge.cs").new()
	assert_int(catalog.UnlockedCount()).is_equal(1)
	catalog = null
	mission.free()


func test_early_middle_late_and_natural_finish_emit_once() -> void:
	## Seeking is deterministic; missing speech still allows every completion path and stops ambience before gameplay.
	for seconds in [0.1, 30.0, 59.8, 60.0]:
		var overlay: Node = load(OVERLAY).new()
		overlay.set("SpeechPath", "")
		var count := [0]
		overlay.connect("Finished", func() -> void: count[0] += 1)
		add_child(overlay)
		overlay.set_process(false)
		overlay.call("Seek", seconds)
		var first: String = overlay.get_node("UI/SubtitlePanel/Subtitle").text
		overlay.call("Seek", 5.0)
		overlay.call("Seek", seconds)
		assert_str(overlay.get_node("UI/SubtitlePanel/Subtitle").text).is_equal(first)
		if seconds == 60.0:
			overlay.call("_Process", 0.1)
		elif seconds < 1:
			var key := InputEventKey.new()
			key.pressed = true
			key.keycode = KEY_ESCAPE
			overlay.call("_Input", key)
		elif seconds < 40:
			var start := InputEventJoypadButton.new()
			start.pressed = true
			start.button_index = JOY_BUTTON_START
			overlay.call("_Input", start)
		else:
			overlay.get_node("UI/Skip").pressed.emit()
		overlay.call("Finish")
		assert_int(count[0]).is_equal(1)
		assert_bool(overlay.get_node("Atmosphere").playing).is_false()
		overlay.free()


func test_missing_art_converges_on_gameplay() -> void:
	## Missing essential artwork must not strand a mission arrival.
	var overlay: Node = load(OVERLAY).new()
	overlay.set("SpeechPath", "")
	overlay.set("ArtPath", "res://missing-wildfire-art/")
	var count := [0]
	overlay.connect("Finished", func() -> void: count[0] += 1)
	add_child(overlay)
	await get_tree().process_frame
	assert_int(count[0]).is_equal(1)
	if is_instance_valid(overlay):
		overlay.free()


func test_pointer_and_touch_skip_only_inside_button() -> void:
	if not ART.available("res://scripts/ui/WildfireStage.cs", self):
		return  # Playback needs art awaiting SpriteCook regeneration (docs/ART_REGENERATION.md).
	## Direct pointer/touch input bypasses Global's mouse suppression without making the full screen skippable.
	for use_touch in [true, false]:
		var overlay: Node = load(OVERLAY).new()
		overlay.set("SpeechPath", "")
		add_child(overlay)
		overlay.set_process(false)
		await get_tree().process_frame
		var event: Variant
		if use_touch:
			event = InputEventScreenTouch.new()
		else:
			event = InputEventMouseButton.new()
			event.button_index = MOUSE_BUTTON_LEFT
		event.pressed = true
		event.position = Vector2(-20, -20)
		overlay.call("_Input", event)
		assert_bool(overlay.get("HasFinished")).is_false()
		event.position = overlay.get_node("UI/Skip").get_global_rect().get_center()
		overlay.call("_Input", event)
		assert_bool(overlay.get("HasFinished")).is_true()
		overlay.free()


func test_teardown_and_checkpoint_recovery_do_not_replay() -> void:
	## Recovery uses the constructed scene; teardown with active audio safely releases all owned nodes.
	var mission: Node = scene_runner("res://scenes/gameplay/level2.tscn").scene()
	mission.get_node("ConcreteWildfireIntro").call("Finish")
	await get_tree().process_frame
	var player := mission.get_node("Actors/CyberShogun")
	player.position.y = 3000
	for _frame in range(3):
		await get_tree().physics_frame
	assert_bool(mission.get("ArrivalActive")).is_false()
	assert_that(mission.get_node_or_null("ConcreteWildfireIntro")).is_null()
	assert_float(player.position.y).is_less(1000.0)
	mission.free()
	var overlay: Node = load(OVERLAY).new()
	add_child(overlay)
	overlay.free()


func test_missing_art_restores_mission_through_same_callback() -> void:
	## The fallback signal uses the production restoration callback, preserving the scene and full dialogue timer.
	var mission: Node = scene_runner("res://scenes/gameplay/level2.tscn").scene()
	var original := mission.get_node("ConcreteWildfireIntro")
	var fallback: Node = load(OVERLAY).new()
	fallback.set("ArtPath", "res://absent-arrival/")
	fallback.set("SpeechPath", "")
	fallback.connect("Finished", func() -> void: original.call("Finish"))
	mission.add_child(fallback)
	await get_tree().process_frame
	assert_bool(mission.get("ArrivalActive")).is_false()
	assert_bool(mission.get_node("Actors/CyberShogun").can_process()).is_true()
	assert_bool(mission.get_node("MissionHUD").visible).is_true()
	mission.free()
