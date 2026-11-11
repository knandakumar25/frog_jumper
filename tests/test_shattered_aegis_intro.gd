class_name TestShatteredAegisIntro
extends GdUnitTestSuite
const ART := preload("res://tests/support/cutscene_art.gd")  # Cutscene art may await SpriteCook regeneration.
## Arrival freeze, deterministic timeline and input lifecycle tests. Every instantiated node is explicitly freed.

const OVERLAY := "res://scripts/ui/ShatteredAegisIntro.cs"
const LEVEL := "res://scenes/gameplay/level4.tscn"


func test_arrival_freezes_complete_mission_and_restores_once() -> void:
	if not ART.available("res://scripts/ui/ShatteredAegisStage.cs", self):
		return  # Playback needs art awaiting SpriteCook regeneration (docs/ART_REGENERATION.md).
	## A fresh mission retains its spawn, score, obstacles and five earned operatives throughout playback.
	var mission: Node = scene_runner(LEVEL).scene()
	var overlay := mission.get_node("ShatteredAegisIntro")
	overlay.set_process(false)
	var player := mission.get_node("Actors/CyberShogun")
	var position: Vector2 = player.position
	var health: float = player.get("CurrentHealth")
	var points: int = mission.get_node("GameManager").get("Points")
	var catalog = load("res://scripts/managers/LevelCatalogBridge.cs").new()
	assert_int(catalog.UnlockedCount()).is_equal(5)
	catalog = null
	assert_vector(position).is_equal(Vector2(200, 554))
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
	assert_int(mission.get("MemoryFragmentsFound")).is_equal(0)
	assert_bool(mission.get("BossDefeated")).is_false()
	assert_that(mission.get("Boss")).is_null()
	assert_bool(mission.get_node("Actors/SuryaJoins").get("Triggered")).is_false()
	assert_bool(mission.get_node("PalaceGeometry/BorderWall").get("IsOpen")).is_false()
	mission.get_node("Actors/SuryaJoins").call("Trigger")
	assert_bool(mission.get_node("Actors/SuryaJoins").get("Triggered")).is_false()
	assert_bool(mission.get("MissionComplete")).is_false()
	var jump := InputEventKey.new()
	jump.pressed = true
	jump.keycode = KEY_SPACE
	jump.physical_keycode = KEY_SPACE
	Input.action_press("jump")
	overlay.call("_Input", jump)
	overlay.call("Finish")
	assert_bool(mission.get("ArrivalActive")).is_true()
	assert_bool(Input.is_action_pressed("jump")).is_false()
	await get_tree().process_frame
	assert_bool(mission.get("ArrivalActive")).is_false()
	assert_bool(player.can_process()).is_true()
	assert_bool(mission.get_node("MissionHUD").visible).is_true()
	assert_bool(mission.get_node("CyberCombatHud").visible).is_true()
	assert_bool(mission.get_node("CanvasLayer/PauseMenu").can_process()).is_true()
	assert_bool(mission.get_node("AudioStreamPlayer").stream_paused).is_false()
	assert_bool(mission.get_node("AudioStreamPlayer").playing).is_true()
	# The cinematic must not consume any of the existing opening dialogue's eleven-second display window.
	var dialogue: Label
	for label in mission.find_children("*", "Label", true, false):
		if label.text.begins_with("AXIOM-7: Reach Surya"):
			dialogue = label
	assert_that(dialogue).is_not_null()
	mission.call("_Process", 10.8)
	assert_bool(dialogue.get_parent().visible).is_true()
	mission.call("_Process", 0.3)
	assert_bool(dialogue.get_parent().visible).is_false()
	mission.free()


func test_teardown_restores_mission_and_recovery_does_not_replay() -> void:
	## Removing an overlay unfreezes its retained mission; a fall recovers without constructing another arrival.
	var mission: Node = scene_runner(LEVEL).scene()
	mission.get_node("ShatteredAegisIntro").free()
	await get_tree().process_frame
	assert_bool(mission.get("ArrivalActive")).is_false()
	var player := mission.get_node("Actors/CyberShogun")
	player.position.y = 3000
	for _frame in range(3):
		await get_tree().physics_frame
	assert_bool(mission.has_node("ShatteredAegisIntro")).is_false()
	assert_float(player.position.y).is_less(1000.0)
	mission.free()


func test_snapshot_preserves_nondefault_hud_audio_and_process_modes() -> void:
	## Exact restoration must preserve intentionally hidden, disabled and prepaused nodes rather than enable everything.
	var mission: Node = load("res://scripts/testing/AegisArrivalSnapshotProbe.cs").new()
	add_child(mission)
	var overlay := mission.get_node("ShatteredAegisIntro")
	assert_bool(mission.get_node("ExplicitAlways").can_process()).is_false()
	overlay.call("Finish")
	await get_tree().process_frame
	assert_bool(mission.get_node("MissionHUD").visible).is_false()
	assert_bool(mission.get_node("CyberCombatHud").visible).is_true()
	assert_bool(mission.get_node("AudioStreamPlayer").stream_paused).is_true()
	assert_int(mission.get_node("ExplicitAlways").process_mode).is_equal(Node.PROCESS_MODE_ALWAYS)
	assert_int(mission.get_node("ExplicitAlways/ExplicitDisabled").process_mode).is_equal(Node.PROCESS_MODE_DISABLED)
	assert_int(mission.get_node("ExplicitPausable").process_mode).is_equal(Node.PROCESS_MODE_PAUSABLE)
	mission.free()


func test_early_middle_late_and_natural_finish_emit_once() -> void:
	## Seeking is deterministic; missing speech still allows every completion path and stops ambience before gameplay.
	for seconds in [0.1, 60.0, 119.8, 120.0]:
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
		if seconds == 120.0:
			overlay.call("_Process", 0.1)
		elif seconds < 1:
			var key := InputEventKey.new()
			key.pressed = true
			key.keycode = KEY_ESCAPE
			overlay.call("_Input", key)
		elif seconds < 50:
			var start := InputEventJoypadButton.new()
			start.pressed = true
			start.button_index = JOY_BUTTON_START
			overlay.call("_Input", start)
		else:
			overlay.get_node("UI/Skip").pressed.emit()
		overlay.call("Finish")
		assert_int(count[0]).is_equal(0)
		assert_bool(overlay.get_node("Atmosphere").playing).is_false()
		await get_tree().process_frame
		assert_int(count[0]).is_equal(1)
		if is_instance_valid(overlay):
			overlay.free()


func test_missing_art_converges_on_gameplay() -> void:
	## Missing essential artwork must not strand a mission arrival.
	var overlay: Node = load(OVERLAY).new()
	overlay.set("SpeechPath", "")
	overlay.set("ArtPath", "res://missing-breach-art/")
	var count := [0]
	overlay.connect("Finished", func() -> void: count[0] += 1)
	add_child(overlay)
	await get_tree().process_frame
	assert_int(count[0]).is_equal(1)
	if is_instance_valid(overlay):
		overlay.free()


func test_pointer_and_touch_skip_only_inside_button() -> void:
	if not ART.available("res://scripts/ui/ShatteredAegisStage.cs", self):
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


func test_all_keyboard_and_controller_skips_consume_once() -> void:
	## Every supported shortcut stops audio synchronously and emits gameplay once after input dispatch.
	for spec in [[true, KEY_ESCAPE], [true, KEY_SPACE], [true, KEY_ENTER], [false, JOY_BUTTON_START], [false, JOY_BUTTON_A]]:
		var overlay: Node = load(OVERLAY).new()
		overlay.set("SpeechPath", "")
		add_child(overlay)
		overlay.set_process(false)
		var count := [0]
		overlay.connect("Finished", func() -> void: count[0] += 1)
		var event: InputEvent
		if spec[0]:
			event = InputEventKey.new()
			event.keycode = spec[1]
		else:
			event = InputEventJoypadButton.new()
			event.button_index = spec[1]
		event.pressed = true
		overlay.call("_Input", event)
		overlay.call("Finish")
		assert_int(count[0]).is_equal(0)
		assert_bool(overlay.get_node("Atmosphere").playing).is_false()
		await get_tree().process_frame
		assert_int(count[0]).is_equal(1)
		if is_instance_valid(overlay):
			overlay.free()


func test_teardown_does_not_emit_gameplay() -> void:
	## Removing an active overlay stops owned audio without delivering a late navigation callback.
	var overlay: Node = load(OVERLAY).new()
	var count := [0]
	overlay.connect("Finished", func() -> void: count[0] += 1)
	add_child(overlay)
	overlay.free()
	await get_tree().process_frame
	assert_int(count[0]).is_equal(0)


func test_teardown_restores_existing_cursor_and_prompt_state() -> void:
	## A scene that already hid Global's prompt keeps it hidden after overlay teardown.
	var global := get_node("/root/Global")
	# Headless Global omits its visual prompt: provide an owned representative canvas for this test.
	var original: Variant = global.get("_prompt_label")
	var prompt := CanvasLayer.new()
	var label := Label.new()
	prompt.add_child(label)
	add_child(prompt)
	global.set("_prompt_label", label)
	var previous_mouse := Input.mouse_mode
	global.call("set_control_prompt_visible", false)
	Input.mouse_mode = Input.MOUSE_MODE_HIDDEN
	# Headless DisplayServer may ignore cursor setters; compare the state the overlay can actually capture.
	var captured_mouse := Input.mouse_mode
	var overlay: Node = load(OVERLAY).new()
	add_child(overlay)
	assert_int(Input.mouse_mode).is_equal(Input.MOUSE_MODE_VISIBLE)
	overlay.free()
	assert_bool(prompt.visible).is_false()
	assert_int(Input.mouse_mode).is_equal(captured_mouse)
	global.set("_prompt_label", original)
	prompt.free()
	Input.mouse_mode = previous_mouse


func test_every_shot_preserves_roster_active_operative_and_locked_gameplay() -> void:
	if not ART.available("res://scripts/ui/ShatteredAegisStage.cs", self):
		return  # Playback needs art awaiting SpriteCook regeneration (docs/ART_REGENERATION.md).
	## Deterministic visual seeking cannot unlock Surya, alter gates, spend health/score or create a gameplay boss.
	var mission: Node = scene_runner(LEVEL).scene()
	var overlay := mission.get_node("ShatteredAegisIntro")
	overlay.set_process(false)
	var player := mission.get_node("Actors/CyberShogun")
	var catalog = load("res://scripts/managers/LevelCatalogBridge.cs").new()
	var active: String = catalog.ActiveOperative()
	var position: Vector2 = player.position
	var health: float = player.get("CurrentHealth")
	var score: int = mission.get_node("GameManager").get("Points")
	for second in [5,15,24,34,44,54,64,74,84,94,104,113,118]:
		overlay.call("Seek", float(second))
		await get_tree().physics_frame
		assert_vector(player.position).is_equal(position)
		assert_float(player.get("CurrentHealth")).is_equal(health)
		assert_int(mission.get_node("GameManager").get("Points")).is_equal(score)
		assert_int(catalog.UnlockedCount()).is_equal(5)
		assert_str(catalog.ActiveOperative()).is_equal(active)
		assert_bool(catalog.IsOperativeUnlocked("Surya Prime")).is_false()
		assert_that(mission.get("Boss")).is_null()
		assert_bool(mission.get_node("PalaceGeometry/BorderWall").get("IsOpen")).is_false()
		assert_bool(mission.get_node("Actors/BulwarkPractice").get("Offline")).is_false()
	# Even an external physics invocation in the arena must obey the mission trigger guard.
	player.position = Vector2(22000,540)
	mission.call("_PhysicsProcess", 0.016)
	assert_that(mission.get("Boss")).is_null()
	player.position = position
	overlay.call("Finish")
	await get_tree().process_frame
	for event in ["encounter:s1-3", "interlock:approach", "encounter:s2-1", "encounter:s2-2"]:
		mission.call("CompleteCombatEvent", event)
	player.position = Vector2(6400,554)
	for frame in range(2):
		await get_tree().physics_frame
	assert_bool(mission.get_node("Actors/SuryaJoins").get("Triggered")).is_true()
	assert_bool(catalog.IsOperativeUnlocked("Surya Prime")).is_true()
	assert_that(mission.get("Boss")).is_null()
	catalog = null
	mission.free()
