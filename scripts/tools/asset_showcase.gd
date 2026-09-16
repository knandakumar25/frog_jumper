extends Node3D
## Isolated asset review scene. Does not touch sessions, character selection or gameplay state.

const MANIFEST := "res://assets/asset_delivery_manifest.json"
var catalog: Array = []
var asset_picker: OptionButton
var clip_picker: OptionButton
var details: Label
var model: Node3D
var animator: AnimationPlayer
var camera: Camera3D
var model_root: Node3D
var ground: MeshInstance3D
var target := Vector3.ZERO
var orbit_yaw := 0.5
var orbit_pitch := 0.22
var distance := 5.0
var dragging := false
var selected := 0
var autoplay := true


func _ready() -> void:
	var file := FileAccess.open(MANIFEST, FileAccess.READ)
	if file == null:
		push_error("Missing asset delivery manifest: " + MANIFEST)
		return
	var data: Variant = JSON.parse_string(file.get_as_text())
	if not data is Dictionary:
		push_error("Invalid asset delivery manifest")
		return
	catalog = data.get("assets", [])
	_create_stage()
	_create_controls()
	if not catalog.is_empty():
		_show_asset(0)


func _create_stage() -> void:
	model_root = Node3D.new()
	model_root.name = "DisplayModel"
	add_child(model_root)
	camera = Camera3D.new()
	camera.fov = 42
	camera.far = 500
	add_child(camera)
	camera.make_current()
	var world := WorldEnvironment.new()
	var env := Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color(0.006, 0.01, 0.022)
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color(0.36, 0.44, 0.62)
	env.ambient_light_energy = 0.6
	env.tonemap_mode = Environment.TONE_MAPPER_ACES
	env.glow_enabled = true
	env.glow_blend_mode = Environment.GLOW_BLEND_MODE_ADDITIVE
	env.glow_hdr_threshold = 0.8
	world.environment = env
	add_child(world)
	for entry in [[Vector3(-35, -30, 0), Color(0.83, 0.92, 1.0), 1.4],
		[Vector3(-25, 140, 0), Color(0.12, 0.7, 1.0), 1.2],
		[Vector3(20, 65, 0), Color(1.0, 0.16, 0.42), 0.8]]:
		var light := DirectionalLight3D.new()
		light.rotation_degrees = entry[0]
		light.light_color = entry[1]
		light.light_energy = entry[2]
		add_child(light)
	ground = MeshInstance3D.new()
	var plane := PlaneMesh.new()
	plane.size = Vector2(200, 200)
	ground.mesh = plane
	var material := StandardMaterial3D.new()
	material.albedo_color = Color(0.018, 0.027, 0.044)
	material.roughness = 0.52
	ground.material_override = material
	ground.position.y = -0.03
	add_child(ground)


func _create_controls() -> void:
	var layer := CanvasLayer.new()
	add_child(layer)
	var panel := PanelContainer.new()
	panel.set_anchors_and_offsets_preset(Control.PRESET_TOP_WIDE)
	panel.offset_left = 24
	panel.offset_right = -24
	panel.offset_top = 24
	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.01, 0.024, 0.045, 0.94)
	style.border_color = Color(0.05, 0.8, 1.0, 0.65)
	style.set_border_width_all(1)
	style.content_margin_left = 18
	style.content_margin_right = 18
	style.content_margin_top = 14
	style.content_margin_bottom = 14
	panel.add_theme_stylebox_override("panel", style)
	layer.add_child(panel)
	var column := VBoxContainer.new()
	column.add_theme_constant_override("separation", 9)
	panel.add_child(column)
	var title := Label.new()
	title.text = "AXIOM FORCE  /  NEON PULSE ASSET REVIEW"
	title.add_theme_font_size_override("font_size", 22)
	title.add_theme_color_override("font_color", Color(0.25, 0.9, 1.0))
	column.add_child(title)
	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 12)
	column.add_child(row)
	asset_picker = OptionButton.new()
	asset_picker.custom_minimum_size.x = 320
	for asset in catalog:
		asset_picker.add_item(str(asset["name"]))
	asset_picker.item_selected.connect(_show_asset)
	row.add_child(asset_picker)
	clip_picker = OptionButton.new()
	clip_picker.custom_minimum_size.x = 210
	clip_picker.item_selected.connect(_play_selected_clip)
	row.add_child(clip_picker)
	var replay := Button.new()
	replay.text = "Replay clip"
	replay.pressed.connect(func(): _play_selected_clip(clip_picker.selected))
	row.add_child(replay)
	var turntable := CheckButton.new()
	turntable.text = "Turntable"
	turntable.button_pressed = autoplay
	turntable.toggled.connect(func(value: bool): autoplay = value)
	row.add_child(turntable)
	var home := Button.new()
	home.text = "Reset view"
	home.pressed.connect(func(): _show_asset(selected))
	row.add_child(home)
	details = Label.new()
	details.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	column.add_child(details)
	var help := Label.new()
	help.text = "Right-drag: orbit   •   Wheel: zoom   •   Q / E: previous / next asset   •   Space: replay"
	help.add_theme_color_override("font_color", Color(0.62, 0.74, 0.86))
	column.add_child(help)


func _show_asset(index: int) -> void:
	selected = wrapi(index, 0, catalog.size())
	asset_picker.select(selected)
	if is_instance_valid(model):
		model_root.remove_child(model)
		model.queue_free()
	animator = null
	clip_picker.clear()
	var entry: Dictionary = catalog[selected]
	var path: String = entry["path"]
	var packed := load(path) as PackedScene
	if packed == null:
		details.text = "Could not load " + path
		return
	model = packed.instantiate() as Node3D
	model_root.add_child(model)
	model_root.rotation = Vector3.ZERO
	var bounds := _model_bounds(model)
	target = bounds.get_center()
	var span := maxf(bounds.size.x, maxf(bounds.size.y, bounds.size.z))
	distance = maxf(2.0, span * 2.25)
	orbit_pitch = 0.2 if str(entry["category"]) in ["operative", "enemy"] else 0.62
	orbit_yaw = 0.45
	ground.position.y = bounds.position.y - 0.025
	animator = model.find_child("AnimationPlayer", true, false) as AnimationPlayer
	if animator == null:
		var players := model.find_children("*", "AnimationPlayer", true, false)
		if not players.is_empty():
			animator = players[0] as AnimationPlayer
	if animator != null:
		for clip in animator.get_animation_list():
			if clip != "RESET":
				clip_picker.add_item(clip)
		for i in clip_picker.item_count:
			if clip_picker.get_item_text(i) == "Idle":
				clip_picker.select(i)
		_play_selected_clip(clip_picker.selected)
	clip_picker.disabled = clip_picker.item_count == 0
	details.text = "%s  |  bounds %.2f × %.2f × %.2f m  |  %d clips\n%s" % [
		str(entry["category"]).to_upper(), bounds.size.x, bounds.size.y, bounds.size.z,
		clip_picker.item_count, path]
	_update_camera()


func _model_bounds(root_node: Node3D) -> AABB:
	var result := AABB()
	var has_bounds := false
	for child in root_node.find_children("*", "MeshInstance3D", true, false):
		var mesh := child as MeshInstance3D
		var local_bounds := mesh.get_aabb()
		for corner in 8:
			var point := mesh.global_transform * local_bounds.get_endpoint(corner)
			if not has_bounds:
				result = AABB(point, Vector3.ZERO)
				has_bounds = true
			else:
				result = result.expand(point)
	return result


func _play_selected_clip(index: int) -> void:
	if animator != null and index >= 0 and index < clip_picker.item_count:
		animator.stop()
		animator.play(clip_picker.get_item_text(index))


func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_RIGHT:
			dragging = event.pressed
		elif event.pressed and event.button_index == MOUSE_BUTTON_WHEEL_UP:
			distance = maxf(0.3, distance * 0.88)
		elif event.pressed and event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			distance = minf(200.0, distance * 1.12)
	elif event is InputEventMouseMotion and dragging:
		orbit_yaw -= event.relative.x * 0.008
		orbit_pitch = clampf(orbit_pitch + event.relative.y * 0.008, -0.1, 1.4)
	elif event is InputEventKey and event.pressed and not event.echo:
		match event.physical_keycode:
			KEY_Q: _show_asset(selected - 1)
			KEY_E: _show_asset(selected + 1)
			KEY_SPACE: _play_selected_clip(clip_picker.selected)


func _process(delta: float) -> void:
	if camera == null:
		return
	if autoplay and not dragging:
		orbit_yaw += delta * 0.12
	_update_camera()


func _update_camera() -> void:
	camera.position = target + distance * Vector3(sin(orbit_yaw) * cos(orbit_pitch), sin(orbit_pitch), cos(orbit_yaw) * cos(orbit_pitch))
	camera.look_at(target)
