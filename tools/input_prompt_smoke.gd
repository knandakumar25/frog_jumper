extends SceneTree
## Isolated runtime verification; run with a .NET Godot console, --windowed and --script.
## Uses preview sessions only, injects viewport input and writes screenshots under ignored reports/.
## This does not substitute for testing a physical controller or native-language visual review.

const OUTPUT := "res://reports/input-prompts-smoke/"
var _failures: Array[String] = []
var _checks: Array[String] = []
var _service: Node
var _scene: Node
var _finished := false


func _initialize() -> void:
	_run.call_deferred()


func _check(condition: bool, description: String) -> void:
	_checks.append(description)
	if not condition:
		_failures.append(description)
		push_error("PROMPT SMOKE: " + description)


func _frames(count := 3) -> void:
	for frame in count:
		await process_frame


func _mount(path: String) -> Node:
	if is_instance_valid(current_scene) and current_scene != _scene:
		current_scene.free()
	if is_instance_valid(_scene):
		_scene.free()
		await _frames()
	paused = false
	var resource := load(path)
	_scene = resource.instantiate() if resource is PackedScene else resource.new()
	root.add_child(_scene)
	current_scene = _scene
	await _frames(5)
	return _scene


func _press(button: int, point: Vector2, pressed := true) -> void:
	var event := InputEventMouseButton.new()
	event.button_index = button
	event.position = point
	event.global_position = point
	event.pressed = pressed
	root.push_input(event, true)
	await _frames(2)


func _click(control: Control) -> void:
	var point := control.get_global_rect().get_center()
	var motion := InputEventMouseMotion.new()
	motion.position = point
	motion.global_position = point
	motion.relative = Vector2(12, 0)
	root.push_input(motion, true)
	await _press(MOUSE_BUTTON_LEFT, point)
	await _press(MOUSE_BUTTON_LEFT, point, false)


func _pad() -> void:
	# An otherwise unbound button selects pad labels through actual scene dispatch.
	await _pad_button(JOY_BUTTON_RIGHT_STICK)


## Routes a complete synthetic pad press/release through the viewport, never a scene callback.
func _pad_button(button: int) -> void:
	var event := InputEventJoypadButton.new()
	event.button_index = button
	event.device = 23
	event.pressed = true
	root.push_input(event, true)
	await _frames()
	event = event.duplicate()
	event.pressed = false
	root.push_input(event, true)
	await _frames()


func _keyboard() -> void:
	var event := InputEventKey.new()
	event.physical_keycode = KEY_F12
	event.keycode = KEY_F12
	event.pressed = true
	root.push_input(event, true)
	await _frames()
	event = event.duplicate()
	event.pressed = false
	root.push_input(event, true)
	await _frames()


func _capture(name: String) -> void:
	await RenderingServer.frame_post_draw
	var image := root.get_texture().get_image()
	_check(image.save_png(OUTPUT + name + ".png") == OK, "capture " + name)


func _visible_label_text(node: Node) -> String:
	var texts: PackedStringArray = []
	for child: Node in node.find_children("*", "Label", true, false):
		if child.is_visible_in_tree():
			texts.append(child.text)
	return "\n".join(texts)


func _run() -> void:
	# Fail closed if a runtime error interrupts this coroutine before writing its report.
	create_timer(180).timeout.connect(func() -> void:
		if not _finished:
			push_error("Input prompt smoke timed out before completion")
			quit(2))
	await _frames()
	_service = root.get_node("InputPromptService")
	_service.call("SetStyle", 1, false)
	root.get_node("SessionManager").set("CurrentSessionId", -1)
	root.mode = Window.MODE_WINDOWED
	root.position = Vector2i(-10000, -10000)
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUTPUT))
	var locale := root.get_node("LocaleService")
	var catalog: RefCounted = load("res://scripts/managers/LevelCatalogBridge.cs").new()

	var home := await _mount("res://scenes/main_menu/game_home.tscn")
	await _keyboard()
	await _press(MOUSE_BUTTON_LEFT, Vector2(900, 600))
	await _press(MOUSE_BUTTON_LEFT, Vector2(900, 600), false)
	await create_timer(0.7).timeout
	_check(is_instance_valid(current_scene) and current_scene.name == "MainMenu", "home left mouse click reaches main menu")
	var menu := await _mount("res://scenes/main_menu/main_menu.tscn")
	await _keyboard()
	await _click(menu.get_node("ButtonContainer/Settings"))
	_check(menu.get_node("SettingsPopup").visible, "real menu Settings mouse click opens popup")
	var dropdown: Control = menu.get_node("SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/LanguageDropdown")
	await _click(dropdown)
	var choices := dropdown.find_children("*", "Button", true, false)
	_check(choices.size() == 17, "custom language dropdown mouse click opens all choices")
	if choices.size() > 1:
		await _click(choices[1])
		_check(int(dropdown.get("SelectedIndex")) == 1, "custom dropdown choice receives mouse click")
	await _click(menu.get_node("SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/ControlsButton"))
	_check(menu.get_node("KeybindingsPopup").visible, "real menu Controls mouse click opens remap popup")
	# Open a real remap row, then change locale while its capture modal remains open.
	locale.call("ApplyLocale", "german")
	await _frames()
	var rows: Node = menu.get_node("KeybindingsPopup/VBoxContainer/ScrollContainer/RowsContainer")
	await _click(rows.get_child(0).get_child(2))
	var remap_prompt: Label = menu.get_node("KeyDetectModal/VBoxContainer/PromptLabel")
	_check(menu.get_node("KeyDetectModal").visible and remap_prompt.text.contains("[" + TranslationServer.translate("Move Left") + "]"), "German remap modal translates action title")
	locale.call("ApplyLocale", "arabic")
	await _frames()
	_check(remap_prompt.text.contains("[" + TranslationServer.translate("Move Left") + "]"), "open remap modal refreshes action title after locale change")
	await _click(menu.get_node("KeyDetectModal/VBoxContainer/ButtonContainer/CancelButton"))
	await _click(menu.get_node("KeybindingsPopup/VBoxContainer/ButtonContainer/CloseKeybindingsButton"))
	_check(not menu.get_node("KeybindingsPopup").visible, "real remap Close mouse click closes popup")
	await _click(menu.get_node("SettingsPopup/VBoxContainer/ButtonContainer/CloseButton"))
	await _click(menu.get_node("ButtonContainer/Settings"))
	await _keyboard()
	await _pad_button(JOY_BUTTON_B)
	_check(not menu.get_node("SettingsPopup").visible and bool(_service.get("UsingController")), "consuming menu pad cancel closes popup and updates active device")
	var instructions: Label = menu.get_node("NavPromptFooter").get_child(0)
	for size: Vector2i in [Vector2i(1920, 1080), Vector2i(2560, 1080)]:
		root.size = size
		for language: String in ["english", "arabic", "german"]:
			locale.call("ApplyLocale", language)
			await _keyboard()
			var keyboard_instructions := instructions.text
			await _capture("menu_%s_%d" % [language, size.x])
			await _pad()
			_check(instructions.text == String(root.get_node("Global").call("get_nav_prompt")) and instructions.text.contains("[ A ]") and instructions.text != keyboard_instructions, "specific menu instruction label refreshes for routed pad input " + language)

	locale.call("ApplyLocale", "english")
	catalog.call("PrepareMission", 1)
	var campaign_one := await _mount("res://scenes/gameplay/level1.tscn")
	await _keyboard()
	_check(_service.get("Context") == "platformer", "current campaign Mission 1 selects platformer context")
	_check(not String(_service.call("GetBindingForDevice", "attack", false)).contains("mouse"), "current campaign Mission 1 keeps mouse combat disabled")
	root.size = Vector2i(1920, 1080)
	await _capture("campaign_mission1_mouse_standard")
	# The current level1 scene attaches PalaceMission. The existing isometric controller
	# remains available as a separate script; inspect it without changing campaign routing.
	root.size = Vector2i(2560, 1080)
	var mission := await _mount("res://scripts/gameplay/IsometricMission.cs")
	await _keyboard()
	_check(_service.get("Context") == "isometric", "separate isometric feature selects its context")
	_check(_visible_label_text(mission).contains("Left mouse"), "isometric feature displays existing mouse attack binding")
	var player: Node = mission.get_node("Actors/CyberShogun")
	var point := root.get_visible_rect().size * Vector2(0.75, 0.5)
	var motion := InputEventMouseMotion.new()
	motion.position = point
	motion.global_position = point
	motion.relative = Vector2(50, 5)
	root.push_input(motion, true)
	await _frames()
	var aim: Vector2 = player.get("AimDirection")
	_check(aim.length_squared() > 0.99, "isometric feature deliberate mouse movement gives normalized aim")
	var toward_mouse: Vector2 = (player.get_global_mouse_position() - player.global_position).normalized()
	_check(aim.dot(toward_mouse) > 0.99, "isometric feature aim points toward the injected mouse position")
	await _press(MOUSE_BUTTON_LEFT, point)
	await _press(MOUSE_BUTTON_LEFT, point, false)
	_check(player.get_node("Sprite2D").animation == "sword_attack", "isometric feature unhandled left mouse invokes existing attack")
	await _press(MOUSE_BUTTON_RIGHT, point)
	await _press(MOUSE_BUTTON_RIGHT, point, false)
	_check(float(player.get("AbilityCooldownRemaining")) > 0, "isometric feature unhandled right mouse invokes existing ability")
	# A foreground UI button must consume the same right-click before gameplay receives it.
	var blocker := Button.new()
	blocker.position = point - Vector2(70, 30)
	blocker.size = Vector2(140, 60)
	var layer := CanvasLayer.new()
	layer.layer = 110
	mission.add_child(layer)
	layer.add_child(blocker)
	await create_timer(0.4).timeout
	await _frames()
	await _press(MOUSE_BUTTON_LEFT, point)
	_check(player.get_node("Sprite2D").animation != "sword_attack", "isometric feature UI click does not attack underneath")
	await _press(MOUSE_BUTTON_LEFT, point, false)
	layer.free()
	await _pad()
	_check(_visible_label_text(mission).contains("LT"), "isometric feature injected controller shows trigger binding")
	await _capture("isometric_feature_controller_wide")
	root.size = Vector2i(1920, 1080)
	await _keyboard()
	await _capture("isometric_feature_mouse_standard")

	catalog.call("PrepareMission", 2)
	var platformer := await _mount("res://scenes/gameplay/level2.tscn")
	platformer.get_node("ConcreteWildfireIntro").call("Finish")
	await _frames()
	await _keyboard()
	_service.call("ReplaceKeyboardBinding", "jump", _physical_key(KEY_R))
	_check(_visible_label_text(platformer).contains("R: Jump"), "platformer HUD reflects live remap")
	_check(not String(_service.call("GetBindingForDevice", "attack", false)).contains("mouse"), "platformer attack remains keyboard only")
	var platform_player: Node = platformer.get_node("Actors/CyberShogun")
	_service.call("SetStyle", 4, false)
	await _keyboard()
	await _pad_button(JOY_BUTTON_X)
	_check(platform_player.get("ActiveAction") == "primary", "viewport pad attack reaches platformer with forced Keyboard prompt style")
	_check(bool(_service.get("UsingController")) and not bool(_service.get("IsGamepad")), "forced Keyboard style preserves controller gameplay while displaying key labels")
	_service.call("SetStyle", 1, false)
	var recruit := platformer.get_node("Actors/OceanusJoins")
	recruit.call("Trigger")
	_check(not String(platformer.get("PromptText")).is_empty(), "recruitment has action-based prompt")
	await _pad()
	await _frames()
	_check(String(platformer.get("PromptText")).contains("Y"), "recruitment prompt follows controller ability binding")
	var controls: Label
	var subtitle: Label
	for label: Label in platformer.find_children("*", "Label", true, false):
		if label.text.contains(String(_service.call("GetBinding", "jump")) + ": " + tr("Jump")):
			controls = label
		if label.is_visible_in_tree() and label.text.begins_with("AXIOM-7:"):
			subtitle = label
	_check(controls != null and subtitle != null, "platformer controls and active subtitle are observable")
	if controls != null and subtitle != null:
		_check(not controls.get_global_rect().intersects((subtitle.get_parent() as Control).get_global_rect()), "platformer control row stays above visible subtitle panel")
	var squad: Control = platformer.find_child("SquadStrip", true, false)
	if controls != null and squad != null:
		_check(not controls.get_global_rect().intersects(squad.get_global_rect()), "platformer control row stays above squad strip")
	for size: Vector2i in [Vector2i(1920, 1080), Vector2i(2560, 1080)]:
		root.size = size
		for language: String in ["english", "arabic", "german"]:
			locale.call("ApplyLocale", language)
			await _frames()
			await _capture("platformer_%s_%d" % [language, size.x])

	locale.call("ApplyLocale", "english")
	var world := await _mount("res://scenes/world/neo_kyoto.tscn")
	await _frames(30)
	await _keyboard()
	var map_action := InputEventAction.new()
	map_action.action = "world_map"
	map_action.pressed = true
	world.call("_UnhandledInput", map_action)
	await _frames()
	var map: Control = world.find_child("WorldMapViewport", true, false)
	_check(map.is_visible_in_tree(), "world map opens")
	var zoom: float = map.get("Zoom")
	await _press(MOUSE_BUTTON_WHEEL_UP, map.get_global_rect().get_center())
	_check(float(map.get("Zoom")) > zoom, "world map wheel reaches GUI zoom handler")
	await RenderingServer.frame_post_draw
	var map_region := Rect2i(map.get_global_rect())
	var before_drag := hash(root.get_texture().get_image().get_region(map_region).get_data())
	await _press(MOUSE_BUTTON_RIGHT, map.get_global_rect().get_center())
	var drag := InputEventMouseMotion.new()
	drag.position = map.get_global_rect().get_center() + Vector2(35, 12)
	drag.global_position = drag.position
	drag.relative = Vector2(35, 12)
	root.push_input(drag, true)
	await _press(MOUSE_BUTTON_RIGHT, drag.position, false)
	await RenderingServer.frame_post_draw
	var after_drag := hash(root.get_texture().get_image().get_region(map_region).get_data())
	_check(before_drag != after_drag, "world map drag changes the rendered map view")
	await _press(MOUSE_BUTTON_LEFT, map.get_global_rect().get_center())
	await _press(MOUSE_BUTTON_LEFT, map.get_global_rect().get_center(), false)
	_check(bool(world.call("InspectionState").get("waypoint_set", false)), "world map pointer creates waypoint")
	await _capture("world_map_mouse_wide")

	if is_instance_valid(_scene):
		_scene.free()
		_scene = null
	await _frames()
	var cinematic: Node = load("res://scripts/ui/TokyoRemembers.cs").new()
	root.add_child(cinematic)
	_scene = cinematic
	cinematic.set_process(false)
	await _keyboard()
	var skip: Button = cinematic.get_node("UI/Skip")
	_check(skip.text.contains("Escape"), "cinematic Skip displays accepted keyboard bindings")
	await _pad()
	_check(skip.text.contains("A"), "cinematic Skip follows controller labels")
	for size: Vector2i in [Vector2i(1920, 1080), Vector2i(2560, 1080)]:
		root.size = size
		for language: String in ["english", "arabic", "german"]:
			locale.call("ApplyLocale", language)
			await _frames()
			await _capture("cinematic_%s_%d" % [language, size.x])
	await _keyboard()
	var completed := [0]
	cinematic.connect("Finished", func() -> void: completed[0] += 1)
	await _click(skip)
	_check(completed[0] == 1, "cinematic Skip mouse click completes exactly once")
	_scene = null
	await _frames()
	# A new cinematic must consume its actual controller shortcut and complete once.
	var pad_cinematic: Node = load("res://scripts/ui/TokyoRemembers.cs").new()
	root.add_child(pad_cinematic)
	_scene = pad_cinematic
	pad_cinematic.set_process(false)
	await _keyboard()
	var pad_completed := [0]
	pad_cinematic.connect("Finished", func() -> void: pad_completed[0] += 1)
	await _pad_button(JOY_BUTTON_A)
	_check(pad_completed[0] == 1 and bool(_service.get("UsingController")), "consuming cinematic pad Skip completes once and observes device before removal")
	_scene = null
	await _frames()
	var result := {"checks": _checks, "failures": _failures,
		"controller_coverage": "viewport-routed synthetic presses/releases; no physical controller",
		"viewport_sizes": ["1920x1080", "2560x1080"], "locales": ["english", "arabic", "german"]}
	var output := FileAccess.open(OUTPUT + "results.json", FileAccess.WRITE)
	output.store_string(JSON.stringify(result, "  "))
	output.close()
	print("INPUT PROMPT SMOKE: %d checks, %d failures" % [_checks.size(), _failures.size()])
	_finished = true
	quit(0 if _failures.is_empty() else 1)


func _physical_key(code: int) -> InputEventKey:
	var event := InputEventKey.new()
	event.physical_keycode = code
	return event
